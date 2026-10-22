using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 燃烧III（BurningIII）：由战士徽章EX的近战命中附加。
    /// 每秒 100 生命、防御 -5、受到伤害 +5%；粒子复刻原版 Hellfire（BuffID.OnFire3）。
    /// </summary>
    internal class BurningIII : ModBuff
    {
        public const int BurnLifeRegen = 200;
        public const float DefenseLoss = 5f;
        public const float DamageTakenMultiplier = 1.05f;
        public const int Duration = 300;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            SpawnHellfireDust(npc);
            AddHellfireLight(npc);
        }
        public override void Update(Player player, ref int buffIndex)
        {
            SpawnHellfireDust(player);
            AddHellfireLight(player);
        }
        /// <summary>
        /// 原版 Hellfire（BuffID.OnFire3）的粒子表现：3/4 概率生成 Torch 尘，
        /// 速度 ×1.8 后上抬，另有 1/4 概率落回重力并缩小。
        /// </summary>
        private static void SpawnHellfireDust(Entity entity)
        {
            if (Main.rand.Next(4) >= 3)
                return;
            Dust dust = Dust.NewDustDirect(new Vector2(entity.position.X - 2f, entity.position.Y - 2f)
                , entity.width + 4, entity.height + 4, DustID.Torch, entity.velocity.X * 0.4f
                , entity.velocity.Y * 0.4f, 100, default, 3.5f);
            dust.noGravity = true;
            dust.velocity *= 1.8f;
            dust.velocity.Y -= 0.5f;
            if (Main.rand.Next(4) == 0)
            {
                dust.noGravity = false;
                dust.scale *= 0.5f;
            }
            dust.customData = 0;
        }
        private static void AddHellfireLight(Entity entity) =>
            Lighting.AddLight((int)(entity.position.X / 16f), (int)(entity.position.Y / 16f + 1f), 1f, 0.3f, 0.1f);
    }
}
