using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 中子光束（NeutronLaser） - 中子枪（NeutronGun）每次开火都会甩出的一道细光束，
    /// 以及蓄力弹命中后从高空落下的三道光束，移植自 CWR 的 NeutronLaser。
    /// <para>
    /// 光束本体不画单体，而是沿速度方向铺一串彼此间隔 3 像素的贴图（CWR 的
    /// <c>Projectile.DrawBeam(200f, 3f, ...)</c>，本工程就地等价实现，见 <see cref="PreDraw"/>）：
    /// <c>localAI[0]</c> 每帧 +3、上限 100，代表这一串铺多少节，因此光束看起来是"射出去后越伸越长"。
    /// </para>
    /// <para>
    /// CWR 侧本类继承自 <c>CloneDefaults(灾厄 DrataliornusExoArrow)</c>，本工程引用不到灾厄的弹幕类型，
    /// 故把这些克隆来的基础属性在 <see cref="SetDefaults"/> 里逐条内联（贴图同样改用从灾厄
    /// LaserProj 复制进来的本地贴图）。
    /// </para>
    /// </summary>
    internal class NeutronLaser : ModProjectile
    {
        // ── 常量 ──
        /// <summary>光束的名义长度（CWR 调 DrawBeam 的第一个实参），决定各节透明度从满到零的衰减基准</summary>
        private const float BeamLength = 200f;
        /// <summary>相邻两节之间的像素间距（CWR 调 DrawBeam 的第二个实参）</summary>
        private const float BeamSpacer = 3f;
        /// <summary>贴图取自灾厄的 LaserProj（6×10 的亮斑），已复制进工程并与 .cs 同名</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Ranged/NeutronLaser";
        /// <summary>
        /// 基础属性：即 <c>CloneDefaults(DrataliornusExoArrow)</c> 的内联等价物
        /// （5×5 判定箱、全隐淡入、无限穿透、每帧更新 5 次、存活 60 帧、不撞地形、忽略水体、远程系），
        /// 再叠加 CWR 覆盖的三项：同敌人 1 帧冷却与 80 点护甲穿透
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 4;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.aiStyle = -1;                 // 走本类的 AI，不套原版模板
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.ArmorPenetration = 80;
        }
        /// <summary>
        /// 每帧：淡入（alpha 每帧 -25），并推进光束长度——<c>ai[1] == 0</c> 时每帧 +3 直到 100，
        /// 否则每帧 -3、收到 0 以下即销毁（供需要"收束"的生成方使用）。
        /// 同时每帧喷一颗蓝紫 DRK_Spark 火花
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            const float inc = 3f;
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += inc;
                if (Projectile.localAI[0] > 100f)
                    Projectile.localAI[0] = 100f;
            }
            else
            {
                Projectile.localAI[0] -= inc;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }
            BaseParticle spark = new DRK_Spark(Projectile.Center, Projectile.velocity, false, 10
                , Main.rand.NextFloat(1.2f, 2.3f), Color.BlueViolet);
            DRKLoader.AddParticle(spark);
        }
        /// <summary>
        /// 命中敌怪：把该敌人的受击无敌帧清零，使光束的每一节都能立刻再次造成伤害
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.immune[Projectile.owner] = 0;
        /// <summary>光束主色为青蓝，透明度跟随淡入</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(0, 125, 210, Projectile.alpha);
        /// <summary>
        /// 自绘光束：从本体位置起沿速度的反方向，每 3 像素铺一节贴图，共 <c>localAI[0]</c> 节，
        /// 越靠后的一节越透明（按 BeamLength 线性衰减），颜色 A 通道归零以走加色观感。
        /// 这等价于灾厄的 <c>Projectile.DrawBeam</c>（CWR 原版就是调它）。
        /// <para>
        /// 注意：CWR 原版这一类**从不设置 Projectile.rotation**，所以每节贴图恒以 90° 绘制，
        /// 只有整条链的行进方向跟随速度。本工程照抄该行为，未做"顺手修正"
        /// </para>
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new Vector2((texture.Width - Projectile.width) * 0.5f + Projectile.width * 0.5f, Projectile.height / 2f);
            SpriteEffects spriteEffects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Rectangle roughScreenBounds = new Rectangle((int)Main.screenPosition.X - 500, (int)Main.screenPosition.Y - 500
                , Main.screenWidth + 1000, Main.screenHeight + 1000);
            if (Projectile.getRect().Intersects(roughScreenBounds))
            {
                Vector2 drawPos = Projectile.position - Main.screenPosition + origin;
                drawPos.Y += Projectile.gfxOffY;
                Vector2 cumulativeOffset = Vector2.Zero;
                Color alpha = Projectile.GetAlpha(lightColor);
                float fixedRotation = Projectile.rotation + MathHelper.PiOver2;
                for (int i = 1; i <= (int)Projectile.localAI[0]; i++)
                {
                    cumulativeOffset += Vector2.Normalize(Projectile.velocity) * BeamSpacer;
                    Color color = alpha * ((BeamLength - i) / BeamLength);
                    color.A = 0;
                    Main.spriteBatch.Draw(texture, drawPos - cumulativeOffset, null, color, fixedRotation, origin, Projectile.scale, spriteEffects, 0f);
                }
            }
            return false;
        }
    }
}
