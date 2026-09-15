using System;
using System.IO;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 迷你神圣守卫·防御者（移植自灾厄 2.2.2 的 MiniGuardianDefense）：亵渎灵魂神器召唤的三守卫之一。
    /// 本体不造成伤害（CanDamage 恒 false），伤害数值只用于给环绕岩石（MiniGuardianRock）乘算；
    /// 护盾在场时召出 5 颗岩石绕主人旋转，护盾消失时把岩石全部甩出。
    /// 护盾耐久（pSoulShieldDurability）与四态取值见 shieldActive / HandleRocks 的说明。
    /// </summary>
    internal class MiniGuardianDefense:ModProjectile
    {
        /// <summary>三种 AI 状态：护盾在场 / 护盾消失（只在主人身上喷尘）/ 纯外观（水晶形态专用）</summary>
        public enum MiniDefenderAIState
        {
            ShieldActive,
            ShieldInactive,
            Vanity
        }
        /// <summary>弹幕主人：跟随、索敌与岩石归属都以主人为参照</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>ai[0] == 1 表示由水晶形态召唤（装备水晶时由 CalamityDemutationPlayer 写入）</summary>
        public bool SpawnedFromPSC => Projectile.ai[0] == 1f;
        /// <summary>原为 SpawnedFromPSC &amp;&amp; !profanedCrystalBuffs；水晶未激活（profanedCrystalBuffs=false）时才按纯外观处理</summary>
        public bool ForcedVanity => SpawnedFromPSC && !Owner.GetModPlayer<CalamityDemutationPlayer>().profanedCrystalBuffs;   // 对应灾厄 SpawnedFromPSC && !profanedCrystalBuffs
        /// <summary>
        /// 护盾是否在场：护盾耐久大于 0 即在位（对应原版 !ForcedVanity &amp;&amp; pSoulShieldDurability &gt; 0）。
        /// 在位时岩石环绕主人；护盾被打破的那一帧会把岩石甩出去；回充回来后重新聚拢。
        /// </summary>
        public bool shieldActive => !ForcedVanity && Owner.GetModPlayer<CalamityDemutationPlayer>().profanedSoulShieldDurability > 0;
        /// <summary>上一帧护盾是否在场：用来检测"召出岩石 / 甩出岩石"的边沿</summary>
        public bool shieldActiveBefore = false;
        /// <summary>当前 AI 状态：纯外观（水晶形态）&gt; 护盾在场 &gt; 护盾消失</summary>
        public MiniDefenderAIState AIState => ForcedVanity ? MiniDefenderAIState.Vanity : (shieldActive ? MiniDefenderAIState.ShieldActive : MiniDefenderAIState.ShieldInactive);
        /// <summary>注册 4 帧动画、登记 4 帧残影缓存，并标记为可牺牲、可右键锁定目标的召唤物</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }
        /// <summary>基础属性：62x80 碰撞箱、友方、无限穿透、不碰撞地形、参与联网同步</summary>
        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.width = 62;
            Projectile.height = 80;
            Projectile.minion = true;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
        }
        /// <summary>
        /// 生 / 甩环绕岩石。spawnRocks：以 2π/岩石数 等分角度在主人位置生成岩石，每颗初速为对应角度的 8 倍方向向量，
        /// 伤害沿用本弹幕的 originalDamage；yeetRocks：把主人名下所有岩石的 ai[0] 置 1，交给岩石自己进入甩出流程。
        /// 岩石数与类型按水晶增益分档（对齐 2.2.2）：水晶态 10 颗与 {1,3,4,5,6}（1/4 是更"厚"的岩石），
        /// 神器态 5 颗与 {3,5,6}
        /// </summary>
        private void HandleRocks(bool spawnRocks = false, bool yeetRocks = false)
        {
            if (spawnRocks)
            {
                // 召出岩石：水晶态 10 颗、神器态 5 颗；类型按同一档位取
                bool psc = Owner.GetModPlayer<CalamityDemutationPlayer>().profanedCrystalBuffs;
                int rockCount = psc ? 10 : 5;
                int[] validRockTypes = psc ? new int[] { 1, 3, 4, 5, 6 } : new int[] { 3, 5, 6 };
                float angleVariance = MathHelper.TwoPi / rockCount;
                float angle = 0f;
                for (int i = 0; i < rockCount; i++)
                {
                    int rockType = validRockTypes[Main.rand.Next(0, validRockTypes.Length)];
                    var rockyRoad = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Owner.position, angle.ToRotationVector2() * 8f, ModContent.ProjectileType<MiniGuardianRock>(), 1, 2f, Owner.whoAmI, 0f, angle, rockType);
                    rockyRoad.originalDamage = Projectile.originalDamage;
                    angle += angleVariance;
                }
            }
            else if (yeetRocks)
            {
                // 给主人名下所有岩石打上"甩出"标记（ai[0] = 1）
                int rock = ModContent.ProjectileType<MiniGuardianRock>();
                foreach (var proj in Main.projectile)
                {
                    if (proj.active && proj.owner == Owner.whoAmI && proj.type == rock)
                        proj.ai[0] = 1f;
                }
            }
        }
        /// <summary>
        /// AI：主人持有神器（profanedSoulArtifact）期间每帧续期实现常驻，神器消失或主人死亡则清标志并消散；
        /// 每帧按 shieldActive 的边沿决定召出 / 甩出岩石，边沿帧在主人周身喷一圈向内收束的神圣色粉尘；
        /// 有目标时悬停在"目标方向距主人 75 像素（护盾在场）或主人背后 50 像素（护盾消失）"处，无目标时漂浮跟随主人。
        /// </summary>
        public override void AI()
        {
            CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
            // 守护者标志置位期间持续续期，实现"神器在身即常驻"
            if (modPlayer.profanedSoulGuardians)
                Projectile.timeLeft = 2;
            // 神器消失 / 主人死亡：清掉守护者标志并让本弹幕消散
            if (!modPlayer.profanedSoulArtifact || Owner.dead || !Owner.active)
            {
                modPlayer.profanedSoulGuardians = false;
                Projectile.active = false;
                return;
            }
            // 4 帧循环动画（每 6 帧切一帧）
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 6 % Main.projFrames[Type];
            var shieldIsActive = shieldActive; // 本帧只判定一次护盾状态，避免重复读取
            bool shouldSpawnRocks = !shieldActiveBefore && shieldIsActive;
            bool shouldYeetRocks = shieldActiveBefore && !shieldIsActive;
            HandleRocks(shouldSpawnRocks, shouldYeetRocks);
            bool shouldDust = shouldSpawnRocks || shouldYeetRocks;
            if (shouldDust)
            {
                // 20 颗粉尘自中心外一小段距离处向内收束；水晶形态的夜晚收束更快（6.9 倍）
                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustPos = new Vector2(Owner.Center.X + Main.rand.NextFloat(-10, 10), Owner.Center.Y + Main.rand.NextFloat(-10, 10));
                    Vector2 velocity = (Owner.Center - dustPos).SafeNormalize(Vector2.Zero);
                    velocity *= (Main.dayTime || !SpawnedFromPSC) ? 3f : 6.9f;
                    var dust = Dust.NewDustPerfect(Owner.Center, MiniGuardianHealer.HolyDustType(!Main.dayTime && SpawnedFromPSC), velocity, 0, default(Color), 2f);
                    if (!Main.dayTime && SpawnedFromPSC)
                        dust.noGravity = true;
                }
            }
            // 索敌（复刻 CalamityUtils.MinionHoming，见 MiniGuardianTargeting）：优先玩家右键锁定的目标，否则取 1500 像素内最近的敌人；
            // 本弹幕不直接造成伤害，索敌结果只用于决定悬停位置
            NPC potentialTarget = MiniGuardianTargeting.MinionHoming(Projectile.Center, 1500f, Owner);
            Vector2 playerDestination = Owner.Center - Projectile.Center;
            switch (AIState)
            {
                case MiniDefenderAIState.ShieldActive:
                case MiniDefenderAIState.ShieldInactive:
                    // 护盾消失期间持续在主人身上喷尘：每帧 2 次、每次 1/3 概率
                    if (AIState == MiniDefenderAIState.ShieldInactive)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if (!Main.rand.NextBool(3))
                                continue;
                            Dust dust = Dust.NewDustDirect(Owner.position, Owner.width, Owner.height, MiniGuardianHealer.HolyDustType(!Main.dayTime && SpawnedFromPSC));
                            dust.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                            dust.velocity.Y -= Main.rand.NextFloat(1f, 3f);
                            dust.scale = Main.rand.NextFloat(1.15f, 1.45f);
                            dust.noGravity = true;
                        }
                    }
                    if (potentialTarget != null)
                    {
                        // 悬停在目标方向：护盾在场时距主人 75 像素（原为水晶增益 125 / 神器 75），护盾消失时退到主人背后 50 像素
                        Vector2 angle = Owner.Center + Owner.SafeDirectionTo(potentialTarget.Center) * (shieldIsActive ? 75f : -50f);
                        playerDestination = angle;
                        playerDestination.X += Main.rand.NextFloat(-5f, 5f);
                        playerDestination.Y += Main.rand.NextFloat(-5f, 5f);
                    }
                    else
                    {
                        playerDestination.X += Main.rand.NextFloat(-10f, 10f) + (75f * (shieldIsActive ? Owner.direction : -Owner.direction));
                        playerDestination.Y += Main.rand.NextFloat(-10f, 10f);
                    }
                    break;
                case MiniDefenderAIState.Vanity:
                    playerDestination.X += Main.rand.NextFloat(-10f, 20f) - (60f * Owner.direction);
                    playerDestination.Y += Main.rand.NextFloat(-10f, 20f) - 60f;
                    break;
            }
            if (potentialTarget != null && AIState != MiniDefenderAIState.Vanity)
            {
                // 贴敌模式：以 40（近距离 28）为期望速度插值靠向目标点，靠近后整体减速
                float dist = Projectile.Center.Distance(playerDestination);
                float num543 = playerDestination.X;
                float num544 = playerDestination.Y;
                float num550 = 40f;
                Vector2 vector43 = Projectile.Center;
                float num551 = num543 - vector43.X;
                float num552 = num544 - vector43.Y;
                float num553 = (float)Math.Sqrt((double)(num551 * num551 + num552 * num552));
                if (num553 < 100f)
                {
                    num550 = 28f; //14
                }
                num553 = num550 / num553;
                num551 *= num553;
                num552 *= num553;
                Projectile.velocity.X = (Projectile.velocity.X * 14f + num551) / 13.5f;
                Projectile.velocity.Y = (Projectile.velocity.Y * 14f + num552) / 13.5f;
                Projectile.velocity *= dist > 10 ? 0.9f : 0.3f;
                Projectile.spriteDirection = Projectile.DirectionTo(potentialTarget.Center).X > 0 ? 1 : -1;
            }
            else
            {
                // 跟随模式：按距离分级调整加速度与回拉速度
                float playerDist = playerDestination.Length();
                float acceleration = 0.5f;
                float returnSpeed = 28f;
                // 距离过远（>2000）直接传送到主人身边
                if (playerDist > 2000f)
                {
                    Projectile.position = Owner.position;
                    Projectile.netUpdate = true;
                }
                // 贴近主人（<50）时大幅减速并阻尼速度，避免抖动
                else if (playerDist < 50f)
                {
                    acceleration = 0.01f;
                    if (Math.Abs(Projectile.velocity.X) > 2f || Math.Abs(Projectile.velocity.Y) > 2f)
                        Projectile.velocity *= 0.9f;
                }
                else
                {
                    // 距离越远加速度越大：<100 缓行、>300 全速
                    if (playerDist < 100f)
                        acceleration = 0.1f;
                    if (playerDist > 300f)
                        acceleration = 1f;
                    playerDist = returnSpeed / playerDist;
                    playerDestination *= playerDist;
                    // 逐分量朝目标分量提速/减速（减速侧通过二次加/减实现快速刹车）
                    if (Projectile.velocity.X < playerDestination.X)
                    {
                        Projectile.velocity.X += acceleration;
                        if (acceleration > 0.05f && Projectile.velocity.X < 0f)
                            Projectile.velocity.X += acceleration;
                    }
                    if (Projectile.velocity.X > playerDestination.X)
                    {
                        Projectile.velocity.X -= acceleration;
                        if (acceleration > 0.05f && Projectile.velocity.X > 0f)
                            Projectile.velocity.X -= acceleration;
                    }
                    if (Projectile.velocity.Y < playerDestination.Y)
                    {
                        Projectile.velocity.Y += acceleration;
                        if (acceleration > 0.05f && Projectile.velocity.Y < 0f)
                            Projectile.velocity.Y += acceleration * 2f;
                    }
                    if (Projectile.velocity.Y > playerDestination.Y)
                    {
                        Projectile.velocity.Y -= acceleration;
                        if (acceleration > 0.05f && Projectile.velocity.Y > 0f)
                            Projectile.velocity.Y -= acceleration * 2f;
                    }
                }
                // 朝向：横向速度足够大时按速度方向翻转
                if (Math.Abs(Projectile.velocity.X) > 0.2f)
                    Projectile.direction = Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
            }
            Projectile.netUpdate = Projectile.netUpdate || (shieldIsActive != shieldActiveBefore);
            shieldActiveBefore = shieldIsActive;
        }
        /// <summary>本体不造成伤害：输出完全由环绕岩石承担</summary>
        public override bool? CanDamage() => false;
        /// <summary>同步 shieldActiveBefore，避免客户端各自判定出不同的岩石生/甩边沿</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(shieldActiveBefore);
        }
        /// <summary>接收 shieldActiveBefore 同步值</summary>
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            shieldActiveBefore = reader.ReadBoolean();
        }
        /// <summary>
        /// 绘制：水晶形态（ai[0]==1 且非纯外观）时改画 4 帧残影。
        /// 原为 CalamityUtils.DrawAfterimagesCentered(..., armorShaderToUse: Owner.cMinion)；
        /// 本工程用 CDUtil.DrawAfterimages 等价替代，但不支持外观染料着色。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (SpawnedFromPSC && !ForcedVanity)
            {
                CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
                return false;
            }
            return true;
        }
    }
}
