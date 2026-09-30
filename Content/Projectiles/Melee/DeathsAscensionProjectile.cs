using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 升格镰刀（Ascended Scythe，移植自灾厄 2.0.4 的 <c>DeathsAscensionProjectile</c>）——
    /// 死神擢升甩出的追踪飞镰：紫色残影拖尾，朝 900 像素内敌人平滑追踪，穿透 2 次后消失。
    /// 移除了源里 override AI 后已失效的 <c>aiStyle = Sickle</c> / <c>AIType = DeathSickle</c> 死配置。
    /// </summary>
    internal class DeathsAscensionProjectile:ModProjectile
    {
        /// <summary>
        /// 静态属性：预留 10 格残影缓存并启用模式 1（圣骑士锤式）残影绘制
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>
        /// 基础属性：102×82、友方、近战伤害、不碰撞物块、初始半透明（alpha 55）、穿透 2 次、
        /// 存活 180 帧、入水不减速、本地无敌帧 30
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 102;
            Projectile.height = 82;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.alpha = 55;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        /// <summary>
        /// AI：发出紫光，1/3 概率甩紫色尘，朝 900 像素内敌人平滑追踪（追踪速度 18、惯性 20）；
        /// 末尾按剩余寿命递减旋转（照搬 1.4.4，模仿原版死神镰刀的甩出旋转）
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0f / 255f, (255 - Projectile.alpha) * 0.65f / 255f);

            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            }

            CDUtil.HomeInOnNPC(Projectile, true, 900f, 18f, 20f);

            // 旋转（照搬 1.4.4：模仿原版死神镰刀，随剩余寿命逐渐减速）
            if (Projectile.velocity.X < 0f)
            {
                Projectile.spriteDirection = -1;
            }
            Projectile.rotation += (float)Projectile.direction * 0.05f;
            Projectile.rotation += (float)Projectile.direction * 0.5f * ((float)Projectile.timeLeft / 180f);
        }
        /// <summary>
        /// 绘制紫色残影拖尾（替换默认绘制）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 2);
            return false;
        }
        /// <summary>
        /// 固定紫色着色（照抄源，alpha 分量原样为 0）
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(150, 0, 200, 0);
        }
    }
}
