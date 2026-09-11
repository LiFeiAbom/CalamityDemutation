using System.Collections.Generic;
using System.Linq;
using CalamityDemutation.Common.Effects;
using CalamityDemutation.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Graphics.Metaballs
{
    /// <summary>
    /// 龙息火焰 Metaball（移植自灾厄的 DragonsBreathFlameMetaball，橙色）
    /// </summary>
    public class DragonsBreathFlameMetaball : DragonsBreathMetaball
    {
        public override Color EdgeColor => Color.Orange;
        /// <summary>
        /// 绘制本形态的所有粒子实例：透明度 0.03，白热阈值传 -1 表示走「按尺寸插值到暗橙」而非白色分支
        /// </summary>
        public override void DrawInstances() => DrawInstancesInternal(0.03f, -1f);
    }

    /// <summary>
    /// 龙息火焰 Metaball 第二形态（移植自灾厄的 DragonsBreathFlameMetaball2，橙红）
    /// </summary>
    public class DragonsBreathFlameMetaball2 : DragonsBreathMetaball2
    {
        public override Color EdgeColor => Color.OrangeRed;
        /// <summary>
        /// 绘制本形态的所有粒子实例：透明度 0.03，白热阈值传 -1 表示走「按尺寸插值到暗橙」而非白色分支
        /// </summary>
        public override void DrawInstances() => DrawInstancesInternal(0.03f, -1f);
    }

    /// <summary>
    /// 龙息 Metaball 基类（移植自灾厄的 DragonsBreathMetaball）
    /// </summary>
    public abstract class DragonsBreathMetaball : Metaball
    {
        public class DragonsBreathParticle
        {
            public Vector2 Center;
            public float Size;
            /// <summary>
            /// 记录粒子的世界坐标中心与初始尺寸（尺寸同时作为绘制缩放与消散判定的依据）
            /// </summary>
            public DragonsBreathParticle(Vector2 center, float size)
            {
                Center = center;
                Size = size;
            }
            /// <summary>
            /// 每帧收缩粒子：先 -0.1 并夹取到 [0,200]，再乘 0.91 做比例收缩；
            /// 尺寸跌破 5 后改用更快的衰减（*0.8-1）加速消散
            /// </summary>
            public void Update()
            {
                Size = MathHelper.Clamp(Size - 0.1f, 0f, 200f) * 0.91f;   // 线性 + 比例的双重收缩
                if (Size < 5f)
                    Size = Size * 0.8f - 1f;   // 尾段加速衰减，缩短小粒子拖尾
            }
        }

        public List<DragonsBreathParticle> Particles { get; private set; } = new List<DragonsBreathParticle>();
        public override bool AnythingToDraw => Particles.Any();
        public override IEnumerable<Texture2D> Layers
        {
            get
            {
                yield return ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/InvisibleProj").Value;
            }
        }
        public override GeneralDrawLayer DrawLayer => GeneralDrawLayer.AfterProjectiles;
        /// <summary>
        /// 每帧推进所有粒子运动并清理已收缩到 2 像素以下的粒子
        /// </summary>
        public override void Update()
        {
            for (int i = 0; i < Particles.Count; i++)
                Particles[i].Update();
            Particles.RemoveAll(p => p.Size <= 2f);   // 尺寸过小已无视觉贡献，直接移除
        }
        /// <summary>
        /// 为龙息元球准备加法混合边缘着色器：只使用屏幕尺寸参数，层级偏移与逐帧屏幕偏移均归零（不分层滚动）
        /// </summary>
        public override void PrepareShaderForTarget(int layerIndex)
        {
            var metaballShader = EffectLoader.AdditiveMetaballEdgeShader;   // 加法混合版边缘着色器
            Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
            metaballShader.Value.Parameters["screenArea"]?.SetValue(screenSize);
            metaballShader.Value.Parameters["layerOffset"]?.SetValue(Vector2.Zero);
            metaballShader.Value.Parameters["singleFrameScreenOffset"]?.SetValue(Vector2.Zero);
            metaballShader.Value.CurrentTechnique.Passes[0].Apply();
        }
        /// <summary>
        /// 在世界坐标 position 处生成一个指定初始尺寸的龙息粒子（由弹幕等调用方传入）
        /// </summary>
        public void SpawnParticle(Vector2 position, float size) => Particles.Add(new DragonsBreathParticle(position, size));
        /// <summary>
        /// 实际绘制入口：把 MetaballMessy 贴图按每个粒子的中心与尺寸画到当前渲染目标，由各形态的 DrawInstances 调用。
        /// 调用前外部必须已 Begin 过 spriteBatch。
        /// whiteSizeThreshold &gt;= 0 时按尺寸向白色过渡；为负时按尺寸向暗橙（0xE0,0x70,0x10）过渡。
        /// </summary>
        internal void DrawInstancesInternal(float opacity, float whiteSizeThreshold)
        {
            float pureWhiteIntensity = 0.16f;   // 白色过渡的最大强度，避免粒子完全洗白
            Texture2D tex = ModContent.Request<Texture2D>("CalamityDemutation/Graphics/Metaballs/MetaballMessy").Value;
            foreach (DragonsBreathParticle particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;   // 世界坐标转屏幕坐标
                var origin = tex.Size() * 0.5f;
                Vector2 scale = Vector2.One * particle.Size / tex.Size();   // 尺寸换算为贴图缩放
                Color drawColor;
                if (whiteSizeThreshold >= 0f)
                {
                    float pureWhiteInterpolant = Utils.GetLerpValue(0.8f * whiteSizeThreshold, whiteSizeThreshold, particle.Size, true) * pureWhiteIntensity;
                    drawColor = Color.Lerp(EdgeColor, Color.DarkOrange, pureWhiteInterpolant) * opacity;
                }
                else
                    drawColor = Color.Lerp(EdgeColor, new Color(0xE0, 0x70, 0x10), Utils.GetLerpValue(60f, 100f, particle.Size, true) * 0.75f);   // 60~100 尺寸区间内逐渐加深
                Main.spriteBatch.Draw(tex, drawPosition, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    /// <summary>
    /// 龙息 Metaball 第二形态基类（移植自灾厄的 DragonsBreathMetaball2）
    /// </summary>
    public abstract class DragonsBreathMetaball2 : Metaball
    {
        public class DragonsBreathParticle2
        {
            public Vector2 Center;
            public float Size;
            /// <summary>
            /// 记录粒子的世界坐标中心与初始尺寸（尺寸同时作为绘制缩放与消散判定的依据）
            /// </summary>
            public DragonsBreathParticle2(Vector2 center, float size)
            {
                Center = center;
                Size = size;
            }
            /// <summary>
            /// 每帧收缩粒子：先 -0.1 并夹取到 [0,200]，再乘 0.91 做比例收缩；
            /// 尺寸跌破 20（比第一形态更早）后改用 *0.8-1 加速消散
            /// </summary>
            public void Update()
            {
                Size = MathHelper.Clamp(Size - 0.1f, 0f, 200f) * 0.91f;   // 线性 + 比例的双重收缩
                if (Size < 20f)
                    Size = Size * 0.8f - 1f;   // 尾段加速衰减，第二形态的阈值比第一形态高
            }
        }

        public List<DragonsBreathParticle2> Particles { get; private set; } = new List<DragonsBreathParticle2>();
        public override bool AnythingToDraw => Particles.Any();
        public override IEnumerable<Texture2D> Layers
        {
            get
            {
                yield return ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/InvisibleProj").Value;
            }
        }
        public override GeneralDrawLayer DrawLayer => GeneralDrawLayer.AfterProjectiles;
        /// <summary>
        /// 每帧推进所有粒子运动并清理已收缩到 2 像素以下的粒子
        /// </summary>
        public override void Update()
        {
            for (int i = 0; i < Particles.Count; i++)
                Particles[i].Update();
            Particles.RemoveAll(p => p.Size <= 2f);   // 尺寸过小已无视觉贡献，直接移除
        }
        /// <summary>
        /// 为第二形态元球准备加法混合边缘着色器：只用屏幕尺寸参数，层级偏移与逐帧屏幕偏移均归零
        /// </summary>
        public override void PrepareShaderForTarget(int layerIndex)
        {
            var metaballShader = EffectLoader.AdditiveMetaballEdgeShader;   // 加法混合版边缘着色器
            Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
            metaballShader.Value.Parameters["screenArea"]?.SetValue(screenSize);
            metaballShader.Value.Parameters["layerOffset"]?.SetValue(Vector2.Zero);
            metaballShader.Value.Parameters["singleFrameScreenOffset"]?.SetValue(Vector2.Zero);
            metaballShader.Value.CurrentTechnique.Passes[0].Apply();
        }
        /// <summary>
        /// 在世界坐标 position 处生成一个指定初始尺寸的第二形态龙息粒子
        /// </summary>
        public void SpawnParticle(Vector2 position, float size) => Particles.Add(new DragonsBreathParticle2(position, size));
        /// <summary>
        /// 实际绘制入口：把 MetaballMessy 贴图按每个粒子的中心与尺寸画到当前渲染目标，由各形态的 DrawInstances 调用。
        /// 调用前外部必须已 Begin 过 spriteBatch。
        /// whiteSizeThreshold &gt;= 0 时按尺寸向白色过渡；为负时按尺寸向暗橙过渡（强度低于第一形态）。
        /// </summary>
        internal void DrawInstancesInternal(float opacity, float whiteSizeThreshold)
        {
            float pureWhiteIntensity = 0.16f;   // 白色过渡的最大强度，避免粒子完全洗白
            Texture2D tex = ModContent.Request<Texture2D>("CalamityDemutation/Graphics/Metaballs/MetaballMessy").Value;
            foreach (DragonsBreathParticle2 particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;   // 世界坐标转屏幕坐标
                var origin = tex.Size() * 0.5f;
                Vector2 scale = Vector2.One * particle.Size / tex.Size();   // 尺寸换算为贴图缩放
                Color drawColor;
                if (whiteSizeThreshold >= 0f)
                {
                    float pureWhiteInterpolant = Utils.GetLerpValue(0.8f * whiteSizeThreshold, whiteSizeThreshold, particle.Size, true) * pureWhiteIntensity;
                    drawColor = Color.Lerp(EdgeColor, Color.White, pureWhiteInterpolant) * opacity;
                }
                else
                    drawColor = Color.Lerp(EdgeColor, Color.DarkOrange, Utils.GetLerpValue(60f, 100f, particle.Size, true) * 0.3f);   // 第二形态加深幅度更小
                Main.spriteBatch.Draw(tex, drawPosition, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
