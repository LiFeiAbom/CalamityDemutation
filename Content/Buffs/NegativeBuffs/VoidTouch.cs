using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 虚空之触（VoidTouch，移植自 CalamityEntropy）：虚空系攻击（虚空分形 / 最终分形 / 虚影光束）命中的标记。
    /// <para>
    /// <b>敌怪侧</b>：这件 buff 只负责在敌怪身上显示减益图标 —— 真正的持续伤害、减速与受击增伤都在
    /// <see cref="CalamityDemutationGlobalNPC.PreAI"/> 里按 <c>VoidTouchTime/Level</c> 结算
    /// （CE 同样是 buff 只挂图标、效果在 EGlobalNPC 里），所以本类没有 <c>Update(NPC)</c>。
    /// <b>玩家侧</b>（只有 PvP 命中玩家才会走到）：执行 CE 的持续伤害 —— 每 4 帧扣 3 血、速度 ×0.99、
    /// 撒腐蚀喷尘，掉血致死时用专属死亡文本。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异（都是本模组缺对应设施，故按最接近的语义落地）：① CE 的扣血间隔是
    /// <c>4 / (1 - 虚空抗性)</c> 帧，抗性来自它的 <c>EModPlayer.voidResistance</c>，本模组暂无任何内容设置该抗性
    /// → 直接按抗性 0 处理，即恒定每 4 帧（等于 CE 的默认情形）；
    /// ② CE 在扣血前会检查 <c>EPlayerDash().velt</c>（冲刺帧不给玩家减速），本模组的三套冲刺都没有该标记，
    /// 补它要改动已在跑的冲刺代码 → 省掉这个检查（代价：极端情况下冲刺中的玩家会被减速 1%）；
    /// ③ CE 每帧还会 spawn 一颗 <c>PRT_Void</c>（走它 EffectLoader 的虚空 shader 管线，本模组没有该管线）
    /// → 省掉，腐蚀喷尘（原版 <see cref="DustID.CorruptSpray"/>）保留。
    /// </para>
    /// </summary>
    internal class VoidTouch:ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;                        // 不随存档保存
            Main.debuff[Type] = true;                            // 注册为减益
            Main.pvpBuff[Type] = true;                           // PvP 下对玩家生效
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;    // 护士去不掉
            BuffID.Sets.LongerExpertDebuff[Type] = true;         // 专家/大师下时长延长
        }
        /// <summary>
        /// 玩家侧持续伤害（CE 原样）：每 4 帧扣 3 血，血量见底则解除无敌并直接判定死亡、
        /// 死亡文本取本模组的 <c>KilledByVoidTouch</c>；每帧撒腐蚀喷尘并把速度压到 99%。
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            if (Main.GameUpdateCount % 4 == 0)
            {
                int dmg = 3;
                player.statLife -= dmg;
                if (player.statLife <= dmg)
                {
                    player.SetImmuneTimeForAllTypes(0);
                    player.immune = false;
                    player.DelBuff(buffIndex--);
                    player.Hurt(PlayerDeathReason.ByCustomReason(Language.GetText("Mods.CalamityDemutation.KilledByVoidTouch").ToNetworkText(player.name)), dmg, 0);
                }
            }
            Dust.NewDust(player.Center, player.width, player.height, DustID.CorruptSpray, Main.rand.NextFloat() * 6f - 3f, Main.rand.NextFloat() * 6f - 3f);
            player.velocity *= 0.99f;
        }
    }
}
