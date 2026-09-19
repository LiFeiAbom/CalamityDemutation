using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形激光（FractalLaser，移植自 CalamityEntropy）：虚空分形第七下全屏斩命中时，从目标中心纵向喷出的贯穿光柱。
    /// 长度固定 2000、宽度按 <c>widthAdd</c> 逐帧递增但每次乘 0.98（起手猛地涨开再迅速收住），
    /// 同时以每帧 8% 的力度朝 1 靠拢；寿命剩 16 帧起整体收细。碰撞判定是沿朝向的一条 2000 长线段。
    /// <para>
    /// 与 CE 原版的差异：① 贴图从 CE 的共享白图换成 <c>Assets/ExtraTextures/white</c>，
    /// <c>CEExtraAssets.B1/T2/FLEND</c> 换成本模组 Assets/ExtraTextures 下的同名贴图；
    /// ② CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式
    /// Begin(Immediate, Additive, LinearWrap/LinearClamp)（CE 那边就是这两个采样器），画完再恢复默认批次；
    /// ③ <c>LineThroughRect</c> 用本工程既有的 <see cref="CDUtil.LineThroughRect"/>。
    /// </para>
    /// </summary>
    internal class FractalLaser:ModProjectile
    {
        /// <summary>本体只用一张 1×1 白图当底，形体全由下面三张额外贴图拉出来（CE 的 CEUtils.WhiteTexPath）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>光柱主体：横向滚动采样的噪声贴图（CE 的 CEExtraAssets.B1）</summary>
        private const string CoreNoiseTexture = "CalamityDemutation/Assets/ExtraTextures/B1";
        /// <summary>光柱内芯：不滚动、纯色加的亮芯（CE 的 CEExtraAssets.T2）</summary>
        private const string CoreTexture = "CalamityDemutation/Assets/ExtraTextures/T2";
        /// <summary>光柱两端的光晕端帽（CE 的 CEExtraAssets.FLEND）</summary>
        private const string EndTexture = "CalamityDemutation/Assets/ExtraTextures/FLEND";
        /// <summary>宽度增量，每帧先加到宽度上、再自乘 0.98（于是涨得快、收得也快）</summary>
        private float widthAdd = 0.2f;
        /// <summary>当前宽度系数</summary>
        private float width = 0;
        /// <summary>收尾系数：寿命剩 16 帧起从 1 匀速收到 0</summary>
        private float width2 = 1;
        /// <summary>光柱长度（像素），两端各伸出 1000</summary>
        private int length = 2000;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;   // 长度远超屏幕，放宽上屏裁剪范围
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;                        // 命中判定完全由 Colliding 的线段接管，碰撞箱取最小
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 80;
        }
        public override void AI()
        {
            width += widthAdd;
            widthAdd *= 0.98f;
            width += (1 - width) * 0.08f;
            if (Projectile.timeLeft < 16)
            {
                width2 -= 1 / 16f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 自绘：层 ① 噪声主体（源矩形按全局时间横向滚动，靠 LinearWrap 无缝循环）；
        /// 层 ② 亮芯；层 ③/④ 两端各一顶光晕端帽（用 LinearClamp，端帽外缘不能重复采样）。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            Texture2D coreNoise = ModContent.Request<Texture2D>(CoreNoiseTexture).Value;
            Texture2D core = ModContent.Request<Texture2D>(CoreTexture).Value;
            Texture2D end = ModContent.Request<Texture2D>(EndTexture).Value;
            const float w = 32;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(coreNoise, pos, new Rectangle((int)(Main.GlobalTimeWrappedHourly * -900), 0, coreNoise.Width, coreNoise.Height),
                new Color(160, 160, 255), Projectile.rotation, coreNoise.Size() / 2f, new Vector2(length / (float)coreNoise.Width, w / coreNoise.Height * width * width2), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(core, pos, null, lightColor, Projectile.rotation, core.Size() / 2f, new Vector2(length / (float)core.Width, w / core.Height * width * width2), SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Vector2 rotVec = Projectile.rotation.ToRotationVector2();
            Main.spriteBatch.Draw(end, Projectile.Center + rotVec * length * 0.5f - Main.screenPosition, null, Color.LightBlue, Projectile.rotation,
                new Vector2(0, end.Height / 2f), new Vector2(0.6f, width * width2), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(end, Projectile.Center - rotVec * length * 0.5f - Main.screenPosition, null, Color.LightBlue, Projectile.rotation + MathHelper.Pi,
                new Vector2(0, end.Height / 2f), new Vector2(0.6f, width * width2), SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>命中判定是沿光柱方向、以中心为原点向两端各伸 1000 像素的一条线段</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 rotVec = Projectile.rotation.ToRotationVector2();
            return CDUtil.LineThroughRect(Projectile.Center + rotVec * length * 0.5f, Projectile.Center + rotVec * length * -0.5f, targetHitbox, (int)(40 * width * width2));
        }
    }
}
