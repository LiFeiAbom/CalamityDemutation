using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形之星（FractalStar，移植自 CalamityEntropy）：星熠分形左键每隔一次挥砍射出的四芒星。
    /// 出手后边自旋边减速；寿命将尽（剩 10 帧）时朝四个正交方向各放一颗 <see cref="FractalStarblight"/> 渊星再自行消散。
    /// 本体不造成伤害（<c>CanHitNPC</c> 恒假），纯演出。
    /// <para>
    /// 与 CE 原版的差异：① 贴图换成本模组已有的 Assets/ExtraTextures/StarTexture（CE 取自它自己的 Assets/Extra/StarTexture，同一张）；
    /// ② <c>CEUtils.randomRot</c> 与 <c>CEUtils.Parabola</c> 在 CE 侧属于工具库，这里内联；
    /// ③ <c>CEUtils.PlaySound("bne_hit", …)</c> 换成本模组 <see cref="CalamityDemutationSounds.bne_hit"/>，
    /// <c>CEUtils.WeapSound</c> 按 1.0 处理；④ 粒子换成 <see cref="GlowSparkCal"/>
    /// （CE 的 <c>PRTLoader.NewParticle&lt;…&gt;(…).Configure(…)</c> 对应本工程「构造 → DRKLoader.NewParticle → Configure」）；
    /// ⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、
    /// 画完再恢复默认批次。
    /// </para>
    /// </summary>
    internal class FractalStar:ModProjectile
    {
        /// <summary>四芒星贴图（CE 的 Assets/Extra/StarTexture，本工程已有同一张）：必须显式覆盖，
        /// 否则 tML 会按类名找 Content/Projectiles/Melee/FractalStar.png 而那个文件并不存在</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/StarTexture";
        /// <summary>是否还没放过分形渊星（一次寿命只放一轮）</summary>
        private bool shoot = true;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透（本体本来就不判定）
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override bool? CanHitNPC(NPC target) => false;
        public override bool? CanCutTiles() => false;
        public override void AI()
        {
            if (Projectile.ai[2]++ == 0)
            {
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * Projectile.velocity.Length() * 0.004f;
            Projectile.velocity *= 0.98f;
            if (Projectile.timeLeft <= 10 && shoot && Main.myPlayer == Projectile.owner)
            {
                shoot = false;
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalStarSplit with { Volume = 0.56f }, Projectile.Center);
                for (int i = 0; i < 360; i += 90)
                {
                    float rot = MathHelper.ToRadians(i) + Projectile.rotation;
                    // 每一路先撒一颗灾厄版发光火花，再射一颗分形渊星
                    GlowSparkCal spark = new GlowSparkCal();
                    DRKLoader.NewParticle(spark, Projectile.Center, rot.ToRotationVector2() * 12, Color.DeepSkyBlue, Projectile.scale * 0.04f);
                    spark.Configure(false, 22, Vector2.One, false, true);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, rot.ToRotationVector2() * 9,
                        ModContent.ProjectileType<FractalStarblight>(), Projectile.damage / 3, Projectile.knockBack / 4, Projectile.owner);
                }
            }
        }
        /// <summary>自绘：加法混合下把星星贴图沿横向与纵向各拉一笔，拼出四芒星（后 20 帧收细、最后 10 帧淡出）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            float w = 0.12f;
            if (Projectile.timeLeft < 20)
            {
                w = Parabola(Projectile.timeLeft / 20f, 1.4f);
                if (Projectile.timeLeft > 10 && w < 0.2f)
                {
                    w = 0.12f;
                }
            }
            float alpha = Projectile.timeLeft < 10 ? Projectile.timeLeft / 10f : 1f;
            Color color = new Color(200, 200, 255) * alpha * Projectile.Opacity;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texture.Size() / 2f, new Vector2(w, 1) * Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texture.Size() / 2f, new Vector2(1, w) * Projectile.scale * 0.8f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.Parabola 的等价实现：开口向下的抛物线，t=0.5 时取到 height</summary>
        private static float Parabola(float t, float height) => 4f * height * t * (1f - t);
    }
}
