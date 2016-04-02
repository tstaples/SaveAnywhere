using System;
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

        private bool isGameMenuOpen = false;
        private bool pollForExitPage = false;
        private bool isExitPageOpen = false;
        private Rectangle saveButtonBounds;

        private IClickableMenu previousMenu = null;
        private bool wasMenuClosedInvoked = false;

        public override void Entry(params object[] objects)
        {
            MenuEvents.MenuChanged += OnMenuChanged;
            GameEvents.UpdateTick += OnUpdateTick;
        }

        // TODO: add gamepad support
        private void OnMouseChanged(object sender, EventArgsMouseStateChanged e)
        {
            if (e.NewState.LeftButton != ButtonState.Pressed)
                return;

                // Probably don't need to check if the menu is null since we wouldn't doing this check if it was
            if (!isExitPageOpen && Game1.activeClickableMenu != null)
            {
                GameMenu gameMenu = (GameMenu)Game1.activeClickableMenu;
                // The tab won't switch until next update so set a flag to check next update
                if (gameMenu.currentTab != GameMenu.exitTab && !pollForExitPage)
                {
                    pollForExitPage = true;
                }
            }
            else
            {
                Debug.Assert(isExitPageOpen, "exit page should be open if we've reached this point");
                if (saveButtonBounds.Contains(e.NewState.X, e.NewState.Y))
                {
                    Save();
                }
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
                ControlEvents.MouseChanged += OnMouseChanged;
            }
        }

        private void OnClickableMenuClosed(IClickableMenu priorMenu)
        {
            pollForExitPage = false;
            isGameMenuOpen = false;
            isExitPageOpen = false;
            UnsubscribeDrawEvent();
            ControlEvents.MouseChanged -= OnMouseChanged;
        }

        private void OnUpdateTick(object sender, EventArgs e)
        {
            if (!wasMenuClosedInvoked && previousMenu != null && Game1.activeClickableMenu == null)
            {
                wasMenuClosedInvoked = true;
                OnClickableMenuClosed(previousMenu);
            }

            if (pollForExitPage)
            {
                GameMenu gameMenu = (GameMenu)Game1.activeClickableMenu;
                if (gameMenu.currentTab == GameMenu.exitTab && !isExitPageOpen)
                {
                    isExitPageOpen = true;
                    pollForExitPage = false;
                    OnExitPageOpened(gameMenu);
                }
            }
        }

        private void OnExitPageOpened(GameMenu gameMenu)
        {
            Log.Debug("Exit tab clicked");

            var pages = Utils.GetNativeField<List<IClickableMenu>, GameMenu>(gameMenu, "pages");
            ExitPage exitPage = (ExitPage)pages[gameMenu.currentTab];

            int x = exitPage.xPositionOnScreen + Game1.tileSize * 3 + Game1.tileSize / 2;
            int y = exitPage.yPositionOnScreen + Game1.tileSize * 4 - Game1.tileSize / 2;
            int w = Game1.tileSize * 5;
            int h = Game1.tileSize * 3 / 2;
            saveButtonBounds = new Rectangle(x, y, w, h);
            var saveButton = new ClickableComponent(saveButtonBounds, "Save Game");

            SubscribeDrawEvent();
        }

        private void OnDraw(object sender, EventArgs e)
        {
            SpriteBatch spriteBatch = Game1.spriteBatch;

            float scale = (float)Game1.pixelZoom;
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
        }

        private void UnsubscribeDrawEvent()
        {
#if SMAPI_VERSION_39_3_AND_PRIOR
            GraphicsEvents.DrawTick -= OnDraw;
#else
            GraphicsEvents.OnPostRenderEvent -= OnDraw;
#endif
        }

        private void SubscribeDrawEvent()
        {
#if SMAPI_VERSION_39_3_AND_PRIOR
            GraphicsEvents.DrawTick += OnDraw;
#else
            GraphicsEvents.OnPostRenderEvent += OnDraw;
#endif
        }
    }
}
