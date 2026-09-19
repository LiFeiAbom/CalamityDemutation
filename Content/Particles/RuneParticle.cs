using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 符文粒子（RuneParticle，移植自 CalamityEntropy 的 PRT_RuneParticle）：随机挑一枚符文小图，
    /// 从蓝紫渐变到纯白并逐帧淡出。符文之歌的挥砍、蓄力与符文脉冲束命中时都会撒一片。
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 14 张符文图是 14 个独立文件
    /// （<c>Content/Particles/Runes/r0</c>~<c>r13</c>），本模组按粒子的帧变体机制把它们纵向拼成
    /// 与类同名的 Content/Particles/RuneParticle.png，用基类的 <c>FrameVariants</c>/<c>Variant</c> 索引；
    /// ③ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与 <c>ShouldKillWhenOffScreen</c>
    /// （本模组粒子系统不淘汰出屏粒子）；④ CE 的 <c>PRTDrawMode</c> 三态混合里所有调用点都只传
    /// AdditiveBlend，本粒子直接固定走 <see cref="UseAdditiveBlend"/>；
    /// ⑤ <c>Configure</c> 由返回 this 的链式写法改为普通设置方法，且只留下调用点实际用到的寿命参数
    /// （CE 的 opacity 参数随后会被 AI 重算、mode 恒为加法混合、rotation 恒为 0，均不保留）。
    /// </para>
    /// </summary>
    internal class RuneParticle:BaseParticle
    {
        /// <summary>符文图共有 14 种（CE 的 Runes/r0~r13）</summary>
        private const int RuneVariants = 14;
        /// <summary>当前透明度：随寿命从 1 线性降到 0，绘制时与颜色相乘</summary>
        public float Opacity = 1f;
        /// <summary>是否发光：为假时颜色改为采样环境光照（CE 的 Configure 里恒传 true）</summary>
        public bool Glow = true;
        /// <summary>贴图与类同名同目录（纵向 14 帧的符文条带）</summary>
        public override string Texture => "CalamityDemutation/Content/Particles/RuneParticle";
        /// <summary>贴图纵向排了 14 枚符文</summary>
        public override int FrameVariants => RuneVariants;
        /// <summary>加法混合，使符文发光</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>需要自绘（按 Variant 取单帧，且颜色随寿命重算）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>按 CE 的 Configure 语义设置寿命（其余参数在 AI 里另有算法，不从此处取）</summary>
        public void Configure(int lifetime)
        {
            if (lifetime > 0)
            {
                Lifetime = lifetime;
            }
        }
        /// <summary>生成时随机挑一枚符文，并补上默认寿命 42 帧</summary>
        public override void SetDRK()
        {
            Variant = Main.rand.Next(RuneVariants);
            if (Lifetime <= 0)
            {
                Lifetime = 42;
            }
        }
        public override void AI()
        {
            Opacity = 1f - LifetimeCompletion;
            Color = Color.Lerp(new Color(110, 120, 255), Color.White, Opacity);
        }
        /// <summary>自绘：按 Variant 取单帧符文，颜色再乘透明度（不发光时先叠环境光）</summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = DRKLoader.ParticleIDToTexturesDic[Type].Value;
            Rectangle frame = texture.Frame(1, FrameVariants, 0, Variant);
            Color color = Color;
            if (!Glow)
            {
                color = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), color);
            }
            color *= Opacity;
            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, color, Rotation, frame.Size() / 2f, Scale, SpriteEffects.None, 0f);
        }
    }
}
