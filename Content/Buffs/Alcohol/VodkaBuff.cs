using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.Alcohol
{
    /// <summary>
    /// 伏特加增益/减益（Vodka）—— 移植自灾厄 2.0.4。
    /// 每帧置位玩家标记并累计 1 级酒醉；具体数值结算在
    /// <see cref="CalamityDemutationPlayer"/> 的 PostUpdateMiscEffects / UpdateLifeRegen。
    /// </summary>
    internal class VodkaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = false;
            Main.persistentBuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.vodka = true;
            modPlayer.alcoholPoisonLevel++;
        }
    }
}
