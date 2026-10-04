using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 着色器加载器：请求并托管本模组全部 .fx 资源——
    /// 刀光 KnifeRendering / KnifeDistortion、屏幕扭曲 WarpShader、中子星扭曲 NeutronWarp、
    /// 变形球边缘 MetaballEdgeShader / AdditiveMetaballEdgeShader、
    /// 深渊裂隙合成 cabyss、颜色插值 ColorLerp。
    /// </summary>
    /// <remarks>
    /// ⚠️ 本工程的 .fx 不是由 tModLoader 在构建时编译的：每新增一个 .fx，都必须先用 tModLoader 自带的 fxc
    /// 在 Effects/ 目录里就地编译出同名 .fxc，构建才会把它打进 .tmod：
    /// <c>FXC\fxc.exe /nologo /T fx_2_0 /Fo 名字.fxc 名字.fx</c>（会刷一条 X4717 "Effects deprecated" 警告，属正常）。
    /// 少编译这一步的后果不是"贴图不显示"，而是运行期 <c>Mod.Assets.Request&lt;Effect&gt;</c> 直接抛
    /// MissingResourceException、整个模组被 tModLoader 禁用——所以新增着色器后务必确认 Effects/ 下同名的 .fxc 已生成。
    /// </remarks>
    public class EffectLoader
    {
        /// <summary>
        /// 变形球边缘着色器（加法混合版），DragonsBreathMetaball 绘制时使用
        /// </summary>
        public static Asset<Effect> AdditiveMetaballEdgeShader;
        /// <summary>
        /// 刀光扭曲着色器，BaseSwingCO.WarpDraw 绘制挥舞弧光时使用
        /// </summary>
        public static Asset<Effect> KnifeDistortion;
        /// <summary>
        /// 刀光条带着色器，BaseSwingCO / DragonRageHeld 的 DrawTrail 绘制弧光拖尾时使用。
        /// 必须在加载期就请求：这个 Asset 用的是默认的异步加载，若等到绘制期才现请求，
        /// 同一表达式里紧跟的 .Value 会取到空引用（大修能吃住那句现请求，是因为它的
        /// EffectLoader 在加载期已把本资源预加载并缓存）
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
        /// <summary>
        /// 请求加载全部 .fx 着色器资源（异步）：路径前缀取 CalamityDemutationConstant.noEffects（"Effects/"）。
        /// 只持有 Asset 句柄，不在此处取 .Value，真正取值推迟到绘制期
        /// </summary>
        public static void LoadEffects()
        {
            // 用默认的异步加载：ImmediateLoad 会在模组加载期阻塞(日志里的 "blocking on asset loading" 警告)，
            // 改为只持有 Asset，在真正使用(绘制期)时才取 .Value。
            var assets = CalamityDemutation.Instance.Assets;
            KnifeDistortion = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "KnifeDistortion");
            KnifeRendering = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "KnifeRendering");
            WarpShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "WarpShader");
            MetaballEdgeShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "Metaballs/MetaballEdgeShader");
            AdditiveMetaballEdgeShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "Metaballs/AdditiveMetaballEdgeShader");
        }
        /// <summary>
        /// 卸载时把所有着色器 Asset 引用置空，便于热重载回收（注意方法名按既有约定写作 UnLoad）
        /// </summary>
        public static void UnLoad()
        {
            KnifeDistortion = null;
            KnifeRendering = null;
            WarpShader = null;
            MetaballEdgeShader = null;
            AdditiveMetaballEdgeShader = null;
        }
    }
}
