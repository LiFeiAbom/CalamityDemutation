using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 弩箭（Ballista Great Arrow） - 弩炮把木箭转化出的重箭，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/BallistaGreatArrow.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 10×10、**穿透 2**、<c>arrow = true</c>、<c>aiStyle = 1</c>（走原版箭 AI，<c>AIType</c> 用原版木箭）、
    /// 10 帧残影；贴图 18×64（判定远小于贴图，视觉上是根长重箭）。
    /// </para>
    /// <para>
    /// 命中：把目标的无敌帧**压到 12 帧**并挂**破甲 `ArmorCrunch` 180 帧**；
    /// 撞墙：砸瓦片尘 + 一声 `Dig` 后消亡（返回 true）。
    /// 消亡：一声 `Item14`，并在主人端朝**两侧各撒 3 枚 <see cref="FossilShard"/>**
    /// （伤害 ×0.2、击退 ÷5，碎片自带破甲 60 帧）。
    /// </para>
    /// <para>
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做。
    /// 经典版的穿透 3 / 破甲 360 帧 / 碎片伤害 ×0.75 / 碎片击退不打折——**一律不取**
    /// （用户 2026-10-15：机制取 2.0）。
    /// </para>
    /// </summary>
    internal class BallistaGreatArrow : ModProjectile
    {
        /// <summary>残影缓存帧数（照源 10）</summary>
        private const int TrailCacheLength = 10;
        /// <summary>命中时给目标压的无敌帧数（照源 12）</summary>
        private const int ImmuneFramesOnHit = 12;
        /// <summary>命中时挂破甲的帧数（照源 180）</summary>
        private const int ArmorCrunchFrames = 180;
        /// <summary>消亡时朝每侧撒的碎片枚数（照源 3）</summary>
        private const int ShardsPerSide = 3;
        /// <summary>碎片撒布的扇形角（照源 <c>90f * 0.0174f</c>）</summary>
        private const float ShardSpread = 90f * 0.0174f;
        /// <summary>碎片初速（照源 5）</summary>
        private const float ShardVelocity = 5f;
        /// <summary>碎片伤害倍率（照 2.0 的 0.2；经典版是 0.75）</summary>
        private const float ShardDamageMult = 0.2f;
        /// <summary>碎片击退除数（照 2.0 的 ÷5；经典版不打折）</summary>
        private const float ShardKnockbackDivisor = 5f;
        /// <summary>残影：缓存 10 帧、模式 1（圣骑士锤式渐隐，配合下面的 <c>PreDraw</c>）——照源</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailCacheLength;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>基础属性：10×10 判定、穿透 2、走原版箭 AI、算作"箭"（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 2;                   // 穿透 2（照 2.0；经典版是 3）
            Projectile.aiStyle = ProjAIStyleID.Arrow;   // 源写裸值 1；工程要求命名常量
            AIType = ProjectileID.WoodenArrowFriendly;
        }
        /// <summary>撞墙：砸瓦片尘 + 一声 `Dig`，然后消亡（返回 true）——照源</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            return true;
        }
        /// <summary>命中：压 12 帧无敌 + 挂破甲 180 帧（照源）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = ImmuneFramesOnHit;
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "ArmorCrunch", ArmorCrunchFrames, BuffID.BrokenArmor);
        }
        /// <summary>
        /// 消亡：一声 `Item14` + 主人端朝两侧各撒 <see cref="ShardsPerSide"/> 枚 <see cref="FossilShard"/>
        /// （照源那套"以速度方向为轴、按 <c>(i + i²)/2 + 32i</c> 排角"的写法）
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            if (Projectile.owner != Main.myPlayer)
                return;

            double startAngle = Math.Atan2(Projectile.velocity.X, Projectile.velocity.Y) - ShardSpread / 2;
            double deltaAngle = ShardSpread / 8f;
            for (int i = 0; i < ShardsPerSide; i++)
            {
                double offsetAngle = startAngle + deltaAngle * (i + i * i) / 2f + 32f * i;
                SpawnShard((float)(Math.Sin(offsetAngle) * ShardVelocity), (float)(Math.Cos(offsetAngle) * ShardVelocity));
                SpawnShard((float)(-Math.Sin(offsetAngle) * ShardVelocity), (float)(-Math.Cos(offsetAngle) * ShardVelocity));
            }
        }
        /// <summary>按给定速度撒一枚碎片（伤害 ×0.2、击退 ÷5，照 2.0）</summary>
        private void SpawnShard(float speedX, float speedY) => Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y,
            speedX, speedY, ModContent.ProjectileType<FossilShard>(), (int)(Projectile.damage * ShardDamageMult), Projectile.knockBack / ShardKnockbackDivisor, Projectile.owner);
        /// <summary>自绘：按残影缓存画一串位置残影（照源用灾厄的 DrawAfterimagesCentered，本工程用同名移植件）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 2);
            return false;
        }
    }
}
