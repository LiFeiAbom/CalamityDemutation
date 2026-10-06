using System.Collections.Generic;
using System.Linq;
using CalamityDemutation.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Graphics.Metaballs
{
    /// <summary>
    /// 虚空场元球（移植自灾厄 2.0.4 的 <c>VoidGeneratorMetaball</c>）：
    /// 由虚无箭袋的 <c>VoidFieldGenerator</c> 弹幕每帧钉住中心与尺寸（120）的暗紫外球，
    /// 贴图取样走 <c>StreamGougeLayer</c>（一层缓慢横向滚动的星空纹），边缘色为深紫。
    /// <para>
    /// 与源的差异：灾厄把它挂在 <c>BeforeProjectiles</c> 层（要画在箭矢之下），
    /// 本工程的绘制层系统原先只接线了 AfterDusts / AfterProjectiles / AfterPlayers，
    /// 故在 <see cref="CalamityDemutation.Systems.Graphic.GeneralDrawLayerSystem"/> 里补上了
    /// BeforeProjectiles 档（只影响声明该层的元球）。
    /// </para>
    /// </summary>
    public class VoidGeneratorMetaball : Metaball
    {
        // ── 嵌套类型 ──
        /// <summary>
        /// 单个虚空场粒子：记录世界坐标中心、速度与当前尺寸（照源）
        /// </summary>
        public class CosmicParticle
        {
            /// <summary>当前尺寸（同时作为绘制缩放与消散判定的依据）</summary>
            public float Size;
            /// <summary>移动速度（每帧衰减 0.96×）</summary>
            public Vector2 Velocity;
            /// <summary>世界坐标中心</summary>
            public Vector2 Center;
            /// <summary>记录粒子的中心、速度与初始尺寸</summary>
            public CosmicParticle(Vector2 center, Vector2 velocity, float size)
            {
                Center = center;
                Velocity = velocity;
                Size = size;
            }
            /// <summary>
            /// 每帧推进：中心按速度平移、速度衰减到 0.96、尺寸收缩到 0.91（照源）。
            /// 虚无箭袋在场时弹幕每帧把尺寸重置为 120，故实际尺寸保持稳定；
            /// 弹幕消失后不再重置，粒子会在约 50 帧内缩到 2 像素以下被移除。
            /// </summary>
            public void Update()
            {
                Center += Velocity;
                Velocity *= 0.96f;
                Size *= 0.91f;
            }
        }
        // ── 静态字段 ──
        /// <summary>
        /// 当前存活的全部虚空场粒子（静态共享，故清理由 <see cref="ClearInstances"/> 负责）
        /// </summary>
        public static List<CosmicParticle> Particles { get; private set; } = new List<CosmicParticle>();
        // ── 属性 ──
        /// <summary>当前是否还有存活的虚空场粒子</summary>
        public override bool AnythingToDraw => Particles.Any();
        /// <summary>
        /// 绘制层级：BeforeProjectiles（照源）——画在弹幕之下，这样被强化过的箭矢
        /// 仍会盖在暗色虚空场之上，与灾厄观感一致
        /// </summary>
        public override GeneralDrawLayer DrawLayer => GeneralDrawLayer.BeforeProjectiles;
        /// <summary>边缘色：紫与黑按 0.75 混合后的深紫（照源）</summary>
        public override Color EdgeColor => Color.Lerp(Color.Purple, Color.Black, 0.75f);
        /// <summary>
        /// 参与合成的图层贴图序列（仅一层）：用灾厄的 StreamGougeLayer 星空纹作为场内的填充/滚动纹理
        /// </summary>
        public override IEnumerable<Texture2D> Layers
        {
            get
            {
                yield return ModContent.Request<Texture2D>("CalamityDemutation/Graphics/Metaballs/StreamGougeLayer").Value;
            }
        }
        // ── 公开方法 ──
        /// <summary>世界卸载时清空全部粒子，避免带着旧世界的虚空场进入新世界</summary>
        public override void ClearInstances() => Particles.Clear();
        /// <summary>
        /// 在世界坐标 position 处生成一个给定初始速度与尺寸的虚空场粒子，并把句柄返回给调用方
        /// （弹幕靠该句柄每帧钉住中心，故不能用 <c>void</c> 版）
        /// </summary>
        public static CosmicParticle SpawnParticle(Vector2 position, Vector2 velocity, float size)
        {
            CosmicParticle particle = new CosmicParticle(position, velocity, size);
            Particles.Add(particle);
            return particle;
        }
        /// <summary>图层手动滚动偏移：随时间沿 X 轴匀速平移，让星空纹缓慢流动（照源）</summary>
        public override Vector2 CalculateManualOffsetForLayer(int layerIndex)
        {
            return Vector2.UnitX * Main.GlobalTimeWrappedHourly * 0.037f;
        }
        /// <summary>每帧推进所有粒子运动并清理已收缩到 2 像素以下的粒子（照源）</summary>
        public override void Update()
        {
            for (int i = 0; i < Particles.Count; i++)
                Particles[i].Update();
            Particles.RemoveAll(p => p.Size <= 2f);
        }
        /// <summary>
        /// 把每个粒子按中心与尺寸画成实心圆（BasicCircle 贴图）到当前渲染目标，
        /// 之后的边缘着色器据此生成深紫描边。调用前外部必须已 Begin 过 spriteBatch。
        /// </summary>
        public override void DrawInstances()
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/BasicCircle").Value;
            foreach (CosmicParticle particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Vector2 origin = tex.Size() * 0.5f;
                Vector2 scale = Vector2.One * particle.Size / tex.Size();
                Main.spriteBatch.Draw(tex, drawPosition, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
