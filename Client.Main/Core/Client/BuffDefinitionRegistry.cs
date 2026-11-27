#nullable enable
using System;
using System.Collections.Generic;

namespace Client.Main.Core.Client
{
    public sealed class BuffDefinition
    {
        public BuffEffectId EffectId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ValueText { get; init; }

        /// <summary>Base duration of the server effect (OpenMU MagicEffectDefinition.Duration constant).</summary>
        public TimeSpan? Duration { get; init; }

        /// <summary>
        /// Energy of the caster per extra second (OpenMU durationPerEnergy: TotalEnergy * 1 / divisor); 0 when the
        /// duration does not grow with energy.
        /// </summary>
        public float EnergyPerSecond { get; init; }

        /// <summary>Skills of the hero that apply this effect (used to add the hero's own energy bonus).</summary>
        public ushort[] CasterSkillIds { get; init; } = Array.Empty<ushort>();

        public bool IsDebuff { get; init; }
    }

    /// <summary>
    /// Buff names follow SourceMain5.2 eBuffState (_enum.h:3690), durations OpenMU's magic effect initializers
    /// (Persistence/Initialization/Skills/*EffectInitializer.cs, SkillsInitializerBase.ApplyElementalModifier).
    /// The MagicEffectStatus packet carries no time: a duration here is only shown, the server ends the effect.
    /// </summary>
    public static class BuffDefinitionRegistry
    {
        private static readonly Dictionary<BuffEffectId, BuffDefinition> Definitions = new();

        static BuffDefinitionRegistry()
        {
            bool zh = Localization.Loc.IsChinese;
            Add(1, zh ? "攻击力提升" : "Greater Damage", zh ? "攻击力提升" : "Attack increased", 60);                              // GreaterDamage
            Add(2, zh ? "防御力提升" : "Greater Defense", zh ? "防御力提升" : "Defense increased", 60);                            // GreaterDefense
            Add(3, zh ? "幻影导师祝福" : "Elf Soldier", zh ? "攻击力与防御力大幅提升" : "Attack and defense increased", 21600);     // eBuff_HelpNpc (6h)
            Add(4, zh ? "守护之魂" : "Soul Barrier", zh ? "消耗魔法值吸收伤害" : "Damage absorbed by mana", 60, 40f, 16);           // SoulBarrier
            Add(5, zh ? "致命圣印" : "Critical Damage", zh ? "幸运一击伤害提升" : "Critical damage increased", 60, 10f, 64);        // CriticalDamageIncrease
            Add(6, zh ? "无影箭" : "Infinity Arrow", zh ? "不消耗箭矢" : "Arrows are not consumed", 600, 0f, 77);                  // InfiniteArrow
            Add(7, zh ? "AG恢复提升" : "AG Recovery", zh ? "AG恢复速度提升" : "AG recovery increased", null);                      // eBuff_AddAG
            Add(8, zh ? "生命之光" : "Swell Life", zh ? "最大生命值提升" : "Maximum HP increased", 60, 5f, 48);                    // GreaterFortitude
            Add(9, zh ? "魔法值提升" : "Mana Increase", zh ? "最大魔法值提升" : "Maximum mana increased", null);                    // eBuff_AddMana
            Add(10, zh ? "祝福药水" : "Potion of Bless", zh ? "攻城伤害提升" : "Damage to castle objects increased", 120);         // PotionOfBless
            Add(11, zh ? "灵魂药水" : "Potion of Soul", zh ? "攻速与AG恢复提升" : "Attack speed and AG recovery increased", 60);    // PotionOfSoul
            Add(12, zh ? "魔力解除" : "Removal Magic", null, null);                                                                // eBuff_RemovalMagic
            Add(18, zh ? "隐身" : "Invisibility", zh ? "怪物无法发现" : "Hidden from monsters", null);                             // Transparency
            Add(19, zh ? "技能加成" : "Skill Addition", null, null);                                                               // eBuff_AddSkill
            Add(28, zh ? "管理员" : "Game Master", null, null);                                                                    // GameMaster
            Add(55, zh ? "中毒" : "Poison", zh ? "持续受到毒素伤害" : "Taking poison damage", 10, isDebuff: true);                 // Poisoned
            Add(56, zh ? "冰冻" : "Ice", zh ? "移动速度降低" : "Movement speed reduced", 10, isDebuff: true);                      // Iced
            Add(57, zh ? "冰封" : "Freeze", zh ? "无法移动" : "Unable to move", 5, isDebuff: true);                                // Freeze (Ice Arrow)
            Add(58, zh ? "防御降低" : "Defense Reduction", zh ? "防御力降低" : "Defense decreased", 10, isDebuff: true);           // DefenseReduction
            Add(59, zh ? "攻击降低" : "Attack Reduction", zh ? "攻击力降低" : "Attack decreased", null, isDebuff: true);           // eDeBuff_Attack
            Add(60, zh ? "魔力降低" : "Magic Reduction", zh ? "魔法攻击力降低" : "Wizardry decreased", null, isDebuff: true);      // eDeBuff_MagicPower
            Add(61, zh ? "眩晕" : "Stun", zh ? "无法移动" : "Unable to move", null, isDebuff: true);                               // Stunned
            Add(65, zh ? "束缚" : "Restraint", zh ? "无法移动" : "Unable to move", null, isDebuff: true);                          // SpellOfRestriction
            Add(71, zh ? "伤害反射" : "Damage Reflection", zh ? "反射所受伤害" : "Reflects received damage", null);                // Reflection
            Add(72, zh ? "睡眠" : "Sleep", zh ? "处于睡眠状态" : "Asleep", null, isDebuff: true);                                  // Sleep
            Add(73, zh ? "致盲" : "Blind", zh ? "攻击成功率降低" : "Attack success rate decreased", null, isDebuff: true);         // Blind
            Add(74, zh ? "流血" : "Requiem", zh ? "持续流血伤害" : "Bleeding", null, isDebuff: true);                              // eDeBuff_NeilDOT
            Add(75, zh ? "灼烧" : "Explosion", zh ? "持续灼烧伤害" : "Burning", null, isDebuff: true);                             // eDeBuff_SahamuttDOT
            Add(76, zh ? "虚弱" : "Weakness", zh ? "攻击力降低" : "Attack decreased", 10, isDebuff: true);                         // Weakness
            Add(77, zh ? "破甲" : "Innovation", zh ? "防御力降低" : "Defense decreased", null, isDebuff: true);                    // eDeBuff_DefenseDown
            Add(81, zh ? "狂暴术" : "Berserker", zh ? "攻击/魔力提升，防御降低" : "Wizardry increased, defense reduced", 30, 20f, 218); // Berserker
            Add(82, zh ? "魔力强化" : "Swell of Magic Power", zh ? "魔法攻击力提升" : "Wizardry increased", 1200);                 // WizEnhance
            Add(85, zh ? "雷电冲击" : "Lightning Shock", zh ? "雷电冲击" : "Lightning Shock", null, isDebuff: true);               // eDeBuff_LightningShockDamage
            Add(86, zh ? "毁灭烈焰" : "Blow of Destruction", zh ? "无法移动" : "Unable to move", null, isDebuff: true);            // eDeBuff_BlowOfDestruction
            Add(129, zh ? "无视防御" : "Ignore Defense", zh ? "无视敌人部分防御" : "Ignores part of the enemy defense", 60, 10f);  // IgnoreDefense
            Add(130, zh ? "体能强化" : "Increase Health", zh ? "最大生命值提升" : "Maximum health increased", 60, 10f);            // IncreaseHealth
            Add(131, zh ? "格挡强化" : "Increase Block", zh ? "防御成功率提升" : "Defense rate increased", 60, 10f);               // IncreaseBlock
        }

        public static BuffDefinition Get(BuffEffectId effectId)
        {
            if (Definitions.TryGetValue(effectId, out var definition))
                return definition;

            return new BuffDefinition
            {
                EffectId = effectId,
                Name = $"Effect {(byte)effectId}",
            };
        }

        public static TimeSpan? GetDuration(BuffEffectId effectId) => Get(effectId).Duration;

        public static string GetName(BuffEffectId effectId) => Get(effectId).Name;

        public static string? GetValueText(BuffEffectId effectId) => Get(effectId).ValueText;

        /// <summary>
        /// Duration of an effect: the base, plus the hero's energy bonus when the hero itself cast one of the
        /// effect's skills just before (the energy of another caster is unknown, so only the base is used).
        /// </summary>
        public static TimeSpan? EstimateDuration(BuffEffectId effectId, ushort? heroCastSkillId, int heroEnergy)
        {
            var definition = Get(effectId);
            if (!definition.Duration.HasValue)
                return null;

            double seconds = definition.Duration.Value.TotalSeconds;
            if (definition.EnergyPerSecond > 0f && heroCastSkillId.HasValue &&
                Array.IndexOf(definition.CasterSkillIds, heroCastSkillId.Value) >= 0)
            {
                seconds += heroEnergy / definition.EnergyPerSecond;
            }

            return TimeSpan.FromSeconds(seconds);
        }

        private static void Add(
            byte effectId,
            string name,
            string? valueText,
            int? seconds,
            float energyPerSecond = 0f,
            ushort casterSkillId = 0,
            bool isDebuff = false)
        {
            var id = (BuffEffectId)effectId;
            Definitions[id] = new BuffDefinition
            {
                EffectId = id,
                Name = name,
                ValueText = valueText,
                Duration = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : null,
                EnergyPerSecond = energyPerSecond,
                CasterSkillIds = casterSkillId != 0 ? new[] { casterSkillId } : Array.Empty<ushort>(),
                IsDebuff = isDebuff,
            };
        }

        private static void Add(byte effectId, string name, string? valueText, int? seconds, bool isDebuff) =>
            Add(effectId, name, valueText, seconds, 0f, 0, isDebuff);
    }
}
