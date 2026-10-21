using CalamityDemutation.Content.Items.Weapons.Ranged;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 发条弓·手持弹幕（ClockworkBowHoldout） - 发条弓的整套蓄力机构，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/ClockworkBowHoldout.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 48×96、无限穿透、不撞物块、自身**不造成伤害**（<c>CanDamage => false</c>），
    /// 贴图直接复用物品的 <c>ClockworkBow.png</c>（源就是 <c>Texture => ".../Items/Weapons/Ranged/ClockworkBow"</c>）。
    /// 弓身悬在玩家手前 35 像素、朝向按"距离鼠标 5~25 像素的插值"平滑追上鼠标（只在主人端算，其余端跟随同步值）。
    /// </para>
    /// <para>
    /// **蓄力（照 2.0）**：按住期间每攒满一次装填间隔就装一支弩箭（6 支为上限），
    /// 每次装填把间隔 **×0.95**（越装越快）；装填到位弹「咔 / 嗒」浮字（橙 / 青两色，按奇偶交替）、
    /// 半程响一声 `Item17`、装填到位响 `Item108`（音量 ×0.3）、**满 6 支时改响 `Item23`**。
    /// **释放（照 2.0）**：松手（或不再满足持握条件）时，把已装填的箭按 **16°** 扇形一次齐射，响 `Item38`；
    /// 未装满时最后一支（正在装的那支）不射，但扇形角度会按它的蓄力进度**平滑扩张**。
    /// </para>
    /// <para>
    /// 与源的三点差别：① 源的 `bool uselessFuckYou = OwnerCanShoot;` 是**死代码**，删掉（否则破坏 0 警告基线）；
    /// ② 持握判据用工程既有的 <c>CDUtil.CantUseHoldout()</c>（与源那四个条件同义，另多兜住死亡/未激活）；
    /// ③ 源的 `灾厄本体:BasicTint` 着色器本工程没有，**改用颜色乘法近似**（见 <c>PreDraw</c>）。
    /// </para>
    /// <para>
    /// 照源保留的两处：① 齐射前那句"伤害阶梯"（面板 ×(已装填+1)/7）**算完就被 <c>PickAmmo</c> 的 out 覆盖**，
    /// 源 2.0 与 2.2.2 都是如此，实际每支伤害 = 武器面板 + 弹药贡献；本工程照源不写那行死赋值。
    /// ② 未装满时 PreDraw 的排布算式在"刚装好第 1 支、蓄力恰好归零"那一帧会出现 0/0 ⇒ NaN 角度，
    /// 该支的不透明度同时为 0（不可见），故无害，照源保留。
    /// </para>
    /// </summary>
    internal class ClockworkBowHoldout : ModProjectile
    {
        /// <summary>弩箭扇形排布的总角度（照源的 <c>MathHelper.ToRadians(16)</c>）</summary>
        private const float AngularSpread = 16f * MathHelper.Pi / 180f;
        /// <summary>弓身悬在玩家手前多远（照源 35 像素）</summary>
        private const float HoldoutDistance = 35f;
        /// <summary>每次装填后间隔的衰减系数（照源 0.95，越装越快）</summary>
        private const float ReloadTimeDecay = 0.95f;
        /// <summary>弩箭贴图（源的 PreDraw 直接从贴图路径取）</summary>
        private const string BoltTexturePath = "CalamityDemutation/Content/Projectiles/Ranged/PrecisionBolt";
        /// <summary>弹幕主人（手持者）</summary>
        private Player Owner => Main.player[Projectile.owner];
        /// <summary>还满足"继续引导"的条件：没松手 / 还有箭 / 没被控住（照源，走工程既有判据）</summary>
        private bool OwnerCanShoot => !Owner.CantUseHoldout() && Owner.HasAmmo(Owner.ActiveItem());
        /// <summary>本次装填已蓄力的帧数（源 <c>ai[0]</c>）</summary>
        private ref float CurrentChargingFrames => ref Projectile.ai[0];
        /// <summary>已装填的弩箭数（源 <c>ai[1]</c>）</summary>
        private ref float LoadedBolts => ref Projectile.ai[1];
        /// <summary>当前这次装填需要的帧数（源 <c>localAI[0]</c>）</summary>
        private ref float FramesToLoadBolt => ref Projectile.localAI[0];
        /// <summary>贴图直接复用物品的弓（照源写法）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Ranged/ClockworkBow";
        /// <summary>
        /// 基础属性：48×96 判定、无限穿透、不撞物块、射手伤害类型、不受水阻（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
        }
        /// <summary>
        /// AI：松手/条件不满足 → 齐射或直接消亡；否则推进蓄力、按时装填（含音效与「咔 / 嗒」浮字），
        /// 最后把弓钉在手上并让玩家保持举弓姿势（照 2.0）
        /// </summary>
        public override void AI()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 tipPosition = armPosition + Projectile.velocity * Projectile.width * 0.5f;

            if (!OwnerCanShoot)
            {
                if (LoadedBolts <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
                UnloadBolts(tipPosition);
            }
            else
            {
                // 第一帧：把"一次装填的帧数"定成物品的使用动画帧数
                if (FramesToLoadBolt == 0f)
                    FramesToLoadBolt = Owner.ActiveItem().useAnimation;

                ++CurrentChargingFrames;

                // 装填过半响一声"咔"
                if (CurrentChargingFrames - FramesToLoadBolt / 2f <= 0.01f)
                    SoundEngine.PlaySound(SoundID.Item17);

                if (CurrentChargingFrames >= FramesToLoadBolt && LoadedBolts < ClockworkBow.MaxBolts)
                {
                    CurrentChargingFrames = 0f;
                    ++LoadedBolts;

                    if (LoadedBolts % 2f == 0f)
                        CombatText.NewText(Owner.Hitbox, new Color(155, 255, 255), Language.GetTextValue("Mods.CalamityDemutation.TextContent.ClockworkBowTock"), true);
                    else
                        CombatText.NewText(Owner.Hitbox, new Color(255, 200, 100), Language.GetTextValue("Mods.CalamityDemutation.TextContent.ClockworkBowTick"), true);

                    FramesToLoadBolt *= ReloadTimeDecay;

                    if (LoadedBolts >= ClockworkBow.MaxBolts)
                        SoundEngine.PlaySound(SoundID.Item23);
                    else
                        SoundEngine.PlaySound(SoundID.Item108 with { Volume = SoundID.Item108.Volume * 0.3f });
                }
            }

            UpdateProjectileHeldVariables(armPosition);
            ManipulatePlayerVariables();
        }
        /// <summary>
        /// 齐射：装满时按 16° 均分；未装满时把"正在装的那支"的蓄力进度算进扇形扩张里（照 2.0）
        /// </summary>
        public void UnloadBolts(Vector2 tipPosition)
        {
            if (LoadedBolts == ClockworkBow.MaxBolts)
            {
                for (int i = 0; i < LoadedBolts; i++)
                {
                    float increment = AngularSpread * (LoadedBolts - 1f) / 2f;
                    float spreadForThisProjectile = MathHelper.Lerp(-increment, increment, i / (LoadedBolts - 1f));
                    ShootProjectiles(tipPosition, spreadForThisProjectile);
                }
                SoundEngine.PlaySound(SoundID.Item38);
            }
            else if (LoadedBolts != 0f)
            {
                for (int i = 0; i < LoadedBolts + 1; i++)
                {
                    if (i == LoadedBolts)   // 正在装的那支不射
                        continue;

                    float loadProgress = MathHelper.Clamp(CurrentChargingFrames * 2f / FramesToLoadBolt, 0f, 1f);
                    float increment = AngularSpread * (LoadedBolts - 1f + loadProgress) / 2f;
                    float spreadForThisProjectile = MathHelper.Lerp(-increment, increment, i / MathHelper.Lerp(LoadedBolts - 1f, LoadedBolts, loadProgress));
                    ShootProjectiles(tipPosition, spreadForThisProjectile);
                }
                SoundEngine.PlaySound(SoundID.Item38);
            }

            FramesToLoadBolt = Owner.ActiveItem().useAnimation;   // 装填间隔复位
            LoadedBolts = 0f;                                     // 清空已装填
        }
        /// <summary>
        /// 射出一支 <see cref="PrecisionBolt"/>：伤害 / 弹速 / 击退全部走 <c>Player.PickAmmo</c>
        /// （它内部会顺带消耗一支弹药，并把武器面板算进伤害里——膨胀由此跟随）。
        /// 只跑在主人端（照源）。
        /// </summary>
        public void ShootProjectiles(Vector2 tipPosition, float projectileRotation)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Item heldItem = Owner.ActiveItem();
            float shootSpeed = heldItem.shootSpeed;
            float knockback = heldItem.knockBack;

            Owner.PickAmmo(heldItem, out _, out shootSpeed, out int boltDamage, out knockback, out _);

            knockback = Owner.GetWeaponKnockback(heldItem, knockback);
            Vector2 shootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(projectileRotation) * shootSpeed;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), tipPosition, shootVelocity, ModContent.ProjectileType<PrecisionBolt>(), boltDamage, knockback, Projectile.owner);
        }
        /// <summary>
        /// 把弓摆到手上：主人端按鼠标方向插值转朝向（变了就 netUpdate 同步），
        /// 其余端照抄同步来的速度；朝向翻转时把多转的 180° 收回去（照源的防抖处理）
        /// </summary>
        private void UpdateProjectileHeldVariables(Vector2 armPosition)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                float interpolant = Utils.GetLerpValue(5f, 25f, Owner.Distance(Main.MouseWorld), true);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.SafeDirectionTo(Main.MouseWorld), interpolant);
                if (Projectile.velocity != oldVelocity)
                {
                    Projectile.netSpam = 0;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.position = armPosition - Projectile.Size * 0.5f + Projectile.velocity.SafeNormalize(Vector2.Zero) * HoldoutDistance;
            Projectile.rotation = Projectile.velocity.ToRotation();
            int oldDirection = Projectile.spriteDirection;
            if (oldDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            Projectile.direction = Projectile.spriteDirection = (Projectile.velocity.X > 0).ToDirectionInt();
            if (Projectile.spriteDirection != oldDirection)
                Projectile.rotation -= MathHelper.Pi;

            Projectile.timeLeft = 3;   // 每帧顶回寿命，防止自然消亡
        }
        /// <summary>让玩家保持"正在使用"的姿势、手臂朝向跟随弓（照源）</summary>
        private void ManipulatePlayerVariables()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
        }
        /// <summary>
        /// 自绘：把已装填的弩箭逐支画在弓上（照源），方向跟随弓、未装满时把"正在装的那支"往下错开并淡出。
        /// <para>
        /// **近似处**：源给"刚装好的那一支"套了灾厄的 <c>灾厄本体:BasicTint</c> 着色器（随装填数推移色相、
        /// 按蓄力进度淡出）。本工程没有这个着色器，改成把同色相的颜色**乘进顶点色**——
        /// 视觉接近，且不新增 `.fx` / `.fxc`（省掉预编译那一步）。
        /// </para>
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            float loops = LoadedBolts + 1f;
            if (LoadedBolts == ClockworkBow.MaxBolts)
                loops = LoadedBolts;

            Texture2D boltTexture = ModContent.Request<Texture2D>(BoltTexturePath).Value;

            for (int i = 0; i < loops; i++)
            {
                float boltAngle;
                float shift = 0f;

                if (LoadedBolts == 0f)
                {
                    boltAngle = 0f;
                }
                else if (LoadedBolts == ClockworkBow.MaxBolts)
                {
                    float increment = AngularSpread * (LoadedBolts - 1f) / 2f;
                    boltAngle = MathHelper.Lerp(-increment, increment, i / (LoadedBolts - 1f));
                }
                else
                {
                    float loadProgress = MathHelper.Clamp(CurrentChargingFrames * 2f / FramesToLoadBolt, 0f, 1f);
                    float increment = AngularSpread * (LoadedBolts - 1f + loadProgress) / 2f;
                    boltAngle = MathHelper.Lerp(-increment, increment, i / MathHelper.Lerp(LoadedBolts - 1f, LoadedBolts, loadProgress));
                }

                // 正在装的那支：往下错开并淡出
                if (i == LoadedBolts)
                    shift = 1f - CurrentChargingFrames / FramesToLoadBolt;

                Color transparency = Color.White * (1f - shift);

                // 刚装好的那一支（或已装满）刷一层随装填数推移的色相（源用 BasicTint 着色器，这里是颜色乘法近似）
                if (i == LoadedBolts - 1 || LoadedBolts == ClockworkBow.MaxBolts)
                {
                    Color rainbowTint = Main.hslToRgb(1f - LoadedBolts / ClockworkBow.MaxBolts, 0.8f, 0.85f);
                    float tintOpacity = 1f - MathHelper.Clamp(CurrentChargingFrames * 2f / FramesToLoadBolt, 0f, 1f);
                    transparency = Color.Lerp(transparency, rainbowTint * (1f - shift), tintOpacity);
                }

                Vector2 pointingTo = new Vector2((float)Math.Cos(Projectile.rotation + boltAngle), (float)Math.Sin(Projectile.rotation + boltAngle));
                Vector2 shiftDown = pointingTo.RotatedBy(-MathHelper.PiOver2);
                float flipFactor = Owner.direction < 0 ? MathHelper.Pi : 0f;
                Vector2 drawPosition = Owner.Center + pointingTo.RotatedBy(flipFactor) * (20f + shift * 40f)
                    - shiftDown.RotatedBy(flipFactor) * (boltTexture.Width / 2) - Main.screenPosition;

                Main.EntitySpriteDraw(boltTexture, drawPosition, null, transparency,
                    Projectile.rotation + boltAngle + MathHelper.PiOver2 + flipFactor, boltTexture.Size(), 1f, SpriteEffects.None, 0);
            }
            return true;
        }
        /// <summary>自身不造成接触伤害（照源）</summary>
        public override bool? CanDamage() => false;
    }
}
