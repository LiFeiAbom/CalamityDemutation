using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 小鱿鱼喷出的**墨汁**（照灾厄 2.0.3.9 <c>Projectiles/Summon/CalamarisLamentProjectile.cs</c> 移植）：
    /// 28×28 判定、寿命 240、5 帧动画、逐敌独立冷却 −1（不打冷却），并**只打出生时锁定的那个目标**
    /// （源注释：这样追踪神明吞噬者那种多节 Boss 时能稳定打到头或尾、又不会把伤害数字刷爆）。
    /// </summary>
    internal class CalamarisLamentProjectile:ModProjectile
    {
        /// <summary>出生时锁定的目标索引（`ai[0]`，照源）</summary>
        public ref float TargetShotID => ref Projectile.ai[0];
        /// <summary>锁定目标</summary>
        public NPC TargetShot => Main.npc[(int)TargetShotID];

        /// <summary>5 帧动画；属于仆从弹药；残影 4 格；屏外宽容度取物品的索敌半径（照源）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)CalamarisLament.EnemyDistanceDetection;
        }
        /// <summary>基础属性：照源（28×28、寿命 240、穿水、不撞地形、召唤伤害、逐敌冷却 −1）</summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 240;
            Projectile.width = Projectile.height = 28;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
        }
        /// <summary>追锁定的目标（惯性 20、速度取物品参数）；目标没了就关掉额外更新，慢慢飘散</summary>
        public override void AI()
        {
            if (TargetShot is not null && TargetShot.active)
            {
                float inertia = 20f;
                Projectile.velocity = (Projectile.velocity * inertia + Projectile.SafeDirectionTo(TargetShot.Center) * CalamarisLament.ShootingProjectileSpeed) / (inertia + 1f);
                Projectile.extraUpdates = 1;

                Projectile.netUpdate = true;
                Projectile.netSpam = 0;
            }
            else
                Projectile.extraUpdates = 0;

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.alpha = (int)Utils.Remap(Projectile.timeLeft, 30f, 0f, 0f, 255f);

            // 墨点拖尾（源写裸值 109，即 DustID.Asphalt）
            Dust trailDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Asphalt, Scale: Main.rand.NextFloat(0.5f, 0.8f), Alpha: 127);
            trailDust.noGravity = true;
            trailDust.noLight = true;
            trailDust.alpha = (int)Utils.Remap(Projectile.timeLeft, 30f, 0f, 127f, 0f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }
        }
        /// <summary>只有和锁定目标重叠时才判伤害（照源：多节 Boss 不会重复刷伤害）</summary>
        public override bool? CanDamage() => Projectile.getRect().Intersects(TargetShot.getRect()) ? null : false;
        /// <summary>消失时再喷一小撮墨点并放个死亡音（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                Dust deathDust = Dust.NewDustPerfect(Projectile.Center, DustID.Asphalt, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 7f), Scale: Main.rand.NextFloat(0.5f, 1.5f), Alpha: 127);
                deathDust.noGravity = true;
                deathDust.noLight = true;
            }

            SoundEngine.PlaySound(SoundID.NPCDeath28, Projectile.Center);
        }
        /// <summary>先画残影再画本体（照源用灾厄的 DrawAfterimagesCentered，本工程用同名移植件）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor);

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
