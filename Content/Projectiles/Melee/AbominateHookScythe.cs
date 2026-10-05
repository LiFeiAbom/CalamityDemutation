using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 惊惧巨镰（AbominateHookScythe） - 女妖之爪重制版（CalamityOverhaul 0.4.0.1.3）引导模式的主力弹幕，逐行移植。
    /// 由引导分支从鼠标位置拉出：<c>ai[0] = 0</c> 时沿初速冲刺并逐步减速；命中任意目标后转为
    /// <c>ai[0] = 1</c> 的锁敌猛扑（每 20 帧重扑一次），并且每次命中都有 1/5 概率额外甩出惊惧之灵。
    /// 目标消失时把自身剩下的威力转成一枚追踪主人的治疗之灵（<c>AbominateSpirit</c> 的 Status 3）。
    /// <para>
    /// 与源的唯一差异（联机修正，2026-10-05）：<c>OnHitNPC</c> 写入锁敌状态（<c>ai[0] = 1</c>、<c>ai[2] = 目标索引</c>）
    /// 后补了一条 <c>Projectile.netUpdate = true</c>——上游 CWR 三个版本都漏了这一步，而项目同步包（msg 27）
    /// 正好携带 <c>ai[0] / ai[1] / ai[2]</c>，补上之后其它客户端才会跟着进入锁敌猛扑。
    /// </para>
    /// </summary>
    internal class AbominateHookScythe : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/BansheeHookScythe";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.scale = 1f;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 160;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }
        /// <summary>冲刺速度缓存（命中锁敌后每 20 帧重算一次）</summary>
        public Vector2 DashVr = Vector2.Zero;
        public ref float Time => ref Projectile.localAI[0];
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.6f / 255f, 0f, 0f);
            Projectile.localAI[0] += MathHelper.ToRadians(35);
            if (Projectile.ai[0] == 0)
            {
                Projectile.velocity *= 0.95f;
            }
            if (Projectile.ai[0] == 1)
            {
                int targetIndex = (int)Projectile.ai[2];
                NPC target = targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].active && !Main.npc[targetIndex].friendly ? Main.npc[targetIndex] : null;
                if (target != null)
                {
                    if (Projectile.ai[1] == 0)
                    {
                        DashVr = Projectile.Center.To(target.Center);
                        Projectile.ai[1] = 1;
                        Time = 0;
                    }
                    if (Projectile.ai[1] == 1)
                    {
                        Projectile.velocity = DashVr.UnitVector() * 32;
                        if (Time > 20)
                        {
                            Projectile.ai[1] = 0;
                            Time = 0;
                        }
                    }
                    Projectile.localAI[1] = target.lifeMax;
                }
                else if (Projectile.owner == Main.myPlayer)
                {
                    // 目标没了：把剩余的"生命值余量"转成治疗之灵飞回主人（照源）
                    Vector2 spanPos = Projectile.Center;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spanPos, RandomVectorInDegrees(0f, 360f, 3f),
                        ModContent.ProjectileType<AbominateSpirit>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 3f, Projectile.localAI[1]);
                    Projectile.Kill();
                }
            }
            Time++;
        }
        /// <summary>
        /// 命中：1/5 概率（且自身命中次数 < 5）从目标上方 70~110° 方向甩出一枚惊惧之灵；
        /// 之后自身寿命 -10、转入锁敌猛扑，并把首个目标记进 ai[2]。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(5) && Projectile.numHits < 5)
            {
                Vector2 offset = RandomVectorInDegrees(70f, 110f, Main.rand.Next(500, 600));
                Vector2 spanPos = target.Center + offset;
                int status = Main.rand.Next(3);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spanPos, offset.UnitVector() * -13f,
                    ModContent.ProjectileType<AbominateSpirit>(), Projectile.damage / 3, 0f, Projectile.owner, status);
            }
            Projectile.timeLeft -= 10;
            Projectile.ai[0] = 1;
            if (Projectile.ai[2] == 0)
                Projectile.ai[2] = target.whoAmI;
            Projectile.netUpdate = true;   // 锁敌状态要发出去，否则别端的这把镰刀不会进入锁敌猛扑
        }
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte b = (byte)(Projectile.timeLeft * 3);
                byte alpha = (byte)(100f * (b / 255f));
                return new Color(b, b, b, alpha);
            }
            return new Color(255, 255, 255, 100);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                CDUtil.RecombinationColor((Color.Red, 0.3f), (Projectile.GetAlpha(Color.Gold), 0.7f)),
                Projectile.localAI[0], texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>按角度区间随机取一个方向向量（对应源的 CWRUtils.GetRandomVevtor）</summary>
        private static Vector2 RandomVectorInDegrees(float startAngle, float targetAngle, float length)
        {
            float radians = MathHelper.ToRadians(startAngle + (targetAngle - startAngle) * Main.rand.NextFloat());
            return new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * length;
        }
    }
}
