using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 泰拉巨刃的小闪电（移植自大修 TerratomereBolts）：挥砍途中射出的追踪闪电，
    /// 飞行中缓慢加速、追踪 1600 像素内敌人，首次命中后在目标周围生成 3 道大刀光（TerratomereBigSlashs）。
    /// 拖尾用 TrailStreak 着色器 + ScarletDevilStreak 贴图。
    /// </summary>
    internal class TerratomereBolts : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public ref float Hue => ref Projectile.ai[0];
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/TerratomereBolt";
        /// <summary>拖尾缓存 20 点、TrailingMode 2</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        /// <summary>基础属性：30×30、穿透 5、存活 160 帧、不碰撞物块、本地免疫 15 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 160;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }
        /// <summary>朝向速度、缓慢加速；寿命 30~130 帧间平滑转向最近敌人，80 帧后以 32 速度限速追击（16 像素内减速）</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.01f;
            NPC target = Projectile.Center.FindClosestNPC(1600);
            if (target != null && Projectile.timeLeft < 130 && Projectile.timeLeft > 30)
            {
                float toTargetRot = (target.Center - Projectile.Center).ToRotation();
                float diff = MathHelper.WrapAngle(toTargetRot - MathHelper.WrapAngle(Projectile.rotation));
                if (Math.Abs(diff) < MathHelper.Pi)
                    Projectile.rotation += diff * 0.17f;
                else
                    Projectile.rotation -= MathHelper.WrapAngle(-diff) * 0.17f;
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * Projectile.velocity.Length();
            }
            if (target != null && Projectile.timeLeft <= 80)
                Projectile.ChasingBehavior(target.Center, 32);
        }
        /// <summary>首次命中（numHits==1）时在目标周围随机方向生成 3 道大刀光（伤害 ×0.75）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.IsOwnedByLocalPlayer() && Projectile.numHits == 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offsetVr = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * Main.rand.Next(660, 720);
                    Vector2 spanPos = target.Center + offsetVr;
                    Vector2 vr = offsetVr.SafeNormalize(Vector2.UnitY) * -50;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spanPos, vr, ModContent.ProjectileType<TerratomereBigSlashs>(), (int)(Projectile.damage * 0.75f), Projectile.knockBack, Projectile.owner);
                }
            }
        }
        /// <summary>拖尾颜色：Hue 色与淡青绿随时间正弦摆动</summary>
        public Color ColorFunction(float completionRatio, Vector2 _)
        {
            float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos((0f - Main.GlobalTimeWrappedHourly) * 3f) * 0.5f + 0.5f);
            float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio, clamped: true) * Projectile.Opacity;
            Color endColor = Color.Lerp(Main.hslToRgb(Hue, 1f, 0.8f), Color.PaleTurquoise, (float)Math.Sin(completionRatio * MathF.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
        }
        /// <summary>拖尾宽度：随完成度三次方衰减到 0</summary>
        public float WidthFunction(float completionRatio, Vector2 _)
        {
            float expansionCompletion = MathF.Pow(1f - completionRatio, 3.0f);
            return MathHelper.Lerp(0f, 22f * Projectile.scale * Projectile.Opacity, expansionCompletion);
        }
        /// <summary>TrailStreak 着色器 + ScarletDevilStreak 贴图绘制拖尾，再画闪电本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityDemutation:TrailStreak"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (float _, Vector2 _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:TrailStreak"]), 30);
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.5f), Projectile.rotation + MathF.PI / 2f, texture.Size() / 2f, Projectile.scale, 0);
            return false;
        }
    }
}
