using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Healing
{
    /// <summary>
    /// 森林球（SilvaOrb） - 森林套（Silva armor）吸血时生成的治疗弹幕。
    /// 由 CalamityDemutationGlobalProjectile 在 silvaSet 触发吸血时生成，参数 ai[0] = 目标玩家编号、ai[1] = 治疗量
    /// （治疗量 = 弹幕伤害 × (3% − 该弹幕已命中次数×1.5%)，会优先送给 1200 像素内血量缺口最大的队友）。
    /// 与同目录 CactusHealOrb / FungalHeal 的异同：三者都是"触碰玩家即回血"的治疗球（前两者用隐形贴图、靠粉尘表现），
    /// 但本弹幕自带贴图与呼吸式缩放、追踪速度更快（6）、每帧额外更新 3 次，且目标可以是任意队友而不限于主人。
    /// </summary>
    internal class SilvaOrb:ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：14x14 碰撞箱、友方；起始全透明后逐帧淡入；单次穿透、存活 300 帧、
        /// 每帧额外更新 3 次（追踪更细腻）、自身发光 0.2。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;          // 贴图/碰撞箱宽（像素）
            Projectile.height = 14;         // 贴图/碰撞箱高（像素）
            Projectile.friendly = true;     // 友方弹幕
            Projectile.alpha = 255;         // 起始全透明，AI 中每帧 -2 淡入
            Projectile.penetrate = 1;       // 单次穿透（被吸收后即消失）
            Projectile.timeLeft = 300;      // 存活 300 帧
            Projectile.extraUpdates = 3;    // 每帧额外更新 3 次，追踪更顺滑
            Projectile.light = 0.2f;        // 自身发光强度
        }
        /// <summary>
        /// AI：逐帧淡入；用 localAI[0] 在放大（到 1.2）与缩小（到 0.8）之间切换，形成呼吸式脉动；
        /// 随后读取 ai[0] 指定的玩家并向其平滑追踪；与玩家碰撞且距离小于 50 像素时被吸收并回血，然后销毁。
        /// </summary>
        public override void AI()
        {
            Projectile.alpha -= 2;   // 每帧淡入 2 点，约 128 帧后完全显现
            // localAI[0] 作为缩放方向开关：0 = 放大中，1 = 缩小中
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale += 0.05f;
                if ((double)Projectile.scale > 1.2)
                {
                    Projectile.localAI[0] = 1f;
                }
            }
            else
            {
                Projectile.scale -= 0.05f;
                if ((double)Projectile.scale < 0.8)
                {
                    Projectile.localAI[0] = 0f;
                }
            }
            int num487 = (int)Projectile.ai[0];   // ai[0]：目标玩家编号
            float num488 = 6f;                    // 追踪飞行速度
            Vector2 vector36 = new(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
            float num489 = Main.player[num487].Center.X - vector36.X;
            float num490 = Main.player[num487].Center.Y - vector36.Y;
            // 计算弹幕与目标玩家的当前距离
            float num491 = (float)Math.Sqrt((double)(num489 * num489 + num490 * num490));
            // 与目标玩家碰撞重叠且距离小于 50 像素时：判定为吸收成功
            if (num491 < 50f && Projectile.position.X < Main.player[num487].position.X + (float)Main.player[num487].width && Projectile.position.X + (float)Projectile.width > Main.player[num487].position.X && Projectile.position.Y < Main.player[num487].position.Y + (float)Main.player[num487].height && Projectile.position.Y + (float)Projectile.height > Main.player[num487].position.Y)
            {
                // 仅主人端执行治疗，且主人处于月亮吸血诅咒（moonLeech）时跳过回血，避免联机重复触发
                if (Projectile.owner == Main.myPlayer && !Main.LocalPlayer.moonLeech)
                {
                    int num492 = (int)Projectile.ai[1];   // ai[1]：本次应回复的生命值
                    Main.player[num487].HealEffect(num492, false);
                    Main.player[num487].statLife += num492;
                    if (Main.player[num487].statLife > Main.player[num487].statLifeMax2)
                    {
                        Main.player[num487].statLife = Main.player[num487].statLifeMax2;
                    }
                    NetMessage.SendData(MessageID.SpiritHeal, -1, -1, null, num487, (float)num492, 0f, 0f, 0, 0, 0);
                }
                Projectile.Kill();   // 无论是否成功回血，接触即销毁
            }
            // 未吸收：把指向玩家的方向缩放为速度 6，再与旧速度做 15:1 加权平滑转向
            num491 = num488 / num491;
            num489 *= num491;
            num490 *= num491;
            Projectile.velocity.X = (Projectile.velocity.X * 15f + num489) / 16f;
            Projectile.velocity.Y = (Projectile.velocity.Y * 15f + num490) / 16f;
            return;
        }
        /// <summary>
        /// 用迪斯科彩虹色（R 通道随时间循环）叠加固定绿 (203,103) 与当前 alpha 作为绘制色。
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(Main.DiscoR, 203, 103, Projectile.alpha);
        }
        /// <summary>
        /// 消亡（被吸收或超时）时喷出 5 颗叶绿粉尘作收尾特效。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            for (int num407 = 0; num407 < 5; num407++)
            {
                int num408 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 0, new Color(Main.DiscoR, 203, 103), 1f);
                Main.dust[num408].noGravity = true;
                Main.dust[num408].velocity *= 1.5f;
                Main.dust[num408].scale = 1.5f;
            }
        }
    }
}
