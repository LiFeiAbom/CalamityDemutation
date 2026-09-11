using CalamityDemutation.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 恶魔烈焰（DemonFlames） - 附着在 NPC 上的高额持续灼烧减益
    /// 由恶魔残影套装（demonshadeSetBonus）命中时按随机档位赋予（360/240/120 tick），
    /// 真正的伤害在 CalamityDemutationGlobalNPC.UpdateLifeRegen 中结算。
    /// </summary>
    internal class DemonFlames:ModBuff
    {
        /// <summary>
        /// 注册为减益：允许 PvP 传播、不随存档保存，并在专家/大师模式下延长持续时长
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 标记为减益：图标按 debuff 显示，对敌怪为负面状态
            Main.debuff[Type] = true;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 不随存档保存：战斗中临时施加的状态
            Main.buffNoSave[Type] = true;
            // 专家/大师模式下自动延长该减益的持续时间
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        /// <summary>
        /// 每帧置位 NPC 上的 demonFlames 标记；灼烧伤害在 GlobalNPC.UpdateLifeRegen 中结算
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            // 置位标记：GlobalNPC 据此把 lifeRegen 减至 -5000，并把伤害下限抬到 2000
            npc.GetGlobalNPC<CalamityDemutationGlobalNPC>().demonFlames = true;
        }
    }
}
