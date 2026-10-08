using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 破坏者的黑焦油团（照灾厄 2.0 的 <c>TotalityTar</c>）：
    /// 14×14 判定、穿透无限、存活 30 帧、同一敌人每 10 帧可再吃一次；
    /// 落地会摊开（速度衰减 + 重力），命中把敌人**油浸**（<c>Oiled</c> 10 秒）并点燃 4 秒；
    /// 消失时爆开成 2~3 团火（伤害等于本团）。
    /// <para>
    /// 命中那处"先清掉敌人的油浸免疫"是源里的刻意写法（灾厄自己给不少敌人设了 Oiled 免疫，
    /// 这套武器要硬吃），照抄。
    /// </para>
    /// <para>
    /// 源 AI 开头那几个 <c>velocity.X != velocity.X</c> 是 NaN 自检（NaN 不等于自身），
    /// 逻辑上永远不成立——**照源原样保留**，不"顺手修"（免得和源的物理表现对不上）。
    /// </para>
    /// </summary>
    internal class TotalityTar : ModProjectile
    {
        /// <summary>火尘类型（源里是裸数字 6）</summary>
        private const int FireDust = 6;

        /// <summary>14×14、穿透无限、存活 30 帧、同一敌人每 10 帧可再命中，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>摊开与下坠（照源逐行）：反弹衰减 → 5 帧后启动空气阻力与重力 → 按水平速度自转</summary>
        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X *= 0.97f;
                    if (Math.Abs(Projectile.velocity.X) < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;
            if (Projectile.velocity.Y < 0.25f && Projectile.velocity.Y > 0.15f)
                Projectile.velocity.X *= 0.8f;
            Projectile.rotation = -Projectile.velocity.X * 0.05f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;
        }
        /// <summary>命中：先解除油浸免疫再挂 10 秒「油浸」，另挂 4 秒「燃烧」（照源）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.buffImmune[BuffID.Oiled])
                target.buffImmune[BuffID.Oiled] = false;
            target.AddBuff(BuffID.Oiled, 600);
            target.AddBuff(BuffID.OnFire, 240);
        }
        /// <summary>PvP 只挂 4 秒「燃烧」（源里没有油浸那段）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 240);
        }
        /// <summary>消失：一声响（原版 Item74）+ 三层火尘 + 2~3 团火（伤害等于本团，主人端生成）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
            Vector2 size = new Vector2(20f, 20f);
            for (int i = 0; i < 3; i++)
                Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, DustID.SpookyWood, 0f, 0f, 0, Color.Red, 1f);
            for (int i = 0; i < 5; i++)
                Main.dust[Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, DustID.Smoke, 0f, 0f, 100, new Color(), 1.5f)].velocity *= 1.4f;
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, FireDust, 0f, 0f, 100, new Color(), 2.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, FireDust, 0f, 0f, 100, new Color(), 1.5f);
                Main.dust[dust].velocity *= 3f;
            }
            if (Projectile.owner != Main.myPlayer)
                return;
            int fireAmount = Main.rand.Next(2, 4);
            for (int i = 0; i < fireAmount; i++)
            {
                Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<TotalityFire>(), Projectile.damage, 1f, Projectile.owner, 0f, 0f);
            }
        }
    }
}
