using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 泰拉巨刃的小刀光（移植自灾厄 TerratomereSlash）：
    /// 一道沿速度方向拉长的弧形刀光（512×24），由 <see cref="TerratomereSlashCreator"/> 生成，
    /// 用加法混合绘制，带中心 BloomCircle 光晕，颜色按 identity 在青色与黄绿之间循环。
    /// </summary>
    internal class TerratomereSlash : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/ExobeamSlash";
        /// <summary>基础属性：512×24 扁长刀光、近战无攻速、穿透 2、存活 35 帧、MaxUpdates 2、缩放 0.75、禁用附魔视觉</summary>
        public override void SetDefaults()
        {
            Projectile.width = 512;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 35;
            Projectile.MaxUpdates = 2;
            Projectile.scale = 0.75f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 12;
            Projectile.noEnchantmentVisuals = true;
        }
        /// <summary>朝向与速度一致，透明度随时间线性衰减</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Projectile.timeLeft / 35f;
        }
        /// <summary>旋转碰撞盒（等价灾厄 RotatingHitboxCollision 默认 scale=1）：把 512×24 的盒视为沿速度方向的线段</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 lineDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 start = Projectile.Center - lineDirection * Projectile.height * 0.5f;
            Vector2 end = Projectile.Center + lineDirection * Projectile.height * 0.5f;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width, ref _);
        }
        /// <summary>颜色按 identity 在青色与黄绿之间循环，乘透明度</summary>
        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.Cyan, Color.Lime, Projectile.identity / 7f % 1f) * Projectile.Opacity;
        /// <summary>
        /// 加法混合绘制：首帧（timeLeft≥34）跳过；中心画 BloomCircle 光晕，再叠两层刀光贴图
        /// （一层本色、一层压扁增亮），最后恢复 AlphaBlend 批状态
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft >= 34f)
                return false;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            float progress = (33f - Projectile.timeLeft) / 33f;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityDemutation/Assets/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 scale = new Vector2(MathHelper.Lerp(0.8f, 1.25f, (float)System.Math.Pow(progress, 0.45)), MathHelper.Lerp(0.6f, 0.24f, (float)System.Math.Pow(progress, 0.4))) * Projectile.scale;
            Vector2 bloomScale = Projectile.Size / bloomTexture.Size() * new Vector2(1f, 2f);
            Vector2 bloomOrigin = bloomTexture.Size() * 0.5f;
            Main.spriteBatch.Draw(bloomTexture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, bloomOrigin, bloomScale, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, origin, scale * new Vector2(1f, 0.6f), 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
