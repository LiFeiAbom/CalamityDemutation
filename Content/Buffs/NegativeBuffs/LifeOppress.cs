using CalamityDemutation.NPCs;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 生命压制（LifeOppress） - 噬渊鞭挞（Ystralyn）命中敌人时挂上的高额持续伤害减益。
    /// 对敌怪 4501 点/秒（结算在 CalamityDemutationGlobalNPC.UpdateLifeRegen），
    /// 对玩家（PvP）按 CE 原样扣 60 点 lifeRegen（结算在 CalamityDemutationPlayer 的 UpdateBadLifeRegen）。
    /// <para>
    /// 与 CE 原版的差异：CE 的伤害写在两个全局类 EDamageOverTimeNPC / EDamageOverTimePlayer 里，
    /// 本模组按既有口径并进 CalamityDemutationGlobalNPC 的标记位 + 玩家的 partial（数值照抄 CE 的 4501 / 60）。
    /// </para>
    /// </summary>
    internal class LifeOppress:ModBuff
    {
        /// <summary>不随存档保存；登记为减益、允许 PvP 传播</summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
        }
        /// <summary>
        /// 每帧置位 NPC 上的 lifeOppress 标记；扣血在 GlobalNPC.UpdateLifeRegen 中结算
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<CalamityDemutationGlobalNPC>().lifeOppress = true;
        }
    }
}
