using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace SaveAnywhere
{
    public class SaveManager
    {
        private static readonly int SaveCompleteFlag = 100;
        private static readonly string savePath = Path.Combine(Constants.DataPath, "Mods", "SaveAnywhere");


        // TODO: create/store backups of players saves first
        // and maybe store up to 5 backups or something
        public static void Save()
        {
            IEnumerator<int> saveEnumerator = SaveGame.Save();
            while (saveEnumerator.MoveNext())
            {
                if (saveEnumerator.Current == SaveCompleteFlag)
                {
                    Log.Debug("Finished saving");
                }
            }

            try
            {
                string saveFile = Path.Combine(savePath, "currentsave");
                Directory.CreateDirectory(savePath);

                IMessage message = PopulateMessage(SaveData.Game1.Descriptor, Game1.game1);

                using (var output = File.Create(saveFile))
                {
                    message.WriteTo(output);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error saving data: " + ex);
            }
        }

        public static void Load()
        {
            try
            {
                string saveFile = Path.Combine(savePath, "currentsave");
                if (!File.Exists(saveFile))
                {
                    Log.Info("No save file found");
                    return;
                }

                SaveData.Game1 game;
                using (var input = File.OpenRead(saveFile))
                {
                    game = SaveData.Game1.Parser.ParseFrom(input);
                }

                DePopulateMessage(game, Game1.game1);

                // TODO: find a way to automate this if there are lots of cases
                // Resolve stuff that can't be assigned
                SaveData.Farmer player = game.Player;
                Game1.warpFarmer(player.CurrentLocation.Name,
                    (int)(player.Position.X / Game1.tileSize),
                    (int)(player.Position.Y / Game1.tileSize), false);
                Game1.player.faceDirection(player.FacingDirection);
            }
            catch (Exception e)
            {
                Log.Error("Failed to load data: " + e);
            }
        }

        public static void ClearSave()
        {
            // Delete our save file so that if the player exits after waking up
            // then we won't accidently set the wrong info when they next load.
            string saveFile = Path.Combine(savePath, "currentsave");
            if (File.Exists(saveFile))
            {
                Log.Debug("Deleting custom save file");
                File.Delete(saveFile);
            }
        }

        private static IMessage PopulateMessage(MessageDescriptor descriptor, object instance)
        {
            // Create an instance of the message
            IMessage message = (IMessage)Activator.CreateInstance(descriptor.ClrType);
            string messageCLRName = descriptor.ClrType.Name;

            foreach (var field in descriptor.Fields.InDeclarationOrder())
            {
                // We might be able to get away with this, but there may be cases were it is supposed to be null (ie. thing hasn't spawned)
                object value = CheckProtoFieldNotNull(GetFieldData(field.Name, instance), field);

                // We can only assign native types at the moment. This means any
                // complex ones must have a proto message representation so we can
                // recursively resolve it and assign the value.
                if (field.FieldType == FieldType.Message)
                {
                    // Find the field that matches the name from the current instance
                    // and run this on it's instance, eventually giving us the correct value.
                    IMessage fieldMessage = PopulateMessage(field.MessageType, value);
                    field.Accessor.SetValue(message, fieldMessage);
                }
                else
                {
                    //object value = GetFieldData(field.Name, instance);
                    if (value == null)
                    {
                        // For now we'll just leave it as it's default value, but still report it
                        Log.Error("value for " + field.Name + " is null");
                        continue;
                    }
                    field.Accessor.SetValue(message, value);
                }
            }
            return message;
        }

        // TODO: think of a better name
        private static void DePopulateMessage(IMessage message, object instance)
        {
            foreach (var field in message.Descriptor.Fields.InDeclarationOrder())
            {
                //object value = CheckProtoFieldNotNull(GetFieldData(field.Name, instance), field);
                object value = field.Accessor.GetValue(message);
                if (field.FieldType == FieldType.Message)
                {
                    DePopulateMessage((IMessage)field.Accessor.GetValue(message), value);
                }
                else
                {
                    SetFieldData(field.Name, instance, value);
                }
            }
        }

        private static T CheckProtoFieldNotNull<T>(T value, FieldDescriptor descriptor)
        {
            if (value == null)
            {
                throw new ArgumentNullException(descriptor.Name, "Proto message for type: " + descriptor.FieldType + " is probably not implemented");
            }
            return value;
        }

        private static Type ResolveTypeFromAssembly(Assembly assembly, string objectName)
        {
            Type type = null;

            // TODO: only do this once and store it as a member of wherever this method is moved to
            var namespaces = GetSDVAssemblyNamespaces(assembly);
            foreach (var nspace in namespaces)
            {
                type = Type.GetType(nspace + "." + objectName);
                if (type != null)
                {
                    break;
                }
            }
            return type;
        }

        private static IEnumerable<string> GetSDVAssemblyNamespaces(Assembly assembly)
        {
            return assembly.GetTypes()
                .Select(t => t.Namespace)
                .Where(n => n != null)
                .Distinct();
        }

        private static FieldInfo GetField(string fieldName, object instance)
        {
            return instance.GetType().GetField(fieldName,
                  BindingFlags.IgnoreCase
                | BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.Public
                | BindingFlags.Static
                | BindingFlags.FlattenHierarchy
                );
        }

        private static object GetFieldData(string fieldName, object instance)
        {
            return GetField(fieldName, instance)?.GetValue(instance);
        }

        private static void SetFieldData(string fieldName, object instance, object value)
        {
            GetField(fieldName, instance)?.SetValue(instance, value);
        }
    }
}
