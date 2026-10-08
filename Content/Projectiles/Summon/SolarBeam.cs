using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 太阳光束（SolarBeam，移植自灾厄 2.0 的 Projectiles/Summon/SolarBeam）—— 太阳神（SolarGod）的射击弹幕。
    /// 本体不绘制（复用工程共用的隐形占位贴图），靠 <c>aiStyle 48</c> 的原版激光行为推进，沿途喷金色光点。
    /// </summary>
    internal class SolarBeam:ModProjectile
    {
        /// <summary>本体不绘制，复用工程里的共用隐形占位贴图（Content/Projectiles/InvisibleProj.png）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>标记为召唤物射击弹幕（原版把这类弹幕视作召唤伤害的载体）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：4x4 碰撞箱、原版激光 AI（源写裸值 48 = ProjAIStyleID.Ray）、穿透 1、220 倍额外更新、200 帧寿命、
        /// 不占召唤栏、伤害类型召唤（照源 2.0）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.aiStyle = ProjAIStyleID.Ray;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 220;
            Projectile.timeLeft = 200;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// 飞行 9 帧后开始拖尾：每帧沿弹道回退补 4 颗金色光点（源写裸值 246 = DustID.GoldCoin，照源同一编号）。
        /// </summary>
        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 9f)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 dustPos = Projectile.position;
                    dustPos -= Projectile.velocity * ((float)i * 0.25f);
                    Projectile.alpha = 255;   // 隐形弹幕本体全透明（源写法）
                    int d = Dust.NewDust(dustPos, 1, 1, DustID.GoldCoin, 0f, 0f, 0, default, 1f);
                    Main.dust[d].position = dustPos;
                    Main.dust[d].scale = (float)Main.rand.Next(70, 110) * 0.013f;
                    Main.dust[d].velocity *= 0.2f;
                }
            }
        }
    }
}
