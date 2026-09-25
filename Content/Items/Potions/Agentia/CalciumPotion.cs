using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 钙质药水（Calcium Potion）：饮用后获得 10 分钟（36000 帧）钙质增益，
    /// 使玩家免疫坠落伤害。效果结算见 Calcium 与 CalamityDemutationPlayer。
    /// </summary>
    internal class CalciumPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 36000 帧（10 分钟）钙质增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                                  // 贴图宽（像素）
            Item.height = 18;                                 // 贴图高（像素）
            Item.useTurn = true;                              // 使用时允许转向
            Item.maxStack = 999;                              // 最大堆叠
            Item.rare = ItemRarityID.Orange;                  // 稀有度：橙色
            Item.useAnimation = 17;                           // 动画时长 17 帧
            Item.useTime = 17;                                // 使用间隔 17 帧
            Item.useStyle = ItemUseStyleID.EatFood;           // 使用样式：进食
            Item.UseSound = SoundID.Item3;                    // 饮用音效
            Item.consumable = true;                           // 一次性消耗品
            Item.buffType = ModContent.BuffType<Calcium>();   // 附加钙质增益
            Item.buffTime = 36000;                            // 增益持续 36000 帧（10 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);           // 价值 2 金
        }
        /// <summary>
        /// 合成表：现代版与经典版灾厄均用同名 AncientBoneDust（药剂瓶），
        /// 或药剂瓶 + 血珠×10（炼金台），故两分支材料一致、仅取 Mod 来源不同
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：药剂瓶 + AncientBoneDust
                if(calamity.TryFind<ModItem>("AncientBoneDust", out ModItem ancientBoneDust1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(ancientBoneDust1.Type);
                    recipe.AddTile(TileID.Bottles);
                    recipe.Register();
                }
                // 现代版灾厄：药剂瓶 + 血珠×10（炼金台）
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 10);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：药剂瓶 + AncientBoneDust
                if(calamity1.TryFind<ModItem>("AncientBoneDust", out ModItem ancientBoneDust2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(ancientBoneDust2.Type);
                    recipe.AddTile(TileID.Bottles);
                    recipe.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×10（炼金台）
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb2.Type, 10);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
        }
    }
}
