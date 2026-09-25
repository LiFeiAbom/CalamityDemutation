using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 凯旋药水（Triumph Potion）：饮用后获得 2 分钟（7200 帧）凯旋增益，
    /// 被接触命中时按目标剩余生命比例减伤（最高 25%）。效果结算见 Triumph 与 CalamityDemutationPlayer。
    /// </summary>
    internal class TriumphPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 7200 帧（2 分钟）凯旋增益
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
            Item.buffType = ModContent.BuffType<Triumph>();   // 附加凯旋增益
            Item.buffTime = 7200;                             // 增益持续 7200 帧（2 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);           // 价值 2 金
        }
        /// <summary>
        /// 合成表：现代版为药剂瓶 + PearlShard×3（炼金台），经典版为药剂瓶 + StormlionMandible + VictoryShard×3；
        /// 各自另有药剂瓶 + 血珠×30 的备选配方。注：经典版主配方用的是药剂瓶（Bottles）而非炼金台，与其余配方不一致
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：药剂瓶 + PearlShard×3（炼金台）
                if(calamity.TryFind<ModItem>("PearlShard", out ModItem pearlShard1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(pearlShard1.Type, 3);
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
                // 经典版灾厄：药剂瓶 + StormlionMandible + VictoryShard×3（此处为药剂瓶合成，非炼金台）
                if(calamity1.TryFind<ModItem>("StormlionMandible", out ModItem stormlionMandible1) && calamity1.TryFind<ModItem>("VictoryShard", out ModItem victoryShard1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(stormlionMandible1.Type);
                    recipe1.AddIngredient(victoryShard1.Type, 3);
                    recipe1.AddTile(TileID.Bottles);
                    recipe1.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×30（炼金台）
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
