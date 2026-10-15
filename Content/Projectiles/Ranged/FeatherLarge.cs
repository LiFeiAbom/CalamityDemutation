using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 羽毛（FeatherLarge） - 烈风洒出的低伤羽毛，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/FeatherLarge.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 14×14（贴图 14×34）、穿透 1、寿命 300 帧；**没有 aiStyle**（自定义 AI 只做一件事：
    /// 按速度摆正贴图）；6 帧残影（模式 0、抽稀 1）。
    /// 撞墙：砸瓦片尘 + 一声 `Dig` 后消亡；消亡：撒 10 颗 `DustID.Harpy`（源写裸值 42）尘。
    /// </para>
    /// <para>
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做；
    /// 源的 <c>+ 1.57f</c> 换成同值的 <see cref="MathHelper.PiOver2"/>。
    /// </para>
    /// </summary>
    internal class FeatherLarge : ModProjectile
    {
        /// <summary>残影缓存帧数（照源 6）</summary>
        private const int TrailCacheLength = 6;
        /// <summary>消亡时的尘埃枚数（照源 10）</summary>
        private const int DeathDustCount = 10;
        /// <summary>残影：缓存 6 帧、模式 0（普通渐隐，配合下面的 <c>PreDraw</c>）——照源</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailCacheLength;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>基础属性：14×14 判定、穿透 1、寿命 300 帧（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
        }
        /// <summary>AI：只按速度摆正贴图（照源）</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>撞墙：砸瓦片尘 + 一声 `Dig` 后消亡（返回 true）——照源</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            return true;
        }
        /// <summary>自绘：按残影缓存画一串位置残影（照源用灾厄的 DrawAfterimagesCentered，本工程用同名移植件）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>消亡：撒 10 颗尘，速度取自身速度的十分之一（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < DeathDustCount; i++)
            {
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height,
                    DustID.Harpy, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 0, default, 1f);
            }
        }
    }
}
