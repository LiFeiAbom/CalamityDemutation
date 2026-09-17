using System;
using CalamityDemutation.Enums;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems.Graphic
{
    /// <summary>
    /// 绘制层系统（移植自灾厄的 GeneralDrawLayerSystem）。
    /// 通过 On_Main.DrawProjectiles 在弹幕绘制后触发 AfterProjectiles 层，
    /// 通过 On_Main.DrawPlayers_AfterProjectiles 在玩家绘制后触发 AfterPlayers 层，
    /// 通过 On_Main.DrawDust 在尘埃绘制后触发 AfterDusts 层。
    /// </summary>
    internal sealed class GeneralDrawLayerSystem : ModSystem
    {
        /// <summary>
        /// 在指定绘制层级触发的事件；订阅者（如 GeneralParticleHandler、MetaballManager）据此挂接绘制
        /// </summary>
        public static event Action<GeneralDrawLayer> OnDrawLayer;
        /// <summary>
        /// 绘制前准备阶段的事件，在 On_Main.CheckMonoliths 之后触发；订阅者（如 MetaballManager）据此预渲染到离屏目标
        /// </summary>
        public static event Action OnPrepareDraw;
        /// <summary>
        /// 加载时挂上四个 On_Main 钩子：CheckMonoliths（触发 OnPrepareDraw 准备阶段）、
        /// DrawProjectiles（弹幕绘制后触发 AfterProjectiles 层）、DrawPlayers_AfterProjectiles（玩家绘制后触发 AfterPlayers 层）
        /// 与 DrawDust（尘埃绘制后触发 AfterDusts 层）
        /// </summary>
        public override void Load()
        {
            On_Main.CheckMonoliths += CheckMonoliths;
            On_Main.DrawDust += GeneralDrawLayer_DrawToLayer_AfterDusts;
            On_Main.DrawProjectiles += GeneralDrawLayer_DrawToLayer_AfterProjectiles;
            On_Main.DrawPlayers_AfterProjectiles += GeneralDrawLayer_DrawToLayer_AfterPlayers;
        }
        /// <summary>
        /// 卸载时把两个静态事件都置空，一次性清除所有订阅者。
        /// OnDrawLayer 的订阅者多为静态方法，OnPrepareDraw 则有实例方法订阅者（MetaballManager.PrepareMetaballTargets），
        /// 不置空会让静态事件一直挂住该 ModSystem 实例，触发 "mod class still using memory" 警告
        /// </summary>
        public override void Unload()
        {
            OnDrawLayer = null;
            OnPrepareDraw = null;
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
        /// On_Main.DrawDust 钩子：先执行原版尘埃绘制，再触发 AfterDusts 层事件，
        /// 让粒子等图形系统绘制在尘埃之后
        /// </summary>
        private static void GeneralDrawLayer_DrawToLayer_AfterDusts(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);
            OnDrawLayer?.Invoke(GeneralDrawLayer.AfterDusts);
        }
        /// <summary>
        /// On_Main.DrawProjectiles 钩子：先执行原版弹幕绘制，再触发 AfterProjectiles 层事件，
        /// 让龙息 Metaball 等要求在弹幕之上合成的图形绘制在弹幕之后
        /// </summary>
        private static void GeneralDrawLayer_DrawToLayer_AfterProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);
            OnDrawLayer?.Invoke(GeneralDrawLayer.AfterProjectiles);
        }
        /// <summary>
        /// On_Main.DrawPlayers_AfterProjectiles 钩子：先执行原版玩家绘制，再触发 AfterPlayers 层事件，
        /// 让亵渎之魂护盾等要求在玩家之上绘制的图形画在玩家之后（不被玩家身体遮挡）
        /// </summary>
        private static void GeneralDrawLayer_DrawToLayer_AfterPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
        {
            orig(self);
            OnDrawLayer?.Invoke(GeneralDrawLayer.AfterPlayers);
        }
    }
}
