using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 泰拉巨刃的爆炸（移植自灾厄 TerratomereExplosion）：
    /// 由 TerratomereBigSlashs 累计 6 次电击后触发，18 帧 3×6 动画向外膨胀，
    /// 周围环绕 36 个 XerocLight 光刺，用加法混合绘制。
    /// </summary>
    internal class TerratomereExplosion : ModProjectile
    {
        /// <summary>泰拉巨刃主题色（灾厄 Terratomere.TerraColor1 / TerraColor2）</summary>
        private static readonly Color TerraColor1 = new Color(141, 203, 50);
        private static readonly Color TerraColor2 = new Color(83, 163, 136);
        /// <summary>基础属性：520×520 大爆炸、穿透无限、存活 150 帧、MaxUpdates 3、初始缩放 0.2、隐藏默认绘制</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 520;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.MaxUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 14;
            Projectile.scale = 0.2f;
            Projectile.hide = true;
        }
        /// <summary>
        /// 首帧播放爆炸音；发出强白光；每 8 帧推进一帧动画（18 帧后销毁）；
        /// 缩放按 1.013（ExplosionExpandFactor）指数膨胀，透明度随时间淡入淡出
        /// </summary>
        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.SubsumingVortexExplosion, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.5f);
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 8 == 7)
                Projectile.frame++;
            if (Projectile.frame >= 18)
                Projectile.Kill();
            Projectile.scale *= 1.013f;
            Projectile.Opacity = Utils.GetLerpValue(5f, 36f, Projectile.timeLeft, true);
        }
        /// <summary>
        /// 加法混合绘制（等价灾厄 IAdditiveDrawer.AdditiveDraw）：
        /// 主贴图按 3×6 取当前帧，周围 36 个光刺用 XerocLight 贴图按 MulticolorLerp 渐变着色环绕
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D lightTexture = ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/XerocLight").Value;
            Rectangle frame = texture.Frame(3, 6, Projectile.frame / 6, Projectile.frame % 6);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            for (int i = 0; i < 36; i++)
            {
                Vector2 lightDrawPosition = drawPosition + (MathHelper.TwoPi * i / 36f + Main.GlobalTimeWrappedHourly * 5f).ToRotationVector2() * Projectile.scale * 12f;
                Color lightBurstColor = CDUtil.MulticolorLerp(Projectile.timeLeft / 144f, TerraColor1, TerraColor2);
                lightBurstColor = Color.Lerp(lightBurstColor, Color.White, 0.4f) * Projectile.Opacity * 0.184f;
                Main.spriteBatch.Draw(lightTexture, lightDrawPosition, null, lightBurstColor, 0f, lightTexture.Size() * 0.5f, Projectile.scale * 1.32f, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(texture, drawPosition, frame, Color.White, 0f, origin, 1.6f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
