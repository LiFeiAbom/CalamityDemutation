using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.Alcohol
{
    /// <summary>
    /// 莫斯科骡子增益/减益（Moscow Mule）—— 移植自灾厄 2.0.3.9 的
    /// <c>Buffs/Alcohol/MoscowMuleBuff.cs</c>，并采用 2.1 的护士不可移除 / 持久化标记。
    /// <para>
    /// 每帧只置位玩家侧标记并累计酒醉等级；具体数值结算在
    /// <see cref="CalamityDemutationPlayer"/> 的
    /// <c>PostUpdateMiscEffects</c> / <c>ModifyWeaponKnockback</c> / <c>UpdateLifeRegen</c> 中完成。
    /// </para>
    /// </summary>
    internal class MoscowMuleBuff : ModBuff
    {
        /// <summary>debuff、PvP 有效、可存档、护士不能移除（照 2.1）</summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = false;
            Main.persistentBuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        /// <summary>置位玩家标记，并累计 1 级酒醉</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.moscowMule = true;
            modPlayer.alcoholPoisonLevel++;
        }
    }
}
