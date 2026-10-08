using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 天狼星光束（SiriusBeam，移植自灾厄 2.0 的 Projectiles/Summon/SiriusBeam）—— 天狼星（SiriusMinion）的射击弹幕。
    /// 本体不绘制（复用工程共用的隐形占位贴图）、直线飞行、沿途喷白蓝光点，命中敌人时施加**夜凋**
    /// （灾厄减益 `Nightwither`，两版灾厄都有；都没有时退回原版暗影炎），并在命中点炸出一颗星
    /// （<see cref="SiriusExplosion"/>）。
    /// </summary>
    internal class SiriusBeam:ModProjectile
    {
        /// <summary>本体不绘制，复用工程里的共用隐形占位贴图（Content/Projectiles/InvisibleProj.png）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>标记为召唤物射击弹幕</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性（照源 2.0）：4×4 碰撞箱、穿透数由天狼星按消耗的召唤栏数**逐发改写**（源不在 SetDefaults 里写）、
        /// 220 倍额外更新（极高弹速）、1000 帧寿命、逐敌 110 帧独立命中冷却、不撞地形、伤害类型召唤。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.extraUpdates = 220;
            Projectile.timeLeft = 1000;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 110;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>飞行 3 帧后开始拖尾：每帧沿弹道回退补 4 颗白蓝光点（源写裸值 20 = DustID.PurificationPowder，照源同一编号）</summary>
        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 3f)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 dustPos = Projectile.position;
                    dustPos -= Projectile.velocity * ((float)i * 0.25f);
                    Projectile.alpha = 255;   // 隐形弹幕本体全透明（源写法）
                    int d = Dust.NewDust(dustPos, 1, 1, DustID.PurificationPowder, 0f, 0f, 0, default, 1f);
                    Main.dust[d].position = dustPos;
                    Main.dust[d].scale = (float)Main.rand.Next(70, 110) * 0.013f;
                    Main.dust[d].velocity *= 0.2f;
                    Main.dust[d].noGravity = true;
                }
            }
        }
        /// <summary>
        /// 命中敌人：施加夜凋 180 帧（灾厄两版都没有该 buff 时退回原版暗影炎），
        /// 并在命中点生成一枚星（<see cref="SiriusExplosion"/>），把色相（ai0）与"来自哪发弹幕"（ai1）带过去。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "Nightwither", 180, BuffID.ShadowFlame);
            float hue = Main.rgbToHsl(new Color(103, 203, Main.DiscoB)).X;
            int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SiriusExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner, hue, Projectile.whoAmI);
            if (Main.projectile.IndexInRange(p))
                Main.projectile[p].originalDamage = Projectile.originalDamage;
        }
    }
}
