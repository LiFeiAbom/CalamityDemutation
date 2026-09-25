using CalamityDemutation.Effects;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 裂空（NxCrack，移植自 CalamityEntropy 的 Content/Projectiles/NxCrack.cs）：
    /// 沧溟渊龙命中时在目标前方撕开的一道"空间裂缝"。它本身不移动（<see cref="ShouldUpdatePosition"/> 返 false），
    /// 命中判定是沿 <c>Center → Center + velocity × 600</c> 的一条宽 60 的长条；
    /// 视觉由 <see cref="drawCrack"/> 用 <see cref="ColoredVertex"/> 三角带画出，**不在本类 PreDraw 里画**——
    /// 与鞭子的深渊裂纹同一套接法：由 <c>EffectsSystem</c> 的深渊上屏分支遍历弹幕时调用，
    /// 画进 screenTargetSwap 当遮罩、再用 cabyss 着色器染成蓝色裂缝叠回屏幕。
    /// <para>
    /// 与 CE 原版的差异：① <c>CEUtils.LineThroughRect</c> → 本工程既有的 <c>CDUtil.LineThroughRect</c>；
    /// ② <c>CEExtraAssets.white</c> → 本模组 <c>Assets/ExtraTextures/white</c>（CE 那个同名 <c>NxCrack.png</c> 只是注册占位图）；
    /// ③ CE 的 <c>Colliding</c> 里有一句取了却没用到的 <c>Main.player[Projectile.owner]</c>，删掉；
    /// ④ CE 从不给本弹幕设 <c>Projectile.rotation</c>（恒 0），所以那条三角带里 <c>(0,±1).RotatedBy(rotation)</c>
    /// 的垂直抖动方向其实**不随目标方向转**——这是 CE 原状，照抄不改。
    /// </para>
    /// </summary>
    internal class NxCrack:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>当前裂缝宽度倍率（从 0.1 起，每帧加上 <see cref="wj"/> 后 wj 自身递减）</summary>
        private float w = 0.1f;
        /// <summary>宽度增量，每帧 -0.065；<see cref="w"/> 被它推到负数即裂缝收口完毕、自行 Kill</summary>
        private float wj = 0.3f;
        // ── 生命周期方法 ──
        /// <summary>注册为仆从弹幕（让它吃到召唤 tag 加伤），1 帧贴图</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        /// <summary>
        /// 基础属性：召唤伤害、无限穿透、不撞地形、每个敌人独立命中冷却；
        /// 命中无敌帧取 7（见下方注释），护甲穿透 100
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            // 命中无敌帧：CE 原样是 16，2026-09-22 用户拍板改成 7。
            // 注：本弹幕 w 收口后约 11 帧就自杀，7 帧冷却意味着一道裂空对同一敌人最多命中 2 次
            Projectile.localNPCHitCooldown = 7;
            Projectile.ArmorPenetration = 100;
        }
        // ── 覆写方法 ──
        /// <summary>占位贴图：真正的裂缝由 <see cref="drawCrack"/> 用这张白图当顶点带纹理画</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>
        /// AI：每帧把宽度增量 <see cref="wj"/> 加到 <see cref="w"/> 上并让 wj 自身递减 0.065，
        /// 宽度收成负数即裂缝合拢完毕、自行 Kill（不移动，见 <see cref="ShouldUpdatePosition"/>）。
        /// </summary>
        public override void AI()
        {
            w += wj;
            wj -= 0.065f;
            if (w < 0)
            {
                Projectile.Kill();
                return;
            }
            Projectile.ai[0]++;   // ai[0]：CE 原样只自增、全工程无人读取（死计数器），保留以对齐原版
        }
        /// <summary>位置由生成时给定、之后完全不动（裂缝是"空间被撕开"而不是飞行的东西）</summary>
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        /// <summary>命中判定：沿裂缝方向 600 像素长、线宽 60 的线段-矩形测试</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity * 600f, targetHitbox, 60);
        }
        /// <summary>不在此处绘制：裂缝由 <see cref="drawCrack"/> 走上屏管线（EffectsSystem 的深渊分支）合成</summary>
        public override bool PreDraw(ref Color dc)
        {
            return false;
        }
        /// <summary>不能砍草（虚幻的空间裂缝）</summary>
        public override bool? CanCutTiles()
        {
            return false;
        }
        // ── 上屏绘制 ──
        /// <summary>
        /// 把裂缝画成一条 <see cref="ColoredVertex"/> 三角带（由 EffectsSystem 的深渊上屏分支调用）：
        /// 沿 <c>velocity × i × 60</c> 摆 10 组顶点，每组上下两点，横向抖动由 c/d 两张权重表 + 时间余弦驱动，
        /// 整体宽度乘当前收口倍率 <see cref="w"/>。顶点直接以 <c>DrawUserPrimitives</c> 提交，用调用方已开好的批次。
        /// </summary>
        public void drawCrack()
        {
            List<float> c = new List<float>() { 0, 1.2f, 0.8f, 1f, 1.27f, 0.9f, 1.2f, 1f, 0.8f, 0 };
            List<float> d = new List<float>() { 0, 0.3f, 0.56f, 0.8f, 0.94f, 0.94f, 0.8f, 0.56f, 0.3f, 0 };
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color color = Color.White;
            for (int i = 0; i < c.Count; i++)
            {
                Vector2 basePos = Projectile.Center - Main.screenPosition + Projectile.velocity * (i * (600f / c.Count));
                float halfWidth = d[i] * 0.7f * (24 + (float)(Math.Cos(c[i] * Main.GlobalTimeWrappedHourly * 3) + 1) * 2) * w;
                float wobble = (float)Math.Cos(c[c.Count - 1 - i] * Main.GlobalTimeWrappedHourly * 4) * d[d.Count - 1 - i] * 0.4f * 2 * w;
                Vector3 texCoord = new Vector3((float)i / c.Count, 1, 1);
                ve.Add(new ColoredVertex(basePos + new Vector2(0, -1).RotatedBy(Projectile.rotation) * halfWidth + new Vector2(0, 1).RotatedBy(Projectile.rotation) * wobble, texCoord, color));
                ve.Add(new ColoredVertex(basePos + new Vector2(0, 1).RotatedBy(Projectile.rotation) * halfWidth + new Vector2(0, 1).RotatedBy(Projectile.rotation) * wobble, texCoord, color));
            }
            if (ve.Count >= 3)
            {
                Texture2D px = ModContent.Request<Texture2D>(Texture).Value;
                GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
                graphicsDevice.Textures[0] = px;
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
        }
    }
}
