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

                MemoryStream stream = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();

                SaveData saveData = new SaveData();
                saveData.gameData.timeOfDay = Game1.timeOfDay;
                saveData.playerData.currentLocation = Game1.currentLocation.Name;
                saveData.playerData.position = Game1.player.position;
                saveData.playerData.facingDirection = Game1.player.facingDirection;
                saveData.playerData.stamina = Game1.player.stamina;
                saveData.playerData.health = Game1.player.health;
                saveData.playerData.swimming = Game1.player.swimming;
                formatter.Serialize(stream, saveData);

                FileStream fileStream = File.Create(saveFile);
                fileStream.Write(stream.ToArray(), 0, (int)stream.Length);
                fileStream.Close();
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
                FileStream filestream = new FileStream(saveFile, FileMode.Open);
                BinaryFormatter formatter = new BinaryFormatter();

                SaveData saveData = (SaveData)formatter.Deserialize(filestream);
                filestream.Close();

                Game1.timeOfDay = saveData.gameData.timeOfDay;
                Vector2 pos = saveData.playerData.position;
                Game1.warpFarmer(saveData.playerData.currentLocation, (int)(pos.X / Game1.tileSize), (int)(pos.Y / Game1.tileSize), false);
                Game1.player.faceDirection(saveData.playerData.facingDirection);
                Game1.player.stamina = saveData.playerData.stamina;
                Game1.player.health = saveData.playerData.health;
                Game1.player.swimming = saveData.playerData.swimming;
            }
            catch (Exception e)
            {
                Log.Error("Failed to load data: " + e);
            }
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
