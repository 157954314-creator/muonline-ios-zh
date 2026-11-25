#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.MuHelper
{
    /// <summary>
    /// Modular MU Helper (游戏助手) auto-hunting, auto-potion, and auto-pickup controller.
    /// Reuses existing GameScene combat, movement, potion, and pickup APIs without modifying core logic.
    /// </summary>
    public sealed class MuHelperController
    {
        private readonly GameScene _scene;
        private readonly CharacterState _state;

        public MuHelperConfig Config { get; private set; }
        public bool IsRunning { get; private set; }
        public Vector2 OriginTile { get; private set; }
        public ushort OriginMapId { get; private set; }
        public string StatusSummary { get; private set; } = "未开启";

        private double _lastTickTime;
        private double _lastHealTime;
        private double _lastManaTime;
        private double _lastAttackTime;
        private double _lastPickupTime;
        private double _lastReturnTime;
        private readonly Dictionary<ushort, int> _pickupAttempts = new();

        private static readonly int[] HpPotionPriority = { 3, 2, 1, 0 }; // 大红 > 中红 > 小红 > 苹果
        private static readonly int[] MpPotionPriority = { 6, 5, 4 };    // 大蓝 > 中蓝 > 小蓝

        public event EventHandler? StateChanged;

        public MuHelperController(GameScene scene, CharacterState state)
        {
            _scene = scene;
            _state = state;
            Config = MuHelperConfig.Load(_state.Name);
        }

        public void ReloadConfig()
        {
            Config = MuHelperConfig.Load(_state.Name);
        }

        public void SaveConfig()
        {
            Config.Save(_state.Name);
        }

        public void Toggle()
        {
            if (IsRunning) Stop("已手动停止游戏助手。");
            else Start();
        }

        public void Start()
        {
            var hero = _scene.Hero;
            if (hero == null || hero.IsDead)
            {
                Notify("当前状态无法启动游戏助手。");
                return;
            }

            OriginTile = hero.Location;
            OriginMapId = _state.MapId;
            _pickupAttempts.Clear();
            IsRunning = true;
            StatusSummary = "挂机中";
            Notify($"游戏助手已开启（原点：{(int)OriginTile.X}, {(int)OriginTile.Y}，范围：{Config.HuntingRange}）。");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop(string? reason = null)
        {
            if (!IsRunning) return;
            IsRunning = false;
            StatusSummary = "未开启";
            if (!string.IsNullOrEmpty(reason))
                Notify(reason);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Update(GameTime gameTime)
        {
            if (!IsRunning) return;

            var hero = _scene.Hero;
            if (hero == null || _scene.World == null)
            {
                Stop();
                return;
            }

            if (hero.IsDead)
            {
                Stop("角色已阵亡，游戏助手自动停止。");
                return;
            }

            if (_state.MapId != OriginMapId)
            {
                Stop("已切换地图，游戏助手自动停止。");
                return;
            }

            double now = gameTime.TotalGameTime.TotalSeconds;
            if (now - _lastTickTime < 0.18)
                return;
            _lastTickTime = now;

            // 1. Auto Potion (HP / MP)
            TryAutoPotions(now);

            // 2. Auto Pickup
            if (Config.AutoPickupEnabled && TryAutoPickup(now, hero))
                return;

            // 3. Auto Hunt nearest monster in range
            var target = FindNearestMonster(hero);
            if (target != null)
            {
                StatusSummary = $"攻击：{target.DisplayName}";
                TryAttackMonster(now, hero, target);
                return;
            }

            // 4. Return to origin when idle
            if (Config.ReturnToOrigin)
            {
                float distToOrigin = Vector2.Distance(hero.Location, OriginTile);
                if (distToOrigin > 1.8f && !hero.IsMoving && now - _lastReturnTime >= 1.2)
                {
                    _lastReturnTime = now;
                    StatusSummary = "返回原点";
                    hero.MoveTo(OriginTile, sendToServer: true, usePathfinding: true);
                    return;
                }
            }

            StatusSummary = "寻敌中...";
        }

        private void TryAutoPotions(double now)
        {
            if (Config.AutoHealEnabled && _state.MaximumHealth > 0 && now - _lastHealTime >= 1.0)
            {
                float hpPct = (_state.CurrentHealth * 100f) / _state.MaximumHealth;
                if (hpPct <= Config.AutoHealPercent)
                {
                    if (TryConsumeBestPotion(HpPotionPriority))
                        _lastHealTime = now;
                }
            }

            if (Config.AutoManaEnabled && _state.MaximumMana > 0 && now - _lastManaTime >= 1.0)
            {
                float mpPct = (_state.CurrentMana * 100f) / _state.MaximumMana;
                if (mpPct <= Config.AutoManaPercent)
                {
                    if (TryConsumeBestPotion(MpPotionPriority))
                        _lastManaTime = now;
                }
            }
        }

        private bool TryConsumeBestPotion(int[] idPriority)
        {
            var svc = MuGame.Network?.GetCharacterService();
            if (svc == null) return false;

            var items = _state.GetInventoryItems();
            for (int p = 0; p < idPriority.Length; p++)
            {
                int targetId = idPriority[p];
                foreach (var kv in items)
                {
                    if (kv.Key < 12) continue; // skip equipment slots
                    var def = ItemDatabase.GetItemDefinition(kv.Value);
                    if (def != null && def.Group == 14 && def.Id == targetId)
                    {
                        byte slot = kv.Key;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await svc.SendConsumeItemRequestAsync(slot);
                                await Task.Delay(250);
                                MuGame.ScheduleOnMainThread(() => _state.RaiseInventoryChanged());
                            }
                            catch { }
                        });
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryAutoPickup(double now, Objects.Player.PlayerObject hero)
        {
            if (now - _lastPickupTime < 0.65)
                return false;

            var scopeManager = MuGame.Network?.GetScopeManager();
            var svc = MuGame.Network?.GetCharacterService();
            var network = MuGame.Network;
            if (scopeManager == null || svc == null || network == null)
                return false;

            ScopeObject? best = null;
            float bestDist = float.MaxValue;

            foreach (var obj in scopeManager.GetScopeItems(ScopeObjectType.Item).Concat(scopeManager.GetScopeItems(ScopeObjectType.Money)))
            {
                if (_pickupAttempts.TryGetValue(obj.RawId, out int tries) && tries >= 3)
                    continue;

                if (!ShouldPickup(obj))
                    continue;

                var pos = new Vector2(obj.PositionX, obj.PositionY);
                float distFromAnchor = Vector2.Distance(pos, Config.ReturnToOrigin ? OriginTile : hero.Location);
                if (distFromAnchor > Config.PickupRange)
                    continue;

                float distFromHero = Vector2.Distance(pos, hero.Location);
                if (distFromHero < bestDist)
                {
                    bestDist = distFromHero;
                    best = obj;
                }
            }

            if (best == null)
                return false;

            var itemPos = new Vector2(best.PositionX, best.PositionY);
            if (bestDist > 2.2f)
            {
                if (!hero.IsMoving)
                {
                    StatusSummary = "前往拾取";
                    hero.MoveTo(itemPos, sendToServer: true, usePathfinding: true);
                }
                return true;
            }

            _lastPickupTime = now;
            _pickupAttempts[best.RawId] = _pickupAttempts.GetValueOrDefault(best.RawId) + 1;
            StatusSummary = "拾取物品";
            _state.SetPendingPickupRawId(best.RawId);
            ushort rawId = best.RawId;
            _ = Task.Run(async () =>
            {
                try
                {
                    await svc.SendPickupItemRequestAsync(rawId, network.TargetVersion);
                }
                catch { }
            });
            return true;
        }

        private bool ShouldPickup(ScopeObject obj)
        {
            if (obj is MoneyScopeObject)
                return Config.PickupZen || Config.PickupAll;

            if (obj is ItemScopeObject itemObj)
            {
                if (Config.PickupAll)
                    return true;

                var data = itemObj.ItemData.ToArray();
                var def = ItemDatabase.GetItemDefinition(data);
                if (def != null && Config.PickupJewels && IsJewel(def.Group, def.Id))
                    return true;

                var details = ItemDatabase.ParseItemDetails(data);
                if (Config.PickupExcellent && (details.IsExcellent || details.IsAncient))
                    return true;
            }

            return false;
        }

        private static bool IsJewel(int group, int id)
        {
            // Chaos (12/15), Bless (14/13), Soul (14/14), Life (14/16), Creation (14/22), Guardian (14/31), Harmony (14/41/42)
            if (group == 12 && id == 15) return true;
            if (group == 14 && (id == 13 || id == 14 || id == 16 || id == 22 || id == 31 || id == 41 || id == 42 || id == 43 || id == 44))
                return true;
            return false;
        }

        private MonsterObject? FindNearestMonster(Objects.Player.PlayerObject hero)
        {
            var world = _scene.World;
            if (world == null) return null;

            Vector2 anchor = Config.ReturnToOrigin ? OriginTile : hero.Location;
            MonsterObject? best = null;
            float bestDist = float.MaxValue;

            var objects = world.Objects.GetSnapshot();
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is not MonsterObject mob)
                    continue;
                if (mob.IsDead || mob.Hidden || mob.IsSummoned)
                    continue;

                float distFromAnchor = Vector2.Distance(mob.Location, anchor);
                if (distFromAnchor > Config.HuntingRange)
                    continue;

                float distFromHero = Vector2.Distance(mob.Location, hero.Location);
                if (distFromHero < bestDist)
                {
                    bestDist = distFromHero;
                    best = mob;
                }
            }

            return best;
        }

        private void TryAttackMonster(double now, Objects.Player.PlayerObject hero, MonsterObject target)
        {
            if (now - _lastAttackTime < 0.35)
                return;

            float dist = Vector2.Distance(hero.Location, target.Location);

            // Try active skill first if configured
            if (Config.UseActiveSkill && _scene.SkillController != null)
            {
                var skill = GetPreferredActiveSkill();
                if (skill != null)
                {
                    var skillDef = SkillDatabase.GetSkillDefinition(skill.SkillId);
                    float skillRange = skillDef != null && skillDef.Distance > 0 ? skillDef.Distance : 5f;
                    bool enoughMana = skillDef == null || _state.CurrentMana >= skillDef.ManaCost;

                    if (enoughMana)
                    {
                        if (dist > skillRange)
                        {
                            if (!hero.IsMoving)
                                hero.MoveTo(target.Location, sendToServer: true, usePathfinding: true);
                            return;
                        }

                        if (_scene.SkillController.CastSkillFromHotbar(skill, target))
                        {
                            _lastAttackTime = now;
                            return;
                        }
                    }
                }
            }

            // Fallback: melee basic attack
            if (dist > 2.2f)
            {
                if (!hero.IsMoving)
                    hero.MoveTo(target.Location, sendToServer: true, usePathfinding: true);
                return;
            }

            _lastAttackTime = now;
            hero.Attack(target);
        }

        private SkillEntryState? GetPreferredActiveSkill()
        {
            // Prefer the currently selected hotbar skill if learned; otherwise first non-self learned skill
            var hud = _scene.ModernHud;
            if (hud?.SelectedSkill != null)
                return hud.SelectedSkill;

            var skills = _state.GetSkills();
            if (skills == null)
                return null;

            foreach (var s in skills)
            {
                if (s != null && !SkillDatabase.IsSelfSkill(s.SkillId))
                    return s;
            }
            return null;
        }

        private void Notify(string message)
        {
            _scene.ChatLog?.AddMessage("System", message, MessageType.System);
        }
    }
}
