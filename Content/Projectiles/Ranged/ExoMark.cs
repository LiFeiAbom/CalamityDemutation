using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 星流印记（ExoMark） - 绿色星流箭消亡时留在原地的那枚印记，＝CI 的
    /// <c>Content/Projectiles/Ranged/ExoMark.cs</c>（与 2.0 / 经典版逐行一致）。
    /// <para>
    /// 时间轴由一个 <c>localAI[1]</c> 计数器驱动（<c>timeLeft</c> 的 900 帧只是兜底）：
    /// 0~30 帧朝四周洒尘、33 帧内淡入、**第 60 帧在自身位置放出 <see cref="ExoTornado"/>**、
    /// 103 帧后淡出、120 帧自毁。判定 14×14、穿透 -1、命中冷却用的是**静态**冷却
    /// （<c>usesIDStaticNPCImmunity</c> / 9 帧，同类型弹幕共享）。
    /// </para>
    /// <para>
    /// 源自带的 <c>PreDraw</c> 画的是那张 1×1 全透明的占位贴图（`Projectiles/InvisibleProj`），
    /// 等价于什么都不画——其观感全来自尘与随后生成的龙卷，故本工程**不另写绘制**，直接走默认（同样不可见）。
    /// </para>
    /// </summary>
    internal class ExoMark : ModProjectile
    {
        /// <summary>存活第几帧放出龙卷（照源：60）</summary>
        private const float TornadoSpawnTime = 60f;
        /// <summary>自毁帧（照源：120）</summary>
        private const float Lifespan = 120f;
        /// <summary>共用的隐形占位贴图（工程里八个隐形弹幕都引这一张，见 AGENTS 第 4 节）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>基础属性：14×14、不撞地形、无限穿透、寿命 900 帧（实际 120 帧自毁）、静态命中冷却 9 帧（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 900;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 9;
        }
        /// <summary>AI：见类型注释里的时间轴；全部照源，仅尘的编号换成命名常量</summary>
        public override void AI()
        {
            Color markColor = Color.White;

            // 登场音（照源：Item60，只播一次）
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = -1;
                SoundEngine.PlaySound(SoundID.Item60, Projectile.position);
            }

            // 0~30 帧：从上方斜向抛出一批尘，模拟"落地扬尘"
            if (Projectile.localAI[1] < 30f)
            {
                float amount = Main.rand.NextFloat();
                Vector2 scatter = new Vector2(MathHelper.Lerp(0.1f, 1f, Main.rand.NextFloat()), MathHelper.Lerp(-0.5f, 0.9f, amount));
                scatter.X *= MathHelper.Lerp(2.2f, 0.6f, amount);
                scatter.X *= -1f;
                Vector2 pinch = new Vector2(2f, 10f);
                Vector2 dustPos = Projectile.Center + new Vector2(60f, 200f) * scatter * 0.5f + pinch;
                Dust dust = Main.dust[Dust.NewDust(dustPos, 0, 0, DustID.TerraBlade, 0f, 0f, 0, default, 0.5f)];
                dust.position = dustPos;
                dust.customData = Projectile.Center + pinch;
                dust.fadeIn = 1f;
                dust.scale = 0.3f;
                if (scatter.X > -1.2f)
                    dust.velocity.X = 1f + Main.rand.NextFloat();
                dust.velocity.Y = Main.rand.NextFloat() * -0.5f - 1f;
            }

            // 首帧：对齐到所在瓦片中心（让印记不悬在格子缝上），并把 scale 起手值定为 0.8
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 0.8f;
                Projectile.direction = 1;
                Point tilePos = Projectile.Center.ToTileCoordinates();
                Projectile.Center = new Vector2(tilePos.X * 16 + 8, tilePos.Y * 16 + 8);
            }

            // 自转：每 40 帧一圈
            Projectile.rotation = Projectile.localAI[1] / 40f * MathHelper.TwoPi * Projectile.direction;

            // 33 帧内淡入、103 帧后淡出（alpha 255 起，8 帧 / 16 帧一档）
            if (Projectile.localAI[1] < 33f)
            {
                if (Projectile.alpha > 0)
                    Projectile.alpha -= 8;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }
            if (Projectile.localAI[1] > 103f)
            {
                if (Projectile.alpha < 255)
                    Projectile.alpha += 16;
                if (Projectile.alpha > 255)
                    Projectile.alpha = 255;
            }
            if (Projectile.alpha == 0)
                Lighting.AddLight(Projectile.Center, markColor.ToVector3() * 0.5f);

            // 两组对称尘（各 2 个方向、各 1/10 概率）
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextBool(10))
                {
                    Vector2 velocity = Vector2.UnitY.RotatedBy(i * MathHelper.Pi).RotatedBy(Projectile.rotation);
                    Dust dust = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.TerraBlade, 0f, 0f, 225, markColor, 1f)];
                    dust.noGravity = true;
                    dust.noLight = true;
                    dust.scale = Projectile.Opacity * Projectile.localAI[0];
                    dust.position = Projectile.Center;
                    dust.velocity = velocity * 2.5f;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextBool(10))
                {
                    Vector2 velocity = Vector2.UnitY.RotatedBy(i * MathHelper.Pi);
                    Dust dust = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.TerraBlade, 0f, 0f, 225, markColor, 1.5f)];
                    dust.noGravity = true;
                    dust.noLight = true;
                    dust.scale = Projectile.Opacity * Projectile.localAI[0];
                    dust.position = Projectile.Center;
                    dust.velocity = velocity * 2.5f;
                }
            }

            // 淡入段与淡出段之外保持固定缩放（中段由 dust 的 scale 表现膨胀）
            if (Projectile.localAI[1] < 33f || Projectile.localAI[1] > 87f)
                Projectile.scale = Projectile.Opacity / 2f * Projectile.localAI[0];

            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[1] += 1f;

            if (Projectile.localAI[1] == TornadoSpawnTime && Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<ExoTornado>(), Projectile.damage, 2f, Projectile.owner, 1f, 0f);
            }
            if (Projectile.localAI[1] >= Lifespan)
                Projectile.Kill();
        }
    }
}
