#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Ornate;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Party
{
    /// <summary>
    /// Ornate dark-gold Party management window opened via the bottom HUD Party button or 'P' hotkey.
    /// Shows current party members (with HP bars and Leave/Kick) and nearby players for one-click invite.
    /// </summary>
    public sealed class PartyWindow : OrnateWindowControl
    {
        private const int WindowWidth = 460;
        private const int WindowHeight = 420;

        private const int HitRefresh = FirstHitId + 0;
        private const int HitCloseBtn = FirstHitId + 1;
        private const int HitMemberActionBase = FirstHitId + 10; // +0..4
        private const int HitInviteBase = FirstHitId + 30;       // +0..3

        private readonly List<PartyMemberInfo> _members = new();
        private readonly List<PlayerScopeObject> _nearbyPlayers = new();

        public PartyWindow() : base(new Point(WindowWidth, WindowHeight))
        {
            Title = "队伍";
            ShowCloseButton = true;
            ModalDim = 0.2f;
            Margin = new Margin { Bottom = 56 };
            Visible = false;
            CloseRequested += (_, _) => Hide();
        }

        public void Show()
        {
            Visible = true;
            BringToFront();
            RefreshData();
            RequestServerList();
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

        public override void Update(GameTime gameTime)
        {
            if (Visible)
                RefreshData();
            base.Update(gameTime);
        }

        private void RequestServerList()
        {
            var svc = MuGame.Network?.GetCharacterService();
            if (svc != null)
                _ = svc.RequestPartyListAsync();
        }

        private void RefreshData()
        {
            _members.Clear();
            var pm = MuGame.Network?.GetPartyManager();
            if (pm != null)
                _members.AddRange(pm.GetPartyMembers().Take(5));

            _nearbyPlayers.Clear();
            var scope = MuGame.Network?.GetScopeManager();
            var state = MuGame.Network?.GetCharacterState();
            if (scope != null && state != null)
            {
                foreach (var obj in scope.GetScopeItems(ScopeObjectType.Player))
                {
                    if (obj is PlayerScopeObject player
                        && player.Id != state.Id
                        && !string.IsNullOrWhiteSpace(player.Name)
                        && (pm == null || !pm.IsMember(player.Id)))
                    {
                        _nearbyPlayers.Add(player);
                        if (_nearbyPlayers.Count >= 4)
                            break;
                    }
                }
            }
        }

        // --- Layout helpers -------------------------------------------------------------------------

        private const int PadX = 30;
        private int ContentW => WindowWidth - PadX * 2;
        private const int YSecMembers = 54;
        private const int YMembersStart = 88;
        private const int MemberRowH = 26;

        private const int YSecNearby = 228;
        private const int YNearbyStart = 260;
        private const int NearbyRowH = 24;

        private Rectangle MemberRowRect(int i) => At(PadX, YMembersStart + i * MemberRowH, ContentW, MemberRowH - 2);
        private Rectangle MemberActionRect(int i) => At(WindowWidth - PadX - 66, YMembersStart + i * MemberRowH + 1, 64, MemberRowH - 4);

        private Rectangle NearbyRowRect(int i) => At(PadX, YNearbyStart + i * NearbyRowH, ContentW, NearbyRowH - 2);
        private Rectangle InviteBtnRect(int i) => At(WindowWidth - PadX - 78, YNearbyStart + i * NearbyRowH + 1, 76, NearbyRowH - 4);

        private Rectangle RefreshBtnRect => At(WindowWidth / 2 - 150, WindowHeight - 54, 136, 34);
        private Rectangle CloseFooterBtnRect => At(WindowWidth / 2 + 14, WindowHeight - 54, 136, 34);

        private bool IsSelf(PartyMemberInfo m)
        {
            var state = MuGame.Network?.GetCharacterState();
            if (state == null) return false;
            if (!string.IsNullOrEmpty(m.Name) && !string.IsNullOrEmpty(state.Name))
                return string.Equals(m.Name, state.Name, StringComparison.OrdinalIgnoreCase);
            return m.Id != 0 && m.Id == state.Id;
        }

        private bool IsSelfLeader() => _members.Count > 1 && IsSelf(_members[0]);

        protected override int HitTest(Point p)
        {
            if (_members.Count > 1)
            {
                bool isLeader = IsSelfLeader();
                for (int i = 0; i < _members.Count; i++)
                {
                    if ((IsSelf(_members[i]) || isLeader) && MemberActionRect(i).Contains(p))
                        return HitMemberActionBase + i;
                }
            }

            for (int i = 0; i < _nearbyPlayers.Count; i++)
            {
                if (InviteBtnRect(i).Contains(p))
                    return HitInviteBase + i;
            }

            if (RefreshBtnRect.Contains(p)) return HitRefresh;
            if (CloseFooterBtnRect.Contains(p)) return HitCloseBtn;

            return HitNone;
        }

        protected override void OnActivate(int hit)
        {
            PlayClickSound();
            var svc = MuGame.Network?.GetCharacterService();

            if (hit == HitRefresh)
            {
                RequestServerList();
                RefreshData();
                return;
            }

            if (hit == HitCloseBtn)
            {
                Hide();
                return;
            }

            if (hit >= HitMemberActionBase && hit < HitMemberActionBase + 5)
            {
                int idx = hit - HitMemberActionBase;
                if (idx >= 0 && idx < _members.Count && svc != null)
                {
                    byte memberIndex = _members[idx].Index;
                    _ = svc.SendPartyKickRequestAsync(memberIndex);
                }
                return;
            }

            if (hit >= HitInviteBase && hit < HitInviteBase + 4)
            {
                int idx = hit - HitInviteBase;
                if (idx >= 0 && idx < _nearbyPlayers.Count && svc != null)
                {
                    ushort targetId = _nearbyPlayers[idx].Id;
                    string targetName = _nearbyPlayers[idx].Name;
                    _ = svc.SendPartyInviteAsync(targetId);
                    if (Scene is Scenes.GameScene gs)
                        gs.ChatLog?.AddMessage("系统", $"已向 {targetName} 发送组队邀请。", MessageType.System);
                }
            }
        }

        protected override void DrawContent(SpriteBatch sb, float alpha)
        {
            var pixel = GraphicsManager.Instance.Pixel;

            // --- Section 1: Party Members ---
            string header = _members.Count > 1 ? $"当前队伍（{_members.Count}/5）" : "当前队伍";
            OrnateWidgets.SectionHeader(sb, At(PadX, YSecMembers, ContentW, 28), header, alpha);

            if (_members.Count <= 1)
            {
                OrnateDraw.TextCentered(
                    sb,
                    "当前尚未加入任何队伍",
                    At(PadX, YMembersStart + 26, ContentW, 26),
                    14.5f,
                    OrnateStyle.TextMuted,
                    alpha);
                OrnateDraw.TextCentered(
                    sb,
                    "可右键点击附近玩家，或在下方直接邀请组队",
                    At(PadX, YMembersStart + 56, ContentW, 24),
                    12.5f,
                    OrnateStyle.TextMuted * 0.85f,
                    alpha);
            }
            else
            {
                bool isLeader = IsSelfLeader();
                for (int i = 0; i < _members.Count; i++)
                {
                    var m = _members[i];
                    var row = MemberRowRect(i);
                    OrnateWidgets.RowBackground(sb, row, i, false, alpha);

                    string displayName = i == 0 ? $"[队长] {m.Name}" : m.Name;
                    OrnateDraw.TextLeft(sb, displayName, new Rectangle(row.X + 6, row.Y, 130, row.Height), 13f,
                        i == 0 ? OrnateStyle.Gold : OrnateStyle.TextLight, alpha, bold: i == 0);

                    string map = MapDatabase.GetMapName(m.MapId);
                    string loc = $"{map} ({m.PositionX},{m.PositionY})";
                    OrnateDraw.TextLeft(sb, loc, new Rectangle(row.X + 138, row.Y, 128, row.Height), 11.5f, OrnateStyle.TextMuted, alpha);

                    // Compact HP bar
                    if (pixel != null)
                    {
                        var bar = new Rectangle(row.X + 268, row.Center.Y - 4, 60, 8);
                        sb.Draw(pixel, bar, new Color(25, 14, 14) * alpha);
                        int fillW = (int)MathF.Round(bar.Width * Math.Clamp(m.HealthPercentage, 0f, 1f));
                        if (fillW > 0)
                            sb.Draw(pixel, new Rectangle(bar.X, bar.Y, fillW, bar.Height), new Color(195, 42, 42) * alpha);
                        UiDrawHelper.DrawBorder(sb, bar, OrnateStyle.BronzeDark * (0.7f * alpha));
                    }

                    bool self = IsSelf(m);
                    if (self || isLeader)
                    {
                        int hitId = HitMemberActionBase + i;
                        OrnateWidgets.Button(
                            sb,
                            MemberActionRect(i),
                            self ? "离开" : "踢出",
                            new WidgetState(Hover: Clicks.IsHover(hitId), Pressed: Clicks.IsPressed(hitId)),
                            alpha,
                            overhang: 0.15f);
                    }
                }
            }

            // --- Section 2: Nearby Players ---
            OrnateWidgets.SectionHeader(sb, At(PadX, YSecNearby, ContentW, 28), "附近玩家", alpha);

            if (_nearbyPlayers.Count == 0)
            {
                OrnateDraw.TextCentered(
                    sb,
                    "附近暂无可邀请的其他玩家",
                    At(PadX, YNearbyStart + 26, ContentW, 24),
                    13.5f,
                    OrnateStyle.TextMuted,
                    alpha);
            }
            else
            {
                for (int i = 0; i < _nearbyPlayers.Count; i++)
                {
                    var pl = _nearbyPlayers[i];
                    var row = NearbyRowRect(i);
                    OrnateWidgets.RowBackground(sb, row, i, false, alpha);

                    OrnateDraw.TextLeft(sb, pl.Name, new Rectangle(row.X + 8, row.Y, 190, row.Height), 13f, OrnateStyle.TextLight, alpha);
                    OrnateDraw.TextLeft(sb, $"({pl.PositionX}, {pl.PositionY})", new Rectangle(row.X + 204, row.Y, 110, row.Height), 12f, OrnateStyle.TextMuted, alpha);

                    int hitId = HitInviteBase + i;
                    OrnateWidgets.Button(
                        sb,
                        InviteBtnRect(i),
                        "邀请组队",
                        new WidgetState(Hover: Clicks.IsHover(hitId), Pressed: Clicks.IsPressed(hitId)),
                        alpha,
                        overhang: 0.15f);
                }
            }

            // --- Footer ---
            OrnateWidgets.Button(
                sb,
                RefreshBtnRect,
                "刷新",
                new WidgetState(Hover: Clicks.IsHover(HitRefresh), Pressed: Clicks.IsPressed(HitRefresh)),
                alpha);

            OrnateWidgets.Button(
                sb,
                CloseFooterBtnRect,
                "关闭",
                new WidgetState(Hover: Clicks.IsHover(HitCloseBtn), Pressed: Clicks.IsPressed(HitCloseBtn)),
                alpha);
        }
    }
}
