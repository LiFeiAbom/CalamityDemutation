using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.NPCs;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 地狱火爆炸 debuff（移植自 CWR 的 HellfireExplosion）：注册为减益，并每帧在宿主实体上喷出火苗粒子。
    /// 玩家侧：置位玩家标记，持续掉血在 CalamityDemutationPlayer.UpdateBadLifeRegen 结算，死因文案在 PreKill 替换；
    /// NPC 侧：置位 GlobalNPC 标记，持续灼烧在 CalamityDemutationGlobalNPC.UpdateLifeRegen 结算。
    /// 该 debuff 由 DragonRageHeld 等弹幕命中时 AddBuff 施加。
    /// </summary>
    internal class HellfireExplosion : ModBuff
    {
        //public override string Texture => "CalamityDemutation/Content/Buffs/HellfireExplosion";
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
        /// 玩家版 Update：每帧在玩家处喷出火苗粒子（SpanFire），并置位玩家标记供掉血与死因结算读取。
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            SpanFire(player);
            player.GetModPlayer<CalamityDemutationPlayer>().hellfireExplosion = true;
        }
        /// <summary>
        /// NPC 版 Update：每帧在 NPC 处喷出火苗粒子（SpanFire），并置位 GlobalNPC 标记供持续灼烧结算读取。
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            SpanFire(npc);
            npc.GetGlobalNPC<CalamityDemutationGlobalNPC>().hellfireExplosion = true;
        }
        /// <summary>
        /// 在实体中心周围随机偏移处喷出一枚 FlameParticle 火苗（写法与 CWR 原版一致）。
        /// 粒子继承实体 80% 的水平速度、初速向上 10，经 ±0.005 弧度随机偏转后，
        /// 按 50% 概率落在低速档（0.4~0.65）或高速档（0.8~1）上，形成随机摇曳的燃烧感。
        /// </summary>
        private static void SpanFire(Entity entity)
        {
            bool lowVel = !Main.rand.NextBool();   // 50% 概率走低速档
            FlameParticle ballFire = new FlameParticle(entity.Center + CDUtil.randVr(entity.width / 2)
                , Main.rand.Next(13, 22), Main.rand.NextFloat(0.1f, 0.22f), Main.rand.NextFloat(0.02f, 0.07f), Color.Gold, Color.DarkRed)
            {
                Velocity = new Vector2(entity.velocity.X * 0.8f, -10).RotatedByRandom(0.005f)
                    * (lowVel ? Main.rand.NextFloat(0.4f, 0.65f) : Main.rand.NextFloat(0.8f, 1f))
            };
            DRKLoader.AddParticle(ballFire);
        }
    }
}
