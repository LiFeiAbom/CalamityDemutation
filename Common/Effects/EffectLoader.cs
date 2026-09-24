using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 着色器加载器：请求并托管本模组全部 .fx 资源——
    /// 刀光 KnifeRendering / KnifeDistortion、屏幕扭曲 WarpShader、中子星扭曲 NeutronWarp、
    /// 变形球边缘 MetaballEdgeShader / AdditiveMetaballEdgeShader、亵渎之魂护盾 RoverDriveShieldShader、
    /// 深渊裂隙合成 cabyss。
    /// </summary>
    public class EffectLoader
    {
        /// <summary>
        /// 深渊裂隙合成着色器（移植自 CalamityEntropy 的 cabyss）：EffectsSystem 把深渊裂隙的白色遮罩
        /// 合成为蓝色深渊裂缝时使用，需配合 AwSky1 噪声贴图（tex1）与 clr 颜色参数
        /// </summary>
        public static Asset<Effect> AbyssShader;
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
        /// 亵渎之魂护盾着色器（移植自灾厄 RoverDriveShield），ProfanedSoulArtifact 绘制护罩气泡与边框圆环时使用
        /// </summary>
        public static Asset<Effect> RoverDriveShieldShader;
        /// <summary>
        /// 屏幕扭曲着色器，EffectsSystem 合成 IDrawWarp 弹幕的扭曲效果时使用
        /// </summary>
        public static Asset<Effect> WarpShader;
        /// <summary>
        /// 中子星扭曲着色器（移植自 CWR 的 NeutronWarp）：与 WarpShader 配套——由它把"位移方向 / 强度"
        /// 写进扭曲遮罩，再由 WarpShader 消费。中子枪的弹丸与爆炸靠它按技法程序化生成位移场，
        /// 取代旧版 CPU 叠几十层遮罩贴图的写法。使用见 <see cref="NeutronWarpHelper"/>
        /// </summary>
        public static Asset<Effect> NeutronWarp;
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
            NeutronWarp = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "NeutronWarp");
            MetaballEdgeShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "Metaballs/MetaballEdgeShader");
            AdditiveMetaballEdgeShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "Metaballs/AdditiveMetaballEdgeShader");
            RoverDriveShieldShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "RoverDriveShield");
            AbyssShader = assets.Request<Effect>(CalamityDemutationConstant.noEffects + "cabyss");
        }
        /// <summary>
        /// 卸载时把所有着色器 Asset 引用置空，便于热重载回收（注意方法名按既有约定写作 UnLoad）
        /// </summary>
        public static void UnLoad()
        {
            KnifeDistortion = null;
            KnifeRendering = null;
            WarpShader = null;
            NeutronWarp = null;
            MetaballEdgeShader = null;
            AdditiveMetaballEdgeShader = null;
            RoverDriveShieldShader = null;
            AbyssShader = null;
        }
    }
}
