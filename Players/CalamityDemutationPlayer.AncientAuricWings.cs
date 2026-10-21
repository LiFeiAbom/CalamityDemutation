using CalamityDemutation.Content.Items.Accessories.Wings;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    internal partial class CalamityDemutationPlayer : ModPlayer
    {
        /// <summary>已装备古圣金源之翼；由饰品每帧置位，ResetEffects 清零。</summary>
        public bool ancientAuricWings = false;
        /// <summary>古圣金源之翼的 +2 MP/s 计时器，每 60 帧结算一次。</summary>
        public int passiveManaRegenTimer = 0;
        public void UpdateAncientAuricWingsManaRegen()
        {
            if (!ancientAuricWings || Player.whoAmI != Main.myPlayer)
            {
                passiveManaRegenTimer = 0;
                return;
            }
            passiveManaRegenTimer++;
            if (passiveManaRegenTimer < 60)
                return;
            passiveManaRegenTimer = 0;
            Player.statMana += AncientAuricWings.ManaPerSecond;
            if (Player.statMana > Player.statManaMax2)
                Player.statMana = Player.statManaMax2;
        }
    }
}
