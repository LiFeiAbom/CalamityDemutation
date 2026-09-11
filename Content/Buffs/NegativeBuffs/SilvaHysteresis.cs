using CalamityDemutation.Content.Items;
using CalamityDemutation.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 森林迟滞（SilvaHysteresis） - 附着在 NPC 上的持续伤害减益
    /// 由 Silva 近战效果命中时以 4% 概率赋予 20 tick，
    /// 真正的伤害在 CalamityDemutationGlobalNPC.UpdateLifeRegen 中结算。
    /// </summary>
    internal class SilvaHysteresis:ModBuff
    {
        /// <summary>
        /// 注册为减益：允许 PvP 传播、不随存档保存，专家/大师模式延长时长
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 标记为减益：图标按 debuff 显示
            Main.debuff[Type] = true;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 不随存档保存：战斗中临时施加的状态
            Main.buffNoSave[Type] = true;
            // 专家/大师模式下自动延长该减益的持续时间
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        /// <summary>
        /// 每帧置位 NPC 上的 silvaHysteresis 标记；伤害在 GlobalNPC.UpdateLifeRegen 中结算
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            // 置位标记：GlobalNPC 据此把 lifeRegen 压低 900，并把伤害下限抬到 400
            npc.GetGlobalNPC<CalamityDemutationGlobalNPC>().silvaHysteresis = true;
        }
    }
}
