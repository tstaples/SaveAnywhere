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
                SaveManager.Load();
            }
            else if (e.KeyPressed == Keys.J)
            {
                Log.Debug("saving");
                SaveManager.Save();
            }
            else if (e.KeyPressed == Keys.N)
            {
                GraphicsEvents.OnPostRenderEvent += OnDraw;
            }
        }

        private void OnNewDay(object sender, EventArgsNewDay e)
        {
            SaveManager.ClearSave();
        }

        private void OnLoadedGame()
        {
            SaveManager.Load();
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
                SaveManager.Save();
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
