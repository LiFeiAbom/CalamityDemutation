using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 泰拉巨刃的大光束剑气（移植自大修 TerratomereBeams）：收招时放出的一道宽大剑气，
    /// 由手持挥砍体传入 ControlPoints 弧光采样点，用 ExobladeSlash 着色器沿这些点画剑气。
    /// </summary>
    internal class TerratomereBeams : ModProjectile
    {
        /// <summary>弧光采样点（由 TerratomereHoldout 传入）</summary>
        public Vector2[] ControlPoints;
        public bool Flipped => Projectile.ai[0] == 1f;
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>基础属性：60×144、穿透无限、不碰撞物块、存活 30 帧、本地免疫 10 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 144;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        /// <summary>跟随主人速度、透明度随寿命淡入淡出、速度衰减、尺寸膨胀</summary>
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            // 源为 CWRUtils.GetPlayerInstance(owner)（非法或已死亡的玩家返回 null，不跟随其速度）
            if (owner.Alives())
                Projectile.position += owner.velocity;
            Projectile.Opacity = Utils.GetLerpValue(Projectile.localAI[0], 26f, Projectile.timeLeft, clamped: true);
            Projectile.velocity *= 0.91f;
            Projectile.scale *= 1.03f;
        }
        public float SlashWidthFunction(float completionRatio, Vector2 _) => Projectile.scale * 50f;
        /// <summary>剑气颜色：ai[1]==0 时按完成度线性淡出，否则按正弦加亮</summary>
        public Color SlashColorFunction(float completionRatio, Vector2 _)
        {
            if (Projectile.ai[1] == 0)
                return Color.Lime * Utils.GetLerpValue(0.07f, 0.57f, completionRatio, clamped: true) * Projectile.Opacity;
            float sengs = MathF.Sin(completionRatio * MathF.PI);
            if (completionRatio < 0.4f)
                sengs = MathF.Pow(completionRatio, 3) * 13;
            return Color.Lime * sengs * Projectile.Opacity;
        }
        /// <summary>用 ExobladeSlash 着色器沿 ControlPoints 画剑气（画 3 遍加亮）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (ControlPoints == null)
                return false;
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseColor(Terratomere.TerraColor1);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseSecondaryColor(Terratomere.TerraColor2);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Shader.Parameters["fireColor"].SetValue(Terratomere.TerraColor1.ToVector3());
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Shader.Parameters["flipped"].SetValue(Flipped);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Apply();
            List<Vector2> list = new List<Vector2>();
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
        /// <summary>线段碰撞：沿速度方向延伸 scale×130，扫过 -90°~+81° 共 20 条采样线</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            bool collBool = false;
            float point = 0;
            Vector2 starPos = Projectile.Center;
            for (int i = 0; i < 20; i++)
            {
                collBool = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), starPos, Projectile.Center + (Projectile.velocity.SafeNormalize(Vector2.UnitY) * Projectile.scale * 130).RotatedBy(MathHelper.ToRadians(-90 + i * 9)), 32, ref point);
                if (collBool)
                    break;
            }
            return collBool;
        }
    }
}
