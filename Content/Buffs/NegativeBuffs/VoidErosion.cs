using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.NPCs;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 虚空侵蚀（VoidErosion） debuff（移植自 CWR 的 VoidErosion）：NPC 专属减益。
    /// 持续伤害在 CalamityDemutationGlobalNPC.UpdateLifeRegen 中结算（lifeRegen -= 10000），
    /// 染色与四周喷射星屑在 CalamityDemutationGlobalNPC.DrawEffects 中表现。
    /// CWR 原版无玩家侧效果，故本类不实现 Update(Player)。
    /// </summary>
    internal class VoidErosion:ModBuff
    {
        /// <summary>
        /// 注册为减益：参与 PvP、不随存档保存，且专家/大师模式下延长持续时间
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        /// <summary>
        /// NPC 版 Update：每帧置位 GlobalNPC 标记，供掉血与染色/星屑结算读取
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex) => npc.GetGlobalNPC<CalamityDemutationGlobalNPC>().voidErosion = true;
        /// <summary>
        /// 在 NPC 中心加上 offset 处喷射星屑：绕中心取 4 个正交方向（0/90/180/270 度），
        /// 每个方向再按 0.1/0.2/0.3 三档速度铺开，共 12 枚。
        /// </summary>
        public static void SpanStar(NPC npc, Vector2 offset)
        {
            for (int i = 0; i < 4; i++)
            {
                float rot1 = MathHelper.PiOver2 * i;
                Vector2 vr = rot1.ToRotationVector2();
                for (int j = 0; j < 3; j++)
                {
                    BaseParticle spark = new DRK_HeavenfallStar(npc.Center + offset
                        , vr * (0.1f + j * 0.1f), false, 3, 0.8f, Color.CadetBlue);
                    DRKLoader.AddParticle(spark);
                }
            }
        }
    }
}
