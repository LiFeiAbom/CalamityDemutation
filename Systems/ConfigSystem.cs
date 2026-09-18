using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// 模组配置类，提供客户端侧的配置选项。
    /// 当前配置项：性能模式、挥砍刀光、武器自适应光照、原版数值回调。
    /// </summary>
    internal class ConfigSystem : ModConfig
    {
        /// <summary>
        /// 当前生效的配置单例，由 OnLoaded 在配置载入后填充，供全局读取
        /// </summary>
        public static ConfigSystem Instance;
        /// <summary>
        /// 配置作用域：客户端侧，仅影响本机显示/性能类选项，不参与多人同步
        /// </summary>
        public override ConfigScope Mode => ConfigScope.ClientSide;
        /// <summary>
        /// 性能模式开关：启用后禁用弹幕残影/拖尾渲染，
        /// 在弹幕数量庞大时可显著降低绘制开销（详见 DrawAfterimages 的实现）。
        /// </summary>
        [BackgroundColor(211, 211, 211, 192)]
        [DefaultValue(false)]
        public bool PerformanceMode { get; set; }
        /// <summary>
        /// 回退灾厄削弱开关：启用后，在加载现代版灾厄（CalamityMod）时，
        /// 把灾厄对原版玩家装备（武器/防具/工具/翅膀/饰品/价值）与部分原版增益 buff 的削弱恢复为原版数值。
        /// </summary>
        [BackgroundColor(211, 211, 211, 192)]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool RevertVanillaNerfs { get; set; }
        /// <summary>
        /// 回退灾厄内容削弱开关：启用后，在加载现代版灾厄（CalamityMod）时，回退灾厄对自身内容的削弱。
        /// </summary>
        [BackgroundColor(211, 211, 211, 192)]
        [DefaultValue(false)]
        public bool RevertCalamityContentNerfs { get; set; }
        /// <summary>
        /// 是否启用挥砍刀光（BaseSwingCO 弧光渲染开关）
        /// </summary>
        [BackgroundColor(192, 54, 94, 192)]
        [DefaultValue(true)]
        public bool EnableSwordLight { get; set; }
        /// <summary>
        /// 武器自适应光照（关闭时刀身强制白色）
        /// </summary>
        [BackgroundColor(192, 54, 94, 192)]
        [DefaultValue(true)]
        public bool WeaponAdaptiveIllumination { get; set; }
        /// <summary>
        /// 配置加载完成钩子：由 tModLoader 在配置读入后自动调用，缓存配置单例
        /// </summary>
        public override void OnLoaded()
        {
            Instance = this;
        }
    }
}
