using CalamityDemutation.Content.Items.Weapons.Rogue;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    internal partial class CalamityDemutationPlayer : ModPlayer
    {
        /// <summary>两次连续命中之间允许的最大帧数，超过后金锅连段清空。</summary>
        public const int SearedPanConsecutiveHitOpening = 40;
        public int searedPanCounter;
        public int searedPanTimer;
        public void RegisterSearedPanHit()
        {
            searedPanCounter++;
            searedPanTimer = 0;
        }
        public void ResetSearedPanCounter()
        {
            searedPanCounter = 0;
            searedPanTimer = 0;
        }
        public void UpdateSearedPanCounter()
        {
            if (searedPanCounter <= 0)
                return;
            if (Player.HeldItem.type != ModContent.ItemType<SearedPan>())
            {
                ResetSearedPanCounter();
                return;
            }
            if (searedPanTimer < SearedPanConsecutiveHitOpening)
                searedPanTimer++;
            else
                ResetSearedPanCounter();
        }
    }
}
