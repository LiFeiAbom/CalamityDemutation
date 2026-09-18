using CalamityDemutation.Particles;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 元素王者之波（移植自灾厄 PrismaticWave）：元素王者之剑蓄力期抛出的彩虹星。
    /// 12 色调色板由生成方经 ai[1] 指定，本体色直接取该色；飞行中拖出彩虹尘，
    /// 并用一颗 <see cref="GenericSparkle"/> 粒子在弹头处**每帧手动跟随**（该粒子寿命被反复清零，随弹幕一同销毁）。
    /// ai[2] == 1 时开启追踪——那是给未移植的 CosmicRainbow 用的分支，本工程当前无人设置该位。
    /// </summary>
    internal class ElementalExcaliburWave : ModProjectile
    {
        /// <summary>彩虹尘的透明度基准（灾厄原值）</summary>
        private int alpha = 50;
        /// <summary>12 色调色板（含 Alpha=50），索引由 ai[1] 决定</summary>
        public Color[] colors =
        [
            new Color(255, 0, 0, 50),
            new Color(255, 128, 0, 50),
            new Color(255, 255, 0, 50),
            new Color(128, 255, 0, 50),
            new Color(0, 255, 0, 50),
            new Color(0, 255, 128, 50),
            new Color(0, 255, 255, 50),
            new Color(0, 128, 255, 50),
            new Color(0, 0, 255, 50),
            new Color(128, 0, 255, 50),
            new Color(255, 0, 255, 50),
            new Color(255, 0, 128, 50)
        ];
        /// <summary>存在帧数（存于 ai[0]）</summary>
        public ref float Timer => ref Projectile.ai[0];
        /// <summary>弹头处那颗常驻闪光粒子；每帧手动跟随并清零寿命</summary>
        public Particle starEffect;
        /// <summary>
        /// 静态属性：邪教徒对其有抗性、残影缓存 10 帧、残影模式 1（圣骑士锤式）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        /// <summary>
        /// 基础属性：36×36、友方近战、初始全透明（靠 AI 逐帧渐显）、存活 360 帧、不撞地形
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.alpha = 255;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
        }
        /// <summary>
        /// AI：计帧、逐帧渐显（下限 64）、按迪斯科色投光、朝向对齐速度、
        /// 1/2 概率喷彩虹尘；首帧随机一个自转角速度，随后创建/跟随弹头闪光粒子；ai[2] == 1 时做追踪。
        /// </summary>
        public override void AI()
        {
            Timer++;
            Projectile.alpha -= 16;
            if (Projectile.alpha < 64)
                Projectile.alpha = 64;
            Lighting.AddLight(Projectile.Center, Main.DiscoR * 0.5f / 255f, Main.DiscoG * 0.5f / 255f, Main.DiscoB * 0.5f / 255f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool())
            {
                int rainbow = Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.RainbowMk2, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, alpha, Main.rand.Next(colors));
                Main.dust[rainbow].noGravity = true;
            }
            // 随机一个自转角速度（正负随机），只在首帧决定一次
            if (Projectile.localAI[0] == 0f)
                Projectile.localAI[0] = Main.rand.NextFloat(MathHelper.Pi / 60f, MathHelper.Pi / 12f) * Main.rand.NextBool().ToDirectionInt();
            // 弹头闪光：不存在则创建，存在则每帧把寿命清零并跟到弹头前缘
            if (starEffect == null)
            {
                Color projColor = Color.Lerp(Color.White, colors[(int)Projectile.ai[1]], 0.4f);
                starEffect = new GenericSparkle(Projectile.Center + Projectile.velocity * 1.5f, Vector2.Zero, projColor, colors[(int)Projectile.ai[1]], Projectile.scale * 2.5f, 2, Timer * Projectile.localAI[0]);
                GeneralParticleHandler.SpawnParticle(starEffect);
            }
            else
            {
                starEffect.Time = 0;
                starEffect.Position = Projectile.Center + Projectile.velocity * 1.5f;
            }
            // 来自 CosmicRainbow 时追踪（本工程未移植该武器，当前无人设置 ai[2] = 1）
            if (Projectile.ai[2] == 1f)
                CDUtil.HomeInOnNPC(Projectile, true, 400f, 18f, 20f);
        }
        /// <summary>自绘：按残影模式 1 绘制残影链（抽稀间隔 2），不画本体贴图</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 2);
            return false;
        }
        /// <summary>本体色直接取调色板里 ai[1] 指定的那一色</summary>
        public override Color? GetAlpha(Color lightColor) => colors[(int)Projectile.ai[1]];
        /// <summary>消亡：先摘掉常驻闪光粒子，再补喷 3 颗彩虹尘</summary>
        public override void OnKill(int timeLeft)
        {
            GeneralParticleHandler.RemoveParticle(starEffect);
            for (int k = 0; k < 3; k++)
            {
                int rainbow = Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.RainbowMk2, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f, alpha, Main.rand.Next(colors));
                Main.dust[rainbow].noGravity = true;
            }
        }
    }
}
