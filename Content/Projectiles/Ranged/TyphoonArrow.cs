using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 台风箭（Typhoon Arrow） - 季风把木箭转化出的五支里的**第 3 支**，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/TyphoonArrow.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 14×14、穿透 1、<c>arrow = true</c>、<c>aiStyle = 1</c>（走原版箭 AI，<c>AIType</c> 用原版木箭）、
    /// 寿命 600 帧；贴图 14×32。消亡时在主人端原地生成一枚 <see cref="TyphoonBubble"/>，
    /// 由它托出整根水柱。
    /// </para>
    /// <para>
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做。
    /// </para>
    /// </summary>
    internal class TyphoonArrow : ModProjectile
    {
        /// <summary>基础属性：14×14 判定、穿透 1、走原版箭 AI、寿命 600 帧、算作"箭"（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = ProjAIStyleID.Arrow;   // 源写裸值 1；工程要求命名常量
            Projectile.timeLeft = 600;
            AIType = ProjectileID.WoodenArrowFriendly;
        }
        /// <summary>消亡：主人端原地生成 <see cref="TyphoonBubble"/>（伤害 / 击退照弹幕自身）——照源</summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                ModContent.ProjectileType<TyphoonBubble>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
    }
}
