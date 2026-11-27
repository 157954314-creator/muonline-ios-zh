#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Controls.UI.Game.MuHelper
{
    public enum MuHelperClassFamily
    {
        DarkWizard,
        DarkKnight,
        FairyElf,
        MagicGladiator,
        DarkLord,
        Summoner,
        RageFighter
    }

    /// <summary>
    /// Per-character S6-grade MU Helper (游戏助手) configuration.
    /// Persisted locally as UTF-8 JSON and synced with OpenMU via the standard 257-byte MuHelper wire payload.
    /// </summary>
    public sealed class MuHelperConfig
    {
        public const int WireLength = 257;
        private const int ExtraItemsOffset = 65;
        private const int ExtraItemsLength = 192;

        // --- Hunting & Skills ---
        public int HuntingRange { get; set; } = 6;
        public bool UseActiveSkill { get; set; } = true;
        public ushort BasicSkillId { get; set; } = 0; // 0 = auto (current hotbar skill)
        public ushort SecondarySkillId { get; set; } = 0;
        public bool SecondarySkillUseDelay { get; set; } = true;
        public bool SecondarySkillUseCondition { get; set; } = false;
        public int SecondarySkillPreCon { get; set; } = 0; // 0=攻击范围内怪物, 1=正在攻击我的怪物
        public int SecondarySkillSubCon { get; set; } = 0; // 0=2只+, 1=3只+, 2=4只+, 3=5只+
        public int SecondarySkillIntervalSec { get; set; } = 5;
        public ushort TertiarySkillId { get; set; } = 0; // ActivationSkill2 (all classes except DL)
        public bool TertiarySkillUseDelay { get; set; } = true;
        public bool TertiarySkillUseCondition { get; set; } = false;
        public int TertiarySkillPreCon { get; set; } = 0;
        public int TertiarySkillSubCon { get; set; } = 0;
        public int TertiarySkillIntervalSec { get; set; } = 8;
        public bool ReturnToOrigin { get; set; } = true;
        public int MaxSecondsAway { get; set; } = 10;
        public bool LongRangeCounterAttack { get; set; } = true;
        public bool FallbackBasicAttack { get; set; } = true;

        // --- Class-Specific Features (S6 parity) ---
        // Dark Knight: 3-skill Combo (BasicSkillId -> SecondarySkillId -> TertiarySkillId)
        public bool UseCombo { get; set; } = false;
        // Fairy Elf: Auto Heal Skill (self & party)
        public bool AutoHealSkillEnabled { get; set; } = false;
        public int AutoHealSkillPercent { get; set; } = 60;
        // Fairy Elf & Dark Wizard: Party Support (Party Heal for Elf, Party Buff for Elf/DW)
        public bool PartySupportEnabled { get; set; } = false;
        public bool PartyAutoHealEnabled { get; set; } = false;
        public int PartyHealPercent { get; set; } = 60;
        public bool PartyBuffEnabled { get; set; } = false;
        // Summoner: Auto Drain Life
        public bool AutoDrainLifeEnabled { get; set; } = false;
        public int AutoDrainLifePercent { get; set; } = 60;
        // Dark Lord: Dark Raven Pet Attack Mode (0=停止攻击, 1=自动攻击, 2=与主人同攻)
        public bool UseDarkRaven { get; set; } = false;
        public int DarkRavenMode { get; set; } = 1;

        public static MuHelperClassFamily GetClassFamily(CharacterClassNumber cls) => cls switch
        {
            CharacterClassNumber.DarkWizard or CharacterClassNumber.SoulMaster or CharacterClassNumber.GrandMaster => MuHelperClassFamily.DarkWizard,
            CharacterClassNumber.DarkKnight or CharacterClassNumber.BladeKnight or CharacterClassNumber.BladeMaster => MuHelperClassFamily.DarkKnight,
            CharacterClassNumber.FairyElf or CharacterClassNumber.MuseElf or CharacterClassNumber.HighElf => MuHelperClassFamily.FairyElf,
            CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster => MuHelperClassFamily.MagicGladiator,
            CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor => MuHelperClassFamily.DarkLord,
            CharacterClassNumber.Summoner or CharacterClassNumber.BloodySummoner or CharacterClassNumber.DimensionMaster => MuHelperClassFamily.Summoner,
            CharacterClassNumber.RageFighter or CharacterClassNumber.FistMaster => MuHelperClassFamily.RageFighter,
            _ => MuHelperClassFamily.DarkKnight
        };

        public static string GetClassFamilyDisplayName(MuHelperClassFamily family) => family switch
        {
            MuHelperClassFamily.DarkWizard => "魔法师系",
            MuHelperClassFamily.DarkKnight => "剑士系",
            MuHelperClassFamily.FairyElf => "弓箭手系",
            MuHelperClassFamily.MagicGladiator => "魔剑士系",
            MuHelperClassFamily.DarkLord => "圣导师系",
            MuHelperClassFamily.Summoner => "召唤术师系",
            MuHelperClassFamily.RageFighter => "格斗家系",
            _ => "通用"
        };

        // --- Buffs ---
        public bool AutoBuffEnabled { get; set; } = true;
        public int BuffIntervalSec { get; set; } = 30;
        public ushort BuffSkill0Id { get; set; } = 0;
        public ushort BuffSkill1Id { get; set; } = 0;
        public ushort BuffSkill2Id { get; set; } = 0;

        // --- Recovery & Maintenance ---
        public bool AutoHealEnabled { get; set; } = true;
        public int AutoHealPercent { get; set; } = 60;
        public bool AutoManaEnabled { get; set; } = true;
        public int AutoManaPercent { get; set; } = 40;
        public bool AutoRepairEnabled { get; set; } = true;

        // --- Pickup & Filters ---
        public bool AutoPickupEnabled { get; set; } = true;
        public int PickupRange { get; set; } = 6;
        public bool PickupZen { get; set; } = true;
        public bool PickupJewels { get; set; } = true;
        public bool PickupExcellent { get; set; } = true;
        public bool PickupAncient { get; set; } = true;
        public bool PickupAll { get; set; } = false;
        public bool PickupExtraEnabled { get; set; } = false;
        public List<string> ExtraPickupKeywords { get; set; } = new();

        // --- Social / Misc ---
        public bool AutoAcceptFriend { get; set; } = false;
        public bool AutoAcceptGuild { get; set; } = false;

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
            MaxSecondsAway = Math.Clamp(MaxSecondsAway, 0, 60);
            SecondarySkillIntervalSec = Math.Clamp(SecondarySkillIntervalSec, 1, 60);
            TertiarySkillIntervalSec = Math.Clamp(TertiarySkillIntervalSec, 1, 60);
            BuffIntervalSec = Math.Clamp(BuffIntervalSec, 5, 300);
            AutoHealPercent = Math.Clamp(AutoHealPercent, 10, 90);
            AutoManaPercent = Math.Clamp(AutoManaPercent, 10, 90);
            AutoHealSkillPercent = Math.Clamp(AutoHealSkillPercent, 10, 90);
            PartyHealPercent = Math.Clamp(PartyHealPercent, 10, 90);
            AutoDrainLifePercent = Math.Clamp(AutoDrainLifePercent, 10, 90);
            DarkRavenMode = Math.Clamp(DarkRavenMode, 0, 2);
            ExtraPickupKeywords ??= new List<string>();
        }

        /// <summary>
        /// Encodes this configuration into the standard 257-byte OpenMU / S6 PRECEIVE_MUHELPER_DATA wire payload.
        /// </summary>
        public byte[] ToWireBytes()
        {
            Clamp();
            var data = new byte[WireLength];

            data[0] = 0x01; // DataStartMarker

            // Byte 1: pickup filter flags
            byte b1 = 0;
            if (PickupJewels) b1 |= 1 << 3;
            if (PickupAncient) b1 |= 1 << 4;
            if (PickupExcellent) b1 |= 1 << 5;
            if (PickupZen) b1 |= 1 << 6;
            if (PickupExtraEnabled) b1 |= 1 << 7;
            data[1] = b1;

            // Byte 2: HuntingRange (low nibble) | ObtainRange (high nibble)
            data[2] = (byte)((HuntingRange & 0x0F) | ((PickupRange & 0x0F) << 4));

            // Bytes 3..22: timers & skill IDs (little-endian uint16)
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(3, 2), (ushort)MaxSecondsAway);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(5, 2), BasicSkillId);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(7, 2), SecondarySkillId);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(9, 2), (ushort)SecondarySkillIntervalSec);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(11, 2), TertiarySkillId);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(13, 2), (ushort)TertiarySkillIntervalSec);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(15, 2), (ushort)BuffIntervalSec);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(17, 2), BuffSkill0Id);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(19, 2), BuffSkill1Id);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(21, 2), BuffSkill2Id);

            // Byte 23: HPStatusAutoPotion (low nibble) | HPStatusAutoHeal (high nibble)
            int hpTenths = Math.Clamp(AutoHealPercent / 10, 1, 9);
            int healSkillTenths = Math.Clamp(AutoHealSkillPercent / 10, 1, 9);
            data[23] = (byte)((hpTenths & 0x0F) | ((healSkillTenths & 0x0F) << 4));

            // Byte 24: HPStatusOfPartyMembers (low nibble) | HPStatusDrainLife (high nibble)
            int partyHealTenths = Math.Clamp(PartyHealPercent / 10, 1, 9);
            int drainTenths = Math.Clamp(AutoDrainLifePercent / 10, 1, 9);
            data[24] = (byte)((partyHealTenths & 0x0F) | ((drainTenths & 0x0F) << 4));

            // Byte 25: core hunting/potion/class toggles
            byte b25 = 0;
            if (AutoHealEnabled) b25 |= 1 << 0;
            if (AutoHealSkillEnabled) b25 |= 1 << 1;
            if (AutoDrainLifeEnabled) b25 |= 1 << 2;
            if (LongRangeCounterAttack) b25 |= 1 << 3;
            if (ReturnToOrigin) b25 |= 1 << 4;
            if (UseCombo) b25 |= 1 << 5;
            if (PartySupportEnabled) b25 |= 1 << 6;
            if (PartyAutoHealEnabled) b25 |= 1 << 7;
            data[25] = b25;

            // Byte 26: party buff, dark raven, self buff & secondary skill delay/condition bits (exact S6 layout)
            byte b26 = 0;
            if (PartyBuffEnabled) b26 |= 1 << 0;
            if (UseDarkRaven) b26 |= 1 << 1;
            if (AutoBuffEnabled) b26 |= 1 << 2;
            if (SecondarySkillId > 0 && SecondarySkillUseDelay) b26 |= 1 << 3;
            if (SecondarySkillId > 0 && SecondarySkillUseCondition) b26 |= 1 << 4;
            if (SecondarySkillPreCon != 0) b26 |= 1 << 5;
            b26 |= (byte)((Math.Clamp(SecondarySkillSubCon, 0, 3) & 0x03) << 6);
            data[26] = b26;

            // Byte 27: skill2 delay/condition, repair & pickup modes (exact S6 layout)
            byte b27 = 0;
            if (TertiarySkillId > 0 && TertiarySkillUseDelay) b27 |= 1 << 0;
            if (TertiarySkillId > 0 && TertiarySkillUseCondition) b27 |= 1 << 1;
            if (TertiarySkillPreCon != 0) b27 |= 1 << 2;
            b27 |= (byte)((Math.Clamp(TertiarySkillSubCon, 0, 3) & 0x03) << 3);
            if (AutoRepairEnabled) b27 |= 1 << 5;
            if (PickupAll) b27 |= 1 << 6;
            if (AutoPickupEnabled && !PickupAll) b27 |= 1 << 7;
            data[27] = b27;

            // Byte 28: Dark Raven pet attack mode (0=Cease, 1=Auto, 2=Together)
            data[28] = (byte)Math.Clamp(DarkRavenMode, 0, 2);

            // Byte 29: client-local extension bits (shared with S6 MuMain)
            byte b29 = 0;
            if (AutoAcceptFriend) b29 |= 1 << 1;
            if (AutoAcceptGuild) b29 |= 1 << 2;
            if (FallbackBasicAttack) b29 |= 1 << 3;
            data[29] = b29;

            // Byte 30: MPStatusAutoPotion (tenths, 0 = disabled)
            data[30] = AutoManaEnabled ? (byte)Math.Clamp(AutoManaPercent / 10, 1, 9) : (byte)0;

            // Bytes 65..256: null-delimited UTF-8 extra pickup keywords
            int cursor = ExtraItemsOffset;
            int limit = ExtraItemsOffset + ExtraItemsLength;
            foreach (var raw in ExtraPickupKeywords)
            {
                string kw = raw?.Trim() ?? string.Empty;
                if (kw.Length == 0) continue;
                byte[] bytes = Encoding.UTF8.GetBytes(kw);
                if (cursor + bytes.Length + 1 > limit) break;
                bytes.CopyTo(data, cursor);
                cursor += bytes.Length;
                data[cursor++] = 0;
            }

            return data;
        }

        /// <summary>
        /// Applies a 257-byte OpenMU / S6 PRECEIVE_MUHELPER_DATA wire payload onto this config.
        /// Returns false if the payload is empty/uninitialized (all zeros).
        /// </summary>
        public bool ApplyFromWireBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length < WireLength)
                return false;

            bool anyNonZero = false;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != 0) { anyNonZero = true; break; }
            }
            if (!anyNonZero)
                return false;

            byte b1 = data[1];
            PickupJewels = (b1 & (1 << 3)) != 0;
            PickupAncient = (b1 & (1 << 4)) != 0;
            PickupExcellent = (b1 & (1 << 5)) != 0;
            PickupZen = (b1 & (1 << 6)) != 0;
            PickupExtraEnabled = (b1 & (1 << 7)) != 0;

            int hRange = data[2] & 0x0F;
            int pRange = (data[2] >> 4) & 0x0F;
            if (hRange > 0) HuntingRange = hRange;
            if (pRange > 0) PickupRange = pRange;

            MaxSecondsAway = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(3, 2));
            BasicSkillId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(5, 2));
            SecondarySkillId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(7, 2));
            int secDelay = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(9, 2));
            if (secDelay > 0) SecondarySkillIntervalSec = secDelay;
            TertiarySkillId = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(11, 2));
            int terDelay = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(13, 2));
            if (terDelay > 0) TertiarySkillIntervalSec = terDelay;

            int buffSec = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(15, 2));
            if (buffSec > 0) BuffIntervalSec = buffSec;
            BuffSkill0Id = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(17, 2));
            BuffSkill1Id = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(19, 2));
            BuffSkill2Id = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(21, 2));

            int hpTenths = data[23] & 0x0F;
            int healSkillTenths = (data[23] >> 4) & 0x0F;
            if (hpTenths > 0) AutoHealPercent = hpTenths * 10;
            if (healSkillTenths > 0) AutoHealSkillPercent = healSkillTenths * 10;

            int partyHealTenths = data[24] & 0x0F;
            int drainTenths = (data[24] >> 4) & 0x0F;
            if (partyHealTenths > 0) PartyHealPercent = partyHealTenths * 10;
            if (drainTenths > 0) AutoDrainLifePercent = drainTenths * 10;

            byte b25 = data[25];
            AutoHealEnabled = (b25 & (1 << 0)) != 0;
            AutoHealSkillEnabled = (b25 & (1 << 1)) != 0;
            AutoDrainLifeEnabled = (b25 & (1 << 2)) != 0;
            LongRangeCounterAttack = (b25 & (1 << 3)) != 0;
            ReturnToOrigin = (b25 & (1 << 4)) != 0;
            UseCombo = (b25 & (1 << 5)) != 0;
            PartySupportEnabled = (b25 & (1 << 6)) != 0;
            PartyAutoHealEnabled = (b25 & (1 << 7)) != 0;

            byte b26 = data[26];
            PartyBuffEnabled = (b26 & (1 << 0)) != 0;
            UseDarkRaven = (b26 & (1 << 1)) != 0;
            AutoBuffEnabled = (b26 & (1 << 2)) != 0;
            SecondarySkillUseDelay = (b26 & (1 << 3)) != 0;
            SecondarySkillUseCondition = (b26 & (1 << 4)) != 0;
            SecondarySkillPreCon = (b26 & (1 << 5)) != 0 ? 1 : 0;
            SecondarySkillSubCon = (b26 >> 6) & 0x03;
            if (!SecondarySkillUseDelay && !SecondarySkillUseCondition)
                SecondarySkillUseDelay = true;

            byte b27 = data[27];
            TertiarySkillUseDelay = (b27 & (1 << 0)) != 0;
            TertiarySkillUseCondition = (b27 & (1 << 1)) != 0;
            TertiarySkillPreCon = (b27 & (1 << 2)) != 0 ? 1 : 0;
            TertiarySkillSubCon = (b27 >> 3) & 0x03;
            if (!TertiarySkillUseDelay && !TertiarySkillUseCondition)
                TertiarySkillUseDelay = true;
            AutoRepairEnabled = (b27 & (1 << 5)) != 0;
            PickupAll = (b27 & (1 << 6)) != 0;
            bool pickSelected = (b27 & (1 << 7)) != 0;
            AutoPickupEnabled = PickupAll || pickSelected || PickupZen || PickupJewels || PickupExcellent || PickupAncient || PickupExtraEnabled;

            DarkRavenMode = Math.Clamp((int)data[28], 0, 2);

            byte b29 = data[29];
            AutoAcceptFriend = (b29 & (1 << 1)) != 0;
            AutoAcceptGuild = (b29 & (1 << 2)) != 0;
            FallbackBasicAttack = (b29 & (1 << 3)) != 0;

            byte mpTenths = data[30];
            if (mpTenths > 0 && mpTenths <= 10)
            {
                AutoManaEnabled = true;
                AutoManaPercent = mpTenths * 10;
            }
            else if (mpTenths == 0)
            {
                AutoManaEnabled = false;
            }

            // Decode null-delimited extra keywords
            var keywords = new List<string>();
            int start = ExtraItemsOffset;
            int end = ExtraItemsOffset + ExtraItemsLength;
            while (start < end)
            {
                int zero = start;
                while (zero < end && data[zero] != 0) zero++;
                if (zero > start)
                {
                    string kw = Encoding.UTF8.GetString(data.Slice(start, zero - start)).Trim();
                    if (kw.Length > 0 && !keywords.Contains(kw, StringComparer.OrdinalIgnoreCase))
                        keywords.Add(kw);
                }
                else
                {
                    break;
                }
                start = zero + 1;
            }
            ExtraPickupKeywords = keywords;

            Clamp();
            return true;
        }
    }
}
