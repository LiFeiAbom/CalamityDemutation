using Microsoft.Xna.Framework;
using System;
using System.Security.Policy;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 火球（FireBall） - 巨龙之怒（DragonRage）命中敌人/玩家时原地生成的爆炸弹幕。
    /// 由生成者以 0 速度放置、并传入 0.85~2.0 的随机膨胀参数（ai[1]）；第一帧把碰撞箱从中心扩张到 52*scale
    /// 并喷出大量烟雾与火焰粉尘，随后逐帧播放 5 帧动画并淡入，总计 15 帧（3*5）后自毁。
    /// 同一敌人 600 帧内只会被它命中一次，主要用于范围伤害与视觉表现。
    /// </summary>
    internal class FireBall:ModProjectile
    {
        /// <summary>
        /// 注册 5 帧动画（爆炸的逐帧贴图）。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }
        /// <summary>
        /// 基础属性：初始 8x8 碰撞箱（第一帧才扩张）、友方、入水会减速、不碰撞地形；
        /// 起始全透明并逐帧淡入；无限穿透、timeLeft 记 60 帧（实际由 AI 在第 15 帧主动 Kill）、同一敌人 600 帧内只吃一次伤害。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;                    // 初始贴图/碰撞箱宽（第一帧按 52*scale 扩张）
            Projectile.height = 8;                   // 初始贴图/碰撞箱高
            Projectile.friendly = true;              // 友方弹幕
            Projectile.ignoreWater = false;          // 入水会减速（保留原版默认行为）
            Projectile.tileCollide = false;          // 不碰撞地形
            Projectile.alpha = 255;                  // 起始完全透明，AI 中每帧 -63 淡入
            Projectile.penetrate = -1;               // 无限穿透
            Projectile.timeLeft = 60; // Lasts so long due to visuals.
            Projectile.usesLocalNPCImmunity = true;  // 使用逐 NPC 独立无敌计时
            Projectile.localNPCHitCooldown = 600; // Under absolutely no circumstances should this explosion hit more than once.
        }
        /// <summary>
        /// AI：ai[0] 为存活帧计数（达到 3*5=15 帧即自毁）；ai[1] 控制膨胀（生成者传入 0.85~2.0 的随机初值，
        /// 每帧 +0.01，scale 取其一半）；每 3 帧推进一帧动画并在播完后隐藏；alpha 每帧 -63 淡入；
        /// 第 1 帧执行爆炸瞬间处理：扩张碰撞箱、播放爆炸音效、喷出烟雾/火焰/环形火焰/环形烟雾四组粉尘。
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.9f, 0.8f, 0.6f);   // 暖色火光
            Projectile.ai[1] += 0.01f;                                 // 膨胀参数随时间缓慢增大
            Projectile.scale = Projectile.ai[1] * 0.5f;                // 贴图缩放 = 膨胀参数的一半
            Projectile.ai[0] += 1f;                                    // 存活帧计数
            // 存活满 3 轮完整动画（3*5=15 帧）即结束
            if (Projectile.ai[0] >= (float)(3 * Main.projFrames[Type]))
            {
                Projectile.Kill();
                return;
            }
            // 每 3 帧推进一帧动画；播完最后一帧后隐藏本体（后续只靠粉尘表现）
            int incrementer = Projectile.frameCounter + 1;
            Projectile.frameCounter = incrementer;
            if (incrementer >= 3)
            {
                Projectile.frameCounter = 0;
                incrementer = Projectile.frame + 1;
                Projectile.frame = incrementer;
                if (incrementer >= Main.projFrames[Type])
                {
                    Projectile.hide = true;
                }
            }
            // 逐帧淡入：alpha 从 255 每帧 -63，约 4 帧后完全可见
            Projectile.alpha -= 63;
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            // 第 1 帧：爆炸瞬间处理（扩张碰撞箱、播放音效与喷出粉尘）
            if (Projectile.ai[0] == 1f)
            {
                // 先记录 Center 再改尺寸，保证碰撞箱扩张后仍以原位置为中心
                Projectile.position = Projectile.Center;
                Projectile.width = Projectile.height = (int)(52f * Projectile.scale);
                Projectile.Center = Projectile.position;
                SoundEngine.PlaySound(SoundID.Item14, Projectile.position);   // 爆炸音效
                // 第 1 组：4 颗烟雾，围绕中心随机散布
                for (int dustIndexA = 0; dustIndexA < 4; dustIndexA = incrementer + 1)
                {
                    int smoky = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                    Main.dust[smoky].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * (float)Projectile.width / 2f;
                    incrementer = dustIndexA;
                }
                // 第 2 组：10 次循环，每次各生成一颗大火尘与一颗小火尘
                for (int dustIndexB = 0; dustIndexB < 10; dustIndexB = incrementer + 1)
                {
                    int fireDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 200, default, 2.7f);
                    Dust dust = Main.dust[fireDust];
                    dust.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * (float)Projectile.width / 2f;
                    dust.noGravity = true;
                    dust.velocity *= 3f;
                    fireDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1.5f);
                    dust.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * (float)Projectile.width / 2f;
                    dust.velocity *= 2f;
                    dust.noGravity = true;
                    dust.fadeIn = 2.5f;
                    incrementer = dustIndexB;
                }
                // 第 3 组：5 颗沿弹幕飞行方向排布的火焰尘
                for (int dustIndexC = 0; dustIndexC < 5; dustIndexC = incrementer + 1)
                {
                    int fireDust2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, default, 2.7f);
                    Dust dust = Main.dust[fireDust2];
                    dust.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy((double)Projectile.velocity.ToRotation(), default) * (float)Projectile.width / 2f;
                    dust.noGravity = true;
                    dust.velocity *= 3f;
                    incrementer = dustIndexC;
                }
                // 第 4 组：10 颗沿弹幕飞行方向排布的烟雾
                for (int dustIndexD = 0; dustIndexD < 10; dustIndexD = incrementer + 1)
                {
                    int smokier = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 0, default, 1.5f);
                    Dust dust = Main.dust[smokier];
                    dust.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy((double)Projectile.velocity.ToRotation(), default) * (float)Projectile.width / 2f;
                    dust.noGravity = true;
                    dust.velocity *= 3f;
                    incrementer = dustIndexD;
                }
            }
        }
        /// <summary>
        /// 第 1 帧（ai[0] 尚未递增）作为纯逻辑/特效帧不绘制，从第 2 帧起才画出爆炸贴图。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            return Projectile.ai[0] > 1f;
        }
        /// <summary>
        /// 返回半透明白色作为绘制色（alpha 固定 127，叠加出明亮的爆炸效果）。
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 127);
        }
        /// <summary>
        /// 命中 NPC 后把朝向对齐主人，保证弹幕方向与玩家朝向一致。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Projectile.direction = Main.player[Projectile.owner].direction;
        /// <summary>
        /// 命中玩家（PvP）后同样把朝向对齐主人。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => Projectile.direction = Main.player[Projectile.owner].direction;
    }
}
