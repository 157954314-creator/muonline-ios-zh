using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI;
using Client.Main.Core.Client;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

// Action buttons used by the Classic mobile HUD.
namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// Touch action cluster in the lower-right corner, based on the MU Immortal layout:
    /// one large attack button and up to seven skill buttons on an arc. Artwork comes from
    /// MU Immortal (atlas_main: Attack, skill_frame, img_guang).
    /// Arc positions come from the main_mainskillui prefab anchored bottom-right.
    /// </summary>
    public sealed class TouchActionButtonsControl : UIControl
    {
        private const string AttackTexPath = "Interface/DH/mi_attack.OZP";
        private const string SkillFrameTexPath = "Interface/DH/mi_skill_frame.OZP";
        private const string SkillGlowTexPath = "Interface/DH/mi_skill_glow.OZP";
        // Skill Settings socket stack: silver ring from bg_allShow, ty_bg_skillBg disc,
        // and img_guang glow. Proportions use the 75px settings icon.
        private const string SocketRingTexPath = "Interface/SkillWin/socket_ring.OZP";
        private const string HoleBaseTexPath = "Interface/SkillWin/hole_base.OZP";
        private const string HoleGlowTexPath = "Interface/SkillWin/hole_glow.OZP";
        private const string AttackRingTexPath = "Interface/SkillWin/attack_ring.OZP";
        private const string AttackIconTexPath = "Interface/SkillWin/attack_icon.OZP";

        // Keep all seven skill slots available and use a separate circular target for reset.
        private const int ResetSize = 56;
        private const int ModernTacticalSize = 52;
        private const int ModernMainSize = 104;
        private const int ModernSkillSize = 62;
        private static readonly Vector2 ResetCenter = new(-320f, 58f);

        // ════════════════════════════════════════════════════════════════════════
        // Exact positions from the main_mainskillui.u3d prefab. They are derived from each
        // button's RectTransform and should not be changed without updating the source data.
        // The root is 350x350 at the lower-right anchor; BtnCommon is 142x132 at Zone(71,-44).
        // The global scale fits the larger prefab into our virtual UI.
        // ════════════════════════════════════════════════════════════════════════
        private const float PrefabScale = 0.80f;

        // Prefab sizes (sizeDelta) multiplied by Scale.
        private static readonly int AttackSize = (int)(142f * PrefabScale);  // BtnCommon 142x132
        private static readonly int SkillSize = (int)(77f * PrefabScale);    // BtnSkill 77x78

        // Without the full-width footer plate, the arc can be lowered for the reference mock.
        private const int BottomHudLift = 0;

        private const int SkillCount = 7;
        // Exact prefab anchored positions relative to the lower-right corner.
        private static readonly Vector2[] SkillArcRaw =
        {
            new(-46.0f,   278.0f),  // BtnSkill1
            new(-140.0f,  244.8f),  // BtnSkill2
            new(-213.8f,  174.8f),  // BtnSkill3
            new(-260.0f,  81.4f),   // BtnSkill4
            new(-134.9f,  343.6f),  // BtnSkill5
            new(-236.2f,  272.0f),  // BtnSkill6
            new(-307.0f,  170.9f),  // BtnSkill7
        };
        private static readonly Vector2[] SkillArc = new Vector2[SkillCount];
        // BtnCommon center relative to the lower-right corner.
        private static readonly Vector2 AttackCenter = new(-104.0f * PrefabScale, 131.0f * PrefabScale);

        private static void BuildFan()
        {
            for (int i = 0; i < SkillCount; i++)
                SkillArc[i] = SkillArcRaw[i] * PrefabScale;
        }

        private Texture2D _texAttack;
        private Texture2D _texSkillFrame;
        private Texture2D _texSkillGlow;
        private Texture2D _texSocketRing;
        private Texture2D _texHoleBase;
        private Texture2D _texHoleGlow;
        private Texture2D _texAttackRing;
        private Texture2D _texAttackIcon;
        private Texture2D _medallionGold;
        private Texture2D _medallionSafe;
        private Texture2D _medallionPk;
        private UiThemeId _loadedTheme = (UiThemeId)(-1);

        private Rectangle _attackRect;
        private Rectangle _resetRect;
        private Rectangle _pkModeRect;
        private readonly Rectangle[] _skillRects = new Rectangle[SkillArc.Length];
        private readonly int[] _modernArcHotbarIndices = new int[3];
        private bool _attackPressed;
        private bool _pkPressed;
        private bool _lockPressed;
        private int _pressedSkill = -1;

        /// <summary>
        /// Alpha global do cluster (0..1), animado pelo TouchMenuControl pra fazer o
        /// crossfade suave quando o menu/painel abre. Abaixo de ~0.95 o input é ignorado
        /// (barra em fade não deve capturar toques).
        /// </summary>
        public float MasterAlpha { get; set; } = 1f;

        public TouchActionButtonsControl()
        {
            Interactive = true;
            AutoViewSize = false;
            // Cobre o canto inferior direito (área toda do cluster).
            int w = 360;
            int h = 380;
            ControlSize = new Point(w, h);
            ViewSize = new Point(w, h);
            X = UiScaler.VirtualSize.X - w;
            Y = UiScaler.VirtualSize.Y - h;
        }

        public override async Task Load()
        {
            await base.Load();
            if (_loadedTheme == UiThemeManager.CurrentId)
                return;
            try { _texAttack = await UiThemeManager.LoadThemeTextureAsync(AttackTexPath); } catch { _texAttack = null; }
            try { _texSkillFrame = await UiThemeManager.LoadThemeTextureAsync(SkillFrameTexPath); } catch { _texSkillFrame = null; }
            try { _texSkillGlow = await UiThemeManager.LoadThemeTextureAsync(SkillGlowTexPath); } catch { _texSkillGlow = null; }
            try { _texSocketRing = await UiThemeManager.LoadThemeTextureAsync(SocketRingTexPath); } catch { _texSocketRing = null; }
            try { _texHoleBase = await UiThemeManager.LoadThemeTextureAsync(HoleBaseTexPath); } catch { _texHoleBase = null; }
            try { _texHoleGlow = await UiThemeManager.LoadThemeTextureAsync(HoleGlowTexPath); } catch { _texHoleGlow = null; }
            try { _texAttackRing = await UiThemeManager.LoadThemeTextureAsync(AttackRingTexPath); } catch { _texAttackRing = null; }
            try { _texAttackIcon = await UiThemeManager.LoadThemeTextureAsync(AttackIconTexPath); } catch { _texAttackIcon = null; }
            _loadedTheme = UiThemeManager.CurrentId;
        }

        private bool IsModernOverlay => UiThemeManager.CurrentId == UiThemeId.Modern;

        private SkillEntryState ResolveModernMainSkill(GameScene gs, IReadOnlyList<SkillEntryState> skills)
        {
            var selected = gs.ModernHud?.SelectedSkill;
            if (selected != null)
                return selected;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null)
                    return skills[i];
            }
            return null;
        }

        private void RebuildModernArcMapping(GameScene gs, IReadOnlyList<SkillEntryState> skills)
        {
            var mainSkill = ResolveModernMainSkill(gs, skills);
            int filled = 0;
            for (int slot = 0; slot < 6 && filled < _modernArcHotbarIndices.Length; slot++)
            {
                var candidate = slot < skills.Count ? skills[slot] : null;
                if (mainSkill != null && candidate != null && candidate.SkillId == mainSkill.SkillId && slot == 0)
                    continue;
                if (mainSkill == null && slot == 0)
                    continue;
                _modernArcHotbarIndices[filled++] = slot;
            }
            while (filled < _modernArcHotbarIndices.Length)
            {
                _modernArcHotbarIndices[filled] = filled + 1;
                filled++;
            }
        }

        private void LayoutButtons()
        {
            // Canto inferior direito da tela em coords de UI virtual, levantado acima do HUD.
            bool modernOverlay = IsModernOverlay;
            int lift = modernOverlay ? 112 : BottomHudLift;
            X = UiScaler.VirtualSize.X - ViewSize.X;
            Y = UiScaler.VirtualSize.Y - ViewSize.Y - lift;

            float cornerX = UiScaler.VirtualSize.X;
            float cornerY = UiScaler.VirtualSize.Y - lift;

            if (modernOverlay)
            {
                // Clean, zero-overlap dark-fantasy PK 5-button ergonomics:
                // 1 big Main Skill + 3 evenly spaced arc slots (2/3/4) + 2 tactical buttons (和平/PK, 锁人)
                // positioned clear of both the skill arc and the bottom-right MP orb wings.
                var ac = new Vector2(cornerX - 74f, cornerY - 96f);
                _attackRect = CenteredRect(ac, ModernMainSize / 2f);

                _skillRects[0] = CenteredRect(new Vector2(cornerX - 56f, cornerY - 198f), ModernSkillSize / 2f);
                _skillRects[1] = CenteredRect(new Vector2(cornerX - 134f, cornerY - 181f), ModernSkillSize / 2f);
                _skillRects[2] = CenteredRect(new Vector2(cornerX - 176f, cornerY - 114f), ModernSkillSize / 2f);

                // [锁人] tucked neatly left of Main Skill & below Slot 4 (clear of MP orb)
                _resetRect = CenteredRect(new Vector2(cornerX - 172f, cornerY - 34f), ModernTacticalSize / 2f);
                // [和平 / PK] cleanly outside-left of Slot 4 & above MP orb wing
                _pkModeRect = CenteredRect(new Vector2(cornerX - 244f, cornerY - 78f), ModernTacticalSize / 2f);
                return;
            }

            var classicAc = new Vector2(cornerX + AttackCenter.X, cornerY - AttackCenter.Y);
            _attackRect = CenteredRect(classicAc, AttackSize / 2f);

            var resetCenter = new Vector2(cornerX + ResetCenter.X, cornerY - ResetCenter.Y);
            _resetRect = CenteredRect(resetCenter, ResetSize / 2f);
            _pkModeRect = Rectangle.Empty;

            BuildFan();
            for (int i = 0; i < SkillArc.Length; i++)
            {
                var c = new Vector2(cornerX + SkillArc[i].X, cornerY - SkillArc[i].Y);
                _skillRects[i] = CenteredRect(c, SkillSize / 2f);
            }
        }

        private IReadOnlyList<SkillEntryState> Skills =>
            (Scene as GameScene)?.ModernHud?.HotbarSkills ?? System.Array.Empty<SkillEntryState>();

        // Modern PK overlay shows exactly 3 cohesive arc slots (Combo 2/3 + Control/Teleport 4)
        // around 1 Main Skill button so players never feel overwhelmed during PK.
        private int VisibleCount =>
            IsModernOverlay
                ? 3
                : System.Math.Min(SkillArc.Length, (Scene as GameScene)?.ModernHud?.VisibleSkillCount ?? SkillCount);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!Visible || Scene is not GameScene gs)
                return;

            LayoutButtons();
            bool modernOverlay = IsModernOverlay;
            var skills = Skills;
            if (modernOverlay)
                RebuildModernArcMapping(gs, skills);

            // Em fade (menu/painel abertos) o cluster não captura toques.
            if (MasterAlpha < 0.95f)
            {
                _attackPressed = false;
                _pkPressed = false;
                _lockPressed = false;
                _pressedSkill = -1;
                return;
            }

            var mouse = MuGame.Instance.UiMouseState;
            var prev = MuGame.Instance.PrevUiMouseState;
            bool down = mouse.LeftButton == ButtonState.Pressed;
            bool justPressed = down && prev.LeftButton == ButtonState.Released;
            var p = new Point(mouse.Position.X, mouse.Position.Y);

            _attackPressed = down && _attackRect.Contains(p);
            _pkPressed = modernOverlay && down && _pkModeRect.Contains(p);
            _lockPressed = down && _resetRect.Contains(p);
            _pressedSkill = -1;

            // Skills têm prioridade de toque sobre o ataque (ficam por cima no arco).
            int hitSkill = -1;
            int visible = VisibleCount;
            for (int i = 0; i < _skillRects.Length && i < visible; i++)
            {
                if (_skillRects[i].Contains(p)) { hitSkill = i; break; }
            }
            if (down && hitSkill >= 0) _pressedSkill = hitSkill;

            if (hitSkill >= 0 && down)
            {
                Scene.SetMouseInputConsumed();
                int hotbarIndex = modernOverlay ? _modernArcHotbarIndices[hitSkill] : hitSkill;
                SkillEntryState skill = hotbarIndex < skills.Count ? skills[hotbarIndex] : null;
                if (skill != null)
                {
                    if (modernOverlay)
                        gs.SkillController?.CastSkillFromTouch(skill, gs.JoystickWorldDirection, justPressed);
                    else if (justPressed)
                        CastSkill(gs, skill);
                }
                else if (justPressed)
                {
                    gs.ModernHud?.BeginSkillAssignment(hotbarIndex);
                }
            }
            else if (modernOverlay && justPressed && _pkModeRect.Contains(p))
            {
                Scene.SetMouseInputConsumed();
                gs.TogglePkMode();
            }
            else if (justPressed && _resetRect.Contains(p))
            {
                Scene.SetMouseInputConsumed();
                if (modernOverlay)
                    gs.CyclePvpTarget();
                else
                    gs.ModernHud?.ResetActiveSkillSet();
            }
            else if (_attackRect.Contains(p) && down)
            {
                Scene.SetMouseInputConsumed();
                if (modernOverlay)
                {
                    var mainSkill = ResolveModernMainSkill(gs, skills);
                    if (mainSkill != null)
                        gs.SkillController?.CastSkillFromTouch(mainSkill, gs.JoystickWorldDirection, justPressed);
                    else if (justPressed)
                        gs.ModernHud?.BeginSkillAssignment(0);
                }
                else if (justPressed)
                {
                    DoAttack(gs);
                }
            }
            else if (down && (_pkModeRect.Contains(p) || _resetRect.Contains(p)))
            {
                Scene.SetMouseInputConsumed();
            }
        }

        private void DoAttack(GameScene gs)
        {
            var target = FindNearestMonster(gs);
            if (target != null)
                gs.Hero.Attack(target);
        }

        private void CastSkill(GameScene gs, SkillEntryState skill)
        {
            var target = FindNearestMonster(gs);
            gs.SkillController?.CastSkillFromHotbar(skill, target);
        }

        private static MonsterObject FindNearestMonster(GameScene gs)
        {
            var world = gs.World;
            var hero = gs.Hero;
            if (world?.Monsters == null || hero == null)
                return null;
            MonsterObject nearest = null;
            float best = 12f * 12f; // ignore far-away monsters (>12 tiles) so touch attack feels responsive
            var hloc = hero.Location;
            var monsters = world.Monsters;
            for (int i = 0; i < monsters.Count; i++)
            {
                var m = monsters[i];
                if (m == null || m.IsDead || m.IsSummoned || m.World != world) continue;
                float d = Vector2.DistanceSquared(hloc, m.Location);
                if (d < best) { best = d; nearest = m; }
            }
            return nearest;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Scene is not GameScene gs) return;
            var sb = GraphicsManager.Instance?.Sprite;
            if (sb == null) return;

            LayoutButtons();
            bool modernOverlay = IsModernOverlay;
            var skills = Skills;
            if (modernOverlay)
                RebuildModernArcMapping(gs, skills);

            var pixel = GraphicsManager.Instance?.Pixel;
            var font = GraphicsManager.Instance?.SmallFont ?? GraphicsManager.Instance?.Font;

            // Botões de skill: disco escuro translúcido (fundo do slot, como na referência)
            // + o aro mi_skill_frame por cima. Ícone só quando há skill no slot da hotbar.
            if (MasterAlpha <= 0.01f)
            {
                base.Draw(gameTime);
                return;
            }

            if (modernOverlay)
                EnsureOrnateMedallions();

            // Escopo LinearClamp próprio: o batch da cena usa PointClamp e o reescalonamento
            // dos sprites do soquete/aro sai serrilhado ("falhado" — feedback do usuário);
            // bilinear = o acabamento liso do mobile. Mesmo padrão do SkillPanelControl.
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, UiScaler.SpriteTransform);

            int visibleDraw = VisibleCount;
            for (int i = 0; i < _skillRects.Length && i < visibleDraw; i++)
            {
                int hotbarIndex = modernOverlay ? _modernArcHotbarIndices[i] : i;
                var skill = (hotbarIndex < skills.Count) ? skills[hotbarIndex] : null;
                float a = ((_pressedSkill == i) ? 0.72f : 1f) * MasterAlpha;
                var rect = (_pressedSkill == i) ? Shrink(_skillRects[i], 3) : _skillRects[i];
                const float InnerScale = 0.855f;
                var inner = ScaledRect(rect, InnerScale);

                if (modernOverlay && Tex(_medallionGold))
                {
                    sb.Draw(_medallionGold, ScaledRect(rect, 1.06f), Color.White * a);
                    bool drewIconModern = skill != null && DrawSkillIcon(sb, ScaledRect(rect, 0.78f), skill, a);
                    if (font != null)
                    {
                        string slotNum = ((hotbarIndex + 1) % 10).ToString();
                        float badgeScale = drewIconModern ? 0.50f : 0.64f;
                        var bsz = font.MeasureString(slotNum) * badgeScale;
                        var bpos = drewIconModern
                            ? new Vector2(rect.X + (rect.Width - bsz.X) / 2f, rect.Bottom - bsz.Y - 4f)
                            : new Vector2(rect.X + (rect.Width - bsz.X) / 2f, rect.Y + (rect.Height - bsz.Y) / 2f);
                        Color numCol = drewIconModern ? new Color(255, 228, 145) : new Color(215, 182, 118, 210);
                        sb.DrawString(font, slotNum, bpos + Vector2.One, Color.Black * 0.9f * a, 0f, Vector2.Zero, badgeScale, SpriteEffects.None, 0f);
                        sb.DrawString(font, slotNum, bpos, numCol * a, 0f, Vector2.Zero, badgeScale, SpriteEffects.None, 0f);
                    }
                    continue;
                }

                bool drewIcon = skill != null && DrawSkillIcon(sb, rect, skill, a);
                if (drewIcon)
                {
                    if (Tex(_texHoleGlow))
                        sb.Draw(_texHoleGlow, ScaledRect(rect, 0.90f), Color.White * a);
                }
                else
                {
                    if (Tex(_texSocketRing))
                        sb.Draw(_texSocketRing, ScaledRect(rect, 88f / 75f), Color.White * a);
                    if (Tex(_texHoleBase))
                        sb.Draw(_texHoleBase, inner, Color.White * a);
                }

                if (_texSocketRing == null && Tex(_texSkillFrame))
                    sb.Draw(_texSkillFrame, rect, Color.White * (skill != null ? a : a * 0.9f));
                else if (!drewIcon && _texSocketRing == null && pixel != null)
                {
                    DrawDisc(sb, pixel, Mid(rect), rect.Width / 2f - 1f, new Color(16, 18, 24) * (0.72f * a));
                    DrawRing(sb, pixel, Mid(rect), rect.Width / 2f, 2.5f, new Color(195, 160, 95) * (0.90f * a));
                }
                else if (drewIcon && _texHoleGlow == null && pixel != null)
                {
                    DrawRing(sb, pixel, Mid(rect), rect.Width / 2f, 2.5f, new Color(225, 185, 105) * (0.92f * a));
                }
            }

            // Upper-left tactical button in Modern overlay: [和平 / PK] Mode Toggle
            if (modernOverlay && _pkModeRect != Rectangle.Empty)
            {
                bool pkOn = gs.IsPkModeEnabled;
                float a = (_pkPressed ? 0.75f : 1f) * MasterAlpha;
                var rect = _pkPressed ? Shrink(_pkModeRect, 2) : _pkModeRect;
                var medTex = pkOn ? _medallionPk : _medallionSafe;
                if (Tex(medTex))
                {
                    sb.Draw(medTex, ScaledRect(rect, 1.08f), Color.White * a);
                }
                else if (pixel != null)
                {
                    Color ringTint = pkOn ? new Color(245, 75, 75) : new Color(95, 215, 140);
                    Color fill = pkOn ? new Color(75, 16, 18) : new Color(14, 42, 26);
                    DrawDisc(sb, pixel, Mid(rect), rect.Width / 2f - 1f, fill * (0.84f * a));
                    DrawRing(sb, pixel, Mid(rect), rect.Width / 2f, 2.5f, ringTint * (0.95f * a));
                }

                if (font != null)
                {
                    const float textScale = 0.58f;
                    string text = pkOn
                        ? "PK"
                        : (Localization.Loc.IsChinese ? "和平" : "SAFE");
                    var size = font.MeasureString(text) * textScale;
                    var pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
                    Color textCol = pkOn ? new Color(255, 175, 160) : new Color(165, 250, 195);
                    sb.DrawString(font, text, pos + Vector2.One, Color.Black * 0.92f * a, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    sb.DrawString(font, text, pos, textCol * a, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                }
            }

            // Lower-left tactical button: [锁人] PvP Target Lock/Cycle in Modern overlay (or Reset in Classic)
            {
                float a = (_lockPressed ? 0.75f : 1f) * MasterAlpha;
                var rect = _lockPressed ? Shrink(_resetRect, 2) : _resetRect;
                bool hasLock = modernOverlay && gs.LockedPvpTargetId != 0;
                if (modernOverlay && Tex(hasLock ? _medallionPk : _medallionGold))
                {
                    sb.Draw(hasLock ? _medallionPk : _medallionGold, ScaledRect(rect, 1.08f), Color.White * a);
                }
                else
                {
                    Color ringTint = hasLock ? new Color(255, 85, 85) : new Color(210, 172, 100);
                    if (Tex(_texSocketRing))
                        sb.Draw(_texSocketRing, ScaledRect(rect, 88f / 75f), ringTint * a);
                    if (Tex(_texHoleBase))
                        sb.Draw(_texHoleBase, ScaledRect(rect, 0.855f), (hasLock ? new Color(150, 40, 40) : Color.White) * a);
                    else if (pixel != null)
                    {
                        Color fill = hasLock ? new Color(75, 16, 18) : new Color(18, 20, 26);
                        DrawDisc(sb, pixel, Mid(rect), rect.Width / 2f - 1f, fill * (0.84f * a));
                        DrawRing(sb, pixel, Mid(rect), rect.Width / 2f, 2.5f, ringTint * (0.95f * a));
                    }
                }

                if (font != null)
                {
                    float textScale = modernOverlay ? 0.56f : 0.56f;
                    string text = modernOverlay
                        ? (Localization.Loc.IsChinese ? "锁人" : "LOCK")
                        : "R";
                    var size = font.MeasureString(text) * textScale;
                    var pos = new Vector2(
                        rect.X + (rect.Width - size.X) / 2f,
                        rect.Y + (rect.Height - size.Y) / 2f);
                    Color textCol = hasLock ? new Color(255, 165, 150) : new Color(245, 214, 138);
                    sb.DrawString(font, text, pos + Vector2.One, Color.Black * 0.9f * a,
                        0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    sb.DrawString(font, text, pos, textCol * a,
                        0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                }
            }

            // Main Corner Button: in Modern PK overlay, renders Current Main Skill (hold-to-repeat!),
            // completely eliminating the unused Basic Attack button.
            {
                float a = (_attackPressed ? 0.72f : 1f) * MasterAlpha;
                var rect = _attackPressed ? Shrink(_attackRect, 4) : _attackRect;
                if (modernOverlay && Tex(gs.IsPkModeEnabled ? _medallionPk : _medallionGold))
                {
                    sb.Draw(gs.IsPkModeEnabled ? _medallionPk : _medallionGold, ScaledRect(rect, 1.06f), Color.White * a);
                }
                else
                {
                    if (pixel != null && !Tex(_texAttackRing))
                    {
                        Color mainRing = (modernOverlay && gs.IsPkModeEnabled) ? new Color(245, 85, 80) : new Color(220, 180, 105);
                        DrawDisc(sb, pixel, Mid(rect), rect.Width / 2f - 2f, new Color(18, 16, 20) * (0.84f * a));
                        DrawRing(sb, pixel, Mid(rect), rect.Width / 2f, 3.5f, mainRing * (0.95f * a));
                        DrawRing(sb, pixel, Mid(rect), rect.Width / 2f - 6f, 1.5f, mainRing * (0.45f * a));
                    }
                    if (Tex(_texAttackRing))
                    {
                        var mid = Mid(rect);
                        int rw = rect.Width;
                        int rh = (int)MathF.Round(rw * 133f / 142f);
                        Color ringCol = (modernOverlay && gs.IsPkModeEnabled) ? new Color(255, 190, 185) : Color.White;
                        sb.Draw(_texAttackRing, new Rectangle((int)(mid.X - rw / 2f), (int)(mid.Y - rh / 2f), rw, rh), ringCol * a);
                    }
                }

                bool drewMainSkill = false;
                if (modernOverlay)
                {
                    var mainSkill = ResolveModernMainSkill(gs, skills);
                    if (mainSkill != null)
                    {
                        var iconRect = ScaledRect(rect, 0.74f);
                        drewMainSkill = DrawSkillIcon(sb, iconRect, mainSkill, a);
                        if (drewMainSkill && Tex(_texHoleGlow))
                            sb.Draw(_texHoleGlow, ScaledRect(iconRect, 0.92f), Color.White * a);
                    }
                }

                if (!drewMainSkill && !modernOverlay)
                {
                    if (Tex(_texAttackIcon))
                    {
                        var mid2 = Mid(rect);
                        const float IconScale = 0.85f;
                        int iw = (int)MathF.Round(rect.Width * 84f / 142f * IconScale);
                        int ih = (int)MathF.Round(rect.Height * 94f / 133f * IconScale);
                        sb.Draw(_texAttackIcon, new Rectangle((int)(mid2.X - iw / 2f), (int)(mid2.Y - ih / 2f), iw, ih), Color.White * a);
                    }
                    else if (_texAttack != null && !_texAttack.IsDisposed)
                        sb.Draw(_texAttack, rect, Color.White * a);
                }

                if (modernOverlay && font != null)
                {
                    string mainTag = Localization.Loc.IsChinese ? "主" : "1";
                    float tagScale = drewMainSkill ? 0.56f : 0.82f;
                    var tsz = font.MeasureString(mainTag) * tagScale;
                    var tpos = drewMainSkill
                        ? new Vector2(rect.X + (rect.Width - tsz.X) / 2f, rect.Bottom - tsz.Y - 6f)
                        : new Vector2(rect.X + (rect.Width - tsz.X) / 2f, rect.Y + (rect.Height - tsz.Y) / 2f);
                    Color tagCol = gs.IsPkModeEnabled ? new Color(255, 195, 170) : new Color(248, 218, 140);
                    sb.DrawString(font, mainTag, tpos + Vector2.One, Color.Black * 0.92f * a, 0f, Vector2.Zero, tagScale, SpriteEffects.None, 0f);
                    sb.DrawString(font, mainTag, tpos, tagCol * a, 0f, Vector2.Zero, tagScale, SpriteEffects.None, 0f);
                }
            }

            // Restaura o batch padrão da cena (PointClamp) pros próximos controles.
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, UiScaler.SpriteTransform);

            base.Draw(gameTime);
        }

        private bool DrawSkillIcon(SpriteBatch sb, Rectangle frameRect, SkillEntryState skill, float alpha)
        {
            // Ícone pré-cortado (detecta célula VAZIA no atlas e devolve null — sem isso
            // o scanline "desenha" texels transparentes e o slot fica vazado no chão).
            bool unusable = !global::Client.Main.Core.Client.SkillManager.MeetsDemandConditions(skill.SkillId);
            var cut = global::Client.Main.Controls.UI.Game.Skills.SkillIconRenderer.GetCircleIconTexture(skill.SkillId, disabled: unusable);
            if (cut != null)
            {
                Color tint = unusable ? global::Client.Main.Controls.UI.Game.Skills.SkillIconAtlas.UnusableTint : Color.White;
                sb.Draw(cut, frameRect, tint * alpha);
                return true;
            }
            return false;
        }

        private static Vector2 Mid(Rectangle r) => new(r.X + r.Width / 2f, r.Y + r.Height / 2f);

        // Disco cheio desenhado com o pixel branco (varredura por linha).
        private static void DrawDisc(SpriteBatch sb, Texture2D pixel, Vector2 c, float r, Color color)
        {
            int ir = (int)r;
            for (int y = -ir; y <= ir; y++)
            {
                int half = (int)MathF.Sqrt(MathF.Max(0f, r * r - y * y));
                sb.Draw(pixel, new Rectangle((int)(c.X - half), (int)(c.Y + y), half * 2, 1), color);
            }
        }

        private static void DrawRing(SpriteBatch sb, Texture2D pixel, Vector2 c, float r, float thickness, Color color)
        {
            float rIn = MathF.Max(0f, r - thickness);
            int ir = (int)r;
            for (int y = -ir; y <= ir; y++)
            {
                float outer = r * r - y * y;
                if (outer <= 0f) continue;
                int xo = (int)MathF.Sqrt(outer);
                float inner = rIn * rIn - y * y;
                if (inner <= 0f)
                {
                    sb.Draw(pixel, new Rectangle((int)(c.X - xo), (int)(c.Y + y), xo * 2, 1), color);
                }
                else
                {
                    int xi = (int)MathF.Sqrt(inner);
                    int seg = xo - xi;
                    if (seg > 0)
                    {
                        sb.Draw(pixel, new Rectangle((int)(c.X - xo), (int)(c.Y + y), seg, 1), color);
                        sb.Draw(pixel, new Rectangle((int)(c.X + xi), (int)(c.Y + y), seg, 1), color);
                    }
                }
            }
        }

        private static bool Tex(Texture2D t) => t != null && !t.IsDisposed;

        // Retângulo concêntrico ao dado, escalado pelo fator (pilha de soquete).
        // Arredonda as BORDAS (não posição+largura separadas): assim o centro é
        // preservado EXATAMENTE para qualquer fator, e aro + base ficam 100%
        // concêntricos (antes, Round(pos) e Round(w) independentes deslocavam o
        // centro em sub-pixel → a skill parecia ~99% centralizada no aro).
        private static Rectangle ScaledRect(Rectangle r, float factor)
        {
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float halfW = r.Width * factor / 2f;
            float halfH = r.Height * factor / 2f;
            int left = (int)MathF.Round(cx - halfW);
            int top = (int)MathF.Round(cy - halfH);
            int right = (int)MathF.Round(cx + halfW);
            int bottom = (int)MathF.Round(cy + halfH);
            return new Rectangle(left, top, right - left, bottom - top);
        }

        private static Rectangle CenteredRect(Vector2 c, float r)
            => new((int)(c.X - r), (int)(c.Y - r), (int)(r * 2f), (int)(r * 2f));
        private static Rectangle Shrink(Rectangle r, int by)
            => new(r.X + by, r.Y + by, r.Width - by * 2, r.Height - by * 2);
        private static Rectangle Expand(Rectangle r, int by)
            => new(r.X - by, r.Y - by, r.Width + by * 2, r.Height + by * 2);

        private void EnsureOrnateMedallions()
        {
            var gd = GraphicsManager.Instance?.GraphicsDevice;
            if (gd == null || gd.IsDisposed)
                return;

            if (!Tex(_medallionGold))
            {
                _medallionGold = CreateOrnateMedallionTexture(
                    gd,
                    rimLight: new Vector3(246, 206, 126),
                    rimDark: new Vector3(112, 76, 34),
                    coreCenter: new Vector3(14, 15, 22),
                    coreEdge: new Vector3(34, 28, 20),
                    gemColor: new Vector3(220, 48, 42));
            }
            if (!Tex(_medallionSafe))
            {
                _medallionSafe = CreateOrnateMedallionTexture(
                    gd,
                    rimLight: new Vector3(120, 238, 175),
                    rimDark: new Vector3(34, 110, 68),
                    coreCenter: new Vector3(10, 26, 20),
                    coreEdge: new Vector3(20, 62, 42),
                    gemColor: new Vector3(135, 255, 190));
            }
            if (!Tex(_medallionPk))
            {
                _medallionPk = CreateOrnateMedallionTexture(
                    gd,
                    rimLight: new Vector3(255, 118, 92),
                    rimDark: new Vector3(128, 26, 22),
                    coreCenter: new Vector3(32, 10, 12),
                    coreEdge: new Vector3(88, 20, 22),
                    gemColor: new Vector3(255, 76, 56));
            }
        }

        private static Texture2D CreateOrnateMedallionTexture(
            GraphicsDevice gd,
            Vector3 rimLight,
            Vector3 rimDark,
            Vector3 coreCenter,
            Vector3 coreEdge,
            Vector3 gemColor)
        {
            const int size = 128;
            var data = new Color[size * size];
            float half = (size - 1) * 0.5f;
            float invHalf = 1f / half;

            for (int y = 0; y < size; y++)
            {
                float ny = (y - half) * invHalf;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) * invHalf;
                    float dist = MathF.Sqrt(nx * nx + ny * ny);
                    if (dist >= 1.0f)
                    {
                        data[y * size + x] = Color.Transparent;
                        continue;
                    }

                    // Directional light from top-left (-0.65, -0.76) for 3D metallic bevel
                    float light = Math.Clamp((-nx * 0.65f - ny * 0.76f) * 0.5f + 0.5f, 0f, 1f);
                    Vector3 rgb;
                    float alpha;

                    if (dist > 0.90f)
                    {
                        // Soft outer drop shadow
                        float t = 1f - (dist - 0.90f) / 0.10f;
                        rgb = Vector3.Zero;
                        alpha = t * t * 0.55f;
                    }
                    else if (dist > 0.77f)
                    {
                        // Outer metallic bevel ring with subtle radial crest
                        float ringT = (dist - 0.77f) / 0.13f;
                        float crest = MathF.Sin(ringT * MathF.PI); // brightest along middle of bevel
                        float shade = Math.Clamp(light * 0.75f + crest * 0.35f, 0f, 1f);
                        rgb = Vector3.Lerp(rimDark, rimLight, shade);
                        // Crisp dark outer hairline
                        if (dist > 0.885f)
                            rgb *= 0.45f;
                        alpha = 0.98f;
                    }
                    else if (dist > 0.735f)
                    {
                        // Recessed dark-bronze inner groove
                        rgb = rimDark * 0.42f;
                        alpha = 0.96f;
                    }
                    else if (dist > 0.715f)
                    {
                        // Thin bright inner bezel highlight
                        rgb = Vector3.Lerp(rimDark, rimLight, 0.7f * light + 0.25f);
                        alpha = 0.95f;
                    }
                    else
                    {
                        // Obsidian-glass inner disc with radial glow + top specular crescent
                        float coreT = dist / 0.715f;
                        rgb = Vector3.Lerp(coreCenter, coreEdge, coreT * coreT);
                        // Top-half curved glass highlight
                        if (ny < 0.05f)
                        {
                            float spec = Math.Clamp((0.05f - ny) * 1.4f, 0f, 1f) * (1f - coreT * 0.65f);
                            rgb += new Vector3(42f, 44f, 52f) * (spec * spec * 0.55f);
                        }
                        alpha = 0.86f;
                    }

                    // 4 ornate cardinal gem rivets at N/S/E/W along the metallic bezel (r = 0.835)
                    const float gemR = 0.835f;
                    const float gemSize = 0.048f;
                    float dN = MathF.Sqrt(nx * nx + (ny + gemR) * (ny + gemR));
                    float dS = MathF.Sqrt(nx * nx + (ny - gemR) * (ny - gemR));
                    float dW = MathF.Sqrt((nx + gemR) * (nx + gemR) + ny * ny);
                    float dE = MathF.Sqrt((nx - gemR) * (nx - gemR) + ny * ny);
                    float dGem = MathF.Min(MathF.Min(dN, dS), MathF.Min(dW, dE));
                    if (dGem < gemSize)
                    {
                        float gt = 1f - dGem / gemSize;
                        rgb = Vector3.Lerp(rimDark * 0.5f, gemColor + new Vector3(60f, 60f, 60f) * gt, Math.Clamp(gt * 1.6f, 0f, 1f));
                        alpha = 1f;
                    }

                    // Anti-alias outer rim transition smoothly
                    if (dist > 0.96f)
                        alpha *= Math.Clamp((1.0f - dist) / 0.04f, 0f, 1f);

                    byte rByte = (byte)Math.Clamp(rgb.X * alpha, 0f, 255f);
                    byte gByte = (byte)Math.Clamp(rgb.Y * alpha, 0f, 255f);
                    byte bByte = (byte)Math.Clamp(rgb.Z * alpha, 0f, 255f);
                    byte aByte = (byte)Math.Clamp(alpha * 255f, 0f, 255f);
                    data[y * size + x] = new Color(rByte, gByte, bByte, aByte);
                }
            }

            var tex = new Texture2D(gd, size, size);
            tex.SetData(data);
            return tex;
        }

        public override void Dispose()
        {
            _medallionGold?.Dispose();
            _medallionGold = null;
            _medallionSafe?.Dispose();
            _medallionSafe = null;
            _medallionPk?.Dispose();
            _medallionPk = null;
            base.Dispose();
        }
    }
}
