using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Content.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 亵渎之怒药水（Profaned Rage Potion）：饮用后获得 3 分钟（10800 帧）亵渎之怒增益，
    /// 提供暴击率、移速与翅膀速度加成。效果结算见 ProfanedRage、CalamityDemutationPlayer 与 GlobalItem。
    /// </summary>
    internal class ProfanedRagePotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 10800 帧（3 分钟）亵渎之怒增益
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
            Item.buffType = ModContent.BuffType<ProfanedRage>();   // 附加亵渎之怒增益
            Item.buffTime = 10800;                                  // 增益持续 10800 帧（3 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 合成表：暴怒药水 + 灾厄 UnholyEssence + 银河奇点，或药剂瓶 + 血珠×40 + UnholyEssence；
        /// 现代版用本模组银河奇点，经典版取经典版灾厄的 GalacticaSingularity（均在炼金台）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：暴怒药水 + UnholyEssence + 本模组银河奇点
                if(calamity.TryFind<ModItem>("UnholyEssence", out ModItem unholyEssence1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.RagePotion);
                    recipe.AddIngredient(unholyEssence1.Type);
                    recipe.AddIngredient<GalacticaSingularity>();
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 现代版灾厄：药剂瓶 + 血珠×40 + UnholyEssence
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1) && calamity.TryFind<ModItem>("UnholyEssence", out ModItem unholyEssence2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 40);
                    recipe.AddIngredient(unholyEssence2.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：暴怒药水 + UnholyEssence + 经典版 GalacticaSingularity
                if(calamity1.TryFind<ModItem>("UnholyEssence", out ModItem unholyEssence3) && calamity1.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.RagePotion);
                    recipe1.AddIngredient(unholyEssence3.Type);
                    recipe1.AddIngredient(galacticaSingularity1.Type);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×40 + UnholyEssence
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2) && calamity1.TryFind<ModItem>("UnholyEssence", out ModItem unholyEssence4))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(bloodOrb2.Type, 40);
                    recipe1.AddIngredient(unholyEssence4.Type);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
