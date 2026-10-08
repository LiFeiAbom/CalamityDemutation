using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 震爆手雷潜行打击留下的电气光环（照灾厄 2.0 的 <c>ShockTeslaAura</c>）：
    /// 218×218 判定框、**半径 98 的圆**、存活 240 帧、同一敌人每 20 帧可再吃一次；
    /// 3×6 帧动画（每 4 帧换一帧）；最后 20 帧淡出并沿半径撒电尘；
    /// 命中挂 3 秒「带电」，并把非霸体敌人往外推。
    /// <para>
    /// 贴图照源取灾厄共用的 <c>Projectiles/Typeless/TeslaAura</c>，本件把它复制进本工程
    /// （<c>ShockTeslaAura.png</c>，654×1308 = 3 帧 × 6 帧），软依赖下不直接引灾厄资源路径。
    /// </para>
    /// </summary>
    internal class ShockTeslaAura : ModProjectile
    {
        /// <summary>圆形判定半径（照源）</summary>
        private const float AuraRadius = 98f;
        /// <summary>总存活帧数（照源）</summary>
        private const int Lifetime = 240;
        /// <summary>横向帧数（贴图 654 = 218 × 3）</summary>
        private const int FramesX = 3;
        /// <summary>纵向帧数（贴图 1308 = 218 × 6）</summary>
        private const int FramesY = 6;
        /// <summary>淡出耗时（最后这么多帧线性变淡，并逐帧撒尘）</summary>
        private const int FadeTime = 20;
        /// <summary>电尘类型（两套配色随机取）</summary>
        private const int SparkDust = 132;

        public override void SetDefaults()
        {
            Projectile.width = 218;
            Projectile.height = 218;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>每 4 帧推一格动画：localAI[0] 走纵向 6 帧，localAI[1] 走横向 3 帧</summary>
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.localAI[0]++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.localAI[0] >= FramesY)
            {
                Projectile.localAI[0] = 0f;
                Projectile.localAI[1]++;
            }
            if (Projectile.localAI[1] >= FramesX)
                Projectile.localAI[1] = 0f;
        }
        /// <summary>手绘 3×6 帧动画；最后 20 帧线性淡出，并按淡出进度沿半径撒电尘</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle sourceRect = new Rectangle(Projectile.width * (int)Projectile.localAI[1], Projectile.height * (int)Projectile.localAI[0], Projectile.width, Projectile.height);
            Vector2 origin = new Vector2(Projectile.width / 2f, Projectile.height / 2f);
            float opacity = 1f;
            int sparkCount = 0;
            if (Projectile.timeLeft < FadeTime)
            {
                opacity = Projectile.timeLeft * (1f / FadeTime);
                sparkCount = FadeTime - Projectile.timeLeft;
            }
            for (int i = 0; i < sparkCount * 2; i++)
            {
                int dustType = SparkDust;
                if (Main.rand.NextBool())
                    dustType = 264;
                const float rangeDiff = 2f;
                Vector2 dustPos = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                dustPos.Normalize();
                dustPos *= AuraRadius + Main.rand.NextFloat(-rangeDiff, rangeDiff);
                int dust = Dust.NewDust(Projectile.Center + dustPos, 1, 1, dustType, 0f, 0f, 0, default, 0.75f);
                Main.dust[dust].noGravity = true;
            }
            Main.EntitySpriteDraw(sprite, Projectile.Center - Main.screenPosition, sourceRect, Color.White * opacity, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>命中：挂 3 秒「带电」；非霸体敌人照源用击退参数往外推一把</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);
            if (target.knockBackResist <= 0f || !ShouldAffectNPC(target))
                return;
            float knockbackMultiplier = Projectile.knockBack - (1f - target.knockBackResist);
            if (knockbackMultiplier < 0f)
                knockbackMultiplier = 0f;
            Vector2 trueKnockback = target.Center - Projectile.Center;
            trueKnockback.Normalize();
            target.velocity = trueKnockback * knockbackMultiplier;
        }
        /// <summary>PvP 同样挂 3 秒「带电」</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 180);
        }
        /// <summary>
        /// 对应灾厄的 <c>CalamityGlobalNPC.ShouldAffectNPC</c>：只对普通敌怪生效
        /// （灾厄还排除了自家的一批小 Boss 部件，那些类型软依赖下引不到，这里只保留原版那部分排除项，
        /// 与工程里 AnarchyBlade 的内联版完全一致）。
        /// </summary>
        private static bool ShouldAffectNPC(NPC target) => target.damage > 0 && !target.boss && !target.friendly && !target.dontTakeDamage
            && target.type != NPCID.Creeper && target.type != NPCID.MourningWood && target.type != NPCID.Everscream
            && target.type != NPCID.SantaNK1 && target.type != NPCID.GolemFistLeft && target.type != NPCID.GolemFistRight
            && target.type != NPCID.DD2Betsy;
        /// <summary>
        /// 圆形判定（内联灾厄 CollisionUtils.CircularHitboxCollision）：圆心 98 像素内算命中。
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle center = new Rectangle((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= AuraRadius;
        }
    }
}
