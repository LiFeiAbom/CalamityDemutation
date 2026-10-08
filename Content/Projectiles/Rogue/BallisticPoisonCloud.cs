using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 弹道毒炸弹炸出的毒云（照灾厄 2.0 的 <c>BallisticPoisonCloud</c>）：
    /// 32×32 判定、10 帧动画（贴图 40×560，纵向切 10 帧）、存活 3600 帧、不撞物块不撞水；
    /// 前 4 帧循环、<c>ai[0] ≥ 219</c> 后放到最后一帧结束（由主人端 <c>Kill()</c>）——
    /// 也就是"先浓后淡、最后消散"的那段动画；同一敌人每 40 帧可再吃一次，命中挂 2 秒「毒液」。
    /// </summary>
    internal class BallisticPoisonCloud : ModProjectile
    {
        /// <summary>开始消散的帧数（照源：255 − 动画节拍 × 消散帧数）</summary>
        private const float DisperseTime = 219f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 10;
        }
        /// <summary>32×32、初始全透明、穿透无限、存活 3600 帧、同一敌人每 40 帧可再命中</summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 3600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>推动画（每 7 帧一帧）：前 4 帧循环到 219 帧，之后播完 10 帧由主人端收尾；速度每帧衰减 2%、alpha 渐显到 80</summary>
        public override void AI()
        {
            Projectile.ai[0] += 1f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.ai[0] < DisperseTime)
            {
                if (Projectile.frame >= 4)
                    Projectile.frame = 0;
            }
            else if (Projectile.owner == Main.myPlayer && Projectile.frame >= Main.projFrames[Projectile.type])
            {
                Projectile.Kill();
            }
            Projectile.velocity *= 0.98f;
            if (Projectile.alpha > 80)
            {
                Projectile.alpha -= 30;
                if (Projectile.alpha < 80)
                    Projectile.alpha = 80;
            }
            if (Math.Abs(Projectile.velocity.X) > 0.1f)
                Projectile.spriteDirection = -Projectile.direction;
        }
        /// <summary>命中敌人挂 2 秒「毒液」</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 120);
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 120);
        }
    }
}
