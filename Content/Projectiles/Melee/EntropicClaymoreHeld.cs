using CalamityDemutation.Content.Projectiles.Melee.Core;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 熵之舞手持挥砍体（移植自灾厄大修 **0.4.0.3.5** 的 <c>EntropicClaymoreHeld</c>）：
    /// 走 <c>BaseKnife</c> 的 <c>UpAndDown</c> 挥舞曲线（左右两段交替、奇偶挥舞镜像），
    /// 每 20×攻速 帧朝准心射出一枚 <see cref="EntropicClaymoreProj"/>（伤害同手持体），
    /// 出弹点沿刀身法线左右错开、并随发数前推；
    /// 出膛方向随发数在准心两侧**张开成扇面**、同时速度逐发递增（见 <see cref="Shoot"/>）。
    /// 剑身**不走常规绘制管线**（<see cref="DrawSwing"/> 空实现），改由 <see cref="IDrawWarp"/> 的
    /// costomDraw 画在屏幕扭曲结果之上（大修同款接法），扭曲遮罩则由 <c>WarpDraw()</c> 画刀光弧线。
    /// </summary>
    internal class EntropicClaymoreHeld : BaseSwingCO, IDrawWarp
    {
        /// <summary>剑身贴图直接复用熵之舞的物品贴图（大修同理）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/EntropicClaymore";
        /// <summary>
        /// 挥舞参数包：大修把它放在 BaseKnife 基类上，本工程 BaseSwingCO 没有该字段，故子类自持一份
        /// </summary>
        private SwingDataStruct SwingData = new();
        /// <summary>
        /// 挥舞基础属性初始化（由基类 <c>SetDefaults</c> 回调）：
        /// 额外更新 4（每帧 5 次更新）、锁玩家朝向、本地无敌帧 10×5、
        /// 判定箱 96×96、弧光参数照抄大修 SetKnifeProperty
        /// </summary>
        public override void SetSwingProperty()
        {
            Projectile.extraUpdates = 4;
            ownerOrientationLock = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10 * updateCount;
            Projectile.width = Projectile.height = 96;
            CanDrawSlashTrail = true;
            distanceToOwner = 20;
            drawTrailBtommWidth = 80;
            drawTrailTopWidth = 60;
            drawTrailCount = 46;
            Length = 122;
            unitOffsetDrawZkMode = -16;
            overOffsetCachesRoting = MathHelper.ToRadians(16);
        }
        /// <summary>
        /// 首帧初始化（由基类 <c>PreUpdate</c> 在 Time==0 时回调）：
        /// 挥砍周期对齐物品 useTime（28 帧）、离心量取判定箱半宽、挥舞索引 0/1 交替。
        /// 伤害类型不动——大修 BaseSwing 的 SetDefaults 给的就是 <c>DamageClass.Melee</c>（本工程 BaseSwingCO 同）。
        /// </summary>
        public override void Initialize()
        {
            maxSwingTime = Item.useTime;
            SwingData.maxSwingTime = maxSwingTime;
            toProjCoreMode = Projectile.width / 2f;
            if (++SwingIndex > 1)
            {
                SwingIndex = 0;
            }
        }
        /// <summary>
        /// 挥舞逻辑：<c>UpAndDown</c> 内联（本工程没有 SwingAIType 枚举）——
        /// 奇数挥舞索引时对角线翻转，起始角 +120°、基础角速度取反，形成上下来回的两段弧
        /// </summary>
        public override void SwingAI()
        {
            SwingDataStruct swingData = SwingData;
            if (SwingIndex == 1)
            {
                inDrawFlipdiagonally = true;
                swingData.starArg += 120;
                swingData.baseSwingSpeed *= -1;
            }
            SwingBehavior(swingData);
        }
        /// <summary>
        /// 到点射出一枚熵之飞刃：音高按已发数递增。
        /// 出弹点 = 玩家稳定中心沿鼠标前推 Length/2，再沿刀身法线按 <c>ai[2]</c> 左右错开、沿射向按 <c>ai[2]</c> 回退，最后沿射向前推 100；
        /// 出膛方向在准心两侧**张开成扇面**（每发差 0.1 弧度，第 3 发起居中），速度按 <c>1 + ai[2]×0.1</c> 逐发递增；
        /// 伤害不折减，与手持体同值。
        /// </summary>
        public override void Shoot()
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.ELRFire with { Pitch = Projectile.ai[2] * 0.15f, Volume = 0.45f }, Projectile.Center);
            int dir = -Math.Sign(rotSpeed);
            Vector2 spwanPos = ShootSpanPos + ShootVelocity.GetNormalVector() * (Projectile.ai[2] * 20 * dir) - ShootVelocity * (Projectile.ai[2] * 6) + ShootVelocity.UnitVector() * 100;
            Projectile.NewProjectile(Source, spwanPos, ShootVelocity.RotatedBy((-2 + Projectile.ai[2]) * 0.1f * -dir) * (1 + Projectile.ai[2] * 0.1f)
                , ModContent.ProjectileType<EntropicClaymoreProj>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
        /// <summary>
        /// 挥舞前的每帧处理：约 1/3 概率在判定箱内洒落暗影焰尘；
        /// 累加 <c>ai[1]</c>，每满 20×攻速 帧就置位发射窗口并把 <c>ai[2]</c>（已发数）推一档
        /// </summary>
        public override bool PreInOwnerUpdate()
        {
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame);
            }
            if (++Projectile.ai[1] > (20 * SetSwingSpeed(1)))
            {
                canShoot = true;
                Projectile.ai[1] = 0;
                Projectile.ai[2]++;
            }
            return true;
        }
        // ── IDrawWarp：剑身画在屏幕扭曲结果之上 ──
        /// <summary>允许 EffectsSystem 在扭曲结果之上再调用 costomDraw 绘制剑身</summary>
        bool IDrawWarp.canDraw() => true;
        /// <summary>走"无蓝移"桶：剑身背后那片扭曲区域不该泛蓝（大修同款）</summary>
        bool IDrawWarp.noBlueshift() => true;
        /// <summary>扭曲遮罩：把刀光弧线画进 screenTargetSwap，由 WarpShader 位移屏幕像素</summary>
        void IDrawWarp.Warp() => WarpDraw();
        /// <summary>
        /// 把剑身贴图本体画在扭曲结果之上。绘制逻辑与基类 <c>DrawSwing</c> 一致
        /// （逆挥垂直翻转、对角线翻转、扣离心量、叠加手臂法线偏移），
        /// 差别只在光照取样与缩放：大修此处用 <c>MeleeSize</c>，本工程无该字段，等价于 <c>Projectile.scale</c>
        /// </summary>
        void IDrawWarp.costomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureValue;
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawOrigin = new Vector2(texture.Width / 2, texture.Height / 2);
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 offsetOwnerPos = safeInSwingUnit.GetNormalVector() * unitOffsetDrawZkMode * Projectile.spriteDirection;
            float drawRoting = Projectile.rotation;
            if (Projectile.spriteDirection == -1)
            {
                drawRoting += MathHelper.Pi;
            }
            //烦人的对角线翻转代码，我凑出来了这个效果，它很稳靠，但我仍旧不想细究这其中的数学逻辑
            if (inDrawFlipdiagonally)
            {
                effects = Projectile.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                drawRoting += MathHelper.PiOver2;
                offsetOwnerPos *= -1;
            }
            Vector2 drawPosValue = Projectile.Center - RodingToVer(toProjCoreMode, (Projectile.Center - Owner.Center).ToRotation()) + offsetOwnerPos;
            Main.EntitySpriteDraw(texture, drawPosValue - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY, new Rectangle?(rect)
                , Projectile.GetAlpha(Lighting.GetColor(new Point((int)(Projectile.Center.X / 16), (int)(Projectile.Center.Y / 16))))
                , drawRoting, drawOrigin, Projectile.scale, effects, 0);
        }
        /// <summary>剑身不走常规绘制管线，改由 IDrawWarp 的 costomDraw 画（大修同为空实现）</summary>
        public override void DrawSwing(SpriteBatch spriteBatch, Color lightColor) { }
    }
}
