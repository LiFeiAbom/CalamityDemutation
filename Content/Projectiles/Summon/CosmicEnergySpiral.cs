using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 归虚之灵召唤的**宇宙之灵**（照 CI 的 <c>Projectiles/Summon/CosmicEnergySpiralOld.cs</c> 移植；
    /// **命名口径（用户 2026-10-09）**：类名去掉 CI 式的 <c>Old</c> 后缀，旧名以
    /// <see cref="LegacyNameAttribute"/> 保留）：
    /// 78×78 判定、**占 10 格召唤栏**、`extraUpdates = 1`、**自身不造成接触伤害**；
    /// 平时悬在主人身边（跟随距离 1400 / 有目标 1600 / 回程 2400，照源那套翻倍的值），
    /// 登场先冷却 100 帧，之后每 60 帧朝 2400 像素内最近的敌人喷一轮爆裂。
    /// </summary>
    /// <remarks>
    /// 本工程**恒处 ExoLore（传颂之物）模式**（照第 9 节 ExoBlade / ExoBeam 口径）：每轮发射 12~18 枚小爆裂
    /// （<see cref="CosmicBlast"/>，伤害取仆从的 1/2）+ 2 枚 ±140° 斜射大爆裂与 1 枚直射大爆裂
    /// （<see cref="CosmicBlastBig"/>，全额伤害）；非 Lore 分支（5~8 小 + 1 大、冷却 100 帧）不另留一份。
    /// 发色也照 Lore 分支：<see cref="GetAlpha"/> 恒返回**纯白**（非 Lore 分支才是彩虹色）。
    /// 一处修正：源 Lore 分支给小爆裂传的 originalDamage 忘了折半（非 Lore 分支是折半的，属上游笔误），
    /// 而本机 tML 对带 minion 标记的弹幕每帧都会用 originalDamage×玩家伤害加成 重算伤害，
    /// 会让小爆裂打满额——本工程按意图补回折半 originalDamage / 2。
    /// </remarks>
    [LegacyName("CosmicEnergySpiralOld")]
    internal class CosmicEnergySpiral:ModProjectile
    {
        /// <summary>登场冷却只设一次的开关（照源的实例字段）</summary>
        private bool justSpawned = true;
        /// <summary>主人</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>主人的本模组玩家实例（读写 <c>cosmicEnergy</c> 标志）</summary>
        public CalamityDemutationPlayer ModdedOwner => Owner.GetModPlayer<CalamityDemutationPlayer>();

        /// <summary>照源：它同时被登记为"宠物"（projPet），并开残影缓存 6 格</summary>
        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>基础属性：照源（78×78、占 10 栏、无限穿透、不撞地形、穿水、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 10f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>彩虹光照 → 续命 → 跟随/回程 → 缩放脉动 → 主人端每 60 帧喷一轮爆裂（ExoLore 分支）</summary>
        public override void AI()
        {
            Player player = Owner;
            Lighting.AddLight((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16, (float)Main.DiscoR / 255f, (float)Main.DiscoG / 255f, (float)Main.DiscoB / 255f);
            player.AddBuff(ModContent.BuffType<CosmicEnergyBuff>(), 3600);
            if (player.dead)
                ModdedOwner.cosmicEnergy = false;
            if (ModdedOwner.cosmicEnergy)
                Projectile.timeLeft = 2;

            // 跟随 / 回程三档距离（源在原版基础上翻倍）
            float enemyDistWhenIdle = 1400f;
            float distToForceReturn = 1600f;
            float distToForceReturnWhenAttacking = 2400f;
            float distToSettleDown = 1600f;
            Projectile.rotation += Projectile.velocity.X * 0.1f;
            Vector2 targetCenter = Projectile.position;
            bool foundTarget = false;
            int target = 0;
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float targetDist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (!foundTarget && targetDist < enemyDistWhenIdle)
                    {
                        targetCenter = npc.Center;
                        foundTarget = true;
                        target = npc.whoAmI;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.CanBeChasedBy(Projectile, false))
                    {
                        float targetDist = Vector2.Distance(npc.Center, Projectile.Center);
                        if (!foundTarget && targetDist < enemyDistWhenIdle)
                        {
                            enemyDistWhenIdle = targetDist;
                            targetCenter = npc.Center;
                            foundTarget = true;
                            target = i;
                        }
                    }
                }
            }
            float distLimit = distToForceReturn;
            if (foundTarget)
            {
                distLimit = distToForceReturnWhenAttacking;
            }
            if (Vector2.Distance(player.Center, Projectile.Center) > distLimit)
            {
                Projectile.ai[1] = 1f;
                Projectile.netUpdate = true;
            }
            if (foundTarget && Projectile.ai[1] == 0f)
            {
                // 有目标：贴到离目标 200 像素处（近了就往外退）
                Vector2 targetDirection = targetCenter - Projectile.Center;
                float targetDistance = targetDirection.Length();
                targetDirection.Normalize();
                if (targetDistance > 200f)
                {
                    float scaleFactor2 = 6f;
                    targetDirection *= scaleFactor2;
                    Projectile.velocity = (Projectile.velocity * 40f + targetDirection) / 41f;
                }
                else
                {
                    float scaleFactor3 = 4f;
                    targetDirection *= -scaleFactor3;
                    Projectile.velocity = (Projectile.velocity * 40f + targetDirection) / 41f;
                }
            }
            else
            {
                // 待机 / 回主人：回程提速到 15、回到 1600 内算归位、>2000 直接瞬移
                bool isReturning = false;
                if (!isReturning)
                {
                    isReturning = Projectile.ai[1] == 1f;
                }
                float returnSpeed = 6f;
                if (isReturning)
                {
                    returnSpeed = 15f;
                }
                Vector2 center2 = Projectile.Center;
                Vector2 playerDirection = player.Center - center2 + new Vector2(0f, -60f);
                float playerDist = playerDirection.Length();
                if (playerDist > 200f && returnSpeed < 8f)
                {
                    returnSpeed = 8f;
                }
                if (playerDist < distToSettleDown && isReturning && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                }
                if (playerDist > 2000f)
                {
                    Projectile.position.X = player.Center.X - (float)(Projectile.width / 2);
                    Projectile.position.Y = player.Center.Y - (float)(Projectile.height / 2);
                    Projectile.netUpdate = true;
                }
                if (playerDist > 70f)
                {
                    playerDirection.Normalize();
                    playerDirection *= returnSpeed;
                    Projectile.velocity = (Projectile.velocity * 40f + playerDirection) / 41f;
                }
                else if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
                {
                    Projectile.velocity.X = -0.15f;
                    Projectile.velocity.Y = -0.05f;
                }
            }

            // 用鼠标文字色做轻微脉动缩放（源写法）
            float scalar = (float)Main.mouseTextColor / 200f - 0.35f;
            scalar *= 0.2f;
            Projectile.scale = scalar + 0.95f;

            if (justSpawned)
            {
                justSpawned = false;
                Projectile.ai[0] = 100f;   // 登场先冷却 100 帧（照源）
            }

            // 索敌与开火只在主人端做（弹幕 AI 两端都会跑）
            if (Projectile.owner == Main.myPlayer)
            {
                if (Projectile.ai[0] != 0f)
                {
                    Projectile.ai[0] -= 1f;
                    return;
                }
                // 开火用的索敌：曼哈顿距离、射程 2400（照源与跟随用的那套分开）
                float nearestDist = 2400f;
                float fireTargetX = Projectile.position.X;
                float fireTargetY = Projectile.position.Y;
                bool hasFireTarget = false;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].CanBeChasedBy(Projectile, false))
                    {
                        float npcX = Main.npc[i].position.X + (float)(Main.npc[i].width / 2);
                        float npcY = Main.npc[i].position.Y + (float)(Main.npc[i].height / 2);
                        float npcDist = Math.Abs(Projectile.position.X + (float)(Projectile.width / 2) - npcX) + Math.Abs(Projectile.position.Y + (float)(Projectile.height / 2) - npcY);
                        if (npcDist < nearestDist)
                        {
                            nearestDist = npcDist;
                            fireTargetX = npcX;
                            fireTargetY = npcY;
                            hasFireTarget = true;
                        }
                    }
                }
                if (hasFireTarget)
                {
                    // ExoLore 分支（本工程常驻）：12~18 枚小爆裂 + 2 枚斜射大爆裂 + 1 枚直射大爆裂
                    SoundEngine.PlaySound(SoundID.Item105, Projectile.position);
                    int blastAmt = Main.rand.Next(12, 18);
                    for (int b = 0; b < blastAmt; b++)
                    {
                        Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                        int p2 = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<CosmicBlast>(), Projectile.damage / 2, 2f, Projectile.owner, (float)target, 0f);
                        if (Main.projectile.IndexInRange(p2))
                            Main.projectile[p2].originalDamage = Projectile.originalDamage / 2;   // 补回折半（源 Lore 分支漏了，非 Lore 分支是对的）
                    }
                    float speed = 15f;
                    float aimX = fireTargetX - Projectile.Center.X;
                    float aimY = fireTargetY - Projectile.Center.Y;
                    float aimLength = (float)Math.Sqrt((double)(aimX * aimX + aimY * aimY));
                    aimLength = speed / aimLength;
                    aimX *= aimLength;
                    aimY *= aimLength;

                    Vector2 randomVelocity = CDUtil.RandomVelocity(100f, 100f, 100f);
                    int p3 = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, randomVelocity.RotatedBy(MathHelper.ToRadians(-140)), ModContent.ProjectileType<CosmicBlastBig>(), Projectile.damage, 2, Projectile.owner, 0f, 0f);
                    if (Main.projectile.IndexInRange(p3))
                        Main.projectile[p3].originalDamage = Projectile.originalDamage;

                    int p4 = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, randomVelocity.RotatedBy(MathHelper.ToRadians(140)), ModContent.ProjectileType<CosmicBlastBig>(), Projectile.damage, 2, Projectile.owner, 0f, 0f);
                    if (Main.projectile.IndexInRange(p4))
                        Main.projectile[p4].originalDamage = Projectile.originalDamage;

                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, aimX * 2, aimY * 2, ModContent.ProjectileType<CosmicBlastBig>(), Projectile.damage, 3f, Projectile.owner, (float)target, 0f);
                    if (Main.projectile.IndexInRange(p))
                        Main.projectile[p].originalDamage = Projectile.originalDamage;
                    Projectile.ai[0] = 60f;   // ExoLore 分支的冷却（非 Lore 是 100 帧）
                }
            }
        }
        /// <summary>Lore 分支的写法：恒为纯白（非 Lore 分支才是随彩虹色变化）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 255);
        }
        /// <summary>宇宙之灵本身不造成接触伤害（照源：伤害全在爆裂上）</summary>
        public override bool? CanDamage() => false;
        /// <summary>自绘：按残影缓存画一串位置残影（照源用灾厄的 DrawAfterimagesCentered，本工程用同名移植件）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
