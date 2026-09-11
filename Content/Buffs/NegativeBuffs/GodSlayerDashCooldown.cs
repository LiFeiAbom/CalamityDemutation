using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 弑神者冲刺冷却（GodSlayerDashCooldown） - 冲刺后的充能冷却标记，45 秒。
    /// 本模组自持的弑神者冲刺不使用灾厄的 cooldown 系统，改由本 buff 承担冷却；
    /// 存在期间 CalamityDemutationPlayer.RequestGodSlayerDash 会拒绝新的冲刺请求。
    /// </summary>
    internal class GodSlayerDashCooldown:ModBuff
    {
        /// <summary>
        /// 注册为减益：可 PvP 传播、不随存档保存，护士无法移除，专家/大师模式下不延长时长
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
