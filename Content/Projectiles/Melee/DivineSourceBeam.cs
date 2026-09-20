using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 金源灭却刃的剑气（移植自大修 DivineSourceBeam）：挥舞时放出的宽大剑气，
    /// 旋转角以「度」存储（初始 -160/90、每帧 -5），画弧线时再转弧度采样 30 个点，
    /// 用 ExobladeSlash 着色器沿弧线画剑气。
    /// </summary>
    internal class DivineSourceBeam : ModProjectile
    {
        public Vector2[] ControlPoints;
        private const float AtoR = MathHelper.Pi / 180f;
        private const float EndRot = 60 * AtoR;
        private const float StarRot = -170 * AtoR;
        private const float LEndRot = -240 * AtoR;
        private const float LStarRot = -10 * AtoR;
        public bool Flipped => Projectile.ai[0] == 1f;
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>基础属性：60×144、真近战（MeleeNoSpeed）、穿透无限、不碰撞物块、存活 30 帧、本地免疫 -1</summary>
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 144;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        /// <summary>生成时以「度」定初始旋转角（右 -160 / 左 90）</summary>
        public override void OnSpawn(IEntitySource source) => Projectile.rotation = Projectile.velocity.X > 0 ? -160 : 90;
        /// <summary>按方向采样 30 个弧光点（把「度」转弧度后从末角插值到始角，再按速度方向取反）</summary>
        public IEnumerable<Vector2> GenerateSlashPoints(bool dir)
        {
            float starRot = StarRot;
            float endRot = EndRot;
            if (dir)
            {
                starRot = LStarRot;
                endRot = LEndRot + 30 * AtoR;
            }
            for (int i = 0; i < 30; i++)
            {
                float completion = MathHelper.Lerp(endRot + Projectile.rotation * AtoR, starRot + Projectile.rotation * AtoR, i / 30f);
                completion *= Math.Sign(Projectile.velocity.X) * -1;
                yield return completion.ToRotationVector2() * 84f;
            }
        }
        /// <summary>跟随主人速度、透明度随寿命淡入淡出、速度衰减、尺寸膨胀、旋转角（度）每帧 -5</summary>
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            // 源为 CWRUtils.GetPlayerInstance(owner)（非法或已死亡的玩家返回 null，不跟随其速度）
            if (owner.Alives())
                Projectile.position += owner.velocity;
            Projectile.Opacity = Utils.GetLerpValue(Projectile.localAI[0], 26f, Projectile.timeLeft, clamped: true);
            Projectile.velocity *= 0.91f;
            Projectile.scale *= 1.03f;
            Projectile.rotation -= 5f;
        }
        public float SlashWidthFunction(float completionRatio, Vector2 _) => Projectile.scale * 50f;
        /// <summary>剑气颜色：按完成度正弦加亮（前 40% 三次方抬升）</summary>
        public Color SlashColorFunction(float completionRatio, Vector2 _)
        {
            float sengs = MathF.Sin(completionRatio * MathF.PI);
            if (completionRatio < 0.4f)
                sengs = MathF.Pow(completionRatio, 3) * 13;
            return Color.Lime * sengs * Projectile.Opacity;
        }
        /// <summary>用 ExobladeSlash 着色器沿弧线采样点画剑气（画 3 遍加亮）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseColor(Terratomere.TerraColor1);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseSecondaryColor(Terratomere.TerraColor2);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Shader.Parameters["fireColor"].SetValue(Terratomere.TerraColor1.ToVector3());
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Shader.Parameters["flipped"].SetValue(Flipped);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Apply();
            List<Vector2> list = new List<Vector2>();
            ControlPoints = GenerateSlashPoints(Projectile.velocity.X < 0).ToArray();
            for (int i = 0; i < ControlPoints.Length; i++)
                list.Add(ControlPoints[i] + ControlPoints[i].SafeNormalize(Vector2.Zero) * (Projectile.scale - 1f) * 70f);
            // 本机 tML 无灾厄的 EnterShaderRegion/ExitShaderRegion，改用 End + 立即模式 Begin 替代，画完恢复默认批状态
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int j = 0; j < 3; j++)
                PrimitiveRenderer.RenderTrail(list, new PrimitiveSettings(SlashWidthFunction, SlashColorFunction, (float _, Vector2 _) => Projectile.Center, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:ExobladeSlash"]), 65);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>线段碰撞：沿速度方向延伸 scale×120，扫过 -160°~+49° 共 20 条采样线</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            bool collBool = false;
            float point = 0;
            Vector2 starPos = Projectile.Center;
            for (int i = 0; i < 20; i++)
            {
                Vector2 endPos = Projectile.Center + MathHelper.ToRadians(-160 + 11 * i).ToRotationVector2() * Projectile.scale * 120;
                if (Projectile.velocity.X < 0)
                    endPos = Projectile.Center + MathHelper.ToRadians(20 - 11 * i).ToRotationVector2() * Projectile.scale * 120;
                collBool = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), starPos, endPos, 32, ref point);
                if (collBool)
                    break;
            }
            return collBool;
        }
    }
}
