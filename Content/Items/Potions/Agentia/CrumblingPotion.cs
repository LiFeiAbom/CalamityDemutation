using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 碎块药水（Crumbling Potion）：饮用后获得 5 分钟（18000 帧）破甲增益，
    /// 提升通用暴击率并在命中时追加灾厄破甲减益。效果结算见 ArmorCrumbling 与 CalamityDemutationPlayer。
    /// </summary>
    internal class CrumblingPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 18000 帧（5 分钟）破甲增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                                         // 贴图宽（像素）
            Item.height = 18;                                        // 贴图高（像素）
            Item.useTurn = true;                                     // 使用时允许转向
            Item.maxStack = 999;                                     // 最大堆叠
            Item.rare = ItemRarityID.Orange;                         // 稀有度：橙色
            Item.useAnimation = 17;                                  // 动画时长 17 帧
            Item.useTime = 17;                                       // 使用间隔 17 帧
            Item.useStyle = ItemUseStyleID.EatFood;                  // 使用样式：进食
            Item.UseSound = SoundID.Item3;                           // 饮用音效
            Item.consumable = true;                                  // 一次性消耗品
            Item.buffType = ModContent.BuffType<ArmorCrumbling>();   // 附加破甲增益
            Item.buffTime = 18000;                                   // 增益持续 18000 帧（5 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);                  // 价值 2 金
        }
        /// <summary>
        /// 合成表：主配方每次产出 5 瓶（炼金台），备选配方为药剂瓶 + 血珠×20；
        /// 两分支的灾厄材料名不同，故分别注册
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：水×5 + AncientBoneDust + 原版远古战斗护甲材料 + 日光精华（每次 5 瓶）
                if(calamity.TryFind<ModItem>("AncientBoneDust", out ModItem ancientBoneDust1) && calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight))
                {
                    Recipe recipe = CreateRecipe(5);
                    recipe.AddIngredient(ItemID.BottledWater, 5);
                    recipe.AddIngredient(ancientBoneDust1.Type);
                    recipe.AddIngredient(ItemID.AncientBattleArmorMaterial);
                    recipe.AddIngredient(essenceofSunlight.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 现代版灾厄：药剂瓶 + 血珠×20
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 20);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：水×5 + AncientBoneDust + 原版远古战斗护甲材料 + EssenceofCinder（每次 5 瓶）
                if(calamity1.TryFind<ModItem>("AncientBoneDust", out ModItem ancientBoneDust2) && calamity1.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder))
                {
                    Recipe recipe1 = CreateRecipe(5);
                    recipe1.AddIngredient(ItemID.BottledWater, 5);
                    recipe1.AddIngredient(ancientBoneDust2.Type);
                    recipe1.AddIngredient(ItemID.AncientBattleArmorMaterial);
                    recipe1.AddIngredient(essenceofCinder.Type);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×20
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(bloodOrb2.Type, 20);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
