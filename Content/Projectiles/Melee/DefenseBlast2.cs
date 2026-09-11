using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 防御爆裂·小型（DefenseBlast2） - DefenseBlast 的小号版本。
    /// 由 DefenseFlame 命中敌怪 / 玩家时在目标中心原地生成（DamageType 为普通 Melee）。
    /// 判定范围与存活时间都远小于 DefenseBlast，纯金色尘粒视觉、无音效，仅存活 5 帧。
    /// </summary>
    internal class DefenseBlast2:ModProjectile
    {
        /// <summary>
        /// 使用工程内的隐形贴图：本体不做贴图绘制，视觉完全由 AI 中的金色尘粒表现。
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 弹幕基础属性：小范围判定框、无限穿透、10 帧独立命中间隔的瞬时爆破。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 50;      // 判定框宽（像素）
            Projectile.height = 50;     // 判定框高（像素）
            Projectile.friendly = true; // 友方弹幕，只伤害敌怪
            Projectile.ignoreWater = false;  // false=会受水减速（此处保留默认值）
            Projectile.tileCollide = false;  // 不与物块碰撞
            Projectile.penetrate = -1;       // -1 = 无限穿透
            Projectile.timeLeft = 5;         // 存活仅 5 帧，属于瞬时爆点
            Projectile.DamageType = DamageClass.Melee;  // 普通近战伤害（吃攻速加成）
            Projectile.usesLocalNPCImmunity = true;  // 每个敌人独立计算免疫计时
            Projectile.localNPCHitCooldown = 10;     // 同一敌人 10 帧内只受击一次
        }
        /// <summary>
        /// 视觉逻辑：快速衰减的金色尘粒爆点（无音效）。
        /// 结构与 DefenseBlast 相同但整体更小：随机方向范围 ±5、速度 2~4、光照系数 0.15。
        /// </summary>
        public override void AI()
        {
            // 光照强度按不透明度比例给出，亮度弱于 DefenseBlast（系数 0.15）
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.15f / 255f, (255 - Projectile.alpha) * 0.15f / 255f, (255 - Projectile.alpha) * 0f / 255f);
            // projTimer 表示本帧要喷出的尘粒数量：初始 25，ai[0] 超过 180 后线性衰减
            float projTimer = 25f;
            if (Projectile.ai[0] > 180f)
            {
                projTimer -= (Projectile.ai[0] - 180f) / 2f;
            }
            if (projTimer <= 0f)
            {
                projTimer = 0f;         // 钳制为 0，避免出现负数的尘粒数
                Projectile.Kill();      // 尘粒耗尽即自毁
            }
            projTimer *= 0.7f;          // 整体衰减系数
            Projectile.ai[0] += 4f;     // 累计计时器（timeLeft=5，实际远未到 180 就会先自然消亡）
            int timerCounter = 0;
            while (timerCounter < projTimer)
            {
                float rando1 = Main.rand.Next(-5, 6);
                float rando2 = Main.rand.Next(-5, 6);
                float rando3 = Main.rand.Next(2, 5);
                float randoAdjuster = (float)Math.Sqrt((double)(rando1 * rando1 + rando2 * rando2));
                randoAdjuster = rando3 / randoAdjuster;   // 归一化：把随机方向缩放到 2~4 的速度
                rando1 *= randoAdjuster;
                rando2 *= randoAdjuster;
                int goldDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, new Color(255, Main.DiscoG, 53), 1.5f);
                Main.dust[goldDust].noGravity = true;
                Main.dust[goldDust].position.X = Projectile.Center.X;    // 统一从爆心出发
                Main.dust[goldDust].position.Y = Projectile.Center.Y;
                Dust expr_149DF_cp_0 = Main.dust[goldDust];
                expr_149DF_cp_0.position.X += Main.rand.Next(-10, 11);   // 随机抖动避免完全重叠
                Dust expr_14A09_cp_0 = Main.dust[goldDust];
                expr_14A09_cp_0.position.Y += Main.rand.Next(-10, 11);
                Main.dust[goldDust].velocity.X = rando1;
                Main.dust[goldDust].velocity.Y = rando2;
                timerCounter++;
            }
        }
    }
}
