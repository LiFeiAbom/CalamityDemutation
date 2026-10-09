using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 空灵征服者召唤的**幻影**（照灾厄 2.0.3.9 <c>Projectiles/Summon/PhantomGuy.cs</c> 移植）：
    /// 30×30 判定、占 **1 格**召唤栏、`extraUpdates = 1`、**自身不造成接触伤害**；
    /// 平时飘在主人身边（离得远会加速回、超过 3500 像素直接瞬移），有目标时贴到 200 像素内绕着走，
    /// 并周期性地开一轮"连喷"——每 20 帧朝四周甩一枚**幽焰**（<see cref="GhostFire"/>），3 发后休整。
    /// </summary>
    /// <remarks>
    /// 贴图照源**复用噬魂幽花 Boss 的 <c>PhantomFuckYou</c> 图**，本工程把它原样拷成同名贴图
    /// `Content/Projectiles/Summon/PhantomGuy.png`（30×30），因而不用写显式 `Texture` 覆盖。
    /// 2.0 那版幻影只占**半格**栏位（`minionSlots = 0.5f`），本工程按 2.0.3.9 取 **1 格**。
    /// </remarks>
    internal class PhantomGuy:ModProjectile
    {
        /// <summary>连喷节拍计数（照源的公开字段）</summary>
        public int shootTimeCounter = 0;
        /// <summary>本轮是否在连喷（照源：只在主人端置位，其余端不生成弹幕）</summary>
        private bool canShoot = false;
        /// <summary>主人</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>主人的本模组玩家实例（读写 <c>pGuy</c> 标志）</summary>
        public CalamityDemutationPlayer ModdedOwner => Owner.GetModPlayer<CalamityDemutationPlayer>();

        /// <summary>可牺牲、可被右键标记目标（照源）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>基础属性：照源（30×30、占 1 栏、无限穿透、不撞地形、穿水、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>登场喷一圈幽尘，之后挂增益／按标志续命／防扎堆／索敌移动／周期性喷幽焰</summary>
        public override void AI()
        {
            Player player = Owner;
            CalamityDemutationPlayer modPlayer = ModdedOwner;
            // 登场一次：沿自身速度方向喷 36 颗幽尘（源写裸值 180，即 DustID.DungeonSpirit）
            if (Projectile.localAI[0] == 0f)
            {
                int dustAmt = 36;
                for (int i = 0; i < dustAmt; i++)
                {
                    Vector2 rotate = Vector2.Normalize(Projectile.velocity) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f;
                    rotate = rotate.RotatedBy((i - (dustAmt / 2 - 1)) * MathHelper.TwoPi / dustAmt) + Projectile.Center;
                    Vector2 faceDirection = rotate - Projectile.Center;
                    int dust = Dust.NewDust(rotate + faceDirection, 0, 0, DustID.DungeonSpirit, faceDirection.X * 1.75f, faceDirection.Y * 1.75f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = faceDirection;
                }
                Projectile.localAI[0] += 1f;
            }
            player.AddBuff(ModContent.BuffType<PhantomBuff>(), 3600);
            if (player.dead)
            {
                modPlayer.pGuy = false;
            }
            if (modPlayer.pGuy)
            {
                Projectile.timeLeft = 2;
            }
            Projectile.MinionAntiClump();

            // 索敌：优先玩家右键标记的目标，否则全场找最近的（射程 3000，照源）
            float attackDistance = 3000f;
            Vector2 targetCenter = Projectile.position;
            bool canAttack = false;
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float targetDist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (targetDist < attackDistance)
                    {
                        attackDistance = targetDist;
                        targetCenter = npc.Center;
                        canAttack = true;
                    }
                }
            }
            if (!canAttack)
            {
                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    NPC nPC2 = Main.npc[j];
                    if (nPC2.CanBeChasedBy(Projectile, false))
                    {
                        float targetDist = Vector2.Distance(nPC2.Center, Projectile.Center);
                        if (!canAttack && targetDist < attackDistance)
                        {
                            attackDistance = targetDist;
                            targetCenter = nPC2.Center;
                            canAttack = true;
                        }
                    }
                }
            }

            // 离主人太远就强制回程（有目标时放宽到 4000，照源）
            float separationAnxietyDist = 3500f;
            if (canAttack)
            {
                separationAnxietyDist = 4000f;
            }
            if (Vector2.Distance(player.Center, Projectile.Center) > separationAnxietyDist)
            {
                Projectile.ai[0] = 1f;
                Projectile.netUpdate = true;
            }

            if (canAttack && Projectile.ai[0] == 0f)
            {
                // 有目标：贴到离目标 200 像素处（贴太近就往外退）
                Vector2 targetDirection = targetCenter - Projectile.Center;
                float targetDistance = targetDirection.Length();
                targetDirection.Normalize();
                if (targetDistance > 200f)
                {
                    float scaleFactor2 = 18f;
                    targetDirection *= scaleFactor2;
                    Projectile.velocity = (Projectile.velocity * 40f + targetDirection) / 41f;
                }
                else
                {
                    targetDirection *= -12f;
                    Projectile.velocity = (Projectile.velocity * 40f + targetDirection) / 41f;
                }
            }
            else
            {
                // 待机或回主人：回程提速到 30，回到 600 内算归位，>3500 直接瞬移
                bool isReturning = false;
                if (!isReturning)
                {
                    isReturning = Projectile.ai[0] == 1f;
                }
                float returnSpeed = 12f;
                if (isReturning)
                {
                    returnSpeed = 30f;
                }
                Vector2 center2 = Projectile.Center;
                Vector2 playerDirection = player.Center - center2 + new Vector2(0f, -120f);
                float playerDist = playerDirection.Length();
                if (playerDist > 200f && returnSpeed < 16f)
                {
                    returnSpeed = 16f;
                }
                if (playerDist < 600f && isReturning)
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                if (playerDist > 3500f)
                {
                    Projectile.position.X = player.Center.X - (Projectile.width / 2);
                    Projectile.position.Y = player.Center.Y - (Projectile.height / 2);
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

            // 朝向：有目标就慢慢转过去，否则朝速度方向
            if (canAttack)
            {
                Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.AngleTo(targetCenter), 0.1f);
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            // 攻击节拍：ai[1] 随机累加、攒到 75 归零；归零且场上有目标时开一轮连喷
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += Main.rand.Next(1, 3);
            }
            if (Projectile.ai[1] > 75f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 0f)
            {
                if (canAttack && Projectile.ai[1] == 0f)
                {
                    Projectile.ai[1] += 1f;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        canShoot = true;
                        Projectile.netUpdate = true;
                    }
                }
            }
            if (canShoot)
            {
                shootTimeCounter++;

                // 前 60 帧里每 20 帧喷一发幽焰（共 3 发），并带一点后坐；只在主人端生成
                if (shootTimeCounter % 20 == 0 && shootTimeCounter <= 60)
                {
                    SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                    float randomRadius = Main.rand.Next(10, 14);
                    Vector2 randomVelocity = Main.rand.NextVector2CircularEdge(randomRadius, randomRadius);
                    Projectile.velocity -= randomVelocity * 0.22f;   // 开火后坐（源注释写的是 funny recoil）
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, randomVelocity.X, randomVelocity.Y, ModContent.ProjectileType<GhostFire>(), Projectile.damage, 0f, Main.myPlayer, 0f, 0f);
                    Projectile.netUpdate = true;
                }
                // 一轮连喷持续 200 帧
                if (shootTimeCounter > 200)
                {
                    canShoot = false;
                    shootTimeCounter = 0;
                    Projectile.netUpdate = true;
                }
            }
        }
        /// <summary>青白色染色（照源）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(100, 250, 250, Projectile.alpha);
        }
        /// <summary>幻影自身不造成接触伤害（照源；伤害全在幽焰上）</summary>
        public override bool? CanDamage() => false;
    }
}
