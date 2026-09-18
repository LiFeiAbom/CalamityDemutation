using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.BaseProjectiles
{
    /// <summary>
    /// 枪械式手持弹幕基类（移植自灾厄 BaseGunHoldoutProjectile）：把"持握、瞄准、手臂表现、发射口位置"这套
    /// 通用逻辑收在一处，子类只需在 <see cref="HoldoutAI"/> 里写实际行为。
    /// <para>
    /// 与灾厄原版的两点区别：① 灾厄要求"类名 = 物品类名 + Holdout"，并据此自动推导贴图路径与显示名、用
    /// AssociatedItemID 校验玩家是否仍持有该武器；本工程不移植物品，故**去掉全部物品联动**，贴图走 tML 默认的
    /// 弹幕路径（与 .cs 同目录同名的 .png），存活判据只由 <see cref="CantUseHoldout"/> 决定。
    /// ② Owner.Calamity().mouseWorld 改用 Main.MouseWorld。
    /// </para>
    /// 子类应自行在 SetDefaults 里设置 Projectile.width/height（会影响发射口 GunTipPosition 的位置）。
    /// </summary>
    internal abstract class BaseGunHoldoutCO : ModProjectile
    {
        // ── 可覆写属性 ──
        /// <summary>发射口位置：默认取本体朝向正前方半宽处；贴图枪口不在此处时可覆写</summary>
        public virtual Vector2 GunTipPosition => Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * Projectile.width * 0.5f;
        /// <summary>武器转向鼠标的速度（0~1，越大越快；默认 0.2）</summary>
        public virtual float WeaponTurnSpeed => 0.2f;
        /// <summary>后坐位移 OffsetLengthFromArm 回弹到 MaxOffsetLengthFromArm 的速度（默认 0.3）</summary>
        public virtual float RecoilResolveSpeed => 0.3f;
        /// <summary>本体距手臂的距离，用于把手持物摆在手臂上</summary>
        public virtual float MaxOffsetLengthFromArm { get; }
        /// <summary>朝上瞄准时的 X 轴偏移</summary>
        public virtual float OffsetXUpwards { get; }
        /// <summary>朝下瞄准时的 X 轴偏移</summary>
        public virtual float OffsetXDownwards { get; }
        /// <summary>水平瞄准时的 Y 轴基准偏移</summary>
        public virtual float BaseOffsetY { get; }
        /// <summary>朝上瞄准时的 Y 轴偏移</summary>
        public virtual float OffsetYUpwards { get; }
        /// <summary>朝下瞄准时的 Y 轴偏移</summary>
        public virtual float OffsetYDownwards { get; }
        // ── 状态 ──
        /// <summary>本手持弹幕的主人，在 OnSpawn 时填充</summary>
        public Player Owner { get; private set; }
        /// <summary>是否持续把 Projectile.timeLeft 刷成 2（用于让它在该死之后再多留一小会儿；默认 true）</summary>
        public bool KeepRefreshingLifetime { get; set; } = true;
        /// <summary>是否持续把主人的 itemTime/itemAnimation 置 2</summary>
        public bool SetUsage { get; set; } = true;
        /// <summary>当前距手臂的距离，靠把它设得比 MaxOffsetLengthFromArm 小来做后坐表现</summary>
        public float OffsetLengthFromArm { get; set; }
        /// <summary>前臂附加旋转（方向已处理，直接给角度即可）</summary>
        public float ExtraFrontArmRotation { get; set; }
        /// <summary>后臂附加旋转（方向已处理，直接给角度即可）</summary>
        public float ExtraBackArmRotation { get; set; }
        /// <summary>前臂拉伸程度（默认 Full）</summary>
        public Player.CompositeArmStretchAmount FrontArmStretch { get; set; } = Player.CompositeArmStretchAmount.Full;
        /// <summary>后臂拉伸程度（默认 Full）</summary>
        public Player.CompositeArmStretchAmount BackArmStretch { get; set; } = Player.CompositeArmStretchAmount.Full;
        // ── 覆写成员 ──
        /// <summary>
        /// 基础属性：尺寸默认 1×1（**子类应在自己的 SetDefaults 里改成贴图尺寸**，否则发射口位置会偏）、
        /// 不撞地形、netImportant、伤害类型默认远程且开启伤害动态刷新（子类按实际职业覆写）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }
        /// <summary>出生时把后坐位移拉满，避免第一帧从手臂位置弹出来</summary>
        public override void OnSpawn(IEntitySource source) => OffsetLengthFromArm = MaxOffsetLengthFromArm;
        /// <summary>位置由 ManageHoldout 每帧指定，不走原版的位置积分</summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>手持弹幕本体不造成伤害（伤害由它生成的弹幕负责）</summary>
        public override bool? CanDamage() => false;
        // ── AI ──
        /// <summary>每帧：补全主人引用 → 判去留 → 持握/瞄准/手臂表现 → 子类行为</summary>
        public override void AI()
        {
            Owner ??= Main.player[Projectile.owner];
            KillHoldoutLogic();
            ManageHoldout();
            HoldoutAI();
        }
        /// <summary>手持弹幕的消失逻辑：默认在主人无法继续持有（松手/死亡/被控）时销毁</summary>
        public virtual void KillHoldoutLogic()
        {
            if (Owner.CantUseHoldout())
                Projectile.Kill();
        }
        /// <summary>
        /// 持握的全部表现逻辑：把本体摆到"玩家中心 + 手臂偏移"处、按鼠标方向转向、
        /// 设置玩家朝向与手持状态，并驱动前后臂的复合姿态；最后刷新存活时间、回弹后坐位移、请求网络同步。
        /// 非主人端不做鼠标瞄准，改用已同步的 rotation/spriteDirection，避免抖动。
        /// </summary>
        public virtual void ManageHoldout()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            float holdoutDirection = Projectile.velocity.ToRotation();
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 ownerToMouse = Main.MouseWorld - armPosition;
                // -1~1：分别对应"朝下/朝上"瞄准的程度，用来在上下偏移之间插值
                float proximityLookingUpwards = Vector2.Dot(ownerToMouse.SafeNormalize(Vector2.Zero), -Vector2.UnitY * Owner.gravDir);
                int direction = MathF.Sign(ownerToMouse.X);
                Vector2 lengthOffset = Projectile.rotation.ToRotationVector2() * OffsetLengthFromArm;
                Vector2 armOffset = new Vector2(Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f, proximityLookingUpwards > 0f ? OffsetXUpwards : OffsetXDownwards) * direction, BaseOffsetY * Owner.gravDir + Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f, proximityLookingUpwards > 0f ? OffsetYUpwards : OffsetYDownwards) * Owner.gravDir);
                Projectile.Center = armPosition + lengthOffset + armOffset;
                Projectile.velocity = holdoutDirection.AngleTowards(ownerToMouse.ToRotation(), WeaponTurnSpeed).ToRotationVector2();
                Projectile.rotation = holdoutDirection;
                Projectile.spriteDirection = direction;
                Owner.ChangeDir(direction);
            }
            else
            {
                Vector2 lengthOffset = Projectile.rotation.ToRotationVector2() * OffsetLengthFromArm;
                float proximityLookingUpwards = Vector2.Dot(Projectile.velocity.SafeNormalize(Vector2.Zero), -Vector2.UnitY * Owner.gravDir);
                int direction = Projectile.spriteDirection;
                Vector2 armOffset = new Vector2(Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f, proximityLookingUpwards > 0f ? OffsetXUpwards : OffsetXDownwards) * direction, BaseOffsetY * Owner.gravDir + Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f, proximityLookingUpwards > 0f ? OffsetYUpwards : OffsetYDownwards) * Owner.gravDir);
                Projectile.Center = armPosition + lengthOffset + armOffset;
                Projectile.velocity = Projectile.rotation.ToRotationVector2();
                Owner.ChangeDir(direction);
            }
            int currentDirection = Projectile.spriteDirection;
            Owner.heldProj = Projectile.whoAmI;
            if (SetUsage)
                Owner.itemTime = Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
            // 减 90°：手臂旋转以"手臂朝下"为 0 点
            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? MathHelper.Pi : 0f);
            Owner.SetCompositeArmFront(true, FrontArmStretch, armRotation + ExtraFrontArmRotation * currentDirection);
            Owner.SetCompositeArmBack(true, BackArmStretch, armRotation + ExtraBackArmRotation * currentDirection);
            if (KeepRefreshingLifetime)
                Projectile.timeLeft = 2;
            if (OffsetLengthFromArm != MaxOffsetLengthFromArm)
                OffsetLengthFromArm = MathHelper.Lerp(OffsetLengthFromArm, MaxOffsetLengthFromArm, RecoilResolveSpeed);
            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }
        /// <summary>子类的实际行为都写在这里</summary>
        public abstract void HoldoutAI();
        // ── 绘制 ──
        /// <summary>按朝向绘制本体；受重力方向影响时需要垂直翻转并把旋转原点镜像到下半张图</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 rotationPoint = texture.Size() * 0.5f;
            float drawRotation = Projectile.rotation;
            SpriteEffects flipSprite = SpriteEffects.None;
            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    flipSprite = SpriteEffects.FlipVertically;
            }
            else
            {
                rotationPoint.Y = texture.Height - rotationPoint.Y;
                if (Projectile.spriteDirection == 1)
                    flipSprite = SpriteEffects.FlipVertically;
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), drawRotation, rotationPoint, Projectile.scale, flipSprite, 0);
            return false;
        }
        // ── 网络同步 ──
        /// <summary>
        /// 同步持握状态（朝向、是否刷新存活、后坐位移、贴图方向）。本方法封死以免子类不小心漏同步，
        /// 子类需要额外同步请用 <see cref="SendExtraAIHoldout"/>
        /// </summary>
        public sealed override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.rotation);
            writer.Write(KeepRefreshingLifetime);
            writer.Write(OffsetLengthFromArm);
            writer.Write7BitEncodedInt(Projectile.spriteDirection);
            SendExtraAIHoldout(writer);
        }
        /// <summary>与 <see cref="SendExtraAI"/> 对应的读取端</summary>
        public sealed override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.rotation = reader.ReadSingle();
            KeepRefreshingLifetime = reader.ReadBoolean();
            OffsetLengthFromArm = reader.ReadSingle();
            Projectile.spriteDirection = reader.Read7BitEncodedInt();
            ReceiveExtraAIHoldout(reader);
        }
        /// <summary>子类的额外同步写出（复用同一个 writer）</summary>
        public virtual void SendExtraAIHoldout(BinaryWriter writer) { }
        /// <summary>子类的额外同步读入（复用同一个 reader）</summary>
        public virtual void ReceiveExtraAIHoldout(BinaryReader reader) { }
    }
}
