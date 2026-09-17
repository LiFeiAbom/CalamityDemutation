using CalamityDemutation.Enums;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    internal class OrnateShield:ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 32;
            Item.value = Item.buyPrice(0, 12, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.defense = 8;
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().ornateShield = true;
            player.GetModPlayer<CalamityDemutationPlayer>().shieldSlamDash = ShieldSlamDash.OrnateShield;
        }
        public override void AddRecipes()
        {
            // 现代版灾厄：寒元锭（CryonicBar）×5 + 水晶碎片×10，秘银砧
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("CryonicBar", out ModItem cryonicBar))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(cryonicBar.Type, 5);
                    recipe.AddIngredient(ItemID.CrystalShard, 10);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            // 经典版灾厄：同名材料两版叫法不同，这里是 VerstaltiteBar ×5 + 水晶碎片×10，秘银砧
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("VerstaltiteBar", out ModItem verstaltiteBar))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(verstaltiteBar.Type, 5);
                    recipe1.AddIngredient(ItemID.CrystalShard, 10);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
