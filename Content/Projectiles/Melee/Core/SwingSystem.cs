using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee.Core
{
    /// <summary>
    /// 挥砍刀光纹理系统（按需加载版，替代 CWR 的 ILoader 反射注册）。
    /// 每个弹幕类型在首次访问时，用其自定义纹理路径（为空则用默认刀光/颜色条）懒加载并缓存。
    /// </summary>
    internal static class SwingSystem
    {
        /// <summary>
        /// 颜色采样贴图缓存（弹幕类型 → 贴图资源）
        /// </summary>
        internal static Dictionary<int, Asset<Texture2D>> gradientTextures = [];
        /// <summary>
        /// 刀光流形贴图缓存（弹幕类型 → 贴图资源）
        /// </summary>
        internal static Dictionary<int, Asset<Texture2D>> trailTextures = [];
        /// <summary>
        /// 初始化：清空两张缓存字典，由 CalamityDemutation.Load() 调用，避免热重载时残留旧贴图引用。
        /// </summary>
        internal static void Load()
        {
            trailTextures = [];
            gradientTextures = [];
        }
        /// <summary>
        /// tML 卸载回调：由 CalamityDemutation.UnLoad() 调用，把两张缓存字典置空以释放贴图资源。
        /// </summary>
        internal static void UnLoad()
        {
            trailTextures = null;
            gradientTextures = null;
        }
        /// <summary>
        /// 按弹幕类型懒加载并缓存颜色采样贴图：customPath 为空时回退到默认的
        /// <c>CalamityDemutationConstant.ColorBar + "NullEffectColorBar"</c>，已缓存则直接返回。
        /// 由 <see cref="BaseSwingCO.GradientTexture"/> 属性调用。
        /// </summary>
        internal static Asset<Texture2D> GetGradientTexture(int type, string customPath)
        {
            if (!gradientTextures.TryGetValue(type, out Asset<Texture2D> tex))
            {
                string path = customPath == "" ? CalamityDemutationConstant.ColorBar + "NullEffectColorBar" : customPath;
                tex = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
                gradientTextures[type] = tex;
            }
            return tex;
        }
        /// <summary>
        /// 按弹幕类型懒加载并缓存刀光流形贴图：传入的 customPath 为空时回退到默认的
        /// <c>CalamityDemutationConstant.Masking + "MotionTrail3"</c>，已缓存则直接返回。
        /// 由 <see cref="BaseSwingCO.TrailTexture"/> 属性调用。
        /// </summary>
        internal static Asset<Texture2D> GetTrailTexture(int type, string customPath)
        {
            if (!trailTextures.TryGetValue(type, out Asset<Texture2D> tex))
            {
                string path = customPath == "" ? CalamityDemutationConstant.Masking + "MotionTrail3" : customPath;
                tex = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
                trailTextures[type] = tex;
            }
            return tex;
        }
    }
}
