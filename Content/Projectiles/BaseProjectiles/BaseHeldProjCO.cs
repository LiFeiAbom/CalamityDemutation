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
        // ── 常量 ──
        /// <summary>
        /// 鼠标向量变化的判定阈值：X/Y 任一分量变化超过该值且可开火时触发一次网络同步
        /// </summary>
        private const float toMouseVer_variationMode = 0.5f;
        // ── 实例字段 ──
        /// <summary>
        /// 本帧左键按下状态（本地玩家更新，联机下由比特体同步）
        /// </summary>
        private bool downLeftValue;
        /// <summary>
        /// 本帧右键按下状态（本地玩家更新，联机下由比特体同步）
        /// </summary>
        private bool downRightValue;
        /// <summary>
        /// 上一帧左键状态，用于检测变化并触发网络同步
        /// </summary>
        private bool old_downLeftValue;
        /// <summary>
        /// 上一帧右键状态，用于检测变化并触发网络同步
        /// </summary>
        private bool old_downRightValue;
        /// <summary>
        /// 上一帧的鼠标向量，用于与当前帧比较是否超过 toMouseVer_variationMode
        /// </summary>
        private Vector2 _old_toMouseVecterDate;
        /// <summary>
        /// 当前鼠标向量（玩家稳定中心 → 鼠标世界坐标），联机同步字段
        /// </summary>
        private Vector2 toMouseVecterDate;
        // ── 属性 ──
        /// <summary>
        /// 是否处于开火时间
        /// </summary>
        public virtual bool CanFire => false;
        /// <summary>
        /// 弹幕的理论朝向
        /// </summary>
        internal virtual int DirSign => Owner.direction * SafeGravDir;
        /// <summary>
        /// 玩家左键控制
        /// </summary>
        protected bool DownLeft { get; private set; }
        /// <summary>
        /// 玩家右键控制
        /// </summary>
        protected bool DownRight { get; private set; }
        /// <summary>
        /// 获取玩家鼠标的位置
        /// </summary>
        internal virtual Vector2 InMousePos { get; private protected set; }
        /// <summary>
        /// 一般情况下我们默认该弹幕的玩家作为弹幕主人
        /// </summary>
        internal virtual Player Owner => Main.player[Projectile.owner];
        /// <summary>
        /// 安全的获取一个重力倒转值
        /// </summary>
        internal int SafeGravDir => Math.Sign(Owner.gravDir);
        /// <summary>
        /// 获取玩家到鼠标的向量
        /// </summary>
        internal virtual Vector2 ToMouse { get; private protected set; }
        /// <summary>
        /// 获取玩家到鼠标的角度
        /// </summary>
        internal virtual float ToMouseA { get; private protected set; }
        /// <summary>
        /// 获取玩家鼠标的单位向量
        /// </summary>
        internal virtual Vector2 UnitToMouseV { get; private protected set; }
        // ── 生命周期方法 ──
        /// <summary>
        /// 在AI更新前进行数据更新
        /// </summary>
        public sealed override bool PreAI()
        {
            UpdateMouseData();
            ExtraPreSet();
            return PreUpdate();
        }
        /// <summary>
        /// AI 更新后检查：玩家不再存活时销毁弹幕
        /// </summary>
        public sealed override void PostAI()
        {
            if (!Owner.Alives())
            {
                Projectile.Kill();
            }
        }
        /// <summary>
        /// 联机同步：写出鼠标向量、按键比特体，再交给子类的 NetCodeHeldSend 补充额外数据
        /// </summary>
        public sealed override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(toMouseVecterDate);
            writer.Write(SandBitsByte(new BitsByte()));
            NetCodeHeldSend(writer);
        }
        /// <summary>
        /// 联机同步：读入鼠标向量、按键比特体，再交给子类的 NetCodeReceiveHeld 补充额外数据
        /// </summary>
        public sealed override void ReceiveExtraAI(BinaryReader reader)
        {
            toMouseVecterDate = reader.ReadVector2();
            ReceiveBitsByte(reader.ReadByte());
            NetCodeReceiveHeld(reader);
        }
        // ── 公开方法 ──
        /// <summary>
        /// 子类在 PreAI 数据更新后、PreUpdate 前的额外处理钩子，默认为空
        /// </summary>
        public virtual void ExtraPreSet() { }
        /// <summary>
        /// 子类的额外网络写出钩子，默认为空
        /// </summary>
        public virtual void NetCodeHeldSend(BinaryWriter writer) { }
        /// <summary>
        /// 子类的额外网络读入钩子，默认为空
        /// </summary>
        public virtual void NetCodeReceiveHeld(BinaryReader reader) { }
        /// <summary>
        /// 子类在 PreAI 中的更新钩子；返回 false 时跳过本帧 AI
        /// </summary>
        public virtual bool PreUpdate()
        {
            return true;
        }
        /// <summary>
        /// 标记弹幕需要网络同步
        /// </summary>
        protected void NetUpdate() => Projectile.netUpdate = true;
        /// <summary>
        /// 接受一个比特体，最多处理8个布尔属性的网络更新
        /// </summary>
        public virtual void ReceiveBitsByte(BitsByte flags)
        {
            downLeftValue = flags[0];
            downRightValue = flags[1];
        }
        /// <summary>
        /// 发送一个比特体，存储8个栏位的布尔值
        /// </summary>
        public virtual BitsByte SandBitsByte(BitsByte flags)
        {
            flags[0] = downLeftValue;
            flags[1] = downRightValue;
            return flags;
        }
        /// <summary>
        /// 让弹幕朝向鼠标水平方向
        /// </summary>
        protected void SetDirection() => Owner.direction = Math.Sign(ToMouse.X);
        /// <summary>
        /// 把本弹幕登记为玩家手持弹幕
        /// </summary>
        protected void SetHeld() => Owner.heldProj = Projectile.whoAmI;
        // ── 私有工具 ──
        /// <summary>
        /// 处理左键点击的更新逻辑
        /// </summary>
        private bool UpdateDownLeftStart()
        {
            if (Projectile.IsOwnedByLocalPlayer())
            {
                downLeftValue = Owner.PressKey();
                if (old_downLeftValue != downLeftValue)
                {
                    NetUpdate();
                }
                old_downLeftValue = downLeftValue;
            }
            return downLeftValue;
        }
        /// <summary>
        /// 处理右键点击的更新逻辑
        /// </summary>
        private bool UpdateDownRightStart()
        {
            if (Projectile.IsOwnedByLocalPlayer())
            {
                downRightValue = Owner.PressKey(false);
                if (old_downRightValue != downRightValue)
                {
                    NetUpdate();
                }
                old_downRightValue = downRightValue;
            }
            return downRightValue;
        }
        /// <summary>
        /// 更新玩家到鼠标的相关数据
        /// </summary>
        private void UpdateMouseData()
        {
            DownLeft = UpdateDownLeftStart();
            DownRight = UpdateDownRightStart();
            ToMouse = UpdateToMouse();
            ToMouseA = ToMouse.ToRotation();
            UnitToMouseV = ToMouse.UnitVector();
            InMousePos = ToMouse + Owner.GetPlayerStabilityCenter();
        }
        /// <summary>
        /// 单独处理玩家到鼠标的方向向量，同时处理对应的网络逻辑
        /// </summary>
        private Vector2 UpdateToMouse()
        {
            if (Projectile.IsOwnedByLocalPlayer())
            {
                toMouseVecterDate = Owner.GetPlayerStabilityCenter().To(Main.MouseWorld);
                int grgDir = 1;
                if (CalamityDemutation.Instance.gravityDontFlipScreen != null && SafeGravDir < 0)
                {
                    grgDir *= -1;
                }
                toMouseVecterDate.Y *= grgDir;
                bool difference = Math.Abs(toMouseVecterDate.X - _old_toMouseVecterDate.X) > toMouseVer_variationMode
                    || Math.Abs(toMouseVecterDate.Y - _old_toMouseVecterDate.Y) > toMouseVer_variationMode;
                if (difference && CanFire)
                {
                    NetUpdate();
                }
                _old_toMouseVecterDate = toMouseVecterDate;
            }
            return toMouseVecterDate;
        }
    }
}
