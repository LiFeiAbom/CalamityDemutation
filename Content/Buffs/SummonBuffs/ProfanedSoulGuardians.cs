using CalamityDemutation.Content.Projectiles.Summon;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 亵渎之魂守护者（The Profaned Soul）：由亵渎之魂神器（ProfanedSoulArtifact）赋予的召唤标记 buff。
    /// 攻击守护者（MiniGuardianAttack）不在场时自动移除，在场时每帧续期；
    /// 三守护者的实际加成在 CalamityDemutationPlayer 中结算（本类只负责标记与去留）。
    /// </summary>
    internal class ProfanedSoulGuardians:ModBuff
    {
        /// <summary>
        /// 注册为增益：不显示剩余时间、不随存档保存
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;   // 不显示剩余时间
            Main.buffNoSave[Type] = true;          // 不随存档保存
        }
        /// <summary>
        /// 每帧判定：攻击守护者不在场则移除本 buff，否则把剩余时间续到 18000 帧
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianAttack>()] <= 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
