using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 防御爆裂（DefenseBlast） - 防御之刃（DefenseBlade）的近战命中爆炸。
    /// 由 DefenseBlade.OnHitNPC / OnHitPvp 在敌人（或 PvP 目标）中心原地生成，速度为零，
    /// 是一个纯视觉 + 范围伤害的 150×150 判定场，持续 60 帧后自动消失。
    /// </summary>
    internal class DefenseBlast:ModProjectile
    {
        /// <summary>
        /// 使用工程内的隐形贴图：本体不做贴图绘制，视觉完全由 AI 中的金色尘粒表现。
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 弹幕基础属性：大范围判定框、无限穿透、20 帧独立命中间隔的原地爆破。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 150;     // 判定框宽（像素），决定爆炸覆盖范围
            Projectile.height = 150;    // 判定框高（像素）
            Projectile.friendly = true; // 友方弹幕，只伤害敌怪
            Projectile.ignoreWater = false;  // false=会受水减速（此处保留默认值）
            Projectile.tileCollide = false;  // 不与物块碰撞，可在方块内存在
            Projectile.penetrate = -1;       // -1 = 无限穿透，不因命中而消失
            Projectile.timeLeft = 60;        // 存活 60 帧
            Projectile.DamageType = DamageClass.MeleeNoSpeed;  // 近战伤害但不吃攻速加成
            Projectile.usesLocalNPCImmunity = true;  // 每个敌人独立计算免疫计时
            Projectile.localNPCHitCooldown = 20;     // 同一敌人 20 帧内只受击一次
        }
        /// <summary>
        /// 视觉逻辑：金色光照 + 首帧音效 + 随时间衰减的金色尘粒爆发。
        /// ai[0] 充当累计计时器，每次 +4；爆裂初期尘粒最密，后期逐渐稀疏直至弹幕自毁。
        /// </summary>
        public override void AI()
        {
            // 光照强度按不透明度（255-alpha）比例给出，金色分量只点亮红绿通道
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.35f / 255f, (255 - Projectile.alpha) * 0.35f / 255f, (255 - Projectile.alpha) * 0f / 255f);
            // localAI[0] 当一次性开关：仅生成首帧播放一次爆炸音效
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
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
            projTimer *= 0.7f;          // 整体衰减系数，压低单帧尘粒上限
            Projectile.ai[0] += 4f;     // 累计计时器
            int timerCounter = 0;
            // 逐颗喷出尘粒（数量由 projTimer 决定，故初期约 17 颗/帧、后期递减）
            while (timerCounter < projTimer)
            {
                float rando1 = Main.rand.Next(-15, 16);
                float rando2 = Main.rand.Next(-15, 16);
                float rando3 = Main.rand.Next(4, 13);
                float randoAdjuster = (float)Math.Sqrt((double)(rando1 * rando1 + rando2 * rando2));
                randoAdjuster = rando3 / randoAdjuster;   // 归一化：把随机方向缩放到 4~12 的速度
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
