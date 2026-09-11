using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Accessories.Function
{
    internal class FrostBarrier:ModItem
    {
        public override void SetDefaults()
        {
            Item.defense = 4;
            Item.width = 20;
            Item.height = 24;
            Item.value = Item.buyPrice(0, 9, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.frostBarrier= true;
        }
    }
}
