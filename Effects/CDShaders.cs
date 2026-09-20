using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
namespace CalamityDemutation.Effects
{
    /// <summary>
    /// 着色器加载与注册系统（移植自灾厄的 CalamityShaders 模式）：
    /// 内容加载完成后从 Effects/ 目录加载 .fx 着色器，
    /// 以 "CalamityDemutation:" 前缀注册到 GameShaders.Misc 供弹幕绘制使用。
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class CDShaders : ModSystem
    {
        // ── 常量 ──
        /// <summary>
        /// 着色器资源所在目录（配 Mod.Assets.Request 使用的相对路径，不含模组名前缀）
        /// </summary>
        private const string ShaderPath = "Effects/";
        /// <summary>
        /// 注册进 GameShaders.Misc 时统一使用的模组名前缀
        /// </summary>
        private const string ShaderPrefix = "CalamityDemutation:";
        /// <summary>
        /// 环形冷却进度着色器的注册名，Cooldowns/CooldownHandler.cs 绘制冷却条时按此名取用
        /// </summary>
        public const string CircularBarShaderName = ShaderPrefix + "CircularBarShader";
        // ── 静态字段 ──
        /// <summary>
        /// HeavenlyGale 硬光箭的拖尾着色器（原灾厄 HeavenlyGaleTrail，PiercePass）
        /// </summary>
        internal static Asset<Effect> HeavenlyGaleTrailShader;
        /// <summary>
        /// PrimitiveRenderer 未指定着色器时的默认兜底着色器（仅输出顶点色）
        /// </summary>
        internal static Asset<Effect> StandardPrimitiveShader;
        /// <summary>
        /// 环形冷却进度条着色器（原灾厄 CalamityMod:CircularBarShader），
        /// 冷却机架 UI 在展开模式下用 uColor/uSecondaryColor 按完成度画出环形进度
        /// </summary>
        internal static Asset<Effect> CircularBarShader;
        /// <summary>
        /// 元素王者激光着色器（原灾厄 ArtemisLaser，TrailPass）：ElementalExcaliburRay 沿激光线拉出光束时使用
        /// </summary>
        internal static Asset<Effect> ArtemisLaserShader;
        /// <summary>
        /// 元素王者魔力阵着色器（原灾厄 ExoVortex，VortexPass）：ElementalExcaliburMagicCircle 绘制噪声法阵时使用
        /// </summary>
        internal static Asset<Effect> ExoVortexShader;
        /// <summary>
        /// 分形之羽拖尾着色器（原灾厄熵 ArtAttack，TrailPass）：FractalFeather 用 PrimitiveRenderer 拉丝带时使用
        /// </summary>
        internal static Asset<Effect> ArtAttackShader;
        /// <summary>
        /// 刀光透明变换着色器（原灾厄熵 SlashTrans，EnchantedPass）：无星之夜的 StarlessNightProj
        /// 用三角带拉刀光时使用，采样 uImage（拖尾噪声）与 uTransformImage（配色图）
        /// </summary>
        internal static Asset<Effect> SlashTransShader;
        /// <summary>
        /// 最终分形刀光着色器（原灾厄熵 FinalFrac，EffectPass）：FinalFractalHeld 与剑影弹幕用三角带拉拖尾时使用，
        /// 把底图（MotionTrail2）的红色通道当权重在 color1/color2 之间插值。与其它着色器不同，CE 是先在
        /// Immediate 批次里 <c>pass.Apply()</c> 再手动 <c>DrawUserPrimitives</c>（不是把 Effect 传给 Begin），
        /// 本模组照抄这个次序
        /// </summary>
        internal static Asset<Effect> FinalFracShader;
        // ── 生命周期方法 ──
        /// <summary>
        /// 内容加载完成后注册着色器：异步请求 Effects/ 下的 .fx 资源，
        /// 分别以 HeavenlyGaleTrail（PiercePass）、StandardPrimitiveShader（PrimitivePass）、
        /// CircularBarShader（Pass0）为名注册进 GameShaders.Misc，
        /// 供弹幕/拖尾/冷却条绘制时通过 GameShaders.Misc["CalamityDemutation:xxx"] 取用
        /// </summary>
        public override void PostSetupContent()
        {
            AssetRepository cdAssets = CalamityDemutation.Instance.Assets;
            // 用默认的异步加载：ImmediateLoad 会在模组加载期阻塞(日志里的 "blocking on asset loading" 警告)，
            // MiscShaderData 本身就接收 Asset，绘制时才取 .Value，无需在加载期就把着色器读出来。
            Asset<Effect> LoadShader(string path) => cdAssets.Request<Effect>($"{ShaderPath}{path}");
            HeavenlyGaleTrailShader = LoadShader("HeavenlyGaleTrailShader");
            RegisterMiscShader(HeavenlyGaleTrailShader, "PiercePass", "HeavenlyGaleTrail");
            StandardPrimitiveShader = LoadShader("StandardPrimitiveShader");
            RegisterMiscShader(StandardPrimitiveShader, "PrimitivePass", "StandardPrimitiveShader");
            // 环形冷却进度条：与灾厄 CalamityShaders 的注册方式一致，第二参数取 .fx 里的 pass 名（Pass0）
            CircularBarShader = LoadShader("CircularBarShader");
            RegisterMiscShader(CircularBarShader, "Pass0", "CircularBarShader");
            // 元素王者之剑系列：激光光束（TrailPass）与魔力阵噪声（VortexPass）
            ArtemisLaserShader = LoadShader("ArtemisLaserShader");
            RegisterMiscShader(ArtemisLaserShader, "TrailPass", "ArtemisLaser");
            ExoVortexShader = LoadShader("ExoVortexShader");
            RegisterMiscShader(ExoVortexShader, "VortexPass", "ExoVortex");
            // 分形系列：分形之羽的丝带拖尾（本模组第一个从灾厄熵搬来的着色器）
            ArtAttackShader = LoadShader("ArtAttack");
            RegisterMiscShader(ArtAttackShader, "TrailPass", "ArtAttack");
            // 分形系列：无星之夜的刀光透明变换
            SlashTransShader = LoadShader("SlashTrans");
            RegisterMiscShader(SlashTransShader, "EnchantedPass", "SlashTrans");
            // 分形系列：最终分形的刀光（三角带 + 手动 pass.Apply，用法见字段注释）
            FinalFracShader = LoadShader("FinalFrac");
            RegisterMiscShader(FinalFracShader, "EffectPass", "FinalFrac");
        }
        /// <summary>
        /// 卸载时从 Terraria 全局的 GameShaders.Misc 字典移除本模组注册的着色器并置空 Asset 引用，
        /// 否则注册项会持有本模组程序集，触发 "mod class still using memory" 警告
        /// </summary>
        public override void Unload()
        {
            // 注册进 GameShaders.Misc 的是 Terraria 侧的全局字典，不清理会把本模组程序集一直挂住，
            // 对应日志里的 "mod class still using memory" 警告
            GameShaders.Misc.Remove($"{ShaderPrefix}HeavenlyGaleTrail");
            GameShaders.Misc.Remove($"{ShaderPrefix}StandardPrimitiveShader");
            GameShaders.Misc.Remove(CircularBarShaderName);
            GameShaders.Misc.Remove($"{ShaderPrefix}ArtemisLaser");
            GameShaders.Misc.Remove($"{ShaderPrefix}ExoVortex");
            GameShaders.Misc.Remove($"{ShaderPrefix}ArtAttack");
            GameShaders.Misc.Remove($"{ShaderPrefix}SlashTrans");
            GameShaders.Misc.Remove($"{ShaderPrefix}FinalFrac");
            HeavenlyGaleTrailShader = null;
            StandardPrimitiveShader = null;
            CircularBarShader = null;
            ArtemisLaserShader = null;
            ExoVortexShader = null;
            ArtAttackShader = null;
            SlashTransShader = null;
            FinalFracShader = null;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 将已加载的着色器注册到 Terraria 图形引擎的 Misc 槽位，
        /// 注册名统一带 "CalamityDemutation:" 前缀，通过 GameShaders.Misc 访问。
        /// </summary>
        private static void RegisterMiscShader(Asset<Effect> shader, string passName, string registrationName)
        {
            MiscShaderData passParamRegistration = new(shader, passName);
            GameShaders.Misc[$"{ShaderPrefix}{registrationName}"] = passParamRegistration;
        }
    }
}
