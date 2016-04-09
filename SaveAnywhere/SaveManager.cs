﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SaveAnywhere
{
    public class SaveManager
    {
        private static readonly string tempSaveSuffix = "_TEMP";
        private static readonly int SaveCompleteFlag = 100;
        private static readonly string rootSavePath = Path.Combine(Constants.DataPath, "Mods", "SaveAnywhere");

        private string currentSaveFileName;
        private string currentSaveFileDir;
        private string currentSaveFilePath;

        public SaveManager()
        {
            currentSaveFileName = Game1.player.name + "_" + Game1.uniqueIDForThisGame;
            currentSaveFileDir = Path.Combine(rootSavePath, currentSaveFileName);
            currentSaveFilePath = Path.Combine(currentSaveFileDir, currentSaveFileName);
        }

        // TODO: create/store backups of players saves first
        // and maybe store up to 5 backups or something
        public void Save()
        {
            Log.Info("[SaveAnywhere] Saving game...");

            // Run the regular game save
            IEnumerator<int> saveEnumerator = SaveGame.Save();
            while (saveEnumerator.MoveNext())
            {
                if (saveEnumerator.Current == SaveCompleteFlag)
                {
                    Log.Debug("Regular game save finished saving");
                }
            }

            try
            {
                // Create the save directory for this user if it doesn't exist
                Directory.CreateDirectory(currentSaveFileDir);

                // Serialize the game data
                IMessage message = PopulateMessage(SaveData.Game1.Descriptor, Game1.game1);

                message = PostSaveFixup((SaveData.Game1)message);

                WriteToSaveFile(message);

                // TODO: create events for this so we can display the text on the screen
                Log.Info("[SaveAnywhere] Save complete!");
            }
            catch (Exception ex)
            {
                Log.Error("[SaveAnywhere] Error saving data: " + ex);
            }
        }

        public void Load()
        {
            try
            {
                // Load normally if we don't find one of our save files
                if (!File.Exists(currentSaveFilePath))
                {
                    Log.Info("[SaveAnywhere] No custom save file found; loading normally.");
                    return;
                }

                Log.Info("[SaveAnywhere] Loading game...");


                // Deserialize the game data
                SaveData.Game1 game;
                using (var input = File.OpenRead(currentSaveFilePath))
                {
                    game = SaveData.Game1.Parser.ParseFrom(input);
                }

                // Set the corresponding game values to our saved ones
                DePopulateMessage(game, Game1.game1);

                // Do any manual adjustments like warping the player
                PostLoadFixup(game);

                Log.Info("[SaveAnywhere] Load complete!");
            }
            catch (Exception e)
            {
                Log.Error("[SaveAnywhere] Failed to load data: " + e);
            }
        }

        public void ClearSave()
        {
            // Delete our save file so that if the player exits after waking up
            // then we won't accidently set the wrong info when they next load.
            DeleteFile(currentSaveFilePath);
        }

        private void PostLoadFixup(SaveData.Game1 game)
        {
            // TODO: find a way to automate this if there are lots of cases
            // Resolve stuff that can't be assigned
            SaveData.Farmer player = game.Player;

            // Temp hack: reset current location before we warp or else it breaks
            Game1.player.currentLocation.name = "FarmHouse";

            Game1.warpFarmer(player.CurrentLocation.Name,
                (int)(player.Position.X / Game1.tileSize),
                (int)(player.Position.Y / Game1.tileSize), false);
            Game1.player.faceDirection(player.FacingDirection);

            var buffs = game.BuffsDisplay.Buffs;
            Game1.buffsDisplay.otherBuffs = new List<Buff>();
            for (int i = 0; i < buffs.Count; ++i)
            {
                var buff = new Buff(-1);
                DePopulateMessage(buffs[i], buff);

                if (!Game1.buffsDisplay.hasBuff(buff.which))
                    Game1.buffsDisplay.addOtherBuff(buff);
            }
        }

        private IMessage PostSaveFixup(SaveData.Game1 game)
        {
            var buffsDict = (Dictionary<ClickableTextureComponent, Buff>)TypeUtils.GetPrivateFieldData("buffs", Game1.buffsDisplay);
            var buffs = buffsDict.Values.ToList().Concat(Game1.buffsDisplay.otherBuffs);
            foreach (var buff in buffs)
            {
                game.BuffsDisplay.Buffs.Add((SaveData.Buff)PopulateMessage(SaveData.Buff.Descriptor, buff));
            }
            return game;
        }

        private IMessage PopulateMessage(MessageDescriptor descriptor, object instance)
        {
            // Create an instance of the message
            IMessage message = (IMessage)Activator.CreateInstance(descriptor.ClrType);
            string messageCLRName = descriptor.ClrType.Name;

            foreach (var field in descriptor.Fields.InDeclarationOrder())
            {
                // If we don't find the value we'll assume it will be assigned manually later
                object value = EnsureField(field.Name, instance);
                if (value == null)
                {
                    Log.Debug("Value for field: " + field.Name + " is null; Leaving as default.");
                    continue;
                }

                if (field.IsRepeated)
                {
                    // We only have the type itself, not the type of collection.
                    // It's pretty unlikely we'll find a collection of the same type with the same name (i hope).
                    if (TypeUtils.IsEnumerableOfType(value, GetFieldTypeName(field)))
                    {
                        // Get the repeated field as a list
                        var list = (IList)field.Accessor.GetValue(message);
                        foreach (var item in (IEnumerable)value)
                        {
                            // If the item is a complex type then recursively create and assign
                            if (field.FieldType == FieldType.Message)
                            {
                                IMessage fieldMessage = PopulateMessage(field.MessageType, item);
                                list.Add(fieldMessage);
                            }
                            else
                            {
                                list.Add(item);
                            }
                        }
                    }
                    continue;
                }

                // We can only assign native types at the moment. This means any
                // complex ones must have a proto message representation so we can
                // recursively resolve it and assign the value.
                if (field.FieldType == FieldType.Message)
                {
                    // Find the field that matches the name from the current instance
                    // and run this on it's instance, eventually giving us the correct value.
                    IMessage fieldMessage = PopulateMessage(field.MessageType, value);
                    field.Accessor.SetValue(message, fieldMessage);
                    continue;
                }

                field.Accessor.SetValue(message, value);
            }
            return message;
        }

        // TODO: think of a better name
        private void DePopulateMessage(IMessage message, object instance)
        {
            foreach (var field in message.Descriptor.Fields.InDeclarationOrder())
            {
                // Get the corresponding field from the object we're deserializing into
                var fieldInfo = TypeUtils.GetField(field.Name, instance);
                if (fieldInfo == null)
                {
                    Log.Debug("Could not find field info for: " + field.Name + "; skipping.");
                    continue;
                }

                // Get the stored value for this field
                object fieldValue = field.Accessor.GetValue(message);
                if (fieldValue == null)
                {
                    Log.Debug("Value for in-field: " + field.Name + " is null. skipping.");
                    continue;
                }

                object outValue = fieldInfo.GetValue(instance);
                if (outValue == null && field.FieldType == FieldType.Message) // we'll need it's fields, so it must be a message
                {
                    Log.Debug("Value for member: " + field.Name + " is null. Creating instance...");
                    outValue = CreateInstance(fieldInfo, (IMessage)fieldValue);
                    if (outValue == null)
                        continue;

                    fieldInfo.SetValue(instance, outValue);
                }


                if (field.IsRepeated)
                {
                    IList src = (IList)fieldValue;

                    // TODO: maybe supports dicts
                    var genericTypeArgs = TypeUtils.GetGenericArgTypes(fieldInfo.FieldType);
                    if (genericTypeArgs.Length > 1)
                    {
                        Log.Debug("Unsupported number of generic arguments for field: " + field.Name + ". Currently only single argument generic containers such as lists are allowed.");
                        continue;
                    }

                    Type listType = (genericTypeArgs.Length == 1) ? genericTypeArgs[0] : null;
                    if (listType == null)
                    {
                        Log.Debug("Could not get list type for field: " + field.Name);
                        continue;
                    }

                    if (outValue == null)
                    {
                        outValue = Array.CreateInstance(listType, src.Count);
                        fieldInfo.SetValue(instance, outValue);
                    }

                    //IList dest = (IList)TypeUtils.CreateGenericList(listType);
                    for (int i = 0; i < src.Count; ++i)
                    {
                        if (field.FieldType == FieldType.Message)
                        {
                            var item = Activator.CreateInstance(listType);
                            DePopulateMessage((IMessage)src[i], item);
                        }
                        else
                        {
                            // Assign it directly to the element
                            (fieldInfo.GetValue(instance) as IList)[i] = src[i];
                            //dest.Add(src[i]);
                        }
                    }
                }
                else if (field.FieldType == FieldType.Message)
                {
                    //DePopulateMessage((IMessage)field.Accessor.GetValue(message), value);
                    DePopulateMessage((IMessage)fieldValue, fieldInfo.GetValue(instance));
                }
                else
                {
                    fieldInfo.SetValue(instance, fieldValue);
                }
            }
        }

        private object CreateInstance(FieldInfo fieldInfo, IMessage fieldMsg)
        {
            object instance = null;
            // 1. get all the ctors from the object
            // 2. checks the params of each and see if we have any members that match
            // 3. if we find a ctor whose params we have all of then create it

            //ConstructorInfo chosenCtor = null;
            //var ctors = fieldInfo.FieldType.GetConstructors();
            //List<object> paramValues = new List<object>();
            //foreach (var ctor in ctors)
            //{
            //    if (chosenCtor != null)
            //        break;

            //    var ctorparams = ctor.GetParameters();
            //    foreach (var param in ctorparams)
            //    {
            //        foreach (var f in fieldMsg.Descriptor.Fields.InDeclarationOrder())
            //        {
            //            if (param.Name == f.Name)
            //            {
            //                chosenCtor = ctor;
            //                paramValues.Add(f.Accessor.GetValue(fieldMsg));
            //                break;
            //            }
            //            else
            //            {
            //                chosenCtor = null;
            //                paramValues.Clear();
            //            }
            //        }
            //    }
            //}

            //if (chosenCtor == null)
            //{
            //    Log.Debug("No suitable constructor found for: " + fieldInfo.Name);
            //}

            //var ctorParams = chosenCtor.GetParameters();
            //instance = Activator.CreateInstance(fieldInfo.FieldType, paramValues.ToArray());

            instance = FormatterServices.GetUninitializedObject(fieldInfo.FieldType);

            return instance;
        }

        private void WriteToSaveFile(IMessage message)
        {
            // Write to a temp file
            string tempSavePath = currentSaveFilePath + tempSaveSuffix;
            try
            {
                using (var output = File.Create(tempSavePath))
                {
                    message.WriteTo(output);
                }
            }
            catch (Exception e)
            {
                Log.Error("[SaveAnywhere] Failed to write to temp save file: " + e);
                return;
            }

            // Remove the current save file
            DeleteFile(currentSaveFilePath);

            try
            {
                // Make the temp one the new one
                File.Move(tempSavePath, currentSaveFilePath);
            }
            catch (Exception e)
            {
                Log.Error("[SaveAnywhere] Failed to rename " + tempSavePath + " to: " + currentSaveFilePath + ":\n" + e);
            }
        }
        
        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                Log.Debug("Deleting: " + path);
                try
                {
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    Log.Error("[SaveAnywhere] Error deleting file: " + path + ".\n" + e);
                }
            }
        }

        private string GetFieldTypeName(FieldDescriptor descriptor)
        {
            if (descriptor.FieldType == FieldType.Message)
            {
                return descriptor.MessageType.Name;
            }
            return descriptor.FieldType.ToString();
        }

        private bool IsFieldDefaultValue(FieldDescriptor descriptor, IMessage message)
        {
            object value = descriptor.Accessor.GetValue(message);
            switch (descriptor.FieldType)
            {
                case FieldType.Message: // Defaults to null
                    return (value == null);
                case FieldType.Bytes: // Defaults to empty bytestring
                    return (value as ByteString).IsEmpty;
                case FieldType.Bool: // Defaults to false
                    return !value.AsBool();
                case FieldType.String:
                    return ((value as String).Length == 0);
            }
            // I'm hoping all the numeric and enum types will cast to int fine
            return ((int)value == 0);
        }

        private static object EnsureField(string fieldName, object instance)
        {
            var fieldInfo = TypeUtils.GetField(fieldName, instance);
            if (fieldInfo == null)
            {
                Log.Debug("Could not find value for: " + fieldName);
                return null;
            }
            return fieldInfo.GetValue(instance);
        }

        private static T CheckProtoFieldNotNull<T>(T value, FieldDescriptor descriptor)
        {
            if (value == null)
            {
                throw new ArgumentNullException(descriptor.Name, "Proto message for type: " + descriptor.FieldType + " is probably not implemented");
            }
            return value;
        }
    }
}
