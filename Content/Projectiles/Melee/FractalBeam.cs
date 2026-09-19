using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形光矛（FractalBeam，移植自 CalamityEntropy）：苍穹分形刺出式射出的追踪光矛。
    /// 出膛后先边减速边直飞 20 帧，随后每帧朝最近的敌怪（1200 像素内）转向；
    /// 绘制用一颗星星贴图叠出十字形光芒，颜色按弹幕索引奇偶在青色与亮绿之间取一。
    /// <para>
    /// 与 CE 原版的差异：① 贴图占位从 <c>CEUtils.WhiteTexPath</c> 换成本模组 Assets/ExtraTextures/white
    /// （本体完全由 PreDraw 自绘，占位贴图不会被画出来）；② 追踪用的 <c>CEUtils.HomingToNPCNearby</c>
    /// 与 <c>FindTarget_HomingProj</c> 属于 CE 工具库，这里内联；
    /// ③ CE 的 <c>UseAdditive</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、
    /// 画完再恢复默认批次。
    /// </para>
    /// </summary>
    internal class FractalBeam:ModProjectile
    {
        /// <summary>贴图只是占位，本体完全由 PreDraw 自绘</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>绘制用的星星贴图（CE 的 CEExtraAssets.StarTexture）</summary>
        private const string StarTexture = "CalamityDemutation/Assets/ExtraTextures/StarTexture";
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 500;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
        }
        /// <summary>返回 null 表示沿用默认的矩形判定（CE 原样）</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => null;
        public override void CutTiles() { }
        public override void AI()
        {
            Projectile.ai[0]++;
            if (Projectile.ai[0] > 20)                   // 出膛 20 帧后开始追踪
            {
                HomingToNPCNearby(2f, 0.95f, 1200f);
            }
            else
            {
                Projectile.velocity *= 0.95f;
            }
            if (Projectile.timeLeft < 30)                // 最后 30 帧淡出
            {
                Projectile.Opacity -= 1 / 30f;
            }
        }
        /// <summary>自绘：加法混合下用星星贴图叠四层互相垂直的窄条，拼出十字形光芒（后两层完全重合，用来加重中心亮度）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D star = ModContent.Request<Texture2D>(StarTexture).Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            float alpha = Projectile.Opacity;
            float scale = 0.5f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 32);
            Color color = Projectile.whoAmI.GetHashCode() % 2 == 0 ? Color.SkyBlue : Color.LimeGreen * 3;
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, color * alpha, 0f, star.Size() * 0.5f, new Vector2(1, 0.2f) * Projectile.scale * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, color * alpha, 0f, star.Size() * 0.5f, new Vector2(0.2f, 1) * Projectile.scale * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, color * alpha, 0f, star.Size() * 0.5f, new Vector2(0.6f, 0.12f) * Projectile.scale * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, color * alpha, 0f, star.Size() * 0.5f, new Vector2(0.6f, 0.12f) * Projectile.scale * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.HomingToNPCNearby + FindTarget_HomingProj 的内联：朝 maxRadius 内最近的可攻击敌怪转向</summary>
        private void HomingToNPCNearby(float vel, float velMult, float maxRadius)
        {
            NPC target = null;
            float nearest = maxRadius;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || npc.friendly)
                {
                    continue;
                }
                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance <= nearest)
                {
                    nearest = distance;
                    target = npc;
                }
            }
            if (target == null)
            {
                return;
            }
            Projectile.velocity *= velMult;
            Projectile.velocity += Vector2.Normalize(target.Center - Projectile.Center) * vel;
        }
    }
}
