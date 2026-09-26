using CalamityDemutation.NPCs;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 狂暴（Enraged） - 同时作用于玩家与敌怪的双向标记减益
    /// 由特定装备/套装触发，玩家侧提升伤害倍率，敌怪侧在 GlobalNPC 中染红并使其造成的伤害 +25%。
    /// </summary>
    internal class Enraged:ModBuff
    {
        /// <summary>
        /// 注册为减益：可 PvP 传播、随存档保存，且护士无法移除
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 标记为减益：图标按 debuff 显示
            Main.debuff[Type] = true;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 随存档保存：显式设为 false，表示该状态需要跨存档保留
            Main.buffNoSave[Type] = false;
            // 专家/大师模式下不延长时长
            BuffID.Sets.LongerExpertDebuff[Type] = false;
            // 护士无法通过治疗移除该减益
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        /// <summary>
        /// 玩家侧：置位 ModPlayer.enraged，实际伤害加成在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 置位玩家标记：ModPlayer 在 ModifyHitNPCWithItem/ModifyHitNPCWithProj 中令 damageMult += 1.25
            player.GetModPlayer<CalamityDemutationPlayer>().enraged = true;
        }
        /// <summary>
        /// 敌怪侧：置位 GlobalNPC.enraged——GlobalNPC.GetAlpha 据此染红，ModifyHitPlayer 据此令其造成伤害 +25%
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            // 置位 NPC 标记：染色与 +25% 增伤结算都在 GlobalNPC 里读它
            npc.GetGlobalNPC<CalamityDemutationGlobalNPC>().enraged = true;
        }
    }
}
