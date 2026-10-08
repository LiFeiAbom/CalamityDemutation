using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 破坏者的焦油火（照灾厄 2.0 的 <c>TotalityFire</c>）：
    /// 14×14 判定、穿透无限、存活 120 帧、同一敌人只吃一次（<c>localNPCHitCooldown = -1</c>）、
    /// 不熄于水；3 帧动画 + 2 点拖影；命中挂 3 秒「燃烧」。
    /// 撞物块不消失，只是把身体转成"贴着走"的姿态（源用 <c>ai[1] = 10</c> 标记）。
    /// <para>
    /// 与源唯一的有意差异：源把"随机起始帧"记在 <c>ModProjectile</c> 的私有 <c>bool initialized</c> 字段上，
    /// 而 tML 的 <c>ModProjectile</c> 是**每类型单例**（所有同种弹幕共用一个实例），那个字段其实是全局开关。
    /// 本工程改用 <c>Projectile.localAI[2]</c>（真正的每实例状态），效果与源的意图一致。
    /// </para>
    /// </summary>
    internal class TotalityFire : ModProjectile
    {
        /// <summary>火尘类型（源里是裸数字 6）</summary>
        private const int FireDust = 6;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>14×14、穿透无限、存活 120 帧、同一敌人只命中一次、不受水影响，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ignoreWater = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>朝向 + 3 帧动画 + 与焦油同款的摊开/下坠物理 + 火尘</summary>
        public override void AI()
        {
            if (Projectile.ai[1] > 0f)
                Projectile.rotation = -Projectile.velocity.X * 0.05f + MathHelper.PiOver2;
            else
                Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.ai[1]--;
            if (Projectile.localAI[2] == 0f)
            {
                Projectile.localAI[2] = 1f;
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.frame = 0;
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X *= 0.97f;
                    if (Projectile.velocity.X > -0.01f && Projectile.velocity.X < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f;
            }
            if (Projectile.velocity.Y < 0.25f && Projectile.velocity.Y > 0.15f)
                Projectile.velocity.X *= 0.8f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, FireDust, 0f, 0f, 100, default, 1f);
                Main.dust[dust].position.X -= 2f;
                Main.dust[dust].position.Y += 2f;
                Main.dust[dust].scale += Main.rand.Next(50) * 0.01f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity.Y -= 2f;
            }
            if (Main.rand.NextBool(10))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, FireDust, 0f, 0f, 100, default, 1f);
                Main.dust[dust].position.X -= 2f;
                Main.dust[dust].position.Y += 2f;
                Main.dust[dust].scale += 0.3f + Main.rand.Next(50) * 0.01f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
        }
        /// <summary>撞物块不消失，只是标记 ai[1] 让它改成"贴地姿态"（照源）</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.ai[1] = 10f;
            return false;
        }
        /// <summary>命中敌人挂 3 秒「燃烧」</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 120);
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 120);
        }
        /// <summary>2 点拖影（照源调灾厄的 DrawAfterimagesCentered，本工程用同名的 CDUtil 版本）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
