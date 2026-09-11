using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// Boss 击杀进度查询系统。
    /// 通过反射读取 CalamityMod（现代版）或 CalamityModClassicPreTrailer（经典版）
    /// 的击杀标记，向模组其余部分提供统一的 Boss 进度查询接口。
    /// 现代版优先，若两者皆不可用则对应查询返回 false。
    /// </summary>
    internal class CalamityDemulationBossSystem
    {
        // ── 静态字段 ──
        /// <summary>
        /// 规范化 Boss 键 → 各数据源成员名的映射表。
        /// CalamityProp = CalamityMod.DownedBossSystem 的属性名（现代版）；
        /// ClassicField = CalamityWorldPreTrailer 的字段名（Classic 版）；
        /// 某项为 null 表示该数据源不存在此 Boss 的标记（如原版进度标记仅 Classic 有）。
        /// </summary>
        private static readonly BossLink[] Links =
        [
              new("BossAny",                null,                       "downedBossAny"),
              new("DesertScourge",          "downedDesertScourge",      "downedDesertScourge"),
              new("Crabulon",               "downedCrabulon",           "downedCrabulon"),
              new("HiveMind",               "downedHiveMind",           "downedHiveMind"),
              new("Perforator",             "downedPerforator",         "downedPerforator"),
              new("SlimeGod",               "downedSlimeGod",           "downedSlimeGod"),
              new("Cryogen",                "downedCryogen",            "downedCryogen"),
              new("AquaticScourge",         "downedAquaticScourge",     "downedAquaticScourge"),
              new("BrimstoneElemental",     "downedBrimstoneElemental", "downedBrimstoneElemental"),
              new("Calamitas",              "downedCalamitasClone",     "downedCalamitas"),
              new("Leviathan",              "downedLeviathan",          "downedLeviathan"),
              new("AstrumAureus",           "downedAstrumAureus",       null),
              new("Astrageldon",            null,                       "downedAstrageldon"),
              new("Plaguebringer",          "downedPlaguebringer",      "downedPlaguebringer"),
              new("Ravager",                "downedRavager",            null),
              new("AstrumDeus",             "downedAstrumDeus",         null),
              new("StarGod",                null,                       "downedStarGod"),
              new("Scavenger",              null,                       "downedScavenger"),
              new("Guardians",              "downedGuardians",          "downedGuardians"),
              new("Dragonfolly",            "downedDragonfolly",        null),
              new("Providence",             "downedProvidence",         "downedProvidence"),
              new("CeaselessVoid",          "downedCeaselessVoid",      null),
              new("StormWeaver",            "downedStormWeaver",        null),
              new("Signus",                 "downedSignus",             null),
              new("Polterghast",            "downedPolterghast",        "downedPolterghast"),
              new("Sentinel1",              null,                       "downedSentinel1"),
              new("Sentinel2",              null,                       "downedSentinel2"),
              new("Sentinel3",              null,                       "downedSentinel3"),
              new("OldDuke",                "downedBoomerDuke",         "downedOldDuke"),
              new("Bumblebirb",             null,                       "downedBumble"),
              new("BuffedMothron",          null,                       "downedBuffedMothron"),
              new("DevourerOfGods",         "downedDoG",                "downedDoG"),
              new("Yharon",                 "downedYharon",             "downedYharon"),
              new("Ares",                   "downedAres",               null),
              new("Thanatos",               "downedThanatos",           null),
              new("ArtemisAndApollo",       "downedArtemisAndApollo",   null),
              new("ExoMechs",               "downedExoMechs",           null),
              new("SupremeCalamitas",       "downedCalamitas",          "downedSCal"),
              new("PrimordialWyrm",         "downedPrimordialWyrm",     null),
              new("Lorde",                  null,                       "downedLORDE"),
              new("Clam",                   null,                       "downedCLAM"),
              // 原版进度标记（Classic 特有）
              new("BrainOfCthulhuOrEoW",    null,                       "downedWhar"),
              new("Skeletron",              null,                       "downedSkullHead"),
              new("WallOfFlesh",            null,                       "downedUgly"),
              new("SkeletronPrime",         null,                       "downedSkeletor"),
              new("Plantera",               null,                       "downedPlantThing"),
              new("Golem",                  null,                       "downedGolemBaby"),
              new("MoonLord",               null,                       "downedMoonDude"),
              new("Betsy",                  null,                       "downedBetsy")
        ];
        /// <summary>
        /// 现代版 CalamityMod 的击杀标记属性缓存（Boss 键 → PropertyInfo）；模组未安装或类型缺失时为 null
        /// </summary>
        private static Dictionary<string, PropertyInfo> _calamityProps;
        /// <summary>
        /// 经典版 CalamityModClassicPreTrailer 的击杀标记字段缓存；模组未安装或类型缺失时为 null
        /// </summary>
        private static Dictionary<string, FieldInfo> _classicFields;
        /// <summary>
        /// 是否已完成惰性初始化，避免每帧重复反射
        /// </summary>
        private static bool _initialized;
        /// <summary>
        /// Links 按键（规范化 Boss 名）展开成的字典，供 O(1) 查找
        /// </summary>
        private static Dictionary<string, BossLink> _links;
        // ── 属性 ──
        /// <summary>
        /// 当前生效的数据源名称（"CalamityMod" / "CalamityModClassicPreTrailer"），两者皆不可用时返回 null，供调试/日志使用
        /// </summary>
        public static string ActiveSource
        {
            get
            {
                EnsureInitialized();
                if (_calamityProps != null)
                    return "CalamityMod";
                if (_classicFields != null)
                    return "CalamityModClassicPreTrailer";
                return null;
            }
        }
        /// <summary>
        /// 以下为各 Boss 的便捷查询属性（"BossAny" 表示任意 Boss 已被击败）；
        /// 实现均为 TryGetBossDowned("Boss键", out v) && v —— 查询失败时按未击败处理。
        /// </summary>
        public static bool AquaticScourge => TryGetBossDowned("AquaticScourge", out bool v) && v;
        public static bool Ares => TryGetBossDowned("Ares", out bool v) && v;
        public static bool ArtemisAndApollo => TryGetBossDowned("ArtemisAndApollo", out bool v) && v;
        public static bool Astrageldon => TryGetBossDowned("Astrageldon", out bool v) && v;
        public static bool AstrumAureus => TryGetBossDowned("AstrumAureus", out bool v) && v;
        public static bool AstrumDeus => TryGetBossDowned("AstrumDeus", out bool v) && v;
        public static bool Betsy => TryGetBossDowned("Betsy", out bool v) && v;
        public static bool BossAny => TryGetBossDowned("BossAny", out bool v) && v;
        public static bool BrimstoneElemental => TryGetBossDowned("BrimstoneElemental", out bool v) && v;
        public static bool BuffedMothron => TryGetBossDowned("BuffedMothron", out bool v) && v;
        public static bool Bumblebirb => TryGetBossDowned("Bumblebirb", out bool v) && v;
        public static bool Calamitas => TryGetBossDowned("Calamitas", out bool v) && v;
        public static bool CeaselessVoid => TryGetBossDowned("CeaselessVoid", out bool v) && v;
        public static bool Clam => TryGetBossDowned("Clam", out bool v) && v;
        public static bool Crabulon => TryGetBossDowned("Crabulon", out bool v) && v;
        public static bool Cryogen => TryGetBossDowned("Cryogen", out bool v) && v;
        public static bool DesertScourge => TryGetBossDowned("DesertScourge", out bool v) && v;
        public static bool DevourerOfGods => TryGetBossDowned("DevourerOfGods", out bool v) && v;
        public static bool Dragonfolly => TryGetBossDowned("Dragonfolly", out bool v) && v;
        public static bool ExoMechs => TryGetBossDowned("ExoMechs", out bool v) && v;
        public static bool Golem => TryGetBossDowned("Golem", out bool v) && v;
        public static bool Guardians => TryGetBossDowned("Guardians", out bool v) && v;
        public static bool HiveMind => TryGetBossDowned("HiveMind", out bool v) && v;
        public static bool Leviathan => TryGetBossDowned("Leviathan", out bool v) && v;
        public static bool Lorde => TryGetBossDowned("Lorde", out bool v) && v;
        public static bool MoonLord => TryGetBossDowned("MoonLord", out bool v) && v;
        public static bool OldDuke => TryGetBossDowned("OldDuke", out bool v) && v;
        public static bool Perforator => TryGetBossDowned("Perforator", out bool v) && v;
        public static bool Plaguebringer => TryGetBossDowned("Plaguebringer", out bool v) && v;
        public static bool Plantera => TryGetBossDowned("Plantera", out bool v) && v;
        public static bool Polterghast => TryGetBossDowned("Polterghast", out bool v) && v;
        public static bool PrimordialWyrm => TryGetBossDowned("PrimordialWyrm", out bool v) && v;
        public static bool Providence => TryGetBossDowned("Providence", out bool v) && v;
        public static bool Ravager => TryGetBossDowned("Ravager", out bool v) && v;
        public static bool Scavenger => TryGetBossDowned("Scavenger", out bool v) && v;
        public static bool Sentinel1 => TryGetBossDowned("Sentinel1", out bool v) && v;
        public static bool Sentinel2 => TryGetBossDowned("Sentinel2", out bool v) && v;
        public static bool Sentinel3 => TryGetBossDowned("Sentinel3", out bool v) && v;
        public static bool Signus => TryGetBossDowned("Signus", out bool v) && v;
        public static bool Skeletron => TryGetBossDowned("Skeletron", out bool v) && v;
        public static bool SkeletronPrime => TryGetBossDowned("SkeletronPrime", out bool v) && v;
        public static bool SlimeGod => TryGetBossDowned("SlimeGod", out bool v) && v;
        public static bool StarGod => TryGetBossDowned("StarGod", out bool v) && v;
        public static bool StormWeaver => TryGetBossDowned("StormWeaver", out bool v) && v;
        public static bool SupremeCalamitas => TryGetBossDowned("SupremeCalamitas", out bool v) && v;
        public static bool Thanatos => TryGetBossDowned("Thanatos", out bool v) && v;
        public static bool WallOfFlesh => TryGetBossDowned("WallOfFlesh", out bool v) && v;
        public static bool Yharon => TryGetBossDowned("Yharon", out bool v) && v;
        // ── 嵌套类型 ──
        /// <summary>
        /// 规范化 Boss 键 → 各数据源成员名的映射项，仅 EnsureInitialized 反射时使用。
        /// </summary>
        private sealed record BossLink(string Key, string CalamityProp, string ClassicField);
        // ── 公开方法 ──
        /// <summary>
        /// 获取所有可查询 Boss 的击杀状态字典（键为 Boss 规范化键）
        /// </summary>
        public static Dictionary<string, bool> GetAllBossDownedStates()
        {
            EnsureInitialized();
            var result = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (_links == null)
                return result;
            foreach (string key in _links.Keys)
            {
                if (TryGetBossDowned(key, out bool v))
                    result[key] = v;
            }
            return result;
        }
        /// <summary>
        /// 查询指定 Boss 是否已被击败。
        /// 按现代版 → 经典版的优先级查找；均不可用时返回 false。
        /// </summary>
        /// <param name="bossKey">Boss 规范化键（如 "Providence"）</param>
        /// <param name="downed">输出：该 Boss 是否已击败</param>
        /// <returns>是否成功取得结果</returns>
        public static bool TryGetBossDowned(string bossKey, out bool downed)
        {
            downed = false;
            EnsureInitialized();
            if (_links == null || !_links.TryGetValue(bossKey, out BossLink link))
                return false;
            // 现代版优先，其次 Classic
            if (_calamityProps != null && link.CalamityProp != null
                && _calamityProps.TryGetValue(link.Key, out PropertyInfo prop))
            {
                downed = (bool)prop.GetValue(null);
                return true;
            }
            if (_classicFields != null && link.ClassicField != null
                && _classicFields.TryGetValue(link.Key, out FieldInfo field))
            {
                downed = (bool)field.GetValue(null);
                return true;
            }
            return false;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 惰性初始化：构建 Boss 键映射表，并通过反射读取两个灾厄变体的击杀标记成员。
        /// 读取失败（mod 未安装 / 类型不存在）时对应字典保持为空，不影响其他功能。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized)
                return;
            _initialized = true;
            _links = new Dictionary<string, BossLink>(StringComparer.Ordinal);
            // 将静态映射表 Links 灌入字典，便于后续按键（规范化 Boss 名）快速查找
            foreach (BossLink link in Links)
                _links[link.Key] = link;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Type type = calamity.Code.GetType("CalamityMod.DownedBossSystem");
                if (type != null)
                {
                    _calamityProps = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
                    foreach (BossLink link in Links)
                    {
                        if (link.CalamityProp == null)
                            continue;
                        PropertyInfo prop = type.GetProperty(link.CalamityProp, flags);
                        if (prop != null)
                            _calamityProps[link.Key] = prop;
                    }
                }
            }
            Mod classic = FindClassicMod();
            if (classic != null)
            {
                Type type = classic.Code.GetType("CalamityModClassicPreTrailer.CalamityWorldPreTrailer");
                if (type != null)
                {
                    _classicFields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
                    foreach (BossLink link in Links)
                    {
                        if (link.ClassicField == null)
                            continue;
                        FieldInfo field = type.GetField(link.ClassicField, flags);
                        if (field != null)
                            _classicFields[link.Key] = field;
                    }
                }
            }
        }
        /// <summary>
        /// 查找经典灾厄模组：优先精确匹配 "CalamityModClassicPreTrailer"，
        /// 其次匹配任何以 "CalamityModClassic" 开头的模组名（兼容不同命名版本）。
        /// </summary>
        private static Mod FindClassicMod()
        {
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod exact))
                return exact;
            foreach (Mod mod in ModLoader.Mods)
            {
                if (mod.Name.StartsWith("CalamityModClassic", StringComparison.OrdinalIgnoreCase))
                    return mod;
            }
            return null;
        }
    }
}
