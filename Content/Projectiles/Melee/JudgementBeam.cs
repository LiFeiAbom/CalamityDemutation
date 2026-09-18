using CalamityDemutation.Particles;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 审判光束（移植自灾厄大修 0.4.0.1.3 的 JudgementBeam）：制裁大剑的弹幕，取代原版灾厄的白色追踪球。
    /// 贴图色板由自身贴图逐像素提取，随寿命在做多段颜色插值；飞行中喷柔和光粒与宝石尘，
    /// 并在 320 像素内追踪最近敌人。首次命中时播冲击音并朝三个均分方向飞散白球。
    /// </summary>
    internal class JudgementBeam : ModProjectile
    {
        /// <summary>从自身贴图提取的调色板，首次 AI 时填充（供按寿命做颜色插值）</summary>
        public Color[] ProjColorDate;
        /// <summary>
        /// 基础属性：12×12、友方近战、穿透 1、存活 120 帧、不受水减速
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Melee;
        }
        /// <summary>
        /// AI：首次从贴图取色板；朝向对齐速度（+45° 适配贴图）；按剩余寿命在色板内插值取色，
        /// 非服务器端每帧喷 5 颗柔和光粒；每 5 帧沿速度法线两侧各喷 8 颗宝石尘，另每帧喷 10 颗；
        /// 最后在 320 像素内找最近敌人并做缓和追踪。
        /// </summary>
        public override void AI()
        {
            if (ProjColorDate == null)
            {
                ProjColorDate = CDUtil.GetColorDate(CDUtil.GetT2DValue(Texture));
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Color color = CDUtil.MultiStepColorLerp(Projectile.timeLeft / 120f, ProjColorDate);
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 pos = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.Next(6);
                    Vector2 particleSpeed = Projectile.velocity * 0.75f;
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(pos, particleSpeed, Main.rand.NextFloat(0.3f, 0.5f), color, 60, 1f, 1.5f));
                }
            }
            if (Projectile.timeLeft % 5 == 0)
            {
                SpawnGemDust(8, 3);
                SpawnGemDust(8, -3);
            }
            for (int i = 0; i < 10; i++)
            {
                int shinyDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GemDiamond, 0f, 0f, 100, default, 1.25f);
                Main.dust[shinyDust].noGravity = true;
                Main.dust[shinyDust].velocity *= 0.5f;
                Main.dust[shinyDust].velocity += Projectile.velocity * 0.1f;
            }
            NPC target = Projectile.Center.FindClosestNPC(320);
            if (target != null)
            {
                Projectile.ChasingBehavior2(target.Center, 1.01f, 0.15f);
            }
        }
        /// <summary>
        /// 沿速度的法线方向喷一排宝石尘（尘色取当前迪斯科色）：velocityMultiplier 为正/负决定喷向哪一侧
        /// </summary>
        public void SpawnGemDust(int count, float velocityMultiplier)
        {
            for (int i = 0; i < count; i++)
            {
                int shinyDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemDiamond, 0f, 0f, 100, Main.DiscoColor, 2.25f);
                Main.dust[shinyDust].noGravity = true;
                Main.dust[shinyDust].velocity = Projectile.velocity.GetNormalVector() * velocityMultiplier;
                Main.dust[shinyDust].velocity += Projectile.velocity * 0.1f;
            }
        }
        /// <summary>
        /// 命中敌人：仅本次弹幕的首次命中（numHits == 0）播冲击音效，并以随机起始角朝三个均分方向
        /// 各生成一颗白球（伤害为光弹的四分之一、击退沿用本体）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits == 0)
            {
                SoundEngine.PlaySound(SoundID.Item122, Projectile.position);
                float randNum = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vr = (MathHelper.TwoPi / 3f * i + randNum).ToRotationVector2() * 3;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vr, ModContent.ProjectileType<OrderbringerWhiteOrbs>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner);
                }
            }
        }
        /// <summary>
        /// 自绘：按固定白色绘制贴图（颜色不随环境光照变化），返回 false 阻止默认绘制
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = CDUtil.GetT2DValue(Texture);
            Main.EntitySpriteDraw(value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, value.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
