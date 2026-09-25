using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 龙之药剂（Draconic Elixir）：饮用后获得 5 分钟（18000 帧）龙之涌动增益，
    /// 大幅强化飞行时间与防御，并受 draconicSurgeCooldown 冷却限制（冷却中不可饮用）。
    /// 效果结算见 DraconicSurgeBuff 与 CalamityDemutationPlayer。
    /// </summary>
    internal class DraconicElixir:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 18000 帧（5 分钟）龙之涌动增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                                            // 贴图宽（像素）
            Item.height = 18;                                           // 贴图高（像素）
            Item.useTurn = true;                                        // 使用时允许转向
            Item.maxStack = 999;                                        // 最大堆叠
            Item.rare = ItemRarityID.Orange;                            // 稀有度：橙色
            Item.useAnimation = 17;                                     // 动画时长 17 帧
            Item.useTime = 17;                                          // 使用间隔 17 帧
            Item.useStyle = ItemUseStyleID.EatFood;                     // 使用样式：进食
            Item.UseSound = SoundID.Item3;                              // 饮用音效
            Item.consumable = true;                                     // 一次性消耗品
            Item.buffType = ModContent.BuffType<DraconicSurgeBuff>();   // 附加龙之涌动增益
            Item.buffTime = 18000;                                      // 增益持续 18000 帧（5 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);                     // 价值 2 金
        }
        /// <summary>
        /// 使用限制：龙之涌动冷却（draconicSurgeCooldown）归零后才可再次饮用
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            return player.GetModPlayer<CalamityDemutationPlayer>().draconicSurgeCooldown == 0;   // 冷却中禁止饮用
        }
        /// <summary>
        /// 合成表：三种原版花草 + 灾厄龙魂碎片（现代版 YharonSoulFragment / 经典版 HellcasterFragment），
        /// 或药剂瓶 + 血珠×50 + 同款碎片，均在炼金台合成
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：药剂瓶 + YharonSoulFragment + 太阳花/月光草/火焰花
                if(calamity.TryFind<ModItem>("YharonSoulFragment", out ModItem yharonSoulFragment1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(yharonSoulFragment1.Type);
                    recipe.AddIngredient(ItemID.Daybloom);
                    recipe.AddIngredient(ItemID.Moonglow);
                    recipe.AddIngredient(ItemID.Fireblossom);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 现代版灾厄：药剂瓶 + 血珠×50 + YharonSoulFragment
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1) && calamity.TryFind<ModItem>("YharonSoulFragment", out ModItem yharonSoulFragment2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 50);
                    recipe.AddIngredient(yharonSoulFragment2.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：药剂瓶 + HellcasterFragment + 太阳花/月光草/火焰花
                if(calamity1.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(hellcasterFragment1.Type);
                    recipe.AddIngredient(ItemID.Daybloom);
                    recipe.AddIngredient(ItemID.Moonglow);
                    recipe.AddIngredient(ItemID.Fireblossom);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×50 + HellcasterFragment
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2) && calamity1.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb2.Type, 50);
                    recipe.AddIngredient(hellcasterFragment2.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
        }
    }
}
