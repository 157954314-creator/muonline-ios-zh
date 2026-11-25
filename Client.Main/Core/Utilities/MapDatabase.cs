using System.Collections.Generic;

namespace Client.Main.Core.Utilities
{
    /// <summary>
    /// Provides mapping between map numbers and their names.
    /// </summary>
    public static class MapDatabase
    {
        private static readonly Dictionary<ushort, string> MapNames = InitializeMapData();

        private static Dictionary<ushort, string> InitializeMapData()
        {
            // Based on the provided map initializer names and common map IDs
            return new Dictionary<ushort, string>
            {
                { 0, "Lorencia" },
                { 1, "Dungeon" },
                { 2, "Devias" },
                { 3, "Noria" },
                { 4, "Lost Tower" },
                { 5, "Exile" },
                { 6, "Arena" },
                { 7, "Atlans" },
                { 8, "Tarkan" },
                { 9, "Devil Square" },
                { 10, "Icarus" },
                { 11, "Blood Castle 1" },
                { 12, "Blood Castle 2" },
                { 13, "Blood Castle 3" },
                { 14, "Blood Castle 4" },
                { 15, "Blood Castle 5" },
                { 16, "Blood Castle 6" },
                { 17, "Blood Castle 7" },
                { 18, "Chaos Castle 1" },
                { 19, "Chaos Castle 2" },
                { 20, "Chaos Castle 3" },
                { 21, "Chaos Castle 4" },
                { 22, "Chaos Castle 5" },
                { 23, "Chaos Castle 6" },
                { 24, "Kalima 1" },
                { 25, "Kalima 2" },
                { 26, "Kalima 3" },
                { 27, "Kalima 4" },
                { 28, "Kalima 5" },
                { 29, "Kalima 6" },
                { 30, "Valley of Loren" },
                { 31, "Land of Trial" },
                { 32, "Devil Square" },
                { 33, "Aida" },
                { 34, "Crywolf" },
                { 36, "Kalima 7" },
                { 37, "Kantru1" },
                { 38, "Kantru2" },
                { 39, "Kantru3" },
                { 40, "Silent" },
                { 41, "T42" },
                { 42, "T43" },
                { 45, "Illusion Temple" },
                { 46, "Illusion Temple 2" },
                { 47, "Illusion Temple 3" },
                { 48, "Illusion Temple 4" },
                { 49, "Illusion Temple 5" },
                { 50, "Elbeland 2" },
                { 51, "Elbeland" },
                { 52, "Blood Castle" },
                { 53, "Chaos Castle 7" },
                { 56, "(null)" },
                { 57, "Raklion" },
                { 58, "Buhwajang" },
                { 62, "Santa" },
                { 63, "Vulcanus" },
                { 64, "Terrain64" },
                { 65, "65_Doppelganger1" },
                { 66, "66_Doppelganger2" },
                { 67, "67_Doppelganger3" },
                { 68, "68_Doppelganger4" },
                { 69, "69_ImperialGuardian1" },
                { 70, "70_ImperialGuardian2" },
                { 71, "71_ImperialGuardian3" },
                { 72, "72_ImperialGuardian4" },
                { 79, "Loren Market" },
                { 80, "80_Karutan1" },
                { 81, "81_Karutan2" },
                { 82, "82_Doppelganger_Renewal" },
                { 91, "91_Acheron" },
                { 92, "91_Acheron" },
                { 95, "95_Debenter" },
                { 96, "96_Debenter_ArcaBattle" },
                { 97, "97_ChaosCastleSurvival" },
                { 98, "98_IllussionTempleLeague" },
                { 99, "99_IllussionTempleLeague2" },
                { 100, "100_UrkMontain" },
                { 102, "102_TormentedSquare" },
                { 110, "110_Nars" },
                { 112, "112_Ferea" },
                { 113, "113_NixieLake" },
                { 114, "114_Terrain115" },
                { 115, "115_Terrain116" },
                { 116, "DeepDugeon1" },
                { 117, "DeepDugeon2" },
                { 118, "DeepDugeon3" },
                { 119, "DeepDugeon4" },
                { 120, "DeepDugeon5" },
                { 121, "PlaceOfQualification" },
                { 122, "SwampOfDarkness" },
                { 123, "KuberaMine1" },
                { 124, "KuberaMine1" },
                { 125, "KuberaMine1" },
                { 126, "KuberaMine1" },
                { 127, "KuberaMine1" },
                { 128, "World129" },
                { 129, "World130" },
                { 130, "World131" },
                { 131, "World132" },
            };
        }

        /// <summary>
        /// Gets the name of the map based on its number.
        /// </summary>
        /// <param name="mapId">The map number (ID).</param>
        /// <returns>The name of the map, or a default string if not found.</returns>
        public static string GetMapName(ushort mapId)
        {
            if (Localization.Loc.IsChinese)
            {
                string zh = mapId switch
                {
                    0 => "勇者大陆",
                    1 => "地下城",
                    2 => "冰风谷",
                    3 => "仙踪林",
                    4 => "失落之塔",
                    5 => "流放之地",
                    6 => "古战场",
                    7 => "亚特兰蒂斯",
                    8 => "死亡沙漠",
                    9 => "恶魔广场",
                    10 => "天空之城",
                    >= 11 and <= 17 => $"血色城堡 {mapId - 10}",
                    >= 18 and <= 23 => $"赤色要塞 {mapId - 17}",
                    >= 24 and <= 29 => $"卡利玛 {mapId - 23}",
                    30 => "罗兰峡谷",
                    31 => "魔炼之地",
                    32 => "恶魔广场",
                    33 => "幽暗森林",
                    34 => "狼魂要塞",
                    36 => "卡利玛 7",
                    37 => "坎特鲁废墟 1",
                    38 => "坎特鲁废墟 2",
                    39 => "坎特鲁遗址",
                    40 => "寂静之地",
                    41 => "巴卡斯兵营",
                    42 => "巴卡斯休息室",
                    45 => "幻影寺院 1",
                    46 => "幻影寺院 2",
                    47 => "幻影寺院 3",
                    48 => "幻影寺院 4",
                    49 => "幻影寺院 5",
                    50 => "幻术园 2",
                    51 => "幻术园",
                    52 => "血色城堡 8",
                    53 => "赤色要塞 7",
                    56 => "安宁池",
                    57 => "冰霜之城",
                    58 => "冰霜之城孵化场",
                    62 => "圣诞之地",
                    63 => "火山",
                    79 => "罗兰市场",
                    80 => "卡伦特 1",
                    81 => "卡伦特 2",
                    91 or 92 => "阿卡伦",
                    95 or 96 => "德班泰尔",
                    100 => "乌尔克山",
                    110 => "纳尔斯",
                    112 => "菲利亚",
                    113 => "尼克西斯之湖",
                    122 => "暗黑沼泽",
                    _ => null
                };
                if (zh != null) return zh;
            }
            return MapNames.TryGetValue(mapId, out var name) ? Localization.Loc.MapName(name) : (Localization.Loc.IsChinese ? $"未知地图 ({mapId})" : $"Unknown Map ({mapId})");
        }
    }
}
