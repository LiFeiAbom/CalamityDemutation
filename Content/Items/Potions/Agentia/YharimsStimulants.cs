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
            Item.width = 28;                              // 贴图宽（像素）
            Item.height = 18;                             // 贴图高（像素）
            Item.useTurn = true;                          // 使用时允许转向
            Item.maxStack = 999;                          // 最大堆叠
            Item.rare = ItemRarityID.Orange;              // 稀有度：橙色
            Item.useAnimation = 17;                       // 动画时长 17 帧
            Item.useTime = 17;                            // 使用间隔 17 帧
            Item.useStyle = ItemUseStyleID.EatFood;       // 使用样式：进食
            Item.UseSound = SoundID.Item3;                // 饮用音效
            Item.consumable = true;                       // 一次性消耗品
            Item.buffType = ModContent.BuffType<YharimPower>();  // 给予"亚利姆之力"增益
            Item.buffTime = 108000;                       // 增益时长 108000 帧（30 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);       // 价值 2 金
        }
        /// <summary>
        /// 提供两种合成路线（六瓶原版药水 / 灾厄血珠），均兼容现代版与经典版灾厄
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄 ──
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 路线一：六瓶原版增益药水，炼药桌合成
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.EndurancePotion);      // 耐力药水
                recipe.AddIngredient(ItemID.IronskinPotion);       // 铁皮药水
                recipe.AddIngredient(ItemID.SwiftnessPotion);      // 敏捷药水
                recipe.AddIngredient(ItemID.ArcheryPotion);        // 箭术药水
                recipe.AddIngredient(ItemID.MagicPowerPotion);     // 魔能药水
                recipe.AddIngredient(ItemID.TitanPotion);          // 泰坦药水
                recipe.AddTile(TileID.AlchemyTable);               // 炼药桌
                recipe.Register();
                // 路线二：灾厄材料血珠 ×50 + 瓶装水
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    recipe = CreateRecipe();
                    recipe.AddIngredient(bloodOrb1.Type, 50);       // 灾厄材料：血珠 ×50
                    recipe.AddIngredient(ItemID.BottledWater);      // 瓶装水
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            // ── 经典版灾厄：配方完全相同，只是分开注册（两版血珠同名） ──
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
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    recipe1 = CreateRecipe();
                    recipe1.AddIngredient(bloodOrb2.Type, 50);
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
