using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.Alcohol
{
    /// <summary>
    /// 龙舌兰日出增益/减益（Tequila Sunrise）—— 移植自经典 1.4.2，
    /// 采用用户 2026-10-19 修正后的通用 / 白天双层加成。
    /// <para>
    /// 每帧只置位玩家侧标记并累计酒醉等级；具体数值结算在
    /// <see cref="CalamityDemutationPlayer"/> 的
    /// <c>PostUpdateMiscEffects</c> / <c>ModifyWeaponKnockback</c> / <c>UpdateLifeRegen</c> 中完成。
    /// </para>
    /// </summary>
    internal class TequilaSunriseBuff : ModBuff
    {
        /// <summary>debuff、PvP 有效、可存档、护士不能移除、持久化</summary>
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
            modPlayer.tequilaSunrise = true;
            modPlayer.alcoholPoisonLevel++;
        }
    }
}
