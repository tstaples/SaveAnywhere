using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Linq;

namespace SaveAnywhere
{
    public class SaveAnywhere : Mod
    {
        private const int SaveCompleteFlag = 100;

        public enum GameMenuTab : int
        {
            Inventory = 0,
            Skills = 1,
            Social = 2,
            Map = 3,
            Crafting = 4,
            Collections = 5,
            Options = 6,
            Exit = 7
        }

        private GameMenuTab previousTab = GameMenuTab.Inventory;
        private bool isGameMenuOpen = false;
        private bool isExitPageOpen = false;
        private Rectangle saveButtonBounds;

        private IClickableMenu previousMenu = null;
        private bool wasMenuClosedInvoked = false;

        string savePath = Path.Combine(Constants.DataPath, "Mods", "SaveAnywhere");

        public override void Entry(params object[] objects)
        {
            MenuEvents.MenuChanged += OnMenuChanged;
            GameEvents.UpdateTick += OnUpdateTick;
            GameEvents.UpdateTick += PollForGameLoaded;
            ControlEvents.KeyReleased += ControlEvents_KeyReleased;
            TimeEvents.OnNewDay += OnNewDay;
        }

        // Debug
        private void ControlEvents_KeyReleased(object sender, EventArgsKeyPressed e)
        {
            if (e.KeyPressed == Keys.K)
            {
                Log.Debug("loading");
                Load();
            }
            else if (e.KeyPressed == Keys.J)
            {
                Log.Debug("saving");
                Save();
            }
            else if (e.KeyPressed == Keys.N)
            {
                GraphicsEvents.OnPostRenderEvent += OnDraw;
            }
        }

        private void OnNewDay(object sender, EventArgsNewDay e)
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

        private void OnLoadedGame()
        {
            Log.Debug("onLoadedGame: loading custom save data");
            string saveFile = Path.Combine(savePath, "currentsave");
            if (File.Exists(saveFile))
            {
                Load();
            }
        }

        private void PollForGameLoaded(object sender, EventArgs e)
        {
            if (Game1.hasLoadedGame && Game1.gameMode == 3)
            {
                Log.Debug("Game loaded... running custom loader");
                GameEvents.UpdateTick -= PollForGameLoaded;
                OnLoadedGame();
            }
        }

        // TODO: add gamepad support
        private void OnMouseChanged(object sender, EventArgsMouseStateChanged e)
        {
            if (e.NewState.LeftButton != ButtonState.Pressed)
                return;

            Debug.Assert(isExitPageOpen, "exit page should be open if we've reached this point");
            if (saveButtonBounds.Contains(e.NewState.X, e.NewState.Y))
            {
                Save();
            }
        }

        private void OnMenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            // Reset flag
            wasMenuClosedInvoked = false;
            previousMenu = e.PriorMenu;

            if (Utils.IsType<GameMenu>(e.NewMenu) && !isGameMenuOpen)
            {
                isGameMenuOpen = true;
            }
        }

        private void OnClickableMenuClosed(IClickableMenu priorMenu)
        {
            isGameMenuOpen = false;
            isExitPageOpen = false;

            UnsubscribeEvents();
        }

        private void OnUpdateTick(object sender, EventArgs e)
        {
            // Check for menu closed events
            if (!wasMenuClosedInvoked && previousMenu != null && Game1.activeClickableMenu == null)
            {
                wasMenuClosedInvoked = true;
                OnClickableMenuClosed(previousMenu);
            }

            if (isGameMenuOpen)
            {
                GameMenu gameMenu = (GameMenu)Game1.activeClickableMenu;
                GameMenuTab currentTab = (GameMenuTab)gameMenu.currentTab;
                if (previousTab != currentTab)
                {
                    OnGameMenuTabChanged(previousTab, currentTab);
                    previousTab = currentTab;
                }
            }
        }

        private void OnGameMenuTabChanged(GameMenuTab prevTab, GameMenuTab newTab)
        {
            if (newTab == GameMenuTab.Exit && !isExitPageOpen)
            {
                isExitPageOpen = true;
                OnExitPageOpened();
            }
            else
            {
                isExitPageOpen = false;
                OnExitPageClosed();
            }
        }

        private void OnExitPageOpened()
        {
            Log.Debug("Exit tab clicked");

            GameMenu gameMenu = (GameMenu)Game1.activeClickableMenu;
            var pages = Utils.GetNativeField<List<IClickableMenu>, GameMenu>(gameMenu, "pages");
            ExitPage exitPage = (ExitPage)pages[gameMenu.currentTab];

            int x = exitPage.xPositionOnScreen + Game1.tileSize * 3 + Game1.tileSize / 2;
            int y = exitPage.yPositionOnScreen + Game1.tileSize * 4 - Game1.tileSize / 2;
            int w = Game1.tileSize * 5;
            int h = Game1.tileSize * 3 / 2;
            saveButtonBounds = new Rectangle(x, y, w, h);
            var saveButton = new ClickableComponent(saveButtonBounds, "Save Game");

            SubscribeEvents();
        }

        private void OnExitPageClosed()
        {
            UnsubscribeEvents();
        }

        private void OnDraw(object sender, EventArgs e)
        {
            SpriteBatch spriteBatch = Game1.spriteBatch;
            // TODO: fix button being on top of cursor
            // TODO: add < 39.3 draw compat
            float scale = Game1.pixelZoom;
            Rectangle tileSheetSourceRect = new Rectangle(432, 439, 9, 9);
            IClickableMenu.drawTextureBox(spriteBatch, Game1.mouseCursors, tileSheetSourceRect, saveButtonBounds.X, saveButtonBounds.Y, saveButtonBounds.Width, saveButtonBounds.Height, Color.White, scale, true);

            SVector2 tpos = new SVector2(saveButtonBounds.Center.X, saveButtonBounds.Center.Y + Game1.pixelZoom) - SVector2.MeasureString("Save Game", Game1.dialogueFont) / 2f;
            Utility.drawTextWithShadow(spriteBatch, "Save Game", Game1.dialogueFont, tpos.ToXNAVector2(), Game1.textColor, 1f, -1f, -1, -1, 0f, 3);
        }

        // TODO: create/store backups of players saves first
        // and maybe store up to 5 backups or something
        private void Save()
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
            catch(Exception ex)
            {
                Log.Error("Error saving data: " + ex);
            }
        }

        private void Load()
        {
            try
            {
                string saveFile = Path.Combine(savePath, "currentsave");

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

        private IMessage PopulateMessage(MessageDescriptor descriptor, object instance)
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
        private void DePopulateMessage(IMessage message, object instance)
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

        private T CheckProtoFieldNotNull<T>(T value, FieldDescriptor descriptor)
        {
            if (value == null)
            {
                throw new ArgumentNullException(descriptor.Name, "Proto message for type: " + descriptor.FieldType + " is probably not implemented");
            }
            return value;
        }

        private Type ResolveTypeFromAssembly(Assembly assembly, string objectName)
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

        private IEnumerable<string> GetSDVAssemblyNamespaces(Assembly assembly)
        {
            return assembly.GetTypes()
                .Select(t => t.Namespace)
                .Where(n => n != null)
                .Distinct();
        }

        private FieldInfo GetField(string fieldName, object instance)
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

        private object GetFieldData(string fieldName, object instance)
        {
            return GetField(fieldName, instance)?.GetValue(instance);
        }

        private void SetFieldData(string fieldName, object instance, object value)
        {
            GetField(fieldName, instance)?.SetValue(instance, value);
        }



        private void UnsubscribeEvents()
        {
            ControlEvents.MouseChanged -= OnMouseChanged;

#if SMAPI_VERSION_39_3_AND_PRIOR
            GraphicsEvents.DrawTick -= OnDraw;
#else
            GraphicsEvents.OnPostRenderEvent -= OnDraw;
#endif
        }

        private void SubscribeEvents()
        {
            ControlEvents.MouseChanged += OnMouseChanged;

#if SMAPI_VERSION_39_3_AND_PRIOR
            GraphicsEvents.DrawTick += OnDraw;
#else
            GraphicsEvents.OnPostRenderEvent += OnDraw;
#endif
        }

        // Debug
        private static void DrawPlayerPosition()
        {
            SpriteBatch spriteBatch = Game1.spriteBatch;
            string text = Game1.player.Position.ToString();
            Vector2 tsize = Game1.smallFont.MeasureString(text);
            Vector2 pos = new Vector2(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.X, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Y);
            spriteBatch.DrawString(Game1.smallFont, text, pos, Color.Green);
        }
    }
}
