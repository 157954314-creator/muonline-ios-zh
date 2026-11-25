#nullable enable
using System;
using System.Collections.Generic;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Ornate;
using Client.Main.Core.Models;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Command
{
    /// <summary>
    /// Classic MU Online 'D' key social command window (社交指令).
    /// Supports both classic MU workflow (pick command -> right-click target player in world)
    /// and direct 1-click execution on a nearby player chosen in the target selector.
    /// </summary>
    public sealed class CommandWindow : OrnateWindowControl
    {
        public enum SocialCommand
        {
            None = 0,
            Trade,
            Buy,
            Party,
            Whisper,
            GuildJoin,
            AddFriend,
            Duel,
            Follow
        }

        private const int WindowWidth = 340;
        private const int WindowHeight = 408;

        private const int HitCmdBase = FirstHitId + 0;      // +0..7
        private const int HitTargetPrev = FirstHitId + 20;
        private const int HitTargetNext = FirstHitId + 21;
        private const int HitStopFollow = FirstHitId + 22;

        private static readonly (SocialCommand Cmd, string Label)[] Commands =
        {
            (SocialCommand.Trade, "交易"),
            (SocialCommand.Buy, "购买商店"),
            (SocialCommand.Party, "邀请组队"),
            (SocialCommand.Whisper, "私聊"),
            (SocialCommand.GuildJoin, "加入战盟"),
            (SocialCommand.AddFriend, "加为好友"),
            (SocialCommand.Duel, "申请决斗"),
            (SocialCommand.Follow, "跟随玩家"),
        };

        private readonly GameScene _scene;
        private readonly Action<string> _whisperRequested;
        private readonly Action<ushort, string> _duelRequested;

        private readonly List<PlayerScopeObject> _nearbyPlayers = new();
        private int _selectedTargetIndex = -1; // -1 = manual right-click target in world

        private SocialCommand _armedCommand = SocialCommand.None;
        private ushort _followTargetId;
        private string _followTargetName = string.Empty;
        private double _lastFollowMoveTime;

        public bool HasArmedCommand => _armedCommand != SocialCommand.None;

        public CommandWindow(
            GameScene scene,
            Action<string> whisperRequested,
            Action<ushort, string> duelRequested) : base(new Point(WindowWidth, WindowHeight))
        {
            _scene = scene;
            _whisperRequested = whisperRequested;
            _duelRequested = duelRequested;

            Title = "社交指令";
            ShowCloseButton = true;
            ModalDim = 0.15f;
            Margin = new Margin { Bottom = 56 };
            Visible = false;
            CloseRequested += (_, _) => Hide();
        }

        public void Show()
        {
            Visible = true;
            BringToFront();
            RefreshNearbyPlayers();
            SoundController.Instance?.PlayBuffer("Sound/iCreateWindow.wav");
        }

        public void Hide()
        {
            Visible = false;
            _armedCommand = SocialCommand.None;
        }

        public void Toggle()
        {
            if (Visible) Hide();
            else Show();
        }

        public void StopFollow()
        {
            if (_followTargetId != 0)
            {
                _followTargetId = 0;
                _followTargetName = string.Empty;
                _scene.ChatLog?.AddMessage("系统", "已停止跟随。", MessageType.System);
            }
        }

        public override void Update(GameTime gameTime)
        {
            UpdateFollow(gameTime);

            if (Visible)
                RefreshNearbyPlayers();

            // Classic MU flow: if a command is armed, right-clicking a player executes it immediately
            if (_armedCommand != SocialCommand.None && !_scene.IsMouseInputConsumedThisFrame)
            {
                var uiMouse = MuGame.Instance.UiMouseState;
                var prevUiMouse = MuGame.Instance.PrevUiMouseState;
                bool rightReleased = prevUiMouse.RightButton == ButtonState.Pressed
                                     && uiMouse.RightButton == ButtonState.Released;
                if (rightReleased && !IsMouseOver)
                {
                    if (_scene.MouseHoverObject is PlayerObject hovered && hovered != _scene.Hero)
                    {
                        var cmd = _armedCommand;
                        _armedCommand = SocialCommand.None;
                        ExecuteCommand(cmd, hovered.NetworkId, hovered.Name);
                        _scene.SetMouseInputConsumed();
                    }
                    else
                    {
                        _armedCommand = SocialCommand.None;
                    }
                }
            }

            base.Update(gameTime);
        }

        private void UpdateFollow(GameTime gameTime)
        {
            if (_followTargetId == 0)
                return;

            var hero = _scene.Hero;
            var scope = MuGame.Network?.GetScopeManager();
            if (hero == null || hero.IsDead || scope == null)
            {
                _followTargetId = 0;
                return;
            }

            double now = gameTime.TotalGameTime.TotalSeconds;
            if (now - _lastFollowMoveTime < 0.45)
                return;

            PlayerScopeObject? target = null;
            foreach (var obj in scope.GetScopeItems(ScopeObjectType.Player))
            {
                if (obj is PlayerScopeObject p && p.Id == _followTargetId)
                {
                    target = p;
                    break;
                }
            }

            if (target == null)
            {
                StopFollow();
                return;
            }

            var targetPos = new Vector2(target.PositionX, target.PositionY);
            if (Vector2.Distance(hero.Location, targetPos) > 2.4f && !hero.IsMoving)
            {
                _lastFollowMoveTime = now;
                hero.MoveTo(targetPos, sendToServer: true, usePathfinding: true);
            }
        }

        private void RefreshNearbyPlayers()
        {
            ushort prevId = (_selectedTargetIndex >= 0 && _selectedTargetIndex < _nearbyPlayers.Count)
                ? _nearbyPlayers[_selectedTargetIndex].Id
                : (ushort)0;

            _nearbyPlayers.Clear();
            var scope = MuGame.Network?.GetScopeManager();
            var state = MuGame.Network?.GetCharacterState();
            if (scope != null && state != null)
            {
                foreach (var obj in scope.GetScopeItems(ScopeObjectType.Player))
                {
                    if (obj is PlayerScopeObject p && p.Id != state.Id && !string.IsNullOrWhiteSpace(p.Name))
                        _nearbyPlayers.Add(p);
                }
            }

            if (prevId != 0)
            {
                int found = _nearbyPlayers.FindIndex(p => p.Id == prevId);
                _selectedTargetIndex = found;
            }
            else if (_selectedTargetIndex >= _nearbyPlayers.Count)
            {
                _selectedTargetIndex = -1;
            }
        }

        private void ExecuteCommand(SocialCommand cmd, ushort targetId, string targetName)
        {
            var svc = MuGame.Network?.GetCharacterService();
            if (svc == null)
                return;

            switch (cmd)
            {
                case SocialCommand.Trade:
                    _ = svc.SendTradeRequestAsync(targetId);
                    _scene.ChatLog?.AddMessage("系统", $"已向 {targetName} 发送交易请求。", MessageType.System);
                    break;
                case SocialCommand.Buy:
                    _scene.PlayerShop?.ExpectShop(targetId);
                    _ = svc.SendPlayerStoreListRequestAsync(targetId, targetName);
                    break;
                case SocialCommand.Party:
                    _ = svc.SendPartyInviteAsync(targetId);
                    _scene.ChatLog?.AddMessage("系统", $"已向 {targetName} 发送组队邀请。", MessageType.System);
                    break;
                case SocialCommand.Whisper:
                    _whisperRequested?.Invoke(targetName);
                    Hide();
                    break;
                case SocialCommand.GuildJoin:
                    _ = svc.SendGuildJoinRequestAsync(targetId);
                    _scene.ChatLog?.AddMessage("系统", $"已向 {targetName} 申请加入战盟。", MessageType.System);
                    break;
                case SocialCommand.AddFriend:
                    if (!string.IsNullOrEmpty(targetName))
                    {
                        _ = svc.SendFriendAddRequestAsync(targetName);
                        _scene.ChatLog?.AddMessage("系统", $"已向 {targetName} 发送好友申请。", MessageType.System);
                    }
                    break;
                case SocialCommand.Duel:
                    _duelRequested?.Invoke(targetId, targetName);
                    break;
                case SocialCommand.Follow:
                    _followTargetId = targetId;
                    _followTargetName = targetName;
                    _lastFollowMoveTime = 0;
                    _scene.ChatLog?.AddMessage("系统", $"开始跟随玩家：{targetName}。", MessageType.System);
                    break;
            }
        }

        // --- Layout ---------------------------------------------------------------------------------

        private const int PadX = 28;
        private int ContentW => WindowWidth - PadX * 2;
        private Rectangle TargetChoiceArea => At(PadX, 86, ContentW, 30);

        private Rectangle CommandBtnRect(int idx)
        {
            int col = idx % 2;
            int row = idx / 2;
            int gapX = 14;
            int gapY = 12;
            int btnW = (ContentW - gapX) / 2;
            int btnH = 34;
            return At(PadX + col * (btnW + gapX), 156 + row * (btnH + gapY), btnW, btnH);
        }

        private Rectangle StopFollowBtnRect => At(WindowWidth / 2 - 80, WindowHeight - 52, 160, 32);

        protected override int HitTest(Point p)
        {
            var box = OrnateWidgets.ChoiceBox(TargetChoiceArea);
            if (OrnateWidgets.ChoiceLeft(box).Contains(p)) return HitTargetPrev;
            if (OrnateWidgets.ChoiceRight(box).Contains(p)) return HitTargetNext;

            for (int i = 0; i < Commands.Length; i++)
            {
                if (CommandBtnRect(i).Contains(p))
                    return HitCmdBase + i;
            }

            if (_followTargetId != 0 && StopFollowBtnRect.Contains(p))
                return HitStopFollow;

            return HitNone;
        }

        protected override void OnActivate(int hit)
        {
            PlayClickSound();

            if (hit == HitTargetPrev)
            {
                if (_nearbyPlayers.Count == 0) _selectedTargetIndex = -1;
                else _selectedTargetIndex = _selectedTargetIndex <= -1 ? _nearbyPlayers.Count - 1 : _selectedTargetIndex - 1;
                return;
            }

            if (hit == HitTargetNext)
            {
                if (_nearbyPlayers.Count == 0) _selectedTargetIndex = -1;
                else _selectedTargetIndex = _selectedTargetIndex >= _nearbyPlayers.Count - 1 ? -1 : _selectedTargetIndex + 1;
                return;
            }

            if (hit == HitStopFollow)
            {
                StopFollow();
                return;
            }

            if (hit >= HitCmdBase && hit < HitCmdBase + Commands.Length)
            {
                var (cmd, label) = Commands[hit - HitCmdBase];
                if (_selectedTargetIndex >= 0 && _selectedTargetIndex < _nearbyPlayers.Count)
                {
                    var target = _nearbyPlayers[_selectedTargetIndex];
                    _armedCommand = SocialCommand.None;
                    ExecuteCommand(cmd, target.Id, target.Name);
                }
                else
                {
                    _armedCommand = _armedCommand == cmd ? SocialCommand.None : cmd;
                    if (_armedCommand != SocialCommand.None)
                        _scene.ChatLog?.AddMessage("系统", $"已选择「{label}」，请右键点击目标玩家执行。", MessageType.System);
                }
            }
        }

        protected override void DrawContent(SpriteBatch sb, float alpha)
        {
            // Target selector section
            OrnateWidgets.SectionHeader(sb, At(PadX, 54, ContentW, 28), "目标玩家", alpha);

            string targetText = (_selectedTargetIndex >= 0 && _selectedTargetIndex < _nearbyPlayers.Count)
                ? _nearbyPlayers[_selectedTargetIndex].Name
                : (_nearbyPlayers.Count > 0 ? "手动右键点选目标" : "附近暂无其他玩家");

            OrnateWidgets.Choice(
                sb,
                TargetChoiceArea,
                targetText,
                _nearbyPlayers.Count > 0,
                _nearbyPlayers.Count > 0,
                Clicks.IsHover(HitTargetPrev),
                Clicks.IsHover(HitTargetNext),
                alpha);

            // Command buttons section
            OrnateWidgets.SectionHeader(sb, At(PadX, 122, ContentW, 28), "指令列表", alpha);

            for (int i = 0; i < Commands.Length; i++)
            {
                var (cmd, label) = Commands[i];
                int hitId = HitCmdBase + i;
                bool isArmed = _armedCommand == cmd;
                var rect = CommandBtnRect(i);

                if (isArmed)
                {
                    OrnateWidgets.PrimaryButton(
                        sb,
                        rect,
                        label,
                        new WidgetState(Hover: Clicks.IsHover(hitId), Pressed: Clicks.IsPressed(hitId)),
                        alpha);
                }
                else
                {
                    OrnateWidgets.Button(
                        sb,
                        rect,
                        label,
                        new WidgetState(Hover: Clicks.IsHover(hitId), Pressed: Clicks.IsPressed(hitId)),
                        alpha,
                        overhang: 0.45f);
                }
            }

            // Status / Footer hint
            if (_followTargetId != 0)
            {
                OrnateWidgets.Button(
                    sb,
                    StopFollowBtnRect,
                    $"停止跟随 ({_followTargetName})",
                    new WidgetState(Hover: Clicks.IsHover(HitStopFollow), Pressed: Clicks.IsPressed(HitStopFollow)),
                    alpha,
                    overhang: 0.35f);
            }
            else
            {
                string hint = _armedCommand != SocialCommand.None
                    ? "请右键点击画面中的目标玩家执行指令"
                    : "选择上方目标直接执行，或点指令后右键点人";
                OrnateDraw.TextCentered(sb, hint, At(PadX, WindowHeight - 48, ContentW, 26), 12.5f,
                    _armedCommand != SocialCommand.None ? OrnateStyle.Gold : OrnateStyle.TextMuted, alpha);
            }
        }
    }
}
