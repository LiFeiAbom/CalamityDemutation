using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 毁灭之星射出的毁灭地雷（照灾厄 2.0 的 <c>DestructionStar</c>）：
    /// 94×94 判定但**实际按半径 47 的圆判命中**、穿透 16、存活 300 帧、不撞物块不撞水、
    /// 同一敌人每 10 帧可再吃一次；每帧按水平方向自转 8°，1/8 概率冒尘。
    /// <para>
    /// 弹数计数：源把 <c>hitCount</c> 记在 <c>ModProjectile</c> 的**公开字段**上——tML 里
    /// <c>ModProjectile</c> 是每类型单例，那颗计数会被**所有**毁灭地雷共享（联机/连发时尤其明显）。
    /// 本工程改用 <c>Projectile.localAI[0]</c> 做真正的每实例计数，逻辑（命中 +1、潜行或 &gt;16 时钳到 16）与源一致。
    /// </para>
    /// <para>消散：原版爆炸音 + 烟雾尘 + 由主人端甩出 <c>hitCount</c> 枚毁灭弹（伤害 ×0.5）。</para>
    /// </summary>
    internal class DestructionStar : ModProjectile
    {
        /// <summary>圆形判定半径（照源）</summary>
        private const float HitRadius = 47f;
        /// <summary>弹数上限（照源）</summary>
        private const float MaxBoltCount = 16f;
        /// <summary>尘类型（源里是裸数字 191）</summary>
        private const int StarDust = DustID.SpookyWood;

        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/StarofDestruction";

        /// <summary>94×94 判定、穿透 16、存活 300 帧、不撞物块不撞水、同一敌人每 10 帧可再命中</summary>
        public override void SetDefaults()
        {
            Projectile.width = 94;
            Projectile.height = 94;
            Projectile.friendly = true;
            Projectile.penetrate = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>冒尘 + 按水平方向自转 8° + 每帧把弹数钳到 16（潜行打击的必定是 16）</summary>
        public override void AI()
        {
            if (Main.rand.NextBool(8))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, StarDust, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * MathHelper.ToRadians(8f);
            if (Projectile.localAI[0] > MaxBoltCount || CDUtil.IsStealthStrike(Projectile, out _))
                Projectile.localAI[0] = MaxBoltCount;
        }
        /// <summary>命中计数 +1（命中回调只在主人端跑一次，联机口径见第 5 节）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.localAI[0]++;
        }
        /// <summary>PvP 同样计数</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.localAI[0]++;
        }
        /// <summary>手绘物品贴图（94×94）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>圆形判定（内联灾厄 CollisionUtils.CircularHitboxCollision，写法照本工程 AbaddonCrit）</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle center = new Rectangle((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= HitRadius;
        }
        /// <summary>消散：爆炸音 + 烟雾尘 + 主人端甩出 <c>hitCount</c> 枚毁灭弹（伤害 ×0.5）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Vector2 size = new Vector2(20f, 20f);
            for (int i = 0; i < 10; i++)
                Main.dust[Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, DustID.Smoke, 0f, 0f, 100, new Color(), 1.5f)].velocity *= 1.4f;
            if (Projectile.owner != Main.myPlayer)
                return;
            int boltCount = (int)Projectile.localAI[0];
            for (int i = 0; i < boltCount; i++)
            {
                Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<DestructionBolt>(), (int)(Projectile.damage * 0.5f), 0f, Projectile.owner, 0f, 0f);
            }
        }
    }
}
