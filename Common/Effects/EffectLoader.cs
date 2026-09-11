using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 刀光渲染 shader 加载器（精简版，仅保留 KnifeRendering/KnifeDistortion）
    /// </summary>
    public class EffectLoader
    {
        public static Asset<Effect> KnifeRendering;
        public static Asset<Effect> KnifeDistortion;
        public static Asset<Effect> WarpShader;
        public static Asset<Effect> MetaballEdgeShader;
        public static Asset<Effect> AdditiveMetaballEdgeShader;
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
