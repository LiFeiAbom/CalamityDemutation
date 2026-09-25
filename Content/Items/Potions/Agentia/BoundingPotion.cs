using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 弹跳药水（Bounding Potion）：饮用后获得 3 分钟（10800 帧）弹跳增益，
    /// 提升跳跃速度/高度并减免坠落伤害。效果结算见 Bounding 与 CalamityDemutationPlayer。
    /// </summary>
    internal class BoundingPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 10800 帧（3 分钟）弹跳增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                                   // 贴图宽（像素）
            Item.height = 18;                                  // 贴图高（像素）
            Item.useTurn = true;                               // 使用时允许转向
            Item.maxStack = 999;                               // 最大堆叠
            Item.rare = ItemRarityID.Orange;                   // 稀有度：橙色
            Item.useAnimation = 17;                            // 动画时长 17 帧
            Item.useTime = 17;                                 // 使用间隔 17 帧
            Item.useStyle = ItemUseStyleID.EatFood;            // 使用样式：进食
            Item.UseSound = SoundID.Item3;                     // 饮用音效
            Item.consumable = true;                            // 一次性消耗品
            Item.buffType = ModContent.BuffType<Bounding>();   // 附加弹跳增益
            Item.buffTime = 10800;                             // 增益持续 10800 帧（3 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);            // 价值 2 金
        }
        /// <summary>
        /// 合成表：现代版与经典版灾厄各两套配方，同名材料不同，故分两分支注册
        /// （药剂瓶处用植物/青蛙，炼金台处用灾厄血珠）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：药剂瓶 + 青蛙 + PlantyMush
                if(calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(ItemID.Frog);
                    recipe.AddIngredient(plantyMush.Type);
                    recipe.AddTile(TileID.Bottles);
                    recipe.Register();
                }
                // 现代版灾厄：药剂瓶 + 血珠×20（炼金台）
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
                // 经典版灾厄：药剂瓶 + 青蛙 + ManeaterBulb
                if(calamity1.TryFind<ModItem>("ManeaterBulb", out ModItem maneaterBulb))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(ItemID.Frog);
                    recipe.AddIngredient(maneaterBulb.Type);
                    recipe.AddTile(TileID.Bottles);
                    recipe.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×20（炼金台）
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb2.Type, 20);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
        }
    }
}
