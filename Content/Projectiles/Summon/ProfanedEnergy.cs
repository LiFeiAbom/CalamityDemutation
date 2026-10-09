using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎能量炮台（照灾厄 2.0.3.9 <c>Projectiles/Summon/ProfanedEnergy.cs</c> 移植）——
    /// 圣化火花插下的**哨兵**：60×60 判定、4 帧动画、自身不接触伤害（<see cref="CanDamage"/> 恒 false），
    /// 每 16 帧在 1000 像素内（且要求看得见）挑个敌人，朝它甩一枚 <see cref="FlameBlast"/> 或
    /// <see cref="FlameBurst"/>（随机二选一，25 速度）。
    /// </summary>
    /// <remarks>
    /// 贴图照源**复用不动明王 NPC 的图**（源写 `Texture => "CalamityMod/NPCs/NormalNPCs/ImpiousImmolator"`，
    /// 62×272 = 4 帧 ×62×68）；本工程把它原样拷成同名贴图 `Content/Projectiles/Summon/ProfanedEnergy.png`，
    /// 于是不必写显式 `Texture` 覆盖，也就绕开了路径坑。
    /// </remarks>
    internal class ProfanedEnergy:ModProjectile
    {
        /// <summary>登场特效只放一次的开关（照源的 count 字段）</summary>
        private float count = 0f;

        /// <summary>4 帧动画；可被玩家右键标记目标</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>基础属性：照源（60×60、哨兵、生存时间取 tML 的哨兵寿命、无限穿透、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.sentry = true;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>4 帧循环动画 + 登场圣火特效 + 主人端每 16 帧朝目标甩一枚火焰弹</summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            // 4 帧循环（每 6 帧一翻，照源）
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
            {
                Projectile.frame = 0;
            }
            // 登场那一下：一声响 + 两轮圣火尘（照源）
            if (count == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
                for (int i = 0; i < 5; i++)
                {
                    int holy = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f, 100, default, 2f);
                    Main.dust[holy].velocity *= 3f;
                    Main.dust[holy].position = Projectile.Center;
                    if (Main.rand.NextBool())
                    {
                        Main.dust[holy].scale = 0.5f;
                        Main.dust[holy].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int j = 0; j < 10; j++)
                {
                    int fire = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, default, 3f);
                    Main.dust[fire].noGravity = true;
                    Main.dust[fire].velocity *= 5f;
                    Main.dust[fire].position = Projectile.Center;
                    fire = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, default, 2f);
                    Main.dust[fire].velocity *= 2f;
                    Main.dust[fire].position = Projectile.Center;
                }
                count += 1f;
            }
            // 索敌与开火只在主人端做（弹幕 AI 两端都会跑）
            if (Projectile.owner == Main.myPlayer)
            {
                // 开火冷却（出手时武器写入 16）
                if (Projectile.ai[0] != 0f)
                {
                    Projectile.ai[0] -= 1f;
                    return;
                }
                bool canAttack = false;
                float projX = Projectile.Center.X;
                float projY = Projectile.Center.Y;
                float attackDistance = 1000f;
                int target = 0;
                // 优先打玩家右键标记的目标
                if (player.HasMinionAttackTargetNPC)
                {
                    NPC npc = Main.npc[player.MinionAttackTargetNPC];
                    if (npc.CanBeChasedBy(Projectile, false))
                    {
                        float targetX = npc.Center.X;
                        float targetY = npc.Center.Y;
                        float targetDist = Math.Abs(Projectile.Center.X - targetX) + Math.Abs(Projectile.Center.Y - targetY);
                        if (targetDist < attackDistance && Collision.CanHit(Projectile.Center, Projectile.width, Projectile.height, npc.Center, npc.width, npc.height))
                        {
                            projX = targetX;
                            projY = targetY;
                            canAttack = true;
                            target = npc.whoAmI;
                        }
                    }
                }
                // 否则扫全场找最近的（曼哈顿距离，照源）
                if (!canAttack)
                {
                    for (int j = 0; j < Main.maxNPCs; j++)
                    {
                        if (Main.npc[j].CanBeChasedBy(Projectile, false))
                        {
                            float npcX = Main.npc[j].position.X + (float)(Main.npc[j].width / 2);
                            float npcY = Main.npc[j].position.Y + (float)(Main.npc[j].height / 2);
                            float npcDist = Math.Abs(Projectile.position.X + (float)(Projectile.width / 2) - npcX) + Math.Abs(Projectile.position.Y + (float)(Projectile.height / 2) - npcY);
                            if (npcDist < attackDistance && Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, Main.npc[j].position, Main.npc[j].width, Main.npc[j].height))
                            {
                                attackDistance = npcDist;
                                projX = npcX;
                                projY = npcY;
                                canAttack = true;
                                target = j;
                            }
                        }
                    }
                }
                if (canAttack)
                {
                    float projXStore = projX;
                    float projYStore = projY;
                    projX -= Projectile.Center.X;
                    projY -= Projectile.Center.Y;
                    // 目标在左边就朝右画（源的反向 spriteDirection 写法照抄）
                    if (projX < 0f)
                    {
                        Projectile.spriteDirection = 1;
                    }
                    else
                    {
                        Projectile.spriteDirection = -1;
                    }
                    // 两种火焰弹随机二选一（形状不同：一枚直、一枚散）
                    int projectileType = Utils.SelectRandom(Main.rand, new int[]
                    {
                        ModContent.ProjectileType<FlameBlast>(),
                        ModContent.ProjectileType<FlameBurst>()
                    });
                    float speed = 25f;
                    Vector2 fireDirection = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
                    float fireXVel = projXStore - fireDirection.X;
                    float fireYVel = projYStore - fireDirection.Y;
                    float fireVelocity = (float)Math.Sqrt((double)(fireXVel * fireXVel + fireYVel * fireYVel));
                    fireVelocity = speed / fireVelocity;
                    fireXVel *= fireVelocity;
                    fireYVel *= fireVelocity;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, fireXVel, fireYVel, projectileType, Projectile.damage, Projectile.knockBack, Projectile.owner, (float)target, 0f);

                    Projectile.ai[0] = 16f;
                }
            }
        }
        /// <summary>炮台自身不造成接触伤害（照源）</summary>
        public override bool? CanDamage() => false;
    }
}
