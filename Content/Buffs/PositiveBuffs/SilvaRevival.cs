using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 森林复苏（SilvaRevival） - 正面增益，森林套（silvaSet）免死保护窗口的显示标记
    /// 当森林套的保命窗口在 PreKill 中生效时赋予 600 tick，生命上限耗尽时被清除；
    /// 本类不含 Update 逻辑，免死效果实际由 ModPlayer 的 silvaSet/silvaCountdown 等字段结算。
    /// </summary>
    internal class SilvaRevival:ModBuff
    {
        /// <summary>
        /// 注册为正面增益：允许 PvP 传播且随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 非减益：图标按增益（buff）显示
            Main.debuff[Type] = false;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 随存档保存：显式设为 false，表示该状态需要跨存档保留
            Main.buffNoSave[Type] = false;
            // 专家/大师模式下不延长时长
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }
    }
}
