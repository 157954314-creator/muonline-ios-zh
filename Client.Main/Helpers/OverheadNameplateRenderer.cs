using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Client.Main.Helpers
{
    public static class OverheadNameplateRenderer
    {
        private readonly struct QueuedNameplate
        {
            public QueuedNameplate(SpriteFont font, Vector3 screen, string name, float? healthFraction, float renderScale,
                bool hoverLabel = false, bool monsterCompactBar = false, bool monsterTopBanner = false)
            {
                Font = font;
                Screen = screen;
                Name = name;
                HealthFraction = healthFraction;
                RenderScale = renderScale;
                HoverLabel = hoverLabel;
                MonsterCompactBar = monsterCompactBar;
                MonsterTopBanner = monsterTopBanner;
            }

            public SpriteFont Font { get; }
            public Vector3 Screen { get; }
            public string Name { get; }
            public float? HealthFraction { get; }
            public float RenderScale { get; }
            public bool HoverLabel { get; }
            public bool MonsterCompactBar { get; }
            public bool MonsterTopBanner { get; }
        }

        /// <summary>S6-style F8 toggle: shows compact 8-segment HP bars over monsters without cluttered name boxes.</summary>
        public static bool ShowMonsterOverheadBars { get; set; } = true;

        // 13 px: an ArialUi size, so the glyphs are drawn 1:1 (scaled glyphs go soft and uneven).
        private const float BaseNameScale = 13f / Constants.BASE_FONT_SIZE;
        private const float BaseHoverNameScale = 13f / Constants.BASE_FONT_SIZE;
        private const float BaseBarWidth = 70f;
        private const float BaseBarHeight = 6f;
        private const float BaseBarOffsetY = 2f;
        private const float BaseBarPadding = 1f;
        private const float BaseNamePaddingX = 4f;
        private const float BaseNamePaddingY = 2f;
        private const float BaseNameBarGap = 2f;
        private const int BarSegments = 8;
        private const int MaxRetainedQueueCapacity = 512;
        private static readonly List<QueuedNameplate> s_queue = new(256);
        public static int LastFrameQueuedNameplates { get; private set; }

        private static class Theme
        {
            public static readonly Color BgDarkest = new(8, 10, 14, 252);
            public static readonly Color BgDark = new(16, 20, 26, 250);
            public static readonly Color BgMid = new(24, 30, 38, 248);
            public static readonly Color BgLight = new(35, 42, 52, 245);
            public static readonly Color BgLighter = new(48, 56, 68, 240);

            public static readonly Color Accent = new(212, 175, 85);
            public static readonly Color AccentBright = new(255, 215, 120);
            public static readonly Color AccentDim = new(140, 115, 55);
            public static readonly Color AccentGlow = new(255, 200, 80, 40);

            public static readonly Color Secondary = new(90, 140, 200);
            public static readonly Color SecondaryBright = new(130, 180, 240);
            public static readonly Color SecondaryDim = new(50, 80, 120);

            public static readonly Color BorderOuter = new(5, 6, 8, 255);
            public static readonly Color BorderInner = new(60, 70, 85, 200);
            public static readonly Color BorderHighlight = new(100, 110, 130, 120);

            public static readonly Color SlotBg = new(12, 15, 20, 240);
            public static readonly Color SlotBorder = new(45, 52, 65, 180);
            public static readonly Color SlotHover = new(70, 85, 110, 150);
            public static readonly Color SlotSelected = new(212, 175, 85, 100);

            public static readonly Color TextWhite = new(240, 240, 245);
            public static readonly Color TextGold = new(255, 220, 130);
            public static readonly Color TextGray = new(160, 165, 175);
            public static readonly Color TextDark = new(100, 105, 115);

            public static readonly Color Success = new(80, 200, 120);
            public static readonly Color Warning = new(240, 180, 60);
            public static readonly Color Danger = new(220, 80, 80);
        }

        public static bool TryProject(BoundingBox bounds, float zOffset, out Vector3 screen)
        {
            Vector3 anchor = new(
                (bounds.Min.X + bounds.Max.X) * 0.5f,
                (bounds.Min.Y + bounds.Max.Y) * 0.5f,
                bounds.Max.Z + zOffset);

            screen = MuGame.Instance.GraphicsDevice.Viewport.Project(
                anchor,
                Camera.Instance.Projection,
                Camera.Instance.View,
                Matrix.Identity);

            return screen.Z >= 0f && screen.Z <= 1f;
        }

        public static void BeginFrame()
        {
            s_queue.Clear();
        }

        public static void EnqueueNameplate(SpriteFont font, Vector3 screen, string name, float? healthFraction, float renderScale)
        {
            if (font == null || string.IsNullOrWhiteSpace(name))
                return;

            s_queue.Add(new QueuedNameplate(font, screen, name, healthFraction, renderScale));
        }

        /// <summary>
        /// Queues the hover name of an NPC/player. Like the nameplates it is drawn with the interface, at the window's
        /// resolution, so render scale neither blurs nor resizes the text.
        /// </summary>
        public static void EnqueueHoverLabel(Vector3 screen, string name, float renderScale)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            s_queue.Add(new QueuedNameplate(null, screen, name, null, renderScale, hoverLabel: true));
        }

        /// <summary>S6-style compact 8-segment red HP bar drawn over a monster without a cluttered name box.</summary>
        public static void EnqueueMonsterOverheadBar(Vector3 screen, float healthFraction, float renderScale)
        {
            s_queue.Add(new QueuedNameplate(null, screen, string.Empty, Math.Clamp(healthFraction, 0f, 1f), renderScale,
                monsterCompactBar: true));
        }

        /// <summary>S6-style top-center crimson monster name banner + 20-segment red HP bar on hover.</summary>
        public static void EnqueueMonsterTopBanner(string name, float? healthFraction, float renderScale)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            s_queue.Add(new QueuedNameplate(null, Vector3.Zero, name, healthFraction, renderScale,
                monsterTopBanner: true));
        }

        /// <param name="targetScale">Size of the target drawn into relative to the scene target the positions were
        /// projected in (1 / render scale when drawn on the window).</param>
        public static void FlushQueuedNameplates(SpriteBatch spriteBatch, float targetScale = 1f)
        {
            if (spriteBatch == null || s_queue.Count == 0)
                return;

            SpriteBatchScope scope = default;
            bool hasScope = false;
            if (!SpriteBatchScope.BatchIsBegun)
            {
                // Use LinearClamp so scaled CJK glyphs (NotoSC 22px/14px) stay smooth instead of jagged point-sampling.
                scope = new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    DepthStencilState.None);
                hasScope = true;
            }

            try
            {
                for (int i = 0; i < s_queue.Count; i++)
                {
                    var entry = s_queue[i];
                    var screen = entry.Screen;
                    screen.X *= targetScale;
                    screen.Y *= targetScale;
                    if (entry.HoverLabel)
                    {
                        DrawHoverLabel(spriteBatch, screen, entry.Name, entry.RenderScale * targetScale);
                        continue;
                    }
                    if (entry.MonsterCompactBar)
                    {
                        DrawS6MonsterOverheadBar(spriteBatch, screen, entry.HealthFraction ?? 1f, entry.RenderScale * targetScale);
                        continue;
                    }
                    if (entry.MonsterTopBanner)
                    {
                        DrawS6MonsterTopBanner(spriteBatch, entry.Name, entry.HealthFraction, entry.RenderScale * targetScale);
                        continue;
                    }

                    DrawNameplate(
                        spriteBatch,
                        entry.Font,
                        screen,
                        entry.Name,
                        entry.HealthFraction,
                        entry.RenderScale * targetScale);
                }
            }
            finally
            {
                if (hasScope)
                    scope.Dispose();
                LastFrameQueuedNameplates = s_queue.Count;
                s_queue.Clear();
                if (s_queue.Capacity > MaxRetainedQueueCapacity)
                    s_queue.Capacity = MaxRetainedQueueCapacity;
            }
        }

        public static void DrawNameplate(SpriteBatch spriteBatch, SpriteFont font, Vector3 screen, string name, float? healthFraction, float renderScale)
        {
            if (spriteBatch == null || font == null || string.IsNullOrEmpty(name))
                return;

            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null)
                return;

            float scale = MathF.Max(renderScale, 0.1f);
            // Small text: a font rasterised near its size on screen instead of the base font shrunk to ~40%.
            font = GetCrispFont(Constants.BASE_FONT_SIZE * BaseNameScale * scale, out float nameScale);
            Vector2 nameSize = font.MeasureString(name) * nameScale;

            float padX = MathF.Max(1f, BaseNamePaddingX * scale);
            float padY = MathF.Max(1f, BaseNamePaddingY * scale);
            bool hasBar = healthFraction.HasValue;
            float barWidth = MathF.Max(BaseBarWidth * scale, nameSize.X + padX * 2f);
            float barHeight = hasBar ? MathF.Max(1f, BaseBarHeight * scale) : 0f;
            float barOffset = MathF.Max(1f, BaseBarOffsetY * scale);
            float barPadding = MathF.Max(1f, BaseBarPadding * scale);
            float nameGap = MathF.Max(1f, BaseNameBarGap * scale);

            float barBottomY = screen.Y - barOffset;
            float barTopY = barBottomY - barHeight;

            float nameBgHeight = nameSize.Y + padY * 2f;
            float nameBgWidth = nameSize.X + padX * 2f;

            int barX = (int)MathF.Round(screen.X - barWidth * 0.5f);
            int barY = (int)MathF.Round(barTopY);
            int barW = Math.Max(1, (int)MathF.Round(barWidth));
            int barH = Math.Max(1, (int)MathF.Round(barHeight));

            int nameX = (int)MathF.Round(screen.X - nameBgWidth * 0.5f);
            float nameBaseY = hasBar ? (barTopY - nameGap) : (screen.Y - barOffset);
            int nameY = (int)MathF.Round(nameBaseY - nameBgHeight);
            int nameW = Math.Max(1, (int)MathF.Round(nameBgWidth));
            int nameH = Math.Max(1, (int)MathF.Round(nameBgHeight));

            Rectangle nameRect = new(nameX, nameY, nameW, nameH);
            Rectangle barRect = new(barX, barY, barW, barH);

            var viewport = spriteBatch.GraphicsDevice.Viewport;
            int minX = Math.Min(nameRect.Left, barRect.Left);
            int maxX = Math.Max(nameRect.Right, barRect.Right);
            int minY = Math.Min(nameRect.Top, barRect.Top);
            int maxY = Math.Max(nameRect.Bottom, barRect.Bottom);
            if (maxX < 0 || minX > viewport.Width || maxY < 0 || minY > viewport.Height)
                return;

            int pad = Math.Max(0, (int)MathF.Round(barPadding));
            int innerWidth = Math.Max(0, barRect.Width - pad * 2);
            int innerHeight = Math.Max(1, barRect.Height - pad * 2);
            float hpFill = Math.Clamp(healthFraction ?? 0f, 0f, 1f);
            int fillWidth = Math.Max(0, (int)MathF.Round(innerWidth * hpFill));

            Rectangle fillRect = new(
                barRect.X + pad,
                barRect.Y + pad,
                fillWidth,
                innerHeight);

            float depth = MathHelper.Clamp(screen.Z, 0f, 1f);
            float opticalY = Localization.Loc.IsChinese ? -nameSize.Y * 0.09f : 0f;
            Vector2 textPos = new(MathF.Round(nameRect.X + padX), MathF.Round(nameRect.Y + padY + opticalY));

            spriteBatch.Draw(pixel, nameRect, null, Theme.BgDarkest * 0.85f, 0f, Vector2.Zero, SpriteEffects.None, depth);
            DrawBorder(spriteBatch, pixel, nameRect, Theme.BorderOuter * 0.9f, depth);

            spriteBatch.DrawString(font, name, textPos + new Vector2(1f, 1f), Color.Black * 0.7f, 0f, Vector2.Zero, nameScale, SpriteEffects.None, depth);
            spriteBatch.DrawString(font, name, textPos, Theme.TextWhite, 0f, Vector2.Zero, nameScale, SpriteEffects.None, depth);

            if (hasBar)
            {
                spriteBatch.Draw(pixel, barRect, null, Color.Black * 0.7f, 0f, Vector2.Zero, SpriteEffects.None, depth);
                if (fillRect.Width > 0)
                    spriteBatch.Draw(pixel, fillRect, null, Theme.Danger, 0f, Vector2.Zero, SpriteEffects.None, depth);

                if (innerWidth > 0 && BarSegments > 1)
                {
                    float segmentWidth = innerWidth / (float)BarSegments;
                    for (int i = 1; i < BarSegments; i++)
                    {
                        int x = barRect.X + pad + (int)MathF.Round(segmentWidth * i);
                        spriteBatch.Draw(pixel,
                            new Rectangle(x, barRect.Y + pad, 1, innerHeight),
                            null,
                            Color.Black * 0.35f,
                            0f,
                            Vector2.Zero,
                            SpriteEffects.None,
                            depth);
                    }
                }

                DrawBorder(spriteBatch, pixel, barRect, Color.Black * 0.8f, depth);
                spriteBatch.Draw(pixel,
                    new Rectangle(barRect.X + 1, barRect.Y + 1, Math.Max(0, barRect.Width - 2), 1),
                    null,
                    new Color(100, 110, 130) * (72f / 255f),
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    depth);
            }
        }

        /// <summary>Screen font for <paramref name="pixels"/>; a scale within a few percent of 1 is snapped to 1.</summary>
        private static SpriteFont GetCrispFont(float pixels, out float scale)
        {
            SpriteFont font = GraphicsManager.GetScreenFont(pixels, out scale);
            if (MathF.Abs(scale - 1f) < 0.06f)
                scale = 1f;
            return font;
        }

        private static void DrawHoverLabel(SpriteBatch spriteBatch, Vector3 screen, string name, float renderScale)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || pixel.IsDisposed)
                return;

            float scale = MathF.Max(renderScale, 0.1f);
            var font = GetCrispFont(Constants.BASE_FONT_SIZE * BaseHoverNameScale * scale, out float fontScale);
            Vector2 size = font.MeasureString(name) * fontScale;
            Vector2 textPos = new(MathF.Round(screen.X - size.X * 0.5f), MathF.Round(screen.Y - size.Y));

            int padX = Math.Max(1, (int)MathF.Round(4f * scale));
            int padY = Math.Max(1, (int)MathF.Round(2f * scale));
            var bgRect = new Rectangle(
                (int)textPos.X - padX,
                (int)textPos.Y - padY,
                (int)MathF.Ceiling(size.X) + padX * 2,
                (int)MathF.Ceiling(size.Y) + padY * 2);

            var viewport = spriteBatch.GraphicsDevice.Viewport;
            if (bgRect.Right < 0 || bgRect.Left > viewport.Width || bgRect.Bottom < 0 || bgRect.Top > viewport.Height)
                return;

            // Premultiplied colours (AlphaBlend).
            spriteBatch.Draw(pixel, bgRect, new Color(30, 50, 70) * (150f / 255f));
            float opticalY = Localization.Loc.IsChinese ? -size.Y * 0.09f : 0f;
            Vector2 drawPos = new(textPos.X, MathF.Round(textPos.Y + opticalY));
            spriteBatch.DrawString(font, name, drawPos + Vector2.One, Color.Black * 0.7f, 0f, Vector2.Zero, fontScale,
                SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, name, drawPos, new Color(176, 224, 230, 255), 0f, Vector2.Zero, fontScale,
                SpriteEffects.None, 0f);
        }

        private static readonly Color S6BannerTop = new(44, 15, 19, 232);
        private static readonly Color S6BannerBottom = new(18, 7, 10, 240);
        private static readonly Color S6BannerBorderOuter = new(8, 6, 8, 248);
        private static readonly Color S6BannerBorderGold = new(176, 138, 72, 195);
        private static readonly Color S6BannerGoldSheen = new(238, 206, 132, 135);
        private static readonly Color S6BannerText = new(255, 240, 212, 255);
        private static readonly Color S6BarBack = new(10, 8, 12, 248);
        private static readonly Color S6BarBorderGold = new(142, 110, 58, 185);
        private static readonly Color S6BarTrack = new(26, 10, 12, 240);
        private static readonly Color S6BarFillTop = new(242, 62, 48, 255);
        private static readonly Color S6BarFillBottom = new(156, 18, 18, 255);
        private static readonly Color S6BarGloss = new(255, 175, 155, 110);

        private static void DrawS6SegmentedBar(SpriteBatch sb, Texture2D pixel, int centerX, int topY, float health, int steps, int totalWidth, int barHeight)
        {
            int x = centerX - totalWidth / 2;
            int border = 1;
            int innerW = Math.Max(4, totalWidth - border * 2);
            int innerH = Math.Max(2, barHeight - border * 2);

            // Soft drop shadow + dark metallic frame + subtle gold bevel
            sb.Draw(pixel, new Rectangle(x + 1, topY + 2, totalWidth, barHeight), Color.Black * 0.55f);
            sb.Draw(pixel, new Rectangle(x, topY, totalWidth, barHeight), S6BarBack);
            sb.Draw(pixel, new Rectangle(x + border, topY + border, innerW, innerH), S6BarTrack);
            DrawBorder(sb, pixel, new Rectangle(x, topY, totalWidth, barHeight), S6BarBorderGold, 0f);

            float clamped = Math.Clamp(health, 0f, 1f);
            int fillW = clamped <= 0f ? 0 : Math.Max(2, (int)MathF.Round(innerW * clamped));
            if (fillW > 0)
            {
                var fillRect = new Rectangle(x + border, topY + border, fillW, innerH);
                UiDrawHelper.DrawVerticalGradient(sb, fillRect, S6BarFillTop, S6BarFillBottom, 4);
                sb.Draw(pixel, new Rectangle(fillRect.X, fillRect.Y, fillRect.Width, 1), S6BarGloss);

                // Subtle classic MU segment dividers (non-blocky)
                if (steps > 1 && innerW >= steps * 3)
                {
                    float stepW = innerW / (float)steps;
                    for (int k = 1; k < steps; k++)
                    {
                        int tx = x + border + (int)MathF.Round(k * stepW);
                        if (tx < fillRect.Right)
                            sb.Draw(pixel, new Rectangle(tx, fillRect.Y, 1, innerH), Color.Black * 0.24f);
                    }
                }
            }
        }

        private static void DrawS6MonsterOverheadBar(SpriteBatch sb, Vector3 screen, float health, float renderScale)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || pixel.IsDisposed)
                return;

            var viewport = sb.GraphicsDevice.Viewport;
            int cx = (int)MathF.Round(screen.X);
            int topY = (int)MathF.Round(screen.Y - 6f * MathF.Max(renderScale, 0.5f));
            if (cx < -60 || cx > viewport.Width + 60 || topY < -20 || topY > viewport.Height + 20)
                return;

            int barW = Math.Clamp((int)MathF.Round(58f * MathF.Max(renderScale, 0.7f)), 44, 74);
            int barH = Math.Clamp((int)MathF.Round(6f * MathF.Max(renderScale, 0.7f)), 5, 8);
            DrawS6SegmentedBar(sb, pixel, cx, topY, health, steps: 8, barW, barH);
        }

        private static void DrawS6MonsterTopBanner(SpriteBatch sb, string name, float? health, float renderScale)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || pixel.IsDisposed)
                return;

            var viewport = sb.GraphicsDevice.Viewport;
            float scale = MathF.Max(renderScale, 0.75f);
            var font = GetCrispFont(14f * scale, out float fontScale);
            if (font == null)
                return;

            Vector2 size = font.MeasureString(name) * fontScale;
            int cx = viewport.Width / 2;
            int topY = (int)MathF.Round(8f * scale);
            int padX = Math.Max(14, (int)MathF.Round(18f * scale));
            int padY = Math.Max(4, (int)MathF.Round(5f * scale));
            int barH = Math.Max(7, (int)MathF.Round(8f * scale));
            int barGap = Math.Max(4, (int)MathF.Round(5f * scale));
            int cardW = Math.Max((int)MathF.Ceiling(size.X) + padX * 2, (int)MathF.Round(176f * scale));
            int cardH = (int)MathF.Ceiling(size.Y) + padY * 2 + barGap + barH + Math.Max(4, (int)MathF.Round(5f * scale));
            var cardRect = new Rectangle(cx - cardW / 2, topY, cardW, cardH);

            // Soft drop shadow + dark outer rim
            sb.Draw(pixel, new Rectangle(cardRect.X + 2, cardRect.Y + 3, cardRect.Width, cardRect.Height), Color.Black * 0.45f);
            sb.Draw(pixel, cardRect, S6BannerBorderOuter);

            // Rich deep crimson-obsidian gradient body
            var innerRect = new Rectangle(cardRect.X + 1, cardRect.Y + 1, Math.Max(1, cardRect.Width - 2), Math.Max(1, cardRect.Height - 2));
            UiDrawHelper.DrawVerticalGradient(sb, innerRect, S6BannerTop, S6BannerBottom, 8);

            // Ornate antique-gold bevel + top sheen + corner rivets
            DrawBorder(sb, pixel, innerRect, S6BannerBorderGold, 0f);
            sb.Draw(pixel, new Rectangle(innerRect.X + 4, innerRect.Y + 1, Math.Max(1, innerRect.Width - 8), 1), S6BannerGoldSheen);
            int rSz = 2;
            sb.Draw(pixel, new Rectangle(innerRect.X + 2, innerRect.Y + 2, rSz, rSz), S6BannerGoldSheen);
            sb.Draw(pixel, new Rectangle(innerRect.Right - 2 - rSz, innerRect.Y + 2, rSz, rSz), S6BannerGoldSheen);
            sb.Draw(pixel, new Rectangle(innerRect.X + 2, innerRect.Bottom - 2 - rSz, rSz, rSz), S6BannerBorderGold);
            sb.Draw(pixel, new Rectangle(innerRect.Right - 2 - rSz, innerRect.Bottom - 2 - rSz, rSz, rSz), S6BannerBorderGold);

            // Crisp monster title
            float opticalY = Localization.Loc.IsChinese ? -size.Y * 0.09f : 0f;
            Vector2 textPos = new(MathF.Round(cx - size.X * 0.5f), MathF.Round(innerRect.Y + padY + opticalY));
            sb.DrawString(font, name, textPos + Vector2.One, Color.Black * 0.85f, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
            sb.DrawString(font, name, textPos, S6BannerText, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);

            // Integrated HP bar inside the bottom of the card
            int barW = Math.Max(40, cardW - Math.Max(20, (int)MathF.Round(24f * scale)));
            int barTopY = innerRect.Y + padY + (int)MathF.Ceiling(size.Y) + barGap;
            DrawS6SegmentedBar(sb, pixel, cx, barTopY, health ?? 1f, steps: 16, barW, barH);
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, float depth)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
        }
    }
}
