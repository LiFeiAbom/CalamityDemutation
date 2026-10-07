using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 蚀日强光（Eclipse Mirror Flash）—— 蚀日魔镜闪避时在脚下炸开的一圈暗日强光，
    /// 整体照灾厄 **2.0** 的 <c>Projectiles/Rogue/EclipseMirrorBurst.cs</c>：
    /// 752×752 判定框、4 帧动画（贴图 2×2 网格、每格 752×752）、穿透 -1、不撞墙、忽略水、
    /// 存活 150 帧（实际动画约 16 帧后自灭）、局部无敌 5 帧。
    /// <para>
    /// 伤害类型按源码的 <c>forceClassless</c> 语义设成 **Generic**（伤害在闪避那一刻已经按盗贼伤害算过，
    /// 再吃一次职业加成就是双份；与深渊魔镜的流明流体、1.4.4 世系的显式 <c>DamageClass.Generic</c> 同款处理）。
    /// </para>
    /// </summary>
    internal class EclipseMirrorBurst : ModProjectile
    {
        private int frameCounter = 0;
        private int frameX = 0;
        private int frameY = 0;
        /// <summary>
        /// 基础属性：752×752、友好弹幕、穿透 -1、不撞墙、忽略水、存活 150 帧；
        /// 局部无敌帧 5（照源）；伤害类型设成 Generic（见类注释）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 752;
            Projectile.height = 752;
            Projectile.friendly = true;
            Projectile.alpha = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>
        /// 4 帧动画：每 4 帧推进一格（先走 Y 后走 X）；走到 (1,1) 即自灭（照源）
        /// </summary>
        public override void AI()
        {
            frameCounter++;
            if (frameCounter > 3)
            {
                frameCounter = 0;
                frameY++;
                if (frameY > 1)
                {
                    frameX++;
                    frameY = 0;
                }
            }
            if (frameX > 0 && frameY > 0)
                Projectile.Kill();
        }
        /// <summary>
        /// 自绘：贴图是 2×2 网格，按 (frameX, frameY) 取 752×752 的那一格（照源）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.spriteBatch.Draw
            (
                texture,
                Projectile.Center - Main.screenPosition,
                new Rectangle(frameX * 752, frameY * 752, 752, 752),
                Color.White,
                Projectile.rotation,
                Projectile.Size / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }
}
