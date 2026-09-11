using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 圣誓之刃：高速穿透弹幕，末期减速淡出，命中施加灼烧与暗影焰
    /// </summary>
    internal class ExaltedOathBladeProj:ModProjectile
    {
        private const int Alpha = 100;          // 常态透明度
        private const int FadeOutTime = 85;     // 最后 85 帧为减速淡出阶段
        private const float MaxVelocity = ExaltedOathblade.ShootSpeed * 4f;  // 最大飞行速度（武器弹速的 4 倍）
        private const int TimeLeft = 435 + FadeOutTime;  // 总存活帧数 = 加速阶段 435 帧 + 淡出阶段 85 帧
        /// <summary>
        /// 静态属性：预留 10 格残影缓存并启用残影绘制
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>
        /// 基础属性：46x46 半透明（alpha 100）碰撞箱；入水不减速、友方、近战伤害、可穿透 3 个敌人；
        /// 每帧额外更新 2 次提速；每个敌人独立 40 帧命中冷却（20×MaxUpdates）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.alpha = Alpha;
            Projectile.timeLeft = TimeLeft;
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.MaxUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20 * Projectile.MaxUpdates;
        }
        /// <summary>
        /// 加速/减速与旋转控制，末期进入减速淡出阶段
        /// </summary>
        public override void AI()
        {
            float alphaLightScale = Projectile.alpha / (float)Alpha;
            Lighting.AddLight(Projectile.Center, 0.5f * alphaLightScale, 0f, 0.5f * alphaLightScale);
            // 加速阶段（剩余时间 > 85 帧）：未达到最大速度前每帧 ×1.03 加速；淡出阶段则 ×0.95 减速
            if (Projectile.timeLeft > FadeOutTime)
            {
                if (Projectile.velocity.Length() < MaxVelocity)
                    Projectile.velocity *= 1.03f;
            }
            else
                Projectile.velocity *= 0.95f;
            if (Projectile.velocity.X < 0f)
                Projectile.spriteDirection = -1;
            // 基础旋转每帧固定 0.025 弧度；加速阶段叠加 0.25 旋转，淡出阶段旋转量随剩余时间线性收窄
            Projectile.rotation += Projectile.direction * 0.025f;
            if (Projectile.timeLeft > FadeOutTime)
                Projectile.rotation += Projectile.direction * 0.25f;
            else
                Projectile.rotation += Projectile.direction * 0.25f * (Projectile.timeLeft / (float)FadeOutTime);
        }
        /// <summary>
        /// 命中敌人时施加灼烧与暗影焰 debuff
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
            target.AddBuff(BuffID.ShadowFlame, 90);
        }
        /// <summary>
        /// 命中玩家（PvP）时施加灼烧与暗影焰 debuff
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 180);
            target.AddBuff(BuffID.ShadowFlame, 90);
        }
        /// <summary>
        /// 绘制残影拖尾
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 2);
            return false;
        }
        /// <summary>
        /// 颜色/透明度：进入淡出阶段后按剩余时间线性降低透明度并整体变暗；常态为白色 + alpha 100
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < FadeOutTime)
            {
                byte b2 = (byte)(Projectile.timeLeft * 3);
                byte a2 = (byte)(Alpha * ((float)b2 / byte.MaxValue));
                Projectile.alpha = a2;
                return new Color(b2, b2, b2, Projectile.alpha);
            }
            Projectile.alpha = Alpha;
            return new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, Projectile.alpha);
        }
    }
}
