using System.Collections.Generic;
using Terraria.GameInput;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// 按键绑定系统：注册本模组的自定义快捷键，并在卸载时清空静态引用。
    /// 两个键都只在 Players/CalamityDemutationPlayer.ProcessTriggers 里被读取消费，本类不处理触发逻辑。
    /// </summary>
    internal class KeybindsSystem:ModSystem
    {
        /// <summary>
        /// 套装主动技能快捷键，默认 Y。由 CalamityDemutationPlayer.ProcessTriggers 消费：
        /// 依次判定恶魔阴影套装（施加狂怒 buff 并迸发吸魂尘埃）、欧米伽蓝、塔拉近战等套装效果。
        /// </summary>
        public static ModKeybind DemonshadeHotKey { get; private set; }
        /// <summary>
        /// 弑神者冲刺快捷键，默认 H。同样在 CalamityDemutationPlayer.ProcessTriggers 中消费，
        /// 交给本模组自持的弑神者冲刺（RequestGodSlayerDash）。
        /// </summary>
        public static ModKeybind GodslayerDashHotKey { get; private set; }
        /// <summary>
        /// 弑神者冲刺键的可读显示名：把已绑定的键用 "/" 连接，未绑定时回退为注册默认键 "H"。
        /// 供套装 setBonus 等提示文本动态引用，玩家改键后提示随之更新。
        /// </summary>
        public static string GodslayerDashKeyDisplay
        {
            get
            {
                List<string> assigned = GodslayerDashHotKey.GetAssignedKeys(InputMode.Keyboard);
                return assigned.Count > 0 ? string.Join("/", assigned) : "H";
            }
        }
        /// <summary>模组加载时注册两个快捷键：注册名与默认键位字符串。</summary>
        public override void Load()
        {
            DemonshadeHotKey = KeybindLoader.RegisterKeybind(Mod, "Demonshade", "Y");// 套装主动技能：默认 Y
            GodslayerDashHotKey = KeybindLoader.RegisterKeybind(Mod, "GodslayerDash", "H");// 弑神者冲刺：默认 H
        }
        /// <summary>卸载时把静态引用置空，防止残留引用导致下次加载拿到失效对象。</summary>
        public override void Unload()
        {
            DemonshadeHotKey = null;
            GodslayerDashHotKey = null;
        }
    }
}
