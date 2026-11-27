#nullable enable
using System;
using System.Collections.Generic;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Hud;
using Client.Main.Controls.UI.Game.Skills;
using Client.Main.Controls.UI.Ornate;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.MuHelper
{
    /// <summary>
    /// 1:1 S6-parity MU Helper (游戏助手) window with refined dark-gold HD styling:
    /// - 3 S6 tabs: 狩猎 (Hunting), 拾取 (Obtaining), 其他 (Other)
    /// - 6 real MU skill icon slots (Basic, Activation 1/2, Buff 1/2/3) + click-to-pick Skill Picker popup
    /// - S6 Potion Settings popup with Red HP & Blue MP threshold bars (+ Elf AutoHeal / Summoner DrainLife)
    /// - S6 Party Settings popup for Fairy Elf & Dark Wizard
    /// - S6 Custom Extra Item TextFieldControl + Add/Delete + framed list box
    /// - Reset / Save (257-byte 0xAE server sync) / Start-Stop actions
    /// </summary>
    public sealed class MuHelperWindow : OrnateWindowControl
    {
        private const int WindowWidth = 424;
        private const int WindowHeight = 588;

        private enum SubPanel
        {
            None = 0,
            SkillPicker = 1,
            PotionConfig = 2,
            PartyConfig = 3,
            SkillCondition = 4
        }

        // Tabs
        private const int HitTabHunt = FirstHitId + 1;
        private const int HitTabPickup = FirstHitId + 2;
        private const int HitTabOther = FirstHitId + 3;

        // Tab 0: Hunting
        private const int HitHuntRangeDec = FirstHitId + 10;
        private const int HitHuntRangeInc = FirstHitId + 11;
        private const int HitPotionCheck = FirstHitId + 12;
        private const int HitPotionConfigBtn = FirstHitId + 13;
        private const int HitAutoHealSkill = FirstHitId + 14;
        private const int HitAutoDrainLife = FirstHitId + 15;
        private const int HitLongRange = FirstHitId + 16;
        private const int HitReturnOrigin = FirstHitId + 17;
        private const int HitMaxAwayDec = FirstHitId + 18;
        private const int HitMaxAwayInc = FirstHitId + 19;

        // Skill icon slots (0=Basic, 1=Sec, 2=Ter, 3=Buff0, 4=Buff1, 5=Buff2)
        private const int HitSkillSlotBase = FirstHitId + 20; // 20..25
        private const int HitSecIntervalDec = FirstHitId + 26;
        private const int HitSecIntervalInc = FirstHitId + 27;
        private const int HitTerIntervalDec = FirstHitId + 28;
        private const int HitTerIntervalInc = FirstHitId + 29;
        private const int HitUseCombo = FirstHitId + 30;
        private const int HitPartyCheck = FirstHitId + 31;
        private const int HitPartyConfigBtn = FirstHitId + 32;
        private const int HitUseDarkRaven = FirstHitId + 33;
        private const int HitDrAuto = FirstHitId + 34;
        private const int HitDrCease = FirstHitId + 35;
        private const int HitDrTogether = FirstHitId + 36;
        private const int HitFallbackAttack = FirstHitId + 37;
        private const int HitAutoBuff = FirstHitId + 38;
        private const int HitBuffIntervalDec = FirstHitId + 39;
        private const int HitBuffIntervalInc = FirstHitId + 40;

        private const int HitSecDelayCheck = FirstHitId + 41;
        private const int HitSecConCheck = FirstHitId + 42;
        private const int HitSecConConfigBtn = FirstHitId + 43;
        private const int HitTerDelayCheck = FirstHitId + 44;
        private const int HitTerConCheck = FirstHitId + 45;
        private const int HitTerConConfigBtn = FirstHitId + 46;

        // Tab 1: Obtaining (Pickup)
        private const int HitPickupRangeDec = FirstHitId + 50;
        private const int HitPickupRangeInc = FirstHitId + 51;
        private const int HitAutoRepair = FirstHitId + 52;
        private const int HitPickupAll = FirstHitId + 53;
        private const int HitPickupSelected = FirstHitId + 54;
        private const int HitPickupJewels = FirstHitId + 55;
        private const int HitPickupAncient = FirstHitId + 56;
        private const int HitPickupZen = FirstHitId + 57;
        private const int HitPickupExcellent = FirstHitId + 58;
        private const int HitPickupExtra = FirstHitId + 59;
        private const int HitAddExtraItem = FirstHitId + 60;
        private const int HitDeleteExtraItem = FirstHitId + 61;
        private const int HitPresetBase = FirstHitId + 62;      // 62..67 (6 presets)
        private const int HitExtraRowBase = FirstHitId + 70;    // 70..75 (6 visible rows)
        private const int HitExtraDelBase = FirstHitId + 76;    // 76..81 (row X buttons)

        // Tab 2: Other
        private const int HitAutoFriend = FirstHitId + 85;
        private const int HitAutoGuild = FirstHitId + 86;
        private const int HitAutoPickupMaster = FirstHitId + 87;

        // Footer
        private const int HitReset = FirstHitId + 90;
        private const int HitSave = FirstHitId + 91;
        private const int HitStartStop = FirstHitId + 92;

        // Sub-panel hits
        private const int HitSubClose = FirstHitId + 100;
        private const int HitSubClearSkill = FirstHitId + 101;
        private const int HitSubSkillBase = FirstHitId + 110;   // 110..139 (up to 30 skills)
        private const int HitSubHpCheck = FirstHitId + 150;
        private const int HitSubHpDec = FirstHitId + 151;
        private const int HitSubHpInc = FirstHitId + 152;
        private const int HitSubHpBar = FirstHitId + 153;
        private const int HitSubMpCheck = FirstHitId + 154;
        private const int HitSubMpDec = FirstHitId + 155;
        private const int HitSubMpInc = FirstHitId + 156;
        private const int HitSubMpBar = FirstHitId + 157;
        private const int HitSubClassPctDec = FirstHitId + 158;
        private const int HitSubClassPctInc = FirstHitId + 159;
        private const int HitSubClassBar = FirstHitId + 160;
        private const int HitSubPartyHealCheck = FirstHitId + 161;
        private const int HitSubPartyHealDec = FirstHitId + 162;
        private const int HitSubPartyHealInc = FirstHitId + 163;
        private const int HitSubPartyHealBar = FirstHitId + 164;
        private const int HitSubPartyBuffCheck = FirstHitId + 165;
        private const int HitSubConPreNearby = FirstHitId + 170;
        private const int HitSubConPreAttacking = FirstHitId + 171;
        private const int HitSubConSub2 = FirstHitId + 172;
        private const int HitSubConSub3 = FirstHitId + 173;
        private const int HitSubConSub4 = FirstHitId + 174;
        private const int HitSubConSub5 = FirstHitId + 175;
        private const int HitSubBackdrop = FirstHitId + 169;

        private static readonly string[] QuickPresets = { "祝福", "灵魂", "生命", "玛雅", "创造", "羽毛" };

        // Refined dark-obsidian & gold palette
        private static readonly Color CardBg = new(10, 12, 17);
        private static readonly Color CardHeaderTop = new(42, 32, 20);
        private static readonly Color CardHeaderBot = new(20, 16, 12);
        private static readonly Color CardBorder = new(118, 92, 52);
        private static readonly Color CardHighlight = new(228, 192, 120);
        private static readonly Color WellBg = new(14, 15, 20);
        private static readonly Color WellHover = new(34, 28, 20);
        private static readonly Color StatusGreen = new(110, 226, 138);
        private static readonly Color HpBarTop = new(218, 48, 42);
        private static readonly Color HpBarBot = new(134, 22, 22);
        private static readonly Color MpBarTop = new(56, 126, 232);
        private static readonly Color MpBarBot = new(26, 66, 148);
        private static readonly Color ElfBarTop = new(76, 196, 118);
        private static readonly Color ElfBarBot = new(36, 116, 66);
        private static readonly Color SumBarTop = new(176, 78, 224);
        private static readonly Color SumBarBot = new(96, 36, 138);

        private readonly MuHelperController _controller;
        private readonly TextFieldControl _extraItemField;
        private int _activeTab; // 0=狩猎, 1=拾取, 2=其他
        private SubPanel _subPanel = SubPanel.None;
        private int _editingSkillSlot = -1; // 0..5
        private int _editingConditionSkill = 1; // 1=激活技能1, 2=激活技能2
        private int _selectedExtraIndex = -1;
        private List<ushort> _pickerSkills = new();
        private ButtonState _prevRightButton = ButtonState.Released;

        public MuHelperWindow(MuHelperController controller) : base(new Point(WindowWidth, WindowHeight))
        {
            _controller = controller;
            Title = "游戏助手设置";
            ShowCloseButton = true;
            ModalDim = 0.22f;
            Margin = new Margin { Bottom = 52 };
            Visible = false;
            CloseRequested += (_, _) => Hide();

            _extraItemField = TextFieldControl.Create();
            OrnateWidgets.StyleTextField(_extraItemField, 12f);
            _extraItemField.AutoViewSize = false;
            _extraItemField.X = 36;
            _extraItemField.Y = 292;
            _extraItemField.ViewSize = new Point(226, 24);
            _extraItemField.ControlSize = _extraItemField.ViewSize;
            _extraItemField.Visible = false;
            _extraItemField.EnterKeyPressed += (_, _) => AddExtraItemFromField();
            Controls.Add(_extraItemField);
        }

        public void Show()
        {
            Title = $"游戏助手 · {MuHelperConfig.GetClassFamilyDisplayName(_controller.ClassFamily)}";
            _subPanel = SubPanel.None;
            _editingSkillSlot = -1;
            Visible = true;
            UpdateFieldVisibility();
            BringToFront();
            SoundController.Instance?.PlayBuffer("Sound/iCreateWindow.wav");
        }

        public void Hide()
        {
            _subPanel = SubPanel.None;
            _editingSkillSlot = -1;
            _extraItemField.Visible = false;
            Visible = false;
        }

        public void Toggle()
        {
            if (Visible) Hide();
            else Show();
        }

        private void UpdateFieldVisibility()
        {
            _extraItemField.Visible = Visible && _activeTab == 1 && _subPanel == SubPanel.None;
        }

        public override void Update(GameTime gameTime)
        {
            UpdateFieldVisibility();

            // Support right-click on skill slots to clear immediately (classic S6 QoL)
            var mouse = MuGame.Instance.Mouse;
            if (Status == GameControlStatus.Ready && Visible && _activeTab == 0 && _subPanel == SubPanel.None)
            {
                if (mouse.RightButton == ButtonState.Pressed && _prevRightButton == ButtonState.Released)
                {
                    Point p = UiScaler.ToVirtual(mouse.Position);
                    for (int slot = 0; slot < 6; slot++)
                    {
                        if (slot == 2 && _controller.ClassFamily == MuHelperClassFamily.DarkLord) continue;
                        if (SkillSlotRect(slot).Contains(p))
                        {
                            SetSkillForSlot(slot, 0);
                            PlayClickSound();
                            _controller.SaveConfig(syncToServer: false);
                            break;
                        }
                    }
                }
            }
            _prevRightButton = mouse.RightButton;

            base.Update(gameTime);
        }

        // --- Layout Metrics -------------------------------------------------------------------------

        private const int PadX = 22;
        private const int ContentW = WindowWidth - PadX * 2; // 380
        private const int NavY = 48;
        private const int NavH = 30;
        private const int BodyY = 86;

        private Rectangle NavBarRect => At(PadX, NavY, ContentW, NavH);

        private Rectangle TabRect(int idx)
        {
            const int tabW = 90;
            const int gap = 4;
            return At(PadX + 3 + idx * (tabW + gap), NavY + 3, tabW, NavH - 6);
        }

        private Rectangle StatusPillRect => At(WindowWidth - PadX - 92, NavY + 4, 89, NavH - 8);

        // Tab 0 cards
        private Rectangle HuntTopCard => At(PadX, BodyY, ContentW, 84);
        private Rectangle HuntMoveCard => At(PadX, BodyY + 92, ContentW, 64);
        private Rectangle HuntSkillCard => At(PadX, BodyY + 164, ContentW, 174);
        private Rectangle HuntBuffCard => At(PadX, BodyY + 346, ContentW, 88);

        // Tab 0 controls
        private Rectangle HuntRangeDecRect => At(PadX + 128, BodyY + 48, 22, 22);
        private Rectangle HuntRangeIncRect => At(PadX + 154, BodyY + 48, 22, 22);
        private Rectangle PotionCheckRect => At(PadX + 196, BodyY + 14, 90, 22);
        private Rectangle PotionConfigBtnRect => At(PadX + 306, BodyY + 13, 62, 24);
        private Rectangle ClassSelfSkillCheckRect => At(PadX + 196, BodyY + 46, 168, 22);

        private Rectangle LongRangeCheckRect => At(PadX + 12, BodyY + 100, 170, 22);
        private Rectangle ReturnOriginCheckRect => At(PadX + 12, BodyY + 126, 130, 22);
        private Rectangle MaxAwayDecRect => At(PadX + 242, BodyY + 126, 22, 22);
        private Rectangle MaxAwayIncRect => At(PadX + 320, BodyY + 126, 22, 22);

        private Rectangle SkillSlotRect(int slot) => slot switch
        {
            0 => At(PadX + 14, BodyY + 190, 38, 44),  // Basic
            1 => At(PadX + 90, BodyY + 190, 38, 44),  // Activation 1
            2 => At(PadX + 90, BodyY + 266, 38, 44),  // Activation 2
            3 => At(PadX + 16, BodyY + 382, 38, 44),  // Buff 1
            4 => At(PadX + 64, BodyY + 382, 38, 44),  // Buff 2
            _ => At(PadX + 112, BodyY + 382, 38, 44), // Buff 3
        };

        private Rectangle SecDelayCheckRect => At(PadX + 140, BodyY + 188, 62, 22);
        private Rectangle SecIntDecRect => At(PadX + 244, BodyY + 188, 22, 22);
        private Rectangle SecIntIncRect => At(PadX + 322, BodyY + 188, 22, 22);
        private Rectangle SecConCheckRect => At(PadX + 140, BodyY + 214, 62, 22);
        private Rectangle SecConBtnRect => At(PadX + 206, BodyY + 213, 56, 22);

        private Rectangle TerDelayCheckRect => At(PadX + 140, BodyY + 264, 62, 22);
        private Rectangle TerIntDecRect => At(PadX + 244, BodyY + 264, 22, 22);
        private Rectangle TerIntIncRect => At(PadX + 322, BodyY + 264, 22, 22);
        private Rectangle TerConCheckRect => At(PadX + 140, BodyY + 290, 62, 22);
        private Rectangle TerConBtnRect => At(PadX + 206, BodyY + 289, 56, 22);

        private Rectangle ComboCheckRect => At(PadX + 12, BodyY + 266, 72, 22);
        private Rectangle PartyCheckRect => At(PadX + 12, BodyY + 260, 72, 22);
        private Rectangle PartyConfigBtnRect => At(PadX + 14, BodyY + 286, 62, 22);
        private Rectangle DarkRavenCheckRect => At(PadX + 12, BodyY + 262, 110, 22);
        private Rectangle DrAutoRect => At(PadX + 12, BodyY + 288, 108, 22);
        private Rectangle DrCeaseRect => At(PadX + 126, BodyY + 288, 108, 22);
        private Rectangle DrTogetherRect => At(PadX + 240, BodyY + 288, 126, 22);
        private Rectangle FallbackCheckRect => At(PadX + 12, BodyY + 318, 160, 16);

        private Rectangle AutoBuffCheckRect => At(PadX + 12, BodyY + 354, 150, 22);
        private Rectangle BuffIntDecRect => At(PadX + 242, BodyY + 354, 22, 22);
        private Rectangle BuffIntIncRect => At(PadX + 320, BodyY + 354, 22, 22);

        // Tab 1 controls
        private Rectangle PickupTopCard => At(PadX, BodyY, ContentW, 68);
        private Rectangle PickupFilterCard => At(PadX, BodyY + 76, ContentW, 118);
        private Rectangle PickupExtraCard => At(PadX, BodyY + 202, ContentW, 212);

        private Rectangle PickupRangeDecRect => At(PadX + 150, BodyY + 36, 22, 22);
        private Rectangle PickupRangeIncRect => At(PadX + 176, BodyY + 36, 22, 22);
        private Rectangle RepairCheckRect => At(PadX + 220, BodyY + 14, 146, 22);

        private Rectangle PickAllCheckRect => At(PadX + 12, BodyY + 86, 220, 22);
        private Rectangle PickSelCheckRect => At(PadX + 12, BodyY + 110, 220, 22);
        private Rectangle PickJewelCheckRect => At(PadX + 28, BodyY + 134, 148, 20);
        private Rectangle PickAncientCheckRect => At(PadX + 196, BodyY + 134, 148, 20);
        private Rectangle PickZenCheckRect => At(PadX + 28, BodyY + 154, 148, 20);
        private Rectangle PickExcCheckRect => At(PadX + 196, BodyY + 154, 148, 20);
        private Rectangle PickExtraCheckRect => At(PadX + 28, BodyY + 174, 180, 20);

        private Rectangle ExtraInputFrameRect => At(PadX + 12, BodyY + 206, 232, 24);
        private Rectangle AddExtraBtnRect => At(PadX + 250, BodyY + 206, 56, 24);
        private Rectangle DelExtraBtnRect => At(PadX + 312, BodyY + 206, 56, 24);

        private Rectangle QuickPresetRect(int idx)
        {
            const int w = 55;
            const int gap = 5;
            return At(PadX + 12 + idx * (w + gap), BodyY + 236, w, 22);
        }

        private Rectangle ExtraListBoxRect => At(PadX + 12, BodyY + 264, ContentW - 24, 140);
        private Rectangle ExtraRowRect(int idx) => At(PadX + 14, BodyY + 268 + idx * 22, ContentW - 28, 20);
        private Rectangle ExtraRowDelRect(int idx) => At(PadX + ContentW - 36, BodyY + 269 + idx * 22, 18, 18);

        // Tab 2 controls
        private Rectangle OtherTopCard => At(PadX, BodyY, ContentW, 134);
        private Rectangle OtherInfoCard => At(PadX, BodyY + 142, ContentW, 150);

        private Rectangle AutoFriendCheckRect => At(PadX + 12, BodyY + 40, 280, 24);
        private Rectangle AutoGuildCheckRect => At(PadX + 12, BodyY + 70, 280, 24);
        private Rectangle AutoPickupMasterRect => At(PadX + 12, BodyY + 100, 280, 24);

        // Footer
        private Rectangle ResetButtonRect => At(PadX, WindowHeight - 48, 92, 30);
        private Rectangle SaveButtonRect => At(PadX + 100, WindowHeight - 48, 134, 30);
        private Rectangle StartStopButtonRect => At(PadX + 242, WindowHeight - 48, 138, 30);

        // Sub-panel overlay bounds
        private Rectangle SubBoxRect => _subPanel switch
        {
            SubPanel.SkillPicker => At(32, 118, WindowWidth - 64, 308),
            SubPanel.PotionConfig => At(36, 138, WindowWidth - 72, 266),
            SubPanel.PartyConfig => At(36, 152, WindowWidth - 72, 220),
            SubPanel.SkillCondition => At(36, 136, WindowWidth - 72, 276),
            _ => Rectangle.Empty
        };

        // Skill condition sub-panel rects
        private Rectangle SubConPreNearbyRect => new(SubBoxRect.X + 20, SubBoxRect.Y + 58, 240, 22);
        private Rectangle SubConPreAttackingRect => new(SubBoxRect.X + 20, SubBoxRect.Y + 84, 240, 22);
        private Rectangle SubConSub2Rect => new(SubBoxRect.X + 20, SubBoxRect.Y + 136, 145, 22);
        private Rectangle SubConSub3Rect => new(SubBoxRect.X + 178, SubBoxRect.Y + 136, 145, 22);
        private Rectangle SubConSub4Rect => new(SubBoxRect.X + 20, SubBoxRect.Y + 164, 145, 22);
        private Rectangle SubConSub5Rect => new(SubBoxRect.X + 178, SubBoxRect.Y + 164, 145, 22);

        private Rectangle SubSkillCellRect(int idx)
        {
            var box = SubBoxRect;
            int col = idx % 6;
            int row = idx / 6;
            return new Rectangle(box.X + 18 + col * 55, box.Y + 42 + row * 54, 46, 48);
        }

        private Rectangle SubClearSkillRect => new(SubBoxRect.X + 16, SubBoxRect.Bottom - 38, 108, 26);
        private Rectangle SubCloseBtnRect => new(SubBoxRect.Right - 108, SubBoxRect.Bottom - 38, 92, 26);

        // Potion sub-panel rects
        private Rectangle SubHpCheckRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 42, 140, 22);
        private Rectangle SubHpDecRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 68, 22, 22);
        private Rectangle SubHpBarRect => new(SubBoxRect.X + 44, SubBoxRect.Y + 70, 222, 18);
        private Rectangle SubHpIncRect => new(SubBoxRect.X + 272, SubBoxRect.Y + 68, 22, 22);

        private Rectangle SubMpCheckRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 104, 140, 22);
        private Rectangle SubMpDecRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 130, 22, 22);
        private Rectangle SubMpBarRect => new(SubBoxRect.X + 44, SubBoxRect.Y + 132, 222, 18);
        private Rectangle SubMpIncRect => new(SubBoxRect.X + 272, SubBoxRect.Y + 130, 22, 22);

        private Rectangle SubClassDecRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 186, 22, 22);
        private Rectangle SubClassBarRect => new(SubBoxRect.X + 44, SubBoxRect.Y + 188, 222, 18);
        private Rectangle SubClassIncRect => new(SubBoxRect.X + 272, SubBoxRect.Y + 186, 22, 22);

        // Party sub-panel rects
        private Rectangle SubPartyHealCheckRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 44, 180, 22);
        private Rectangle SubPartyHealDecRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 72, 22, 22);
        private Rectangle SubPartyHealBarRect => new(SubBoxRect.X + 44, SubBoxRect.Y + 74, 222, 18);
        private Rectangle SubPartyHealIncRect => new(SubBoxRect.X + 272, SubBoxRect.Y + 72, 22, 22);
        private Rectangle SubPartyBuffCheckRect => new(SubBoxRect.X + 16, SubBoxRect.Y + 114, 220, 22);

        // --- Skill Helpers --------------------------------------------------------------------------

        private ushort GetSkillForSlot(int slot) => slot switch
        {
            0 => _controller.Config.BasicSkillId,
            1 => _controller.Config.SecondarySkillId,
            2 => _controller.Config.TertiarySkillId,
            3 => _controller.Config.BuffSkill0Id,
            4 => _controller.Config.BuffSkill1Id,
            5 => _controller.Config.BuffSkill2Id,
            _ => 0
        };

        private void SetSkillForSlot(int slot, ushort skillId)
        {
            var cfg = _controller.Config;
            switch (slot)
            {
                case 0: cfg.BasicSkillId = skillId; break;
                case 1: cfg.SecondarySkillId = skillId; break;
                case 2: cfg.TertiarySkillId = skillId; break;
                case 3: cfg.BuffSkill0Id = skillId; break;
                case 4: cfg.BuffSkill1Id = skillId; break;
                case 5: cfg.BuffSkill2Id = skillId; break;
            }
        }

        private void OpenSkillPicker(int slot)
        {
            _editingSkillSlot = slot;
            _pickerSkills = new List<ushort>();
            bool wantBuff = slot >= 3;
            var skills = MuGame.Network?.GetCharacterState()?.GetSkills();
            if (skills != null)
            {
                foreach (var s in skills)
                {
                    if (s == null) continue;
                    bool isSelf = SkillDatabase.IsSelfSkill(s.SkillId);
                    if (isSelf == wantBuff && !_pickerSkills.Contains(s.SkillId))
                        _pickerSkills.Add(s.SkillId);
                }
            }
            _subPanel = SubPanel.SkillPicker;
        }

        private void AddExtraItemFromField()
        {
            string kw = (_extraItemField.Value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(kw)) return;
            var cfg = _controller.Config;
            if (!cfg.ExtraPickupKeywords.Contains(kw) && cfg.ExtraPickupKeywords.Count < 12)
            {
                cfg.ExtraPickupKeywords.Add(kw);
                cfg.PickupExtraEnabled = true;
                cfg.PickupAll = false;
                _controller.SaveConfig(syncToServer: false);
            }
            _extraItemField.Value = string.Empty;
        }

        // --- Hit Testing ----------------------------------------------------------------------------

        protected override int HitTest(Point p)
        {
            if (_subPanel != SubPanel.None)
                return HitTestSubPanel(p);

            if (TabRect(0).Contains(p)) return HitTabHunt;
            if (TabRect(1).Contains(p)) return HitTabPickup;
            if (TabRect(2).Contains(p)) return HitTabOther;

            if (ResetButtonRect.Contains(p)) return HitReset;
            if (SaveButtonRect.Contains(p)) return HitSave;
            if (StartStopButtonRect.Contains(p)) return HitStartStop;

            if (_activeTab == 0) return HitTestHuntTab(p);
            if (_activeTab == 1) return HitTestPickupTab(p);
            return HitTestOtherTab(p);
        }

        private int HitTestSubPanel(Point p)
        {
            if (!SubBoxRect.Contains(p)) return HitSubBackdrop;
            if (SubCloseBtnRect.Contains(p)) return HitSubClose;

            if (_subPanel == SubPanel.SkillPicker)
            {
                if (SubClearSkillRect.Contains(p)) return HitSubClearSkill;
                int count = Math.Min(18, _pickerSkills.Count);
                for (int i = 0; i < count; i++)
                {
                    if (SubSkillCellRect(i).Contains(p)) return HitSubSkillBase + i;
                }
                return HitNone;
            }

            if (_subPanel == SubPanel.PotionConfig)
            {
                if (SubHpCheckRect.Contains(p)) return HitSubHpCheck;
                if (SubHpDecRect.Contains(p)) return HitSubHpDec;
                if (SubHpIncRect.Contains(p)) return HitSubHpInc;
                if (SubHpBarRect.Contains(p)) return HitSubHpBar;
                if (SubMpCheckRect.Contains(p)) return HitSubMpCheck;
                if (SubMpDecRect.Contains(p)) return HitSubMpDec;
                if (SubMpIncRect.Contains(p)) return HitSubMpInc;
                if (SubMpBarRect.Contains(p)) return HitSubMpBar;
                var fam = _controller.ClassFamily;
                if (fam == MuHelperClassFamily.FairyElf || fam == MuHelperClassFamily.Summoner)
                {
                    if (SubClassDecRect.Contains(p)) return HitSubClassPctDec;
                    if (SubClassIncRect.Contains(p)) return HitSubClassPctInc;
                    if (SubClassBarRect.Contains(p)) return HitSubClassBar;
                }
                return HitNone;
            }

            if (_subPanel == SubPanel.PartyConfig)
            {
                if (_controller.ClassFamily == MuHelperClassFamily.FairyElf)
                {
                    if (SubPartyHealCheckRect.Contains(p)) return HitSubPartyHealCheck;
                    if (SubPartyHealDecRect.Contains(p)) return HitSubPartyHealDec;
                    if (SubPartyHealIncRect.Contains(p)) return HitSubPartyHealInc;
                    if (SubPartyHealBarRect.Contains(p)) return HitSubPartyHealBar;
                }
                if (SubPartyBuffCheckRect.Contains(p)) return HitSubPartyBuffCheck;
                return HitNone;
            }

            if (_subPanel == SubPanel.SkillCondition)
            {
                if (SubConPreNearbyRect.Contains(p)) return HitSubConPreNearby;
                if (SubConPreAttackingRect.Contains(p)) return HitSubConPreAttacking;
                if (SubConSub2Rect.Contains(p)) return HitSubConSub2;
                if (SubConSub3Rect.Contains(p)) return HitSubConSub3;
                if (SubConSub4Rect.Contains(p)) return HitSubConSub4;
                if (SubConSub5Rect.Contains(p)) return HitSubConSub5;
                return HitNone;
            }

            return HitNone;
        }

        private int HitTestHuntTab(Point p)
        {
            var family = _controller.ClassFamily;

            if (HuntRangeDecRect.Contains(p)) return HitHuntRangeDec;
            if (HuntRangeIncRect.Contains(p)) return HitHuntRangeInc;
            if (PotionCheckRect.Contains(p)) return HitPotionCheck;
            if (PotionConfigBtnRect.Contains(p)) return HitPotionConfigBtn;

            if (family == MuHelperClassFamily.FairyElf && ClassSelfSkillCheckRect.Contains(p)) return HitAutoHealSkill;
            if (family == MuHelperClassFamily.Summoner && ClassSelfSkillCheckRect.Contains(p)) return HitAutoDrainLife;

            if (LongRangeCheckRect.Contains(p)) return HitLongRange;
            if (ReturnOriginCheckRect.Contains(p)) return HitReturnOrigin;
            if (MaxAwayDecRect.Contains(p)) return HitMaxAwayDec;
            if (MaxAwayIncRect.Contains(p)) return HitMaxAwayInc;

            for (int slot = 0; slot < 6; slot++)
            {
                if (slot == 2 && family == MuHelperClassFamily.DarkLord) continue;
                if (SkillSlotRect(slot).Contains(p)) return HitSkillSlotBase + slot;
            }

            if (SecDelayCheckRect.Contains(p)) return HitSecDelayCheck;
            if (SecIntDecRect.Contains(p)) return HitSecIntervalDec;
            if (SecIntIncRect.Contains(p)) return HitSecIntervalInc;
            if (SecConCheckRect.Contains(p)) return HitSecConCheck;
            if (SecConBtnRect.Contains(p)) return HitSecConConfigBtn;

            if (family != MuHelperClassFamily.DarkLord)
            {
                if (TerDelayCheckRect.Contains(p)) return HitTerDelayCheck;
                if (TerIntDecRect.Contains(p)) return HitTerIntervalDec;
                if (TerIntIncRect.Contains(p)) return HitTerIntervalInc;
                if (TerConCheckRect.Contains(p)) return HitTerConCheck;
                if (TerConBtnRect.Contains(p)) return HitTerConConfigBtn;
            }

            if (family == MuHelperClassFamily.DarkKnight && ComboCheckRect.Contains(p)) return HitUseCombo;
            if ((family == MuHelperClassFamily.FairyElf || family == MuHelperClassFamily.DarkWizard))
            {
                if (PartyCheckRect.Contains(p)) return HitPartyCheck;
                if (PartyConfigBtnRect.Contains(p)) return HitPartyConfigBtn;
            }
            if (family == MuHelperClassFamily.DarkLord)
            {
                if (DarkRavenCheckRect.Contains(p)) return HitUseDarkRaven;
                if (DrAutoRect.Contains(p)) return HitDrAuto;
                if (DrCeaseRect.Contains(p)) return HitDrCease;
                if (DrTogetherRect.Contains(p)) return HitDrTogether;
            }

            if (FallbackCheckRect.Contains(p)) return HitFallbackAttack;

            if (AutoBuffCheckRect.Contains(p)) return HitAutoBuff;
            if (BuffIntDecRect.Contains(p)) return HitBuffIntervalDec;
            if (BuffIntIncRect.Contains(p)) return HitBuffIntervalInc;

            return HitNone;
        }

        private int HitTestPickupTab(Point p)
        {
            if (PickupRangeDecRect.Contains(p)) return HitPickupRangeDec;
            if (PickupRangeIncRect.Contains(p)) return HitPickupRangeInc;
            if (RepairCheckRect.Contains(p)) return HitAutoRepair;

            if (PickAllCheckRect.Contains(p)) return HitPickupAll;
            if (PickSelCheckRect.Contains(p)) return HitPickupSelected;
            if (PickJewelCheckRect.Contains(p)) return HitPickupJewels;
            if (PickAncientCheckRect.Contains(p)) return HitPickupAncient;
            if (PickZenCheckRect.Contains(p)) return HitPickupZen;
            if (PickExcCheckRect.Contains(p)) return HitPickupExcellent;
            if (PickExtraCheckRect.Contains(p)) return HitPickupExtra;

            if (AddExtraBtnRect.Contains(p)) return HitAddExtraItem;
            if (DelExtraBtnRect.Contains(p)) return HitDeleteExtraItem;

            for (int i = 0; i < QuickPresets.Length; i++)
            {
                if (QuickPresetRect(i).Contains(p)) return HitPresetBase + i;
            }

            int show = Math.Min(6, _controller.Config.ExtraPickupKeywords.Count);
            for (int i = 0; i < show; i++)
            {
                if (ExtraRowDelRect(i).Contains(p)) return HitExtraDelBase + i;
                if (ExtraRowRect(i).Contains(p)) return HitExtraRowBase + i;
            }

            return HitNone;
        }

        private int HitTestOtherTab(Point p)
        {
            if (AutoFriendCheckRect.Contains(p)) return HitAutoFriend;
            if (AutoGuildCheckRect.Contains(p)) return HitAutoGuild;
            if (AutoPickupMasterRect.Contains(p)) return HitAutoPickupMaster;
            return HitNone;
        }

        // --- Activation -----------------------------------------------------------------------------

        private static int PercentFromBarClick(Rectangle bar)
        {
            Point mp = UiScaler.ToVirtual(MuGame.Instance.Mouse.Position);
            float t = MathHelper.Clamp((mp.X - bar.X) / (float)Math.Max(1, bar.Width), 0f, 1f);
            int step = (int)MathF.Round(t * 8f); // 0..8 -> 10%..90%
            return Math.Clamp(10 + step * 10, 10, 90);
        }

        protected override void OnActivate(int hit)
        {
            var cfg = _controller.Config;
            PlayClickSound();

            // Sub-panel interactions
            if (hit == HitSubClose || hit == HitSubBackdrop)
            {
                _subPanel = SubPanel.None;
                _editingSkillSlot = -1;
                return;
            }
            if (hit == HitSubClearSkill && _editingSkillSlot >= 0)
            {
                SetSkillForSlot(_editingSkillSlot, 0);
                _subPanel = SubPanel.None;
                _editingSkillSlot = -1;
                _controller.SaveConfig(syncToServer: false);
                return;
            }
            if (hit >= HitSubSkillBase && hit < HitSubSkillBase + _pickerSkills.Count && _editingSkillSlot >= 0)
            {
                SetSkillForSlot(_editingSkillSlot, _pickerSkills[hit - HitSubSkillBase]);
                _subPanel = SubPanel.None;
                _editingSkillSlot = -1;
                _controller.SaveConfig(syncToServer: false);
                return;
            }

            // Tabs
            if (hit == HitTabHunt) { _activeTab = 0; UpdateFieldVisibility(); return; }
            if (hit == HitTabPickup) { _activeTab = 1; UpdateFieldVisibility(); return; }
            if (hit == HitTabOther) { _activeTab = 2; UpdateFieldVisibility(); return; }

            // Skill slots
            if (hit >= HitSkillSlotBase && hit < HitSkillSlotBase + 6)
            {
                OpenSkillPicker(hit - HitSkillSlotBase);
                return;
            }

            // Quick presets
            if (hit >= HitPresetBase && hit < HitPresetBase + QuickPresets.Length)
            {
                string kw = QuickPresets[hit - HitPresetBase];
                if (!cfg.ExtraPickupKeywords.Contains(kw) && cfg.ExtraPickupKeywords.Count < 12)
                {
                    cfg.ExtraPickupKeywords.Add(kw);
                    cfg.PickupExtraEnabled = true;
                    cfg.PickupAll = false;
                }
                else
                {
                    cfg.ExtraPickupKeywords.Remove(kw);
                }
                _controller.SaveConfig(syncToServer: false);
                return;
            }

            // Extra item list row select / delete
            if (hit >= HitExtraRowBase && hit < HitExtraRowBase + cfg.ExtraPickupKeywords.Count)
            {
                _selectedExtraIndex = hit - HitExtraRowBase;
                return;
            }
            if (hit >= HitExtraDelBase && hit < HitExtraDelBase + cfg.ExtraPickupKeywords.Count)
            {
                int idx = hit - HitExtraDelBase;
                cfg.ExtraPickupKeywords.RemoveAt(idx);
                if (_selectedExtraIndex >= cfg.ExtraPickupKeywords.Count) _selectedExtraIndex = -1;
                _controller.SaveConfig(syncToServer: false);
                return;
            }

            switch (hit)
            {
                case HitHuntRangeDec: cfg.HuntingRange = Math.Max(1, cfg.HuntingRange - 1); break;
                case HitHuntRangeInc: cfg.HuntingRange = Math.Min(8, cfg.HuntingRange + 1); break;
                case HitPotionCheck: cfg.AutoHealEnabled = !cfg.AutoHealEnabled; break;
                case HitPotionConfigBtn: _subPanel = SubPanel.PotionConfig; return;
                case HitAutoHealSkill: cfg.AutoHealSkillEnabled = !cfg.AutoHealSkillEnabled; break;
                case HitAutoDrainLife: cfg.AutoDrainLifeEnabled = !cfg.AutoDrainLifeEnabled; break;
                case HitLongRange: cfg.LongRangeCounterAttack = !cfg.LongRangeCounterAttack; break;
                case HitReturnOrigin: cfg.ReturnToOrigin = !cfg.ReturnToOrigin; break;
                case HitMaxAwayDec: cfg.MaxSecondsAway = Math.Max(0, cfg.MaxSecondsAway - 5); break;
                case HitMaxAwayInc: cfg.MaxSecondsAway = Math.Min(60, cfg.MaxSecondsAway + 5); break;

                case HitSecDelayCheck:
                    cfg.SecondarySkillUseDelay = !cfg.SecondarySkillUseDelay;
                    if (cfg.SecondarySkillUseDelay) cfg.SecondarySkillUseCondition = false;
                    break;
                case HitSecConCheck:
                    cfg.SecondarySkillUseCondition = !cfg.SecondarySkillUseCondition;
                    if (cfg.SecondarySkillUseCondition) cfg.SecondarySkillUseDelay = false;
                    break;
                case HitSecConConfigBtn:
                    cfg.SecondarySkillUseCondition = true;
                    cfg.SecondarySkillUseDelay = false;
                    _editingConditionSkill = 1;
                    _subPanel = SubPanel.SkillCondition;
                    return;
                case HitSecIntervalDec: cfg.SecondarySkillIntervalSec = Math.Max(1, cfg.SecondarySkillIntervalSec - 1); break;
                case HitSecIntervalInc: cfg.SecondarySkillIntervalSec = Math.Min(60, cfg.SecondarySkillIntervalSec + 1); break;

                case HitTerDelayCheck:
                    cfg.TertiarySkillUseDelay = !cfg.TertiarySkillUseDelay;
                    if (cfg.TertiarySkillUseDelay) cfg.TertiarySkillUseCondition = false;
                    break;
                case HitTerConCheck:
                    cfg.TertiarySkillUseCondition = !cfg.TertiarySkillUseCondition;
                    if (cfg.TertiarySkillUseCondition) cfg.TertiarySkillUseDelay = false;
                    break;
                case HitTerConConfigBtn:
                    cfg.TertiarySkillUseCondition = true;
                    cfg.TertiarySkillUseDelay = false;
                    _editingConditionSkill = 2;
                    _subPanel = SubPanel.SkillCondition;
                    return;
                case HitTerIntervalDec: cfg.TertiarySkillIntervalSec = Math.Max(1, cfg.TertiarySkillIntervalSec - 1); break;
                case HitTerIntervalInc: cfg.TertiarySkillIntervalSec = Math.Min(60, cfg.TertiarySkillIntervalSec + 1); break;
                case HitUseCombo: cfg.UseCombo = !cfg.UseCombo; break;
                case HitPartyCheck: cfg.PartySupportEnabled = !cfg.PartySupportEnabled; break;
                case HitPartyConfigBtn: _subPanel = SubPanel.PartyConfig; return;
                case HitUseDarkRaven: cfg.UseDarkRaven = !cfg.UseDarkRaven; break;
                case HitDrCease: cfg.DarkRavenMode = 0; break;
                case HitDrAuto: cfg.DarkRavenMode = 1; break;
                case HitDrTogether: cfg.DarkRavenMode = 2; break;
                case HitFallbackAttack: cfg.FallbackBasicAttack = !cfg.FallbackBasicAttack; break;
                case HitAutoBuff: cfg.AutoBuffEnabled = !cfg.AutoBuffEnabled; break;
                case HitBuffIntervalDec: cfg.BuffIntervalSec = Math.Max(5, cfg.BuffIntervalSec - 5); break;
                case HitBuffIntervalInc: cfg.BuffIntervalSec = Math.Min(300, cfg.BuffIntervalSec + 5); break;

                case HitPickupRangeDec: cfg.PickupRange = Math.Max(1, cfg.PickupRange - 1); break;
                case HitPickupRangeInc: cfg.PickupRange = Math.Min(8, cfg.PickupRange + 1); break;
                case HitAutoRepair: cfg.AutoRepairEnabled = !cfg.AutoRepairEnabled; break;
                case HitPickupAll:
                    cfg.PickupAll = !cfg.PickupAll;
                    if (cfg.PickupAll) cfg.AutoPickupEnabled = true;
                    break;
                case HitPickupSelected:
                    cfg.AutoPickupEnabled = true;
                    cfg.PickupAll = false;
                    break;
                case HitPickupJewels: cfg.PickupJewels = !cfg.PickupJewels; cfg.PickupAll = false; break;
                case HitPickupAncient: cfg.PickupAncient = !cfg.PickupAncient; cfg.PickupAll = false; break;
                case HitPickupZen: cfg.PickupZen = !cfg.PickupZen; cfg.PickupAll = false; break;
                case HitPickupExcellent: cfg.PickupExcellent = !cfg.PickupExcellent; cfg.PickupAll = false; break;
                case HitPickupExtra: cfg.PickupExtraEnabled = !cfg.PickupExtraEnabled; cfg.PickupAll = false; break;
                case HitAddExtraItem: AddExtraItemFromField(); return;
                case HitDeleteExtraItem:
                    if (_selectedExtraIndex >= 0 && _selectedExtraIndex < cfg.ExtraPickupKeywords.Count)
                    {
                        cfg.ExtraPickupKeywords.RemoveAt(_selectedExtraIndex);
                        _selectedExtraIndex = -1;
                    }
                    else if (cfg.ExtraPickupKeywords.Count > 0)
                    {
                        cfg.ExtraPickupKeywords.RemoveAt(cfg.ExtraPickupKeywords.Count - 1);
                    }
                    break;

                case HitAutoFriend: cfg.AutoAcceptFriend = !cfg.AutoAcceptFriend; break;
                case HitAutoGuild: cfg.AutoAcceptGuild = !cfg.AutoAcceptGuild; break;
                case HitAutoPickupMaster: cfg.AutoPickupEnabled = !cfg.AutoPickupEnabled; break;

                // Sub-panel Potion / Party
                case HitSubHpCheck: cfg.AutoHealEnabled = !cfg.AutoHealEnabled; break;
                case HitSubHpDec: cfg.AutoHealPercent = Math.Max(10, cfg.AutoHealPercent - 10); break;
                case HitSubHpInc: cfg.AutoHealPercent = Math.Min(90, cfg.AutoHealPercent + 10); break;
                case HitSubHpBar: cfg.AutoHealPercent = PercentFromBarClick(SubHpBarRect); break;
                case HitSubMpCheck: cfg.AutoManaEnabled = !cfg.AutoManaEnabled; break;
                case HitSubMpDec: cfg.AutoManaPercent = Math.Max(10, cfg.AutoManaPercent - 10); break;
                case HitSubMpInc: cfg.AutoManaPercent = Math.Min(90, cfg.AutoManaPercent + 10); break;
                case HitSubMpBar: cfg.AutoManaPercent = PercentFromBarClick(SubMpBarRect); break;
                case HitSubClassPctDec:
                    if (_controller.ClassFamily == MuHelperClassFamily.FairyElf)
                        cfg.AutoHealSkillPercent = Math.Max(10, cfg.AutoHealSkillPercent - 10);
                    else
                        cfg.AutoDrainLifePercent = Math.Max(10, cfg.AutoDrainLifePercent - 10);
                    break;
                case HitSubClassPctInc:
                    if (_controller.ClassFamily == MuHelperClassFamily.FairyElf)
                        cfg.AutoHealSkillPercent = Math.Min(90, cfg.AutoHealSkillPercent + 10);
                    else
                        cfg.AutoDrainLifePercent = Math.Min(90, cfg.AutoDrainLifePercent + 10);
                    break;
                case HitSubClassBar:
                    int pct = PercentFromBarClick(SubClassBarRect);
                    if (_controller.ClassFamily == MuHelperClassFamily.FairyElf) cfg.AutoHealSkillPercent = pct;
                    else cfg.AutoDrainLifePercent = pct;
                    break;
                case HitSubPartyHealCheck: cfg.PartyAutoHealEnabled = !cfg.PartyAutoHealEnabled; break;
                case HitSubPartyHealDec: cfg.PartyHealPercent = Math.Max(10, cfg.PartyHealPercent - 10); break;
                case HitSubPartyHealInc: cfg.PartyHealPercent = Math.Min(90, cfg.PartyHealPercent + 10); break;
                case HitSubPartyHealBar: cfg.PartyHealPercent = PercentFromBarClick(SubPartyHealBarRect); break;
                case HitSubPartyBuffCheck: cfg.PartyBuffEnabled = !cfg.PartyBuffEnabled; break;

                // Sub-panel Skill Condition
                case HitSubConPreNearby:
                    if (_editingConditionSkill == 1) cfg.SecondarySkillPreCon = 0;
                    else cfg.TertiarySkillPreCon = 0;
                    break;
                case HitSubConPreAttacking:
                    if (_editingConditionSkill == 1) cfg.SecondarySkillPreCon = 1;
                    else cfg.TertiarySkillPreCon = 1;
                    break;
                case HitSubConSub2:
                    if (_editingConditionSkill == 1) cfg.SecondarySkillSubCon = 0;
                    else cfg.TertiarySkillSubCon = 0;
                    break;
                case HitSubConSub3:
                    if (_editingConditionSkill == 1) cfg.SecondarySkillSubCon = 1;
                    else cfg.TertiarySkillSubCon = 1;
                    break;
                case HitSubConSub4:
                    if (_editingConditionSkill == 1) cfg.SecondarySkillSubCon = 2;
                    else cfg.TertiarySkillSubCon = 2;
                    break;
                case HitSubConSub5:
                    if (_editingConditionSkill == 1) cfg.SecondarySkillSubCon = 3;
                    else cfg.TertiarySkillSubCon = 3;
                    break;

                case HitReset:
                    ResetToDefaults(cfg);
                    _selectedExtraIndex = -1;
                    break;

                case HitSave:
                    _controller.SaveConfig(syncToServer: true);
                    Hide();
                    return;

                case HitStartStop:
                    _controller.SaveConfig(syncToServer: true);
                    _controller.Toggle();
                    if (_controller.IsRunning) Hide();
                    return;
            }

            _controller.SaveConfig(syncToServer: false);
        }

        private static void ResetToDefaults(MuHelperConfig cfg)
        {
            var fresh = new MuHelperConfig();
            cfg.HuntingRange = fresh.HuntingRange;
            cfg.UseActiveSkill = fresh.UseActiveSkill;
            cfg.BasicSkillId = 0;
            cfg.SecondarySkillId = 0;
            cfg.SecondarySkillUseDelay = true;
            cfg.SecondarySkillUseCondition = false;
            cfg.SecondarySkillPreCon = 0;
            cfg.SecondarySkillSubCon = 0;
            cfg.SecondarySkillIntervalSec = fresh.SecondarySkillIntervalSec;
            cfg.TertiarySkillId = 0;
            cfg.TertiarySkillUseDelay = true;
            cfg.TertiarySkillUseCondition = false;
            cfg.TertiarySkillPreCon = 0;
            cfg.TertiarySkillSubCon = 0;
            cfg.TertiarySkillIntervalSec = fresh.TertiarySkillIntervalSec;
            cfg.ReturnToOrigin = fresh.ReturnToOrigin;
            cfg.MaxSecondsAway = fresh.MaxSecondsAway;
            cfg.LongRangeCounterAttack = fresh.LongRangeCounterAttack;
            cfg.FallbackBasicAttack = fresh.FallbackBasicAttack;
            cfg.UseCombo = false;
            cfg.AutoHealSkillEnabled = false;
            cfg.AutoHealSkillPercent = 60;
            cfg.PartySupportEnabled = false;
            cfg.PartyAutoHealEnabled = false;
            cfg.PartyHealPercent = 60;
            cfg.PartyBuffEnabled = false;
            cfg.AutoDrainLifeEnabled = false;
            cfg.AutoDrainLifePercent = 60;
            cfg.UseDarkRaven = false;
            cfg.DarkRavenMode = 1;
            cfg.AutoBuffEnabled = fresh.AutoBuffEnabled;
            cfg.BuffIntervalSec = fresh.BuffIntervalSec;
            cfg.BuffSkill0Id = 0;
            cfg.BuffSkill1Id = 0;
            cfg.BuffSkill2Id = 0;
            cfg.AutoHealEnabled = fresh.AutoHealEnabled;
            cfg.AutoHealPercent = fresh.AutoHealPercent;
            cfg.AutoManaEnabled = fresh.AutoManaEnabled;
            cfg.AutoManaPercent = fresh.AutoManaPercent;
            cfg.AutoRepairEnabled = fresh.AutoRepairEnabled;
            cfg.AutoPickupEnabled = fresh.AutoPickupEnabled;
            cfg.PickupRange = fresh.PickupRange;
            cfg.PickupZen = fresh.PickupZen;
            cfg.PickupJewels = fresh.PickupJewels;
            cfg.PickupExcellent = fresh.PickupExcellent;
            cfg.PickupAncient = fresh.PickupAncient;
            cfg.PickupAll = false;
            cfg.PickupExtraEnabled = false;
            cfg.ExtraPickupKeywords.Clear();
            cfg.AutoAcceptFriend = false;
            cfg.AutoAcceptGuild = false;
        }

        // --- Drawing --------------------------------------------------------------------------------

        protected override void DrawContent(SpriteBatch sb, float alpha)
        {
            DrawTopTabs(sb, alpha);

            if (_activeTab == 0) DrawHuntTab(sb, alpha);
            else if (_activeTab == 1) DrawPickupTab(sb, alpha);
            else DrawOtherTab(sb, alpha);

            DrawFooter(sb, alpha);

            if (_subPanel != SubPanel.None)
                DrawSubPanelOverlay(sb, alpha);
        }

        private void DrawTopTabs(SpriteBatch sb, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            OrnateDraw.Inset(sb, NavBarRect, alpha, 0.55f);
            UiDrawHelper.DrawBorder(sb, NavBarRect, CardBorder * (0.55f * alpha));

            string[] titles = { "狩猎", "拾取", "其他" };
            int[] hits = { HitTabHunt, HitTabPickup, HitTabOther };
            for (int i = 0; i < 3; i++)
            {
                var r = TabRect(i);
                bool active = _activeTab == i;
                bool hover = _subPanel == SubPanel.None && Clicks.IsHover(hits[i]);
                if (active)
                {
                    UiDrawHelper.DrawVerticalGradient(sb, r,
                        new Color(120, 76, 28) * (0.94f * alpha),
                        new Color(58, 34, 14) * (0.96f * alpha), 8);
                    if (pixel != null)
                        sb.Draw(pixel, new Rectangle(r.X + 8, r.Bottom - 2, r.Width - 16, 2), OrnateStyle.Gold * alpha);
                    UiDrawHelper.DrawBorder(sb, r, OrnateStyle.GoldDim * (0.85f * alpha));
                }
                else if (hover && pixel != null)
                {
                    sb.Draw(pixel, r, new Color(180, 140, 72) * (0.14f * alpha));
                }
                OrnateDraw.TextCentered(sb, titles[i], r, 12.5f,
                    active ? OrnateStyle.Gold : hover ? OrnateStyle.TextLight : OrnateStyle.TextMuted, alpha, bold: active);
            }

            // Compact live status badge
            var pill = StatusPillRect;
            bool running = _controller.IsRunning;
            if (pixel != null)
            {
                sb.Draw(pixel, pill, (running ? new Color(18, 46, 28) : new Color(18, 20, 26)) * (0.85f * alpha));
                UiDrawHelper.DrawBorder(sb, pill, (running ? StatusGreen : CardBorder) * (0.55f * alpha));
                sb.Draw(pixel, new Rectangle(pill.X + 10, pill.Center.Y - 3, 6, 6),
                    (running ? StatusGreen : OrnateStyle.TextMuted) * alpha);
            }
            OrnateDraw.TextLeft(sb, running ? "运行中" : "待机中",
                new Rectangle(pill.X + 22, pill.Y, pill.Width - 24, pill.Height),
                11.5f, running ? StatusGreen : OrnateStyle.TextMuted, alpha, bold: running);
        }

        private void DrawHuntTab(SpriteBatch sb, float alpha)
        {
            var cfg = _controller.Config;
            var family = _controller.ClassFamily;
            var pixel = GraphicsManager.Instance.Pixel;

            // Card 1: Range & Potion
            DrawCardFrame(sb, HuntTopCard, alpha);
            OrnateDraw.TextLeft(sb, $"狩猎范围：{cfg.HuntingRange}", At(PadX + 12, BodyY + 14, 150, 20), 12f, OrnateStyle.Gold, alpha, bold: true);
            DrawRangeSegments(sb, At(PadX + 12, BodyY + 51, 108, 16), cfg.HuntingRange, 8, alpha);
            DrawMiniBtn(sb, HuntRangeDecRect, "-", Clicks.IsHover(HitHuntRangeDec), cfg.HuntingRange > 1, alpha);
            DrawMiniBtn(sb, HuntRangeIncRect, "+", Clicks.IsHover(HitHuntRangeInc), cfg.HuntingRange < 8, alpha);

            if (pixel != null)
                sb.Draw(pixel, At(PadX + 186, BodyY + 10, 1, 64), CardBorder * (0.4f * alpha));

            DrawCheckBox(sb, PotionCheckRect, "药水", cfg.AutoHealEnabled, Clicks.IsHover(HitPotionCheck), alpha);
            DrawCompactBtn(sb, PotionConfigBtnRect, "设置", Clicks.IsHover(HitPotionConfigBtn), alpha);

            if (family == MuHelperClassFamily.FairyElf)
                DrawCheckBox(sb, ClassSelfSkillCheckRect, $"自动治疗 ({cfg.AutoHealSkillPercent}%)", cfg.AutoHealSkillEnabled, Clicks.IsHover(HitAutoHealSkill), alpha);
            else if (family == MuHelperClassFamily.Summoner)
                DrawCheckBox(sb, ClassSelfSkillCheckRect, $"吸血术 ({cfg.AutoDrainLifePercent}%)", cfg.AutoDrainLifeEnabled, Clicks.IsHover(HitAutoDrainLife), alpha);
            else
                OrnateDraw.TextLeft(sb, $"红 {cfg.AutoHealPercent}%  ·  蓝 {cfg.AutoManaPercent}%",
                    At(PadX + 196, BodyY + 48, 174, 20), 11.5f, OrnateStyle.TextMuted, alpha);

            // Card 2: Movement
            DrawCardFrame(sb, HuntMoveCard, alpha);
            DrawCheckBox(sb, LongRangeCheckRect, "远距离反击", cfg.LongRangeCounterAttack, Clicks.IsHover(HitLongRange), alpha);
            DrawCheckBox(sb, ReturnOriginCheckRect, "返回原点", cfg.ReturnToOrigin, Clicks.IsHover(HitReturnOrigin), alpha);
            OrnateDraw.TextRight(sb, "距离", At(PadX + 196, BodyY + 126, 40, 22), 11.5f, OrnateStyle.TextMuted, alpha);
            DrawMiniBtn(sb, MaxAwayDecRect, "-", Clicks.IsHover(HitMaxAwayDec), cfg.MaxSecondsAway > 0, alpha);
            DrawValueWell(sb, At(PadX + 266, BodyY + 126, 52, 22), cfg.MaxSecondsAway == 0 ? "空闲" : $"{cfg.MaxSecondsAway}秒", alpha);
            DrawMiniBtn(sb, MaxAwayIncRect, "+", Clicks.IsHover(HitMaxAwayInc), cfg.MaxSecondsAway < 60, alpha);

            // Card 3: Skill Slots (Exact S6 visual icon slots + Delay / Condition!)
            DrawCardFrame(sb, HuntSkillCard, alpha);
            OrnateDraw.TextLeft(sb, "基础技能", At(PadX + 10, BodyY + 170, 68, 18), 11f, OrnateStyle.GoldDim, alpha);
            DrawSkillSlot(sb, SkillSlotRect(0), cfg.BasicSkillId, Clicks.IsHover(HitSkillSlotBase + 0), "自动", alpha);

            OrnateDraw.TextLeft(sb, "激活技能 1", At(PadX + 82, BodyY + 170, 74, 18), 11f, OrnateStyle.GoldDim, alpha);
            DrawSkillSlot(sb, SkillSlotRect(1), cfg.SecondarySkillId, Clicks.IsHover(HitSkillSlotBase + 1), "空", alpha);

            DrawCheckBox(sb, SecDelayCheckRect, "延迟", cfg.SecondarySkillUseDelay, Clicks.IsHover(HitSecDelayCheck), alpha);
            DrawMiniBtn(sb, SecIntDecRect, "-", Clicks.IsHover(HitSecIntervalDec), cfg.SecondarySkillIntervalSec > 1, alpha);
            DrawValueWell(sb, At(PadX + 268, BodyY + 188, 52, 22), $"{cfg.SecondarySkillIntervalSec} 秒", alpha);
            DrawMiniBtn(sb, SecIntIncRect, "+", Clicks.IsHover(HitSecIntervalInc), cfg.SecondarySkillIntervalSec < 60, alpha);

            DrawCheckBox(sb, SecConCheckRect, "条件", cfg.SecondarySkillUseCondition, Clicks.IsHover(HitSecConCheck), alpha);
            DrawCompactBtn(sb, SecConBtnRect, "设置", Clicks.IsHover(HitSecConConfigBtn), alpha);
            if (cfg.SecondarySkillUseCondition)
                OrnateDraw.TextLeft(sb, FormatConditionSummary(cfg.SecondarySkillPreCon, cfg.SecondarySkillSubCon),
                    At(PadX + 268, BodyY + 214, 102, 20), 10.5f, OrnateStyle.GoldDim, alpha);

            if (family == MuHelperClassFamily.DarkLord)
            {
                DrawCheckBox(sb, DarkRavenCheckRect, "使用天鹰", cfg.UseDarkRaven, Clicks.IsHover(HitUseDarkRaven), alpha);
                DrawRadio(sb, DrAutoRect, "自动攻击", cfg.DarkRavenMode == 1, Clicks.IsHover(HitDrAuto), alpha);
                DrawRadio(sb, DrCeaseRect, "停止攻击", cfg.DarkRavenMode == 0, Clicks.IsHover(HitDrCease), alpha);
                DrawRadio(sb, DrTogetherRect, "与主人同攻", cfg.DarkRavenMode == 2, Clicks.IsHover(HitDrTogether), alpha);
            }
            else
            {
                if (family == MuHelperClassFamily.DarkKnight)
                {
                    DrawCheckBox(sb, ComboCheckRect, "连击", cfg.UseCombo, Clicks.IsHover(HitUseCombo), alpha);
                }
                else if (family == MuHelperClassFamily.FairyElf || family == MuHelperClassFamily.DarkWizard)
                {
                    DrawCheckBox(sb, PartyCheckRect, "组队", cfg.PartySupportEnabled, Clicks.IsHover(HitPartyCheck), alpha);
                    DrawCompactBtn(sb, PartyConfigBtnRect, "设置", Clicks.IsHover(HitPartyConfigBtn), alpha);
                }

                OrnateDraw.TextLeft(sb, "激活技能 2", At(PadX + 82, BodyY + 246, 74, 18), 11f, OrnateStyle.GoldDim, alpha);
                DrawSkillSlot(sb, SkillSlotRect(2), cfg.TertiarySkillId, Clicks.IsHover(HitSkillSlotBase + 2), "空", alpha);

                DrawCheckBox(sb, TerDelayCheckRect, "延迟", cfg.TertiarySkillUseDelay, Clicks.IsHover(HitTerDelayCheck), alpha);
                DrawMiniBtn(sb, TerIntDecRect, "-", Clicks.IsHover(HitTerIntervalDec), cfg.TertiarySkillIntervalSec > 1, alpha);
                DrawValueWell(sb, At(PadX + 268, BodyY + 264, 52, 22), $"{cfg.TertiarySkillIntervalSec} 秒", alpha);
                DrawMiniBtn(sb, TerIntIncRect, "+", Clicks.IsHover(HitTerIntervalInc), cfg.TertiarySkillIntervalSec < 60, alpha);

                DrawCheckBox(sb, TerConCheckRect, "条件", cfg.TertiarySkillUseCondition, Clicks.IsHover(HitTerConCheck), alpha);
                DrawCompactBtn(sb, TerConBtnRect, "设置", Clicks.IsHover(HitTerConConfigBtn), alpha);
                if (cfg.TertiarySkillUseCondition)
                    OrnateDraw.TextLeft(sb, FormatConditionSummary(cfg.TertiarySkillPreCon, cfg.TertiarySkillSubCon),
                        At(PadX + 268, BodyY + 290, 102, 20), 10.5f, OrnateStyle.GoldDim, alpha);
            }

            DrawCheckBox(sb, FallbackCheckRect, "普攻兜底", cfg.FallbackBasicAttack, Clicks.IsHover(HitFallbackAttack), alpha);
            OrnateDraw.TextRight(sb, "左键选技能 · 右键清空", At(PadX + 190, BodyY + 317, 178, 18), 10.5f, OrnateStyle.TextMuted, alpha);

            // Card 4: Buffs (3 visual Buff Skill Icon Slots!)
            DrawCardFrame(sb, HuntBuffCard, alpha);
            DrawCheckBox(sb, AutoBuffCheckRect, "增益持续时间", cfg.AutoBuffEnabled, Clicks.IsHover(HitAutoBuff), alpha);
            OrnateDraw.TextRight(sb, "间隔", At(PadX + 196, BodyY + 354, 40, 22), 11.5f, OrnateStyle.TextMuted, alpha);
            DrawMiniBtn(sb, BuffIntDecRect, "-", Clicks.IsHover(HitBuffIntervalDec), cfg.BuffIntervalSec > 5, alpha);
            DrawValueWell(sb, At(PadX + 266, BodyY + 354, 52, 22), $"{cfg.BuffIntervalSec}秒", alpha);
            DrawMiniBtn(sb, BuffIntIncRect, "+", Clicks.IsHover(HitBuffIntervalInc), cfg.BuffIntervalSec < 300, alpha);

            DrawSkillSlot(sb, SkillSlotRect(3), cfg.BuffSkill0Id, Clicks.IsHover(HitSkillSlotBase + 3), "增益1", alpha);
            DrawSkillSlot(sb, SkillSlotRect(4), cfg.BuffSkill1Id, Clicks.IsHover(HitSkillSlotBase + 4), "增益2", alpha);
            DrawSkillSlot(sb, SkillSlotRect(5), cfg.BuffSkill2Id, Clicks.IsHover(HitSkillSlotBase + 5), "增益3", alpha);

            // Show hovered skill name beside the 3 buff slots
            string hoverSkillTip = GetHoveredSlotName();
            OrnateDraw.TextLeft(sb, hoverSkillTip, At(PadX + 162, BodyY + 392, 206, 22), 11.5f, OrnateStyle.GoldDim, alpha);
        }

        private static string FormatConditionSummary(int preCon, int subCon)
        {
            int count = Math.Clamp(subCon, 0, 3) + 2;
            return preCon == 1 ? $"攻击我 {count}+ 怪" : $"范围内 {count}+ 怪";
        }

        private string GetHoveredSlotName()
        {
            for (int i = 0; i < 6; i++)
            {
                if (Clicks.IsHover(HitSkillSlotBase + i))
                {
                    ushort id = GetSkillForSlot(i);
                    return id == 0 ? "点击选择技能图标" : $"当前：{SkillDatabase.GetSkillName(id)}";
                }
            }
            return "最多可配置 3 个定时增益技能";
        }

        private void DrawPickupTab(SpriteBatch sb, float alpha)
        {
            var cfg = _controller.Config;
            var pixel = GraphicsManager.Instance.Pixel;

            // Card 1: Pickup Range & Repair
            DrawCardFrame(sb, PickupTopCard, alpha);
            OrnateDraw.TextLeft(sb, $"拾取范围：{cfg.PickupRange}", At(PadX + 12, BodyY + 10, 140, 20), 12f, OrnateStyle.Gold, alpha, bold: true);
            DrawRangeSegments(sb, At(PadX + 12, BodyY + 39, 132, 16), cfg.PickupRange, 8, alpha);
            DrawMiniBtn(sb, PickupRangeDecRect, "-", Clicks.IsHover(HitPickupRangeDec), cfg.PickupRange > 1, alpha);
            DrawMiniBtn(sb, PickupRangeIncRect, "+", Clicks.IsHover(HitPickupRangeInc), cfg.PickupRange < 8, alpha);

            if (pixel != null)
                sb.Draw(pixel, At(PadX + 208, BodyY + 10, 1, 48), CardBorder * (0.4f * alpha));

            DrawCheckBox(sb, RepairCheckRect, "修理物品", cfg.AutoRepairEnabled, Clicks.IsHover(HitAutoRepair), alpha);
            OrnateDraw.TextLeft(sb, "耐久低于 35% 自动自修", At(PadX + 220, BodyY + 40, 150, 18), 10.5f, OrnateStyle.TextMuted, alpha);

            // Card 2: Pickup Filter Checkboxes (Exact S6 hierarchy)
            DrawCardFrame(sb, PickupFilterCard, alpha);
            DrawCheckBox(sb, PickAllCheckRect, "拾取附近所有物品", cfg.PickupAll, Clicks.IsHover(HitPickupAll), alpha);
            DrawCheckBox(sb, PickSelCheckRect, "拾取选定物品", !cfg.PickupAll && cfg.AutoPickupEnabled, Clicks.IsHover(HitPickupSelected), alpha);
            DrawCheckBox(sb, PickJewelCheckRect, "宝石 / 玛雅", cfg.PickupJewels, Clicks.IsHover(HitPickupJewels), alpha);
            DrawCheckBox(sb, PickAncientCheckRect, "套装物品", cfg.PickupAncient, Clicks.IsHover(HitPickupAncient), alpha);
            DrawCheckBox(sb, PickZenCheckRect, "金币", cfg.PickupZen, Clicks.IsHover(HitPickupZen), alpha);
            DrawCheckBox(sb, PickExcCheckRect, "卓越物品", cfg.PickupExcellent, Clicks.IsHover(HitPickupExcellent), alpha);
            DrawCheckBox(sb, PickExtraCheckRect, "添加其他物品", cfg.PickupExtraEnabled, Clicks.IsHover(HitPickupExtra), alpha);

            // Card 3: Extra Item Input + Quick Presets + List Box
            DrawCardFrame(sb, PickupExtraCard, alpha);
            OrnateWidgets.InputFrame(sb, ExtraInputFrameRect, _extraItemField.IsFocused, false, alpha);
            if (string.IsNullOrEmpty(_extraItemField.Value))
                OrnateDraw.TextLeft(sb, "输入物品关键词后点添加...",
                    new Rectangle(ExtraInputFrameRect.X + 8, ExtraInputFrameRect.Y, ExtraInputFrameRect.Width - 12, ExtraInputFrameRect.Height),
                    11f, OrnateStyle.TextDisabled, alpha);

            DrawCompactBtn(sb, AddExtraBtnRect, "添加", Clicks.IsHover(HitAddExtraItem), alpha);
            DrawCompactBtn(sb, DelExtraBtnRect, "删除", Clicks.IsHover(HitDeleteExtraItem), alpha);

            for (int i = 0; i < QuickPresets.Length; i++)
            {
                bool has = cfg.ExtraPickupKeywords.Contains(QuickPresets[i]);
                DrawPresetChip(sb, QuickPresetRect(i), QuickPresets[i], has, Clicks.IsHover(HitPresetBase + i), alpha);
            }

            // Framed list box
            var listBox = ExtraListBoxRect;
            OrnateDraw.Inset(sb, listBox, alpha, 0.65f);
            UiDrawHelper.DrawBorder(sb, listBox, CardBorder * (0.6f * alpha));

            int show = Math.Min(6, cfg.ExtraPickupKeywords.Count);
            if (show == 0)
            {
                OrnateDraw.TextCentered(sb, "暂无自定义拾取关键词（最多 12 项）", listBox, 11.5f, OrnateStyle.TextMuted, alpha);
            }
            else
            {
                for (int i = 0; i < show; i++)
                {
                    var row = ExtraRowRect(i);
                    bool sel = i == _selectedExtraIndex;
                    bool hov = Clicks.IsHover(HitExtraRowBase + i);
                    if ((sel || hov) && pixel != null)
                        sb.Draw(pixel, row, (sel ? new Color(150, 108, 42) : new Color(180, 140, 72)) * ((sel ? 0.35f : 0.14f) * alpha));

                    OrnateDraw.TextLeft(sb, $"{i + 1}. {cfg.ExtraPickupKeywords[i]}",
                        new Rectangle(row.X + 8, row.Y, row.Width - 32, row.Height),
                        11.5f, sel ? OrnateStyle.Gold : OrnateStyle.TextLight, alpha);

                    var delR = ExtraRowDelRect(i);
                    bool delHov = Clicks.IsHover(HitExtraDelBase + i);
                    OrnateDraw.TextCentered(sb, "×", delR, 12f, delHov ? OrnateStyle.DangerHover : OrnateStyle.TextMuted, alpha, bold: delHov);
                }
            }
        }

        private void DrawOtherTab(SpriteBatch sb, float alpha)
        {
            var cfg = _controller.Config;

            DrawCardWithHeader(sb, OtherTopCard, "组队与通用设置", alpha);
            DrawCheckBox(sb, AutoFriendCheckRect, "自动接受好友组队邀请", cfg.AutoAcceptFriend, Clicks.IsHover(HitAutoFriend), alpha);
            DrawCheckBox(sb, AutoGuildCheckRect, "自动接受战盟成员组队邀请", cfg.AutoAcceptGuild, Clicks.IsHover(HitAutoGuild), alpha);
            DrawCheckBox(sb, AutoPickupMasterRect, "启用自动拾取总开关", cfg.AutoPickupEnabled, Clicks.IsHover(HitAutoPickupMaster), alpha);

            DrawCardWithHeader(sb, OtherInfoCard, "S6 游戏助手说明", alpha);
            OrnateDraw.TextLeft(sb, "• 按 Home 键或点击左上角图标随时启停挂机", At(PadX + 14, BodyY + 178, ContentW - 28, 22), 11.5f, OrnateStyle.TextMuted, alpha);
            OrnateDraw.TextLeft(sb, "• 按 Z 键快速打开 / 关闭本设置窗口", At(PadX + 14, BodyY + 202, ContentW - 28, 22), 11.5f, OrnateStyle.TextMuted, alpha);
            OrnateDraw.TextLeft(sb, "• 点击「保存设置」按 S6 257 字节协议云同步服务端", At(PadX + 14, BodyY + 226, ContentW - 28, 22), 11.5f, OrnateStyle.TextMuted, alpha);
            OrnateDraw.TextLeft(sb, "• 技能图标槽支持左键选择技能、右键快速清空", At(PadX + 14, BodyY + 250, ContentW - 28, 22), 11.5f, OrnateStyle.TextMuted, alpha);
        }

        private void DrawFooter(SpriteBatch sb, float alpha)
        {
            OrnateDraw.Divider(sb, At(PadX, WindowHeight - 64, ContentW, 10), alpha * 0.85f);

            DrawActionBtn(sb, ResetButtonRect, "重置", ButtonKind.Subtle,
                new WidgetState(Clicks.IsHover(HitReset), Clicks.IsPressed(HitReset)), alpha);
            DrawActionBtn(sb, SaveButtonRect, "保存设置", ButtonKind.Gold,
                new WidgetState(Clicks.IsHover(HitSave), Clicks.IsPressed(HitSave)), alpha);
            DrawActionBtn(sb, StartStopButtonRect, _controller.IsRunning ? "停止挂机" : "开始挂机",
                _controller.IsRunning ? ButtonKind.Crimson : ButtonKind.Emerald,
                new WidgetState(Clicks.IsHover(HitStartStop), Clicks.IsPressed(HitStartStop)), alpha);
        }

        // --- Sub-Panel Overlays (Skill Picker, Potion Settings, Party Settings) ---------------------

        private void DrawSubPanelOverlay(SpriteBatch sb, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
                sb.Draw(pixel, Frame, Color.Black * (0.62f * alpha));

            var box = SubBoxRect;
            string header = _subPanel switch
            {
                SubPanel.SkillPicker => _editingSkillSlot >= 3 ? "选择增益技能" : "选择攻击技能",
                SubPanel.PotionConfig => "药水设置",
                SubPanel.PartyConfig => "组队辅助设置",
                SubPanel.SkillCondition => _editingConditionSkill == 1 ? "激活技能 1 · 触发条件" : "激活技能 2 · 触发条件",
                _ => string.Empty
            };

            DrawCardWithHeader(sb, box, header, alpha, bgOpacity: 0.96f);

            if (_subPanel == SubPanel.SkillPicker)
                DrawSkillPickerBody(sb, box, alpha);
            else if (_subPanel == SubPanel.PotionConfig)
                DrawPotionSubBody(sb, box, alpha);
            else if (_subPanel == SubPanel.PartyConfig)
                DrawPartySubBody(sb, box, alpha);
            else if (_subPanel == SubPanel.SkillCondition)
                DrawSkillConditionSubBody(sb, box, alpha);

            DrawCompactBtn(sb, SubCloseBtnRect, _subPanel == SubPanel.SkillPicker ? "关闭" : "确定",
                Clicks.IsHover(HitSubClose), alpha);
        }

        private void DrawSkillPickerBody(SpriteBatch sb, Rectangle box, float alpha)
        {
            ushort currentId = _editingSkillSlot >= 0 ? GetSkillForSlot(_editingSkillSlot) : (ushort)0;
            int count = Math.Min(18, _pickerSkills.Count);
            string hoverName = currentId > 0 ? $"当前已选：{SkillDatabase.GetSkillName(currentId)}" : "点击下方图标分配技能";

            if (count == 0)
            {
                OrnateDraw.TextCentered(sb, "当前角色暂无可用的已学技能",
                    new Rectangle(box.X + 16, box.Y + 60, box.Width - 32, 160), 12.5f, OrnateStyle.TextMuted, alpha);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    ushort id = _pickerSkills[i];
                    var cell = SubSkillCellRect(i);
                    bool hov = Clicks.IsHover(HitSubSkillBase + i);
                    bool sel = id == currentId;
                    DrawSkillSlot(sb, cell, id, hov || sel, "", alpha, highlightGold: sel);
                    if (hov) hoverName = SkillDatabase.GetSkillName(id);
                }
            }

            OrnateDraw.TextLeft(sb, hoverName, new Rectangle(box.X + 18, box.Bottom - 68, box.Width - 36, 22), 12f, OrnateStyle.Gold, alpha, bold: true);
            DrawCompactBtn(sb, SubClearSkillRect, "清空此槽位", Clicks.IsHover(HitSubClearSkill), alpha);
        }

        private void DrawPotionSubBody(SpriteBatch sb, Rectangle box, float alpha)
        {
            var cfg = _controller.Config;
            DrawCheckBox(sb, SubHpCheckRect, $"自动喝红药 ({cfg.AutoHealPercent}%)", cfg.AutoHealEnabled, Clicks.IsHover(HitSubHpCheck), alpha);
            DrawMiniBtn(sb, SubHpDecRect, "-", Clicks.IsHover(HitSubHpDec), cfg.AutoHealPercent > 10, alpha);
            DrawLevelBar(sb, SubHpBarRect, cfg.AutoHealPercent, HpBarTop, HpBarBot, alpha);
            DrawMiniBtn(sb, SubHpIncRect, "+", Clicks.IsHover(HitSubHpInc), cfg.AutoHealPercent < 90, alpha);
            OrnateDraw.TextRight(sb, $"{cfg.AutoHealPercent}%", new Rectangle(SubHpIncRect.Right + 4, SubHpIncRect.Y, 44, 22), 12f, OrnateStyle.Gold, alpha, bold: true);

            DrawCheckBox(sb, SubMpCheckRect, $"自动喝蓝药 ({cfg.AutoManaPercent}%)", cfg.AutoManaEnabled, Clicks.IsHover(HitSubMpCheck), alpha);
            DrawMiniBtn(sb, SubMpDecRect, "-", Clicks.IsHover(HitSubMpDec), cfg.AutoManaPercent > 10, alpha);
            DrawLevelBar(sb, SubMpBarRect, cfg.AutoManaPercent, MpBarTop, MpBarBot, alpha);
            DrawMiniBtn(sb, SubMpIncRect, "+", Clicks.IsHover(HitSubMpInc), cfg.AutoManaPercent < 90, alpha);
            OrnateDraw.TextRight(sb, $"{cfg.AutoManaPercent}%", new Rectangle(SubMpIncRect.Right + 4, SubMpIncRect.Y, 44, 22), 12f, OrnateStyle.Gold, alpha, bold: true);

            var fam = _controller.ClassFamily;
            if (fam == MuHelperClassFamily.FairyElf || fam == MuHelperClassFamily.Summoner)
            {
                bool isElf = fam == MuHelperClassFamily.FairyElf;
                int pct = isElf ? cfg.AutoHealSkillPercent : cfg.AutoDrainLifePercent;
                OrnateDraw.TextLeft(sb, isElf ? $"自动治疗术触发血线 ({pct}%)" : $"吸血术触发血线 ({pct}%)",
                    new Rectangle(box.X + 16, box.Y + 162, 260, 20), 12f, OrnateStyle.TextLight, alpha);
                DrawMiniBtn(sb, SubClassDecRect, "-", Clicks.IsHover(HitSubClassPctDec), pct > 10, alpha);
                DrawLevelBar(sb, SubClassBarRect, pct, isElf ? ElfBarTop : SumBarTop, isElf ? ElfBarBot : SumBarBot, alpha);
                DrawMiniBtn(sb, SubClassIncRect, "+", Clicks.IsHover(HitSubClassPctInc), pct < 90, alpha);
                OrnateDraw.TextRight(sb, $"{pct}%", new Rectangle(SubClassIncRect.Right + 4, SubClassIncRect.Y, 44, 22), 12f, OrnateStyle.Gold, alpha, bold: true);
            }
            else
            {
                OrnateDraw.TextLeft(sb, "点击彩色进度条或 +/- 调节触发阈值（10% ~ 90%）",
                    new Rectangle(box.X + 16, box.Y + 176, box.Width - 32, 22), 11f, OrnateStyle.TextMuted, alpha);
            }
        }

        private void DrawPartySubBody(SpriteBatch sb, Rectangle box, float alpha)
        {
            var cfg = _controller.Config;
            if (_controller.ClassFamily == MuHelperClassFamily.FairyElf)
            {
                DrawCheckBox(sb, SubPartyHealCheckRect, $"队友自动治疗 ({cfg.PartyHealPercent}%)", cfg.PartyAutoHealEnabled, Clicks.IsHover(HitSubPartyHealCheck), alpha);
                DrawMiniBtn(sb, SubPartyHealDecRect, "-", Clicks.IsHover(HitSubPartyHealDec), cfg.PartyHealPercent > 10, alpha);
                DrawLevelBar(sb, SubPartyHealBarRect, cfg.PartyHealPercent, HpBarTop, HpBarBot, alpha);
                DrawMiniBtn(sb, SubPartyHealIncRect, "+", Clicks.IsHover(HitSubPartyHealInc), cfg.PartyHealPercent < 90, alpha);
                OrnateDraw.TextRight(sb, $"{cfg.PartyHealPercent}%", new Rectangle(SubPartyHealIncRect.Right + 4, SubPartyHealIncRect.Y, 44, 22), 12f, OrnateStyle.Gold, alpha, bold: true);
            }
            DrawCheckBox(sb, SubPartyBuffCheckRect, "定时为队友施放增益 Buff", cfg.PartyBuffEnabled, Clicks.IsHover(HitSubPartyBuffCheck), alpha);
            OrnateDraw.TextLeft(sb, "将下方「增益技能」槽中的辅助技能定时施放给附近队友。",
                new Rectangle(box.X + 16, box.Y + 146, box.Width - 32, 22), 11f, OrnateStyle.TextMuted, alpha);
        }

        private void DrawSkillConditionSubBody(SpriteBatch sb, Rectangle box, float alpha)
        {
            var cfg = _controller.Config;
            int preCon = _editingConditionSkill == 1 ? cfg.SecondarySkillPreCon : cfg.TertiarySkillPreCon;
            int subCon = _editingConditionSkill == 1 ? cfg.SecondarySkillSubCon : cfg.TertiarySkillSubCon;

            OrnateDraw.TextLeft(sb, "触发目标类型（前置条件）", new Rectangle(box.X + 18, box.Y + 36, box.Width - 36, 18), 11.5f, OrnateStyle.GoldDim, alpha, bold: true);
            DrawRadio(sb, SubConPreNearbyRect, "攻击范围内的怪物", preCon == 0, Clicks.IsHover(HitSubConPreNearby), alpha);
            DrawRadio(sb, SubConPreAttackingRect, "正在攻击我的怪物", preCon == 1, Clicks.IsHover(HitSubConPreAttacking), alpha);

            OrnateDraw.TextLeft(sb, "怪物数量阈值（触发条件）", new Rectangle(box.X + 18, box.Y + 112, box.Width - 36, 18), 11.5f, OrnateStyle.GoldDim, alpha, bold: true);
            DrawRadio(sb, SubConSub2Rect, "2 只以上怪物", subCon == 0, Clicks.IsHover(HitSubConSub2), alpha);
            DrawRadio(sb, SubConSub3Rect, "3 只以上怪物", subCon == 1, Clicks.IsHover(HitSubConSub3), alpha);
            DrawRadio(sb, SubConSub4Rect, "4 只以上怪物", subCon == 2, Clicks.IsHover(HitSubConSub4), alpha);
            DrawRadio(sb, SubConSub5Rect, "5 只以上怪物", subCon == 3, Clicks.IsHover(HitSubConSub5), alpha);

            OrnateDraw.TextLeft(sb, "满足条件时自动优先施放该激活技能（与延迟互斥）。",
                new Rectangle(box.X + 18, box.Bottom - 70, box.Width - 36, 20), 11f, OrnateStyle.TextMuted, alpha);
        }

        // --- Refined Primitives ---------------------------------------------------------------------

        private static void DrawCardFrame(SpriteBatch sb, Rectangle rect, float alpha, float bgOpacity = 0.72f)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;
            sb.Draw(pixel, rect, CardBg * (bgOpacity * alpha));
            UiDrawHelper.DrawBorder(sb, rect, CardBorder * (0.58f * alpha));
            sb.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), CardHighlight * (0.18f * alpha));
        }

        private static void DrawCardWithHeader(SpriteBatch sb, Rectangle rect, string title, float alpha, float bgOpacity = 0.76f)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;
            sb.Draw(pixel, rect, CardBg * (bgOpacity * alpha));
            var header = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 27);
            UiDrawHelper.DrawVerticalGradient(sb, header, CardHeaderTop * (0.95f * alpha), CardHeaderBot * (0.95f * alpha), 8);
            UiDrawHelper.DrawBorder(sb, rect, CardBorder * (0.72f * alpha));
            sb.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), CardHighlight * (0.26f * alpha));
            sb.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 28, rect.Width - 2, 1), CardBorder * (0.55f * alpha));
            sb.Draw(pixel, new Rectangle(rect.X + 10, rect.Y + 11, 4, 6), OrnateStyle.Gold * alpha);
            OrnateDraw.TextLeft(sb, title, new Rectangle(rect.X + 20, rect.Y, rect.Width - 28, 28), 12f, OrnateStyle.Gold, alpha, bold: true);
        }

        private static void DrawSkillSlot(SpriteBatch sb, Rectangle rect, ushort skillId, bool hover, string emptyLabel, float alpha, bool highlightGold = false)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                sb.Draw(pixel, rect, new Color(12, 14, 20) * (0.92f * alpha));
                Color border = highlightGold ? OrnateStyle.Gold : hover ? CardHighlight : CardBorder * 0.8f;
                UiDrawHelper.DrawBorder(sb, rect, border * alpha);
                if (highlightGold)
                    UiDrawHelper.DrawBorder(sb, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2), OrnateStyle.Gold * (0.4f * alpha));
            }

            bool drewIcon = false;
            if (skillId > 0)
            {
                var def = SkillDatabase.GetSkillDefinition(skillId);
                if (SkillIconAtlas.TryResolve(skillId, def, out var frame))
                {
                    var tex = SkillIconRenderer.GetIconTexture(frame.TexturePath, false, out Color tint);
                    if (tex != null)
                    {
                        var inner = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
                        sb.Draw(tex, inner, frame.SourceRectangle, tint * alpha);
                        drewIcon = true;
                    }
                }
            }

            if (!drewIcon && !string.IsNullOrEmpty(emptyLabel))
            {
                OrnateDraw.TextCentered(sb, emptyLabel, rect, 10.5f, OrnateStyle.TextMuted, alpha);
            }
        }

        private static void DrawCheckBox(SpriteBatch sb, Rectangle rect, string label, bool isChecked, bool hover, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            var box = new Rectangle(rect.X, rect.Center.Y - 7, 14, 14);
            if (pixel != null)
            {
                sb.Draw(pixel, box, (isChecked ? new Color(82, 54, 18) : WellBg) * alpha);
                UiDrawHelper.DrawBorder(sb, box, (isChecked || hover ? OrnateStyle.Gold : CardBorder) * alpha);
                if (isChecked)
                    sb.Draw(pixel, new Rectangle(box.X + 3, box.Y + 3, 8, 8), OrnateStyle.Gold * alpha);
            }
            OrnateDraw.TextLeft(sb, label, new Rectangle(box.Right + 7, rect.Y, rect.Width - 22, rect.Height),
                11.5f, hover ? Color.White : (isChecked ? OrnateStyle.TextLight : OrnateStyle.TextMuted), alpha);
        }

        private static void DrawRadio(SpriteBatch sb, Rectangle rect, string label, bool selected, bool hover, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            var box = new Rectangle(rect.X, rect.Center.Y - 6, 12, 12);
            if (pixel != null)
            {
                sb.Draw(pixel, box, WellBg * alpha);
                UiDrawHelper.DrawBorder(sb, box, (selected || hover ? OrnateStyle.Gold : CardBorder) * alpha);
                if (selected)
                    sb.Draw(pixel, new Rectangle(box.X + 3, box.Y + 3, 6, 6), StatusGreen * alpha);
            }
            OrnateDraw.TextLeft(sb, label, new Rectangle(box.Right + 6, rect.Y, rect.Width - 18, rect.Height),
                11.5f, selected ? OrnateStyle.Gold : hover ? Color.White : OrnateStyle.TextMuted, alpha);
        }

        private static void DrawRangeSegments(SpriteBatch sb, Rectangle rect, int value, int max, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;
            int gap = 2;
            int segW = (rect.Width - gap * (max - 1)) / max;
            for (int i = 0; i < max; i++)
            {
                var seg = new Rectangle(rect.X + i * (segW + gap), rect.Y, segW, rect.Height);
                bool lit = i < value;
                if (lit)
                    UiDrawHelper.DrawVerticalGradient(sb, seg, new Color(238, 192, 92) * alpha, new Color(148, 96, 28) * alpha, 4);
                else
                    sb.Draw(pixel, seg, new Color(22, 24, 30) * alpha);
                UiDrawHelper.DrawBorder(sb, seg, (lit ? OrnateStyle.GoldDim : CardBorder * 0.45f) * alpha);
            }
        }

        private static void DrawLevelBar(SpriteBatch sb, Rectangle rect, int percent, Color top, Color bot, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;
            sb.Draw(pixel, rect, new Color(14, 14, 18) * alpha);
            int fillW = (int)MathF.Round((rect.Width - 2) * MathHelper.Clamp(percent / 100f, 0f, 1f));
            if (fillW > 0)
            {
                var fill = new Rectangle(rect.X + 1, rect.Y + 1, fillW, rect.Height - 2);
                UiDrawHelper.DrawVerticalGradient(sb, fill, top * alpha, bot * alpha, 6);
                sb.Draw(pixel, new Rectangle(fill.X, fill.Y, fill.Width, 1), Color.White * (0.28f * alpha));
            }
            UiDrawHelper.DrawBorder(sb, rect, CardBorder * (0.85f * alpha));
        }

        private static void DrawValueWell(SpriteBatch sb, Rectangle rect, string text, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                sb.Draw(pixel, rect, WellBg * alpha);
                UiDrawHelper.DrawBorder(sb, rect, CardBorder * (0.65f * alpha));
            }
            OrnateDraw.TextCentered(sb, text, rect, 11.5f, OrnateStyle.Gold, alpha, bold: true);
        }

        private static void DrawMiniBtn(SpriteBatch sb, Rectangle rect, string glyph, bool hover, bool enabled, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                sb.Draw(pixel, rect, (hover && enabled ? WellHover : new Color(24, 24, 30)) * alpha);
                UiDrawHelper.DrawBorder(sb, rect, (hover && enabled ? OrnateStyle.Gold : CardBorder * 0.7f) * alpha);
            }
            OrnateDraw.TextCentered(sb, glyph, rect, 13f, !enabled ? OrnateStyle.TextDisabled : hover ? Color.White : OrnateStyle.Gold, alpha, bold: true);
        }

        private static void DrawCompactBtn(SpriteBatch sb, Rectangle rect, string label, bool hover, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                UiDrawHelper.DrawVerticalGradient(sb, rect,
                    (hover ? new Color(86, 62, 30) : new Color(48, 36, 22)) * alpha,
                    (hover ? new Color(46, 32, 16) : new Color(26, 20, 14)) * alpha, 6);
                UiDrawHelper.DrawBorder(sb, rect, (hover ? OrnateStyle.Gold : CardBorder) * alpha);
            }
            OrnateDraw.TextCentered(sb, label, rect, 11.5f, hover ? Color.White : OrnateStyle.Gold, alpha, bold: hover);
        }

        private static void DrawPresetChip(SpriteBatch sb, Rectangle rect, string label, bool selected, bool hover, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                if (selected)
                    UiDrawHelper.DrawVerticalGradient(sb, rect, new Color(116, 78, 28) * alpha, new Color(64, 42, 16) * alpha, 6);
                else
                    sb.Draw(pixel, rect, (hover ? WellHover : WellBg) * alpha);
                UiDrawHelper.DrawBorder(sb, rect, (selected || hover ? OrnateStyle.Gold : CardBorder * 0.65f) * alpha);
            }
            OrnateDraw.TextCentered(sb, label, rect, 11f, selected ? OrnateStyle.Gold : hover ? Color.White : OrnateStyle.TextLight, alpha, bold: selected);
        }

        private enum ButtonKind { Subtle, Gold, Emerald, Crimson }

        private static void DrawActionBtn(SpriteBatch sb, Rectangle rect, string label, ButtonKind kind, WidgetState state, float alpha)
        {
            if (state.Pressed) rect.Y += 1;
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                Color top, bot, border;
                switch (kind)
                {
                    case ButtonKind.Emerald:
                        top = state.Hover ? new Color(54, 142, 88) : new Color(38, 112, 68);
                        bot = state.Hover ? new Color(28, 84, 52) : new Color(20, 64, 40);
                        border = state.Hover ? StatusGreen : new Color(82, 174, 114);
                        break;
                    case ButtonKind.Crimson:
                        top = state.Hover ? new Color(172, 46, 40) : new Color(138, 34, 30);
                        bot = state.Hover ? new Color(108, 24, 22) : new Color(82, 18, 18);
                        border = state.Hover ? OrnateStyle.Gold : new Color(196, 98, 72);
                        break;
                    case ButtonKind.Gold:
                        top = state.Hover ? new Color(112, 80, 34) : new Color(78, 56, 26);
                        bot = state.Hover ? new Color(62, 42, 18) : new Color(42, 30, 14);
                        border = state.Hover ? OrnateStyle.Gold : OrnateStyle.GoldDim;
                        break;
                    default:
                        top = state.Hover ? new Color(56, 48, 38) : new Color(36, 32, 28);
                        bot = state.Hover ? new Color(34, 28, 22) : new Color(22, 20, 18);
                        border = state.Hover ? OrnateStyle.GoldDim : CardBorder * 0.8f;
                        break;
                }
                sb.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 2, rect.Width, rect.Height), Color.Black * (0.4f * alpha));
                UiDrawHelper.DrawVerticalGradient(sb, rect, top * alpha, bot * alpha, 8);
                UiDrawHelper.DrawBorder(sb, rect, border * alpha);
                sb.Draw(pixel, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), Color.White * (0.2f * alpha));
            }
            OrnateDraw.TextCentered(sb, label, rect, 12.5f, state.Hover ? Color.White : OrnateStyle.Gold, alpha, bold: true);
        }
    }
}
