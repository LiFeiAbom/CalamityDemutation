using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 龙息弹（DragonsBreathRound） - 巨龙之息射出的特殊弹丸（照 CI 的 `DragonsBreathRound` 逐行）。
    /// 判定 4×4（贴图是 2×28 的细长曳光条）、`extraUpdates = 10`、穿透 1、存活 180 帧、
    /// 火焰拖尾（CI 用 `DustID.Flare`；1.3-release/2.0 写的是等价的数字 127——两者是否同一颗尘以 tML 常量为准）。
    /// <para>
    /// 命中：挂 **Dragonfire** 300 帧（走本工程 <c>ApplyCalamityBuffWithFallback</c>，缺灾厄时退回原版
    /// 「着火了！」`OnFire`）+ 原版「燃烧」`OnFire3` 600 帧（CI 的两条都保留）。
    /// 消亡：炸出一枚本工程自持的爆炸弹幕 <see cref="FuckYou"/>（伤害沿用弹丸伤害、
    /// `ai[1]` 随机 0.85~2.0 当缩放、并标成 Ranged 让它吃射手加成），音 `Item14`。
    /// </para>
    /// <para>
    /// 与源的两点差别：① 源写 `Projectile.Calamity().pointBlankShotDuration`（灾厄的贴脸射击机制）——
    /// 本工程没有这套机制，按用户 2026-10-14 口径**不做**；② 拖影走本工程 <c>CDUtil.DrawAfterimagesCentered</c>。
    /// </para>
    /// </summary>
    internal class DragonsBreathRound:ModProjectile
    {
        /// <summary>拖影参数：10 点、模式 0（照源）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        /// <summary>
        /// 弹丸基础属性：4×4 判定、穿透 1、180 帧、射手伤害类型（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.extraUpdates = 10;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.DamageType = DamageClass.Ranged;
        }
        /// <summary>
        /// AI：先按速度方向摆正贴图，再从第 4 帧起每帧撒一颗火焰尘、1/20 概率再补一颗小尘（照源）
        /// </summary>
        public override void AI()
        {
            // 朝向
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) + MathHelper.ToRadians(90) * Projectile.direction;

            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 4f)
            {
                float num93 = Projectile.velocity.X / 3f;
                float num94 = Projectile.velocity.Y / 3f;
                int num95 = 4;
                int idx = Dust.NewDust(new Vector2(Projectile.position.X + num95, Projectile.position.Y + num95), Projectile.width - num95 * 2, Projectile.height - num95 * 2, DustID.Flare, 0f, 0f, 100, default, 2f);
                Dust dust = Main.dust[idx];
                dust.noGravity = true;
                dust.velocity *= 0.1f;
                dust.velocity += Projectile.velocity * 0.1f;
                dust.position.X -= num93;
                dust.position.Y -= num94;

                if (Main.rand.NextBool(20))
                {
                    int num97 = 4;
                    int num98 = Dust.NewDust(new Vector2(Projectile.position.X + num97, Projectile.position.Y + num97), Projectile.width - num97 * 2, Projectile.height - num97 * 2, DustID.Flare, 0f, 0f, 100, default, 0.6f);
                    Main.dust[num98].velocity *= 0.25f;
                    Main.dust[num98].velocity += Projectile.velocity * 0.5f;
                }
            }
        }
        /// <summary>
        /// 消亡：生成爆炸弹幕（只在主人端，见工程记忆第 5 节），并把它的伤害类型标成射手
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FuckYou>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
                // 源用灾厄自己的扩展方法 WithinBounds（本工程软依赖调不到，改成等价的下标判据）
                if (proj >= 0 && proj < Main.maxProjectiles)
                    Main.projectile[proj].DamageType = DamageClass.Ranged;
            }
        }
        /// <summary>
        /// 命中：Dragonfire 300 帧（缺灾厄时退回「着火了！」）+ 原版「燃烧」600 帧（照 CI）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "Dragonfire", 300, BuffID.OnFire);
            target.AddBuff(BuffID.OnFire3, 600);
        }
        /// <summary>
        /// 绘制：画拖影后交给引擎画本体（拖影走本工程 CDUtil）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
