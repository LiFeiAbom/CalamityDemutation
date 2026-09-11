using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 防御火舌（DefenseFlame） - 防御之刃弹幕链的末环。
    /// 由 DefenseBeam.OnKill 爆炸时随机散射 3~4 枚，带重力下坠的火花类弹幕。
    /// 命中敌怪或玩家后原地生成 DefenseBlast2 小爆点，形成「光束→火舌→小爆」的三段伤害链。
    /// </summary>
    internal class DefenseFlame:ModProjectile
    {
        /// <summary>
        /// 使用工程内的隐形贴图：本体不做贴图绘制，视觉完全由 AI 中的金色尘粒表现。
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 弹幕基础属性：细小判定框、穿透 1 次、受重力下坠的友方近战弹幕。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 6;       // 判定框宽（像素），很细
            Projectile.height = 12;     // 判定框高（像素）
            Projectile.friendly = true; // 友方弹幕，只伤害敌怪
            Projectile.penetrate = 1;   // 命中一次即消失（触发 OnHitNPC 生成 DefenseBlast2）
            Projectile.timeLeft = 120;  // 存活 120 帧，随后自然消亡
            Projectile.DamageType = DamageClass.Melee;  // 归属近战伤害
        }
        /// <summary>
        /// 运动逻辑：先修正异常速度，延迟 5 帧后施加重力，同时用金色尘粒模拟火焰。
        /// 速度躺平后横向摩擦减速、纵向恒加速度下坠，并限制最大下落速度。
        /// </summary>
        public override void AI()
        {
            // 三处 velocity.X != velocity.X / velocity.Y != velocity.Y 是 NaN 检测惯用写法：
            // NaN 与自身比较恒为 false（即 != 恒为 true），借此识别速度异常并取反修正
            if (Projectile.velocity.X != Projectile.velocity.X)
            {
                Projectile.velocity.X = Projectile.velocity.X * -0.1f;
            }
            if (Projectile.velocity.X != Projectile.velocity.X)
            {
                Projectile.velocity.X = Projectile.velocity.X * -0.5f;
            }
            if (Projectile.velocity.Y != Projectile.velocity.Y && Projectile.velocity.Y > 1f)
            {
                Projectile.velocity.Y = Projectile.velocity.Y * -0.5f;
            }
            Projectile.ai[0] += 1f;   // 出生后的帧计数（用于延迟启动重力）
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;   // 钳制，避免继续累加
                // 落地（Y 速度归零）且仍有水平速度时施加 0.97 的横向摩擦
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    if (Projectile.velocity.X > -0.01 && Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;   // 速度过小直接归零，防止无限缓慢滑动
                        Projectile.netUpdate = true;  // 同步该速度变化到其他客户端
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;   // 每帧加重力加速度
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;   // 依水平速度滚动（下方会被覆盖）
            // 主火焰尘粒：左移 2、下移 2 作为偏移，尺寸随机放大后被“上抬”2（火焰上飘观感）
            int gFlame = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, new Color(255, Main.DiscoG, 53), 1f);
            Dust d = Main.dust[gFlame];
            d.position.X -= 2f;
            Dust d2 = Main.dust[gFlame];
            d2.position.Y += 2f;
            Main.dust[gFlame].scale += Main.rand.Next(50) * 0.01f;
            Main.dust[gFlame].noGravity = true;
            Dust d3 = Main.dust[gFlame];
            d3.velocity.Y -= 2f;
            // 50% 概率追加一颗更大、速度极小的副尘粒，增强火焰的浓密感
            if (Main.rand.NextBool())
            {
                int gFlame2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, new Color(255, Main.DiscoG, 53), 1f);
                Dust d4 = Main.dust[gFlame2];
                d4.position.X -= 2f;
                Dust d5 = Main.dust[gFlame2];
                d5.position.Y += 2f;
                Main.dust[gFlame2].scale += 0.3f + Main.rand.Next(50) * 0.01f;
                Main.dust[gFlame2].noGravity = true;
                Main.dust[gFlame2].velocity *= 0.1f;
            }
            // 下落初段（Y 速度 0.15~0.25 的过渡区间）额外衰减水平速度，让抛物线更自然
            if (Projectile.velocity.Y < 0.25 && Projectile.velocity.Y > 0.15)
            {
                Projectile.velocity.X = Projectile.velocity.X * 0.8f;
            }
            Projectile.rotation = -Projectile.velocity.X * 0.05f;   // 最终旋转角由水平速度决定（覆盖上方累加值）
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;   // 限制最大下落速度，避免高速穿透
            }
        }
        /// <summary>
        /// 命中敌怪：在目标中心原地生成 DefenseBlast2 小爆点，伤害沿用本次命中伤害。
        /// 仅拥有者客户端生成，避免多人重复。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center.X, target.Center.Y, 0f, 0f, ModContent.ProjectileType<DefenseBlast2>(), hit.Damage, hit.Knockback, Main.myPlayer);
            }
        }
        /// <summary>
        /// PvP 命中玩家：同样在目标中心生成 DefenseBlast2 小爆点（伤害取自 HurtInfo）。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center.X, target.Center.Y, 0f, 0f, ModContent.ProjectileType<DefenseBlast2>(), info.Damage, info.Knockback, Main.myPlayer);
            }
        }
        /// <summary>
        /// 返回 false 表示撞击物块后不消亡（火舌可穿过地形继续飞行），本方法不改动速度。
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity) => false;
    }
}
