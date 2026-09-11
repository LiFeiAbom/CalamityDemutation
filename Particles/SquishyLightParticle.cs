using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 柔和发光粒子（移植自灾厄的 SquishyLightParticle）：随速度方向拉伸的软光晕，带 bloom 光晕底。
    /// </summary>
    public class SquishyLightParticle : Particle
    {
        public override string Texture => "CalamityDemutation/Particles/Light";
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;
        public float Opacity;          // 当前整体透明度（Update 中随生存进度变化）
        public float SquishStrenght;   // 拉伸强度：越大越容易随速度拉长
        public float MaxSquish;        // 拉伸倍率上限
        public float HueShift;         // 每帧色相偏移量（在 Update 中累加到 HSL 的 H 分量）

        /// <summary>
        /// 构造柔和发光粒子：写入位置、速度、缩放、颜色、寿命与拉伸/色相参数，初始旋转为 0。
        /// </summary>
        /// <param name="position">世界坐标起点</param>
        /// <param name="velocity">每帧叠加到位置的速度（同时决定绘制时的拉伸方向与长度）</param>
        /// <param name="scale">绘制缩放</param>
        /// <param name="color">基础颜色（Update 中会按 HueShift 做色相偏移）</param>
        /// <param name="lifetime">寿命帧数，到期后由 GeneralParticleHandler 自动移除</param>
        /// <param name="opacity">绘制透明度倍率（默认 1）</param>
        /// <param name="squishStrenght">拉伸强度（默认 1）</param>
        /// <param name="maxSquish">拉伸倍率上限（默认 3）</param>
        /// <param name="hueShift">每帧色相偏移量（默认 0，即不变色）</param>
        public SquishyLightParticle(Vector2 position, Vector2 velocity, float scale, Color color, int lifetime, float opacity = 1f, float squishStrenght = 1f, float maxSquish = 3f, float hueShift = 0f)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            Color = color;
            Opacity = opacity;
            Rotation = 0;
            Lifetime = lifetime;
            SquishStrenght = squishStrenght;
            MaxSquish = maxSquish;
            HueShift = hueShift;
        }
        /// <summary>
        /// 每帧由 GeneralParticleHandler 调用：前 34% 寿命轻微提速（×1.02），之后减速（×0.93）；
        /// 透明度在寿命过半后加 0.8 的基值做 sin 衰减，前半程为纯 sin；缩放每帧 ×0.95；
        /// 最后按 HueShift 对颜色做色相循环
        /// </summary>
        public override void Update()
        {
            Velocity *= (LifetimeCompletion >= 0.34f) ? 0.93f : 1.02f;
            Opacity = LifetimeCompletion > 0.5f ? (float)Math.Sin(LifetimeCompletion * MathHelper.Pi) * 0.2f + 0.8f : (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            Scale *= 0.95f;
            Vector3 hsl = Main.rgbToHsl(Color);
            Color = Main.hslToRgb(hsl.X + HueShift, hsl.Y, hsl.Z);
        }
        /// <summary>
        /// 自定义绘制（UseCustomDraw=true 时被调用）：先画 BloomCircle 光晕底，再叠两层 Light 贴图。
        /// 拉伸量由速度大小算出并夹在 [1, MaxSquish]，旋转对齐速度方向，横向随拉伸收窄、
        /// 纵向拉长（scale 的 X 收窄、Y 拉伸），形成速度方向上的软光条；光晕底按贴图高宽比换算尺寸
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityDemutation/Particles/Light").Value;
            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityDemutation/Particles/BloomCircle").Value;
            float squish = MathHelper.Clamp(Velocity.Length() / 10f * SquishStrenght, 1f, MaxSquish);
            float rot = Velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 origin = tex.Size() / 2f;
            Vector2 scale = new Vector2(Scale - Scale * squish * 0.3f, Scale * squish);
            float properBloomSize = (float)tex.Height / (float)bloomTex.Height;
            Vector2 drawPosition = Position - Main.screenPosition;
            Main.spriteBatch.Draw(bloomTex, drawPosition, null, Color * Opacity * 0.8f, rot, bloomTex.Size() / 2f, scale * 2 * properBloomSize, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, drawPosition, null, Color * Opacity * 0.8f, rot, origin, scale * 1.1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, drawPosition, null, Color.White * Opacity * 0.9f, rot, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
