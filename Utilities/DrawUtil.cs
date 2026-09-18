using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（绘制部分）：提供弹幕/物品绘制信息计算等扩展方法
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 在背包/UI 中以自定义缩放绘制物品贴图，缩放不足时按物品栏比例放大
        /// </summary>
        public static void DrawInventoryCustomScale(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale, float wantedScale = 1f, Vector2 drawOffset = default, SpriteEffects spriteEffects = SpriteEffects.None, float rotation = 0f)
        {
            // 若期望缩放小于物品栏缩放系数，则按物品栏缩放放大，保证在背包中清晰可见
            wantedScale = Math.Max(scale, wantedScale * Main.inventoryScale);
            // 将调用方传入的偏移量乘以最终缩放后叠加到绘制位置
            position += drawOffset * wantedScale;
            // 物品颜色为全透明时回退为白色，避免整张贴图被误隐藏
            if (itemColor == Color.Transparent) itemColor = Color.White;
            spriteBatch.Draw(texture, position, frame, itemColor.MultiplyRGB(drawColor), 0f, origin, wantedScale, SpriteEffects.None, 0);
        }
        /// <summary>
        /// 近战武器式弹幕的绘制信息辅助：计算贴图、屏幕绘制位置、旋转角、
        /// 旋转中心与翻转方向，便于按"挥砍武器"的观感绘制弹幕贴图。
        /// </summary>
        public static void GetProjDrawInfo_Melee(this Projectile proj, out Texture2D texture, out Vector2 drawPosition, out float drawRotation, out Vector2 rotationPoint, out SpriteEffects flipSprite)
        {
            texture = TextureAssets.Projectile[proj.type].Value;
            drawPosition = proj.Center - Main.screenPosition;
            // 逆挥方向（spriteDirection == -1）时补 3/4π 转角，令贴图随挥砍方向镜像旋转
            drawRotation = proj.rotation + (proj.spriteDirection == -1 ? MathHelper.PiOver2 + MathHelper.PiOver4 : MathHelper.PiOver4);
            // 旋转中心取在贴图的握柄端：逆挥取右下角、正挥取左下角，模拟握持旋转
            rotationPoint = proj.spriteDirection == -1 ? new Vector2(texture.Width, texture.Height) : new Vector2(0, texture.Height);
            // 逆挥时水平翻转贴图，使刀刃朝向与速度方向一致
            flipSprite = proj.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }
        /// <summary>
        /// 绘制物品单帧 glowmask 发光层（移植自灾厄的 DrawItemGlowmaskSingleFrame）：
        /// 以物品中心为原点、按指定旋转角绘制一张纯白光晕贴图，用于掉落物的发光叠加。
        /// </summary>
        public static void DrawItemGlowmaskSingleFrame(this Item item, SpriteBatch spriteBatch, float rotation, Texture2D glowmaskTexture)
        {
            spriteBatch.Draw(glowmaskTexture, item.Center - Main.screenPosition, null, Color.White, rotation, glowmaskTexture.Size() / 2f, 1f, SpriteEffects.None, 0f);
        }
        /// <summary>
        /// 十向偏移背光：以弹幕中心为基准绕一圈 10 个方向各叠画一次贴图，形成发光描边
        /// （移植自灾厄 ProjectileUtils.DrawBackglow 的单色版本）。帧取 Main.projFrames 的当前帧，
        /// 因此单帧贴图与多帧动画贴图都能正确取样；贴图留空时取弹幕自身的贴图。
        /// </summary>
        public static void DrawBackglow(this Projectile projectile, Color backglowColor, float backglowArea, Texture2D texture = null)
        {
            texture ??= TextureAssets.Projectile[projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Color backAfterimageColor = backglowColor * projectile.Opacity;
            SpriteEffects spriteEffects = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * backglowArea;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, backAfterimageColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
            }
        }
        /// <summary>
        /// 多色渐变插值：按 <paramref name="increment"/>（0-1 递增）在多个颜色间循环过渡。
        /// </summary>
        public static Color MulticolorLerp(float increment, params Color[] colors)
        {
            increment %= 0.999f;
            int currentColorIndex = (int)(increment * colors.Length);
            Color currentColor = colors[currentColorIndex];
            Color nextColor = colors[(currentColorIndex + 1) % colors.Length];
            return Color.Lerp(currentColor, nextColor, increment * colors.Length % 1f);
        }
        /// <summary>
        /// 多段颜色插值（移植自灾厄大修的 MultiStepColorLerp）：把 [0,1] 的 percent 均分给 colors.Length - 1 段，
        /// 在相邻两色之间逐段插值。与 <see cref="MulticolorLerp"/> 的"循环取色"不同——本方法是首尾不循环的单向扫过，
        /// 故两者并存、不可互相替代。
        /// </summary>
        public static Color MultiStepColorLerp(float percent, params Color[] colors)
        {
            if (colors == null)
            {
                return Color.White;
            }
            float per = 1f / (colors.Length - 1f);
            float total = per;
            int currentID = 0;
            while (percent / total > 1f && currentID < colors.Length - 2)
            {
                total += per;
                currentID++;
            }
            return Color.Lerp(colors[currentID], colors[currentID + 1], (percent - (per * currentID)) / per);
        }
        /// <summary>
        /// 计算顶点着色器使用的透视矩阵（视野矩阵 + 投影矩阵），
        /// 供 PrimitiveRenderer 等 GPU 图元绘制使用，已处理屏幕尺寸、缩放与重力翻转。
        /// </summary>
        public static void CalculatePerspectiveMatricies(out Matrix viewMatrix, out Matrix projectionMatrix)
        {
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);
            // 屏幕边界。
            int width = Main.instance.GraphicsDevice.Viewport.Width;
            int height = Main.instance.GraphicsDevice.Viewport.Height;
            // 得到一个朝向 Z 轴的矩阵（这些计算相对于 2D 世界）。
            viewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);
            // 将矩阵平移到适当位置。
            viewMatrix *= Matrix.CreateTranslation(0f, -height, 0f);
            // 将矩阵翻转 180 度。
            viewMatrix *= Matrix.CreateRotationZ(MathHelper.Pi);
            // 考虑重力翻转效果。
            if (Main.LocalPlayer.gravDir == -1f)
                viewMatrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);
            // 然后考虑当前缩放。
            viewMatrix *= zoomScaleMatrix;
            projectionMatrix = Matrix.CreateOrthographicOffCenter(0f, width * zoom.X, 0f, height * zoom.Y, 0f, 1f) * zoomScaleMatrix;
        }
    }
}
