using CalamityDemutation.Graphics.Metaballs;
using CalamityDemutation.Particles;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 龙怒火球弹幕（移植自 CWR 的 DragonRageFireOrb）：追踪敌人的火球。
    /// 从简裁剪：metaball 拖尾、SmallSmoke/Spark 粒子全部换成原版 Dust，Dragonfire 换成原版着火 debuff。
    /// </summary>
    internal class DragonRageFireOrb : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        private ref float Time => ref Projectile.ai[0];
        /// <summary>
        /// 基础属性：32×32、近战无攻速伤害类型、穿透 2、extraUpdates 6（高频多段更新）、本地无敌帧 20 帧；
        /// timeLeft 设为 1220 × extraUpdates，使其在没有命中时也能存活很久。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.extraUpdates = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = 1220 * Projectile.extraUpdates;
        }
        /// <summary>
        /// 命中判定：出生后的前 15 × extraUpdates 个更新内不可命中，避免刚生成就贴脸命中（此时它还在从刀刃上飞离）。
        /// </summary>
        public override bool? CanHitNPC(NPC target)
        {
            return Time < 15 * Projectile.extraUpdates ? false : base.CanHitNPC(target);
        }
        /// <summary>
        /// 更新逻辑：出生瞬间喷出一圈火星尘；此后每 5 帧在玩家 1400 像素范围内抛出一颗 Torch 火花；
        /// 同范围内持续叠加两层 DragonsBreathFlameMetaball 拖尾。
        /// 存活超过 160 × extraUpdates 帧后，改为用 ChasingBehavior2 追踪最近的敌人（搜寻半径 1600）；
        /// 否则仅按 ai[1] 每帧旋转速度方向。返回 false 完全接管 AI。
        /// </summary>
        public override bool PreAI()
        {
            Player owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);
            if (Time == 0)
            {
                for (int i = 0; i <= 7; i++)
                {
                    int dustType1 = 259;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? dustType1 : 174);
                    dust.scale = dust.type == dustType1 ? Main.rand.NextFloat(0.9f, 1.9f) : Main.rand.NextFloat(0.8f, 1.7f);
                    dust.velocity = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.3f, 0.8f);
                    dust.noGravity = true;
                }
            }
            if (Time % 5 == 0 && Time > 35f && targetDist < 1400f)
            {
                Dust spark = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(1 + Time * 0.1f, 1 + Time * 0.1f), 0, 0, DustID.Torch, 0f, 0f, 100, Main.rand.NextBool() ? Color.DarkOrange : Color.OrangeRed, Main.rand.NextFloat(0.4f, 0.7f));
                spark.velocity = -Projectile.velocity * 0.5f;
                spark.noGravity = true;
            }
            if (targetDist < 1400f)
            {
                ModContent.GetInstance<DragonsBreathFlameMetaball2>().SpawnParticle(Projectile.Center, Time * 0.1f + 0.2f);
                ModContent.GetInstance<DragonsBreathFlameMetaball>().SpawnParticle(Projectile.Center + Projectile.velocity, Time * 0.09f + 0.15f);
            }
            if (Time > 160 * Projectile.extraUpdates)
            {
                NPC target = Projectile.Center.FindClosestNPC(1600);
                if (target != null)
                {
                    Projectile.ChasingBehavior2(target.Center, 1, 0.08f);
                }
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1]);
            }
            Time++;
            return false;
        }
        /// <summary>
        /// 命中敌怪：施加 420 帧的 OnFire3（着火了！3）debuff。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 420);
        }
        /// <summary>
        /// 消失时：在弹幕中心叠加橙红／白两层 bloom 粒子，并调用 Projectile.Explode() 产生原版爆炸。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            float orbSize = Main.rand.NextFloat(0.5f, 0.8f);
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, Color.OrangeRed, orbSize + 0.6f, 8, true));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, Color.White, orbSize + 0.2f, 8, true));
            Projectile.Explode();
        }
    }
}
