using System;
using System.Collections.Generic;
using System.Reflection;
using Client.Main.Core.Utilities;

namespace Client.Main.Controls.UI.Loading
{
    /// <summary>Chooses the loading illustration and display name for a destination world, and holds loading tips.</summary>
    public static class LoadingArt
    {
        public const string Town = "town";

        // First match wins; keys are lower-case fragments of the world type name (e.g. "DeviasWorld").
        private static readonly (string Art, string[] Keys)[] Themes =
        {
            ("underwater", new[] { "atlans", "abyss", "underwater" }),
            ("snow", new[] { "devias", "raklion", "ice" }),
            ("forest", new[] { "noria", "elveland", "crywolf", "swamp", "ferea", "aida" }),
            ("desert", new[] { "tarkan", "kanturu", "karutan", "vulcanus", "blaze", "nars" }),
            ("dungeon", new[]
            {
                "dungeon", "tower", "kalima", "barracks", "refuge", "bloodcastle", "chaoscastle", "devilsquare",
                "illusion", "doppelganger", "acheron", "debenter", "boss"
            }),
        };

        /// <summary>Art key under <c>Ornate/Loading/</c> for the world; the town art when nothing matches.</summary>
        public static string ForWorld(Type worldType)
        {
            string name = worldType?.Name.ToLowerInvariant() ?? string.Empty;
            foreach (var (art, keys) in Themes)
                foreach (string key in keys)
                    if (name.Contains(key, StringComparison.Ordinal))
                        return art;
            return Town;
        }

        /// <summary>Player-facing map name from <see cref="WorldInfoAttribute"/>, or the type name without "World".</summary>
        public static string DisplayName(Type worldType)
        {
            if (worldType == null)
                return null;

            var info = worldType.GetCustomAttribute<WorldInfoAttribute>();
            if (!string.IsNullOrWhiteSpace(info?.DisplayName))
                return Localization.Loc.MapName(info.DisplayName);

            string name = worldType.Name;
            string clean = name.EndsWith("World", StringComparison.Ordinal) ? name[..^5] : name;
            return Localization.Loc.MapName(clean);
        }

        /// <summary>Tips about real client features, shown in rotation while loading.</summary>
        public static IReadOnlyList<string> Tips { get; } = Localization.Loc.IsChinese
            ? new[]
            {
                "按 ESC 键可打开游戏菜单、系统设置与返回选人界面。",
                "按空格键（Space）可拾取脚下最近的掉落物品。",
                "按住 Alt 键可高亮显示地面上的所有掉落物品名称。",
                "按 I 或 V 键打开背包，按 C 键查看角色属性面板。",
                "按 M 键打开地图传送列表，快速前往各大地图。",
                "按 Tab 键可开关当前地图的小地图。",
                "按回车键（Enter）聊天；F5 开关聊天框，F4 调整聊天框大小。",
                "按 G 键打开战盟界面，按 F 键打开好友列表。",
                "在选人界面双击角色或按回车键即可直接进入游戏世界。",
                "如果画面帧率偏低，可在设置中调低渲染比例或阴影画质。",
                "可在「设置 > 界面」中自由切换现代界面与经典界面。",
            }
            : new[]
            {
                "Press ESC to open the game menu, the settings and character select.",
                "Press Space to pick up the nearest item in range.",
                "Hold Alt to highlight items lying on the ground.",
                "Press I or V to open your inventory and C for character info.",
                "Press M to open the move list and travel between maps.",
                "Press Tab to toggle the mini map.",
                "Press Enter to chat. F5 shows or hides the chat frame, F4 changes its size.",
                "Press G to open your guild and F for your friends list.",
                "Double-click a character or press Enter on the selection screen to enter the world.",
                "If the game runs slowly, lower Render Scale or Shadow Quality in Settings.",
                "Switch between the Modern and Classic interface in Settings > Interface.",
            };
    }
}
