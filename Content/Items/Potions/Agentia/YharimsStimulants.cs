using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 亚利姆兴奋剂：饮用后获得亚利姆之力增益，大幅提升战斗属性
    /// </summary>
    internal class YharimsStimulants:ModItem
    {
        /// <summary>
        /// 药水类消耗品：使用后获得 30 分钟（108000 帧）亚利姆之力增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 18;
            Item.useTurn = true;
            Item.maxStack = 30;
            Item.rare = ItemRarityID.Orange;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.buffType = ModContent.BuffType<YharimPower>();
            Item.buffTime = 108000;
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 提供两种合成路线（六瓶原版药水 / 灾厄血珠），均兼容现代版与经典版灾厄
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.EndurancePotion);
                recipe.AddIngredient(ItemID.IronskinPotion);
                recipe.AddIngredient(ItemID.SwiftnessPotion);
                recipe.AddIngredient(ItemID.ArcheryPotion);
                recipe.AddIngredient(ItemID.MagicPowerPotion);
                recipe.AddIngredient(ItemID.TitanPotion);
                recipe.AddTile(TileID.AlchemyTable);
                recipe.Register();
                recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("BloodOrb").Type, 50);
                recipe.AddIngredient(ItemID.BottledWater);
                recipe.AddTile(TileID.AlchemyTable);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.EndurancePotion);
                recipe1.AddIngredient(ItemID.IronskinPotion);
                recipe1.AddIngredient(ItemID.SwiftnessPotion);
                recipe1.AddIngredient(ItemID.ArcheryPotion);
                recipe1.AddIngredient(ItemID.MagicPowerPotion);
                recipe1.AddIngredient(ItemID.TitanPotion);
                recipe1.AddTile(TileID.AlchemyTable);
                recipe1.Register();
                recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("BloodOrb").Type, 50);
                recipe1.AddIngredient(ItemID.BottledWater);
                recipe1.AddTile(TileID.AlchemyTable);
                recipe1.Register();
            }
        }
    }
}
