using System.Collections.Generic;
using CalamityDemutation.Systems.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
namespace CalamityDemutation.Systems.UI
{
    /// <summary>
    /// 冷却机架 UI 的挂载系统：（只取灾厄 Systems/UIManagementSystem.cs 中调用 CooldownRackUI.Draw 的那一段，
    /// 其余与冷却无关的 UI 全部不要）在原版"Resource Bars"层之前插入一个 LegacyGameInterfaceLayer，
    /// 使冷却条绘制在 buff 栏同一层级上。
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    internal class CooldownRackUISystem:ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int buffDisplayIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Resource Bars");
            if (buffDisplayIndex != -1)
            {
                layers.Insert(buffDisplayIndex, new LegacyGameInterfaceLayer("Cooldown Rack UI", delegate ()
                {
                    CooldownRackUI.Draw(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
            }
        }
    }
}
