using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 地狱火爆炸 debuff（移植自 CWR 的 HellfireExplosion）：被标记为 debuff 并持续喷火粒子，
    /// 原版的 FlameParticle 已用原版 Dust 替代（从简）。
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
        /// 玩家版 Update：每帧在玩家处喷出火焰粒子（SpanFire），只做视觉表现。
        /// 本文件不置位任何玩家标记、也不在此结算伤害；该 debuff 由 DragonRageHeld 等弹幕命中时 AddBuff 施加。
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            SpanFire(player);
        }
        /// <summary>
        /// NPC 版 Update：每帧在 NPC 处喷出火焰粒子（SpanFire），只做视觉表现。
        /// 同样未置位 GlobalNPC 标记、未见持续伤害；实际伤害来自施加该 debuff 的弹幕命中结算（如 DragonRageHeld）。
        /// </summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            SpanFire(npc);
        }
        /// <summary>
        /// 在实体中心附近随机生成向上飘散的火焰尘埃（原版 FlameParticle 的简化替代）。
        /// 一半概率生成低速小火苗，一半概率生成高速大火苗，形成随机摇曳的燃烧感。
        /// </summary>
        private static void SpanFire(Entity entity)
        {
            bool lowVel = Main.rand.NextBool();   // 50% 概率走低速分支
            Vector2 fireVel = new Vector2(entity.velocity.X * 0.8f, -10).RotatedByRandom(0.005f) * (lowVel ? Main.rand.NextFloat(0.4f, 0.65f) : Main.rand.NextFloat(0.8f, 1f));   // 继承实体水平速度并向上飘，随机微幅偏转后按速度档缩放
            Dust fire = Dust.NewDustDirect(entity.Center + new Vector2(Main.rand.Next(-entity.width / 2, entity.width / 2), 0), 0, 0, DustID.Torch, 0f, 0f, 100, Color.White, Main.rand.NextFloat(0.1f, 0.22f));   // 在实体顶部宽度范围内生成火炬尘埃
            fire.velocity = fireVel;   // 覆盖尘埃初速度
            fire.noGravity = true;     // 关闭重力，让其自行飘散
        }
    }
}
