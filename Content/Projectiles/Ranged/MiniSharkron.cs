using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 小鲨鱼龙（Mini Sharkron） - 季风把木箭转化出的四支"鲨鱼箭"之一，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/MiniSharkron.cs</c>（逐行照搬）。
    /// <para>
    /// 它其实是**原版小鲨鱼龙的模组皮**：判定 10×10、<c>aiStyle = 1</c>（箭）但
    /// <c>AIType = <see cref="ProjectileID.MiniSharkron"/></c>（借原版鲨鱼龙的飞行 AI）、
    /// <c>alpha = 255</c>（自绘淡入由 AI 负责）、不受水阻；贴图 54×56（2 帧）。
    /// </para>
    /// <para>
    /// 消亡：撒 15 颗血尘（照源 50×50 的散布框）+ 在非服务端生成**三段原版鱼尸**
    /// （编号 584 / 585 / 586，把寿命除以 10 让它迅速消失）。
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做。
    /// </para>
    /// </summary>
    internal class MiniSharkron : ModProjectile
    {
        /// <summary>动画帧数（照源 2）</summary>
        private const int AnimationFrames = 2;
        /// <summary>死亡时的血尘颗粒数（照源 15）</summary>
        private const int BloodDustCount = 15;
        /// <summary>三段原版鱼尸的贴图编号（照源裸值 584/585/586：鱼尾 / 鱼身 / 鱼头）</summary>
        private const int GoreTail = 584;
        private const int GoreBody = 585;
        private const int GoreHead = 586;
        /// <summary>两帧动画（照源）</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = AnimationFrames;
        /// <summary>
        /// 基础属性：10×10 判定、走箭的 AI 但借原版小鲨鱼龙的 <c>AIType</c>、初始全透明、不受水阻（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = ProjAIStyleID.Arrow;   // 源写裸值 1；工程要求命名常量（0 警告基线）
            AIType = ProjectileID.MiniSharkron;         // 借原版鲨鱼龙 AI
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }
        /// <summary>消亡：血尘 + 三段快速消失的原版鱼尸（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < BloodDustCount; i++)
            {
                int index = Dust.NewDust(Projectile.Center - Vector2.One * 10f, 50, 50, DustID.Blood, 0f, -2f, 0, default, 1f);
                Main.dust[index].velocity /= 2f;
            }
            if (Main.netMode != NetmodeID.Server)
            {
                int tail = Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Projectile.velocity * 0.8f, GoreTail, 1f);
                Main.gore[tail].timeLeft /= 10;
                int body = Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Projectile.velocity * 0.9f, GoreBody, 1f);
                Main.gore[body].timeLeft /= 10;
                int head = Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Projectile.velocity * 1f, GoreHead, 1f);
                Main.gore[head].timeLeft /= 10;
            }
        }
    }
}
