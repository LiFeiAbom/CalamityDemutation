using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    internal class ShieldoftheOcean:ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.value = Item.buyPrice(0, 3, 0, 0);
            Item.rare = ItemRarityID.Green;
            Item.defense = 2;
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().shieldoftheOcean = true;
        }
        public override void AddRecipes()
        {
            // 现代版灾厄：海之遗骸（SeaRemains）×5 + 珊瑚×5，铁砧
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("SeaRemains", out ModItem seaRemains))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(seaRemains.Type, 5);
                    recipe.AddIngredient(ItemID.Coral, 5);
                    recipe.AddTile(TileID.Anvils);
                    recipe.Register();
                }
            }
            // 经典版灾厄：同名材料两版叫法不同，这里是维斯蒂德锭（VictideBar）×5 + 珊瑚×5，铁砧
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("VictideBar", out ModItem victideBar))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(victideBar.Type, 5);
                    recipe1.AddIngredient(ItemID.Coral, 5);
                    recipe1.AddTile(TileID.Anvils);
                    recipe1.Register();
                }
            }
        }
    }
}
