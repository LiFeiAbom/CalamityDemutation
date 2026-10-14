using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 灼海喷流（Seas Searing Spout） - <see cref="WaterSpout"/> 的"带硫磺毒"版本，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/SeasSearingSpout.cs</c>（逐行照搬）。
    /// <para>
    /// 与 <see cref="WaterSpout"/> **几乎同构**（同样的 150×42 / 缩放 0.6 / 寿命 60 帧 / 6 帧动画 /
    /// 逐段向上续接 / 摇摆收尾），三处差别：① 命中用**本地**冷却（`usesLocalNPCImmunity` / 30 帧）；
    /// ② 自绘成 <c>(60, Main.DiscoG, 190)</c> 的**彩虹偏色**；③ 命中挂**硫磺毒** 240 帧。
    /// </para>
    /// <para>
    /// **它目前是一枚"死件"**：2.0 全工程只有 <c>TyphoonBubble</c> 在
    /// <c>localAI[1] == 1f</c> 时生成它，而没有任何地方给泡泡写过 <c>localAI[1]</c>。
    /// 本工程照源保留（并照工程口径把减益走软依赖）。
    /// </para>
    /// </summary>
    internal class SeasSearingSpout : ModProjectile
    {
        /// <summary>动画帧数（照源 6）</summary>
        private const int AnimationFrames = 6;
        /// <summary>缩放公式里的基数 / 步长（照源 10 / 15）</summary>
        private const int SegmentBase = 10;
        private const int SegmentStep = 15;
        /// <summary>贴图满宽 / 满高（照源 150 / 42）</summary>
        private const int FullWidth = 150;
        private const int FullHeight = 42;
        /// <summary>续接下一段时传入的存活档位（照源 10）</summary>
        private const float NextSegmentTimer = 10f;
        /// <summary>命中挂的减益时长（照源 240 帧）</summary>
        private const int PoisonDuration = 240;
        /// <summary>六帧动画（照源）</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = AnimationFrames;
        /// <summary>
        /// 基础属性：150×42 起步判定、缩放 0.6、不撞物块、不受水阻、穿透 -1、初始全透明、寿命 60 帧、
        /// **本地**命中冷却 30 帧（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 42;
            Projectile.scale = 0.6f;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        /// <summary>AI：与 <see cref="WaterSpout"/> 逐行一致（照源）</summary>
        public override void AI()
        {
            if (Projectile.velocity.X != 0f)
                Projectile.direction = Projectile.spriteDirection = -Math.Sign(Projectile.velocity.X);

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= AnimationFrames)
                Projectile.frame = 0;

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.position.X += Projectile.width / 2f;
                Projectile.position.Y += Projectile.height / 2f;
                Projectile.scale = (SegmentBase + SegmentStep - Projectile.ai[1]) / (float)(SegmentStep + SegmentBase);
                Projectile.width = (int)(FullWidth * Projectile.scale);
                Projectile.height = (int)(FullHeight * Projectile.scale);
                Projectile.position.X -= Projectile.width / 2f;
                Projectile.position.Y -= Projectile.height / 2f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[1] != -1f)
            {
                Projectile.scale = (SegmentBase + SegmentStep - Projectile.ai[1]) / (float)(SegmentStep + SegmentBase);
                Projectile.width = (int)(FullWidth * Projectile.scale);
                Projectile.height = (int)(FullHeight * Projectile.scale);
            }

            if (!Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                Projectile.alpha -= 30;
                if (Projectile.alpha < 60)
                    Projectile.alpha = 60;
            }
            else
            {
                Projectile.alpha += 30;
                if (Projectile.alpha > 150)
                    Projectile.alpha = 150;
            }

            if (Projectile.ai[0] > 0f)
                Projectile.ai[0] -= 1f;

            if (Projectile.ai[0] == 1f && Projectile.ai[1] > 0f && Projectile.owner == Main.myPlayer)
            {
                Projectile.netUpdate = true;
                Vector2 center = Projectile.Center;
                center.Y -= FullHeight * Projectile.scale / 2f;
                float nextScale = (SegmentBase + SegmentStep - Projectile.ai[1] + 1f) / (float)(SegmentStep + SegmentBase);
                center.Y -= FullHeight * nextScale / 2f;
                center.Y += 2f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), center.X, center.Y, Projectile.velocity.X, Projectile.velocity.Y,
                    Projectile.type, Projectile.damage, Projectile.knockBack, Projectile.owner, NextSegmentTimer, Projectile.ai[1] - 1f);
            }

            if (Projectile.ai[0] <= 0f)
            {
                const float swayRate = 0.104719758f;
                float swayWidth = Projectile.width / 5f;
                float swayOffset = (float)(Math.Cos(swayRate * -Projectile.ai[0]) - 0.5) * swayWidth;
                Projectile.position.X -= swayOffset * -Projectile.direction;
                Projectile.ai[0] -= 1f;
                swayOffset = (float)(Math.Cos(swayRate * -Projectile.ai[0]) - 0.5) * swayWidth;
                Projectile.position.X += swayOffset * -Projectile.direction;
            }
        }
        /// <summary>彩虹偏色（照源 <c>new Color(60, Main.DiscoG, 190, alpha)</c>）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(60, Main.DiscoG, 190, Projectile.alpha);
        /// <summary>自绘（照源，同 <see cref="WaterSpout"/> 的切帧画法）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), frame,
                Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
        /// <summary>
        /// 命中挂**硫磺毒** 240 帧（照源 <c>SulphuricPoisoning</c>）。走工程软依赖口径：
        /// 现代版取 `CalamityMod/SulphuricPoisoning`，取不到退回原版**毒液**（`BuffID.Venom`）。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "SulphuricPoisoning", PoisonDuration, BuffID.Venom);
        /// <summary>PvP 命中：同样的减益（照 <c>OnHitPlayer</c> 口径）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "SulphuricPoisoning", PoisonDuration, BuffID.Venom);
    }
}
