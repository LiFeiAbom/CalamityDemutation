using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// 配方组系统（照 tModLoader 的新口径写在 ModSystem 里——<c>Mod.AddRecipeGroups</c> 已标记过时）。
    /// 目前登记三项：虚无箭袋（QuiverofNihility）照灾厄 2.0.4 的配方要用「任意箭袋」入料，
    /// 现代版灾厄自带 <c>AnyQuiver</c> 组、经典版灾厄没有，故本模组自建同内容的
    /// <c>CalamityDemutation:AnyQuiver</c> 供经典分支使用（现代分支仍直接用灾厄自己的 AnyQuiver）；
    /// 静默剑鞘（SilencingSheath）的源配方用的是灾厄自建的 <c>AnyEvilBar</c> / <c>Boss2Material</c>
    /// 两个组（内容全是原版物品：魔金锭/血金锭、暗影鳞片/组织样本），本件刻意**不依赖灾厄**，
    /// 故同样自建同内容的两组供它使用；欺诈硬币（CoinofDeceit）的源配方又额外用到灾厄的
    /// <c>AnyCopperBar</c> 组（内容 = 铜锭/锡锭，同样是原版物品），一并自建同内容的
    /// <c>CalamityDemutation:AnyCopperBar</c> 供它使用。
    /// </summary>
    internal class RecipeSystem : ModSystem
    {
        /// <summary>
        /// 注册本模组的配方组：任意箭袋 = 魔法箭袋 / 熔火箭袋 / 潜猎者箭袋（与灾厄的 AnyQuiver 逐项一致）；
        /// 任意魔金锭 = 魔金锭 / 血金锭（= 灾厄 AnyEvilBar）；任意二阶 Boss 材料 = 暗影鳞片 / 组织样本
        /// （= 灾厄 Boss2Material）；任意铜锭 = 铜锭 / 锡锭（= 灾厄 AnyCopperBar）。
        /// </summary>
        public override void AddRecipeGroups()
        {
            RecipeGroup group = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.MagicQuiver)}", ItemID.MagicQuiver, ItemID.MoltenQuiver, ItemID.StalkersQuiver);
            RecipeGroup.RegisterGroup("CalamityDemutation:AnyQuiver", group);

            RecipeGroup copperBar = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.CopperBar)}", ItemID.CopperBar, ItemID.TinBar);
            RecipeGroup.RegisterGroup("CalamityDemutation:AnyCopperBar", copperBar);

            RecipeGroup evilBar = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.DemoniteBar)}", ItemID.DemoniteBar, ItemID.CrimtaneBar);
            RecipeGroup.RegisterGroup("CalamityDemutation:AnyEvilBar", evilBar);

            RecipeGroup boss2Material = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.ShadowScale)}", ItemID.ShadowScale, ItemID.TissueSample);
            RecipeGroup.RegisterGroup("CalamityDemutation:Boss2Material", boss2Material);
        }
    }
}
