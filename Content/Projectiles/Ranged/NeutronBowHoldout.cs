using CalamityDemutation.Content.Items.Weapons.Ranged;
using CalamityDemutation.Content.Projectiles.BaseProjectiles;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 洛希之弦手持弹幕（NeutronBowHoldout） - 洛希之弦（NeutronBow）的持弓弹幕，移植自 CWR 0.4.0.1.3 的 NeutronBowHeldProj。
    /// <para>
    /// CWR 侧继承它自己的弓械框架 BaseBow（433 行，含弹药强制转换、搭箭绘制、弦偏移一整套），
    /// 本模组照 NeutronGunHoldout 的先例不移植该框架，改为在 <see cref="BaseGunHoldoutCO"/> 上手写等价行为：
    /// 左键每 10 帧射一发 <see cref="NeutronArrow"/>；右键三级蓄力（蓄力值 0→80，每帧 +0.5），
    /// 到达 2 / 30 / 60 阈值时各弹一次战斗文字并播一次装填音（音高递减），弓上搭的箭数 1→2→3；
    /// 蓄满（80）后自动射出三发 <see cref="EXNeutronArrow"/>（伤害 ×3/×5/×3、张角 ±0.25 弧度）并震屏。
    /// </para>
    /// <para>
    /// 与源码的三处内部实现差异（除左键射速系用户点名提速外，外部行为等价）：
    /// ① 大修的计数器只有 <c>Projectile.ai[1]</c> 一个（<c>ShootCoolingValue</c> 只是它的属性包装），
    /// 这里对应成一个 <see cref="fireCooling"/>，蓄力期间被 <c>Charge / 4</c> 顶住、蓄满后恢复自增即触发；
    /// ② 大修靠逐帧改写 <c>Item.useTime</c> 在「左键射速」与「蓄力闸门」之间切换（两处源码都硬编码 20），
    /// 本工程拆成 <see cref="FireTime"/> 与 <see cref="ChargeGate"/> 两个常量，改左键射速不再波及蓄力；
    /// ③ 大修发射点用 <c>Projectile.Center + FireOffsetPos</c>（FireOffsetPos 系其射击位移功能，弓恒为零向量），
    /// 本工程直接取 <see cref="Projectile.Center"/>。
    /// </para>
    /// </summary>
    internal class NeutronBowHoldout : BaseGunHoldoutCO
    {
        // ── 常量 ──
        /// <summary>蓄力上限：与中子枪/中子之刃共用同一条充能条，故同取 80</summary>
        private const float MaxCharge = 80f;
        /// <summary>每帧蓄力增量（源码 0.5）</summary>
        private const float ChargeSpeed = 0.5f;
        /// <summary>弹幕初速（物品 shootSpeed）</summary>
        private const float ShootSpeed = 16f;
        /// <summary>左键射击间隔（源码 20，用户 2026-09-25 点名提速到 10）；物品 useTime 同步取该值</summary>
        private const int FireTime = 10;
        /// <summary>
        /// 右键蓄力期的开火闸门：大修在蓄力分支里把 <c>Item.useTime</c> 钉死成 20（= <c>MaxCharge / 4</c>），
        /// 左键射速与它是同一个字段。本工程拆成两个常量，否则左键一提速，蓄到一半就会被判成"冷却完毕"而提前击发
        /// </summary>
        private const float ChargeGate = MaxCharge / 4f;
        /// <summary>本体距手臂的基准距离（大修 HandDistance）</summary>
        private const float HandDistance = 15f;
        /// <summary>右键蓄力时本体前伸到的距离（大修 HandFireDistance）</summary>
        private const float HandFireDistance = 22f;
        /// <summary>搭箭绘制的基准后移量（大修 BaseBow.DrawArrowMode）</summary>
        private const float DrawArrowMode = -25f;
        /// <summary>
        /// 三级蓄力的文字与音效阈值：源码一级判的是 <c>ShootCoolingValue &gt; 2</c>，
        /// 而 ShootCoolingValue 就是 <c>Charge / 4</c>，换算到蓄力值上即一级 &gt;8；
        /// 二级、三级源码直接判 Charge，故照抄 &gt;30 / &gt;60
        /// </summary>
        private const float Level1Value = 8f;
        private const float Level2Value = 30f;
        private const float Level3Value = 60f;
        /// <summary>右键三连射的伤害倍率（中间那发 5 倍，两侧 3 倍）</summary>
        private const float RightDamageSide = 3f;
        private const float RightDamageCenter = 5f;
        /// <summary>右键三连射的张角（弧度）</summary>
        private const float RightSpread = 0.25f;
        /// <summary>右键开火的屏幕震动强度（大修 ModOwner.SetScreenShake(6.2f)）</summary>
        private const float RightShake = 6.2f;
        // ── 实例字段 ──
        /// <summary>蓄力值（0~80），同时决定充能条长度与搭箭数</summary>
        private float charge;
        /// <summary>是否处于右键蓄力（决定手臂前伸距离；非主人端靠同步）</summary>
        private bool chargingRight;
        /// <summary>冷却计数：蓄力期间被 charge/4 顶住，蓄满后恢复自增即触发击发（大修的 ShootCoolingValue + ai[1] 合并）</summary>
        private int fireCooling;
        /// <summary>弓体动画帧计数（0~15，共 16 帧，与 0.4.0.1.3 的贴图一致）</summary>
        private int uiframe;
        /// <summary>三级蓄力各自只触发一次文字与音效</summary>
        private bool level1 = true;
        private bool level2 = true;
        private bool level3 = true;
        /// <summary>弓上搭的箭数（1 / 2 / 3），非主人端靠同步</summary>
        private int arrowCount = 1;
        // ── 覆写属性 ──
        /// <summary>直接复用武器本体的贴图（0.4.0.1.3 的 50×1952、16 帧竖直排布）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Ranged/NeutronBow";
        /// <summary>右键蓄力时把弓往前伸一些（大修按 onFireR 在 HandDistance / HandFireDistance 之间切）</summary>
        public override float MaxOffsetLengthFromArm => chargingRight ? HandFireDistance : HandDistance;
        /// <summary>大修的 CanFire：左右键任一按下即视为"已开弓"（远端只看同步来的蓄力标记）</summary>
        private bool CanFireNow => chargingRight || (Projectile.owner == Main.myPlayer && Main.mouseLeft);
        // ── 生命周期方法 ──
        /// <summary>
        /// 基础属性：在基类默认值之上把尺寸设为单帧贴图尺寸（50×122）
        /// </summary>
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 50;
            Projectile.height = 122;
            Projectile.DamageType = DamageClass.Ranged;
        }
        /// <summary>
        /// 去留判据覆写：本武器左右键共用一件物品，player.channel 不可靠，故改判三件事——
        /// 玩家是否已无法持械（死亡/被控/物品栏被锁）、手里的东西是否还是洛希之弦、以及两个鼠标键是否都松开了
        /// </summary>
        public override void KillHoldoutLogic()
        {
            if (Owner.CantUseHoldout(false) || Owner.HeldItem.type != ModContent.ItemType<NeutronBow>()
                || (Projectile.owner == Main.myPlayer && !Main.mouseLeft && !Main.mouseRight))
                Projectile.Kill();
        }
        /// <summary>
        /// 每帧：先按 16 帧推进弓体动画与充能条帧，再由主人端结算蓄力与击发（远端只按同步数据播放）
        /// </summary>
        public override void HoldoutAI()
        {
            CDUtil.ClockFrame(ref Projectile.frame, 2, 15);   // 弓体 0~15 共 16 帧，与 16 帧贴图一致
            CDUtil.ClockFrame(ref uiframe, 5, 6);             // 充能条 0~6 共 7 帧
            if (Projectile.owner != Main.myPlayer)
                return;   // 鼠标状态、蓄力与开火都只由主人端结算
            bool hasAmmo = Owner.HasAmmo(Owner.HeldItem);
            // 左键优先：源码 FiringIncident 左键先置 onFire，右键分支要求 !onFire 才进，故双键同按走左键
            chargingRight = Main.mouseRight && !Main.mouseLeft && hasAmmo;   // 无弹药时连蓄力姿态都不进（源码 CanFire 的门槛）
            bool firingLeft = Main.mouseLeft;
            fireCooling++;   // 大修的 ShootCoolingValue++（每帧）
            if (chargingRight)
            {
                if (charge < MaxCharge)
                {
                    charge += ChargeSpeed;
                    fireCooling = (int)(charge / 4f);   // 蓄力期间冷却被顶成 charge/4：蓄不满就打不出去
                    arrowCount = 1;
                    if (charge > Level1Value)
                    {
                        if (level1)
                        {
                            StageText(0);
                            PlayLoadSound(-0.3f, 0.6f);
                            level1 = false;
                        }
                    }
                    if (charge > Level2Value)
                    {
                        if (level2)
                        {
                            StageText(60);
                            PlayLoadSound(-0.2f, 0.7f);
                            level2 = false;
                        }
                        arrowCount++;
                    }
                    if (charge > Level3Value)
                    {
                        if (level3)
                        {
                            StageText(120);
                            PlayLoadSound(-0.1f, 0.8f);
                            level3 = false;
                        }
                        arrowCount++;
                    }
                }
                else
                {
                    arrowCount = 3;   // 蓄满：弓上搭三支，且冷却恢复自增，随即击发
                }
            }
            else
            {
                arrowCount = 1;
                charge = 0f;
                level1 = level2 = level3 = true;
            }
            float gateNow = chargingRight ? ChargeGate : FireTime;   // 源码靠逐帧改写 Item.useTime 在两种闸门间切换
            if (fireCooling > gateNow && (chargingRight || firingLeft))
            {
                fireCooling = 0;
                if (!ConsumeAmmo())
                    return;
                if (chargingRight)
                    FireChargedShot();
                else
                    FireNormalShot();
                charge = 0f;
                arrowCount = 1;
                level1 = level2 = level3 = true;
            }
        }
        /// <summary>
        /// 左键单发射击：射出一发 <see cref="NeutronArrow"/>，伤害取持弓体自身（大修 BowShoot 用 WeaponDamage）；
        /// 音效走物品的 UseSound（大修 FiringDefaultSound）
        /// </summary>
        private void FireNormalShot()
        {
            SoundEngine.PlaySound(SoundID.Item5 with { Pitch = -0.1f, Volume = 0.35f }
                , Projectile.Center, _ => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * ShootSpeed
                , ModContent.ProjectileType<NeutronArrow>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
        }
        /// <summary>
        /// 右键蓄满的三连射：中间那发 5 倍伤害、两侧各 3 倍，三发在 ±0.25 弧度内张开；
        /// 音效取马格南（大修 HanderPlaySound 的 onFireR 分支），并震屏 6.2
        /// </summary>
        private void FireChargedShot()
        {
            if (Main.myPlayer == Projectile.owner)
                CalamityDemutation.ScreenShakeAmp = Math.Max(CalamityDemutation.ScreenShakeAmp, RightShake);
            SoundEngine.PlaySound(CalamityDemutationSounds.Gun_Magnum_Shoot with { Pitch = 0.7f, Volume = 0.6f }
                , Projectile.Center, _ => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
            for (int i = 0; i < 3; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center
                    , (Projectile.velocity * ShootSpeed).RotatedBy((-1 + i) * RightSpread)
                    , ModContent.ProjectileType<EXNeutronArrow>()
                    , (int)(Projectile.damage * (i == 1 ? RightDamageCenter : RightDamageSide)), Projectile.knockBack, Owner.whoAmI);
            }
        }
        /// <summary>
        /// 消耗一发箭类弹药。本武器把任何箭强制转成中子箭，故只看能否取出弹药、不管取到的类型；
        /// 无弹药时返回 false，本次不开火
        /// </summary>
        private bool ConsumeAmmo() => Owner.PickAmmo(Owner.HeldItem, out _, out _, out _, out _, out _, true);
        /// <summary>
        /// 弹一条三级蓄力的战斗文字：文字取本地化 TextContent 段，竖直按档位错开、颜色随之变亮
        /// </summary>
        private void StageText(int offsetY)
        {
            Rectangle textArea = Owner.Hitbox;
            textArea.Y -= offsetY;
            CombatText.NewText(textArea, new Color(155, 200, 100 + offsetY), GetStageText(offsetY), true);
        }
        /// <summary>按档位取三段蓄力文字（大修的 CWRLocText 键名原样保留）</summary>
        private static string GetStageText(int offsetY) => offsetY switch
        {
            0 => Language.GetTextValue("Mods.CalamityDemutation.TextContent.Wap_NeutronBow_LoadingText1"),
            60 => Language.GetTextValue("Mods.CalamityDemutation.TextContent.Wap_NeutronBow_LoadingText2"),
            _ => Language.GetTextValue("Mods.CalamityDemutation.TextContent.Wap_NeutronBow_LoadingText3"),
        };
        /// <summary>播放一次装填音（大修 CWRSound.loadTheRounds，音高随档位升高）</summary>
        private void PlayLoadSound(float pitch, float volume)
            => SoundEngine.PlaySound(CalamityDemutationSounds.LoadTheRounds with { Pitch = pitch, Volume = volume }
            , Projectile.Center, _ => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
        /// <summary>
        /// 自绘：先在头顶画充能条（复用中子长戟的 DrawBar，两者蓄力上限同为 80），
        /// 再按 16 帧切片画弓体，最后在弦上按蓄力档位搭 1~3 支箭
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            NeutronGlaiveHeld.DrawBar(Owner, charge, uiframe);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
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
            Main.EntitySpriteDraw(texture, drawPosition, CDUtil.GetRec(texture, Projectile.frame, 16)
                , CanFireNow ? Color.White : Projectile.GetAlpha(lightColor)
                , Projectile.rotation, CDUtil.GetOrig(texture, 16), Projectile.scale, flipSprite, 0);
            ArrowDraw();
            return false;
        }
        /// <summary>
        /// 搭箭绘制（大修 BaseBow.ArrowDraw）：刚开弓一小会儿后开始显形，弦上按 <see cref="arrowCount"/> 画箭。
        /// 箭贴图取强制转换目标的弹幕贴图（即 <see cref="NeutronArrow"/> 的 14×86），
        /// 原点取"上边中点"并翻转 180°（大修 ISForcedConversionDrawAmmoInversion 的效果），
        /// 再沿弓的朝向按 <c>弦系数 × 16 + DrawArrowMode</c> 后移——越蓄力越往回收，做出拉弦感
        /// </summary>
        private void ArrowDraw()
        {
            if (!CanFireNow)
                return;
            int cooltime = Math.Min(3, FireTime / 3);
            if (fireCooling <= cooltime)
                return;
            Texture2D arrowTexture = TextureAssets.Projectile[ModContent.ProjectileType<NeutronArrow>()].Value;
            float chordCoefficient = 1f - fireCooling / (float)FireTime;
            Vector2 inprojRot = Projectile.rotation.ToRotationVector2();
            Vector2 offsetDrawPos = inprojRot * (chordCoefficient * 16f + DrawArrowMode);
            Vector2 norlInRotUnit = inprojRot.GetNormalVector();
            float drawRot = Projectile.rotation + MathHelper.PiOver2;
            Vector2 drawOrig = new Vector2(arrowTexture.Width / 2, 0);
            Vector2 drawPos = Projectile.Center - Main.screenPosition + offsetDrawPos;
            void drawArrow(float overOffsetRot = 0f, Vector2 overOffsetPos = default) => Main.EntitySpriteDraw(arrowTexture
                , drawPos + overOffsetPos, null, Color.White, drawRot + MathHelper.Pi + overOffsetRot
                , drawOrig, Projectile.scale, SpriteEffects.FlipVertically);
            switch (arrowCount)
            {
                case 2:
                    drawArrow(0.3f * chordCoefficient);
                    drawArrow(-0.3f * chordCoefficient);
                    break;
                case 3:
                    chordCoefficient = Math.Min(chordCoefficient + 0.5f, 1f);
                    drawArrow(0.45f * chordCoefficient, norlInRotUnit * -1f);
                    drawArrow();
                    drawArrow(-0.45f * chordCoefficient, norlInRotUnit * 1f);
                    break;
                default:
                    drawArrow();
                    break;
            }
        }
        // ── 网络同步 ──
        /// <summary>
        /// 额外同步三组仅主人端可算的数据：是否处于右键蓄力（决定手臂前伸距离）、
        /// 蓄力值（决定头顶充能条长度）、冷却计数与搭箭数（决定弦上画几支箭、拉弦多深）
        /// </summary>
        public override void SendExtraAIHoldout(BinaryWriter writer)
        {
            writer.Write(chargingRight);
            writer.Write(charge);
            writer.Write7BitEncodedInt(fireCooling);
            writer.Write7BitEncodedInt(arrowCount);
        }
        /// <summary>与 <see cref="SendExtraAIHoldout"/> 对应的读取端</summary>
        public override void ReceiveExtraAIHoldout(BinaryReader reader)
        {
            chargingRight = reader.ReadBoolean();
            charge = reader.ReadSingle();
            fireCooling = reader.Read7BitEncodedInt();
            arrowCount = reader.Read7BitEncodedInt();
        }
    }
}
