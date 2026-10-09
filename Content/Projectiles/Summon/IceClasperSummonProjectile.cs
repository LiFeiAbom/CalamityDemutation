using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 冰灵喷出的冰锥（照灾厄 2.0.3.9 <c>Projectiles/Summon/IceClasperSummonProjectile.cs</c> 移植）：
    /// 28×28 判定、寿命 300 帧、召唤伤害、可穿水不撞地形；命中挂 **霜冻 180 帧**，飞行带冰尘与残影。
    /// </summary>
    internal class IceClasperSummonProjectile:ModProjectile
    {
        /// <summary>属于仆从弹药（MinionShot），并开残影缓存 5 格（照源）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
        }
        /// <summary>基础属性：照源（28×28、寿命 300、召唤伤害、不撞地形、穿水）</summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 300;
            Projectile.width = Projectile.height = 28;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        /// <summary>朝速度方向对齐（+90°），并留下冰晶尘（源写裸值 172，即 DustID.DungeonWater）</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ)
            {
                Dust trailDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.DungeonWater, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.5f);
                trailDust.noGravity = true;
            }
        }
        /// <summary>命中挂霜冻 180 帧（照源）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Frostburn, 180);
        /// <summary>先画残影再走常规绘制（照源；残影受性能开关控制）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return true;
        }
    }
}
