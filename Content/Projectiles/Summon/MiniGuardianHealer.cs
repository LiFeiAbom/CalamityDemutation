using System;
using System.IO;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
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
    /// 迷你神圣守卫·治愈者（移植自灾厄 2.2.2 的 MiniGuardianHealer）：亵渎灵魂神器召唤的三守卫之一，
    /// 自身没有伤害判定（CanDamage 恒 false），平时漂浮跟随主人；处于强化形态且主人为本地玩家时，
    /// 每 180 帧向目标抛出 3 圈共 90 颗环形星弹（MiniGuardianStars），
    /// 每 1200 帧向目标方向两侧各发射一道神圣射线（MiniGuardianHolyRay）。
    /// </summary>
    internal class MiniGuardianHealer:ModProjectile
    {
        // ── 常量与字段 ──
        /// <summary>星弹齐射冷却（帧）：归零那一帧抛出三圈共 90 颗星弹并重置</summary>
        internal const int starTimer = 180;
        /// <summary>神圣射线冷却（帧）：归零那一帧朝目标两侧各发一道射线并重置</summary>
        internal const int laserTimer = 1200;
        /// <summary>出生时是否已把两个冷却写入 ai[1]（星弹）/ ai[2]（射线）</summary>
        private bool hasSetTimers;
        // ── 状态与属性 ──
        /// <summary>弹幕主人：跟随与攻击基准都以主人为参照</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>ai[0] == 1 表示由水晶形态召唤（装备水晶时由 CalamityDemutationPlayer 写入）</summary>
        public bool SpawnedFromPSC => Projectile.ai[0] == 1f;
        /// <summary>原为 SpawnedFromPSC &amp;&amp; !profanedCrystalBuffs；水晶未激活（profanedCrystalBuffs=false）时才按纯外观处理</summary>
        public bool ForcedVanity => SpawnedFromPSC && !Owner.GetModPlayer<CalamityDemutationPlayer>().profanedCrystalBuffs;   // 对应灾厄 SpawnedFromPSC && !profanedCrystalBuffs
        /// <summary>
        /// 是否处于强化形态（对齐 2.2.2：玩家水晶四态 == Empowered，即装备水晶且没有其它仆从/哨兵）。
        /// 神器档的 pscState 恒为 Vanity，故神器形态下不成立——强化档的星弹齐射与神圣射线都只看这一条
        /// </summary>
        public bool isEmpowered => Owner.GetModPlayer<CalamityDemutationPlayer>().pscState == (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Empowered;
        /// <summary>
        /// 神圣色粉尘类型：白天金色火焰（原为灾厄自定义粉尘 CalamityDusts.ProfanedFire，此处用原版 DustID.GoldFlame），
        /// 夜晚蓝色火焰（原为 CalamityDusts.Nightwither，此处用原版 DustID.BlueTorch）。
        /// </summary>
        internal static int HolyDustType(bool night) => night ? DustID.BlueTorch : DustID.GoldFlame;
        /// <summary>
        /// 水晶鞭刃增益 ProfanedCrystalWhipBuff 是否生效（对应灾厄的 owner.HasBuff&lt;ProfanedCrystalWhipBuff&gt;()）。
        /// 生效时治疗守护者才会发射神圣射线（另见 AI 里的 canDoLaser / 射线冷却）。
        /// 由水晶鞭本体（ProfanedCrystalWhip）命中时给主人挂上
        /// </summary>
        private bool WhipBuffed => Owner.HasBuff<ProfanedCrystalWhipBuff>();
        /// <summary>
        /// 神圣配色：直接转发亵渎之魂四态配色 GetColorForPsc（对齐 2.2.2 的调用口径）——
        /// 神器档 pscState 为 Vanity（暖橙），水晶档按当前四态取色
        /// </summary>
        internal static Color PscColor(int pscState, bool day) => ProfanedSoulCrystal.GetColorForPsc(pscState, day);
        // ── 生命周期方法 ──
        /// <summary>注册 4 帧动画，标记为可牺牲、可右键锁定目标的召唤物，并登记 4 帧残影缓存</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }
        /// <summary>基础属性：68x82 碰撞箱、友方、不占召唤栏、无限穿透、存活约 25 分钟（18000×5 帧）、不碰撞地形</summary>
        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.width = 68;
            Projectile.height = 82;
            Projectile.friendly = true;
            Projectile.minionSlots = 0f;
            Projectile.minion = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.timeLeft *= 5;
        }
        // ── 覆写方法 ──
        /// <summary>能否砍草：只在神器形态下允许，水晶形态不许（对齐 2.2.2 的 pSoulArtifact &amp;&amp; !profanedCrystal）</summary>
        public override bool? CanCutTiles()
        {
            CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
            if (!modPlayer.profanedSoulArtifact || modPlayer.profanedCrystal)
                return false;
            return null;
        }
        /// <summary>
        /// AI：主人持有神器（profanedSoulArtifact）期间每帧续期实现常驻，神器消失或主人死亡则消散；
        /// 无目标时以"减速+回拉"的漂浮方式跟随主人（超 2000 像素直接传送回主人身边），
        /// 有目标时悬停在目标上方，并按 ai[1]/ai[2] 两个冷却分别释放星弹与神圣射线。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 守护者标志置位期间持续续期，实现"神器在身即常驻"
            if (modPlayer.profanedSoulGuardians)
            {
                Projectile.timeLeft = 2;
            }
            // 神器消失 / 主人死亡：清掉守护者标志并让本弹幕消散
            if (!modPlayer.profanedSoulArtifact || player.dead || !player.active)
            {
                modPlayer.profanedSoulGuardians = false;
                Projectile.active = false;
                return;
            }
            // 出生时写入两个冷却并同步给其他客户端（在 AI 首帧完成，避免中途加入的客户端计时错乱）
            if (!hasSetTimers)
            {
                Projectile.ai[1] = starTimer;
                Projectile.ai[2] = laserTimer;
                hasSetTimers = true;
                Projectile.netUpdate = true;
            }
            // 反堆叠：与同主人的同类守卫靠太近时互相推开（替代灾厄的 MinionAntiClump 扩展方法，逻辑同原版仆从模板）
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (i != Projectile.whoAmI && Main.projectile[i].active && Main.projectile[i].owner == Projectile.owner
                    && Main.projectile[i].type == Projectile.type
                    && Math.Abs(Projectile.position.X - Main.projectile[i].position.X) + Math.Abs(Projectile.position.Y - Main.projectile[i].position.Y) < Projectile.width)
                {
                    Projectile.velocity.X += Projectile.position.X < Main.projectile[i].position.X ? -0.05f : 0.05f;
                    Projectile.velocity.Y += Projectile.position.Y < Main.projectile[i].position.Y ? -0.05f : 0.05f;
                }
            }
            Player owner = Owner;
            Vector2 playerDestination = owner.Center - Projectile.Center;
            bool empowered = isEmpowered;
            // 4 帧循环动画（每 6 帧切一帧）
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
            NPC target = null;
            // 只有主人本机参与索敌与生成，否则每个客户端都会生成一份星弹/射线（原注释如此）
            if (empowered && player.whoAmI == Main.myPlayer)
            {
                // 原为 CalamityUtils.MinionHoming：优先玩家右键锁定的目标，否则取 2000 像素内最近的敌人
                target = player.HasMinionAttackTargetNPC ? Main.npc[player.MinionAttackTargetNPC] : MiniGuardianTargeting.MinionHoming(Projectile.Center, 2000f, player);
                if (target != null)
                {
                    Projectile.spriteDirection = Projectile.DirectionTo(owner.Center).X > 0 ? 1 : -1;
                    if (Projectile.ai[1] <= 0)
                    {
                        // 原为灾厄音效，此处沿用原版 SoundID.DD2_BetsyFireballImpact（火球爆裂）
                        SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.Center);
                        // 三圈星弹：每圈 30 颗；偶数圈初速度带斜向分量，形成内外两层的环形散布
                        int totalFlameProjectiles = 30;
                        int totalRings = 3;
                        double radians = MathHelper.TwoPi / totalFlameProjectiles;
                        double angleA = radians * 0.5;
                        double angleB = MathHelper.ToRadians(90f) - angleA;
                        for (int i = 0; i < totalRings; i++)
                        {
                            bool firstRing = i % 2 == 0;
                            float starVelocity = i + 2f;
                            float velocityX = (float)(starVelocity * Math.Sin(angleA) / Math.Sin(angleB));
                            Vector2 spinningPoint = firstRing ? new Vector2(-velocityX, -starVelocity) : new Vector2(0f, -starVelocity);
                            for (int j = 0; j < totalFlameProjectiles; j++)
                            {
                                Vector2 vector2 = spinningPoint.RotatedBy(radians * j);
                                int type = ModContent.ProjectileType<MiniGuardianStars>();
                                int dmgAmt = Projectile.originalDamage;
                                var star = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, vector2 * 2.5f, type, dmgAmt, 0f, Main.myPlayer);
                                star.originalDamage = Projectile.originalDamage;
                                // 每颗星弹拖 3 颗神圣色粉尘，并额外复制一颗半尺寸白色粉尘
                                Color dustColor = PscColor(player.GetModPlayer<CalamityDemutationPlayer>().pscState, Main.dayTime);
                                dustColor.A = 255;
                                int maxDust = 3;
                                for (int k = 0; k < maxDust; k++)
                                {
                                    // 原代码用已废弃的 Dust.BetterCloneDust 复制粉尘，此处用等价的 NewDustPerfect 内联实现
                                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, vector2 * starVelocity * (k * 0.5f + 1f), 0, dustColor, 1f + k);
                                    dust.noGravity = true;
                                    dust.fadeIn = Main.rand.NextFloat() * 2f;
                                    // 复制体：位置/速度相同，尺寸与 fadeIn 减半，颜色改为纯白
                                    Dust clone = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, vector2 * starVelocity * (k * 0.5f + 1f), 0, new Color(255, 255, 255, 255), (1f + k) / 2f);
                                    clone.noGravity = true;
                                    clone.fadeIn = dust.fadeIn / 2f;
                                }
                            }
                        }
                        Projectile.ai[1] = starTimer;
                    }
                    // 水晶鞭增益门控：只有持有 ProfanedCrystalWhipBuff 时才发神圣射线（见 WhipBuffed）
                    bool canDoLaser = WhipBuffed;
                    if (canDoLaser && Projectile.ai[2] <= 0)
                    {
                        // 原为灾厄音效 Providence.HolyRaySound，此处用原版 SoundID.Item12（激光步枪）替代
                        SoundEngine.PlaySound(SoundID.Item12, player.Center);
                        float rotation = 435f;
                        Vector2 velocity = target.Center - player.Center;
                        velocity.Normalize();
                        float beamDirection = -1f;
                        if (velocity.X < 0f)
                            beamDirection = 1f;
                        int holyLaserDamage = Projectile.originalDamage * 5;
                        // 以目标方向旋转 90 度作为第一束射线的方向
                        velocity = velocity.RotatedBy(-(double)beamDirection * MathHelper.TwoPi / 4f);
                        int projectile = Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center.X, player.Center.Y, velocity.X, velocity.Y, ModContent.ProjectileType<MiniGuardianHolyRay>(), holyLaserDamage, 0f, Main.myPlayer, beamDirection * MathHelper.TwoPi / rotation, player.whoAmI);
                        if (Main.projectile.IndexInRange(projectile))
                            Main.projectile[projectile].originalDamage = holyLaserDamage;
                        // 与第一束成对称的另一束（速度反向、ai[0] 取反）
                        projectile = Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center.X, player.Center.Y, -velocity.X, -velocity.Y, ModContent.ProjectileType<MiniGuardianHolyRay>(), holyLaserDamage, 0f, Main.myPlayer, -beamDirection * MathHelper.TwoPi / rotation, player.whoAmI);
                        if (Main.projectile.IndexInRange(projectile))
                            Main.projectile[projectile].originalDamage = holyLaserDamage;
                        Projectile.ai[2] = laserTimer;
                    }
                    Projectile.ai[1]--;
                    if (canDoLaser)
                        Projectile.ai[2]--;
                }
            }
            if (target == null)
            {
                // 无目标：在主人身边随机抖动漂浮，按距离分级调整加速度与回拉速度
                playerDestination.X += Main.rand.NextFloat(-5f, 5f);
                playerDestination.Y += Main.rand.NextFloat(-10f, 10f);
                float playerDist = playerDestination.Length();
                float acceleration = 0.5f;
                float returnSpeed = 28f;
                // 距离过远（>2000）直接传送到主人身边
                if (playerDist > 2000f)
                {
                    Projectile.position = owner.position;
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
            else
            {
                // 射线冷却仅剩 120 帧以内时回撤到主人身上（贴到主人时直接清零冷却），否则悬停在目标上方 155~160 像素
                if (Projectile.ai[2] <= 120f)
                {
                    playerDestination = player.Center;
                    if (Projectile.Hitbox.Intersects(player.Hitbox))
                        Projectile.ai[2] = 0f;
                }
                else
                {
                    playerDestination = target.Center;
                    playerDestination.X += Main.rand.NextFloat(-5f, 5f);
                    playerDestination.Y += Main.rand.NextFloat(-155f, -160f);
                }
                // 以 40（近距离时 28）为期望速度向目标点插值，并按距离做额外阻尼
                float dist = Vector2.Distance(Projectile.Center, playerDestination);
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
                if (Projectile.ai[2] <= 120f)
                    Projectile.velocity *= dist > 10 ? MathHelper.SmoothStep(0.65f, 0.95f, 120f - Utils.GetLerpValue(120, Projectile.ai[2], 60)) : 0.1f;
                else
                    Projectile.velocity *= dist > 10 ? 0.9f : 0.3f;
            }
            // 射线即将就绪（ai[2] ≤ 120）且持有水晶鞭增益时，主人周身环绕一圈神圣色粉尘充能
            if (Projectile.ai[2] <= 120f && WhipBuffed)
            {
                int dustCount = (int)Math.Round(MathHelper.SmoothStep(1f, 5f, Projectile.ai[2] / 120));
                float outwardness = MathHelper.SmoothStep(40f, 60f, Projectile.ai[2] / 120);
                float dustScale = MathHelper.Lerp(1.15f, 1.425f, Projectile.ai[2] / 120);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 spawnPosition = player.Center + Main.rand.NextVector2Unit() * outwardness * Main.rand.NextFloat(0.75f, 1.1f);
                    Vector2 dustVelocity = (player.Center - spawnPosition) * 0.085f + owner.velocity;
                    Dust dust = Dust.NewDustPerfect(spawnPosition, HolyDustType(!Main.dayTime));
                    dust.velocity = dustVelocity;
                    dust.scale = dustScale * Main.rand.NextFloat(0.75f, 1.15f);
                    dust.color = Color.Lerp(Color.LightCoral, Color.White, Projectile.ai[2] / 120 * Main.rand.NextFloat(0.65f, 1f));
                    dust.noGravity = true;
                    dust.noLight = true;
                }
            }
        }
        /// <summary>
        /// 绘制：水晶形态（ai[0]==1）且处于强化形态时改画 4 帧残影。
        /// 原为 CalamityUtils.DrawAfterimagesCentered(..., armorShaderToUse: Owner.cMinion)；
        /// 本工程用 CDUtil.DrawAfterimages 等价替代，但不支持外观染料着色。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (SpawnedFromPSC && !ForcedVanity && isEmpowered)
            {
                CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
                return false;
            }
            return true;
        }
        /// <summary>本体不造成伤害：星弹与射线才是输出手段</summary>
        public override bool? CanDamage() => false;
        // ── 网络同步 ──
        /// <summary>同步 hasSetTimers，避免中途加入的客户端重复初始化冷却</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(hasSetTimers);
        }
        /// <summary>接收 hasSetTimers 同步值</summary>
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            hasSetTimers = reader.ReadBoolean();
        }
    }
}
