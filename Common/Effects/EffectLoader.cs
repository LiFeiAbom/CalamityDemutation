using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 着色器加载器：请求并托管本模组全部 .fx 资源——
    /// 刀光 KnifeRendering / KnifeDistortion、屏幕扭曲 WarpShader、
    /// 变形球边缘 MetaballEdgeShader / AdditiveMetaballEdgeShader。
    /// </summary>
    public class EffectLoader
    {
        // ── 静态字段 ──
        /// <summary>
        /// 变形球边缘着色器（加法混合版），DragonsBreathMetaball 绘制时使用
        /// </summary>
        public static Asset<Effect> AdditiveMetaballEdgeShader;
        /// <summary>
        /// 刀光扭曲着色器，BaseSwingCO.WarpDraw 绘制挥舞弧光时使用
        /// </summary>
        public static Asset<Effect> KnifeDistortion;
        /// <summary>
        /// 刀光渲染着色器，BaseSwingCO / DragonRageHeld 绘制挥舞弧光时使用
        /// </summary>
        public static Asset<Effect> KnifeRendering;
        /// <summary>
        /// 变形球边缘着色器（普通版本），Metaball 绘制时使用
        /// </summary>
        public static Asset<Effect> MetaballEdgeShader;
        /// <summary>
        /// 屏幕扭曲着色器，EffectsSystem 合成 IDrawWarp 弹幕的扭曲效果时使用
        /// </summary>
        public static Asset<Effect> WarpShader;
        // ── 静态方法 ──
        /// <summary>
        /// 请求加载全部 .fx 着色器资源（异步）：路径前缀取 CalamityDemutationConstant.noEffects（"Effects/"）。
        /// 只持有 Asset 句柄，不在此处取 .Value，真正取值推迟到绘制期
        /// </summary>
        public static void LoadEffects()
        {
            // 用默认的异步加载：ImmediateLoad 会在模组加载期阻塞(日志里的 "blocking on asset loading" 警告)，
            // 改为只持有 Asset，在真正使用(绘制期)时才取 .Value。
            var assets = CalamityDemutation.Instance.Assets;
            KnifeRendering = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "KnifeRendering");
            KnifeDistortion = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "KnifeDistortion");
            WarpShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "WarpShader");
            MetaballEdgeShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "Metaballs/MetaballEdgeShader");
            AdditiveMetaballEdgeShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "Metaballs/AdditiveMetaballEdgeShader");
        }
        /// <summary>
        /// 卸载时把所有着色器 Asset 引用置空，便于热重载回收（注意方法名按既有约定写作 UnLoad）
        /// </summary>
        public static void UnLoad()
        {
            KnifeRendering = null;
            KnifeDistortion = null;
            WarpShader = null;
            MetaballEdgeShader = null;
            AdditiveMetaballEdgeShader = null;
        }
    }
}
