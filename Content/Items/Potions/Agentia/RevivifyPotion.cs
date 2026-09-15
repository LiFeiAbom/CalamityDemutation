using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 复苏药水（Revivify Potion）：使用后获得复苏增益，受到伤害时按本次伤害的 1/15 回复生命。
    /// 效果结算见 Revivify 与 CalamityDemutationPlayer.OnHurt。
    /// </summary>
    internal class RevivifyPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 18000 帧（5 分钟）复苏增益
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
            Item.buffType = ModContent.BuffType<Revivify>();   // 附加复苏增益
            Item.buffTime = 18000;                             // 增益持续 18000 帧（5 分钟）
            Item.consumable = true;
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 合成表：现代版为圣水 + StarblightSoot×4 + 水晶碎块 + 日光精华，
        /// 经典版为圣水×5 + Stardust×20 + 水晶碎块×5 + EssenceofCinder×3；
        /// 各自另有圣水 + 血珠（现代 20 / 经典 50）的备选配方（均在炼金台）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：圣水 + StarblightSoot×4 + 水晶碎块 + EssenceofSunlight
                if(calamity.TryFind<ModItem>("StarblightSoot", out ModItem starblightSoot1) && calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.HolyWater);
                    recipe.AddIngredient(starblightSoot1.Type, 4);
                    recipe.AddIngredient(ItemID.CrystalShard);
                    recipe.AddIngredient(essenceofSunlight1.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 现代版灾厄：圣水 + 血珠×20
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.HolyWater);
                    recipe.AddIngredient(bloodOrb1.Type, 20);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：圣水×5 + Stardust×20 + 水晶碎块×5 + EssenceofCinder×3
                if(calamity1.TryFind<ModItem>("Stardust", out ModItem stardust1) && calamity1.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.HolyWater, 5);
                    recipe1.AddIngredient(stardust1.Type, 20);
                    recipe1.AddIngredient(ItemID.CrystalShard, 5);
                    recipe1.AddIngredient(essenceofCinder1.Type, 3);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
                // 经典版灾厄：圣水×5 + 血珠×50
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(bloodOrb2.Type, 50);
                    recipe1.AddIngredient(ItemID.HolyWater, 5);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
