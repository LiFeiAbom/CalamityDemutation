using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>绯红恶魔 Public 2.0.4 的追踪弹：先减速，60 帧后在 1000 像素内追踪。</summary>
    internal class ScarletDevilBullet : ModProjectile
    {
        private ref float TargetIndex => ref Projectile.ai[1];
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.timeLeft = 140;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            Projectile.ai[0]++;
            if (Projectile.ai[0] <= 60f)
            {
                Projectile.velocity *= 0.975f;
                return;
            }
            int targetIndex = (int)TargetIndex - 1;
            if (!IsValidTarget(targetIndex))
            {
                targetIndex = AcquireTarget();
                TargetIndex = targetIndex + 1f;
            }
            if (targetIndex < 0)
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }
            Vector2 targetCenter = Main.npc[targetIndex].Center;
            Vector2 moveDirection = Projectile.SafeDirectionTo(targetCenter, Vector2.UnitY);
            Projectile.velocity = (Projectile.velocity * 10f + moveDirection * 30f) / 11f;
        }
        private int AcquireTarget()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!IsValidTarget(i))
                    continue;
                return i;
            }
            return -1;
        }
        private bool IsValidTarget(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return false;
            NPC npc = Main.npc[targetIndex];
            if (!npc.CanBeChasedBy(Projectile, false))
                return false;
            float extraDistance = npc.width / 2f + npc.height / 2f;
            return Vector2.Distance(npc.Center, Projectile.Center) < 1000f + extraDistance;
        }
        public override Color? GetAlpha(Color lightColor) => new Color(250, 250, 250);
        public override bool? CanDamage()
        {
            if (IsStealthStrike() && Projectile.ai[0] < 60f)
                return false;
            return null;
        }
        private bool IsStealthStrike()
        {
            // localAI[1] 的 1/2 分别缓存“普通 / 潜行”，避免每帧反复走灾厄反射桥。
            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = CDUtil.IsStealthStrike(Projectile, out _) ? 2f : 1f;
            return Projectile.localAI[1] == 2f;
        }
    }
}
