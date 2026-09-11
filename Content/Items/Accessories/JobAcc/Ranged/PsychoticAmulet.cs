using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged
{
    internal class PsychoticAmulet:ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.rare = ItemRarityID.LightPurple;
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.psychoticAmulet = true;
        }
    }
}
