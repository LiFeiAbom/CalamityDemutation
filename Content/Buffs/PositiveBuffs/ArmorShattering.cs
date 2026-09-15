using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 碎甲增益（Armor Shattering）：破甲药水的强化版，由碎甲药水（ShatteringPotion）赋予。
    /// 每帧仅置位 armorShattering 标记，数值与附加效果在两处结算：
    /// CalamityDemutationPlayer.PostUpdateMiscEffects（通用暴击率 +8%、通用伤害 +8%）、
    /// CalamityDemutationGlobalItem / CalamityDemutationGlobalProjectile 的命中回调（触发时追加灾厄破甲减益）。
    /// </summary>
    internal class ArmorShattering:ModBuff
    {
        /// <summary>
        /// 注册为正面增益：允许 PvP 传播且随存档保存，专家/大师模式不延长时长
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;                      // 非减益：按增益图标显示
            Main.pvpBuff[Type] = true;                      // 允许在 PvP 中传播
            Main.buffNoSave[Type] = false;                  // 随存档保存
            BuffID.Sets.LongerExpertDebuff[Type] = false;   // 专家/大师模式不延长时长
        }
        /// <summary>
        /// 每帧置位 armorShattering 标记，供暴击/伤害结算与命中附加减益读取
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().armorShattering = true;   // 置位标记，供 PostUpdateMiscEffects / 命中回调读取
        }
    }
}
