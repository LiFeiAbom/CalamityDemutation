using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
namespace CalamityDemutation.Content.Projectiles.BaseProjectiles
{
    /// <summary>
    /// 手持弹幕基类（移植自 CWR 的 BaseHeldProj）：处理玩家到鼠标的向量、左右键状态与联机同步。
    /// </summary>
    internal abstract class BaseHeldProjCO : ModProjectile
    {
        private bool old_downLeftValue;
        private bool downLeftValue;
        private bool old_downRightValue;
        private bool downRightValue;
        /// <summary>
        /// 玩家左键控制
        /// </summary>
        protected bool DownLeft { get; private set; }
        /// <summary>
        /// 玩家右键控制
        /// </summary>
        protected bool DownRight { get; private set; }
        /// <summary>
        /// 一般情况下我们默认该弹幕的玩家作为弹幕主人
        /// </summary>
        internal virtual Player Owner => Main.player[Projectile.owner];
        /// <summary>
        /// 安全的获取一个重力倒转值
        /// </summary>
        internal int SafeGravDir => Math.Sign(Owner.gravDir);
        /// <summary>
        /// 弹幕的理论朝向
        /// </summary>
        internal virtual int DirSign => Owner.direction * SafeGravDir;
        /// <summary>
        /// 获取玩家到鼠标的向量
        /// </summary>
        internal virtual Vector2 ToMouse { get; private protected set; }
        /// <summary>
        /// 获取玩家鼠标的位置
        /// </summary>
        internal virtual Vector2 InMousePos { get; private protected set; }
        /// <summary>
        /// 获取玩家到鼠标的角度
        /// </summary>
        internal virtual float ToMouseA { get; private protected set; }
        /// <summary>
        /// 获取玩家鼠标的单位向量
        /// </summary>
        internal virtual Vector2 UnitToMouseV { get; private protected set; }
        /// <summary>
        /// 这个值用于在联机同步中使用
        /// </summary>
        private Vector2 toMouseVecterDate;
        private Vector2 _old_toMouseVecterDate;
        private const float toMouseVer_variationMode = 0.5f;
        /// <summary>
        /// 是否处于开火时间
        /// </summary>
        public virtual bool CanFire => false;
        /// <summary>
        /// 单独处理玩家到鼠标的方向向量，同时处理对应的网络逻辑
        /// </summary>
        private Vector2 UpdateToMouse() {
            if (Projectile.IsOwnedByLocalPlayer()) {
                toMouseVecterDate = Owner.GetPlayerStabilityCenter().To(Main.MouseWorld);
                int grgDir = 1;
                if (CalamityDemutation.Instance.gravityDontFlipScreen != null && SafeGravDir < 0) {
                    grgDir *= -1;
                }
                toMouseVecterDate.Y *= grgDir;
                bool difference = Math.Abs(toMouseVecterDate.X - _old_toMouseVecterDate.X) > toMouseVer_variationMode
                    || Math.Abs(toMouseVecterDate.Y - _old_toMouseVecterDate.Y) > toMouseVer_variationMode;
                if (difference && CanFire) {
                    NetUpdate();
                }
                _old_toMouseVecterDate = toMouseVecterDate;
            }
            return toMouseVecterDate;
        }
        /// <summary>
        /// 处理左键点击的更新逻辑
        /// </summary>
        private bool UpdateDownLeftStart() {
            if (Projectile.IsOwnedByLocalPlayer()) {
                downLeftValue = Owner.PressKey();
                if (old_downLeftValue != downLeftValue) {
                    NetUpdate();
                }
                old_downLeftValue = downLeftValue;
            }
            return downLeftValue;
        }
        /// <summary>
        /// 处理右键点击的更新逻辑
        /// </summary>
        private bool UpdateDownRightStart() {
            if (Projectile.IsOwnedByLocalPlayer()) {
                downRightValue = Owner.PressKey(false);
                if (old_downRightValue != downRightValue) {
                    NetUpdate();
                }
                old_downRightValue = downRightValue;
            }
            return downRightValue;
        }
        /// <summary>
        /// 更新玩家到鼠标的相关数据
        /// </summary>
        private void UpdateMouseData() {
            DownLeft = UpdateDownLeftStart();
            DownRight = UpdateDownRightStart();
            ToMouse = UpdateToMouse();
            ToMouseA = ToMouse.ToRotation();
            UnitToMouseV = ToMouse.UnitVector();
            InMousePos = ToMouse + Owner.GetPlayerStabilityCenter();
        }
        /// <summary>
        /// 在AI更新前进行数据更新
        /// </summary>
        public sealed override bool PreAI() {
            UpdateMouseData();
            ExtraPreSet();
            return PreUpdate();
        }
        public sealed override void PostAI() {
            if (!Owner.Alives()) {
                Projectile.Kill();
            }
        }
        /// <summary>
        /// 发送一个比特体，存储8个栏位的布尔值
        /// </summary>
        public virtual BitsByte SandBitsByte(BitsByte flags) {
            flags[0] = downLeftValue;
            flags[1] = downRightValue;
            return flags;
        }
        /// <summary>
        /// 接受一个比特体，最多处理8个布尔属性的网络更新
        /// </summary>
        public virtual void ReceiveBitsByte(BitsByte flags) {
            downLeftValue = flags[0];
            downRightValue = flags[1];
        }
        public sealed override void SendExtraAI(BinaryWriter writer) {
            writer.WriteVector2(toMouseVecterDate);
            writer.Write(SandBitsByte(new BitsByte()));
            NetCodeHeldSend(writer);
        }
        public sealed override void ReceiveExtraAI(BinaryReader reader) {
            toMouseVecterDate = reader.ReadVector2();
            ReceiveBitsByte(reader.ReadByte());
            NetCodeReceiveHeld(reader);
        }
        public virtual void NetCodeHeldSend(BinaryWriter writer) { }
        public virtual void NetCodeReceiveHeld(BinaryReader reader) { }
        public virtual void ExtraPreSet() { }
        public virtual bool PreUpdate() {
            return true;
        }
        protected void SetHeld() => Owner.heldProj = Projectile.whoAmI;
        protected void SetDirection() => Owner.direction = Math.Sign(ToMouse.X);
        protected void NetUpdate() => Projectile.netUpdate = true;
    }
}
