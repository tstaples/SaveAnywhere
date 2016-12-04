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
            foreach (var field in data.GetType().GetProperties())
            {
                var nativeField = TypeUtils.GetField(field.Name, o);

                // Recursively follow base data types until we come to a native type
                if (field.PropertyType?.GetCustomAttribute(typeof(SaveData.NativeClassDataWrapperAttribute)) != null)
                {
                    var nativeObject = nativeField?.GetValue(o);
                    monitor.Log($"{field.Name} is a native data wrapper, doing deeping");
                    SerializeObject(nativeObject, (SaveData.BaseData)field.GetValue(data));
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
            foreach (var field in data.GetType().GetProperties())
            {
                var nativeField = TypeUtils.GetField(field.Name, o);

                // Recursively follow base data types until we come to a native type
                if (field.PropertyType?.GetCustomAttribute(typeof(SaveData.NativeClassDataWrapperAttribute)) != null)
                {
                    var nativeObject = nativeField?.GetValue(o);
                    if (nativeObject != null)
                    {
                        monitor.Log($"{field.Name} is a native data wrapper, doing deeping");
                        DeserializeObject(nativeObject, (SaveData.BaseData)field.GetValue(data));
                    }
                    else
                    {
                        monitor.Log($"Native object for {field.Name} is null. Probably need to create the instance");
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

            // Re-add food and drink buffs since their affects wouldn't have been applied
            // when they were set directly.
            // TODO: set food and drink fields to 'don't set' in meta.
            //Buff foodBuff = Game1.buffsDisplay.food;
            //Buff drinkBuff = Game1.buffsDisplay.drink;
            //Game1.buffsDisplay.clearAllBuffs();
            //if (foodBuff != null)
            //    Game1.buffsDisplay.tryToAddFoodBuff(foodBuff, foodBuff.millisecondsDuration);
            //if (drinkBuff != null)
            //    Game1.buffsDisplay.tryToAddDrinkBuff(drinkBuff);

            //var buffs = game.BuffsDisplay.Buffs;
            //Game1.buffsDisplay.otherBuffs = new List<Buff>();
            //for (int i = 0; i < buffs.Count; ++i)
            //{
            //    var buff = new Buff(-1);
            //    DePopulateMessage(buffs[i], buff);

            //    if (!Game1.buffsDisplay.hasBuff(buff.which) &&
            //        (Game1.buffsDisplay.food == null || Game1.buffsDisplay.food?.which != buff.which) &&
            //        (Game1.buffsDisplay.drink == null || Game1.buffsDisplay.drink?.which != buff.which))
            //    {
            //        Game1.buffsDisplay.addOtherBuff(buff);
            //    }
            //}
        }

        //private IMessage PostSaveFixup(SaveData.Game1 game)
        //{
        //    var buffsDict = (Dictionary<ClickableTextureComponent, Buff>)TypeUtils.GetPrivateFieldData("buffs", Game1.buffsDisplay);
        //    var buffs = buffsDict.Values.ToList().Concat(Game1.buffsDisplay.otherBuffs).Distinct();
        //    foreach (var buff in buffs)
        //    {
        //        game.BuffsDisplay.Buffs.Add((SaveData.Buff)PopulateMessage(SaveData.Buff.Descriptor, buff));
        //    }
        //    return game;
        //}

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
