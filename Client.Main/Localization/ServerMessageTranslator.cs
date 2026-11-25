#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Client.Main.Localization
{
    /// <summary>
    /// Modular client-side translator for English OpenMU server notices (0x0D ServerMessage)
    /// and client-generated system chat messages (ChatLogWindow.AddMessage).
    /// </summary>
    public static class ServerMessageTranslator
    {
        private static readonly Dictionary<string, string> ExactMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Happy Hour event has been started!"] = "双倍狂欢活动（Happy Hour）已开启！",
            ["Happy Hour event has started!"] = "双倍狂欢活动（Happy Hour）已开启！",
            ["Happy Hour event has ended!"] = "双倍狂欢活动（Happy Hour）已结束！",
            ["Happy Hour event has been finished!"] = "双倍狂欢活动（Happy Hour）已结束！",
            ["Happy Hour event finished!"] = "双倍狂欢活动（Happy Hour）已结束！",
            ["Another user attempted to login this account. If it wasn't you, we suggest you to change your password."] = "检测到其他用户尝试登录此账号。如果不是您本人操作，建议尽快修改密码。",
            ["Welcome to the world of MU!"] = "欢迎来到奇迹 MU 世界！",
            ["Welcome to MU Online!"] = "欢迎来到奇迹 MU！",
            ["Blood Castle is now open!"] = "血色城堡入口现已开启！",
            ["Blood Castle has started!"] = "血色城堡活动已开始！",
            ["Blood Castle has ended!"] = "血色城堡活动已结束！",
            ["Devil Square is now open!"] = "恶魔广场入口现已开启！",
            ["Devil Square has started!"] = "恶魔广场活动已开始！",
            ["Devil Square has ended!"] = "恶魔广场活动已结束！",
            ["Chaos Castle is now open!"] = "赤色要塞入口现已开启！",
            ["Chaos Castle has started!"] = "赤色要塞活动已开始！",
            ["Chaos Castle has ended!"] = "赤色要塞活动已结束！",
            ["Golden Invasion has started!"] = "黄金部队入侵活动已开启！",
            ["Golden Invasion has ended!"] = "黄金部队入侵活动已结束！",
            ["Red Dragon Invasion has started!"] = "火龙王入侵活动已开启！",
            ["Red Dragon Invasion has ended!"] = "火龙王入侵活动已结束！",
            ["White Wizard Invasion has started!"] = "白色魔法师军团入侵已开启！",
            ["Not enough Zen."] = "金币（Zen）不足。",
            ["Not enough space in inventory."] = "背包空间不足。",
            ["You don't have enough Zen."] = "你的金币（Zen）不足。",
            ["Trade canceled."] = "交易已取消。",
            ["Trade completed."] = "交易已完成。",
            ["Level up!"] = "等级提升！",
            ["Moving the item failed."] = "移动物品失败。",
            ["Dropping the item failed."] = "丢弃物品失败。",
            ["Item is too far away."] = "物品距离太远，无法拾取。",
            ["Failed to pick up item."] = "拾取物品失败。",
            ["Inventory is full."] = "背包已满。",
            ["Quest: progress updated."] = "任务进度已更新。",
            ["You received a buff from Elf Soldier that enhances attack and defense!"] = "你获得了幻影导师的攻防祝福！",
            ["Cannot send message while disconnected or changing maps."] = "断线或切换地图期间无法发送消息。",
            ["Failed to send whisper message."] = "发送私聊消息失败。",
            ["Failed to send message."] = "发送消息失败。",
            ["Target player is no longer available."] = "目标玩家已离开或不可用。",
            ["Failed to send duel request."] = "发送决斗请求失败。",
        };

        private static readonly (Regex Pattern, string Replacement)[] RegexRules =
        {
            (new Regex(@"^(.+?)\s+entered the game\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "$1 进入了游戏。"),
            (new Regex(@"^(.+?)\s+left the game\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "$1 离开了游戏。"),
            (new Regex(@"^Gained (\d+) experience\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "获得 $1 点经验值。"),
            (new Regex(@"^You gained (\d+) experience\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "获得 $1 点经验值。"),
            (new Regex(@"^You have reached Level (\d+)! You have (\d+) stat points remaining\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "恭喜升至 $1 级！当前剩余 $2 点属性点。"),
            (new Regex(@"^You have reached Master Level (\d+)! You have (\d+) master points remaining\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "恭喜大师等级升至 $1 级！当前剩余 $2 点大师点。"),
            (new Regex(@"^You have reached Master Level (\d+)! You received (\d+) master point\(s\), (\d+) available\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "恭喜大师等级升至 $1 级！获得 $2 点大师点，当前可用 $3 点。"),
            (new Regex(@"^Warping to (.+?) \(ID (\d+)\)\.\.\.$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "正在传送至 $1..."),
            (new Regex(@"^Welcome\s+(.+?)!$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "欢迎回来，$1！"),
            (new Regex(@"^Welcome to (.+?)!$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "欢迎来到 $1！"),
            (new Regex(@"^Happy Hour.*?(start|begin).*$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "双倍狂欢活动（Happy Hour）已开启！"),
            (new Regex(@"^Happy Hour.*?(end|finish|over).*$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "双倍狂欢活动（Happy Hour）已结束！"),
            (new Regex(@"^Blood Castle will open in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "血色城堡将在 $1 分钟后开放！"),
            (new Regex(@"^Blood Castle will close in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "血色城堡入口将在 $1 分钟后关闭！"),
            (new Regex(@"^Devil Square will open in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "恶魔广场将在 $1 分钟后开放！"),
            (new Regex(@"^Devil Square will close in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "恶魔广场入口将在 $1 分钟后关闭！"),
            (new Regex(@"^Chaos Castle will open in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "赤色要塞将在 $1 分钟后开放！"),
            (new Regex(@"^Chaos Castle will close in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "赤色要塞入口将在 $1 分钟后关闭！"),
            (new Regex(@"^You received ([\d,]+) Zen\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "获得 $1 金币。"),
            (new Regex(@"^Picked up Zen\. Total:\s*([\d,]+)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "拾取金币，当前总计：$1"),
            (new Regex(@"^Picked up ([\d,]+) Zen\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "拾取 $1 金币。"),
            (new Regex(@"^Picked up (.+?)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "拾取物品：$1"),
            (new Regex(@"^(.+?) has joined the party\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "$1 加入了队伍。"),
            (new Regex(@"^(.+?) has left the party\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "$1 离开了队伍。"),
            (new Regex(@"^Could not connect to the chat server \((.+?)\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "无法连接到聊天服务器（$1）。"),
            (new Regex(@"^Server will shut down in (\d+) second\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled), "服务器将在 $1 秒后关闭维护。"),
        };

        private static string TranslateEventName(string name) => name.Trim() switch
        {
            var s when s.Equals("Chaos Castle", StringComparison.OrdinalIgnoreCase) => "赤色要塞",
            var s when s.Equals("Blood Castle", StringComparison.OrdinalIgnoreCase) => "血色城堡",
            var s when s.Equals("Devil Square", StringComparison.OrdinalIgnoreCase) => "恶魔广场",
            var s when s.Equals("Illusion Temple", StringComparison.OrdinalIgnoreCase) => "幻影寺院",
            var s when s.Equals("Castle Siege", StringComparison.OrdinalIgnoreCase) => "罗兰攻城战",
            var s when s.Equals("Doppelganger", StringComparison.OrdinalIgnoreCase) => "次元镜像",
            var s when s.Equals("Imperial Guardian", StringComparison.OrdinalIgnoreCase) => "帝国守护者要塞",
            var s when s.Equals("Crywolf", StringComparison.OrdinalIgnoreCase) => "狼魂要塞保卫战",
            var s when s.Equals("Kanturu", StringComparison.OrdinalIgnoreCase) => "坎特鲁提炼之塔",
            var s when s.Equals("Golden Invasion", StringComparison.OrdinalIgnoreCase) => "黄金部队入侵",
            var s when s.Equals("Red Dragon Invasion", StringComparison.OrdinalIgnoreCase) => "火龙王入侵",
            var s when s.Equals("White Wizard Invasion", StringComparison.OrdinalIgnoreCase) => "白色魔法师入侵",
            var s when s.Equals("Skeleton King Invasion", StringComparison.OrdinalIgnoreCase) => "骷髅王入侵",
            var s when s.Equals("Happy Hour", StringComparison.OrdinalIgnoreCase) => "双倍狂欢活动",
            _ => name.Trim()
        };

        private static readonly (Regex Pattern, Func<Match, string> Formatter)[] DynamicRules =
        {
            (new Regex(@"^(.+?)\s+entrance is open and closes in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}入口已开放，将在 {m.Groups[2].Value} 分钟后关闭！"),
            (new Regex(@"^(.+?)\s+entrance is now open\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}入口现已开放！"),
            (new Regex(@"^(.+?)\s+(?:entrance is |is )?closed\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}入口已关闭！"),
            (new Regex(@"^(.+?)\s+(?:will start|starts) in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}将在 {m.Groups[2].Value} 分钟后开始！"),
            (new Regex(@"^(.+?)\s+will open in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}将在 {m.Groups[2].Value} 分钟后开放！"),
            (new Regex(@"^(.+?)\s+will close in (\d+) minute\(s\)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}入口将在 {m.Groups[2].Value} 分钟后关闭！"),
            (new Regex(@"^(.+?)\s+(?:has started|has been started|started)[!\.]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}已开始！"),
            (new Regex(@"^(.+?)\s+(?:has ended|has finished|has been finished|ended|finished)[!\.]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                m => $"{TranslateEventName(m.Groups[1].Value)}已结束！"),
        };

        public static string Translate(string? message)
        {
            if (!Loc.IsChinese || string.IsNullOrWhiteSpace(message))
                return message ?? string.Empty;

            var trimmed = message.Trim();
            if (ExactMap.TryGetValue(trimmed, out var zh))
                return zh;

            foreach (var (pattern, formatter) in DynamicRules)
            {
                var match = pattern.Match(trimmed);
                if (match.Success)
                    return formatter(match);
            }

            foreach (var (pattern, replacement) in RegexRules)
            {
                if (pattern.IsMatch(trimmed))
                    return pattern.Replace(trimmed, replacement);
            }

            return message;
        }
    }
}
