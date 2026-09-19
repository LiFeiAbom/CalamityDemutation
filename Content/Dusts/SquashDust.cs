using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Dusts
{
    /// <summary>
    /// 压扁光球尘（SquashDust，移植自 CalamityEntropy，CE 又自灾厄 SquashDust 移植）：
    /// 随速度被横向压扁、纵向拉长的发光球尘——速度越快越细长，静止时收成圆点，逐帧缩小至消失。
    /// 无重力时按固定速率收缩，有重力时收缩更慢并缓慢下坠。<c>customData</c> 可传一个 <c>Vector2</c> 当基础宽高比。
    /// <para>
    /// 与 CE 原版的差异：① CE 用 <c>VaultLoaden</c> 在加载期把两张球贴图赋给静态字段，
    /// 本模组改用 <c>ModContent.Request</c>（同工程其它弹幕的取图口径）；
    /// ② 贴图落在本模组的 Assets/Particles 下（BasicCircle / BloomCircle，与 CE 侧同名同物）；
    /// ③ 类贴图 SquashDust.png 只是让 tML 能解析到资源的占位图，实际绘制全走 <see cref="PreDraw"/> 的两张球贴图。
    /// </para>
    /// </summary>
    internal class SquashDust:ModDust
    {
        /// <summary>实心圆贴图（CE 的 Assets/Particles/BasicCircle）：只在允许受光时才叠的亮核</summary>
        private const string SolidCircleTexture = "CalamityDemutation/Assets/Particles/BasicCircle";
        /// <summary>泛光圆贴图（CE 的 Assets/Particles/BloomCircle）：尘的主体光晕</summary>
        private const string BloomCircleTexture = "CalamityDemutation/Assets/Particles/BloomCircle";
        /// <summary>生成时把体积随机缩到 0.8~1 倍，避免一批尘大小整齐划一</summary>
        public override void OnSpawn(Dust dust)
        {
            dust.scale *= Main.rand.NextFloat(0.8f, 1f);
        }
        /// <summary>每帧：朝速度方向拉伸、阻尼、收缩（有重力时更慢并下坠）、按体积投光，缩到 0 就消失</summary>
        public override bool Update(Dust dust)
        {
            float fadeSpeed = dust.fadeIn + 1;
            dust.rotation = dust.velocity.ToRotation() + MathHelper.PiOver2;
            dust.velocity *= 0.96f;
            if (dust.noGravity)
            {
                dust.scale -= 0.045f * fadeSpeed;
            }
            else
            {
                dust.scale -= 0.03f * fadeSpeed;
                dust.velocity.Y += Main.rand.NextFloat(0.1f, 0.35f) * fadeSpeed;
            }
            float light = MathHelper.Clamp(dust.scale * 0.8f, 0f, 1f);
            if (!dust.noLightEmittence)
            {
                Lighting.AddLight(dust.position, dust.color.ToVector3() * light);
            }
            if (dust.scale <= 0)
            {
                dust.active = false;
            }
            dust.position += dust.velocity;
            return false;
        }
        /// <summary>
        /// 自绘：按速度把圆拉成椭圆——泛光层画两层（主体大、外圈更大更淡），
        /// 允许受光时再叠一层实心亮核。颜色一律抹掉 alpha 通道，交给 alpha 值单独控制透明度。
        /// </summary>
        public override bool PreDraw(Dust dust)
        {
            Vector2 baseSize = Vector2.One;
            if (dust.customData != null && dust.customData is Vector2)
            {
                baseSize = (Vector2)dust.customData;
            }
            // 速度 2~7 之间做重映射：横向最多压到 0.5 倍、纵向最多拉到 2.5 倍
            Vector2 squash = new Vector2(Utils.Remap(dust.velocity.Length(), 2, 7, 1 * baseSize.X, 0.5f * baseSize.X),
                Utils.Remap(dust.velocity.Length(), 2, 7, 1 * baseSize.Y, 2.5f * baseSize.Y));
            Texture2D bloom = ModContent.Request<Texture2D>(BloomCircleTexture).Value;
            Main.spriteBatch.Draw(bloom, dust.position - Main.screenPosition, null, dust.color with { A = 0 } * Utils.GetLerpValue(255, 0, dust.alpha),
                dust.rotation, bloom.Size() * 0.5f, squash * dust.scale * 0.1f, SpriteEffects.None, 0);
            if (dust.alpha < 1)
            {
                Main.spriteBatch.Draw(bloom, dust.position - Main.screenPosition, null, dust.color with { A = 0 } * 0.85f * Utils.GetLerpValue(255, 0, dust.alpha),
                    dust.rotation, bloom.Size() * 0.5f, squash * dust.scale * 0.04f, SpriteEffects.None, 0);
            }
            if (!dust.noLight)
            {
                Texture2D solid = ModContent.Request<Texture2D>(SolidCircleTexture).Value;
                Main.spriteBatch.Draw(solid, dust.position - Main.screenPosition, null, Color.Lerp(dust.color, Color.White, 0.3f) with { A = 0 } * Utils.GetLerpValue(255, 0, dust.alpha),
                    dust.rotation, solid.Size() * 0.5f, squash * dust.scale * 0.075f, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
