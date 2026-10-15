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
    /// 纳米净化的手持弹幕（Nano Purge Holdout） - 逐行照搬灾厄 **2.0.4** 的
    /// <c>Projectiles/Magic/NanoPurgeHoldout.cs</c>（那一版是灾厄自实现的普通弹幕，
    /// 不依赖 `BaseGunHoldoutProjectile` 基类——用户 2026-10-15 点名就要这一版）。
    /// <para>
    /// 机制：整件武器是"机炮式预热"——每帧朝鼠标转一点（只有主人端改速度，其余端靠位置同步），
    /// 每 36 帧把射速档抬一级（1 倍到 4 倍），再用当前射速档去累加一个"出手进度"计数器；
    /// 累够一个使用周期（20）就出手一次：扣一次魔力、播一声 `Item91`，再从左右两个枪口位
    /// （偏移随动画帧在 4/3/0/3 之间变化）各射出一发 <see cref="NanoPurgeLaser"/>。
    /// 出手机理与 2.1 相同，差别只是 2.1 那版改走基类、并把瞄准交给基类管理。
    /// </para>
    /// <para>
    /// 判定 34×62、穿透 -1、不撞地形、不显伤害（`CanDamage` 恒假，贴图只是表现）；
    /// 4 帧动画；首帧不出手也不画贴图（源注释：防止第 1 帧出现反向贴图）。
    /// 魔力付不起（`CheckMana` 失败）时直接自毁。
    /// </para>
    /// </summary>
    internal class NanoPurgeHoldout:ModProjectile
    {
        /// <summary>每多少帧把射速档抬一级（照源 36）</summary>
        private const int FramesPerFireRateIncrease = 36;
        /// <summary>两个枪口位相对枪管的左右偏移，按当前动画帧取值（照源 <c>{ 4, 3, 0, 3 }</c>）</summary>
        private static readonly int[] LaserOffsetByAnimationFrame = { 4, 3, 0, 3 };
        /// <summary>主人端玩家</summary>
        private Player Owner => Main.player[Projectile.owner];
        /// <summary>还能不能继续开火：仍按住、手里有弹药、没被缴械/控制（照源）</summary>
        private bool OwnerCanShoot => Owner.channel && Owner.HasAmmo(Owner.ActiveItem()) && !Owner.noItems && !Owner.CCed;
        /// <summary>已展开帧数（顺带充当"预热进度"）</summary>
        private ref float DeployedFrames => ref Projectile.ai[0];
        /// <summary>朝下一次出手累积的进度（每帧加"当前射速档"）</summary>
        private ref float ChargeTowardsNextShot => ref Projectile.ai[1];
        /// <summary>4 帧动画</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = 4;
        /// <summary>判定 34×62、穿透 -1、不撞地形、忽略水、法师伤害（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>每帧：跟着主人、按射速档出手、朝鼠标转（照源，一行不动）</summary>
        public override void AI()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 gunBarrelPos = armPosition + Projectile.velocity * Projectile.height * 0.5f;

            // 主人开不动这具手持弹幕了就把它删掉
            if (!OwnerCanShoot)
            {
                Projectile.Kill();
                return;
            }

            // 每帧按当前法师加成重算伤害（这样"魔力病"之类的减益也会生效，照源）
            // 【工程侧必要改动】源写的是 `GetTotalDamage<MagicDamageClass>().ApplyTo(weaponItem.damage)`——
            // 它读原始 `Item.damage`，会**绕过**本工程 §7 的数值膨胀钩子（`ModifyWeaponDamage`）。
            // 改用引擎入口 `GetWeaponDamage`：内部同样乘本职业伤害加成（语义等价，2.0.7.2 起灾厄自己也改成了这一句），
            // 但会先跑一遍物品钩子，于是"面板 135 → 膨胀 275"能带动这具手持弹幕与它派生的激光。
            Item weaponItem = Owner.ActiveItem();
            Projectile.damage = weaponItem is null ? 0 : Owner.GetWeaponDamage(weaponItem);

            // 武器的原始使用周期（手持弹幕跟着词缀/攻速改动走）
            int itemUseTime = weaponItem?.useAnimation ?? NanoPurge.UseTime;

            DeployedFrames += 1f;

            // 射速档：开局 1 倍，每 36 帧抬一级，封顶 4 倍（照源）
            int fireRate = (int)MathHelper.Clamp(DeployedFrames / FramesPerFireRateIncrease, 1f, 4f);
            ChargeTowardsNextShot += fireRate;

            if (ChargeTowardsNextShot >= itemUseTime)
            {
                ChargeTowardsNextShot -= itemUseTime;

                // 换动画帧（偏移表随之变化，看起来像两根枪管交替）
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

                bool manaCostPaid = Owner.CheckMana(Owner.ActiveItem(), -1, true, false);
                if (manaCostPaid)
                {
                    SoundEngine.PlaySound(SoundID.Item91, Projectile.Center);

                    if (Main.myPlayer == Projectile.owner)
                    {
                        int projID = ModContent.ProjectileType<NanoPurgeLaser>();
                        float shootSpeed = weaponItem.shootSpeed;
                        float inaccuracyRatio = 0.045f;
                        Vector2 shootDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                        Vector2 perp = shootDirection.RotatedBy(MathHelper.PiOver2);

                        // 一对激光：一发负偏移、一发正偏移（照源）
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 spread = Main.rand.NextVector2CircularEdge(shootSpeed, shootSpeed);
                            Vector2 shootVelocity = shootDirection * shootSpeed + inaccuracyRatio * spread;
                            Vector2 splitBarrelPos = gunBarrelPos + i * LaserOffsetByAnimationFrame[Projectile.frame] * perp;
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), splitBarrelPos, shootVelocity, projID, Projectile.damage, Projectile.knockBack, Projectile.owner);
                            SpawnFiringDust(splitBarrelPos, shootVelocity);
                        }
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
        /// <summary>枪口喷尘（照源：`DustID.TerraBlade` 107，半径 5 的方框内两枚、速度乘 0.125、缩放置 1.4）</summary>
        private void SpawnFiringDust(Vector2 gunBarrelPos, Vector2 laserVelocity)
        {
            int dustID = DustID.TerraBlade;
            int dustRadius = 5;
            int dustDiameter = 2 * dustRadius;
            Vector2 dustCorner = gunBarrelPos - Vector2.One * dustRadius;
            for (int i = 0; i < 2; i++)
            {
                Vector2 dustVel = laserVelocity + Main.rand.NextVector2Circular(7f, 7f);
                Dust d = Dust.NewDustDirect(dustCorner, dustDiameter, dustDiameter, dustID, dustVel.X, dustVel.Y);
                d.velocity *= 0.125f;
                d.noGravity = true;
                d.scale = 1.4f;
            }
        }
        /// <summary>手持弹幕本身不造成伤害（照源）</summary>
        public override bool? CanDamage() => false;
        /// <summary>首帧不画（源注释：防止第 1 帧出现反向贴图），之后走默认绘制</summary>
        public override bool PreDraw(ref Color lightColor) => DeployedFrames > 0f;
    }
}
