using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形光星（FractalBlight，移植自 CalamityEntropy）：光辉分形挥砍中段甩出的追踪小星。
    /// 全程朝 1200 像素内最近的敌怪加速转向，最后 60 帧淡出；拖尾用 ArtAttack 着色器拉出带条纹的丝带。
    /// <para>
    /// 与 CE 原版的差异：① 索敌沿用 tML 自带的 <c>Projectile.FindTargetWithinRange</c>（CE 调的就是同一个 API），
    /// 不需要额外内联；② <c>CEUtils.normalize</c> 等价为 <c>SafeNormalize</c>；③ 拖尾贴图经 CE 的
    /// <c>SetShaderTexture</c> 落在着色器的 uImage1，本机没有该扩展方法，改用 <c>UseImage1</c>
    /// （ArtAttack.fx 只采样 uImage1，uImage0 声明未用）；④ CE 的 <c>EnterShaderRegion</c>/<c>ExitShaderRegion</c>
    /// 本机 tML 没有，改用 End + 立即模式 Begin(AlphaBlend)、画完再恢复默认批次；
    /// ⑤ CE 里那一笔位置修正乘了恒为 0 的 num166，已省略；⑥ 减益不新建——现代版灾厄取 <c>ElementalMix</c>，
    /// 经典版灾厄没有该 buff，按经典版元素系武器（ElementalShortsword）自身口径以
    /// <c>HolyLight</c> + <c>GlacialState</c> + <c>BrimstoneFlames</c> + <c>Plague</c> 等效。
    /// </para>
    /// </summary>
    internal class FractalBlight:ModProjectile
    {
        /// <summary>ArtAttack 着色器拖尾使用的条纹贴图（CE 的 CEExtraAssets.SylvestaffStreakAsset）</summary>
        private const string StreakTexture = "CalamityDemutation/Assets/ExtraTextures/SylvestaffStreak";
        /// <summary>当前锁定的追踪目标（未锁定或目标失效时为 null）</summary>
        private NPC homing = null;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;         // 同时记录 oldPos 与 oldRot，供拖尾重绘
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            if (homing == null)
            {
                homing = Projectile.FindTargetWithinRange(1200f);
            }
            else if (!homing.active)
            {
                homing = null;
            }
            if (homing != null)
            {
                // 朝目标加速并限速：每帧先叠一个朝向分量，再整体衰减
                Projectile.velocity += (homing.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 1.6f;
                Projectile.velocity *= 0.96f;
            }
            if (Projectile.timeLeft < 60)
            {
                Projectile.Opacity -= 1 / 60f;
            }
        }
        /// <summary>命中敌人：ai[1]==1（元素分形那一路）时附加元素减益，其余情况不挂任何减益</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[1] == 1)
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 400);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 400);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GlacialState", 400);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", 400);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", 400);
            }
        }
        /// <summary>拖尾颜色：白→金（ai[1]==1 时为粉红），并随完成度与淡出透明度衰减</summary>
        private Color TrailColor(float completionRatio, Vector2 vertex)
        {
            Color color = (Projectile.ai[1] == 1 ? Color.Lerp(new Color(255, 120, 130), new Color(255, 160, 170), completionRatio) : Color.Lerp(Color.White, Color.Gold, completionRatio)) * (1 - completionRatio) * Projectile.Opacity;
            return color;
        }
        /// <summary>拖尾宽度：中段最粗（8→20 平滑插值），两端收细</summary>
        private float TrailWidth(float completionRatio, Vector2 vertex)
        {
            float widthInterpolant = Utils.GetLerpValue(0f, 0.25f, completionRatio, true) * Utils.GetLerpValue(1.1f, 0.7f, completionRatio, true);
            return MathHelper.SmoothStep(8f, 20f, widthInterpolant);
        }
        /// <summary>
        /// 自绘：先用 ArtAttack 着色器（条纹贴图走 uImage1）拉一条丝带拖尾，
        /// 再用同一张星图叠四笔十字光条当作星体，最后把批次恢复成默认状态。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            MiscShaderData artAttack = GameShaders.Misc["CalamityDemutation:ArtAttack"];
            artAttack.UseImage1(ModContent.Request<Texture2D>(StreakTexture));// 对应 ArtAttack.fx 的 sampler uImage1
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            artAttack.Apply();
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Projectile.Size * 0.5f, shader: artAttack), 180);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Color color = Projectile.ai[1] == 1 ? new Color(255, 160, 185) : Color.LightGoldenrodYellow;
            color.A = 0;                                     // CE 原样：把 alpha 清零，只留 RGB 给加法混合提亮
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Vector2 origin = texture.Size() / 2f;
            // num165 是一个随时间脉动的亮度系数，同时兼职放大与整体提亮
            float num165 = 18 * (1f + 0.2f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * MathHelper.TwoPi * 3f)) * 0.8f;
            Color color36 = color * num165;
            Color color37 = color * 0.5f * num165;
            Vector2 vector31 = new Vector2(0.5f, 1.4f) * num165 * 0.02f;
            Vector2 vector32 = new Vector2(0.5f, 1f) * num165 * 0.02f;
            Main.EntitySpriteDraw(texture, drawPos, null, color36 * Projectile.Opacity, MathHelper.PiOver2, origin, vector31, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, color36 * Projectile.Opacity, 0f, origin, vector32, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, color37 * Projectile.Opacity, MathHelper.PiOver2, origin, vector31 * 0.6f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, color37 * Projectile.Opacity, 0f, origin, vector32 * 0.6f, SpriteEffects.None);
            return false;
        }
    }
}
