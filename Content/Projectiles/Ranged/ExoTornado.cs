using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 星流风暴（ExoTornado） - 由 <see cref="ExoMark"/> 放出的贯穿地形竖直龙卷，＝旧版支线的
    /// <c>Content/Projectiles/Ranged/ExoTornado.cs</c>。
    /// <para>
    /// 时间轴由两个计数器分担：<c>ai[0]</c> 是存活计时（上限 <see cref="TornadoLifespan"/> 帧，AI 内自管，
    /// 与 <c>timeLeft</c> 的 240 帧无关，起身值 1 由印记生成时传入）；<c>localAI[0]</c> 是"前
    /// <see cref="DamageDuration"/> 帧有伤害"的威力计时（满帧后 <c>damage</c> 置 0）。
    /// 一旦伤害归零或龙卷看不到主人，<c>ai[0]</c> 会被"对齐"到收尾段（<c>TornadoLifespan - 120</c> 起），
    /// 让它自然卷完再消失。
    /// </para>
    /// <para>
    /// **旧版支线相对 2.0 改小过参数**：寿命 900 → 165、纵向取样范围 15 → 30（源码注释里留着这两个旧值）。
    /// **本工程补的一处上游缺口**：源（旧版支线 / 经典版 / 2.0 三处都一样）只**读**从不**写** <c>localAI[0]</c>，
    /// 于是"30 帧后伤害归零"永远不触发；照工程对同一段源代码在 `SandTornado` 上的既有处理，
    /// 这里补上每帧自增，让该计时真正生效。
    /// </para>
    /// </summary>
    internal class ExoTornado : ModProjectile
    {
        /// <summary>存活计时上限（照旧版支线：165）</summary>
        private const float TornadoLifespan = 165f;
        /// <summary>有伤害的帧数（照源：前 30 帧）</summary>
        private const int DamageDuration = 30;
        /// <summary>纵向取样范围（照旧版支线：上下各 30 格）</summary>
        private const int VerticalSampleRange = 30;
        /// <summary>龙卷的横向收紧系数（照源 <c>0.2</c>）</summary>
        private const float WidthFactor = 0.2f;
        /// <summary>复用工程内的通用龙卷贴图（与沙之娘的沙尘龙卷同一张，60×60）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/TornadoProj";
        /// <summary>
        /// 基础属性：10×10 的初始判定（每帧由 AI 按地形重算成贯穿地形的长条）、不撞地形、
        /// 无限穿透、寿命 240 帧、静态命中冷却 10 帧（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }
        /// <summary>AI：登场音 + 存活计时 + 威力计时 + 每帧把判定重算成"自立足点起贯穿地形的长条"</summary>
        public override void AI()
        {
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = -1;
                SoundEngine.PlaySound(SoundID.Item122, Projectile.position);
            }

            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= TornadoLifespan)
                Projectile.Kill();

            // 威力计时（源只读不写，本工程补上自增，见类型注释）
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= DamageDuration)
            {
                Projectile.damage = 0;
                // 伤害归零后把存活计时对齐到收尾段，保持"卷完再消失"的节奏
                if (Projectile.ai[0] < TornadoLifespan - 120f)
                {
                    Projectile.ai[0] = TornadoLifespan - 120f + Projectile.ai[0] % 60f;
                    Projectile.netUpdate = true;
                }
            }

            // 按地形把判定重算成一条贯穿的竖直长条
            Point tilePos = Projectile.Center.ToTileCoordinates();
            Collision.ExpandVertically(tilePos.X, tilePos.Y, out int topTile, out int bottomTile, VerticalSampleRange, VerticalSampleRange);
            topTile++;
            bottomTile--;
            Vector2 top = new Vector2(tilePos.X, topTile) * 16f + new Vector2(8f);
            Vector2 bottom = new Vector2(tilePos.X, bottomTile) * 16f + new Vector2(8f);
            Projectile.Center = Vector2.Lerp(top, bottom, 0.5f);
            float verticalSpan = bottom.Y - top.Y;
            Projectile.width = (int)(verticalSpan * WidthFactor * 0.65f);
            Projectile.height = (int)verticalSpan;

            // 看不到主人（中间被地形挡死）时，同样把存活计时对齐到收尾段
            if (Projectile.owner == Main.myPlayer)
            {
                bool canSeeOwner = false;
                Vector2 ownerCenter = Main.player[Projectile.owner].Center;
                Vector2 ownerTop = Main.player[Projectile.owner].Top;
                for (float lerp = 0f; lerp < 1f; lerp += 0.05f)
                {
                    Vector2 checkPoint = Vector2.Lerp(top, bottom, lerp);
                    if (Collision.CanHitLine(checkPoint, 0, 0, ownerCenter, 0, 0) || Collision.CanHitLine(checkPoint, 0, 0, ownerTop, 0, 0))
                    {
                        canSeeOwner = true;
                        break;
                    }
                }
                if (!canSeeOwner && Projectile.ai[0] < TornadoLifespan - 120f)
                {
                    Projectile.ai[0] = TornadoLifespan - 120f + Projectile.ai[0] % 60f;
                    Projectile.netUpdate = true;
                }
            }
        }
        /// <summary>
        /// 自绘：把龙卷贴图沿地形长条**自下而上**每隔 5.1 像素盖印一枚，各枚按位置给出透明度、
        /// 并按存活帧数逐枚累加旋转角，叠成"旋转的尘柱"（照源；源里那两行算完不用的死代码已略去）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            const float fadeInFrames = 30f;     // 淡入帧数（照旧版支线）
            const float shownFrames = 150f;     // 淡出起点（照旧版支线：150，源 2.0 是 600）
            const float maxAlpha = 0.3f;        // 最大不透明度（照旧版支线）
            const float stampSpacing = 5.1f;    // 相邻盖印的像素间距（照源）

            float time = Projectile.ai[0];
            float opacity = MathHelper.Clamp(time / fadeInFrames, 0f, maxAlpha);
            if (time > shownFrames - 60f)
                opacity = MathHelper.Lerp(maxAlpha, 0f, (time - (shownFrames - 60f)) / 60f);

            Point tilePos = Projectile.Center.ToTileCoordinates();
            Collision.ExpandVertically(tilePos.X, tilePos.Y, out int topTile, out int bottomTile, VerticalSampleRange, VerticalSampleRange);
            topTile++;
            bottomTile--;
            Vector2 top = new Vector2(tilePos.X, topTile) * 16f + new Vector2(8f);
            Vector2 bottom = new Vector2(tilePos.X, bottomTile) * 16f + new Vector2(8f);
            float verticalSpan = bottom.Y - top.Y;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, 1, 0, 0);
            Vector2 origin = frame.Size() / 2f;
            Color tornadoColor = new Color(0, 250, 0);

            float distanceFromBottom = 0f;
            for (int y = (int)bottom.Y; y > (int)top.Y; y -= (int)stampSpacing)
            {
                distanceFromBottom += stampSpacing;
                float ratio = distanceFromBottom / verticalSpan;
                float spin = distanceFromBottom * MathHelper.TwoPi / -20f;

                // 上下两端渐隐：中段最亮
                Color stampColor = ratio > 0.5f
                    ? Color.Lerp(Color.Transparent, tornadoColor, 2f - ratio * 2f)
                    : Color.Lerp(Color.Transparent, tornadoColor, ratio * 2f);
                stampColor.A = (byte)(stampColor.A * 0.5f);
                stampColor *= opacity;

                Vector2 drawPos = new Vector2(bottom.X, y) - Main.screenPosition;
                Main.spriteBatch.Draw(texture, drawPos, frame, stampColor, -0.06283186f * time + spin, origin, 1f + (ratio - 0.15f), SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
