using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    [AutoloadEquip(EquipType.Legs)]
    internal class AuricTeslaCuisses:ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(1, 8, 0, 0);
            Item.defense = 44;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.5f;
            player.carpet = true;
            player.GetDamage<GenericDamageClass>() += 0.14f;
            player.GetCritChance<GenericDamageClass>() += 14;
        }
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<TarragonHelm>();
                recipe.AddIngredient<BloodflareMask>();
                recipe.AddIngredient<SilvaHelm>();
                recipe.AddIngredient<GodSlayerHelm>();
                recipe.AddIngredient(ItemID.FlyingCarpet);
                recipe.AddIngredient(calamity.Find<ModItem>("AuricBar").Type, 15);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<TarragonHelm>();
                recipe1.AddIngredient<BloodflareMask>();
                recipe1.AddIngredient<SilvaHelm>();
                recipe1.AddIngredient<GodSlayerHelm>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("AuricOre").Type, 80);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 15);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DarksunFragment").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("BarofLife").Type, 8);
                recipe1.AddIngredient(calamity1.Find<ModItem>("HellcasterFragment").Type, 6);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofCalamity").Type, 3);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DivineGeode").Type, 18);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofCalamity").Type, 18);
                recipe1.AddIngredient(calamity1.Find<ModItem>("GalacticaSingularity").Type, 2);
                recipe1.AddIngredient(ItemID.FlyingCarpet);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
