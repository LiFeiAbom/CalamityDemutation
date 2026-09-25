using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 翱翔药水（Soaring Potion）：饮用后获得 4 分钟（14400 帧）翱翔增益，
    /// 提升翅膀飞行时间与翅膀速度。效果结算见 Soaring、CalamityDemutationPlayer 与 GlobalItem。
    /// </summary>
    internal class SoaringPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 14400 帧（4 分钟）翱翔增益
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
            Item.buffType = ModContent.BuffType<Soaring>();   // 附加翱翔增益
            Item.buffTime = 14400;                            // 增益持续 14400 帧（4 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);           // 价值 2 金
        }
        /// <summary>
        /// 合成表：药剂瓶 + 羽毛 + 飞魂 + 灾厄阳光精华（现代版 EssenceofSunlight / 经典版 EssenceofCinder），
        /// 或药剂瓶 + 血珠×30，均在炼金台
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：药剂瓶 + 羽毛 + 飞魂 + EssenceofSunlight
                if(calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(ItemID.Feather);
                    recipe.AddIngredient(ItemID.SoulofFlight);
                    recipe.AddIngredient(essenceofSunlight1.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 现代版灾厄：药剂瓶 + 血珠×30
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 30);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：药剂瓶 + 羽毛 + 飞魂 + EssenceofCinder
                if(calamity1.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(ItemID.Feather);
                    recipe1.AddIngredient(ItemID.SoulofFlight);
                    recipe1.AddIngredient(essenceofCinder1.Type);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×30
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(bloodOrb2.Type, 30);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
