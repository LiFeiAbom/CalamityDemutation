using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Players;
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
    /// 灾厄挽歌召唤的**小鱿鱼**（照灾厄 2.0.3.9 <c>Projectiles/Summon/CalamarisLamentMinion.cs</c> 移植）：
    /// 31×31 判定（源注释：取一帧的一半，缠上去时看着更合身）、占 **1 格**召唤栏、5 帧动画。
    /// 三状态机——**待机**（在主人 320 像素内飘、>1200 直接瞬移）→ **远程**（离主人太远就使劲往回贴，
    /// 每 30 帧朝目标喷一枚强追踪的墨汁弹，带随机后坐与喷墨尘）→ **缠斗**（玩家离目标 400 以内切过去，
    /// 贴到目标碰撞箱上、伤害 ×1.25）；只有缠斗态才有接触伤害。
    /// </summary>
    /// <remarks>
    /// 源继承灾厄的 <c>BaseMinionProjectile</c>，本工程照旧把那套内联在本文件里：逐敌冷却 =
    /// `IFrames × MaxUpdates`、<c>MinionHoming</c> 索敌（无目标 960 / 有目标 **8000**——源注释说写这么大
    /// 是为了能稳定咬住神明吞噬者）、挂增益 + 标志续命、逐帧动画。
    /// 一处未移植：源里 `PreventTargettingUntilTargetHit = false` 会去设灾厄的 `overridesMinionDamagePrevention`
    /// （让仆从能打深渊/沉海那类"必须先被打中才吃伤害"的灾厄敌怪）；本工程不引灾厄类型，故这条效果缺失
    /// （对原版敌怪无影响）。
    /// </remarks>
    internal class CalamarisLamentMinion:ModProjectile
    {
        /// <summary>三状态：待机 / 远程 / 缠斗</summary>
        public enum AIState { Idle, Shooting, Latching }
        /// <summary>开火计时（`ai[0]`，照源）</summary>
        public ref float ShootingTimer => ref Projectile.ai[0];
        /// <summary>状态（`ai[1]`，照源）</summary>
        public AIState State { get => (AIState)Projectile.ai[1]; set => Projectile.ai[1] = (int)value; }
        /// <summary>主人</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>主人的本模组玩家实例（读写 <c>calamarisLament</c> 标志）</summary>
        public CalamityDemutationPlayer ModdedOwner => Owner.GetModPlayer<CalamityDemutationPlayer>();
        /// <summary>当前目标（每帧索敌刷新，照源基类的 Target 属性）</summary>
        public NPC Target { get; set; }

        /// <summary>5 帧动画；可牺牲、可被右键标记目标；残影与屏外宽容度照源基类设置</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)CalamarisLament.EnemyDistanceDetection;
        }
        /// <summary>基础属性：照源（31×31、占 1 栏、无限穿透、不撞地形、穿水、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 31;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.netImportant = true;
        }
        /// <summary>内联的基类四步（逐敌冷却 → 索敌 → 挂增益/续命 → 逐帧动画）+ 三状态机 + 环境音</summary>
        public override void AI()
        {
            Projectile.localNPCHitCooldown = CalamarisLament.LatchingIFrames * Projectile.MaxUpdates;
            Target = Owner.Center.MinionHoming(Target is null ? 960f : CalamarisLament.EnemyDistanceDetection, Owner, true);

            Owner.AddBuff(ModContent.BuffType<CalamarisLamentBuff>(), 2);
            if (Owner.dead)
                ModdedOwner.calamarisLament = false;
            if (ModdedOwner.calamarisLament)
                Projectile.timeLeft = 2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5 * Projectile.MaxUpdates)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            // 环境音：1/600 咕嘟声（两种随机），另有 1/200 概率串成一声怪鸟叫（照源）
            if (Main.rand.NextBool(600))
            {
                SoundStyle glubNoise = Main.rand.NextBool() ? SoundID.Zombie35 : SoundID.Zombie34;
                SoundStyle trollBirdChirpingSound = SoundID.Zombie16;
                SoundEngine.PlaySound(Main.rand.NextBool(200) ? trollBirdChirpingSound : glubNoise, Projectile.Center);
            }

            switch (State)
            {
                case AIState.Idle:
                    IdleState();
                    break;
                case AIState.Shooting:
                    ShootingState();
                    break;
                case AIState.Latching:
                    LatchingState();
                    break;
            }
        }

        /// <summary>待机：在主人 320 内飘、>1200 瞬移；朝向按横向速度倾斜；有目标就切远程态</summary>
        private void IdleState()
        {
            if (Vector2.Distance(Projectile.Center, Owner.Center) > 320f)
            {
                Projectile.velocity = (Projectile.velocity + Projectile.SafeDirectionTo(Owner.Center)) * 0.9f;
                SyncVariables();
            }

            if (Vector2.Distance(Projectile.Center, Owner.Center) > 1200f)
            {
                Projectile.Center = Owner.Center;
                SyncVariables();
            }

            Projectile.rotation = Projectile.rotation.AngleTowards(MathHelper.ToRadians(Projectile.velocity.X * 2f), .1f);
            Projectile.MinionAntiClump(0.5f);

            if (Target is not null)
                SwitchState(AIState.Shooting);
        }

        /// <summary>远程：离主人太远就使劲回贴；每 30 帧喷一枚墨汁弹（带随机散布、后坐与喷墨尘）</summary>
        private void ShootingState()
        {
            if (Target is not null)
            {
                Vector2 toTargetDirection = Projectile.SafeDirectionTo(Target.Center);

                if (Vector2.Distance(Projectile.Center, Owner.Center) > 320f)
                {
                    float inertia = 8f;
                    Projectile.velocity = (Projectile.velocity * inertia + Projectile.SafeDirectionTo(Owner.Center) * 25f) / (inertia + 1f);
                    SyncVariables();
                }

                // 随机让计时器偶尔多走一步，几只鱿鱼就不会整齐划一地开火（源注释：对平衡几乎无影响）
                ShootingTimer += Main.rand.NextBool(30) ? 2 : 1;
                if (ShootingTimer >= CalamarisLament.ShootingFireRate && Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (toTargetDirection * (CalamarisLament.ShootingProjectileSpeed + 10f)).RotatedByRandom(MathHelper.PiOver4), ModContent.ProjectileType<CalamarisLamentProjectile>(), Projectile.damage, 1f, Projectile.owner, Target.whoAmI);

                    // 开火后坐 + 喷墨尘（源写裸值 109，即 DustID.Asphalt）
                    Projectile.velocity -= toTargetDirection * 3f;
                    for (int i = 0; i < 15; i++)
                    {
                        Dust shootDust = Dust.NewDustPerfect(Projectile.Center + (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * Projectile.height / 2, DustID.Asphalt, toTargetDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 7f), Scale: Main.rand.NextFloat(0.5f, 1.5f), Alpha: 127);
                        shootDust.noGravity = true;
                    }

                    SoundEngine.PlaySound(SoundID.Item111, Projectile.Center);

                    ShootingTimer = 0f;
                    SyncVariables();
                }

                Projectile.rotation = Projectile.rotation.AngleTowards(toTargetDirection.ToRotation() - MathHelper.PiOver2, .1f);
                Projectile.MinionAntiClump(0.5f);

                if (Vector2.Distance(Owner.Center, Target.Center) <= CalamarisLament.LatchingDistanceRequired)
                    SwitchState(AIState.Latching);
            }
            else
                SwitchState(AIState.Idle);
        }

        /// <summary>缠斗：扑向目标碰撞箱，贴上后快速减速；玩家离远就回远程态</summary>
        private void LatchingState()
        {
            if (Target is not null)
            {
                Vector2 toTargetDirection = Projectile.SafeDirectionTo(Target.Center);

                if (!Projectile.getRect().Intersects(Target.getRect()))
                {
                    float inertia = 10f;
                    Projectile.velocity = (Projectile.velocity * inertia + toTargetDirection * (Target.velocity.Length() + CalamarisLament.LatchingExtraTargettingSpeed)) / (inertia + 1f);
                    Projectile.rotation = Projectile.rotation.AngleTowards(toTargetDirection.ToRotation() - MathHelper.PiOver2, .3f);
                    Projectile.MinionAntiClump(0.5f);
                    SyncVariables();
                }
                else
                {
                    Projectile.velocity *= 0.2f;
                    SyncVariables();
                }

                if (Vector2.Distance(Owner.Center, Target.Center) > CalamarisLament.LatchingDistanceRequired)
                    SwitchState(AIState.Shooting);
            }
            else
                SwitchState(AIState.Idle);
        }

        /// <summary>切状态并发一次网络同步</summary>
        private void SwitchState(AIState state)
        {
            State = state;
            SyncVariables();
        }
        /// <summary>发一次网络同步（照源：顺手把 netSpam 清零）</summary>
        private void SyncVariables()
        {
            Projectile.netUpdate = true;
            Projectile.netSpam = 0;
        }

        /// <summary>登场喷 40 颗墨点（源写裸值 109，即 DustID.Asphalt；照源无重力、不发光）</summary>
        public override void OnSpawn(IEntitySource source)
        {
            int dustAmount = 40;
            for (int dustIndex = 0; dustIndex < dustAmount; dustIndex++)
            {
                float angle = MathHelper.TwoPi / dustAmount * dustIndex;
                Vector2 velocity = angle.ToRotationVector2() * 8f;
                Dust spawnDust = Dust.NewDustPerfect(Projectile.Center, DustID.Asphalt, velocity);
                spawnDust.noGravity = true;
                spawnDust.noLight = true;
            }
        }
        /// <summary>只有缠斗态才有接触伤害（照源）</summary>
        public override bool? CanDamage() => (State == AIState.Latching) ? null : false;
        /// <summary>缠上去时伤害 ×1.25（照源）</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= CalamarisLament.LatchingDamageMultiplier;
        /// <summary>自绘：5 帧动画本体（照源）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
