#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Localization
{
    /// <summary>
    /// Centralized Simplified Chinese (zh-Hans) localization for MuMono game data and UI.
    /// </summary>
    public static class Loc
    {
        public static bool IsChinese { get; set; } = true;

        private const string ResourcePrefix = "Client.Main.Data.Localization.zh_Hans.";
        private const string ResourcePrefixDash = "Client.Main.Data.Localization.zh-Hans.";

        private static readonly Lazy<Dictionary<string, string>> _items = new(() => LoadEmbeddedDict("items.json"));
        private static readonly Lazy<Dictionary<string, string>> _skills = new(() => LoadEmbeddedDict("skills.json"));
        private static readonly Lazy<Dictionary<string, string>> _sets = new(() => LoadEmbeddedDict("sets.json"));
        private static readonly Lazy<Dictionary<string, string>> _npcs = new(() => LoadEmbeddedDict("npcs.json"));

        private static readonly Dictionary<string, string> _maps = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Lorencia"] = "勇者大陆",
            ["Dungeon"] = "地下城",
            ["Dungeon2"] = "地下城2层",
            ["Dungeon3"] = "地下城3层",
            ["Devias"] = "冰风谷",
            ["Devias2"] = "冰风谷2",
            ["Devias3"] = "冰风谷3",
            ["Devias4"] = "冰风谷4",
            ["Noria"] = "仙踪林",
            ["LostTower"] = "失落之塔",
            ["LostTower2"] = "失落之塔2层",
            ["LostTower3"] = "失落之塔3层",
            ["LostTower4"] = "失落之塔4层",
            ["LostTower5"] = "失落之塔5层",
            ["LostTower6"] = "失落之塔6层",
            ["LostTower7"] = "失落之塔7层",
            ["Arena"] = "古战场",
            ["Stadium"] = "古战场",
            ["Atlans"] = "亚特兰蒂斯",
            ["Atlans2"] = "亚特兰蒂斯2",
            ["Atlans3"] = "亚特兰蒂斯3",
            ["Tarkan"] = "死亡沙漠",
            ["Tarkan2"] = "死亡沙漠2",
            ["Icarus"] = "天空之城",
            ["Elbeland"] = "幻术园",
            ["Elbeland2"] = "幻术园2",
            ["Elbeland3"] = "幻术园3",
            ["Aida"] = "幽暗森林",
            ["Aida1"] = "幽暗森林1",
            ["Aida2"] = "幽暗森林2",
            ["Crywolf"] = "狼魂要塞",
            ["Kanturu"] = "坎特鲁废墟",
            ["Kanturu1"] = "坎特鲁废墟1",
            ["Kanturu2"] = "坎特鲁废墟2",
            ["Kanturu3"] = "坎特鲁废墟3",
            ["KanturuRelics"] = "坎特鲁遗址",
            ["KanturuRemain"] = "坎特鲁遗址",
            ["Kanturu_ruin1"] = "坎特鲁废墟1",
            ["Kanturu_ruin2"] = "坎特鲁废墟2",
            ["Kanturu_ruin3"] = "坎特鲁废墟3",
            ["Kanturu_ruin_island"] = "坎特鲁废墟岛",
            ["Swamp of Calmness"] = "安宁池",
            ["SwampOfCalmness"] = "安宁池",
            ["Calmness"] = "安宁池",
            ["LaCleon"] = "冰霜之城",
            ["Raklion"] = "冰霜之城",
            ["Vulcanus"] = "火山",
            ["Karutan1"] = "卡伦特1",
            ["Karutan2"] = "卡伦特2",
            ["LorenMarket"] = "罗兰市场",
            ["Valley of Loren"] = "罗兰峡谷",
            ["ValleyOfLoren"] = "罗兰峡谷",
            ["LorenDeep"] = "罗兰峡谷",
            ["Land of Trials"] = "魔炼之地",
            ["LandOfTrials"] = "魔炼之地",
            ["Balgass Barracks"] = "巴卡斯兵营",
            ["Barracks"] = "巴卡斯兵营",
            ["Refuge"] = "巴卡斯休息室",
            ["Blood Castle"] = "血色城堡",
            ["BloodCastle"] = "血色城堡",
            ["Devil Square"] = "恶魔广场",
            ["DevilSquare"] = "恶魔广场",
            ["Chaos Castle"] = "赤色要塞",
            ["ChaosCastle"] = "赤色要塞",
            ["Illusion Temple"] = "幻影寺院",
            ["IllusionTemple"] = "幻影寺院",
            ["Kalima"] = "卡利玛",
            ["Kalima1"] = "卡利玛1",
            ["Kalima2"] = "卡利玛2",
            ["Kalima3"] = "卡利玛3",
            ["Kalima4"] = "卡利玛4",
            ["Kalima5"] = "卡利玛5",
            ["Kalima6"] = "卡利玛6",
            ["Kalima7"] = "卡利玛7",
            ["Santa Village"] = "圣诞之地",
            ["Silent Map"] = "寂静之地",
        };

        private static Dictionary<string, string> LoadEmbeddedDict(string fileName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(ResourcePrefix + fileName)
                    ?? asm.GetManifestResourceStream(ResourcePrefixDash + fileName);
                if (stream == null)
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (dict != null)
                {
                    foreach (var kv in dict)
                        result[kv.Key] = kv.Value;
                }
                return result;
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static string ItemName(byte group, short id, string fallback)
        {
            if (!IsChinese) return fallback;
            return _items.Value.TryGetValue($"{group}:{id}", out var zh) && !string.IsNullOrWhiteSpace(zh)
                ? zh
                : fallback;
        }

        public static string SkillName(int skillId, string fallback)
        {
            if (!IsChinese) return fallback;
            return _skills.Value.TryGetValue(skillId.ToString(), out var zh) && !string.IsNullOrWhiteSpace(zh)
                ? zh
                : fallback;
        }

        public static string SetName(int setIndex, string fallback)
        {
            if (!IsChinese) return fallback;
            return _sets.Value.TryGetValue(setIndex.ToString(), out var zh) && !string.IsNullOrWhiteSpace(zh)
                ? zh
                : fallback;
        }

        public static string NpcName(string englishName)
        {
            if (!IsChinese || string.IsNullOrWhiteSpace(englishName)) return englishName;
            return _npcs.Value.TryGetValue(englishName.Trim(), out var zh) && !string.IsNullOrWhiteSpace(zh)
                ? zh
                : englishName;
        }

        public static Dictionary<int, string> LoadGlobalTextOverrides()
        {
            var raw = LoadEmbeddedDict("globaltext.json");
            var map = new Dictionary<int, string>(raw.Count);
            foreach (var kv in raw)
            {
                if (int.TryParse(kv.Key, out int id) && !string.IsNullOrWhiteSpace(kv.Value))
                    map[id] = kv.Value;
            }
            return map;
        }

        public static string MapName(string name)
        {
            if (!IsChinese || string.IsNullOrWhiteSpace(name)) return name;
            var trimmed = name.Trim();
            if (_maps.TryGetValue(trimmed, out var zh))
                return zh;
            // Try removing spaces
            var compact = trimmed.Replace(" ", string.Empty);
            if (_maps.TryGetValue(compact, out zh))
                return zh;
            return name;
        }

        public static string ClassName(CharacterClassNumber cls)
        {
            if (!IsChinese) return cls.ToString();
            return cls switch
            {
                CharacterClassNumber.DarkWizard => "魔法师",
                CharacterClassNumber.SoulMaster => "魔导师",
                CharacterClassNumber.GrandMaster => "神导师",
                CharacterClassNumber.DarkKnight => "剑士",
                CharacterClassNumber.BladeKnight => "骑士",
                CharacterClassNumber.BladeMaster => "神骑士",
                CharacterClassNumber.FairyElf => "弓箭手",
                CharacterClassNumber.MuseElf => "圣射手",
                CharacterClassNumber.HighElf => "神射手",
                CharacterClassNumber.MagicGladiator => "魔剑士",
                CharacterClassNumber.DuelMaster => "剑圣",
                CharacterClassNumber.DarkLord => "圣导师",
                CharacterClassNumber.LordEmperor => "祭师",
                CharacterClassNumber.Summoner => "召唤术师",
                CharacterClassNumber.BloodySummoner => "血腥召唤师",
                CharacterClassNumber.DimensionMaster => "召唤导师",
                CharacterClassNumber.RageFighter => "格斗家",
                CharacterClassNumber.FistMaster => "格斗大师",
                _ => cls.ToString()
            };
        }
    }
}
