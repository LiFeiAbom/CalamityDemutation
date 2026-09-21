using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 中子光束（NeutronLaser） - 中子弹（<see cref="NeutronBullet"/>）命中后从目标四周朝内落下的光束，
    /// 移植自 CWR main 版的同名类。
    /// <para>
    /// **本弹幕完全不绘制**：贴图是工程的隐形占位，<see cref="PreDraw"/> 恒返回 false，
    /// 视觉全部由每帧喷出的一颗蓝紫 DRK_Spark 串成拖尾。这是新版相对 0.5.0.1.7 的关键改动——
    /// 旧版沿速度方向铺最多 100 节贴图，而 CWR 那一类从不设置 <c>Projectile.rotation</c>，
    /// 导致每节贴图恒以 90° 绘制、只有整条链的行进方向跟随速度，观感很怪；新版直接把这一层删掉了。
    /// </para>
    /// </summary>
    internal class NeutronLaser : ModProjectile
    {
        /// <summary>本体无贴图，使用工程通用的 InvisibleProj 占位</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：5×5 判定箱、全隐起步、无限穿透、每帧更新 5 次、存活 60 帧、不撞地形、忽略水体、
        /// 远程系、同敌人 1 帧冷却、80 点护甲穿透
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 4;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = -1;                 // 走本类的 AI，不套原版模板
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.ArmorPenetration = 80;
        }
        /// <summary>
        /// 每帧：淡入（alpha 每帧 -25），并按 <c>ai[1]</c> 决定光束"生长"还是"收束"——
        /// 为 0 时 <c>localAI[0]</c> 每帧 +3 涨到上限 100，否则每帧 -3、收到 0 以下即销毁。
        /// 每帧另喷一颗蓝紫 DRK_Spark，视觉全靠它
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            const float inc = 3f;
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += inc;
                if (Projectile.localAI[0] > 100f)
                    Projectile.localAI[0] = 100f;
            }
            else
            {
                Projectile.localAI[0] -= inc;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }
            BaseParticle spark = new DRK_Spark(Projectile.Center, Projectile.velocity, false, 10
                , Main.rand.NextFloat(1.2f, 2.3f), Color.BlueViolet);
            DRKLoader.AddParticle(spark);
        }
        /// <summary>命中敌怪：把该敌人的受击无敌帧清零，使光束能连续多段命中</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.immune[Projectile.owner] = 0;
        /// <summary>本体不绘制：外观完全交给 DRK_Spark 粒子拖尾</summary>
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
