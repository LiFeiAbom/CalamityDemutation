using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 韵律药水（Cadence Potion）：饮用后获得 5 分钟（18000 帧）韵律增益，
    /// 提供商店打折、生命磁铁、生命回复与最大生命提升。效果结算见 Cadence 与 CalamityDemutationPlayer。
    /// </summary>
    internal class CadencePotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 18000 帧（5 分钟）韵律增益
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
            Item.buffType = ModContent.BuffType<Cadence>();   // 附加韵律增益
            Item.buffTime = 18000;                            // 增益持续 18000 帧（5 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 合成表：五瓶原版生活类药水，或灾厄血珠×40，均兼容现代版与经典版灾厄（炼金台合成）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：爱意 + 心灵感应 + 生命之力 + 再生 + 平静 五瓶药水
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.LovePotion);
                recipe.AddIngredient(ItemID.HeartreachPotion);
                recipe.AddIngredient(ItemID.LifeforcePotion);
                recipe.AddIngredient(ItemID.RegenerationPotion);
                recipe.AddIngredient(ItemID.CalmingPotion);
                recipe.AddTile(TileID.AlchemyTable);
                recipe.Register();
                // 现代版灾厄：药剂瓶 + 血珠×40
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 40);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：同为上述五瓶原版药水
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.LovePotion);
                recipe1.AddIngredient(ItemID.HeartreachPotion);
                recipe1.AddIngredient(ItemID.LifeforcePotion);
                recipe1.AddIngredient(ItemID.RegenerationPotion);
                recipe1.AddIngredient(ItemID.CalmingPotion);
                recipe1.AddTile(TileID.AlchemyTable);
                recipe1.Register();
                // 经典版灾厄：药剂瓶 + 血珠×40
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(bloodOrb2.Type, 40);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
