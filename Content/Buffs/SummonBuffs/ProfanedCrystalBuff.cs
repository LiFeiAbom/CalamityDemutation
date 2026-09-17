using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 亵渎之魂水晶的"Devotion"标记 buff（移植自灾厄 2.2.2 的 ProfanedCrystalBuff）。
    /// 装备水晶期间常驻显示、卸下即移除；本身不提供任何数值（四态加成在 CalamityDemutationPlayer 中结算）。
    /// 灾厄把它登记为 debuff 且护士无法移除，本工程照抄该语义。
    /// </summary>
    internal class ProfanedCrystalBuff:ModBuff
    {
        /// <summary>
        /// 注册为 debuff（"Devotion" 诅咒）：不显示剩余时间、不随存档保存、护士无法移除
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;                            // 灾厄原样登记为 debuff
            Main.buffNoSave[Type] = true;                        // 不随存档保存
            Main.buffNoTimeDisplay[Type] = true;                 // 不显示剩余时间
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;    // 护士不能移除
        }
        /// <summary>
        /// 每帧判定：仍戴着水晶就把剩余时间续到 18000 帧，否则移除本 buff。
        /// 用实际饰品槽判定（而非每帧重置的 profanedCrystal 标记），避免在 UpdateBuffs 阶段读到旧值
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            if (CalamityDemutationPlayer.IsAccessoryEquipped(player, ModContent.ItemType<ProfanedSoulCrystal>()))
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
