using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.SummonBuffs
{
    /// <summary>
    /// 亵渎之魂水晶的鞭痕 debuff（移植自灾厄 2.2.2 的 ProfanedCrystalWhipDebuff）。
    /// 由水晶鞭命中时挂在**敌人**身上，属于"召唤 tag"：装备水晶的玩家让自己的仆从或转化弹幕打这个敌人时，
    /// 额外吃到 20% 的乘算伤害（强化档 40%）——加成在 CalamityDemutationGlobalProjectile.ModifyHitNPC 里结算。
    /// 同时按原版口径清掉敌人身上其它鞭的 tag，并登记为 tag buff（BuffID.Sets.IsATagBuff）。
    /// 贴图取自灾厄的 SentinalLash（原版也是借用这张通用鞭痕图）。
    /// </summary>
    internal class ProfanedCrystalWhipDebuff:ModBuff
    {
        /// <summary>
        /// 需要被本 debuff 清掉的原版鞭痕 debuff（对齐 2.2.2 的同一个名单）：
        /// 皮鞭 / 火鞭 / 骨鞭 / 镰刀鞭 / 冷却鞭 / 狼牙锤鞭 / 彩虹鞭 / 剑鞭 / 荆棘鞭
        /// </summary>
        private static readonly int[] OtherWhipDebuffs = new int[]
        {
            BuffID.BlandWhipEnemyDebuff,
            BuffID.FlameWhipEnemyDebuff,
            BuffID.BoneWhipNPCDebuff,
            BuffID.ScytheWhipEnemyDebuff,
            BuffID.CoolWhipNPCDebuff,
            BuffID.MaceWhipNPCDebuff,
            BuffID.RainbowWhipNPCDebuff,
            BuffID.SwordWhipNPCDebuff,
            BuffID.ThornWhipNPCDebuff
        };
        /// <summary>
        /// 登记为 debuff、不随存档保存，并标记为"鞭的召唤 tag"（BuffID.Sets.IsATagBuff）——
        /// 原版鞭的 tag 结算与本工程自己的 tag 加伤都靠这个标记识别
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.IsATagBuff[Type] = true;
        }
        /// <summary>
        /// 每帧清掉该敌人身上其它鞭的 tag（对齐 2.2.2）：避免"亵渎水晶鞭 + 原版鞭"把两套 tag 叠在一起。
        /// 原版注释也承认这招挡不住其它模组的鞭，本工程同样只处理原版九种
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            for (int i = 0; i < NPC.maxBuffs; i++)
            {
                int buffType = npc.buffType[i];
                if (npc.buffTime[i] > 0 && buffType != Type && OtherWhipDebuffs.Contains(buffType))
                    npc.RequestBuffRemoval(buffType);
            }
        }
    }
}
