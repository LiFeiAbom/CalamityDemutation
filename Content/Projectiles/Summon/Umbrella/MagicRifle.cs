using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 魔法步枪（MagicRifle，按 CI 的 MagicRifleOld 移植；类名去掉 Old 后缀）——
    /// 光阴流时伞的礼帽随机抛出的远程工具：30×30、占 0 栏、穿地形、无限穿透、**自身无接触伤害**、
    /// 存活 180 帧。开场 30 帧只做防重叠与悬浮；之后在 <see cref="MagicHat.Range"/> 内索敌，
    /// 有目标时贴近到 200 像素内并转身瞄准，**每约 90 帧**朝目标射出一枚
    /// <see cref="MagicBullet"/>（速度 6）；无目标时跟随主人（离太远则回位、超过 2000 像素直接瞬移回）。
    /// </summary>
    internal class MagicRifle:ModProjectile
    {
        /// <summary>开场计时（源里的实例字段）</summary>
        private int counter = 0;
        /// <summary>标记为仆从射弹（源原样）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>基础属性（源原样）：30×30、占 0 栏、穿地形、无限穿透、180 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 0f;
            Projectile.timeLeft = 180;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
        }
        /// <summary>
        /// AI：防重叠 → 开场 30 帧只记时 → 索敌（优先玩家标记的目标）→ 有目标则贴近并瞄向它，
        /// 否则跟随主人（1600 像素的牵引距离，索敌时放宽到 2600）→ 按 90 帧冷却朝目标射击。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.MinionAntiClump();   // 源调用无参版本（默认推力 0.05f，与本工程移植件一致）
            counter++;
            if (counter == 30)
            {
                Projectile.netUpdate = true;
            }
            else if (counter < 30)
            {
                return;
            }
            float homingRange = MagicHat.Range;
            Vector2 targetVec = Projectile.position;
            bool foundTarget = false;
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float extraDist = npc.width / 2 + npc.height / 2;
                    float targetDist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (targetDist < homingRange + extraDist)
                    {
                        homingRange = targetDist;
                        targetVec = npc.Center;
                        foundTarget = true;
                    }
                }
            }
            if (!foundTarget)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.CanBeChasedBy(Projectile, false))
                    {
                        continue;
                    }
                    float extraDist = npc.width / 2 + npc.height / 2;
                    float targetDist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (targetDist < homingRange + extraDist)
                    {
                        homingRange = targetDist;
                        targetVec = npc.Center;
                        foundTarget = true;
                    }
                }
            }
            float separationAnxietyDist = foundTarget ? 2600f : 1600f;
            if (Vector2.Distance(player.Center, Projectile.Center) > separationAnxietyDist)
            {
                Projectile.ai[0] = 1f;
                Projectile.netUpdate = true;
            }
            if (foundTarget && Projectile.ai[0] == 0f)
            {
                Vector2 vecToTarget = targetVec - Projectile.Center;
                float targetDist = vecToTarget.Length();
                vecToTarget.Normalize();
                if (targetDist > 200f)
                {
                    vecToTarget *= 18f;
                    Projectile.velocity = (Projectile.velocity * 40f + vecToTarget) / 41f;
                }
                else
                {
                    vecToTarget *= -9f;
                    Projectile.velocity = (Projectile.velocity * 40f + vecToTarget) / 41f;
                }
            }
            else
            {
                bool returningToPlayer = Projectile.ai[0] == 1f;
                float moveSpeed = returningToPlayer ? 30f : 12f;
                Vector2 vecToPlayer = player.Center - Projectile.Center + new Vector2(0f, -120f);
                float playerDist = vecToPlayer.Length();
                if (playerDist < 200f && moveSpeed < 16f)
                {
                    moveSpeed = 16f;
                }
                if (playerDist < 600f && returningToPlayer)
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                if (playerDist > 2000f)
                {
                    Projectile.position.X = player.Center.X - Projectile.width / 2;
                    Projectile.position.Y = player.Center.Y - Projectile.height / 2;
                    Projectile.netUpdate = true;
                }
                if (playerDist > 70f)
                {
                    vecToPlayer.Normalize();
                    vecToPlayer *= moveSpeed;
                    Projectile.velocity = (Projectile.velocity * 40f + vecToPlayer) / 41f;
                }
                else if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
                {
                    Projectile.velocity.X = -0.15f;
                    Projectile.velocity.Y = -0.05f;
                }
            }
            if (foundTarget)
            {
                Projectile.spriteDirection = Projectile.direction = (targetVec.X - Projectile.Center.X > 0).ToDirectionInt();
                Projectile.rotation = Projectile.rotation.AngleTowards(
                    Projectile.AngleTo(targetVec) + (Projectile.spriteDirection == 1 ? MathHelper.ToRadians(45) : MathHelper.ToRadians(135)), 0.1f);
            }
            else
            {
                Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
                Projectile.rotation = Projectile.velocity.ToRotation()
                    + (Projectile.spriteDirection == 1 ? MathHelper.ToRadians(45) : MathHelper.ToRadians(135));
            }
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 90f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] != 0f || !foundTarget || Projectile.ai[1] != 0f)
            {
                return;
            }
            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }
            if (Main.rand.NextBool(6))
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = SoundID.Item20.Volume * 0.1f }, Projectile.position);
            }
            Projectile.ai[1] += 1f;
            Vector2 bulletVelocity = targetVec - Projectile.Center;
            bulletVelocity.Normalize();
            bulletVelocity *= 6f;
            SoundEngine.PlaySound(SoundID.Item40, Projectile.position);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, bulletVelocity,
                ModContent.ProjectileType<MagicBullet>(), Projectile.damage, 0f, Projectile.owner);
            Projectile.netUpdate = true;
        }
        /// <summary>绘制色固定为深紫（源原样）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(148, 0, 211, Projectile.alpha);
        /// <summary>步枪本体不造成接触伤害（照源）</summary>
        public override bool? CanDamage() => false;
        /// <summary>消亡：喷 10 粒彩虹火尘（源原样）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 speed = new Vector2(Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-7f, 7f));
                int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.RainbowTorch, speed.X, speed.Y, 160, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 0.75f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
