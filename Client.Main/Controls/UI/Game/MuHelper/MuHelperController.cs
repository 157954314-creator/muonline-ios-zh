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
using MUnique.OpenMU.Network.Packets;
using MUnique.OpenMU.Network.Packets.ClientToServer;

namespace Client.Main.Controls.UI.Game.MuHelper
{
    /// <summary>
    /// Modular S6-grade MU Helper (游戏助手) controller with full class-specific support:
    /// - Primary + timed Secondary & Tertiary skill rotation, fallback basic attack
    /// - Dark Knight: 3-skill Combo rotation (Basic -> Secondary -> Tertiary)
    /// - Fairy Elf: Auto Heal skill (self & party) + Party Buff
    /// - Dark Wizard: Party Buff
    /// - Summoner: Auto Drain Life on low HP
    /// - Dark Lord: Dark Raven pet mode (Cease / Auto / Attack with Owner)
    /// - Timed self-buff rotation (up to 3 buff slots), auto HP/MP potions & auto self-repair
    /// - Auto pickup with Zen / Jewels / Excellent / Ancient / Extra keyword filters
    /// - Obstacle skip & max-seconds-away return to origin + 257-byte OpenMU 0xAE sync
    /// </summary>
    public sealed class MuHelperController
    {
        private readonly GameScene _scene;
        private readonly CharacterState _state;

        public MuHelperConfig Config { get; private set; }
        public MuHelperClassFamily ClassFamily => MuHelperConfig.GetClassFamily(_state.Class);
        public bool IsRunning { get; private set; }
        public Vector2 OriginTile { get; private set; }
        public ushort OriginMapId { get; private set; }
        public string StatusSummary { get; private set; } = "未开启";

        private double _lastTickTime;
        private double _lastHealTime;
        private double _lastManaTime;
        private double _lastAttackTime;
        private double _lastSecondarySkillTime;
        private double _lastTertiarySkillTime;
        private double _lastBuffCycleTime;
        private double _lastHealSkillTime;
        private double _lastDrainLifeTime;
        private double _lastPickupTime;
        private double _lastReturnTime;
        private double _lastRepairCheckTime;
        private double _awayFromOriginSince;
        private int _comboStep;
        private double _comboLastStepTime;
        private int _appliedDarkRavenMode = -1;
        private readonly Dictionary<ushort, int> _pickupAttempts = new();
        private readonly Dictionary<ushort, double> _unreachableUntil = new();
        private ushort _chaseTargetId;
        private float _chaseBestDist = float.MaxValue;
        private double _chaseProgressTime;

        private static readonly int[] HpPotionPriority = { 3, 2, 1, 0 }; // 大红 > 中红 > 小红 > 苹果
        private static readonly int[] MpPotionPriority = { 6, 5, 4 };    // 大蓝 > 中蓝 > 小蓝

        public event EventHandler? StateChanged;

        public MuHelperController(GameScene scene, CharacterState state)
        {
            _scene = scene;
            _state = state;
            Config = MuHelperConfig.Load(_state.Name);
            if (_state.PendingMuHelperWireData is { Length: >= 257 } wire)
            {
                Config.ApplyFromWireBytes(wire);
            }
        }

        public void ReloadConfig()
        {
            Config = MuHelperConfig.Load(_state.Name);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SaveConfig(bool syncToServer = true)
        {
            Config.Save(_state.Name);
            if (syncToServer)
            {
                var svc = MuGame.Network?.GetCharacterService();
                if (svc != null)
                {
                    byte[] payload = Config.ToWireBytes();
                    _ = svc.SendMuHelperSaveDataRequestAsync(payload);
                }
            }
        }

        public void ApplyServerWireData(ReadOnlySpan<byte> wireData)
        {
            if (Config.ApplyFromWireBytes(wireData))
            {
                Config.Save(_state.Name);
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
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
            _unreachableUntil.Clear();
            _chaseTargetId = 0;
            _chaseBestDist = float.MaxValue;
            _awayFromOriginSince = 0;
            _lastBuffCycleTime = -999; // trigger initial buff cast shortly after start
            _lastSecondarySkillTime = -999;
            _lastTertiarySkillTime = -999;
            _comboStep = 0;
            _comboLastStepTime = 0;
            _appliedDarkRavenMode = -1;
            IsRunning = true;
            StatusSummary = "挂机中";
            ApplyDarkRavenModeIfNeeded();
            Notify($"游戏助手已开启（{MuHelperConfig.GetClassFamilyDisplayName(ClassFamily)} | 原点：{(int)OriginTile.X}, {(int)OriginTile.Y}，范围：{Config.HuntingRange} 格）。");
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

            // 2. Auto Repair worn equipment (throttled every 15s)
            if (Config.AutoRepairEnabled && now - _lastRepairCheckTime >= 15.0)
            {
                _lastRepairCheckTime = now;
                TryAutoRepairEquipment();
            }

            // 2.5. Dark Lord: sync Dark Raven attack mode if changed
            ApplyDarkRavenModeIfNeeded();

            // 2.8. Fairy Elf: Auto Heal skill (self & party)
            if (ClassFamily == MuHelperClassFamily.FairyElf && TryElfAutoHeal(now, hero))
                return;

            // 3. Auto Self-Buffs & Party Buffs (timed rotation)
            if (Config.AutoBuffEnabled && TryAutoBuffs(now))
                return;

            // 4. Max-seconds-away leash back to origin
            if (Config.ReturnToOrigin && Config.MaxSecondsAway > 0)
            {
                float distFromOrigin = Vector2.Distance(hero.Location, OriginTile);
                if (distFromOrigin > 2.2f)
                {
                    if (_awayFromOriginSince <= 0) _awayFromOriginSince = now;
                    if (now - _awayFromOriginSince >= Config.MaxSecondsAway)
                    {
                        if (!hero.IsMoving && now - _lastReturnTime >= 1.2)
                        {
                            _lastReturnTime = now;
                            StatusSummary = "超时返回原点";
                            hero.MoveTo(OriginTile, sendToServer: true, usePathfinding: true);
                        }
                        return;
                    }
                }
                else
                {
                    _awayFromOriginSince = 0;
                }
            }

            // 5. Auto Pickup
            if (Config.AutoPickupEnabled && TryAutoPickup(now, hero))
                return;

            // 6. Auto Hunt nearest monster in range
            var target = FindNearestMonster(now, hero);
            if (target != null)
            {
                StatusSummary = $"攻击：{target.DisplayName}";
                TryAttackMonster(now, hero, target);
                return;
            }

            // 7. Return to origin when idle
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

        private void TryAutoRepairEquipment()
        {
            var svc = MuGame.Network?.GetCharacterService();
            if (svc == null || _state.InventoryZen < 500)
                return;

            var items = _state.GetInventoryItems();
            for (byte slot = 0; slot < 12; slot++)
            {
                if (slot == 8) continue; // skip helper/pet slot
                if (!items.TryGetValue(slot, out var data) || data == null || data.Length < 3)
                    continue;

                var def = ItemDatabase.GetItemDefinition(data);
                if (def == null || def.BaseDurability <= 1)
                    continue;

                var details = ItemDatabase.ParseItemDetails(data);
                int maxDur = Common.ItemUiHelper.CalculateMaxDurability(def, details, isStaff: def.Group == 5);
                if (maxDur <= 1) continue;

                int curDur = ItemDatabase.GetItemDurability(data);
                if (curDur <= maxDur * 0.35f)
                {
                    byte repairSlot = slot;
                    string itemName = def.Name ?? "装备";
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await svc.SendRepairItemRequestAsync(repairSlot, isSelfRepair: true);
                        }
                        catch { }
                    });
                    Notify($"游戏助手：已自动修理 {itemName}。");
                    return; // one slot per check cycle
                }
            }
        }

        private void ApplyDarkRavenModeIfNeeded()
        {
            if (ClassFamily != MuHelperClassFamily.DarkLord || !Config.UseDarkRaven)
                return;

            int desired = Math.Clamp(Config.DarkRavenMode, 0, 2);
            if (_appliedDarkRavenMode == desired)
                return;

            var svc = MuGame.Network?.GetCharacterService();
            if (svc == null)
                return;

            _appliedDarkRavenMode = desired;
            var mode = (PetCommandMode)desired;
            _ = svc.SendDarkRavenCommandAsync(mode, 0);
        }

        private bool TryElfAutoHeal(double now, Objects.Player.PlayerObject hero)
        {
            if (now - _lastHealSkillTime < 1.5)
                return false;

            bool checkSelf = Config.AutoHealSkillEnabled && _state.MaximumHealth > 0;
            bool checkParty = Config.PartySupportEnabled && Config.PartyAutoHealEnabled;
            if (!checkSelf && !checkParty)
                return false;

            var healSkill = FindLearnedSkill(413) ?? FindLearnedSkill(26); // Master Heal / Heal
            if (healSkill == null || !SkillManager.MeetsDemandConditions(healSkill.SkillId))
                return false;

            var def = SkillDatabase.GetSkillDefinition(healSkill.SkillId);
            if (def != null && (_state.CurrentMana < def.ManaCost || _state.CurrentAbility < def.AbilityGaugeCost))
                return false;

            var svc = MuGame.Network?.GetCharacterService();
            if (svc == null)
                return false;

            // 1. Self heal first if below threshold
            if (checkSelf)
            {
                float selfHpPct = (_state.CurrentHealth * 100f) / _state.MaximumHealth;
                if (selfHpPct <= Config.AutoHealSkillPercent)
                {
                    _lastHealSkillTime = now;
                    _ = svc.SendSkillRequestAsync(healSkill.SkillId, _state.Id);
                    StatusSummary = "治疗自身";
                    return true;
                }
            }

            // 2. Party heal: lowest-HP party member on same map within 7 tiles
            if (checkParty)
            {
                var partyMgr = MuGame.Network?.GetPartyManager();
                if (partyMgr != null)
                {
                    PartyMemberInfo? injured = null;
                    float lowestPct = Config.PartyHealPercent;
                    foreach (var m in partyMgr.GetPartyMembers())
                    {
                        if (m == null || m.Id == 0 || m.Id == _state.Id || m.MapId != _state.MapId)
                            continue;
                        if (m.MaximumHealth == 0 || m.CurrentHealth == 0)
                            continue;
                        float pct = m.HealthPercentage * 100f;
                        if (pct > lowestPct)
                            continue;
                        float dist = Vector2.Distance(hero.Location, new Vector2(m.PositionX, m.PositionY));
                        if (dist > 7.5f)
                            continue;
                        lowestPct = pct;
                        injured = m;
                    }

                    if (injured != null)
                    {
                        _lastHealSkillTime = now;
                        _ = svc.SendSkillRequestAsync(healSkill.SkillId, injured.Id);
                        StatusSummary = $"治疗队友：{injured.Name}";
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryAutoBuffs(double now)
        {
            if (_scene.SkillController == null)
                return false;

            if (now - _lastBuffCycleTime < Config.BuffIntervalSec)
                return false;

            ushort[] buffSlots = { Config.BuffSkill0Id, Config.BuffSkill1Id, Config.BuffSkill2Id };
            bool anyConfigured = false;
            bool castedAny = false;

            bool canPartyBuff = (ClassFamily == MuHelperClassFamily.FairyElf || ClassFamily == MuHelperClassFamily.DarkWizard)
                                && Config.PartySupportEnabled && Config.PartyBuffEnabled;
            var partyMembers = canPartyBuff ? MuGame.Network?.GetPartyManager()?.GetPartyMembers() : null;
            var svc = canPartyBuff ? MuGame.Network?.GetCharacterService() : null;
            var hero = _scene.Hero;

            for (int i = 0; i < buffSlots.Length; i++)
            {
                ushort skillId = buffSlots[i];
                if (skillId == 0) continue;
                anyConfigured = true;

                var skill = FindLearnedSkill(skillId);
                if (skill == null || !SkillManager.MeetsDemandConditions(skill.SkillId))
                    continue;

                var def = SkillDatabase.GetSkillDefinition(skill.SkillId);
                if (def != null && (_state.CurrentMana < def.ManaCost || _state.CurrentAbility < def.AbilityGaugeCost))
                    continue;

                if (_scene.SkillController.CastSkillFromHotbar(skill, target: null))
                {
                    castedAny = true;
                    StatusSummary = $"施放增益：{SkillDatabase.GetSkillName(skill.SkillId)}";
                }

                // Party buff cast on nearby same-map teammates (Elf / Dark Wizard)
                if (canPartyBuff && partyMembers != null && svc != null && hero != null)
                {
                    foreach (var m in partyMembers)
                    {
                        if (m == null || m.Id == 0 || m.Id == _state.Id || m.MapId != _state.MapId)
                            continue;
                        if (Vector2.Distance(hero.Location, new Vector2(m.PositionX, m.PositionY)) > 7.5f)
                            continue;
                        _ = svc.SendSkillRequestAsync(skill.SkillId, m.Id);
                        castedAny = true;
                    }
                }
            }

            if (anyConfigured)
                _lastBuffCycleTime = now;

            return castedAny;
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
                if (Config.PickupExcellent && details.IsExcellent)
                    return true;
                if (Config.PickupAncient && details.IsAncient)
                    return true;

                if (Config.PickupExtraEnabled && Config.ExtraPickupKeywords.Count > 0)
                {
                    string displayName = ItemDatabase.GetItemName(data) ?? def?.Name ?? string.Empty;
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        for (int i = 0; i < Config.ExtraPickupKeywords.Count; i++)
                        {
                            string kw = Config.ExtraPickupKeywords[i];
                            if (!string.IsNullOrWhiteSpace(kw) &&
                                displayName.Contains(kw.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsJewel(int group, int id)
        {
            // Chaos (12/15), Bless (14/13), Soul (14/14), Life (14/16), Creation (14/22), Guardian (14/31), Harmony (14/41-44)
            if (group == 12 && id == 15) return true;
            if (group == 14 && (id == 13 || id == 14 || id == 16 || id == 22 || id == 31 || id == 41 || id == 42 || id == 43 || id == 44))
                return true;
            return false;
        }

        private MonsterObject? FindNearestMonster(double now, Objects.Player.PlayerObject hero)
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
                if (_unreachableUntil.TryGetValue(mob.NetworkId, out double skipUntil) && now < skipUntil)
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

            // Track chase progress so obstacles / safe-zone walls never lock the helper on one mob forever
            if (_chaseTargetId != target.NetworkId)
            {
                _chaseTargetId = target.NetworkId;
                _chaseBestDist = dist;
                _chaseProgressTime = now;
            }
            else if (dist < _chaseBestDist - 0.4f)
            {
                _chaseBestDist = dist;
                _chaseProgressTime = now;
            }

            // 0. Summoner: Auto Drain Life when HP drops below threshold
            if (ClassFamily == MuHelperClassFamily.Summoner && Config.AutoDrainLifeEnabled &&
                _state.MaximumHealth > 0 && now - _lastDrainLifeTime >= 2.0)
            {
                float hpPct = (_state.CurrentHealth * 100f) / _state.MaximumHealth;
                if (hpPct <= Config.AutoDrainLifePercent)
                {
                    var drainSkill = FindLearnedSkill(458) ?? FindLearnedSkill(214); // Master Drain Life / Drain Life
                    if (drainSkill != null && TryCastAttackSkill(now, hero, target, dist, drainSkill))
                    {
                        _lastDrainLifeTime = now;
                        StatusSummary = "施放吸血回血";
                        return;
                    }
                }
            }

            // 0.5. Dark Knight: 3-skill Combo (Basic -> Secondary -> Tertiary)
            if (ClassFamily == MuHelperClassFamily.DarkKnight && Config.UseActiveSkill && Config.UseCombo &&
                Config.BasicSkillId > 0 && Config.SecondarySkillId > 0 && Config.TertiarySkillId > 0 &&
                _scene.SkillController != null)
            {
                if (_comboStep > 0 && now - _comboLastStepTime > 3.5)
                    _comboStep = 0;

                ushort comboSkillId = _comboStep switch
                {
                    0 => Config.BasicSkillId,
                    1 => Config.SecondarySkillId,
                    _ => Config.TertiarySkillId
                };
                var comboSkill = FindLearnedSkill(comboSkillId);
                if (comboSkill != null && TryCastAttackSkill(now, hero, target, dist, comboSkill))
                {
                    // Advance step only when actually cast (not while still chasing out of range)
                    var def = SkillDatabase.GetSkillDefinition(comboSkill.SkillId);
                    float range = ResolveHelperSkillRange(comboSkill.SkillId, def);
                    if (dist <= range)
                    {
                        _comboStep = (_comboStep + 1) % 3;
                        _comboLastStepTime = now;
                        StatusSummary = $"剑士连击 ({(_comboStep == 0 ? 3 : _comboStep)}/3)";
                    }
                    return;
                }
            }

            // 1. Secondary Skill (ActivationSkill1): delay OR monster-count condition
            if (Config.UseActiveSkill && Config.SecondarySkillId > 0 && _scene.SkillController != null)
            {
                bool secReady =
                    (Config.SecondarySkillUseDelay && now - _lastSecondarySkillTime >= Config.SecondarySkillIntervalSec) ||
                    (Config.SecondarySkillUseCondition && IsSkillConditionMet(hero, Config.SecondarySkillPreCon, Config.SecondarySkillSubCon));

                if (secReady)
                {
                    var secSkill = FindLearnedSkill(Config.SecondarySkillId);
                    if (TryCastAttackSkill(now, hero, target, dist, secSkill))
                    {
                        _lastSecondarySkillTime = now;
                        return;
                    }
                }
            }

            // 1.5. Tertiary Skill (ActivationSkill2, all classes except Dark Lord): delay OR condition
            if (ClassFamily != MuHelperClassFamily.DarkLord &&
                Config.UseActiveSkill && Config.TertiarySkillId > 0 && _scene.SkillController != null)
            {
                bool terReady =
                    (Config.TertiarySkillUseDelay && now - _lastTertiarySkillTime >= Config.TertiarySkillIntervalSec) ||
                    (Config.TertiarySkillUseCondition && IsSkillConditionMet(hero, Config.TertiarySkillPreCon, Config.TertiarySkillSubCon));

                if (terReady)
                {
                    var terSkill = FindLearnedSkill(Config.TertiarySkillId);
                    if (TryCastAttackSkill(now, hero, target, dist, terSkill))
                    {
                        _lastTertiarySkillTime = now;
                        return;
                    }
                }
            }

            // 2. Primary Skill
            if (Config.UseActiveSkill && _scene.SkillController != null)
            {
                var primary = GetPreferredActiveSkill();
                if (TryCastAttackSkill(now, hero, target, dist, primary))
                    return;
            }

            // 3. Fallback: melee basic attack (if enabled or no active skill used)
            if (!Config.FallbackBasicAttack && Config.UseActiveSkill)
                return;

            if (dist > 2.2f)
            {
                if (now - _chaseProgressTime > 4.0)
                {
                    _unreachableUntil[target.NetworkId] = now + 6.0;
                    _chaseTargetId = 0;
                    return;
                }
                if (!hero.IsMoving)
                    hero.MoveTo(target.Location, sendToServer: true, usePathfinding: true);
                return;
            }

            _lastAttackTime = now;
            _chaseProgressTime = now;
            hero.Attack(target);
        }

        private bool IsSkillConditionMet(Objects.Player.PlayerObject hero, int preCon, int subCon)
        {
            if (_scene.World is not WalkableWorldControl world)
                return false;

            int requiredCount = Math.Clamp(subCon, 0, 3) + 2; // 0->2+, 1->3+, 2->4+, 3->5+
            Vector2 anchor = preCon == 1
                ? hero.Location
                : (Config.ReturnToOrigin ? OriginTile : hero.Location);
            float radius = preCon == 1 ? 3.5f : Math.Max(2f, Config.HuntingRange);

            int count = 0;
            foreach (var monster in world.Monsters)
            {
                if (monster == null || monster.IsDead || monster.Status == GameControlStatus.Disposed)
                    continue;
                if (Vector2.Distance(monster.Location, anchor) <= radius)
                {
                    count++;
                    if (count >= requiredCount)
                        return true;
                }
            }

            return false;
        }

        private bool TryCastAttackSkill(double now, Objects.Player.PlayerObject hero, MonsterObject target, float dist, SkillEntryState? skill)
        {
            if (skill == null || _scene.SkillController == null || !SkillManager.MeetsDemandConditions(skill.SkillId))
                return false;

            var skillDef = SkillDatabase.GetSkillDefinition(skill.SkillId);
            float skillRange = ResolveHelperSkillRange(skill.SkillId, skillDef);
            bool enoughMana = skillDef == null || _state.CurrentMana >= skillDef.ManaCost;
            bool enoughAg = skillDef == null || _state.CurrentAbility >= skillDef.AbilityGaugeCost;
            if (!enoughMana || !enoughAg)
                return false;

            if (dist > skillRange)
            {
                if (now - _chaseProgressTime > 4.0)
                {
                    _unreachableUntil[target.NetworkId] = now + 6.0;
                    _chaseTargetId = 0;
                    return true;
                }
                if (!hero.IsMoving)
                    hero.MoveTo(target.Location, sendToServer: true, usePathfinding: true);
                return true;
            }

            if (_scene.SkillController.CastSkillFromHotbar(skill, target))
            {
                _lastAttackTime = now;
                _chaseProgressTime = now;
                return true;
            }

            return false;
        }

        private static float ResolveHelperSkillRange(ushort skillId, Client.Data.BMD.SkillBMD? skillDef)
        {
            if (skillDef != null && skillDef.Distance > 0)
                return Math.Clamp((float)skillDef.Distance, 2.2f, 8.0f);

            // Melee / self-centered spin skills (e.g. Twisting Slash 41, Rageful Blow 42, Death Stab 43)
            // have Distance == 0 in Skill.bmd; keep them in melee range instead of casting at 5 tiles.
            return skillId switch
            {
                9 or 10 or 14 or 38 or 39 or 40 => 6.0f, // Evil Spirit, Hellfire, Inferno, Decay, Ice Storm, Nova
                _ => 2.4f
            };
        }

        private SkillEntryState? GetPreferredActiveSkill()
        {
            if (Config.BasicSkillId > 0)
            {
                var configured = FindLearnedSkill(Config.BasicSkillId);
                if (configured != null)
                    return configured;
            }

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

        private SkillEntryState? FindLearnedSkill(ushort skillId)
        {
            var skills = _state.GetSkills();
            if (skills == null) return null;
            foreach (var s in skills)
            {
                if (s?.SkillId == skillId)
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
