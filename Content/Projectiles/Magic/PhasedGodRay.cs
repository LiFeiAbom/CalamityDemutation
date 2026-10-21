using System;
using CalamityDemutation.Content.Items.Weapons.Magic;
using CalamityDemutation.Particles;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 相位神光（Phased God Ray） - 逐行照搬灾厄 **2.1** 的 <c>Projectiles/Magic/PhasedGodRay.cs</c>
    /// （用户 2026-10-15 点名：手持弹幕取 2.0.4、这颗光弹取 2.1 的重做版）。
    /// <para>
    /// 机制：**一个像素都不画**（`PreDraw` 恒 false、源的 `DrawBeam` 那行已被灾厄自己注释掉），
    /// 观感 100% 由粒子拖尾构成——每帧在身后叠两枚 <see cref="CustomSpark"/> 光晕
    /// （一枚偏色、一枚偏白的亮芯），出场那一帧再崩 3 枚 <see cref="CustomSpark"/> 长条火花与 3 颗彩色尘。
    /// 运动是**波动前进**：`ai[0]` 记当前摆动方向（手持弹幕按 ±0.5 传入，两道互相反向 → 交叉缠绕成双螺旋），
    /// 每帧把速度整体旋转 `WaveTheta`（0.09 弧度），摆够 `WaveTwistFrames`（9）帧就换向；
    /// 长度仍走 `localAI[0]` 由 0 涨到 80（`ai[1]` 置 1 时反向缩回、缩到 0 自毁）。
    /// </para>
    /// <para>
    /// **与 2.0.x 版本的实质差别**（也是本件"取 2.1 光弹"的全部意义）：判定由 5×5 放大到 **20×20**、
    /// 穿透 2 → **3**、`localNPCHitCooldown` 12 → **-1（无视无敌帧）**，命中后**回满寿命**并让
    /// **自身伤害每次命中乘 0.865 递减**；命中挂圣火 180 帧（**2.1 有、2.2.2 删了**）。
    /// </para>
    /// <para>
    /// **三处工程侧适配**：① 灾厄 `LightDust` → 命名常量 `DustID.FireworksRGB`（一样吃 `color` 赋值）；
    /// ② 命中音 `<c>CalamityMod/Sounds/Item/MeldShoot</c>` → 本工程自持的
    /// <see cref="CalamityDemutationSounds.MeldShoot"/>；③ 圣火减益走
    /// <see cref="CalamityDemutationPlayer.ApplyCalamityBuffWithFallback"/>（经典灾厄没这个减益，退回原版燃烧）。
    /// 粒子贴图仍取自 `Particles/BloomCircle` 与 `Particles/SemiCircularSmearVerticalBlank` 两张共用素材。
    /// </para>
    /// </summary>
    internal class PhasedGodRay:ModProjectile
    {
        /// <summary>光束长度上限（照源 80）</summary>
        private const float LaserLength = 80f;
        /// <summary>收缩阶段每帧的长度变化量（照源 2）</summary>
        private const float LaserLengthChangeRate = 2f;
        /// <summary>每帧把速度旋转的弧度（照源注释：这几个数是配"波浪数学"的，别乱动）</summary>
        private const float WaveTheta = 0.09f;
        /// <summary>摆动换向所需的帧数（照源 9）</summary>
        private const int WaveTwistFrames = 9;
        /// <summary>当前摆动状态：介于 -1 与 1 之间表示"正在初始化"，正负越大越接近换向</summary>
        private ref float WaveFrameState => ref Projectile.ai[0];
        /// <summary>光束条贴图走工程共用的 `LaserProjectile`（照源同样借灾厄的共用贴图）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>留 10 帧残影的历史旋转（波动初始化时要回填，避免开头几帧闪一下）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }
        /// <summary>
        /// 判定 20×20、穿透 3、**无视无敌帧**（`localNPCHitCooldown = -1`）、额外更新 3 帧、寿命 280 帧、
        /// 穿墙穿水（全照 2.1；2.0.x 是 5×5 / 穿透 2 / 局部冷却 12）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = 280;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        /// <summary>淡入 + 波动前进 + 长度伸缩 + 粒子拖尾（照源，一行不动）</summary>
        public override void AI()
        {
            // 极快地淡入
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 25;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }

            float waveSign = WaveFrameState < 0f ? -1f : 1f;

            // 初始化摆动：|ai[0]| 小于 1 说明还在起步，此时确定往哪边摆（手持弹幕会传 ±0.5 指定方向）。
            // 传进来的正好是 0 就抛硬币。
            if (Math.Abs(WaveFrameState) < 1f)
            {
                float dirToUse = WaveFrameState == 0f ? (Main.rand.NextBool() ? -1f : 1f) : waveSign;
                waveSign = -dirToUse;
                WaveFrameState = dirToUse * WaveTwistFrames * 0.5f;

                // 回填历史旋转，防止开头几帧的残影乱甩
                float iterRotation = Projectile.velocity.ToRotation();
                for (int i = 0; i < Projectile.oldRot.Length; ++i)
                {
                    Projectile.oldRot[i] = iterRotation;
                    iterRotation += waveSign * WaveTheta;
                }
            }
            // 摆够了就换向
            else if (Math.Abs(WaveFrameState) > WaveTwistFrames)
            {
                WaveFrameState = -waveSign;
            }
            else
            {
                WaveFrameState += waveSign;
            }

            // 每帧把速度整体转一点，形成持续快速的波浪
            Projectile.velocity = Projectile.velocity.RotatedBy(waveSign * WaveTheta);
            Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, 0.87f, 0.65f, 0.1725f);

            // 长度伸缩：localAI[0] 是当前长度，ai[1] 为 0 表示还在变长、为 1 表示正在消散
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += 10f;
                if (Projectile.localAI[0] > LaserLength)
                {
                    Projectile.localAI[0] = LaserLength;
                }
            }
            else
            {
                Projectile.localAI[0] -= LaserLengthChangeRate;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                }
            }

            // 每帧两枚光晕粒子：外圈偏色 + 内芯偏白（fadeInLerp 让出场瞬间更大更圆）
            float fadeInLerp = Utils.GetLerpValue(280, 265, Projectile.timeLeft, true);
            Color beamColor = Color.Lerp(AetherfluxCannon.accentColor, AetherfluxCannon.mainColor, fadeInLerp);
            Color subColor = Color.Lerp(AetherfluxCannon.accentColor, AetherfluxCannon.mainColor, Main.rand.NextFloat());

            GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, -Projectile.velocity * 0.05f, "CalamityDemutation/Particles/BloomCircle", false, 7, 0.65f - 0.3f * fadeInLerp, beamColor * 0.8f, new Vector2(1.1f - 0.4f * fadeInLerp, 0.8f + 0.6f * fadeInLerp), true, false, shrinkSpeed: 0.6f));
            GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, -Projectile.velocity * 0.05f, "CalamityDemutation/Particles/BloomCircle", false, 5, 0.35f - 0.15f * fadeInLerp, Color.Lerp(beamColor, Color.White, 0.6f), new Vector2(0.7f, 1.4f), true, false, shrinkSpeed: 0.6f));

            // 出场那一帧（timeLeft 正好满 280）额外崩 3 枚长条火花 + 3 颗彩色尘
            if (Projectile.timeLeft == 280)
            {
                for (int i = 0; i < 3; i++)
                {
                    subColor = Color.Lerp(AetherfluxCannon.accentColor, AetherfluxCannon.mainColor, Main.rand.NextFloat());
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center - Projectile.velocity * 2, Projectile.velocity.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.5f, 0.7f), "CalamityDemutation/Particles/SemiCircularSmearVerticalBlank", false, 10, 0.45f, subColor, new Vector2(0.8f, 1.2f), true, false, shrinkSpeed: 0.5f));

                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB);
                    dust2.scale = Main.rand.NextFloat(0.6f, 1.4f);
                    dust2.noGravity = true;
                    dust2.velocity = Projectile.velocity.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.3f, 0.8f);
                    dust2.color = subColor;
                }
            }
        }
        /// <summary>
        /// 命中（照 2.1）：挂圣火 180 帧 + 叠两枚光晕粒子 + **回满寿命**（于是它还能继续飞/继续打）
        /// + 播一声 `MeldShoot` + **自身伤害乘 0.865 递减**。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "HolyFlames", 180, BuffID.OnFire);
            for (int i = 0; i < 2; i++)
            {
                Color color = AetherfluxCannon.accentColor;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Vector2.Zero, "CalamityDemutation/Particles/BloomCircle", false, 20, Main.rand.NextFloat(0.15f, 0.25f) * 3, color, new Vector2(1f, 1f), true, false));
            }
            Projectile.timeLeft = 280;
            SoundEngine.PlaySound(CalamityDemutationSounds.MeldShoot with { Volume = 0.4f, Pitch = Main.rand.NextFloat(0.5f, 0.65f) }, Projectile.Center);
            if (Projectile.damage > 1)
            {
                Projectile.damage = (int)(Projectile.damage * 0.865f);
            }
        }
        /// <summary>打玩家同样挂圣火 180 帧（照 2.1）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "HolyFlames", 180, BuffID.OnFire);
        /// <summary>本体颜色（照源 <c>(222, 166, 44, 0)</c>；本件其实不画贴图，只为对照留档）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(222, 166, 44, 0);
        /// <summary>不画任何东西（照 2.1：源的 `DrawBeam` 那行已被灾厄自己注释掉，观感全靠粒子）</summary>
        public override bool PreDraw(ref Color lightColor) => false;
        /// <summary>消散时朝前方崩 4 枚变速度火花（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 4; i++)
            {
                Color subColor = Color.Lerp(AetherfluxCannon.accentColor, AetherfluxCannon.mainColor, Main.rand.NextFloat());
                float rot = Main.rand.NextFloat(-0.1f, 0.1f);
                Vector2 startVel = (Projectile.velocity * 0.1f).RotatedBy(rot);
                Vector2 endVel = startVel.RotatedBy(rot * 5) * 20;
                GeneralParticleHandler.SpawnParticle(new VelChangingSpark(Projectile.Center, startVel, endVel, "CalamityDemutation/Particles/BloomCircle", Main.rand.Next(15, 21), Main.rand.NextFloat(0.015f, 0.025f) * 15, subColor, new Vector2(1.1f, 1f), true, false, lerpRate: 0.015f, shrinkSpeed: 0.4f));
            }
        }
    }
}
