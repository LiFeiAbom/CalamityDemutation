using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 中子追踪球（NeutronsOrb） - 中子长戟右键手持弹幕（NeutronGlaiveHeld）蓄力时喷出的追踪弹。
    /// 本体无贴图（InvisibleProj 占位），仅靠 DRK_Spark 粒子表现；每帧追踪最近敌人。
    /// CWR 原版靠灾厄的 allProjectilesHome 追踪，本模组用 ChasingBehavior2 + FindClosestNPC 等价实现。
    /// </summary>
    internal class NeutronsOrb : ModProjectile
    {
        /// <summary>
        /// 无贴图弹幕，使用工程通用的 InvisibleProj 占位贴图
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 弹幕基础属性：22×22 判定箱、存活 120 帧、无贴图追踪弹
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;   // 正方形判定箱
            Projectile.timeLeft = 120;                   // 存活 120 帧
            Projectile.friendly = true;                  // 友方弹幕
            Projectile.ignoreWater = true;               // 忽略水体
            Projectile.tileCollide = false;              // 不与物块碰撞
        }
        /// <summary>
        /// 每帧在自身位置喷出一枚 DRK_Spark 火花，并追踪最近敌人（软依赖等价物，替代 CWR 的 allProjectilesHome）
        /// </summary>
        public override void AI()
        {
            BaseParticle spark = new DRK_Spark(Projectile.Center, Projectile.velocity, false, 17, Main.rand.NextFloat(0.2f, 0.3f), Color.BlueViolet);
            DRKLoader.AddParticle(spark);
            NPC target = Projectile.Center.FindClosestNPC(1000f);   // 找 1000 范围内最近敌人
            if (target != null)
            {
                Projectile.ChasingBehavior2(target.Center, 1f, 0.15f);   // 平滑转向目标，速度大小不变
            }
        }
    }
}
