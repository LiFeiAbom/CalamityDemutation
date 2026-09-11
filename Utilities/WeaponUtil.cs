using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（武器部分）：提供近战武器挥舞位置修正等扩展方法
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 修正近战武器的挥舞位置（BetterSwing）：
        /// 依据挥砍动画的进度偏移武器位置，使巨剑类武器的挥舞更贴合视觉
        /// 起始阶段稍靠前上方、中段居中、后段略偏后，倒立（重力反转）时对称处理。
        /// </summary>
        public static void BetterSwing(this Player player)
        {
            // 默认偏移：武器略微靠前（xOffset>0）并抬高（yOffset<0 表示向上），贴近巨剑挥舞观感
            float xOffset = 6f;
            float yOffset = -10f;
            // itemAnimation 随时间递减：挥砍收尾（<1/3）时武器下压贴近身体；刚出手（≥2/3）时武器略微后拉
            if (player.itemAnimation < player.itemAnimationMax * 0.333f)
                yOffset = 4f;
            else if (player.itemAnimation >= player.itemAnimationMax * 0.666f)
                xOffset = -4f;
            player.itemLocation.X = player.Center.X + xOffset * player.direction;
            player.itemLocation.Y = player.MountedCenter.Y + yOffset;
            // 重力反转（倒立状态）时，沿 Y 轴对称翻转武器位置，保证挥砍方向视觉一致
            if (player.gravDir < 0)
                player.itemLocation.Y = player.Center.Y + (player.position.Y - player.itemLocation.Y);
        }
        /// <summary>
        /// 为目标 NPC 附加“全套混合元素减益”：先上 4 种原版减益各 5 秒，
        /// 若安装了现代版/经典版灾厄，再补上灾厄专属元素减益；
        /// 经典版灾厄另有低概率（1/30）附带极寒冻结（ExoFreeze）。
        /// </summary>
        public static void ExoDebuffs(this NPC target)
        {
            // 原版基础元素减益（各 300 帧 = 5 秒）：霜火、着火、诅咒地狱、灵液
            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(BuffID.CursedInferno, 300);
            target.AddBuff(BuffID.Ichor, 300);
            // 现代版灾厄：附加灾厄专属元素减益（500 帧 ≈ 8.3 秒）
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 500); }
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight)) { target.AddBuff(miracleBlight.Type, 500); }
                if (calamity.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 500); }
                if (calamity.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 500); }
            }
            // 经典版灾厄：稳定附加 4 种元素减益，且有 1/30 概率额外附加极寒冻结
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (Main.rand.NextBool(30))
                {
                    if (calamity1.TryFind<ModBuff>("ExoFreeze", out ModBuff exoFreeze)) { target.AddBuff(exoFreeze.Type, 300); }
                }
                if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 300); }
                if (calamity1.TryFind<ModBuff>("GlacialState", out ModBuff glacialState)) { target.AddBuff(glacialState.Type, 300); }
                if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 300); }
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 300); }
            }
        }
    }
}
