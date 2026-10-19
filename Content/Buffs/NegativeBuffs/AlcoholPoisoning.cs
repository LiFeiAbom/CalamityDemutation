using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 2.0 版酒精中毒（独立实现，不依赖灾厄同名字段/buff）。
    /// 阈值与数值在 <see cref="CalamityDemutationPlayer.UpdateLifeRegen"/>：
    /// 本地等级 &gt;3 时挂 2 帧本 buff，清零正再生并追加 3 × 等级的负再生。
    /// buff 本身只置位 <see cref="CalamityDemutationPlayer.alcoholPoisoning"/>，供死亡文案判定。
    /// </summary>
    internal class AlcoholPoisoning : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().alcoholPoisoning = true;
        }
    }
}
