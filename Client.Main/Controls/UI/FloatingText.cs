// GameScene.cs
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Client.Main.Controls.UI
{
    /// <summary>
    /// Represents a single on-screen floating text notification.
    /// </summary>
    public class FloatingText : UIControl
    {
        // ──────────────────────────── Fields ────────────────────────────
        public string Text { get; }
        public Color TextColor { get; }

        private readonly SpriteFont _font;
        private readonly Vector2 _rawSize;
        private Vector2 _center;
        private float _alpha = 1f;

        /// <summary>
        /// Timestamp when this instance was created (seconds since game start).
        /// </summary>
        public float CreationTime { get; }

        // ────────────────────────── Tuning Constants ──────────────────────────
        private const float FONT_SIZE = 13f;
        private const float ORIGINAL_FPS = 25f;
        private const float FLASH_STEPS = 10f;
        private const float FLASH_ON_STEPS = 5f;
        private const int BACKGROUND_PADDING_X = 14;
        private const int BACKGROUND_PADDING_Y = 4;
        private const int FADE_EDGE_WIDTH = 22;

        private static readonly Color GoldenNoticeColor = new(255, 214, 112);
        private static readonly Color GuildNoticeColor = new(118, 248, 206);
        private static readonly Color NoticeBgCenter = new(10, 12, 18, 178);
        private static readonly Color NoticeGoldRim = new(214, 176, 104, 88);
        private static readonly Color NoticeCyanRim = new(100, 224, 188, 88);

        private readonly float _fontScale;

        // ─────────────────────────── Constructors ───────────────────────────
        public FloatingText(string text, Color color, Vector2 spawnCenter, float creationTime)
        {
            Text = text ?? string.Empty;
            TextColor = color;
            _font = GraphicsManager.GetUiFont(FONT_SIZE, out _fontScale)
                           ?? GraphicsManager.Instance.Font
                           ?? throw new InvalidOperationException("SpriteFont is missing.");
            CreationTime = creationTime;

            _rawSize = _font.MeasureString(Text);
            ControlSize = (_rawSize * _fontScale).ToPoint();
            ViewSize = ControlSize;

            _center = spawnCenter;
            UpdatePosition();

            Interactive = false;
            Visible = true;
        }

        // ────────────────────────── Public API ───────────────────────────
        /// <summary>
        /// Updates the vertical center for layout in NotificationManager.
        /// </summary>
        internal void SetCenterY(float newCenterY)
        {
            _center.Y = newCenterY;
            UpdatePosition();
        }

        /// <summary>
        /// Moves the text up or down by the given delta.
        /// </summary>
        public void MoveUp(float deltaY)
        {
            _center.Y += deltaY;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            X = (int)(_center.X - ControlSize.X * 0.5f);
            Y = (int)(_center.Y - ControlSize.Y * 0.5f);
        }

        public float ScaledHeight => _rawSize.Y * _fontScale;

        // ─────────────────────────── Overrides ───────────────────────────
        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;

            // Subtle smooth pulse for golden system notices (instead of harsh binary 50% blinking).
            if (IsGoldenNotice)
            {
                float t = (float)gameTime.TotalGameTime.TotalSeconds;
                _alpha = 0.84f + 0.16f * MathF.Sin(t * 5.2f);
            }
            else
            {
                _alpha = 1f;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || _alpha <= 0.01f || string.IsNullOrEmpty(Text)) return;

            var spriteBatch = GraphicsManager.Instance.Sprite;
            var pixel = GraphicsManager.Instance.Pixel;

            Vector2 scaledSize = _rawSize * _fontScale;
            Vector2 boxPos = _center - scaledSize * 0.5f;
            float opticalY = Localization.Loc.IsChinese ? -scaledSize.Y * 0.08f : 0f;
            Vector2 drawPos = UiScaler.SnapToPixel(new Vector2(boxPos.X, boxPos.Y + opticalY));

            if (pixel != null)
            {
                int coreX = (int)MathF.Floor(boxPos.X) - BACKGROUND_PADDING_X;
                int coreY = (int)MathF.Floor(boxPos.Y) - BACKGROUND_PADDING_Y;
                int coreW = (int)MathF.Ceiling(scaledSize.X) + BACKGROUND_PADDING_X * 2;
                int coreH = (int)MathF.Ceiling(scaledSize.Y) + BACKGROUND_PADDING_Y * 2;

                // Sleek tapered dark obsidian ribbon with soft fading edges
                var coreRect = new Rectangle(coreX, coreY, coreW, coreH);
                spriteBatch.Draw(pixel, coreRect, NoticeBgCenter);
                UiDrawHelper.DrawHorizontalGradient(spriteBatch,
                    new Rectangle(coreX - FADE_EDGE_WIDTH, coreY, FADE_EDGE_WIDTH, coreH),
                    Color.Transparent, NoticeBgCenter, 8);
                UiDrawHelper.DrawHorizontalGradient(spriteBatch,
                    new Rectangle(coreX + coreW, coreY, FADE_EDGE_WIDTH, coreH),
                    NoticeBgCenter, Color.Transparent, 8);

                // Subtle 1px top/bottom metallic accent lines
                Color rim = IsGoldenNotice ? NoticeGoldRim : NoticeCyanRim;
                spriteBatch.Draw(pixel, new Rectangle(coreX, coreY, coreW, 1), rim);
                spriteBatch.Draw(pixel, new Rectangle(coreX, coreY + coreH - 1, coreW, 1), rim);
                UiDrawHelper.DrawHorizontalGradient(spriteBatch,
                    new Rectangle(coreX - FADE_EDGE_WIDTH, coreY, FADE_EDGE_WIDTH, 1), Color.Transparent, rim, 6);
                UiDrawHelper.DrawHorizontalGradient(spriteBatch,
                    new Rectangle(coreX + coreW, coreY, FADE_EDGE_WIDTH, 1), rim, Color.Transparent, 6);
                UiDrawHelper.DrawHorizontalGradient(spriteBatch,
                    new Rectangle(coreX - FADE_EDGE_WIDTH, coreY + coreH - 1, FADE_EDGE_WIDTH, 1), Color.Transparent, rim, 6);
                UiDrawHelper.DrawHorizontalGradient(spriteBatch,
                    new Rectangle(coreX + coreW, coreY + coreH - 1, FADE_EDGE_WIDTH, 1), rim, Color.Transparent, 6);
            }

            float px = UiScaler.InverseScale;
            Color shadowColor = Color.Black * (0.90f * _alpha);
            Color textColor = (IsGoldenNotice ? GoldenNoticeColor : GuildNoticeColor) * _alpha;

            // Crisp 1px shadow for sharp readability on bright maps (e.g. Devias snow)
            spriteBatch.DrawString(_font, Text, drawPos + new Vector2(px, px), shadowColor, 0f, Vector2.Zero, _fontScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, Text, drawPos, textColor, 0f, Vector2.Zero, _fontScale, SpriteEffects.None, 0f);
        }

        private bool IsGoldenNotice =>
            TextColor.R == Color.Goldenrod.R &&
            TextColor.G == Color.Goldenrod.G &&
            TextColor.B == Color.Goldenrod.B;
    }
}
