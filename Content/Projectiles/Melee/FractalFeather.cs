using CalamityDemutation.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形之羽（FractalFeather，移植自 CalamityEntropy）：苍穹分形挥砍期间从天而降的羽毛弹。
    /// 飞行时以余弦微幅摆动方向，拖尾分两层绘制——先铺一条金色加法混合的光带，
    /// 再用 ArtAttack 着色器驱动图元渲染器拉出带纹理的丝带；命中后自身伤害递减 20%。
    /// <para>
    /// 与 CE 原版的差异：① 顶点结构从 InnoVault 的 <c>ColoredVertex</c> 换成本模组结构完全相同、
    /// 但按本工程命名与字段布局的 <see cref="VertexPosition2DColorTexture"/>；
    /// ② 图元渲染从 <c>CEPrimitiveRenderer</c> 换成本模组同源的 <see cref="PrimitiveRenderer"/>，
    /// 参数对象 <c>CEPrimitiveSettings</c> → <see cref="PrimitiveSettings"/>（构造签名一一对应）；
    /// ③ ArtAttack 着色器已一并移植到本模组 Effects/ 并注册为 "CalamityDemutation:ArtAttack"；
    /// ④ CE 的 <c>EnterShaderRegion</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin、
    /// 画完再恢复默认批次。
    /// </para>
    /// </summary>
    internal class FractalFeather:ModProjectile
    {
        /// <summary>光带贴图（CE 的 CEExtraAssets.wohslash）</summary>
        private const string SlashTexture = "CalamityDemutation/Assets/ExtraTextures/wohslash";
        /// <summary>ArtAttack 着色器使用的条纹贴图（CE 的 CEExtraAssets.Streak2Asset）</summary>
        private const string StreakTexture = "CalamityDemutation/Assets/ExtraTextures/Streak2";
        /// <summary>拖尾采样点（最多保留 8 个）</summary>
        private readonly List<Vector2> odp = new List<Vector2>();
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;         // 同一个敌人只结算一次
        }
        public override void AI()
        {
            // 采样点取在羽毛尖端前方一点，让拖尾从刀刃位置延伸出去
            odp.Add(Projectile.Center + Projectile.rotation.ToRotationVector2() * 28);
            if (odp.Count > 8)
            {
                odp.RemoveAt(0);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            // 以余弦在飞行方向上做小幅左右摇摆（ai[0] 在此累加，也是本弹幕的摆动相位）
            Projectile.velocity = Projectile.velocity.RotatedBy(Math.Cos(Projectile.ai[0]++ * 0.2f) * 0.025f);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        /// <summary>每命中一个敌人自身伤害递减 20%（CE 原设定）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.damage = (int)(Projectile.damage * 0.8f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, texture.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            DrawTrail(texture);
            return false;
        }
        /// <summary>拖尾颜色：由尾到头按完成比例渐亮（CE 原样）</summary>
        private Color TrailColor(float completionRatio, Vector2 vertex) => Color.White * completionRatio;
        /// <summary>拖尾宽度：由尾到头由 0 渐变到羽毛宽度的 0.6 倍（CE 原样）</summary>
        private float TrailWidth(float completionRatio, Vector2 vertex) => MathHelper.Lerp(0, 22 * 0.6f * Projectile.scale, completionRatio);
        /// <summary>
        /// 分两层画拖尾：先以 wohslash 贴图配合手工三角形带铺一条金色光带，
        /// 再用 ArtAttack 着色器驱动 PrimitiveRenderer 拉出带 Streak2 纹理的丝带，最后把羽毛本体重画一遍盖在最上层。
        /// </summary>
        private void DrawTrail(Texture2D texture)
        {
            // 第一层：金色光带（加法混合）
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (odp.Count > 1)
            {
                List<VertexPosition2DColorTexture> vertices = new List<VertexPosition2DColorTexture>();
                float a = 0;
                for (int i = 1; i < odp.Count; i++)
                {
                    a += 1f / odp.Count;
                    Vector2 normal = (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 6;
                    vertices.Add(new VertexPosition2DColorTexture(odp[i] - Main.screenPosition + normal, Color.Gold * a, new Vector2((float)(i + 1) / odp.Count, 1), 1f));
                    vertices.Add(new VertexPosition2DColorTexture(odp[i] - Main.screenPosition - normal, Color.Gold * a, new Vector2((float)(i + 1) / odp.Count, 0), 1f));
                }
                if (vertices.Count >= 3)
                {
                    GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
                    graphicsDevice.Textures[0] = ModContent.Request<Texture2D>(SlashTexture).Value;
                    graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Count - 2);
                }
            }
            Main.spriteBatch.End();
            // 第二层：ArtAttack 着色器丝带
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            MiscShaderData artAttack = GameShaders.Misc["CalamityDemutation:ArtAttack"];
            artAttack.UseImage1(ModContent.Request<Texture2D>(StreakTexture));// 对应 ArtAttack.fx 的 sampler uImage1
            artAttack.Apply();
            PrimitiveRenderer.RenderTrail(odp, new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, shader: artAttack), 180);
            Main.spriteBatch.End();
            // 本体：换一个不带着色器的批次重画，避免被 ArtAttack 影响
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
