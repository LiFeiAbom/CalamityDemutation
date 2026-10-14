using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 水柱（Water Spout） - 泡泡托出的那根**向上续接**的水柱，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/WaterSpout.cs</c>（逐行照搬）。
    /// <para>
    /// 判定从 150×42 起步、按 <c>ai[1]</c> 档位缩放（0.6 → 越来越小），逐段向上"长"：
    /// 每段在 <c>ai[0]</c> 倒数到 1 时**再生成一段**（<c>ai[0] = 10</c>、<c>ai[1] - 1</c>），
    /// 于是 16 档连成一整根水柱；每段寿命 60 帧、穿透 -1、6 帧动画、
    /// 命中用**静态**冷却（`usesIDStaticNPCImmunity` / 10 帧）。
    /// 嵌进实心块时变淡（alpha → 150），露天则变清晰（→ 60）；展开到位后左右摇摆收尾（照源）。
    /// </para>
    /// <para>
    /// 联机：续接那一段写在 <c>Projectile.owner == Main.myPlayer</c> 判据里（照源），
    /// 其余只是位置/透明度演算，各端一致。
    /// </para>
    /// </summary>
    internal class WaterSpout : ModProjectile
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
        /// <summary>六帧动画（照源）</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = AnimationFrames;
        /// <summary>
        /// 基础属性：150×42 起步判定、缩放 0.6、不撞物块、不受水阻、穿透 -1、初始全透明、寿命 60 帧、
        /// **静态**命中冷却 10 帧（照源）
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
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }
        /// <summary>AI（照源）：摆向 → 逐帧换帧 → 按档位定缩放与判定 → 透明度随是否嵌块变化 → 续接下一段 → 收尾摇摆</summary>
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

            // 首帧：以自身中心为锚点按档位重算缩放与判定框
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

            // 露天变清晰、嵌块变淡
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

            // 向上续接下一段（照源：只在主人端生成）
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

            // 展开到位后左右摇摆（照源）
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
        /// <summary>自绘（照源）：按 <c>Main.projFrames</c> 切帧、以帧高一半为原点贴到自身中心</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), frame,
                Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
