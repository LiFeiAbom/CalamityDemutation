using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 苍华之庭·种子（PlantationStaffSeed，移植自灾厄 2.0.3.9 的同名弹幕）——
    /// 树灵撒出的两帧小种子，直线飞行、两种随机贴图（绿/粉），性能模式关闭时带同色残影。
    /// </summary>
    internal class PlantationStaffSeed:ModProjectile
    {
        /// <summary>随机贴图档：0 = 绿色那套，1 = 粉色那套（源在生成时写进 ai[0]）</summary>
        public ref float RandomTexture => ref Projectile.ai[0];

        /// <summary>两帧动画 + 残影缓存</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 2;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
        }
        /// <summary>基础属性（照源）：14×14、600 帧寿命、不撞地形</summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = Projectile.height = 14;
            Projectile.timeLeft = 600;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        public override void AI()
        {
            DoAnimation();

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>两帧动画（每 8 帧推一帧）</summary>
        private void DoAnimation()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }
        /// <summary>自绘：按随机档换贴图；性能模式关闭时按历史位置叠绿/粉残影</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (RandomTexture == 0f) ? ModContent.Request<Texture2D>(Texture).Value : ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/PlantationStaffSeed2").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            if (ConfigSystem.Instance?.PerformanceMode != true)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Color afterimageDrawColor = ((RandomTexture == 0f) ? Color.Green : Color.Pink) with { A = 25 } * Projectile.Opacity * (1f - i / (float)Projectile.oldPos.Length);
                    Vector2 afterimageDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(texture, afterimageDrawPosition, frame, afterimageDrawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
