using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
namespace CalamityDemutation.Content.Particles
{
    /// <summary>
    /// 深渊粒子（AbyssalParticle，移植自 CalamityEntropy 的 PRT_Abyssal）：深渊分形与渊水弹吐出的暗色碎片。
    /// 它**不进常规粒子桶**——本类只承载位置、速度与透明度数据，真正的绘制在 EffectsSystem 的深渊裂隙上屏
    /// 合成里完成（按粒子位置把 cvmask 贴图当遮罩画出来，再由 cabyss 着色器染成蓝色深渊裂缝）。
    /// <para>
    /// 与 CE 原版的差异：① CE 的 <c>PRT_Abyssal</c> 继承 <c>PRT_Void</c> 只是为了给 EffectLoader 分流
    /// （虚空/深渊两套 RT 各画各的），本工程只有一条上屏路径，直接从 <see cref="BaseParticle"/> 派生即可；
    /// ② CE 的 <c>Opacity</c> 由基类提供，本模组基类没有该字段，这里自带同名字段承载淡出
    /// （同时也是上屏绘制时的强度）；③ 去掉 CE 的对象池（<c>CanPool</c>/<c>Reset</c>）与
    /// <c>ShouldKillWhenOffScreen</c>（本模组粒子系统不淘汰出屏粒子）；
    /// ④ 贴图指向 cvmask 只是为了让粒子注册表拿到一张合法贴图（CE 同样塞占位贴图防警告），默认绘制路径不会用到它。
    /// </para>
    /// </summary>
    internal class AbyssalParticle:BaseParticle
    {
        /// <summary>速度衰减系数（CE 老字段 vd）：每帧速度乘以它</summary>
        public float vd = 0.99f;
        /// <summary>透明度衰减系数（CE 老字段 ad）：每帧透明度减去它</summary>
        public float ad = 0.014f;
        /// <summary>当前透明度：既是存活判据（跌破 0.05 即消失），也是上屏绘制时的强度</summary>
        public float Opacity = 1f;
        /// <summary>占位贴图，见类注释第 ④ 条（本粒子的绘制在 EffectsSystem 里）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/cvmask";
        /// <summary>不走常规绘制路径，绘制统一交给上屏合成</summary>
        public override bool UseCustomDraw => true;
        /// <summary>生成时透明度归 1（沿用 CE 基类 SetProperty 的初值）</summary>
        public override void SetDRK() => Opacity = 1f;
        /// <summary>
        /// 每帧按 vd/ad 衰减速度与透明度，透明度跌破 0.05 即消失
        /// （CE 的深渊这批阈值就是 0.05，虚空那批才是 0.02）
        /// </summary>
        public override void AI()
        {
            Opacity -= ad;
            Velocity *= vd;
            if (Opacity < 0.05f)
            {
                Kill();
            }
        }
    }
}
