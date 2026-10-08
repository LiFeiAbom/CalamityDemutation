using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 超新星炸出的追踪能量（照灾厄 2.0 的 <c>SupernovaHoming</c>）：
    /// 8×8 判定、穿透 1、不撞物块、贴图是共用隐形贴图。
    /// </summary>
    /// <remarks>
    /// 前 90 帧只是边减速（每帧 ×0.98）边自转、**不能造成伤害**（<c>CanDamage</c> 返回 false），
    /// 90 帧后开始 `extraUpdates = 1` 并在 500 像素内归航（锁定速度 12、惯性 20），找不到目标就自我消灭。
    /// 每 2 帧撒一颗彩虹尘（107/234/269），消失时炸一圈 36 颗尘；命中挂整套星云系减益。
    /// </remarks>
    internal class SupernovaHoming : ModProjectile
    {
        /// <summary>起追踪的帧数（照源）</summary>
        private const int HomingDelay = 90;
        /// <summary>归航索敌半径（照源）</summary>
        private const float HomingRange = 500f;
        /// <summary>归航速度（照源）</summary>
        private const float HomingSpeed = 12f;
        /// <summary>归航惯性（照源）</summary>
        private const float HomingInertia = 21f;

        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";

        /// <summary>8×8、穿透 1、不撞物块，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>撒彩虹尘 + 前 90 帧减速自转、之后转入归航</summary>
        public override void AI()
        {
            int dustType = Main.rand.NextBool(2) ? DustID.Terra : DustID.BoneTorch;
            if (Main.rand.NextBool(4))
                dustType = DustID.Sandnado;
            Projectile.ai[0] += 1;
            if (Projectile.ai[0] % 2 == 0)
            {
                const int dustShrink = 14;
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width - dustShrink * 2, Projectile.height - dustShrink * 2, dustType, 0f, 0f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
                Main.dust[dust].velocity += Projectile.velocity * 0.5f;
            }

            if (Projectile.ai[0] < HomingDelay)
            {
                Projectile.velocity *= 0.98f;
            }
            else
            {
                Projectile.extraUpdates = 1;
                Vector2 targetCenter = Projectile.Center;
                bool homeIn = false;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.CanBeChasedBy(Projectile, false))
                        continue;
                    float extraDistance = npc.width / 2f + npc.height / 2f;
                    if (Vector2.Distance(npc.Center, Projectile.Center) < HomingRange + extraDistance && Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    {
                        targetCenter = npc.Center;
                        homeIn = true;
                        break;
                    }
                }
                if (homeIn)
                {
                    Vector2 moveDirection = Projectile.SafeDirectionTo(targetCenter, Vector2.UnitY);
                    Projectile.velocity = (Projectile.velocity * 20f + moveDirection * HomingSpeed) / HomingInertia;
                }
                else
                {
                    Projectile.Kill();
                }
            }
            Projectile.rotation += 0.25f;
        }
        /// <summary>90 帧之前不造成伤害（照源）</summary>
        public override bool? CanDamage() => Projectile.ai[0] >= HomingDelay ? null : false;
        /// <summary>命中敌人挂整套星云系减益</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.ExoDebuffs();
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.ExoDebuffs();
        }
        /// <summary>消失：沿速度方向炸一圈 36 颗尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 24;
            Projectile.position -= Projectile.Size * 0.5f;
            const int ringDust = 36;
            for (int i = 0; i < ringDust; i++)
            {
                Vector2 ringOffset = Vector2.Normalize(Projectile.velocity) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f;
                ringOffset = ringOffset.RotatedBy((i - (ringDust / 2 - 1)) * 6.28318548f / ringDust) + Projectile.Center;
                Vector2 spawnPos = ringOffset + (ringOffset - Projectile.Center);
                int dust = Dust.NewDust(spawnPos, 0, 0, DustID.BoneTorch, (ringOffset - Projectile.Center).X * 0.5f, (ringOffset - Projectile.Center).Y * 0.5f, 100, default, 0.75f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
