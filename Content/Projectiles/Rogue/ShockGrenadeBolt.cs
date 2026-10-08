using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 震爆手雷炸出的闪电（照灾厄 2.0 的 <c>ShockGrenadeBolt</c>）：
    /// 12×26 的 4 帧动画、穿透 3、存活 120 帧、不撞墙（存活不足 55 帧后才开始撞墙）；
    /// 命中挂 2 秒「带电」；消失时播一声轻响并撒 5 颗电尘。
    /// <para>
    /// ai[0] 选贴图（0 = ShockGrenadeBolt、1 = ShockGrenadeBolt2），
    /// ai[1] = 1 表示这是**潜行打击**的闪电：每帧在 999 像素内找最近的敌人归航（速度上限 10）。
    /// </para>
    /// </summary>
    internal class ShockGrenadeBolt : ModProjectile
    {
        /// <summary>单帧宽度（贴图 12×104，纵向 4 帧）</summary>
        private const int FrameWidth = 12;
        /// <summary>单帧高度</summary>
        private const int FrameHeight = 26;
        /// <summary>带电视觉类型（与源一致，两套配色随机取）</summary>
        private const int SparkDust = 132;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        /// <summary>10×10 判定、穿透 3、存活 120 帧、不撞墙（AI 里再放行），伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 120;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>4 帧动画（每 6 帧一换）+ 朝速度方向自转；存活不足 55 帧后开始撞墙；潜行闪电归航</summary>
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.frame = 0;

            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + 1.57f;

            if (Projectile.timeLeft < 55)
                Projectile.tileCollide = true;

            if (Projectile.ai[1] == 1f)
                HomeInOnClosestNPC();
        }
        /// <summary>
        /// 潜行闪电的归航：扫描全场找**最近的**可追击敌人（不做视线判定），
        /// 把速度往目标方向加 2f，再把总速度截到 10f（照源实现）。
        /// </summary>
        private void HomeInOnClosestNPC()
        {
            float minDist = 999f;
            int index = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;
                float dist = (Projectile.Center - npc.Center).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    index = i;
                }
            }
            if (minDist >= 999f)
                return;
            Vector2 velocityNew = Main.npc[index].Center - Projectile.Center;
            velocityNew.Normalize();
            velocityNew *= 2f;
            Projectile.velocity += velocityNew;
            if (Projectile.velocity.Length() > 10f)
            {
                Projectile.velocity.Normalize();
                Projectile.velocity *= 10f;
            }
        }
        /// <summary>命中敌人挂 2 秒「带电」</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 120);
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 120);
        }
        /// <summary>手绘 4 帧动画；贴图按 ai[0] 在 ShockGrenadeBolt / ShockGrenadeBolt2 之间二选一</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sprite = Projectile.ai[0] == 0f
                ? ModContent.Request<Texture2D>(Texture).Value
                : ModContent.Request<Texture2D>(Texture + "2").Value;
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);
            Main.EntitySpriteDraw(sprite, Projectile.Center - Main.screenPosition, new Rectangle(0, FrameHeight * Projectile.frame, FrameWidth, FrameHeight), Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>消失：一声轻响（原版 Item93 的 1/4 音量）+ 5 颗去重力的电尘</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = SoundID.Item93.Volume * 0.25f }, Projectile.position);
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(Projectile.Center, 1, 1, SparkDust, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 0.5f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
