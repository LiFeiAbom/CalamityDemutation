using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Attack
{
    /// <summary>
    /// 虚空灭绝 - 攻击型专家饰品，由灾厄之戒 + 地狱火 + 黑曜石玫瑰进阶合成
    /// 提供 +15% 通用伤害（身处岩浆时额外 +25%）、火焰免疫与 +240 秒岩浆安全时间；
    /// 受击无敌帧期间概率降下烈焰，并周期性从天空喷发更强力的虚空地狱火。
    /// </summary>
    internal class VoidofExtinction:ModItem
    {
        /// <summary>
        /// 基础属性：中尺寸贴图、专家限定、售价 30 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 30, 0, 0);
            Item.expert = true;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时仅置位标记，增伤/岩浆抗性/受击与周期烈焰统一在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().voidofExtinction = true;
        }
        /// <summary>
        /// 互斥判定：本饰品已继承灾厄之戒，故禁止与灾厄之戒或大杂烩同装备，避免效果叠加
        /// </summary>
        public override bool CanEquipAccessory(Player player, int slot, bool modded)/* tModPorter Suggestion: Consider using new hook CanAccessoryBeEquippedWith */
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.calamityRing || modPlayer.theAmalgam)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 配方：黑曜石玫瑰 + 地狱火 + 灾厄之戒在秘银砧合成（兼容现代版与经典版灾厄）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.ObsidianRose);
                recipe.AddIngredient<Gehenna>();
                recipe.AddIngredient<CalamityRing>();
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
