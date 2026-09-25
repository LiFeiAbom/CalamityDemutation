using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 中子箭（NeutronArrow） - 洛希之弦左键射出的重力箭矢，逻辑逐行移植自 CWR 0.4.0.1.3。
    /// 高速（额外更新 3）穿透 18 个目标、忽略 80 点护甲，飞行途中沿速度法线两侧各甩出一串星屑，
    /// 首次命中时在目标周围降下三道中子光束并叠三次中子爆点。
    /// <para>
    /// 粒子差异（非玩法偏离）：大修此处用 <c>CWRParticle</c> 体系的 <c>HeavenfallStarParticle</c>，
    /// 本工程没有那套旧粒子框架，改用既有的 <see cref="DRK_HeavenfallStar"/> —— 两者贴图
    /// （灾厄 <c>Projectiles/StarProj</c>）、AI 衰减曲线与双次绘制逐行相同，只是新旧命名体系之别。
    /// </para>
    /// </summary>
    internal class NeutronArrow : ModProjectile
    {
        /// <summary>贴图取大修的中子箭（14×86 的竖直箭矢，配合 SetArrowRot 的 +90° 旋转）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Ranged/NeutronArrow2";
        /// <summary>
        /// 基础属性：32×32 判定箱、存活 120 tick、穿透 18、额外更新 3、忽略 80 点护甲、
        /// 启用本地无敌帧（同一敌人 1 tick 内只吃一次）、出生时全透明（随后逐帧淡入）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.timeLeft = 120;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.MaxUpdates = 3;
            Projectile.penetrate = 18;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.ArmorPenetration = 80;
        }
        /// <summary>
        /// 每帧：淡入 → 按速度摆正箭身（大修 SetArrowRot 的 +90° 版本）→ 补一点白光；
        /// 每满 10 次更新（或 ai[1] 计满 2）沿速度法线两侧各甩出 53 颗星屑，随后把两个计数器复位
        /// （ai[1] 复位成 -1000 是大修原样，等于此后只走 ai[0] 那条计时）
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center + Projectile.velocity, Color.AntiqueWhite.ToVector3());
            if (++Projectile.ai[0] > 10 || ++Projectile.ai[1] > 2)
            {
                Vector2 norl = Projectile.velocity.GetNormalVector();
                float sengs = Projectile.timeLeft * 0.01f;
                for (int j = 0; j < 53; j++)
                {
                    DRKLoader.AddParticle(new DRK_HeavenfallStar(Projectile.Center
                        , norl * (0.1f + j * 0.34f) * sengs, false, 20, Main.rand.NextFloat(1.2f, 2.3f), Color.BlueViolet));
                }
                for (int j = 0; j < 53; j++)
                {
                    DRKLoader.AddParticle(new DRK_HeavenfallStar(Projectile.Center
                        , norl * -(0.1f + j * 0.34f) * sengs, false, 20, Main.rand.NextFloat(1.2f, 2.3f), Color.BlueViolet));
                }
                Projectile.ai[0] = 0;
                Projectile.ai[1] = -1000;
            }
        }
        /// <summary>
        /// 首次命中（numHits 归零那一次，即"命中递减"里的第一击）时，在目标周围 560~780 像素处
        /// 降下三道朝内的中子光束（伤害 ×2）并叠三次中子爆点（伤害 ×1）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits > 0)
                return;
            for (int i = 0; i < 3; i++)
            {
                Vector2 rand = Main.rand.NextVector2Unit() * Main.rand.NextFloat(560f, 780f);
                Vector2 vr = rand.UnitVector() * -20f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center + rand
                    , vr, ModContent.ProjectileType<NeutronLaser>(), Projectile.damage * 2, 0f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center
                    , Vector2.Zero, ModContent.ProjectileType<NeutronExplosionRanged>(), Projectile.damage, 0f);
            }
        }
    }
}
