using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 亵渎之魂水晶的鞭刃增益（移植自灾厄 2.2.2 的 ProfanedCrystalWhipBuff）。
    /// 由水晶鞭命中时给自己挂上，持续期间把三只守护者的出手节奏切到强化档（见各守护者的 WhipBuffed），
    /// 并清掉原版四系鞭增益（冷却鞭/镰刀鞭/剑鞭/荆棘鞭）。
    /// 不是 debuff、不随存档保存；离开水晶态（上一帧四态低于 Buffs）时自清。
    /// 由水晶鞭（ProfanedCrystalWhip）命中敌人时挂上，持续 30 秒。
    /// </summary>
    internal class ProfanedCrystalWhipBuff:ModBuff
    {
        /// <summary>
        /// 贴图路径：Content/Buffs/SummonBuffs/ProfanedCrystalWhipBuff（源自灾厄的 SentinalLash）
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Buffs/SummonBuffs/ProfanedCrystalWhipBuff";
        /// <summary>
        /// 登记为增益（非 debuff）且不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;      // 增益而非 debuff
            Main.buffNoSave[Type] = true;   // 不随存档保存
        }
        /// <summary>
        /// 每帧判定：上一帧的四态低于 Buffs（未处于水晶态）时自清本增益；
        /// 否则清除原版四系鞭增益，避免与水晶体系的鞭增益并存
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 对齐 2.2.2：profanedCrystalStatePrevious < Buffs 即自清（原版用 ClearBuff(Type)，
            // 本工程统一用 DelBuff + buffIndex-- 的写法，避免在遍历增益表时索引错位）
            if (player.GetModPlayer<CalamityDemutationPlayer>().profanedCrystalStatePrevious < (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Buffs)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }
            player.ClearBuff(BuffID.CoolWhipPlayerBuff);    // 冷却鞭增益
            player.ClearBuff(BuffID.ScytheWhipPlayerBuff);  // 镰刀鞭增益
            player.ClearBuff(BuffID.SwordWhipPlayerBuff);   // 剑鞭增益
            player.ClearBuff(BuffID.ThornWhipPlayerBuff);   // 荆棘鞭增益
        }
    }
}
