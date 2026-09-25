using System;
using System.IO;
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
    /// 迷你神圣守卫·进攻者（移植自灾厄 2.2.2 的 MiniGuardianAttack）：亵渎灵魂神器召唤的三守卫之一。
    /// 本体可近身撞击（纯外观形态除外），并按下述阶段循环出手（每阶段 360 帧）：
    /// 长矛齐射（MiniGuardianSpear）→ 冲刺撞击 → 爆弹（MiniGuardianFireball）。
    /// 冲刺/长矛的实际伤害在 AI 里按召唤伤害实时乘算 originalDamage。
    /// 水晶鞭刃增益（ProfanedCrystalWhipBuff）生效时切到强化档，相关取值见 WhipBuffed 的说明。
    /// </summary>
    internal class MiniGuardianAttack:ModProjectile
    {
        // ── 状态与属性 ──
        /// <summary>进攻阶段：纯外观 / 神器形态跟随 / 长矛 / 冲刺 / 爆弹</summary>
        public enum MiniOffenseAIState
        {
            Vanity,
            Psa,
            Spears,
            Charges,
            Fireballs
        }
        /// <summary>弹幕主人：跟随、索敌与伤害乘算都以主人为参照</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>ai[0] == 1 表示由水晶形态召唤（装备水晶时由 CalamityDemutationPlayer 写入）</summary>
        public bool SpawnedFromPSC => Projectile.ai[0] == 1f;
        /// <summary>
        /// 水晶鞭刃增益 ProfanedCrystalWhipBuff 是否生效（对应灾厄的 owner.HasBuff&lt;ProfanedCrystalWhipBuff&gt;()）：
        /// 生效时三守护者的阶段时长（冲刺/爆弹 10 秒、长矛 8 秒，否则统统 6 秒）、长矛齐射数量与爆弹反冲都按强化档走。
        /// 由水晶鞭本体（ProfanedCrystalWhip）命中时给主人挂上
        /// </summary>
        private bool WhipBuffed => Owner.HasBuff<ProfanedCrystalWhipBuff>();
        /// <summary>阶段计时器（写入 ai[1] 并同步）：归零即切到下一阶段并重置为该阶段时长</summary>
        internal int phaseTimer
        {
            get => (int)Projectile.ai[1];
            set
            {
                Projectile.ai[1] = value;
                Projectile.netUpdate = true;
            }
        }
        /// <summary>出手间隔：冲刺阶段是冲刺节奏，长矛/爆弹阶段是开火冷却</summary>
        private int attackDelay = 0;
        /// <summary>当前阶段：由水晶形态召唤时读 ai[2]（夹在长矛~爆弹之间），否则恒为神器形态</summary>
        public MiniOffenseAIState getAiState => SpawnedFromPSC ?
            (MiniOffenseAIState)Math.Clamp(Projectile.ai[2], (int)MiniOffenseAIState.Spears, (int)MiniOffenseAIState.Fireballs) :
            MiniOffenseAIState.Psa;
        /// <summary>原为 SpawnedFromPSC &amp;&amp; !profanedCrystalBuffs；水晶未激活（profanedCrystalBuffs=false）时才按纯外观处理</summary>
        public bool ForcedVanity => SpawnedFromPSC && !Owner.GetModPlayer<CalamityDemutationPlayer>().profanedCrystalBuffs;   // 对应灾厄 SpawnedFromPSC && !profanedCrystalBuffs
        /// <summary>
        /// 阶段推进：水晶形态 / 神器形态直接锁定状态并提前返回（避免阶段计时器覆盖长矛的命中计数），
        /// 否则计时器归零时按枚举顺序切到下一阶段（越过爆弹则回到长矛），并按时长重置计时器。
        /// 时长按鞭刃增益取 10/8 秒档，未生效时为 6 秒档（WhipBuffed 说明同上）。
        /// </summary>
        public MiniOffenseAIState updateAiState(Player player, MiniOffenseAIState currentAIState)
        {
            MiniOffenseAIState? result = null;
            if (SpawnedFromPSC && ForcedVanity)
                result = MiniOffenseAIState.Vanity;
            else if (!SpawnedFromPSC)
                result = MiniOffenseAIState.Psa;
            if (result != null)
            {
                Projectile.ai[2] = (int)result;
                return (MiniOffenseAIState)result;
                // 提前返回：否则下面的阶段推进会把 ai[1] 覆盖掉，长矛阶段的命中计数就废了
            }
            int currentPhase = (int)currentAIState;
            if (phaseTimer <= 0)
            {
                currentPhase++;
                if (currentPhase > (int)MiniOffenseAIState.Fireballs) // 超出最后一个阶段（按枚举顺序）时回到长矛阶段
                    currentPhase = (int)MiniOffenseAIState.Spears;
                result = (MiniOffenseAIState)currentPhase;
                bool whip = WhipBuffed; // 原读 ProfanedCrystalWhipBuff
                int newPhaseTimer = result == MiniOffenseAIState.Charges ? 60 * (whip ? 10 : 6) : // 冲刺阶段：强化 10 秒 / 平时 6 秒
                    result == MiniOffenseAIState.Fireballs ? 60 * (whip ? 10 : 6) : // 爆弹阶段：强化 10 秒 / 平时 6 秒
                    60 * (whip ? 8 : 6); // 长矛阶段：强化 8 秒 / 平时 6 秒
                phaseTimer = newPhaseTimer;
            }
            else
            {
                result = (MiniOffenseAIState)currentAIState;
                phaseTimer--;
            }
            Projectile.ai[2] = (int)result;
            return (MiniOffenseAIState)result;
        }
        // ── 生命周期方法 ──
        /// <summary>注册 4 帧动画、登记 4 帧残影缓存，并标记为可牺牲、可右键锁定目标的召唤物</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }
        /// <summary>基础属性：60x88 碰撞箱、友方、无限穿透、不碰撞地形、每个敌人独立命中冷却</summary>
        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.width = 60;
            Projectile.height = 88;
            Projectile.minion = true;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 预测瞄准（移植自灾厄的 CalculatePredictiveAimToTarget，欧拉迭代 4 次估算目标未来位置）：
        /// 以目标当前速度按"到达耗时"迭代外推命中点，返回朝该点的 shootSpeed 倍单位速度。
        /// </summary>
        internal static Vector2 CalculatePredictiveAimToTarget(Vector2 startingPosition, Entity target, float shootSpeed, int iterations = 4)
        {
            float previousTimeToReachDestination = 0f;
            Vector2 currentTargetPosition = target.Center;
            for (int i = 0; i < iterations; i++)
            {
                float timeToReachDestination = Vector2.Distance(startingPosition, currentTargetPosition) / shootSpeed;
                currentTargetPosition += target.velocity * (timeToReachDestination - previousTimeToReachDestination);
                previousTimeToReachDestination = timeToReachDestination;
            }
            return (currentTargetPosition - startingPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;
        }
        /// <summary>预测式扑向目标（移植自灾厄的 SuperhomeTowardsTarget）：按 predictionStrength 缩放预测提前量后与惯性插值</summary>
        private Vector2 SuperhomeTowardsTarget(NPC target, float homingSpeed, float inertia, float predictionStrength = 1f)
        {
            if (predictionStrength < 0.01f)
                predictionStrength = 0.01f;
            Vector2 idealVelocity = CalculatePredictiveAimToTarget(Projectile.Center, target, homingSpeed / predictionStrength) * predictionStrength;
            return (Projectile.velocity * (inertia - 1f) + idealVelocity) / inertia;
        }
        /// <summary>
        /// 基础 AI：有目标且非纯外观时以"短距 28、常规 24"基准速度按惯性插值追目标
        /// （水晶形态速度翻倍、惯性更大）；否则在主人身边随机抖动漂浮跟随（超 2000 像素直接传送回主人身边）。
        /// </summary>
        private void BaseAI(NPC potentialTarget)
        {
            if (potentialTarget != null && !ForcedVanity)
            {
                Vector2 targetDestination = potentialTarget.Center - Projectile.Center;
                float targetDist = targetDestination.Length();
                // 水晶形态速度翻倍、惯性更大（原 SpawnedFromPSC ? 2f : 0.95f）
                float baseSpeed = (targetDist < 100f ? 28f : 24f) * (SpawnedFromPSC ? 2f : 0.95f);
                float inertia = SpawnedFromPSC ? 20f : 12f;
                targetDist = baseSpeed / targetDist;
                targetDestination *= targetDist;
                Projectile.velocity = (Projectile.velocity * inertia + targetDestination) / (inertia + 1f);
            }
            else
            {
                // 跟随模式：先把旋转角清零（避免延续冲刺阶段的朝向）
                Projectile.rotation = (float)(Math.Atan(0));
                Vector2 playerDestination = Owner.Center - Projectile.Center;
                playerDestination.X += Main.rand.NextFloat(-10f, 20f) - (60f * Owner.direction);
                playerDestination.Y += Main.rand.NextFloat(-10f, 20f) - 60f;
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
                }
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
        }
        /// <summary>
        /// 进阶 AI：长矛阶段悬停在目标上方 155~160 像素处放矛（冷却归零时先来一轮 5 发霰射），
        /// 爆弹阶段同样悬停并做"开火反冲"，冲刺阶段贴脸撞击（未强化时每 25 帧一次预测式扑击）。
        /// 原灾厄按鞭刃增益分成强化/未强化两条支路，本工程按 WhipBuffed（主人是否持有水晶鞭增益）取支路。
        /// </summary>
        public void AdvancedAI(NPC potentialTarget, Player owner, MiniOffenseAIState aiState)
        {
            bool buffedAi = WhipBuffed;
            Vector2 targetDestination = potentialTarget.Center - Projectile.Center;
            if (attackDelay > 0)
                attackDelay--;
            switch (aiState)
            {
                case MiniOffenseAIState.Charges:
                    break;
                case MiniOffenseAIState.Fireballs:
                    targetDestination.X += Main.rand.NextFloat(-5f, 5f);
                    targetDestination.Y += Main.rand.NextFloat(155f, 160f);
                    break;
                case MiniOffenseAIState.Spears:
                    targetDestination.X += Main.rand.NextFloat(-5f, 5f);
                    targetDestination.Y += Main.rand.NextFloat(155f, 160f);
                    break;
            }
            if (aiState != MiniOffenseAIState.Charges)
            {
                // 长矛/爆弹阶段：先在目标上方就位（速度翻倍、惯性 20）
                Projectile.rotation = (float)(Math.Atan(0));
                float targetDist = targetDestination.Length();
                float baseSpeed = (targetDist < 100f ? 28f : 24f) * 2f;
                float inertia = 20f;
                targetDist = baseSpeed / targetDist;
                targetDestination *= targetDist;
                Projectile.velocity = (Projectile.velocity * inertia + targetDestination) / (inertia + 1f);
                if (aiState == MiniOffenseAIState.Fireballs)
                {
                    // 圣光爆弹阶段（MiniGuardianFireball 已随本步移植，生成调用恢复）：
                    // 强化档 3 连发（-20 度起每发 +20 度）、平时单发，均带预测瞄准；两档都先把自身反向弹开作"开火反冲"。
                    if (attackDelay == 0)
                    {
                        if (buffedAi)
                        {
                            attackDelay = 100;
                            Projectile.velocity = Projectile.Center - potentialTarget.Center;
                            Projectile.velocity.Normalize();
                            Projectile.velocity *= 29f;
                            int shotCount = 3;
                            float spread = -20;
                            var velocity = CalculatePredictiveAimToTarget(Projectile.Center, potentialTarget, 28f);
                            for (int i = 0; i < shotCount; i++)
                            {
                                Vector2 perturbedspeed = new Vector2(velocity.X, velocity.Y).RotatedBy(MathHelper.ToRadians(spread));
                                SpawnMiniGuardianFireball(perturbedspeed, 1f);
                                spread += 20;
                            }
                        }
                        else
                        {
                            Projectile.velocity = Projectile.Center - potentialTarget.Center;
                            Projectile.velocity.Normalize();
                            Projectile.velocity *= 20f;
                            attackDelay = 75;
                            SpawnMiniGuardianFireball(CalculatePredictiveAimToTarget(Projectile.Center, potentialTarget, 25f), 0f);
                        }
                    }
                }
                else // 长矛阶段
                {
                    if (attackDelay % (buffedAi ? 6 : 8) == 0)
                    {
                        // 原为灾厄音效，此处沿用原版 SoundID.Item20（魔法射击）
                        SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
                        bool shouldShotGun = attackDelay == 0;
                        if (shouldShotGun)
                        {
                            // 霰射：5 发按扇形铺开，垂直方向错开 4 像素形成"矛排"；基础伤害为本弹幕的一半（乘算后覆写 originalDamage）
                            int numProj = 5;
                            var velocity = CalculatePredictiveAimToTarget(Projectile.Center, potentialTarget, buffedAi ? 28f : 20f);
                            int spread = buffedAi ? -10 : -20;
                            for (int i = 0; i < numProj; i++)
                            {
                                Vector2 perturbedspeed = velocity.RotatedBy(MathHelper.ToRadians(spread));
                                int separation = (i * 4) - 8;
                                int spearBaseDamage = (int)(Projectile.originalDamage * 0.5f);
                                int spearDamage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(spearBaseDamage);
                                int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y - separation, perturbedspeed.X, perturbedspeed.Y, ModContent.ProjectileType<MiniGuardianSpear>(), spearDamage, 1f, Projectile.owner, 1f, 1f);
                                if (Main.projectile.IndexInRange(proj))
                                {
                                    Main.projectile[proj].DamageType = DamageClass.Summon;
                                    Main.projectile[proj].originalDamage = spearBaseDamage;
                                }
                                spread += buffedAi ? 5 : 10;
                            }
                        }
                        else
                        {
                            int spearBaseDamage = Projectile.originalDamage;
                            int spearDamage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(spearBaseDamage);
                            var velocity = CalculatePredictiveAimToTarget(Projectile.Center, potentialTarget, buffedAi ? 28f : 20f);
                            int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<MiniGuardianSpear>(), spearDamage, 1f, Projectile.owner, 1f, 1f);
                            if (Main.projectile.IndexInRange(proj))
                            {
                                Main.projectile[proj].DamageType = DamageClass.Summon;
                                Main.projectile[proj].originalDamage = Projectile.originalDamage;
                            }
                        }
                    }
                    if (attackDelay == 0)
                        attackDelay = buffedAi ? 30 : 40;
                }
            }
            else
            {
                // 冲刺阶段：未强化时每 25 帧一次预测式扑击（把单帧攻击间隔当"蓄力帧"用）
                var shouldDrawDust = buffedAi || attackDelay > 0;
                if (buffedAi)
                {
                    // 强化冲刺：蓄力帧到点就滚一次追加长矛（对齐 2.2.2 的 rollBabSpears(1, true)），
                    // 之后沿当前朝向持续加速并缓慢转向目标
                    if (attackDelay <= 0)
                        owner.GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(1, true);
                    float distToTarget = Projectile.Distance(potentialTarget.Center) + .01f;   // 加 0.01 是防止下一步做除数时除零
                    Projectile.velocity = Projectile.rotation.ToRotationVector2() * (28f + (28f / (distToTarget * .01f)));
                    Projectile.velocity = Vector2.Clamp(Projectile.velocity, Vector2.One * -50f, Vector2.One * 50f);   // 冲刺速度封顶 ±50
                    // 原为 Projectile.rotation.AngleTowards(...)，本工程以 CDUtil.RotTowards 等价替代
                    Projectile.rotation = CDUtil.RotTowards(Projectile.rotation, Projectile.AngleTo(potentialTarget.Center), .001f * distToTarget);
                }
                else
                {
                    if (attackDelay == 24)
                    {
                        // 第 24 帧（即蓄力帧的最后一帧）才真正扑出：预测式瞄准目标后整体提速 1.369 倍
                        Projectile.velocity = SuperhomeTowardsTarget(potentialTarget, 35f, 1f);
                        Projectile.velocity *= 1.369f;
                    }
                }
                if (attackDelay <= 0)
                    attackDelay = buffedAi ? 20 : 25;
                if (shouldDrawDust)
                {
                    // 沿朝向在身体外缘喷 6 颗粉尘，方向以 ±20 度交替偏移；夜晚按 dayTime 压暗缩小
                    var shouldAdjust = !Main.dayTime && buffedAi;
                    int dustId = MiniGuardianHealer.HolyDustType(!Main.dayTime);
                    for (int i = 0; i < 6; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + (Projectile.Size / 2f).RotatedBy(Projectile.rotation), dustId);
                        dust.velocity = Projectile.velocity.RotatedBy(MathHelper.ToRadians(20f * (i % 2 == 0).ToDirectionInt()));
                        dust.noGravity = true;
                        dust.fadeIn = shouldAdjust ? 0.9f : 1.8f;
                        dust.scale = Main.dayTime ? dust.scale : 0.45f;
                    }
                }
            }
        }
        /// <summary>
        /// 生成一枚圣光爆弹：基础伤害取守卫自身的 originalDamage，折算后回写 originalDamage
        /// （漏写会让爆弹与其裂片被二次折算）；ai[0] 传给爆弹决定炸出几枚裂片（1 → 4 枚、0 → 8 枚）。
        /// </summary>
        private void SpawnMiniGuardianFireball(Vector2 velocity, float ai0)
        {
            int fireballBaseDamage = (int)Projectile.originalDamage;
            int fireballDamage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(fireballBaseDamage);
            int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<MiniGuardianFireball>(), fireballDamage, 1f, Projectile.owner, ai0);
            if (Main.projectile.IndexInRange(proj))
            {
                Main.projectile[proj].DamageType = DamageClass.Generic;
                Main.projectile[proj].originalDamage = fireballBaseDamage;
            }
        }
        // ── 覆写方法 ──
        /// <summary>
        /// AI：主人持有神器（profanedSoulArtifact）期间每帧续期实现常驻，神器消失或主人死亡则清标志并消散；
        /// 每帧按召唤伤害重算 damage 与命中冷却，按阶段选择基础 AI（跟随/贴敌）或进阶 AI（长矛/爆弹/冲刺）。
        /// </summary>
        public override void AI()
        {
            Player owner = Owner;
            CalamityDemutationPlayer modPlayer = owner.GetModPlayer<CalamityDemutationPlayer>();
            // 守护者标志置位期间持续续期，实现"神器在身即常驻"
            if (modPlayer.profanedSoulGuardians)
                Projectile.timeLeft = 2;
            // 神器消失 / 主人死亡：清掉守护者标志并让本弹幕消散
            if (!modPlayer.profanedSoulArtifact || owner.dead || !owner.active)
            {
                modPlayer.profanedSoulGuardians = false;
                Projectile.active = false;
                return;
            }
            // 伤害与命中冷却在此实时刷新（originalDamage 由召唤源写入）
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            Projectile.localNPCHitCooldown = SpawnedFromPSC ? 6 : 9;
            var currentAIState = getAiState;
            // 变身动画期间（profanedCrystalAnim != -1）强制切纯外观（对齐 2.2.2）：120 帧的变身过程里守护者不参战
            if (owner.GetModPlayer<CalamityDemutationPlayer>().profanedCrystalAnim != -1)
                currentAIState = MiniOffenseAIState.Vanity;
            var newAIState = updateAiState(owner, currentAIState);
            if (newAIState != currentAIState)
            {
                Projectile.netUpdate = true;
            }
            // 索敌（复刻 CalamityUtils.MinionHoming，见 MiniGuardianTargeting）：优先玩家右键锁定的目标，否则取 3000 像素内最近的敌人
            NPC potentialTarget = MiniGuardianTargeting.MinionHoming(Projectile.Center, 3000f, owner);
            switch (currentAIState)
            {
                case MiniOffenseAIState.Vanity:
                case MiniOffenseAIState.Psa:
                    BaseAI(potentialTarget);
                    break;
                default:
                    if (potentialTarget != null && !ForcedVanity)
                        AdvancedAI(potentialTarget, owner, newAIState);
                    else
                        BaseAI(null);
                    break;
            }
            // 朝向：横向速度足够大时按速度方向翻转
            if (Math.Abs(Projectile.velocity.X) > 0.2f)
                Projectile.direction = Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
            // 4 帧循环动画（每 6 帧切一帧）
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 6 % Main.projFrames[Type];
        }
        /// <summary>纯外观形态（水晶形态召唤）不造成伤害</summary>
        public override bool? CanDamage() => !ForcedVanity;
        /// <summary>
        /// 命中敌人（对齐 2.2.2）：神器形态下 ai[1]（既是阶段计时器，也当作命中计数）按"减 1、减到负值重置为 15"参与节奏，
        /// 且 ai[1] 归零那一帧滚一次追加长矛；水晶形态**不做这个计数**（否则每次命中都会削水晶档 ai[1] 的投矛计时 480），
        /// 只在冲刺阶段且没有水晶鞭增益时滚一次长矛。
        /// 原灾厄此处还有"天使同盟追加 BanishingFire 减益"，属另一套未移植体系，已删除。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
            if (!modPlayer.profanedCrystal)
            {
                if (Projectile.ai[1] == 0f)
                    modPlayer.rollBabSpears(1, target.chaseable);
                Projectile.ai[1] -= 1f;
                if (Projectile.ai[1] < 0f)
                    Projectile.ai[1] = 15;
            }
            else if (getAiState == MiniOffenseAIState.Charges && !WhipBuffed)
                modPlayer.rollBabSpears(1, true);
        }
        /// <summary>命中玩家（PvP）：与命中敌人同构，只有 ai 形态判定取"可追击恒真"</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
            if (!modPlayer.profanedCrystal)
            {
                if (Projectile.ai[1] == 0f)
                    modPlayer.rollBabSpears(1, true);
                Projectile.ai[1] -= 1f;
                if (Projectile.ai[1] < 0f)
                    Projectile.ai[1] = 15;
            }
            else if (getAiState == MiniOffenseAIState.Charges && !WhipBuffed)
                modPlayer.rollBabSpears(1, true);
        }
        /// <summary>
        /// 绘制：水晶形态（ai[0]==1 且非纯外观）时改画 4 帧残影。
        /// 原为 CalamityUtils.DrawAfterimagesCentered(..., armorShaderToUse: Owner.cMinion)；
        /// 本工程用 CDUtil.DrawAfterimages 等价替代，但不支持外观染料着色。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (!ForcedVanity && SpawnedFromPSC)
            {
                CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
                return false;
            }
            return true;
        }
        // ── 网络同步 ──
        /// <summary>接收 attackDelay 同步值</summary>
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            attackDelay = reader.ReadInt32();
        }
        /// <summary>同步 attackDelay，避免客户端各自算出不同的出手时机</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(attackDelay);
        }
    }
}
