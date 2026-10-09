using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 宇宙灯笼甩出的**宇宙光束**（照灾厄 2.0.3.9 <c>Projectiles/Summon/CosmilampBeam.cs</c> 移植）：
    /// 16×16 判定、`MaxUpdates = 3`、穿透 3、逐敌 45 帧独立冷却、寿命 120 帧（会再乘 3 次更新）；
    /// 前 45 帧只减速"蓄势"，之后高速追踪 1360 像素内的敌人；
    /// 贴到 160 像素以内改成绕着目标转（转速随目标体积放大、上限 π×0.05），做出"切削"的观感。
    /// </summary>
    /// <remarks>
    /// 贴图隐形，画面全靠拖尾：源用灾厄的 `ImpFlameTrail` 着色器 + `ExtraTextures/Trails/ScarletDevilStreak` 贴图，
    /// 本工程改用自家既有等价件——`CalamityDemutation:TrailStreak` 着色器（`Effects/FadedUVMapStreak.fx`）+ 同一张
    /// `ExtraTextures/Trails/ScarletDevilStreak`，经 <see cref="PrimitiveRenderer"/> 绘制（与泰拉巨刃小闪电同一套写法）。
    /// </remarks>
    internal class CosmilampBeam:ModProjectile
    {
        /// <summary>贴图引工程共用的隐形占位图（源也走隐形图，画面全靠拖尾）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>蓄势时长（帧，源值 45）</summary>
        public const int SlowdownTime = 45;
        /// <summary>基准寿命（帧，源值 120；实际 timeLeft 再乘 MaxUpdates）</summary>
        public const int Lifetime = 120;
        /// <summary>自身计时器（`ai[0]`，照源）</summary>
        public ref float Timer => ref Projectile.ai[0];

        /// <summary>属于仆从弹药（MinionShot）；残影 32 格、模式 2（照源）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 32;
        }
        /// <summary>基础属性：照源（16×16、穿水、不撞地形、穿透 3、MaxUpdates 3、逐敌 45 帧、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 3;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 15;
        }
        /// <summary>蓄势减速 → 高速追踪 → 贴近后绕圈"切削"（照源）</summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.2f, 0.01f, 0.1f);
            Projectile.Opacity = Utils.GetLerpValue(0f, Projectile.MaxUpdates * 10f, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, Vector3.One * Projectile.Opacity * 0.7f);

            NPC potentialTarget = Projectile.Center.MinionHoming(Cosmilamp.MaxTargetingDistance, Main.player[Projectile.owner]);

            // 前 45 帧只是慢慢减速（蓄势）
            if (Timer < SlowdownTime)
                Projectile.velocity *= 0.995f;
            // 之后以极高速扑向附近目标
            else if (potentialTarget is not null)
            {
                Vector2 idealVelocity = Projectile.SafeDirectionTo(potentialTarget.Center) * Cosmilamp.BeamHomeSpeed;
                // 离得还远（>160 像素）就一边转向一边加速追
                if (Vector2.Distance(Projectile.Center, potentialTarget.Center) > 160f)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.036f);
                    Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(idealVelocity.ToRotation(), 0.12f).ToRotationVector2() * Projectile.velocity.Length();
                }
                // 贴到 160 以内就绕着转（切削感）；转速随目标体积放大、上限 π×0.05，免得绕过头（照源）
                else
                {
                    float angularTurnSpeed = MathHelper.Pi * potentialTarget.Size.Length() / 12000f;
                    if (angularTurnSpeed > MathHelper.Pi * 0.05f)
                        angularTurnSpeed = MathHelper.Pi * 0.05f;

                    Projectile.velocity = Projectile.velocity.RotatedBy(angularTurnSpeed);
                }
            }

            // 源用灾厄扩展 Projectile.FinalExtraUpdate()（每帧只算一次），本机 tML 没有这个扩展
            // （AGENTS.md 第 9 节记过），等价写法是 numUpdates == 0 —— 同样每帧只进一次
            if (Projectile.numUpdates == 0)
                Timer++;
        }
        /// <summary>拖尾颜色：品红→暗紫循环，再经 MulticolorLerp 混入中紫与红；内层（`localAI[0] == 1`）更白更细</summary>
        private Color ColorFunction(float completionRatio, Vector2 _)
        {
            float streakOpacity = Utils.GetLerpValue(0.8f, 0.54f, completionRatio, true) * Projectile.Opacity;
            Color endColor = Color.Lerp(Color.Fuchsia, Color.DarkViolet, (float)Math.Sin(completionRatio * MathHelper.Pi * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            endColor = CDUtil.MulticolorLerp(completionRatio * completionRatio, endColor, Color.MediumPurple, Color.Red);
            if (Projectile.localAI[0] == 1f)
                endColor = Color.Lerp(endColor, Color.White, 0.5f) * streakOpacity;

            return Color.Lerp(endColor, Color.Black, 0.3f) * streakOpacity;
        }
        /// <summary>拖尾宽度：前 30% 完成度内展开到 `宽 × 1.65`；内层再乘 0.4（照源）</summary>
        private float WidthFunction(float completionRatio, Vector2 _)
        {
            float expansionCompletion = 1f - (float)Math.Pow(1f - Utils.GetLerpValue(0f, 0.3f, completionRatio, true), 2D);
            float maxWidth = Projectile.Opacity * Projectile.width * 1.65f;
            if (Projectile.localAI[0] == 1f)
                maxWidth *= 0.4f;

            return MathHelper.Lerp(0f, maxWidth, expansionCompletion);
        }
        /// <summary>先用外层参数画一遍、再用内层参数（更白更细）叠一遍（照源的两趟画法）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityDemutation:TrailStreak"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/Trails/ScarletDevilStreak"));

            Projectile.localAI[0] = 0f;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (float _, Vector2 _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:TrailStreak"]), 42);

            Projectile.localAI[0] = 1f;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (float _, Vector2 _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:TrailStreak"]), 42);
            return false;
        }
    }
}
