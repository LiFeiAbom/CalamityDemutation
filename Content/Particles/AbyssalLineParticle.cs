using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 深渊斩击线（AbyssalLineParticle，移植自 CalamityEntropy 的 PRT_AbyssalLine）：
    /// 把一张几何圆（a_circle）横向拉成条带、双层叠出刃光，逐帧收细并淡出。
    /// 噬渊鞭挞（Ystralyn）命中敌人时在原地撒 4 颗。
    /// <para>
    /// CE 注释里特别写明：它跟 <see cref="AbyssalParticle"/>（深渊裂隙那套走上屏 RT 的数据型粒子）**不是一回事**，
    /// 这是常规桶里带 PreDraw 的普通粒子，故本模组按后者口径另起名 AbyssalLineParticle 以示区分。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 基类换成本模组 <see cref="BaseParticle"/>（<c>SetProperty→SetDRK</c>、
    /// <c>PreDraw→UseCustomDraw + CustomDraw</c>）；② CE 的 <c>Opacity</c> 在本模组基类里没有，
    /// 且唯一调用点传的就是 1，绘制里乘的是每帧现算的 <c>1 - LifetimeCompletion</c>，故整条 Opacity 链省掉；
    /// ③ <c>Glow</c> 字段在 CE 里只写不读（原 PreDraw 里的混合切换是注释掉的），删掉；
    /// ④ <c>PRTDrawMode</c> 在唯一调用点恒为加法混合，固定走 <see cref="UseAdditiveBlend"/>（同
    /// <see cref="SparkleParticle"/> 的处理口径）；⑤ 去掉对象池（<c>CanPool</c>/<c>Reset</c>）与
    /// <c>ShouldKillWhenOffScreen</c>；⑥ 贴图按工程做法放同目录同名（CE 指向 Content/Particles/AbyssalLine，
    /// 但那只是占位——真正画的是 a_circle）。
    /// </para>
    /// </summary>
    internal class AbyssalLineParticle:BaseParticle
    {
        /// <summary>亮端颜色（CE 默认 190,190,255）</summary>
        public Color SpawnColor = new Color(190, 190, 255);
        /// <summary>暗端颜色（CE 默认纯蓝）</summary>
        public Color EndColor = Color.Blue;
        /// <summary>当前横向长度倍率，AI 里每帧加上 <see cref="XAdd"/> 后把 XAdd 衰减</summary>
        public float XScale = 0f;
        /// <summary>XAdd 的每帧衰减系数</summary>
        public float XDec = 0.87f;
        /// <summary>横向长度增量，AI 里先加到 XScale 再自乘衰减</summary>
        public float XAdd = 3.2f;
        /// <summary>厚度倍率，AI 里每帧 ×0.88，缩到 0.01 以下就自行 Kill（不必等寿命到点）</summary>
        public float LineScale = 3f;
        /// <summary>几何圆贴图（CE 的 Assets/Extra/a_circle）</summary>
        private const string CircleTexture = "CalamityDemutation/Assets/ExtraTextures/a_circle";
        /// <summary>加法混合（CE 在唯一调用点钉死的模式）</summary>
        public override bool UseAdditiveBlend => true;
        /// <summary>需要自绘（双层条带叠画）</summary>
        public override bool UseCustomDraw => true;
        /// <summary>到达 Lifetime 后自动移除</summary>
        public override bool SetLifetime => true;
        /// <summary>生成时补上默认寿命 50 帧（CE 旧默认值）</summary>
        public override void SetDRK()
        {
            if (Lifetime <= 0)
            {
                Lifetime = 50;
            }
        }
        /// <summary>
        /// 按 CE 的 Configure 语义设置参数（调用点在 <c>DRKLoader.NewParticle</c> 之后）：
        /// 只保留真正被读的旋转角，初始寿命由 <see cref="SetDRK"/> 兜底。
        /// </summary>
        public void Configure(float rotation = 0f)
        {
            Rotation = rotation;
        }
        public override void AI()
        {
            LineScale *= 0.88f;
            XScale += XAdd;
            XAdd *= XDec;
            Velocity *= 0.96f;
            // 厚度缩到 0.01 且还没到最后一帧就自杀，不必等寿命到点（CE 原样）
            if (Time < Lifetime - 1 && LineScale <= 0.01f)
            {
                Kill();
            }
        }
        /// <summary>
        /// 自绘两层：外层宽而淡（厚度 0.56、透明度 0.7）、内层窄而亮（厚度 0.2、全不透明），
        /// 颜色在暗端与亮端之间按剩余寿命插值。画的是 a_circle 拉成的条带，不是本粒子的占位贴图。
        /// </summary>
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            float remaining = 1f - LifetimeCompletion;
            Texture2D circleTexture = ModContent.Request<Texture2D>(CircleTexture).Value;
            Color lerpColor = Color.Lerp(EndColor, SpawnColor, remaining);
            Vector2 drawPosition = Position - Main.screenPosition;
            spriteBatch.Draw(circleTexture, drawPosition, null, lerpColor * remaining * 0.7f, Rotation, circleTexture.Size() / 2f, new Vector2(0.6f * (XScale + 0.1f), 0.56f * LineScale) * Scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(circleTexture, drawPosition, null, lerpColor * remaining, Rotation, circleTexture.Size() / 2f, new Vector2(0.6f * XScale, 0.2f * LineScale) * Scale, SpriteEffects.None, 0f);
        }
    }
}
