using CalamityDemutation.Content.Items.Weapons.Ranged;
using CalamityDemutation.Content.Projectiles.BaseProjectiles;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 中子枪手持弹幕（NeutronGunHoldout） - 中子枪（NeutronGun）的持握弹幕，移植自 CWR 的 NeutronGunHeldProj。
    /// <para>
    /// CWR 侧继承它自己的枪械框架 BaseFeederGun（1058 行，含弹匣/装填/手动装填按键/服务端配置一整套），
    /// 本模组不移植该框架，改为照 NeutronGlaive 的先例在 <see cref="BaseGunHoldoutCO"/> 上手写等价行为：
    /// 左键速射（每发一颗 <see cref="NeutronBullet"/> + 一道 <see cref="NeutronLaser"/>，每第 3 发射速由 5 帧放慢到 12 帧）、
    /// 右键蓄力射击（射速 45 帧、伤害 ×2.6；充能满 80 时在鼠标处炸开 <see cref="EXNeutronExplosionRanged"/>，
    /// 其后下一发伤害变 ×5.6，且充能逐帧回落直到清零）。
    /// </para>
    /// <para>
    /// 刻意未移植：CWR 的 120 发弹匣与装填动画（SetCartridgeGun&lt;NeutronGunHeldProj&gt;(120)），
    /// 那套要连 CWR 的按键与配置系统一并搬，故改走原版弹药消耗（<see cref="Player.PickAmmo"/>）。
    /// </para>
    /// </summary>
    internal class NeutronGunHoldout : BaseGunHoldoutCO
    {
        // ── 常量 ──
        /// <summary>充能上限：蓄力值达到该值时触发大爆点</summary>
        private const float MaxCharge = 80f;
        /// <summary>左键速射的基准射速（帧）</summary>
        private const int FireTimeNormal = 5;
        /// <summary>左键每第 3 发射速放慢到的帧数</summary>
        private const int FireTimeSlow = 12;
        /// <summary>右键蓄力射击的射速（帧）</summary>
        private const int FireTimeCharged = 45;
        /// <summary>右键伤害倍率（未充满时）</summary>
        private const float ChargedDamageMult = 2.6f;
        /// <summary>右键伤害倍率（充满后的下一发）</summary>
        private const float ChargedDamageMultBoosted = 5.6f;
        /// <summary>本体距手臂的基准距离（CWR 的 HandDistance）</summary>
        private const float HandDistance = 35f;
        /// <summary>右键蓄力时本体前伸到的距离（CWR 的 HandFireDistance）</summary>
        private const float HandFireDistance = 65f;
        /// <summary>弹幕初速（CWR 的 Item.shootSpeed）</summary>
        private const float ShootSpeed = 12f;
        /// <summary>右键时替换的持枪贴图路径（CWR 的 Item_Ranged + "NeutronGun2"）</summary>
        private const string ShootGunTexture = "CalamityDemutation/Content/Items/Weapons/Ranged/NeutronGun2";
        // ── 实例字段 ──
        /// <summary>蓄力值：右键每发 +10，满 80 触发大爆点</summary>
        private float charge;
        /// <summary>本次开火后还要等多少帧才允许下一发</summary>
        private int fireTimer;
        /// <summary>左键连发计数：每 3 发把射速放慢一次</summary>
        private int fireIndex;
        /// <summary>充能条动画帧计数器（0~6，共 7 帧）</summary>
        private int uiframe;
        /// <summary>CWR 的 canattce：充满后置位，期间下一发伤害 ×5.6</summary>
        private bool charged;
        /// <summary>当前是否处于右键蓄力模式（决定手臂前伸距离与替换贴图）</summary>
        private bool firingRight;
        // ── 覆写属性 ──
        /// <summary>直接复用武器本体的贴图（7 帧竖直排布）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Ranged/NeutronGun";
        /// <summary>右键蓄力时把手持物前伸得更远（CWR 每帧按 onFireR 切换 HandDistance）</summary>
        public override float MaxOffsetLengthFromArm => firingRight ? HandFireDistance : HandDistance;
        // ── 生命周期方法 ──
        /// <summary>
        /// 基础属性：在基类默认值之上把尺寸设为贴图尺寸（108×56，width 决定发射口 GunTipPosition 的位置）
        /// </summary>
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 108;
            Projectile.height = 56;
            Projectile.DamageType = DamageClass.Ranged;
        }
        /// <summary>
        /// 去留判据覆写：本武器左右键共用一件物品，player.channel 不可靠，故改判三件事——
        /// 玩家是否已无法持械（死亡/被控/物品栏被锁）、手里的东西是否还是中子枪、以及两个鼠标键是否都松开了
        /// </summary>
        public override void KillHoldoutLogic()
        {
            if (Owner.CantUseHoldout(false) || Owner.HeldItem.type != ModContent.ItemType<NeutronGun>()
                || (Projectile.owner == Main.myPlayer && !Main.mouseLeft && !Main.mouseRight))
                Projectile.Kill();
        }
        /// <summary>
        /// 每帧：切换动画面板、驱动充能标记回落，主人端按鼠标键决定开哪一种火
        /// </summary>
        public override void HoldoutAI()
        {
            CDUtil.ClockFrame(ref Projectile.frame, 5, 6);   // 0~6 共 7 帧，与 7 帧贴图一致
            CDUtil.ClockFrame(ref uiframe, 5, 6);
            if (Projectile.owner != Main.myPlayer)
                return;   // 鼠标状态、蓄力与开火都只由主人端结算，其余端只负责按同步来的数据播放
            firingRight = Main.mouseRight;
            // 充满后的强化标记逐帧回落（CWR 在 PostInOwnerUpdate 里做同一件事）
            if (charged && charge > 0f)
            {
                charge--;
                if (charge <= 0f)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.6f }, Projectile.Center);
                    charged = false;
                }
            }
            if (fireTimer > 0)
                fireTimer--;
            if (Main.mouseRight)
            {
                if (fireTimer <= 0)
                    FireChargedShot();
            }
            else if (Main.mouseLeft)
            {
                if (fireTimer <= 0)
                    FireNormalShot();
            }
            else
            {
                fireTimer = 0;   // 松手后取消冷却，下次按下即刻开火
            }
        }
        /// <summary>
        /// 左键速射：重置蓄力状态，按连发计数决定本次射速，然后消耗一发弹药射出中子弹与中子光束。
        /// CWR 原版在此处每第 3 发把 FireTime 设为 12，本方法等价地把"下一次冷却"设成 12
        /// </summary>
        private void FireNormalShot()
        {
            charge = 0f;
            charged = false;
            fireTimer = FireTimeNormal;
            if (++fireIndex > 2)
            {
                fireTimer = FireTimeSlow;
                fireIndex = 0;
            }
            if (!ConsumeAmmo())
                return;
            SoundEngine.PlaySound(CalamityDemutationSounds.Gun_AWP_Shoot with { Pitch = -0.1f, Volume = 0.25f }
                , Projectile.Center, _ => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
            Vector2 shootVelocity = Projectile.velocity * ShootSpeed;
            // ai[0] = 0：普通弹，命中后不追加天降光束
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, shootVelocity
                , ModContent.ProjectileType<NeutronBullet>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f);
            // 同时甩出一道光束，出生点随机方向偏出 130 像素（CWR 的 CWRUtils.randVr(130, 131) 即"随机方向 × 130"）
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition + Main.rand.NextVector2Unit() * 130f
                , shootVelocity, ModContent.ProjectileType<NeutronLaser>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f);
        }
        /// <summary>
        /// 右键蓄力射击：消耗一发弹药，按是否已充满决定伤害倍率；每发累积 10 点蓄力，
        /// 满 80 的那一发在鼠标位置炸开大爆点，并置位强化标记
        /// </summary>
        private void FireChargedShot()
        {
            fireTimer = FireTimeCharged;
            if (!ConsumeAmmo())
                return;
            SoundEngine.PlaySound(CalamityDemutationSounds.Gun_AWP_Shoot with { Pitch = -0.2f, Volume = 0.3f }
                , Projectile.Center, _ => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
            int shotDamage = (int)(Projectile.damage * (charged ? ChargedDamageMultBoosted : ChargedDamageMult));
            // ai[0] = 1：蓄力弹，命中且命中次数未超 8 时额外从天上落下 3 道光束
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, Projectile.velocity * ShootSpeed
                , ModContent.ProjectileType<NeutronBullet>(), shotDamage, Projectile.knockBack, Owner.whoAmI, 1f);
            if (!charged)
                charge += 10f;
            if (charge >= MaxCharge)
            {
                if (!charged)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.2f }, Projectile.Center);
                    SoundEngine.PlaySound(CalamityDemutationSounds.Pecharge with { Pitch = -0.2f, Volume = 0.8f }, Projectile.Center);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.MouseWorld, Vector2.Zero
                        , ModContent.ProjectileType<EXNeutronExplosionRanged>(), Projectile.damage, 0f);
                }
                charged = true;
                charge = MaxCharge;
            }
        }
        /// <summary>
        /// 消耗一发子弹类弹药。本武器把任何子弹强制转成中子弹，故只看能否取出弹药、不管取到的类型；
        /// 无弹药时返回 false，本次不开火
        /// </summary>
        private bool ConsumeAmmo() => Owner.PickAmmo(Owner.HeldItem, out _, out _, out _, out _, out _, true);
        /// <summary>
        /// 自绘：先在头顶画充能条，再按当前模式取贴图（右键用 NeutronGun2），
        /// 按 bodyFrame 对应的动画帧切片绘制，翻转规则沿用基类的重力方向处理
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            NeutronGlaiveHeld.DrawBar(Owner, charge, uiframe);   // 复用中子长戟的充能条，两者蓄力上限同为 80
            Texture2D texture = firingRight
                ? ModContent.Request<Texture2D>(ShootGunTexture).Value
                : TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects flipSprite = SpriteEffects.None;
            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    flipSprite = SpriteEffects.FlipVertically;
            }
            else if (Projectile.spriteDirection == 1)
            {
                flipSprite = SpriteEffects.FlipVertically;
            }
            Main.EntitySpriteDraw(texture, drawPosition, CDUtil.GetRec(texture, Projectile.frame, 7)
                , Projectile.GetAlpha(lightColor), Projectile.rotation, CDUtil.GetOrig(texture, 7), Projectile.scale, flipSprite, 0);
            return false;
        }
        // ── 网络同步 ──
        /// <summary>
        /// 额外同步两组仅主人端可算的数据：
        /// ① "是否处于右键蓄力模式"——它决定手臂前伸距离与替换贴图，非主人端没有鼠标状态；
        /// ② 蓄力值——它决定头顶充能条画多长，不同步的话远端那条充能条永远是空的
        /// </summary>
        public override void SendExtraAIHoldout(BinaryWriter writer)
        {
            writer.Write(firingRight);
            writer.Write(charge);
        }
        /// <summary>与 <see cref="SendExtraAIHoldout"/> 对应的读取端</summary>
        public override void ReceiveExtraAIHoldout(BinaryReader reader)
        {
            firingRight = reader.ReadBoolean();
            charge = reader.ReadSingle();
        }
    }
}
