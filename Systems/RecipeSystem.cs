using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// 配方组系统（照 tModLoader 的新口径写在 ModSystem 里——<c>Mod.AddRecipeGroups</c> 已标记过时）。
    /// 目前只登记一项：虚无箭袋（QuiverofNihility）照灾厄 2.0.4 的配方要用「任意箭袋」入料，
    /// 现代版灾厄自带 <c>AnyQuiver</c> 组、经典版灾厄没有，故本模组自建同内容的
    /// <c>CalamityDemutation:AnyQuiver</c> 供经典分支使用（现代分支仍直接用灾厄自己的 AnyQuiver）。
    /// </summary>
    internal class RecipeSystem : ModSystem
    {
        /// <summary>
        /// 注册本模组的配方组：任意箭袋 = 魔法箭袋 / 熔火箭袋 / 潜猎者箭袋（与灾厄的 AnyQuiver 逐项一致）
        /// </summary>
        public override void AddRecipeGroups()
        {
            RecipeGroup group = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.MagicQuiver)}", ItemID.MagicQuiver, ItemID.MoltenQuiver, ItemID.StalkersQuiver);
            RecipeGroup.RegisterGroup("CalamityDemutation:AnyQuiver", group);
        }
    }
}
