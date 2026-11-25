#nullable enable
using System;
using System.IO;
using System.Text.Json;

namespace Client.Main.Controls.UI.Game.MuHelper
{
    /// <summary>
    /// Per-character MU Helper (游戏助手) configuration persisted as clean UTF-8 JSON.
    /// </summary>
    public sealed class MuHelperConfig
    {
        public int HuntingRange { get; set; } = 6;
        public bool UseActiveSkill { get; set; } = true;
        public bool ReturnToOrigin { get; set; } = true;

        public bool AutoHealEnabled { get; set; } = true;
        public int AutoHealPercent { get; set; } = 60;
        public bool AutoManaEnabled { get; set; } = true;
        public int AutoManaPercent { get; set; } = 40;

        public bool AutoPickupEnabled { get; set; } = true;
        public int PickupRange { get; set; } = 6;
        public bool PickupZen { get; set; } = true;
        public bool PickupJewels { get; set; } = true;
        public bool PickupExcellent { get; set; } = true;
        public bool PickupAll { get; set; } = false;

        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        private static string GetConfigPath(string? characterName)
        {
            string safe = string.IsNullOrWhiteSpace(characterName) ? "default" : characterName.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MuMono",
                "MuHelper");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{safe}.json");
        }

        public static MuHelperConfig Load(string? characterName)
        {
            try
            {
                string path = GetConfigPath(characterName);
                if (File.Exists(path))
                {
                    var cfg = JsonSerializer.Deserialize<MuHelperConfig>(File.ReadAllText(path));
                    if (cfg != null)
                    {
                        cfg.Clamp();
                        return cfg;
                    }
                }
            }
            catch
            {
                // Fall back to defaults on corrupt or unreadable config.
            }
            return new MuHelperConfig();
        }

        public void Save(string? characterName)
        {
            try
            {
                Clamp();
                string path = GetConfigPath(characterName);
                File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOpts));
            }
            catch
            {
                // Best-effort save.
            }
        }

        public void Clamp()
        {
            HuntingRange = Math.Clamp(HuntingRange, 2, 12);
            PickupRange = Math.Clamp(PickupRange, 2, 12);
            AutoHealPercent = Math.Clamp(AutoHealPercent, 10, 95);
            AutoManaPercent = Math.Clamp(AutoManaPercent, 10, 95);
        }
    }
}
