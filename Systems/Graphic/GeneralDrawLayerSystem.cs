using System;
using CalamityDemutation.Enums;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems.Graphic
{
    /// <summary>
    /// 绘制层系统（移植自灾厄的 GeneralDrawLayerSystem，裁剪到当前实际使用的 AfterDusts 层）。
    /// 通过 On_Main.DrawDust 钩子在尘埃绘制后触发粒子绘制。
    /// </summary>
    internal sealed class GeneralDrawLayerSystem : ModSystem
    {
        public static event Action<GeneralDrawLayer> OnDrawLayer;
        public static event Action OnPrepareDraw;
        /// <summary>
        /// 加载时挂上两个 On_Main 钩子：CheckMonoliths（用于在绘制准备阶段触发 OnPrepareDraw）
        /// 与 DrawDust（在尘埃绘制之后触发 AfterDusts 层的 OnDrawLayer）
        /// </summary>
        public override void Load()
        {
            On_Main.CheckMonoliths += CheckMonoliths;
            On_Main.DrawDust += GeneralDrawLayer_DrawToLayer_AfterDusts;
        }
        /// <summary>
        /// On_Main.CheckMonoliths 钩子：先执行原版逻辑，再触发 OnPrepareDraw 通知订阅者做绘制前准备
        /// </summary>
        private static void CheckMonoliths(On_Main.orig_CheckMonoliths orig)
        {
            orig();
            OnPrepareDraw?.Invoke();
        }
        /// <summary>
        /// 卸载时把 OnDrawLayer 事件置空，从而一次性清除所有订阅者（例如 GeneralParticleHandler 的绘制回调）
        /// </summary>
        public override void Unload()
        {
            OnDrawLayer = null;
        }
        /// <summary>
        /// On_Main.DrawDust 钩子：先执行原版尘埃绘制，再触发 AfterDusts 层事件，
        /// 让粒子等图形系统绘制在尘埃之后（当前唯一实际接线的绘制层）
        /// </summary>
        private static void GeneralDrawLayer_DrawToLayer_AfterDusts(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);
            OnDrawLayer?.Invoke(GeneralDrawLayer.AfterDusts);
        }
    }
}
