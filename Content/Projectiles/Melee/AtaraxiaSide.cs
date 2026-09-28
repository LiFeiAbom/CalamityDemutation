using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 禅心剑 · 侧弹（AtaraxiaSide）—— 挥砍时从两肩斜 45° 各射出的一枚「分裂导弹」（照搬灾厄 2.0.4 的 <c>AtaraxiaSide</c>）。
    /// 贴图 5 帧横向动画；<c>ai[1]</c> 记 1 / 2 区分左右两枚，只用来把绘制朝向偏转 ±0.2 弧度。
    /// 穿透 1，命中即亡，消亡时以自身为中心放射状分裂出 6 枚针叶弹（<see cref="AtaraxiaSplit"/>，每枚伤害取本弹的 2%）。
    /// 两处偏离：① DustID 由灾厄原码的魔法数字 261 改用具名常量 AncientLight；② 补写了 PvP 用的 OnHitPlayer。
    /// </summary>
    internal class AtaraxiaSide : ModProjectile
    {
        // ── 静态字段 ──
        /// <summary>贴图横向帧数</summary>
        private const int NumAnimationFrames = 5;
        /// <summary>换帧间隔（帧计数器超过它就进一帧）</summary>
        private const int AnimationFrameTime = 9;
        // ── 实例字段 ──
        /// <summary>存活帧计数：前 8 帧不撒尘</summary>
        public int time = 0;
        // ── 生命周期方法 ──
        /// <summary>贴图切成 5 帧横向动画</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = NumAnimationFrames;
        }
        /// <summary>基础属性：8×8、友方近战、穿透 1、每帧更新 6 格（extraUpdates 5）、不碰撞物块（改用圆形判定）、存活 300 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 300;
        }
        /// <summary>每帧：按 ai[1] 把朝向偏转 ±0.2 弧度、发紫光、第 8 帧起撒青尘、推进 5 帧动画</summary>
        public override void AI()
        {
            DrawOffsetX = -28;
            DrawOriginOffsetY = -2;
            DrawOriginOffsetX = 12;
            if (Projectile.ai[1] == 2)
            {
                Projectile.rotation = (Projectile.velocity.RotatedBy(0.2f)).ToRotation();
            }
            else
            {
                Projectile.rotation = (Projectile.velocity.RotatedBy(-0.2f)).ToRotation();
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.1f, 0.45f);
            if (time > 8 && Main.rand.NextBool())
            {
                float colorRando = Main.rand.NextFloat(0, 1);
                Vector2 dustvel = Projectile.ai[1] == 2 ? Projectile.velocity.RotatedBy(0.2f) : Projectile.velocity.RotatedBy(-0.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity, DustID.AncientLight, -dustvel * Main.rand.NextFloat(0.2f, 1.2f), 0, default, Main.rand.NextFloat(0.4f, 0.6f));
                dust.noGravity = true;
                dust.color = Color.Lerp(Color.DarkOrchid, Color.IndianRed, colorRando);
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter > AnimationFrameTime)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= NumAnimationFrames)
                Projectile.frame = 0;
            time++;
        }
        /// <summary>命中敌人：挂原版暗影焰 180 帧</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
        }
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构，挂原版暗影焰</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
        }
        /// <summary>
        /// 消亡：播碎片音，以自身为中心把 6 枚针叶弹均布成一圈甩出去（角度步长 2π/6，起始方向随机、逐枚转过去）。
        /// 每枚伤害取本弹的 2%（灾厄原注释如此）。子弹幕只在本地生成，避免多人下各端重复。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item89, Projectile.Center);
            int numSplits = 6;
            int splitID = ModContent.ProjectileType<AtaraxiaSplit>();
            int damage = (int)(Projectile.damage * 0.02f);
            float angleVariance = MathHelper.TwoPi / numSplits;
            Vector2 projVec = new Vector2(4.5f, 0f).RotatedByRandom(MathHelper.TwoPi);
            for (int i = 0; i < numSplits; ++i)
            {
                projVec = projVec.RotatedBy(angleVariance);
                if (Projectile.owner == Main.myPlayer)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, projVec, splitID, damage, 1.5f, Main.myPlayer);
            }
        }
        /// <summary>圆形判定（内联灾厄 CollisionUtils.CircularHitboxCollision）：半径 15</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle center = new Rectangle((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= 15f;
        }
    }
}
