using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 辐光光环（SarosAura，移植自灾厄 2.0 的 `Projectiles/Summon/SarosAura`）——
    /// 星律之握览（<see cref="Content.Items.Weapons.Summon.SarosPossession"/>）召唤的光环，
    /// 贴住主人头顶旋转，朝 1600 像素内的敌人倾泻日耀圣火与微缩太阳。
    /// </summary>
    /// <remarks>
    /// **它吃掉主人全部剩余召唤栏**（与天狼星同一套设计）：出手时武器把算出的剩余栏位写进 <c>ai[0]</c>，
    /// 光环每帧把 <c>minionSlots = ai[0]</c>（tML 每帧重新汇总，故实时生效），并按栏位数变强：
    /// 伤害倍率 = <c>log₃(栏位) + 1</c>（超过 3 倍后走软上限 <c>(x−3)×0.1+3</c>）、
    /// 生成率 = <c>130 × 0.9^栏位</c> 帧（下限 7 帧，伤害硬上限 10000）、**转速也随栏位加快**（`栏位×0.85+3` 度/帧）。
    /// 存续靠每帧由 <c>modPlayer.radiantResolution</c> 标志把 <c>timeLeft</c> 压成 2 续命。
    /// <para>
    /// 攻击（照源 2.0）：每 35 帧朝目标 ±20° 射 2 枚 <see cref="SarosSunfire"/>（伤害 = 光环伤害 ÷2、速度 15）；
    /// 每"生成率"帧在自身 100~360 半径的随机位置召出 1 枚 <see cref="SarosMicrosun"/>
    /// （初速 2 像素/帧朝目标，伤害 = 光环伤害、击退 ×4）+ 3 枚 <see cref="SarosSunfire"/>（±30°、速度 19、伤害 ÷2）。
    /// </para>
    /// <para>
    /// 一处与源的有意差异：源 2.0 除默认绘制外还在 <c>PostDraw</c> 里把同一张贴图按 `rotation + π/2` 再画一遍
    ///（等于叠画两层），2.0.3.9 起已把那段删掉。本工程只保留默认绘制（＝2.0.3.9 的观感）。
    /// </para>
    /// </remarks>
    internal class SarosAura:ModProjectile
    {
        /// <summary>主人</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>本次吃下的召唤栏位数（由武器在 <c>Shoot</c> 里写入）</summary>
        public ref float AllocatedSlots => ref Projectile.ai[0];
        /// <summary>通用计时器（攻击节拍）</summary>
        public ref float GeneralTimer => ref Projectile.ai[1];
        /// <summary>索敌距离</summary>
        public const float TargetCheckDistance = 1600f;
        /// <summary>微缩太阳生成率的下限（帧）</summary>
        public const int RadiantOrbAppearRateLowerBound = 7;
        /// <summary>单发伤害的硬上限（源保留的兜底值）</summary>
        public const int RadiantOrbDamageUpperBound = 10000;
        /// <summary>
        /// 标记为可牺牲的召唤物，并允许玩家用召唤武器右键锁定目标（MinionTargettingFeature）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：66×66 判定框、无限穿透、不撞地形、无接触伤害（<see cref="CanDamage"/> 恒 false）、
        /// 超长兜底寿命（实际靠召唤标志压到 2 帧续命；召唤栏占用每帧由 <c>ai[0]</c> 覆写）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 66;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 90000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// 光环 AI：发光 → 校验召唤身份（挂增益 / 续命）→ 把栏位占用刷成 <c>ai[0]</c> →
        /// 贴住主人头顶并按栏位加快自转 → 按栏位算出伤害倍率与生成率 → 主人端索敌并开火。
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Vector3.One * 1.2f);
            VerifyIdentityOfCaller();
            // 每帧把"吃下的栏位数"刷回 minionSlots，tML 每帧重新汇总，故数值始终正确
            Projectile.minionSlots = Projectile.ai[0];
            // 贴在主人头顶上方 16 像素，并随栏位数加快旋转
            Projectile.Center = Owner.Center - Vector2.UnitY * 16f;
            Projectile.rotation += MathHelper.ToRadians(AllocatedSlots * 0.85f + 3f);
            // 栏位越多越强：倍率 = log₃(栏位) + 1，超过 3 倍后每多 1 倍只按 10% 记（源的软上限）
            float damageMultiplier = (float)Math.Log(AllocatedSlots, 3D) + 1f;
            float softcappedDamageMultiplier = damageMultiplier;
            if (softcappedDamageMultiplier > 3f)
            {
                softcappedDamageMultiplier = ((damageMultiplier - 3f) * 0.1f) + 3f;
            }
            int radiantOrbDamage = (int)(Projectile.damage * softcappedDamageMultiplier);
            int radiantOrbAppearRate = (int)(130 * Math.Pow(0.9, AllocatedSlots));
            if (radiantOrbAppearRate < RadiantOrbAppearRateLowerBound)
            {
                radiantOrbAppearRate = RadiantOrbAppearRateLowerBound;
            }
            if (radiantOrbDamage > RadiantOrbDamageUpperBound)
            {
                radiantOrbDamage = RadiantOrbDamageUpperBound;
            }
            GeneralTimer++;
            NPC potentialTarget = Projectile.Center.MinionHoming(TargetCheckDistance, Owner);
            if (potentialTarget != null && Main.myPlayer == Projectile.owner)
            {
                AttackTarget(potentialTarget, radiantOrbAppearRate, radiantOrbDamage);
            }
        }
        /// <summary>
        /// 校验召唤身份：挂上同名增益；玩家死亡时清掉 <c>radiantResolution</c> 标志，
        /// 标志仍在则把 <c>timeLeft</c> 压成 2 实现常驻跟随（照源的 VerifyIdentityOfCaller）。
        /// </summary>
        private void VerifyIdentityOfCaller()
        {
            Owner.AddBuff(ModContent.BuffType<SarosPossessionBuff>(), 3600);
            bool isCorrectProjectile = Projectile.type == ModContent.ProjectileType<SarosAura>();
            if (isCorrectProjectile)
            {
                CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
                if (Owner.dead)
                {
                    modPlayer.radiantResolution = false;
                }
                if (modPlayer.radiantResolution)
                {
                    Projectile.timeLeft = 2;
                }
            }
        }
        /// <summary>
        /// 开火：每 35 帧朝目标 ±20° 两枚日耀圣火（伤害减半、速度 15）；
        /// 每"生成率"帧召出 1 枚微缩太阳（自身 100~360 半径随机处出现、初速 2 朝目标、击退 ×4）
        /// 外加 ±30° 三枚日耀圣火（速度 19、伤害减半）。全部只在主人端生成。
        /// </summary>
        private void AttackTarget(NPC target, int radiantOrbAppearRate, int radiantOrbDamage)
        {
            if (GeneralTimer % 35f == 34f)
            {
                for (int i = 0; i < 2; i++)
                {
                    float angle = MathHelper.Lerp(-MathHelper.ToRadians(20f), MathHelper.ToRadians(20f), i / 2f);
                    Vector2 fireVelocity = Projectile.SafeDirectionTo(target.Center).RotatedBy(angle) * 15f;
                    int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, fireVelocity,
                        ModContent.ProjectileType<SarosSunfire>(), radiantOrbDamage / 2, Projectile.knockBack, Projectile.owner);
                    if (Main.projectile.IndexInRange(idx))
                    {
                        Main.projectile[idx].originalDamage = radiantOrbDamage / 2;
                    }
                }
            }
            if (GeneralTimer % radiantOrbAppearRate == radiantOrbAppearRate - 1)
            {
                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(100f, 360f);
                Vector2 orbVelocity = Projectile.SafeDirectionTo(target.Center) * 2f;
                int orbIdx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, orbVelocity,
                    ModContent.ProjectileType<SarosMicrosun>(), radiantOrbDamage, Projectile.knockBack * 4f, Projectile.owner);
                if (Main.projectile.IndexInRange(orbIdx))
                {
                    Main.projectile[orbIdx].originalDamage = radiantOrbDamage;
                }
                for (int i = 0; i < 3; i++)
                {
                    float angle = MathHelper.Lerp(-MathHelper.ToRadians(30f), MathHelper.ToRadians(30f), i / 3f);
                    Vector2 fireVelocity = Projectile.SafeDirectionTo(target.Center).RotatedBy(angle) * 19f;
                    int idx = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, fireVelocity,
                        ModContent.ProjectileType<SarosSunfire>(), radiantOrbDamage / 2, Projectile.knockBack, Projectile.owner);
                    if (Main.projectile.IndexInRange(idx))
                    {
                        Main.projectile[idx].originalDamage = radiantOrbDamage / 2;
                    }
                }
            }
        }
        /// <summary>光环本体不造成接触伤害（照源）</summary>
        public override bool? CanDamage() => false;
    }
}
