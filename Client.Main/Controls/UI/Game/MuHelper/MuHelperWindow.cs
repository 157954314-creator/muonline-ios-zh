#nullable enable
using System;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Ornate;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.MuHelper
{
    /// <summary>
    /// Ornate dark-gold MU Helper (游戏助手) configuration window.
    /// Compact two-column layout so all options stay visible above the bottom HUD with zero overlap.
    /// </summary>
    public sealed class MuHelperWindow : OrnateWindowControl
    {
        private const int WindowWidth = 640;
        private const int WindowHeight = 468;

        private const int HitHuntRangeDec = FirstHitId + 0;
        private const int HitHuntRangeInc = FirstHitId + 1;
        private const int HitUseSkill = FirstHitId + 2;
        private const int HitReturnOrigin = FirstHitId + 3;

        private const int HitAutoHeal = FirstHitId + 10;
        private const int HitHealPctDec = FirstHitId + 11;
        private const int HitHealPctInc = FirstHitId + 12;
        private const int HitAutoMana = FirstHitId + 13;
        private const int HitManaPctDec = FirstHitId + 14;
        private const int HitManaPctInc = FirstHitId + 15;

        private const int HitAutoPickup = FirstHitId + 20;
        private const int HitPickupRangeDec = FirstHitId + 21;
        private const int HitPickupRangeInc = FirstHitId + 22;
        private const int HitPickupZen = FirstHitId + 23;
        private const int HitPickupJewels = FirstHitId + 24;
        private const int HitPickupExcellent = FirstHitId + 25;
        private const int HitPickupAll = FirstHitId + 26;

        private const int HitSave = FirstHitId + 40;
        private const int HitStartStop = FirstHitId + 41;

        private readonly MuHelperController _controller;

        public MuHelperWindow(MuHelperController controller) : base(new Point(WindowWidth, WindowHeight))
        {
            _controller = controller;
            Title = "游戏助手设置";
            ShowCloseButton = true;
            ModalDim = 0.25f;
            Margin = new Margin { Bottom = 56 }; // Keep comfortably above the bottom HUD
            Visible = false;
            CloseRequested += (_, _) => Hide();
        }

        public void Show()
        {
            Visible = true;
            BringToFront();
            SoundController.Instance?.PlayBuffer("Sound/iCreateWindow.wav");
        }

        public void Hide()
        {
            Visible = false;
        }

        public void Toggle()
        {
            if (Visible) Hide();
            else Show();
        }

        // --- Two-column layout ----------------------------------------------------------------------

        private const int LeftColX = 36;
        private const int RightColX = 338;
        private const int ColW = 266;
        private const int RowH = 30;

        // Left column rows
        private const int YSecHunt = 56;
        private const int YHuntRange = 90;
        private const int YUseSkill = 124;
        private const int YReturnOrigin = 158;

        private const int YSecRecovery = 198;
        private const int YAutoHeal = 232;
        private const int YHealPct = 266;
        private const int YAutoMana = 300;
        private const int YManaPct = 334;

        // Right column rows
        private const int YSecPickup = 56;
        private const int YAutoPickup = 90;
        private const int YPickupRange = 124;

        private const int YSecFilter = 166;
        private const int YPickupZen = 200;
        private const int YPickupJewels = 234;
        private const int YPickupExcellent = 268;
        private const int YPickupAll = 302;

        private Rectangle SectionRect(int colX, int y) => At(colX, y, ColW, RowH);
        private Rectangle LabelRect(int colX, int y, int width = 104) => At(colX + 6, y, width, RowH);
        private Rectangle ToggleArea(int colX, int y) => At(colX + 118, y, ColW - 118, RowH);
        private Rectangle ChoiceArea(int colX, int y) => At(colX + 102, y, ColW - 102, RowH);

        // Footer buttons (well below lowest row at Y=334+30=364)
        private Rectangle SaveButtonRect => At(WindowWidth / 2 - 208, WindowHeight - 66, 188, 38);
        private Rectangle StartStopButtonRect => At(WindowWidth / 2 + 20, WindowHeight - 66, 188, 38);

        protected override int HitTest(Point p)
        {
            // Left: Hunting
            var huntBox = OrnateWidgets.ChoiceBox(ChoiceArea(LeftColX, YHuntRange));
            if (OrnateWidgets.ChoiceLeft(huntBox).Contains(p)) return HitHuntRangeDec;
            if (OrnateWidgets.ChoiceRight(huntBox).Contains(p)) return HitHuntRangeInc;
            if (OrnateWidgets.ToggleBounds(ToggleArea(LeftColX, YUseSkill)).Contains(p)) return HitUseSkill;
            if (OrnateWidgets.ToggleBounds(ToggleArea(LeftColX, YReturnOrigin)).Contains(p)) return HitReturnOrigin;

            // Left: Recovery
            if (OrnateWidgets.ToggleBounds(ToggleArea(LeftColX, YAutoHeal)).Contains(p)) return HitAutoHeal;
            var healBox = OrnateWidgets.ChoiceBox(ChoiceArea(LeftColX, YHealPct));
            if (OrnateWidgets.ChoiceLeft(healBox).Contains(p)) return HitHealPctDec;
            if (OrnateWidgets.ChoiceRight(healBox).Contains(p)) return HitHealPctInc;

            if (OrnateWidgets.ToggleBounds(ToggleArea(LeftColX, YAutoMana)).Contains(p)) return HitAutoMana;
            var manaBox = OrnateWidgets.ChoiceBox(ChoiceArea(LeftColX, YManaPct));
            if (OrnateWidgets.ChoiceLeft(manaBox).Contains(p)) return HitManaPctDec;
            if (OrnateWidgets.ChoiceRight(manaBox).Contains(p)) return HitManaPctInc;

            // Right: Pickup & Filters
            if (OrnateWidgets.ToggleBounds(ToggleArea(RightColX, YAutoPickup)).Contains(p)) return HitAutoPickup;
            var pickBox = OrnateWidgets.ChoiceBox(ChoiceArea(RightColX, YPickupRange));
            if (OrnateWidgets.ChoiceLeft(pickBox).Contains(p)) return HitPickupRangeDec;
            if (OrnateWidgets.ChoiceRight(pickBox).Contains(p)) return HitPickupRangeInc;

            if (OrnateWidgets.ToggleBounds(ToggleArea(RightColX, YPickupZen)).Contains(p)) return HitPickupZen;
            if (OrnateWidgets.ToggleBounds(ToggleArea(RightColX, YPickupJewels)).Contains(p)) return HitPickupJewels;
            if (OrnateWidgets.ToggleBounds(ToggleArea(RightColX, YPickupExcellent)).Contains(p)) return HitPickupExcellent;
            if (OrnateWidgets.ToggleBounds(ToggleArea(RightColX, YPickupAll)).Contains(p)) return HitPickupAll;

            // Footer
            if (SaveButtonRect.Contains(p)) return HitSave;
            if (StartStopButtonRect.Contains(p)) return HitStartStop;

            return HitNone;
        }

        protected override void OnActivate(int hit)
        {
            var cfg = _controller.Config;
            PlayClickSound();

            switch (hit)
            {
                case HitHuntRangeDec: cfg.HuntingRange = Math.Max(2, cfg.HuntingRange - 1); break;
                case HitHuntRangeInc: cfg.HuntingRange = Math.Min(12, cfg.HuntingRange + 1); break;
                case HitUseSkill: cfg.UseActiveSkill = !cfg.UseActiveSkill; break;
                case HitReturnOrigin: cfg.ReturnToOrigin = !cfg.ReturnToOrigin; break;

                case HitAutoHeal: cfg.AutoHealEnabled = !cfg.AutoHealEnabled; break;
                case HitHealPctDec: cfg.AutoHealPercent = Math.Max(10, cfg.AutoHealPercent - 10); break;
                case HitHealPctInc: cfg.AutoHealPercent = Math.Min(90, cfg.AutoHealPercent + 10); break;
                case HitAutoMana: cfg.AutoManaEnabled = !cfg.AutoManaEnabled; break;
                case HitManaPctDec: cfg.AutoManaPercent = Math.Max(10, cfg.AutoManaPercent - 10); break;
                case HitManaPctInc: cfg.AutoManaPercent = Math.Min(90, cfg.AutoManaPercent + 10); break;

                case HitAutoPickup: cfg.AutoPickupEnabled = !cfg.AutoPickupEnabled; break;
                case HitPickupRangeDec: cfg.PickupRange = Math.Max(2, cfg.PickupRange - 1); break;
                case HitPickupRangeInc: cfg.PickupRange = Math.Min(12, cfg.PickupRange + 1); break;
                case HitPickupZen: cfg.PickupZen = !cfg.PickupZen; break;
                case HitPickupJewels: cfg.PickupJewels = !cfg.PickupJewels; break;
                case HitPickupExcellent: cfg.PickupExcellent = !cfg.PickupExcellent; break;
                case HitPickupAll: cfg.PickupAll = !cfg.PickupAll; break;

                case HitSave:
                    _controller.SaveConfig();
                    Hide();
                    return;

                case HitStartStop:
                    _controller.SaveConfig();
                    _controller.Toggle();
                    if (_controller.IsRunning)
                        Hide();
                    return;
            }

            _controller.SaveConfig();
        }

        protected override void DrawContent(SpriteBatch sb, float alpha)
        {
            var cfg = _controller.Config;

            // Subtle vertical divider between columns
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                sb.Draw(pixel, At(WindowWidth / 2, 64, 1, 306), OrnateStyle.BronzeDark * (0.45f * alpha));
            }

            // --- Left Column: Hunting & Recovery ---
            OrnateWidgets.SectionHeader(sb, SectionRect(LeftColX, YSecHunt), "狩猎设置", alpha);
            DrawChoiceRow(sb, LeftColX, YHuntRange, "狩猎范围", $"{cfg.HuntingRange} 格",
                cfg.HuntingRange > 2, cfg.HuntingRange < 12, HitHuntRangeDec, HitHuntRangeInc, alpha);
            DrawToggleRow(sb, LeftColX, YUseSkill, "优先用技能", cfg.UseActiveSkill, alpha);
            DrawToggleRow(sb, LeftColX, YReturnOrigin, "返回原点", cfg.ReturnToOrigin, alpha);

            OrnateWidgets.SectionHeader(sb, SectionRect(LeftColX, YSecRecovery), "自动恢复", alpha);
            DrawToggleRow(sb, LeftColX, YAutoHeal, "自动喝红药", cfg.AutoHealEnabled, alpha);
            DrawChoiceRow(sb, LeftColX, YHealPct, "红药阈值", $"{cfg.AutoHealPercent}%",
                cfg.AutoHealPercent > 10, cfg.AutoHealPercent < 90, HitHealPctDec, HitHealPctInc, alpha);
            DrawToggleRow(sb, LeftColX, YAutoMana, "自动喝蓝药", cfg.AutoManaEnabled, alpha);
            DrawChoiceRow(sb, LeftColX, YManaPct, "蓝药阈值", $"{cfg.AutoManaPercent}%",
                cfg.AutoManaPercent > 10, cfg.AutoManaPercent < 90, HitManaPctDec, HitManaPctInc, alpha);

            // --- Right Column: Pickup & Filters ---
            OrnateWidgets.SectionHeader(sb, SectionRect(RightColX, YSecPickup), "自动拾取", alpha);
            DrawToggleRow(sb, RightColX, YAutoPickup, "自动拾取", cfg.AutoPickupEnabled, alpha);
            DrawChoiceRow(sb, RightColX, YPickupRange, "拾取范围", $"{cfg.PickupRange} 格",
                cfg.PickupRange > 2, cfg.PickupRange < 12, HitPickupRangeDec, HitPickupRangeInc, alpha);

            OrnateWidgets.SectionHeader(sb, SectionRect(RightColX, YSecFilter), "拾取过滤", alpha);
            DrawToggleRow(sb, RightColX, YPickupZen, "拾取金币", cfg.PickupZen, alpha);
            DrawToggleRow(sb, RightColX, YPickupJewels, "拾取宝石", cfg.PickupJewels, alpha);
            DrawToggleRow(sb, RightColX, YPickupExcellent, "卓越/套装", cfg.PickupExcellent, alpha);
            DrawToggleRow(sb, RightColX, YPickupAll, "全部物品", cfg.PickupAll, alpha);

            // Shortcut hint above footer
            OrnateDraw.TextCentered(
                sb,
                "快捷键：Home 启停挂机  |  Z 打开设置",
                At(RightColX, 340, ColW, 24),
                12.5f,
                OrnateStyle.TextMuted,
                alpha);

            // --- Footer buttons ---
            OrnateWidgets.Button(
                sb,
                SaveButtonRect,
                "保存设置",
                new WidgetState(Hover: Clicks.IsHover(HitSave), Pressed: Clicks.IsPressed(HitSave)),
                alpha);

            OrnateWidgets.PrimaryButton(
                sb,
                StartStopButtonRect,
                _controller.IsRunning ? "停止挂机" : "开始挂机",
                new WidgetState(Hover: Clicks.IsHover(HitStartStop), Pressed: Clicks.IsPressed(HitStartStop)),
                alpha);
        }

        private void DrawToggleRow(SpriteBatch sb, int colX, int y, string label, bool value, float alpha)
        {
            OrnateDraw.TextLeft(sb, label, LabelRect(colX, y), 14f, OrnateStyle.TextLight, alpha);
            OrnateWidgets.Toggle(sb, ToggleArea(colX, y), value, alpha);
        }

        private void DrawChoiceRow(
            SpriteBatch sb, int colX, int y, string label, string value,
            bool canLeft, bool canRight, int hitLeft, int hitRight, float alpha)
        {
            OrnateDraw.TextLeft(sb, label, LabelRect(colX, y, 96), 14f, OrnateStyle.TextLight, alpha);
            OrnateWidgets.Choice(
                sb,
                ChoiceArea(colX, y),
                value,
                canLeft,
                canRight,
                Clicks.IsHover(hitLeft),
                Clicks.IsHover(hitRight),
                alpha);
        }
    }
}
