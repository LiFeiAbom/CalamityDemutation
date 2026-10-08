using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 震爆手雷的本体爆炸（照灾厄 2.0 的 <c>ShockGrenadeExplosion</c>）：
    /// 贴图是共用隐形贴图、判定是**半径 100 的圆**、320×320 的判定框、存活 10 帧；
    /// 前两帧撒 50 颗电尘（132 / 264 各半），命中挂 3 秒「带电」；
    /// <c>localNPCHitCooldown = -1</c> 表示同一个敌人只吃这一次。
    /// </summary>
    internal class ShockGrenadeExplosion : ModProjectile
    {
        /// <summary>圆形判定半径（照源）</summary>
        private const float ExplosionRadius = 100f;

        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>320×320 判定框、穿透无限、不撞物块不撞水、存活 10 帧、同一敌人只命中一次</summary>
        public override void SetDefaults()
        {
            Projectile.width = 320;
            Projectile.height = 320;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>前两帧（timeLeft ≥ 8）撒 50 颗高速电尘</summary>
        public override void AI()
        {
            if (Projectile.timeLeft < 8)
                return;
            for (int i = 0; i < 50; i++)
            {
                int dustType = 132;
                if (Main.rand.NextBool())
                    dustType = 264;
                Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                dustVelocity.Normalize();
                dustVelocity *= 8f;
                int dust = Dust.NewDust(Projectile.Center, 1, 1, dustType, dustVelocity.X, dustVelocity.Y, 0, default, 0.75f);
                Main.dust[dust].noGravity = true;
            }
        }
        /// <summary>命中敌人挂 3 秒「带电」</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 180);
        }
        /// <summary>
        /// 圆形判定（内联灾厄 CollisionUtils.CircularHitboxCollision，写法照本工程 AbaddonCrit）：
        /// 圆心落在目标框内直接算命中；否则取圆心到目标框四角的最小距离与半径比较。
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle center = new Rectangle((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= ExplosionRadius;
        }
    }
}
