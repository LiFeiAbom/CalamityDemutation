using System.Collections.Generic;
using Terraria.GameInput;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// 按键绑定系统：注册本模组的自定义快捷键，并在卸载时清空静态引用。
    /// 全部快捷键都只在 Players/CalamityDemutationPlayer.ProcessTriggers（或冲刺请求入口）里被读取消费，
    /// 本类只负责注册/注销与显示名，不处理任何触发逻辑。
    /// </summary>
    internal class KeybindsSystem:ModSystem
    {
        /// <summary>弑神者冲刺键的注册默认键（与 GodslayerDashKeyDisplay 回退共用，避免字面量漂移）</summary>
        private const string GodslayerDashDefaultKey = "H";
        /// <summary>极乐之庇护守护状态键的注册默认键（与 ElysianKeyDisplay 回退共用，避免字面量漂移）</summary>
        private const string ElysianDefaultKey = "N";
        /// <summary>
        /// 套装主动技能快捷键，默认 Y。由 CalamityDemutationPlayer.ProcessTriggers 消费：
        /// 依次判定恶魔阴影套装（施加狂怒 buff 并迸发吸魂尘埃）、欧米伽蓝、塔拉近战等套装效果。
        /// </summary>
        public static ModKeybind DemonshadeHotKey { get; private set; }
        /// <summary>
        /// 极乐之庇护守护状态快捷键，默认 N。由 CalamityDemutationPlayer.ProcessTriggers 消费：
        /// 装备极乐之庇护时切换守护状态（elysianGuard）——开启后护盾充能随时间消耗，
        /// 并按消耗量转化为伤害/暴击率/仇恨/防御（见 if(elysianAegispower) 块）；坐骑上强制关闭。
        /// </summary>
        public static ModKeybind ElysianHotKey { get; private set; }
        /// <summary>
        /// 弑神者冲刺快捷键，默认 H。同样在 CalamityDemutationPlayer.ProcessTriggers 中消费，
        /// 交给本模组自持的弑神者冲刺（RequestGodSlayerDash）。
        /// </summary>
        public static ModKeybind GodslayerDashHotKey { get; private set; }
        /// <summary>
        /// 蓝色欧米茄套装技能快捷键，默认 Y。由 ProcessTriggers 消费：穿着整套蓝色欧米茄且冷却归零时触发，
        /// 置 1800 帧冷却、播放音效并喷一圈净化尘。
        /// </summary>
        public static ModKeybind OmegaBlueHotKey { get; private set; }
        /// <summary>
        /// 龙蒿近战防御形态快捷键，默认 Y。由 ProcessTriggers 消费：穿着龙蒿近战套且冷却归零时进入
        /// 防御形态（tarraDefense）。
        /// </summary>
        public static ModKeybind TarragonHotKey { get; private set; }
        /// <summary>
        /// 弑神者冲刺键的可读显示名：把已绑定的键用 "/" 连接，未绑定时回退为注册默认键 "H"。
        /// 供套装 setBonus 等提示文本动态引用，玩家改键后提示随之更新。
        /// </summary>
        public static string GodslayerDashKeyDisplay
        {
            get
            {
                List<string> assigned = GodslayerDashHotKey?.GetAssignedKeys(InputMode.Keyboard);
                return assigned != null && assigned.Count > 0 ? string.Join("/", assigned) : GodslayerDashDefaultKey;
            }
        }
        /// <summary>
        /// 极乐之庇护守护状态键的可读显示名：与 <see cref="GodslayerDashKeyDisplay"/> 同一套写法
        /// （已绑定的键用 "/" 连接），未绑定时回退为注册默认键 "N"。
        /// 供极乐之庇护的 tooltip 把 [KEY] 占位替换成实际绑定键。
        /// </summary>
        public static string ElysianKeyDisplay
        {
            get
            {
                List<string> assigned = ElysianHotKey?.GetAssignedKeys(InputMode.Keyboard);
                return assigned != null && assigned.Count > 0 ? string.Join("/", assigned) : ElysianDefaultKey;
            }
        }
        /// <summary>
        /// 模组加载时注册全部快捷键：每个键给注册名与默认键位字符串，
        /// 显示名由 Localization 的 Keybinds.&lt;注册名&gt;.DisplayName 提供（两份 hjson 都要有）
        /// </summary>
        public override void Load()
        {
            DemonshadeHotKey = KeybindLoader.RegisterKeybind(Mod, "Demonshade", "Y");// 套装主动技能：默认 Y
            ElysianHotKey = KeybindLoader.RegisterKeybind(Mod, "Elysian", ElysianDefaultKey);// 极乐之庇护守护状态：默认 N
            GodslayerDashHotKey = KeybindLoader.RegisterKeybind(Mod, "GodslayerDash", GodslayerDashDefaultKey);// 弑神者冲刺：默认 H
            OmegaBlueHotKey = KeybindLoader.RegisterKeybind(Mod, "OmegaBlue", "Y");// 蓝色欧米茄套装技能：默认 Y
            TarragonHotKey = KeybindLoader.RegisterKeybind(Mod, "Tarragon", "Y");// 龙蒿防御形态：默认 Y
        }
        /// <summary>卸载时把静态引用置空，防止残留引用导致下次加载拿到失效对象。</summary>
        public override void Unload()
        {
            DemonshadeHotKey = null;
            ElysianHotKey = null;
            GodslayerDashHotKey = null;
            OmegaBlueHotKey = null;
            TarragonHotKey = null;
        }
    }
}
