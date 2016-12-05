using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Newtonsoft.Json;
using System.Diagnostics;

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

        private IModHelper modHelper;
        private IMonitor monitor;

        private delegate void SerializationMethod(object o, object data);

        public SaveManager(IModHelper modHelper, IMonitor monitor)
        {
            this.modHelper = modHelper;
            this.monitor = monitor;

            currentSaveFileName = Game1.player.name + "_" + Game1.uniqueIDForThisGame;
            currentSaveFileDir = Path.Combine(rootSavePath, currentSaveFileName);
            currentSaveFilePath = Path.Combine(currentSaveFileDir, currentSaveFileName);
        }

        // TODO: create/store backups of players saves first
        // and maybe store up to 5 backups or something
        public void Save()
        {
            monitor.Log("Saving game...");

            // Run the regular game save
            IEnumerator<int> saveEnumerator = SaveGame.Save();
            while (saveEnumerator.MoveNext())
            {
                if (saveEnumerator.Current == SaveCompleteFlag)
                {
                    monitor.Log("Regular game save finished saving");
                }
            }

            try
            {
                // Create the save directory for this user if it doesn't exist
                Directory.CreateDirectory(currentSaveFileDir);

                // Serialize the game data
                //IMessage message = PopulateMessage(SaveData.Game1.Descriptor, Game1.game1);
                SaveData.GameData gameData = SerializeGameData(Game1.game1);

                //message = PostSaveFixup((SaveData.Game1)message);

                WriteToSaveFile(gameData);

                // TODO: create events for this so we can display the text on the screen
                monitor.Log("Save complete!");
            }
            catch (Exception ex)
            {
                monitor.Log($"Error saving data: {ex}", LogLevel.Error);
            }
        }

        public void Load()
        {
            try
            {
                // Load normally if we don't find one of our save files
                if (!File.Exists(currentSaveFilePath))
                {
                    monitor.Log("No custom save file found; loading normally.");
                    return;
                }

                monitor.Log("Loading game...");

                PreLoadSetup();

                // Deserialize the game data
                SaveData.GameData gameData = modHelper.ReadJsonFile<SaveData.GameData>(currentSaveFilePath);
                if (gameData == null)
                {
                    monitor.Log("Failed to load game data", LogLevel.Warn);
                    return;
                }

                DeserializeGameData(gameData, Game1.game1);

                // Do any manual adjustments like warping the player
                PostLoadFixup(gameData);

                monitor.Log("Load complete!");
            }
            catch (Exception e)
            {
                monitor.Log($"Failed to load data: {e}", LogLevel.Error);
            }
        }

        public void ClearSave()
        {
            // Delete our save file so that if the player exits after waking up
            // then we won't accidently set the wrong info when they next load.
            try
            {
                Utils.DeleteFile(currentSaveFilePath);
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to delete file: {currentSaveFilePath}: {ex}", LogLevel.Error);
            }
        }

        private void PreLoadSetup()
        {
            // Reset all the stats so they don't over-accumulate
            Game1.player.addedCombatLevel = 0;
            Game1.player.addedFarmingLevel = 0;
            Game1.player.addedFishingLevel = 0;
            Game1.player.addedForagingLevel = 0;
            Game1.player.addedLuckLevel = 0;
            Game1.player.addedMiningLevel = 0;
            Game1.player.addedSpeed = 0;
        }

        private SaveData.GameData SerializeGameData(Game1 game)
        {
            SaveData.GameData gameData = new SaveData.GameData();

            SerializeObject(game, gameData);

            return gameData;
        }

        private void SerializeObject(object o, SaveData.BaseData data)
        {
            if (o == null)
            {
                return;
            }

            foreach (var field in data.GetType().GetProperties())
            {
                var nativeField = TypeUtils.GetField(field.Name, o);
                if (nativeField?.GetValue(o) == null)
                {
                    field.SetValue(data, null);
                    continue;
                }

                // Recursively follow base data types until we come to a native type
                if (TypeUtils.HasCustomAttribute<SaveData.NativeClassDataAttribute>(field.PropertyType))
                {
                    var nativeObject = nativeField?.GetValue(o);
                    monitor.Log($"{field.Name} is a native data wrapper, going deeping");
                    SerializeObject(nativeObject, (SaveData.BaseData)field.GetValue(data));
                }
                else if (TypeUtils.HasCustomAttribute<SaveData.NativePropertyDataAttribute>(field))
                {
                    monitor.Log($"{field.Name} has a NativePropertyDataAttribute");

                    var attribute = field.GetCustomAttribute<SaveData.NativePropertyDataAttribute>();
                    if (attribute.IsCollection)
                    {
                        // Handle complex collection serialization (aka converting types to wrapper types etc.)
                        var wrapperCollection = field.GetValue(data) as ICollection;
                        var nativeCollection = nativeField.GetValue(o) as ICollection;

                        ICollection serializeCollection = wrapperCollection;
                        if (nativeCollection is IDictionary)
                        {
                            SerializeDictionary((IDictionary)nativeCollection, (IDictionary)wrapperCollection, (IDictionary)serializeCollection, (a, b) =>
                            {
                                SerializeObject(a, b as SaveData.BaseData);
                            });
                        }
                        else if (nativeCollection is IList)
                        {
                            SerializeNativeCollectionToWrapper(nativeCollection, wrapperCollection, (IList)serializeCollection, (a, b) =>
                            {
                                SerializeObject(a, b as SaveData.BaseData);
                            });
                        }
                        else
                        {
                            monitor.Log("Collection type not supported", LogLevel.Warn);
                        }

                        field.SetValue(data, serializeCollection);
                    }
                }
                else
                {
                    // Set the value of the data member with the native member
                    if (nativeField != null)
                    {
                        field.SetValue(data, nativeField.GetValue(o));
                        monitor.Log($"Setting field: {field.Name} with value: {nativeField.GetValue(o)}");
                    }
                    else
                    {
                        monitor.Log($"Failed to get field: {field.Name} from object: {o?.ToString()}");
                    }
                }
            }
        }

        private void DeserializeGameData(SaveData.GameData gameData, Game1 game)
        {
            DeserializeObject(game, gameData);
        }

        private void DeserializeObject(object o, SaveData.BaseData data)
        {
            if (o == null)
                return;

            foreach (var field in data.GetType().GetProperties())
            {
                if (field.GetValue(data) == null)
                    continue;

                var nativeField = TypeUtils.GetField(field.Name, o);

                // Recursively follow base data types until we come to a native type
                if (TypeUtils.HasCustomAttribute<SaveData.NativeClassDataAttribute>(field.PropertyType))
                {
                    var nativeObject = nativeField?.GetValue(o);
                    if (nativeObject == null)
                    {
                        monitor.Log($"Native object for {field.Name} is null; creating instance.");
                        nativeObject = FormatterServices.GetUninitializedObject(nativeField.FieldType);
                    }

                    monitor.Log($"{field.Name} is a native data wrapper, going deeper");
                    DeserializeObject(nativeObject, (SaveData.BaseData)field.GetValue(data));
                    nativeField.SetValue(o, nativeObject);
                }
                else if (TypeUtils.HasCustomAttribute<SaveData.NativePropertyDataAttribute>(field))
                {
                    monitor.Log($"{field.Name} has a NativePropertyDataAttribute");

                    var attribute = field.GetCustomAttribute<SaveData.NativePropertyDataAttribute>();
                    if (attribute.IsCollection)
                    {
                        // Handle complex collection serialization (aka converting types to wrapper types etc.)
                        var wrapperCollection = field.GetValue(data) as ICollection;
                        var nativeCollection = nativeField.GetValue(o) as ICollection;

                        ICollection serializeCollection = nativeCollection;
                        if (nativeCollection is IDictionary)
                        {
                            //serializeCollection = SerializeDictionary((IDictionary)nativeCollection, (IDictionary)wrapperCollection, DeserializeObject);
                            SerializeDictionary((IDictionary)wrapperCollection, (IDictionary)nativeCollection, (IDictionary)serializeCollection, (a, b) =>
                            {
                                DeserializeObject(b, a as SaveData.BaseData);
                            });
                        }
                        else if (nativeCollection is IList)
                        {
                            SerializeNativeCollectionToWrapper(wrapperCollection, nativeCollection, (IList)serializeCollection, (a, b) =>
                            {
                                DeserializeObject(b, a as SaveData.BaseData);
                            });
                        }
                        else
                        {
                            monitor.Log("Collection type not supported", LogLevel.Warn);
                        }

                        nativeField.SetValue(o, serializeCollection);
                    }
                }
                else
                {
                    // Set the value of the data member with the native member
                    if (nativeField != null)
                    {
                        nativeField.SetValue(o, field.GetValue(data));
                        monitor.Log($"Setting field: {field.Name} with value: {field.GetValue(data)}");
                    }
                    else
                    {
                        monitor.Log($"Failed to get field: {field.Name} from object: {o?.ToString()}");
                    }
                }
            }
        }

        // Convert from native to wrapper
        private void SerializeDictionary(IDictionary native, IDictionary wrapper, IDictionary outDict, SerializationMethod serialMethod)
        {
            Debug.Assert(native != null && wrapper != null);

            Type wrapperKeyType = wrapper.GetType().GetGenericArguments()[0];
            Type wrapperValueType = wrapper.GetType().GetGenericArguments()[1];

            foreach (DictionaryEntry entry in native)
            {
                object wrapperKey = entry.Key;
                object wrapperValue = entry.Value;

                if (entry.Key.GetType() != wrapperKeyType)
                {
                    wrapperKey = Activator.CreateInstance(wrapperKeyType);
                    serialMethod(entry.Key, wrapperKey);
                }
                if (entry.Value.GetType() != wrapperValueType)
                {
                    wrapperValue = Activator.CreateInstance(wrapperValueType);
                    serialMethod(entry.Value, wrapperValue);
                }
                outDict.Add(wrapperKey, wrapperValue);
            }
        }

        //TODO: fix naming to be more generic from one collection to another as we use it both ways
        private void SerializeNativeCollectionToWrapper(ICollection native, ICollection wrapper, IList outCollection, SerializationMethod serialMethod)
        {
            // If they're the same type then just use the native one
            Type nativeType = native.GetType().GetGenericArguments()[0];
            Type wrapperType = wrapper.GetType().GetGenericArguments()[0];

            foreach (var item in native)
            {
                var wrapperItem = item;
                if (nativeType != wrapperType)
                {
                    //wrapperItem = Activator.CreateInstance(wrapperType);
                    wrapperItem = FormatterServices.GetUninitializedObject(wrapperType);
                    serialMethod(item, wrapperItem);
                }
                outCollection.Add(wrapperItem);
            }
        }

        private void PostLoadFixup(SaveData.GameData gameData)
        {
            // TODO: find a way to automate this if there are lots of cases
            // Resolve stuff that can't be assigned
            SaveData.FarmerData player = gameData.player;

            // Temp hack: reset current location before we warp or else it breaks
            Game1.player.currentLocation.name = "FarmHouse";

            Game1.warpFarmer(player.currentLocation.name,
                (int)(player.position.X / Game1.tileSize),
                (int)(player.position.Y / Game1.tileSize), false);
            Game1.player.faceDirection(player.facingDirection);

            // Force it to re-evaluate the buffs it has and display the correct icons
            Game1.buffsDisplay.syncIcons();
        }

        private void WriteToSaveFile(SaveData.GameData gameData)
        {
            // Write to a temp file
            string tempSavePath = currentSaveFilePath + tempSaveSuffix;
            try
            {
                Utils.WriteJsonFile(MakeSavePath(tempSavePath), gameData);
            }
            catch (Exception e)
            {
                monitor.Log($"Failed to write to temp save file: {e}", LogLevel.Error);
                return;
            }

            // Remove the current save file
            Utils.DeleteFile(currentSaveFilePath);

            try
            {
                // Make the temp one the new one
                File.Move(tempSavePath, currentSaveFilePath);
            }
            catch (Exception e)
            {
                monitor.Log($"Failed to rename {tempSavePath} to {currentSaveFilePath}:\n {e}");
            }
        }

        private object EnsureField(string fieldName, object instance)
        {
            var fieldInfo = TypeUtils.GetField(fieldName, instance);
            if (fieldInfo == null)
            {
                monitor.Log($"Could not find value for: {fieldName}");
                return null;
            }
            return fieldInfo.GetValue(instance);
        }

        private string MakeSavePath(string path)
        {
            return Path.Combine(modHelper.DirectoryPath, path);
        }
    }
}
