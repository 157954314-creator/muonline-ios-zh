using System.Collections.Generic;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Controls.UI.SelectCharacter
{
    /// <summary>Playable base classes as presented by the character screens (roster, creation).</summary>
    public static class CharacterClasses
    {
        /// <param name="PreviewModel">Creation preview bust in <c>Data/Logo</c>. NewFace01..07 follow the base class
        /// order Wizard, Knight, Elf, Gladiator, Lord, Summoner, Rage Fighter (MODEL_FACE + class in the original client).</param>
        public sealed record Info(CharacterClassNumber Class, string Name, string Description, string Emblem, string PreviewModel);

        /// <summary>Classes offered by character creation, in display order.</summary>
        public static IReadOnlyList<Info> Creatable { get; } = new[]
        {
            new Info(CharacterClassNumber.DarkWizard, Localization.Loc.IsChinese ? "魔法师" : "Dark Wizard",
                Localization.Loc.IsChinese ? "掌控毁灭性元素魔法。\n高魔法伤害，防御较低。" : "Masters of destructive magic.\nHigh magic damage, low defense.", "class_wizard", "NewFace01"),
            new Info(CharacterClassNumber.DarkKnight, Localization.Loc.IsChinese ? "剑士" : "Dark Knight",
                Localization.Loc.IsChinese ? "力量与荣耀的近战战士。\n高生命值与物理输出。" : "Warriors of strength and honor.\nHigh health and physical damage.", "class_knight", "NewFace02"),
            new Info(CharacterClassNumber.FairyElf, Localization.Loc.IsChinese ? "弓箭手" : "Fairy Elf",
                Localization.Loc.IsChinese ? "敏捷的远程射手与辅助。\n高攻速，擅长增益与治疗。" : "Agile archers and healers.\nHigh agility, support abilities.", "class_elf", "NewFace03"),
            new Info(CharacterClassNumber.MagicGladiator, Localization.Loc.IsChinese ? "魔剑士" : "Magic Gladiator",
                Localization.Loc.IsChinese ? "魔武双修的混战强者。\n兼顾近战剑术与魔法技能。" : "Hybrid warriors with magic.\nBalanced melee and magic skills.", "class_gladiator", "NewFace04"),
            new Info(CharacterClassNumber.DarkLord, Localization.Loc.IsChinese ? "圣导师" : "Dark Lord",
                Localization.Loc.IsChinese ? "统御战场的黑暗统帅。\n召唤战马天鹰并强化队友。" : "Commanders with dark powers.\nSummons pets and commands armies.", "class_lord", "NewFace05"),
            new Info(CharacterClassNumber.Summoner, Localization.Loc.IsChinese ? "召唤术师" : "Summoner",
                Localization.Loc.IsChinese ? "精通诅咒与召唤秘术。\n削弱敌人并召唤异界生物。" : "Mystics with curse powers.\nCurses enemies and summons allies.", "class_summoner", "NewFace06"),
            new Info(CharacterClassNumber.RageFighter, Localization.Loc.IsChinese ? "格斗家" : "Rage Fighter",
                Localization.Loc.IsChinese ? "近身格斗与体术宗师。\n超高生命与强力连击。" : "Hand-to-hand combat masters.\nHighest HP, powerful combos.", "class_fighter", "NewFace07"),
        };

        public static string PreviewModelFor(CharacterClassNumber value)
        {
            foreach (Info info in Creatable)
                if (info.Class == value)
                    return info.PreviewModel;
            return null;
        }

        /// <summary>Emblem key (<c>Icons/</c>, <c>Emblems/</c>) of a class or any of its evolutions.</summary>
        public static string EmblemFor(CharacterClassNumber value) => value switch
        {
            CharacterClassNumber.DarkWizard or CharacterClassNumber.SoulMaster or CharacterClassNumber.GrandMaster => "class_wizard",
            CharacterClassNumber.DarkKnight or CharacterClassNumber.BladeKnight or CharacterClassNumber.BladeMaster => "class_knight",
            CharacterClassNumber.FairyElf or CharacterClassNumber.MuseElf or CharacterClassNumber.HighElf => "class_elf",
            CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster => "class_gladiator",
            CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor => "class_lord",
            CharacterClassNumber.Summoner or CharacterClassNumber.BloodySummoner or CharacterClassNumber.DimensionMaster => "class_summoner",
            CharacterClassNumber.RageFighter or CharacterClassNumber.FistMaster => "class_fighter",
            _ => "class_knight"
        };

        /// <summary>Name rules of character creation: 3-10 letters or digits.</summary>
        public const int MinNameLength = 3;
        public const int MaxNameLength = 10;

        /// <summary>Keeps only letters and digits, cut to <see cref="MaxNameLength"/>.</summary>
        public static string SanitizeName(string value)
        {
            var result = new System.Text.StringBuilder(MaxNameLength);
            foreach (char c in value ?? string.Empty)
            {
                if (result.Length >= MaxNameLength)
                    break;
                if (char.IsLetterOrDigit(c))
                    result.Append(c);
            }
            return result.ToString();
        }

        /// <summary>Returns an error message, or null when the name can be submitted.</summary>
        public static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Localization.Loc.IsChinese ? "请输入角色名称。" : "Enter a character name.";
            if (name.Length < MinNameLength)
                return Localization.Loc.IsChinese ? $"角色名称至少需要 {MinNameLength} 个字符。" : $"The name needs at least {MinNameLength} characters.";
            if (name.Length > MaxNameLength)
                return Localization.Loc.IsChinese ? $"角色名称最多 {MaxNameLength} 个字符。" : $"The name can have at most {MaxNameLength} characters.";
            return null;
        }
    }
}
