using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 灵魂紊乱（SoulDisorder，移植自 CalamityEntropy）：游魂系武器（聚魂分形、符文之歌）命中后挂在敌怪身上的减益。
    /// <para>
    /// 效果分两处：① 实际数值（受击 **+15 护甲穿透、最终伤害 ×1.05**）写在
    /// <see cref="CalamityDemutationGlobalNPC.ModifyIncomingHit"/> 里（CE 是单开一个
    /// <c>SoulDisorderDebuffNPC : GlobalNPC</c> 写同一件事）；② 带此减益的敌怪会被套一层灵魂色染色，
    /// 见 <see cref="CalamityDemutationGlobalNPC.PreDraw"/> 与着色器 <c>SoulDiscorder</c>。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① CE 的玩家侧 <c>Update</c> 只是把 <c>EModPlayer.soulDisorder</c> 置真，
    /// 而那个标记在 CE 全树**从未被读取**（只有声明、每帧复位与这里置真三处），属不产生效果的死字段
    /// → 本模组不移植它，故本类没有 <c>Update(Player)</c>；② CE 的 <c>EGlobalNPC.DamageReduceMult</c> 里
    /// 灵魂紊乱还占 <c>-0.12</c> 一档，那属于 CE 自研的 NPC 减伤体系（本模组未移植该体系）→ 一并省掉。
    /// </para>
    /// </summary>
    internal class SoulDisorder:ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;                        // 不随存档保存
            Main.debuff[Type] = true;                            // 注册为减益
            Main.pvpBuff[Type] = true;                           // PvP 下对玩家生效
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;    // 护士去不掉
            BuffID.Sets.LongerExpertDebuff[Type] = true;         // 专家/大师下时长延长
        }
    }
}
