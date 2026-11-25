#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Map;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.MuHelper
{
    /// <summary>
    /// Compact MU Helper (游戏助手) icon dock placed immediately to the right of CurrentLocationControl.
    /// Uses the official MacroUI wrench (Setup) and orange play/stop sprites, and reserves its width so
    /// ActiveBuffsPanel shifts cleanly to the right without overlapping.
    /// </summary>
    public sealed class MuHelperBarControl : UIControl
    {
        private const string SetupTexturePath = "Interface/MacroUI/MacroUI_Setup.tga";
        private const string StartTexturePath = "Interface/MacroUI/MacroUI_Start.tga";
        private const string StopTexturePath = "Interface/MacroUI/MacroUI_Stop.tga";

        private readonly MuHelperController _controller;
        private readonly CurrentLocationControl _locationBar;
        private readonly Action _openSettings;

        private Texture2D? _setupTex;
        private Texture2D? _startTex;
        private Texture2D? _stopTex;
        private SpriteFont? _font;

        private Rectangle _settingsBtnRect;
        private Rectangle _toggleBtnRect;
        private bool _hoverSettings;
        private bool _hoverToggle;
        private bool _pressSettings;
        private bool _pressToggle;

        private const int GapFromLocation = -1; // Seamlessly connect to CurrentLocationControl right edge

        public MuHelperBarControl(
            MuHelperController controller,
            CurrentLocationControl locationBar,
            Action openSettings)
        {
            _controller = controller;
            _locationBar = locationBar;
            _openSettings = openSettings;

            AutoViewSize = false;
            Interactive = true;
            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;
            BorderThickness = 0;

            SyncLayout();
        }

        public override async Task Load()
        {
            await base.Load();
            _setupTex = await TextureLoader.Instance.PrepareAndGetTexture(SetupTexturePath);
            _startTex = await TextureLoader.Instance.PrepareAndGetTexture(StartTexturePath);
            _stopTex = await TextureLoader.Instance.PrepareAndGetTexture(StopTexturePath);
        }

        public override void Update(GameTime gameTime)
        {
            SyncLayout();
            base.Update(gameTime);

            if (Status != GameControlStatus.Ready || !Visible)
            {
                _hoverSettings = _hoverToggle = false;
                _pressSettings = _pressToggle = false;
                return;
            }

            var mouse = MuGame.Instance.UiMouseState;
            var prevMouse = MuGame.Instance.PrevUiMouseState;
            Point pos = mouse.Position;

            bool isTopmostHover = IsMouseOver && DisplayRectangle.Contains(pos);
            _hoverSettings = isTopmostHover && _settingsBtnRect.Contains(pos);
            _hoverToggle = isTopmostHover && _toggleBtnRect.Contains(pos);
            bool down = mouse.LeftButton == ButtonState.Pressed;
            _pressSettings = _hoverSettings && down;
            _pressToggle = _hoverToggle && down;

            bool freshClick = isTopmostHover
                && down
                && prevMouse.LeftButton == ButtonState.Released;

            if (freshClick)
            {
                if (_hoverSettings)
                {
                    SoundController.Instance?.PlayBuffer("Sound/iButtonClick.wav");
                    _openSettings();
                    Scene?.SetMouseInputConsumed();
                }
                else if (_hoverToggle)
                {
                    SoundController.Instance?.PlayBuffer("Sound/iButtonClick.wav");
                    _controller.Toggle();
                    Scene?.SetMouseInputConsumed();
                }
            }
        }

        private void SyncLayout()
        {
            Rectangle loc = _locationBar.DisplayRectangle;
            int barHeight = loc.Height > 0 ? loc.Height : 28;
            float scale = barHeight / 28f;

            int pad = Math.Max(3, (int)MathF.Round(4f * scale));
            int gap = Math.Max(3, (int)MathF.Round(4f * scale));
            int btnH = Math.Max(16, barHeight - pad * 2);
            // Match native 18:13 MacroUI button aspect ratio (~1.38)
            int btnW = Math.Max(20, (int)MathF.Round(btnH * (18f / 13f)));
            int width = pad * 2 + btnW * 2 + gap;

            X = (loc.Width > 0 ? loc.Right : 292) + GapFromLocation;
            Y = loc.Y > 0 ? loc.Y : 10;
            ControlSize = new Point(width, barHeight);
            ViewSize = ControlSize;

            _locationBar.ExtraRightOffset = width + GapFromLocation;

            Rectangle rect = DisplayRectangle;
            _settingsBtnRect = new Rectangle(rect.X + pad, rect.Y + pad, btnW, btnH);
            _toggleBtnRect = new Rectangle(_settingsBtnRect.Right + gap, rect.Y + pad, btnW, btnH);
        }

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible)
                return;

            var spriteBatch = GraphicsManager.Instance.Sprite;
            var pixel = GraphicsManager.Instance.Pixel;
            if (spriteBatch == null || pixel == null)
                return;

            SpriteBatchScope? scope = null;
            if (!SpriteBatchScope.BatchIsBegun)
            {
                scope = new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    transform: UiScaler.SpriteTransform);
            }

            try
            {
                Rectangle rect = DisplayRectangle;

                // Seamless continuation of CurrentLocationControl bar (no gap, continuous top/bottom/right border)
                spriteBatch.Draw(pixel, rect, ModernHudTheme.BorderOuter);
                var inner = new Rectangle(rect.X, rect.Y + 1, Math.Max(1, rect.Width - 1), Math.Max(1, rect.Height - 2));
                UiDrawHelper.DrawVerticalGradient(spriteBatch, inner, ModernHudTheme.BgDark, ModernHudTheme.BgDarkest);
                // Continuous top gold accent line
                spriteBatch.Draw(pixel, new Rectangle(inner.X, inner.Y, Math.Max(1, inner.Width - 1), 1), ModernHudTheme.Accent * 0.6f * Alpha);
                // Subtle inner vertical divider separating coordinates from helper icons
                spriteBatch.Draw(pixel, new Rectangle(rect.X, inner.Y + 3, 1, Math.Max(1, inner.Height - 6)), ModernHudTheme.BorderInner * 0.45f * Alpha);

                // Left button: Settings (Wrench)
                DrawSpriteButton(spriteBatch, pixel, _setupTex, _settingsBtnRect, _hoverSettings, _pressSettings, new Color(75, 75, 78));

                // Right button: Start (Orange Play) / Stop
                bool running = _controller.IsRunning;
                Texture2D? playTex = running ? _stopTex : _startTex;
                Color fallbackColor = running ? new Color(40, 140, 75) : new Color(185, 96, 18);
                DrawSpriteButton(spriteBatch, pixel, playTex, _toggleBtnRect, _hoverToggle, _pressToggle, fallbackColor);

                // Subtle green active rim when running
                if (running)
                {
                    UiDrawHelper.DrawBorder(spriteBatch, _toggleBtnRect, new Color(90, 240, 140) * 0.9f);
                }

                // Compact hover tooltip directly below the hovered icon
                if (_hoverSettings || _hoverToggle)
                {
                    string tip = _hoverSettings
                        ? "游戏助手设置 (Z)"
                        : (running ? "停止挂机 (Home)" : "开始挂机 (Home)");
                    DrawTooltip(spriteBatch, pixel, tip, _hoverSettings ? _settingsBtnRect : _toggleBtnRect);
                }
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private static void DrawSpriteButton(
            SpriteBatch sb, Texture2D pixel, Texture2D? tex,
            Rectangle dest, bool hover, bool pressed, Color fallbackColor)
        {
            if (tex != null && tex.Width >= 18 && tex.Height >= 39)
            {
                // MacroUI_* OZT has 3 stacked 18x13 states; inset 1px to trim stray border artifacts
                int frame = pressed ? 2 : (hover ? 1 : 0);
                var src = new Rectangle(1, frame * 13 + 1, 16, 11);
                sb.Draw(tex, dest, src, Color.White);
            }
            else
            {
                sb.Draw(pixel, dest, hover ? Color.Lerp(fallbackColor, Color.White, 0.18f) : fallbackColor);
                UiDrawHelper.DrawBorder(sb, dest, ModernHudTheme.BorderInner);
            }
        }

        private void DrawTooltip(SpriteBatch sb, Texture2D pixel, string text, Rectangle anchor)
        {
            _font ??= GraphicsManager.Instance.Font;
            if (_font == null) return;

            const float scale = 0.44f;
            Vector2 size = _font.MeasureString(text) * scale;
            int padX = 7;
            int padY = 4;
            int w = (int)MathF.Ceiling(size.X) + padX * 2;
            int h = (int)MathF.Ceiling(size.Y) + padY * 2;
            int x = Math.Max(4, anchor.X);
            int y = DisplayRectangle.Bottom + 4;

            var bg = new Rectangle(x, y, w, h);
            sb.Draw(pixel, bg, new Color(10, 12, 18, 225));
            UiDrawHelper.DrawBorder(sb, bg, ModernHudTheme.Accent * 0.75f);

            var pos = new Vector2(x + padX, y + padY);
            sb.DrawString(_font, text, pos + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sb.DrawString(_font, text, pos, new Color(245, 232, 198), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
