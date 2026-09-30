using System.ComponentModel;
using Terraria.ModLoader.Config;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// 服务端侧配置：影响世界内实际数值的选项（联机时以主机为准，由 tModLoader 同步给客户端）。
    /// 与 <see cref="ConfigSystem"/>（客户端侧、只管显示与性能）分开放，
    /// 避免把会影响战斗数值的开关塞进只对本机生效的客户端配置里。
    /// 目前只有一个开关：数值膨胀（武器数值与 BOSS 血量统一挂在它下面）。
    /// </summary>
    public class GameplayConfig : ModConfig
    {
        /// <summary>当前生效的配置单例，由 OnLoaded 在配置载入后填充，供全局读取</summary>
        public static GameplayConfig Instance;
        /// <summary>配置作用域：服务端侧（联机时主机说了算）</summary>
        public override ConfigScope Mode => ConfigScope.ServerSide;
        /// <summary>
        /// 数值膨胀总开关：**武器数值与 BOSS 血量共用这一个开关**（口径照 CI（灾厄遗产）的做法：
        /// 一个开关统管一整套旧版数值回调，而不是一项一个开关）。
        /// <para>
        /// BOSS 血量侧已实现在 <see cref="CalamityDemutation.NPCs.CalamityDemutationGlobalNPC.SetDefaults"/>：
        /// 所有 <c>npc.boss</c> 为真的目标（原版 / 灾厄 / 本模组）基础血量统一乘以
        /// <see cref="CalamityDemutation.NPCs.CalamityDemutationGlobalNPC.BossHealthMultiplier"/>。
        /// 武器侧由本模组的武器代码自行读取本开关（建议统一走 <see cref="StatInflationEnabled"/>）。
        /// </para>
        /// </summary>
        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(false)]
        public bool StatInflation { get; set; }
        /// <summary>
        /// 数值膨胀是否启用的静态便捷入口：武器侧与 BOSS 侧代码统一读这一处，
        /// 省掉每处都写 <c>Instance is null</c> 判空（配置尚未载入时按"未启用"处理）。
        /// </summary>
        public static bool StatInflationEnabled => Instance?.StatInflation ?? false;
        /// <summary>配置加载完成钩子：缓存配置单例</summary>
        public override void OnLoaded()
        {
            Instance = this;
        }
    }
}
