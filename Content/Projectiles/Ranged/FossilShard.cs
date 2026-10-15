using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 碎片（Shard） - 弩箭消亡时迸出的骨屑，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/FossilShard.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 10×10（贴图 10×12）、穿透 1、寿命 120 帧、<c>aiStyle = 1</c>（走原版箭 AI，<c>AIType</c> 用原版木箭）。
    /// AI 只做三件事：**不许往上飘**（<c>velocity.Y &lt;= 0</c> 时按 0.1 起步）、
    /// 每帧把自己的 Y 速度 **×1.05**（越掉越快）、自旋跟着 Y 速度走。
    /// </para>
    /// <para>
    /// 命中挂**破甲 `ArmorCrunch` 60 帧**；消亡撒 3 颗 <c>DustID.Sand</c> 尘（源写裸值 32）。
    /// </para>
    /// </summary>
    internal class FossilShard : ModProjectile
    {
        /// <summary>命中时挂破甲的帧数（照源 60）</summary>
        private const int ArmorCrunchFrames = 60;
        /// <summary>下落速度下限：非正时按此值起步（照源 0.1）</summary>
        private const float MinimumFallSpeed = 0.1f;
        /// <summary>下落加速：每帧把 Y 速度自乘这个系数（照源 1.05）</summary>
        private const float FallAcceleration = 1.05f;
        /// <summary>消亡时的尘埃枚数（照源 3）</summary>
        private const int DeathDustCount = 3;
        /// <summary>
        /// 基础属性：10×10 判定、穿透 1、走原版箭 AI、寿命 120 帧（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.aiStyle = ProjAIStyleID.Arrow;   // 源写裸值 1；工程要求命名常量
            Projectile.timeLeft = 120;
            AIType = ProjectileID.WoodenArrowFriendly;
        }
        /// <summary>AI：压住"往上飘" + 每帧加速下坠 + 自旋跟 Y 速度（照源）</summary>
        public override void AI()
        {
            if (Projectile.velocity.Y <= 0f)
                Projectile.velocity.Y = MinimumFallSpeed;
            Projectile.rotation += Projectile.velocity.Y;
            Projectile.velocity.Y *= FallAcceleration;
        }
        /// <summary>命中：挂破甲 60 帧（照源）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "ArmorCrunch", ArmorCrunchFrames, BuffID.BrokenArmor);
        }
        /// <summary>消亡：撒 3 颗尘，速度取死前速度的一半（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < DeathDustCount; i++)
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Sand,
                    Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
            }
        }
    }
}
