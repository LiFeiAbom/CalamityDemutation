using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    [AutoloadEquip(EquipType.Body)]
    internal class AuricTeslaBodyArmor:ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(1, 44, 0, 0);
            Item.defense = 48;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
                EquipLoader.AddEquipTexture(Mod, Texture + "_Back", EquipType.Back, this);
        }
        public override void EquipFrameEffects(Player player, EquipType type)
        {
            if (player.body == Item.bodySlot)
                player.back = (sbyte)EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);
        }
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.frostBarrier = true;
            modPlayer.godSlayerReflect = true;
            player.statLifeMax2 += 400;
            player.statManaMax2 += 400;
            player.moveSpeed += 0.25f;
            player.GetCritChance<GenericDamageClass>() += 30;
            player.GetDamage<GenericDamageClass>() += 0.3f;
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<TarragonBreastplate>();
                recipe.AddIngredient<BloodflareBodyArmor>();
                recipe.AddIngredient<SilvaArmor>();
                recipe.AddIngredient<GodSlayerChestplate>();
                recipe.AddIngredient(calamity.Find<ModItem>("AuricBar").Type, 20);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<TarragonBreastplate>();
                recipe1.AddIngredient<BloodflareBodyArmor>();
                recipe1.AddIngredient<SilvaArmor>();
                recipe1.AddIngredient<GodSlayerChestplate>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("AuricOre").Type, 100);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 30);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 30);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DarksunFragment").Type, 15);
                recipe1.AddIngredient(calamity1.Find<ModItem>("BarofLife").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("HellcasterFragment").Type, 7);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofCalamity").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("GalacticaSingularity").Type, 3);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
