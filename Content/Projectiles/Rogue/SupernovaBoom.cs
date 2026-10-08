using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 超新星的巨大爆炸（照灾厄 2.0 的 <c>SupernovaBoom</c>）：
    /// 408×410 判定框、<c>scale = 2</c>、穿透无限、不撞物块不撞水；
    /// 贴图是 **5 列 × 4 行**的帧表（2040×1640，每帧 408×410），每帧 4 tick、走完整张表自动消失；
    /// 判定是**半径 204.5 的圆**，同一敌人每 16 帧可再吃一次（= 帧长 × 列 × 行 ÷ 5，照源的算式）。
    /// </summary>
    internal class SupernovaBoom : ModProjectile
    {
        /// <summary>横向帧数（照源）</summary>
        private const int HorizontalFrames = 5;
        /// <summary>纵向帧数（照源）</summary>
        private const int VerticalFrames = 4;
        /// <summary>每帧停留的 tick 数（照源）</summary>
        private const int FrameLength = 4;
        /// <summary>圆形判定半径（照源）</summary>
        private const float ExplosionRadius = 204.5f;

        /// <summary>当前播放到第几列（照源用普通字段记；本弹幕每帧只被自己推进，且是视觉状态，照搬）</summary>
        private int frameX;
        /// <summary>当前播放到第几行</summary>
        private int frameY;

        /// <summary>408×410、缩放 2 倍、穿透无限、不撞物块不撞水、同一敌人每 16 帧可再命中</summary>
        public override void SetDefaults()
        {
            Projectile.width = 408;
            Projectile.height = 410;
            Projectile.scale = 2f;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = FrameLength * HorizontalFrames * VerticalFrames / 5;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>逐帧推进（每 4 tick 一行、走满一列换下一列），走完整张表即消失；同时打变色照明</summary>
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % FrameLength == FrameLength - 1)
            {
                frameY++;
                if (frameY >= VerticalFrames)
                {
                    frameX++;
                    frameY = 0;
                }
                if (frameX >= HorizontalFrames)
                    Projectile.Kill();
            }
            Lighting.AddLight(Projectile.Center, Main.DiscoR * 0.5f / 255f, Main.DiscoG * 0.5f / 255f, Main.DiscoB * 0.5f / 255f);
        }
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
        /// <summary>手绘 5×4 帧表：按 frameX / frameY 取当前帧，缩放 2 倍铺满</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameWidth = texture.Width / HorizontalFrames;
            int frameHeight = texture.Height / VerticalFrames;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Rectangle frame = new Rectangle(frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight / 2f);
            Main.EntitySpriteDraw(texture, drawPos, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>圆形判定（内联灾厄 CollisionUtils.CircularHitboxCollision，写法照本工程 AbaddonCrit）</summary>
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
