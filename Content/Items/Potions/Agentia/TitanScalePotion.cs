using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 泰坦鳞片药水（Titan Scale Potion）：饮用后获得 5 分钟（18000 帧）泰坦鳞片增益，
    /// 提供伤害减免、防御与抗击退。效果结算见 TitanScale 与 CalamityDemutationPlayer。
    /// </summary>
    internal class TitanScalePotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 18000 帧（5 分钟）泰坦鳞片增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 18;
            Item.useTurn = true;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Orange;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.buffType = ModContent.BuffType<TitanScale>();   // 附加泰坦鳞片增益
            Item.buffTime = 18000;                                // 增益持续 18000 帧（5 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 合成表：泰坦药水 + 甲虫壳，或药剂瓶 + 血珠×10 + 甲虫壳，均在炼金台
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：泰坦药水 + 甲虫壳
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.TitanPotion);
                recipe.AddIngredient(ItemID.BeetleHusk);
                recipe.AddTile(TileID.AlchemyTable);
                recipe.Register();
                // 现代版灾厄：药剂瓶 + 血珠×10 + 甲虫壳
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 10);
                    recipe.AddIngredient(ItemID.BeetleHusk);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：泰坦药水 + 甲虫壳
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.TitanPotion);
                recipe1.AddIngredient(ItemID.BeetleHusk);
                recipe1.AddTile(TileID.AlchemyTable);
                recipe1.Register();
                // 经典版灾厄：血珠×10 + 药剂瓶 + 甲虫壳
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    recipe1 = CreateRecipe();
                    recipe1.AddIngredient(bloodOrb2.Type, 10);
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(ItemID.BeetleHusk);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
