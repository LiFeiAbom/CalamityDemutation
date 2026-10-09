using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 古冰晶召唤的冰灵（照灾厄 2.0.3.9 <c>Projectiles/Summon/IceClasperMinion.cs</c> 移植）。
    /// 62×62 碰撞箱、占 **1 格**召唤栏、6 帧动画、寒气伤害（`coldDamage`），两状态机：
    /// **跟随**（离主人过远就回、远得离谱直接瞬移；有目标时——玩家离目标够近就换冲撞，
    /// 否则每 80 帧朝目标喷一枚伤害 ×1.5 的冰锥 <see cref="IceClasperSummonProjectile"/>）
    /// 与**冲撞**（朝自身朝向猛扑，速度随到目标的距离自适应、上限 25）。**只有冲撞态才有接触伤害**。
    /// </summary>
    /// <remarks>
    /// 源用灾厄基类 <c>BaseMinionProjectile</c>，本工程不引它——把基类那套（逐敌冷却、索敌、挂增益、
    /// 标志续命、逐帧动画）按原行为内联在本文件里。源里那句
    /// `Projectile.Calamity().overridesMinionDamagePrevention` 按其默认参数算出来就是"不覆盖"（false），
    /// 即不改变任何东西，故未移植。
    /// </remarks>
    internal class IceClasperMinion:ModProjectile
    {
        /// <summary>两状态：跟随（含喷冰锥）与冲撞</summary>
        public enum AIState { Follow, Ram }
        /// <summary>状态存在 <c>ai[0]</c>，切换时立刻发一次网络同步（照源写法）</summary>
        public AIState State
        {
            get => (AIState)Projectile.ai[0];
            set
            {
                Projectile.ai[0] = (int)value;
                SyncVariables();
            }
        }
        /// <summary>喷冰锥的计时（<c>ai[1]</c>，照源）</summary>
        public ref float TimerForShooting => ref Projectile.ai[1];
        /// <summary>残影渐显/渐隐插值（<c>localAI[0]</c>，照源）</summary>
        public ref float AfterimageInterpolant => ref Projectile.localAI[0];
        /// <summary>主人</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>主人的本模组玩家实例（读写 <c>iceClasperBool</c> 标志）</summary>
        public CalamityDemutationPlayer ModdedOwner => Owner.GetModPlayer<CalamityDemutationPlayer>();
        /// <summary>当前目标（每帧索敌刷新，照源基类的 Target 属性）</summary>
        public NPC Target { get; set; }

        /// <summary>
        /// 6 帧动画；可牺牲、可被右键标记目标；残影缓存 6 格（模式 2）；
        /// 屏外绘制宽容度取索敌半径 1200（都照源基类的设置值）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
        }
        /// <summary>基础属性：照源（62×62、占 1 栏、无限穿透、不撞地形、逐敌独立冷却、寒气伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 62;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.netImportant = true;
            Projectile.coldDamage = true;
        }
        /// <summary>内联的基类四步（逐敌冷却 → 索敌 → 挂增益/续命 → 逐帧动画）+ 两状态机 + 防扎堆</summary>
        public override void AI()
        {
            Projectile.localNPCHitCooldown = AncientIceChunk.IFrames * Projectile.MaxUpdates;
            // 索敌：没目标时用较近的 960、有目标时放宽到 1200；照源基类**以主人为圆心**、且不查视线
            Target = Owner.Center.MinionHoming(Target is null ? 960f : 1200f, Owner, true);

            Owner.AddBuff(ModContent.BuffType<IceClasperBuff>(), 2);
            if (Owner.dead)
                ModdedOwner.iceClasperBool = false;
            if (ModdedOwner.iceClasperBool)
                Projectile.timeLeft = 2;

            // 6 帧动画：每 5 帧（× MaxUpdates）翻一帧
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5 * Projectile.MaxUpdates)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            switch (State)
            {
                case AIState.Follow:
                    FollowState();
                    break;
                case AIState.Ram:
                    RamState();
                    break;
            }

            // 同类冰灵互相推开（源用 0.5 的推力）
            Projectile.MinionAntiClump(0.5f);

            if (!Main.dedServ)
            {
                // 时不时吐一点冰蓝幽灵尘（源写裸值 56，即 DustID.BlueFairy）
                if (Main.rand.NextBool(10))
                {
                    Dust ghostDust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueFairy, -Projectile.rotation.ToRotationVector2().RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 3f));
                    ghostDust.customData = false;
                    ghostDust.noLight = true;
                    ghostDust.noLightEmittence = true;
                }

                Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3());
            }
        }

        #region 状态机

        /// <summary>
        /// 跟随态：离主人超过 400 就朝主人加速回、超过 1200 直接瞬移；
        /// 有目标时——玩家离目标 250 以内就换冲撞，否则喷冰锥；朝向朝目标（没目标就朝速度方向）平滑转
        /// </summary>
        public void FollowState()
        {
            // 离主人过远：朝主人加速回（源写法：速度与"指向主人"合成后再 ×0.9）
            if (Vector2.Distance(Projectile.Center, Owner.Center) > AncientIceChunk.MaxDistanceFromOwner)
            {
                Projectile.velocity = (Projectile.velocity + Projectile.SafeDirectionTo(Owner.Center)) * 0.9f;
                SyncVariables();
            }
            // 远得离谱：直接瞬移到主人身上
            else if (Vector2.Distance(Projectile.Center, Owner.Center) > 1200f)
            {
                Projectile.Center = Owner.Center;
                SyncVariables();
            }

            if (Target is not null)
            {
                // 注意判的是"玩家"与目标的距离（不是冰灵的），照源
                if (Vector2.Distance(Owner.Center, Target.Center) <= AncientIceChunk.DistanceToDash)
                    State = AIState.Ram;
                else
                    ShootTarget();

                Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.AngleTo(Target.Center), .15f);
            }
            else
                Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.velocity.ToRotation(), .15f);
        }

        /// <summary>
        /// 冲撞态：主人离冰灵仍在 800 以内且目标还在时，朝自身朝向猛扑
        ///（速度 = 12 + 12/距离系数，再夹到 ±25），朝向按距离系数缓慢咬住目标；否则退回跟随态
        /// </summary>
        public void RamState()
        {
            if (Target is not null && Vector2.Distance(Owner.Center, Projectile.Center) <= AncientIceChunk.DistanceToStopDash)
            {
                // 到目标的距离加个小数，免得为 0 把后面的除法弄崩
                float distanceToTarget = Vector2.Distance(Projectile.Center, Target.Center) + .01f;

                // 朝自己的朝向冲：离得近就加速、离得远就减速，免得绕着目标空转
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * (AncientIceChunk.MinVelocity + (12f / (distanceToTarget * .01f)));
                Projectile.velocity = Vector2.Clamp(Projectile.velocity, Vector2.One * -25f, Vector2.One * 25f);
                Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.AngleTo(Target.Center), .001f * distanceToTarget);
            }
            // 没目标了、或主人离冰灵太远：回到跟随态
            else
                State = AIState.Follow;
        }

        /// <summary>
        /// 每 80 帧朝目标喷一枚冰锥（速度 25 的预判弹道、伤害 ×1.5），并带一点后坐力；
        /// 生成只在主人端做（弹幕 AI 两端都会跑）
        /// </summary>
        public void ShootTarget()
        {
            ++TimerForShooting;
            if (TimerForShooting >= AncientIceChunk.TimeToShoot && Projectile.owner == Main.myPlayer)
            {
                Vector2 velocity = CDUtil.CalculatePredictiveAimToTarget(Projectile.Center, Target, 25f);

                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<IceClasperSummonProjectile>(),
                    (int)(Projectile.damage * AncientIceChunk.ProjectileDMGMultiplier),
                    Projectile.knockBack,
                    Projectile.owner);

                // 开火后坐：自己往后飘一点
                Projectile.velocity -= velocity * .1f;

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item28, Projectile.Center);

                TimerForShooting = 0f;
                SyncVariables();
            }
        }

        /// <summary>发一次网络同步；顺手把 netSpam 压回 9 以内，免得 tML 丢包（照源）</summary>
        public void SyncVariables()
        {
            Projectile.netUpdate = true;
            if (Projectile.netSpam >= 10)
                Projectile.netSpam = 9;
        }

        #endregion

        /// <summary>登场喷一圈冰尘（照源：45 颗、无重力、速度越快的颗粒越大）</summary>
        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;

            int dustAmount = 45;
            for (int dustIndex = 0; dustIndex < dustAmount; dustIndex++)
            {
                float angle = MathHelper.TwoPi / dustAmount * dustIndex;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                Dust spawnDust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueFairy, velocity);
                spawnDust.customData = false;
                spawnDust.noGravity = true;
                spawnDust.velocity *= .75f;
                spawnDust.scale = velocity.Length() * .2f;
            }
        }

        /// <summary>只有冲撞态才有接触伤害（照源；返回 null = 用默认判定，false = 这一帧不打伤害）</summary>
        public override bool? CanDamage() => (State == AIState.Ram) ? null : false;

        /// <summary>
        /// 自绘：6 帧动画贴图按 `rotation - π/2` 对齐旋转，另加一层蓝色的位置残影
        ///（残影的渐显/渐隐插值：有目标且玩家离得近、或正在冲撞时淡入，否则淡出；
        /// 残影本身受性能开关 `ConfigSystem.PerformanceMode` 控制——照源用灾厄残影总开关的写法）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            AfterimageInterpolant += ((Target is not null && Vector2.Distance(Owner.Center, Target.Center) <= 450f) || State == AIState.Ram) ? .05f : -.05f;
            AfterimageInterpolant = MathHelper.Clamp(AfterimageInterpolant, 0f, 1f);
            float afterimageFade = MathHelper.Lerp(0f, 1f, AfterimageInterpolant);

            if (ConfigSystem.Instance?.PerformanceMode != true)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Color afterimageDrawColor = new Color(0.05f, 0.33f, 0.63f) with { A = 25 } * Projectile.Opacity * (1f - i / (float)Projectile.oldPos.Length) * afterimageFade;
                    Vector2 afterimageDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(texture, afterimageDrawPosition, frame, afterimageDrawColor, Projectile.rotation - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
