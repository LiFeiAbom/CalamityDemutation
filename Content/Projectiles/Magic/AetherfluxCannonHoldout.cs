using CalamityDemutation.Content.Items.Weapons.Magic;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 以太流光炮的手持弹幕（Aetherflux Cannon Holdout） - 逐行照搬灾厄 **2.0.4** 的
    /// <c>Projectiles/Magic/AetherfluxCannonHoldout.cs</c>（那一版仍是灾厄自实现的普通弹幕，
    /// 不依赖手持弹幕基类——用户 2026-10-15 点名就要这一版）。
    /// <para>
    /// 机制：整件武器是"多管机炮式预热"——每帧朝鼠标转一点（只有主人端改速度，其余端靠位置同步），
    /// 8 帧动画在预热期按 `Lerp(7, 2, 进度)` 的节奏由慢变快；**预热 36 帧（＝使用周期）走完之前只迸火花、
    /// 不出弹也不耗蓝**，预热完成后每 `使用周期 / 7` 帧（照源注释：36 帧基准 = 5 帧一发，射速词缀能压到 4）
    /// **成对**发射 <see cref="PhasedGodRay"/>：两发先在垂直于射向的方向上错开 18~28 像素、
    /// 再给**相反的螺旋方向**，于是两道会在飞行中交叉缠绕成"双螺旋"。魔力付不起时直接自毁。
    /// </para>
    /// <para>
    /// 判定 58×94、穿透 -1、不撞地形、不显伤害（`CanDamage` 恒假，贴图只是表现）。
    /// 伤害每帧取 <c>Owner.GetWeaponDamage(weaponItem)</c>——**2.0.4 源里就已经是这个引擎入口**
    /// （2.0 那版才是手写的 `GetTotalDamage&lt;MagicDamageClass&gt;().ApplyTo(item.damage)`），
    /// 所以 §7 的数值膨胀钩子天然生效、无需额外改动。
    /// </para>
    /// <para>
    /// **三处工程侧适配**：① `Owner.CantUseHoldout()` 是灾厄的扩展方法，本工程**已有同义实现**
    /// （`CDUtil.CantUseHoldout()`，见 `Utilities/CDUtil_Laser.cs`，判据逐项相同），照用即可；
    /// ② 螺旋方向**走 `ai[0]` 传**（本工程的光弹取的是 2.1 版，读 `ai[0]`；灾厄 2.0.x 是生成后补
    /// `localAI[1]`，2.0.7.2 起才改成 `ai[0]`，见工程记忆 §18.5）；③ 枪口尘源写裸值 `133`，
    /// 改用命名常量 `DustID.FireworksRGB` 并按 `mainColor/accentColor` 插值取色（2.1 正是这么改的，观感一致）。
    /// 另：源 2.0.4 的 `PreDraw` 写成了常量比较 `FramesPerFireRateIncrease &gt; 0f`（恒真、等于不抑制首帧），
    /// 本工程按 2.0 的原意保留 `DeployedFrames &gt; 0f`。
    /// </para>
    /// </summary>
    internal class AetherfluxCannonHoldout:ModProjectile
    {
        /// <summary>预热期每帧把动画节奏插值用的常数（照源 36，同时是"枪口尘是否需要"的判据来源）</summary>
        private const int FramesPerFireRateIncrease = 36;
        /// <summary>主人端玩家</summary>
        private Player Owner => Main.player[Projectile.owner];
        /// <summary>已展开帧数（顺带充当"预热进度"）</summary>
        private ref float DeployedFrames => ref Projectile.ai[0];
        /// <summary>当前动画节奏（预热期由 7 线性收到 2，预热结束后恒为 2）</summary>
        private ref float AnimationRate => ref Projectile.ai[1];
        /// <summary>上一次"尝试出手"时已展开的帧数</summary>
        private ref float LastShootAttemptTime => ref Projectile.localAI[0];
        /// <summary>上一次换动画帧时已展开的帧数</summary>
        private ref float LastAnimationTime => ref Projectile.localAI[1];
        /// <summary>8 帧动画</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = 8;
        /// <summary>判定 58×94、穿透 -1、不撞地形、忽略水、法师伤害（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 94;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>每帧：跟着主人、预热、成对放出双螺旋光弹（照源，一行不动）</summary>
        public override void AI()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 gunBarrelPos = armPosition + Projectile.velocity * Projectile.height * 0.4f;

            // 主人开不动这具手持弹幕（松手/被控制/没弹药）了就把它删掉
            if (Owner.CantUseHoldout() || !Owner.HasAmmo(Owner.ActiveItem()))
            {
                Projectile.Kill();
                return;
            }

            // 首帧播一声起手音（照源：DD2 黑暗法师治疗音，音量乘 1.5）
            if (DeployedFrames <= 0f)
            {
                SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Volume = SoundID.DD2_DarkMageCastHeal.Volume * 1.5f }, Projectile.Center);
            }

            // 每帧按当前法师加成重算伤害（这样"魔力病"之类的减益也会生效，照源）
            Item weaponItem = Owner.ActiveItem();
            Projectile.damage = weaponItem is null ? 0 : Owner.GetWeaponDamage(weaponItem);

            // 武器的原始使用周期；出手间隔 = 使用周期 / 7（照源注释：36 基准 = 5 帧一发，射速词缀能压到 4）
            int itemUseTime = weaponItem?.useAnimation ?? AetherfluxCannon.UseTime;
            int framesPerShot = itemUseTime / 7;

            DeployedFrames += 1f;

            // 动画节奏：预热期由 7 线性收到 2，预热结束后恒为 2（照源）
            AnimationRate = DeployedFrames >= itemUseTime ? 2f : MathHelper.Lerp(7f, 2f, DeployedFrames / itemUseTime);

            // 换动画帧：预热期慢、热起来之后快（照源）
            if (DeployedFrames - LastAnimationTime >= AnimationRate)
            {
                LastAnimationTime = DeployedFrames;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            // 到了出手节拍：预热没走完只迸火花，走完了才真出手（照源）
            if (DeployedFrames - LastShootAttemptTime >= framesPerShot)
            {
                LastShootAttemptTime = DeployedFrames;
                bool actuallyShoot = DeployedFrames >= itemUseTime;
                bool manaOK = !actuallyShoot || Owner.CheckMana(Owner.ActiveItem(), -1, true, false);
                if (manaOK)
                {
                    if (actuallyShoot)
                    {
                        SoundEngine.PlaySound(SoundID.Item91, Projectile.Center);
                    }

                    int projID = ModContent.ProjectileType<PhasedGodRay>();
                    float shootSpeed = weaponItem.shootSpeed;
                    Vector2 shootDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                    Vector2 shootVelocity = shootDirection * shootSpeed;

                    // 两道波动光弹要错开起始点，交叉起来才好看（照源 18~28 像素）
                    float waveSideOffset = Main.rand.NextFloat(18f, 28f);
                    Vector2 perp = shootDirection.RotatedBy(-MathHelper.PiOver2) * waveSideOffset;

                    // 预热/开火期间晶体持续崩出尘（照源 0.045 的散布系数）
                    float dustInaccuracy = 0.045f;

                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 laserStartPos = gunBarrelPos + i * perp + Main.rand.NextVector2CircularEdge(6f, 6f);
                        Vector2 dustOnlySpread = Main.rand.NextVector2Circular(shootSpeed, shootSpeed);
                        Vector2 dustVelocity = shootVelocity + dustInaccuracy * dustOnlySpread;
                        if (actuallyShoot)
                        {
                            // 螺旋方向走 ai[0] 传（本工程的光弹是 2.1 版，读的正是 ai[0]；见类注释 ②）
                            Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), laserStartPos, shootVelocity, projID, Projectile.damage, Projectile.knockBack, Projectile.owner, i * 0.5f, 0f);
                        }
                        SpawnFiringDust(gunBarrelPos, dustVelocity);
                    }
                }
                // 魔力付不起就把枪收了（照源）
                else
                {
                    Projectile.Kill();
                    return;
                }
            }

            UpdateProjectileHeldVariables(armPosition);
            ManipulatePlayerVariables();
        }
        /// <summary>
        /// 位置/朝向维护（照源）：只有主人端朝鼠标方向插值修改速度（改了就发一次同步），
        /// 然后按速度方向摆正旋转、把中心钉在手臂前方 8 像素处，并每帧把寿命续到 2。
        /// </summary>
        private void UpdateProjectileHeldVariables(Vector2 armPosition)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                float interpolant = Utils.GetLerpValue(5f, 25f, Projectile.Distance(Main.MouseWorld), true);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Main.MouseWorld), interpolant);
                if (Projectile.velocity != oldVelocity)
                {
                    Projectile.netSpam = 0;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Center = armPosition + Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * 8f;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
        }
        /// <summary>把主人的朝向/抬手状态钉成"正在使用这件武器"（照源）</summary>
        private void ManipulatePlayerVariables()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
        }
        /// <summary>
        /// 枪口喷尘（照源：半径 12 的方框内两枚、速度乘 0.18、缩放置 0.6）。
        /// 【工程侧适配】源写裸值 `133`；本工程照 2.1 的口径改用命名常量 <c>DustID.FireworksRGB</c>，
        /// 颜色按 <see cref="AetherfluxCannon.mainColor"/> 与 <see cref="AetherfluxCannon.accentColor"/> 插值取色
        /// （2.1 那边正是把该尘换成了自家 `LightDust` 并这样取色，观感一致）。
        /// </summary>
        private void SpawnFiringDust(Vector2 gunBarrelPos, Vector2 laserVelocity)
        {
            int dustRadius = 12;
            float dustRandomness = 11f;
            int dustDiameter = 2 * dustRadius;
            Vector2 dustCorner = gunBarrelPos - Vector2.One * dustRadius;
            for (int i = 0; i < 2; i++)
            {
                Vector2 dustVel = laserVelocity + Main.rand.NextVector2Circular(dustRandomness, dustRandomness);
                Dust d = Dust.NewDustDirect(dustCorner, dustDiameter, dustDiameter, DustID.FireworksRGB, dustVel.X, dustVel.Y);
                d.velocity *= 0.18f;
                d.noGravity = true;
                d.scale = 0.6f;
                d.color = Color.Lerp(AetherfluxCannon.accentColor, AetherfluxCannon.mainColor, Main.rand.NextFloat());
            }
        }
        /// <summary>手持弹幕本身不造成伤害（照源）</summary>
        public override bool? CanDamage() => false;
        /// <summary>首帧不画（防第 1 帧出现反向贴图，取 2.0 的原意；2.0.4 那行误写成常量比较），之后走默认绘制</summary>
        public override bool PreDraw(ref Color lightColor) => DeployedFrames > 0f;
    }
}
