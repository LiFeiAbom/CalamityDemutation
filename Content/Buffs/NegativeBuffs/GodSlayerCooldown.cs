using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 弑神冷却（GodSlayerCooldown） - 玩家触发弑神保命效果后的充能冷却减益
    /// 触发时赋予 2700 tick；期间玩家获得 +10% 通用伤害，但无法再次触发弑神保命。
    /// </summary>
    internal class GodSlayerCooldown:ModBuff
    {
        /// <summary>
        /// 注册为减益：可 PvP 传播、不随存档保存，护士无法移除
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 标记为减益：图标按 debuff 显示
            Main.debuff[Type] = true;
            // 允许在 PvP 中传播
            Main.pvpBuff[Type] = true;
            // 不随存档保存：冷却状态无需跨存档保留
            Main.buffNoSave[Type] = true;
            // 专家/大师模式下不延长时长
            BuffID.Sets.LongerExpertDebuff[Type] = false;
            // 护士无法移除该减益，防止绕过冷却
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        /// <summary>
        /// 每帧置位 ModPlayer.godSlayerCooldown；该标记既提供 +10% 伤害，也用于封锁弑神保命触发
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            // 置位冷却标记：godSlayerCooldown 为 true 时 +10% 通用伤害，并阻止 godSlayer 保命效果再次触发
            player.GetModPlayer<CalamityDemutationPlayer>().godSlayerCooldown = true;
        }
    }
}
