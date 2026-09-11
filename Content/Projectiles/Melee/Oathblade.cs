using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 誓刃：镰刀式旋转飞行的近战弹幕，命中施加灼烧与暗影焰
    /// </summary>
    internal class Oathblade:ModProjectile
    {
        /// <summary>
        /// 静态属性：预留 10 格残影缓存并启用残影绘制
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>
        /// 基础属性：58x58 碰撞箱；近战伤害、友方；借用恶魔镰刀 AI 模板、半透明（alpha 100）、
        /// 撞实心块反弹、单次穿透、存活 300 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.aiStyle = ProjAIStyleID.Sickle;
            Projectile.alpha = 100;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            AIType = ProjectileID.DemonScythe;
        }
        /// <summary>
        /// 发出紫光并随机生成暗影系拖尾粉尘
        /// </summary>
        public override void AI()
        {
            // 叠加紫色光照，营造暗影武器氛围
            Lighting.AddLight(Projectile.Center, 0.35f, 0f, 0.35f);
            // 1/3 概率沿路径生成一粒受 25% 速度影响的暗影光束粉尘作拖尾
            if (Main.rand.NextBool(3))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f);
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
    }
}
