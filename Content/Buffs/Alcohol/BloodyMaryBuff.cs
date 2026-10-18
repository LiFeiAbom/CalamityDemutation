using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.Alcohol
{
    /// <summary>
    /// 血腥玛丽增益/减益（Bloody Mary）—— 移植自灾厄 2.1 的
    /// <c>Buffs/Alcohol/BloodyMaryBuff.cs</c>。
    /// <para>
    /// 与灾厄一致：属于 debuff、可存档、护士不能移除；每帧只置位玩家侧标记，
    /// 具体数值结算在 <see cref="CalamityDemutationPlayer"/> 的
    /// <c>PostUpdateMiscEffects</c> / <c>UpdateLifeRegen</c> 与全局 NPC 的
    /// <c>EditSpawnRate</c> 中完成。
    /// </para>
    /// </summary>
    internal class BloodyMaryBuff : ModBuff
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
            modPlayer.bloodyMary = true;
            modPlayer.alcoholPoisonLevel++;
        }
    }
}
