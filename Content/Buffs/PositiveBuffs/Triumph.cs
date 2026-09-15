using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 凯旋增益（Triumph）：由凯旋药水（TriumphPotion）赋予的减伤增益。
    /// 每帧仅置位 triumph 标记，效果在 CalamityDemutationPlayer.ModifyHitByNPC 中结算：
    /// 按被接触对象的剩余生命比例减伤——目标血线越低减伤越高，满血时不减伤，最高 25%。
    /// </summary>
    internal class Triumph:ModBuff
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
        /// 每帧置位 triumph 标记，供 ModifyHitByNPC 结算接触减伤
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().triumph = true;   // 置位标记，供 ModifyHitByNPC 读取
        }
    }
}
