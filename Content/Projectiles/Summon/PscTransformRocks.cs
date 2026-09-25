using System.Collections.Generic;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎之魂水晶变身动画里飞向主人的岩石（移植自灾厄 2.2.2 的 PscTransformRocks）：
    /// 由 PscTransformAnimation 从主人周围 ±500 像素处召出，先放大到 1.25 倍再高速飞向主人，
    /// 撞到主人后骤减速并逐渐淡出；若生成时场上岩石已超 20 个（ai[0] == 0）则到主人身边立刻消散，
    /// 变身动画结束时（profanedCrystalAnim == -1）也会加速淡出。
    /// 贴图按 ai[1] 取 6 种外观之一（与灾厄一致地取自 ArtifactOfResilienceShard1-6，本工程即 PscTransformRocks1-6）。
    /// 本身无伤害，绘制时画在玩家身前（DrawBehind 的 overPlayers）。
    /// </summary>
    internal class PscTransformRocks:ModProjectile
    {
        // ── 状态与属性 ──
        /// <summary>弹幕主人：吸附目标与变身状态都以主人为参照</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>
        /// 贴图为 6 张岩石外观的第一张，实际绘制时用 PreDraw 按 ai[1] 替换后缀（PscTransformRocks1..6）
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Summon/PscTransformRocks1";
        // ── 生命周期方法 ──
        /// <summary>登记 4 帧残影缓存、关闭残影自动模式（PreDraw 里手动画），并禁用液体扭曲</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.NoLiquidDistortion[Type] = true;
        }
        /// <summary>基础属性：26x26 碰撞箱、忽略水面、不碰地形、存活 120 帧、初始缩放 0.1</summary>
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ProfanedSoulCrystal.maxPscAnimTime;
            Projectile.alpha = 0;
            Projectile.hide = true;
            Projectile.scale = 0.1f;
        }
        // ── 覆写方法 ──
        /// <summary>画在玩家身前（对应灾厄"重写 DrawBehind 是为了画在玩家前面"的原注）</summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        /// <summary>
        /// AI：主人卸下水晶即消散；先放大到 1.25 倍，随后按"是否还要留在主人身边"决定淡出速度，
        /// 并持续朝主人加速（撞上主人后骤减速）；淡透即移除
        /// </summary>
        public override void AI()
        {
            CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
            if (!modPlayer.profanedCrystal)
            {
                Projectile.active = false;
                return;
            }
            // 逐步放大到 1.25 倍
            if (Projectile.scale < 1.25f)
                Projectile.scale += 0.1f;
            // 不允许留下的（ai[0]==0）到主人身边就立刻淡出，允许留下的（ai[0]!=0）到寿命尾段才淡出
            if ((Projectile.velocity.Length() <= 3f && Projectile.ai[0] == 0f) || Projectile.timeLeft <= 25)
                Projectile.alpha += Projectile.ai[0] == 0f ? 20 : 10;
            // 变身动画已结束：无论岩石在哪都加速淡出
            if (modPlayer.profanedCrystalAnim == -1)
                Projectile.alpha += 15;
            // 淡透的岩石直接移除
            if (Projectile.alpha >= 255)
                Projectile.active = false;
            // 贴到主人身上就骤减速，否则持续以 18 像素/帧追主人
            if (Projectile.Hitbox.Intersects(Owner.Hitbox))
                Projectile.velocity *= 0.1f;
            else
            {
                Vector2 target = Owner.Center - Projectile.Center;
                target.Normalize();
                Projectile.velocity = target * 18f;
            }
        }
        /// <summary>不造成伤害</summary>
        public override bool? CanDamage() => false;
        /// <summary>
        /// 绘制：按 ai[1] 取岩石外观，开启性能模式以外的残影设置时手动画 4 帧残影
        /// （原灾厄的残影开关 CalamityClientConfig.Afterimages 用本工程的 ConfigSystem.PerformanceMode 等价替代）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            int rockType = (int)MathHelper.Clamp(Projectile.ai[1], 1f, 6f);
            Texture2D texture = ModContent.Request<Texture2D>(Texture[..^1] + rockType.ToString()).Value;
            Vector2 drawOrigin = new Vector2(texture.Width / 2, texture.Height / 2);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            drawPos -= new Vector2(texture.Width, texture.Height) * Projectile.scale / 2f;
            drawPos += drawOrigin * Projectile.scale + new Vector2(0f, Projectile.gfxOffY);
            Rectangle frame = new Rectangle(0, 0, texture.Width, texture.Height);
            if (ConfigSystem.Instance?.PerformanceMode != true)   // 手动画残影（灾厄的公用工具画法有问题）
            {
                for (int i = 0; i < Projectile.oldPos.Length; ++i)
                {
                    drawPos = Projectile.oldPos[i] + (Projectile.Size / 2f) - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                    // 下面这两个"多余"的 float 强制转换不能删，删了残影会画错
                    Color color = Projectile.GetAlpha(lightColor) * ((float)(Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);
                    Main.EntitySpriteDraw(texture, drawPos, frame, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0f);
                }
            }
            else
            {
                Main.EntitySpriteDraw(texture, drawPos, frame, Color.White, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
