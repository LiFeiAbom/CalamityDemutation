using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 深渊裂隙（AbyssalCrack，移植自 CalamityEntropy）：分形深渊刃起手冲刺时撕开的一道空间裂缝。
    /// 前 16 帧沿冲刺方向逐帧追加折线采样点（带随机抖动），此后停在原地；命中判定沿整条折线取线段，
    /// 护甲穿透 128。本体不绘制任何东西 —— 绘制由 EffectsSystem 在深渊裂隙的上屏合成里调用
    /// <see cref="DrawCrack"/> 完成，再经 cabyss 着色器染成蓝色裂缝。
    /// <para>
    /// 与 CE 原版的差异：① CE 把绘制函数挂在 <c>EffectLoader</c> 的巨型上屏函数里按类型分发，
    /// 本工程改为公开 <see cref="DrawCrack"/> 供 EffectsSystem 调用；② <c>CEUtils.randomVec</c>、
    /// <c>CEUtils.LineThroughRect</c> 与 <c>CEUtils.drawLine</c> 在 CE 侧属于工具库，这里内联；
    /// ③ CE 的 <c>CEExtraAssets.white</c> 换成本模组 Assets/ExtraTextures/white。
    /// </para>
    /// </summary>
    internal class AbyssalCrack:ModProjectile
    {
        /// <summary>折线采样点（世界坐标），每帧沿飞行方向追加一段</summary>
        public List<Vector2> points = new List<Vector2>();
        /// <summary>已生成帧数，超过 16 帧后裂缝不再延伸</summary>
        private int d = 0;
        /// <summary>占位贴图（本体由 DrawCrack 用白图自绘）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 128;
            Projectile.timeLeft = 48;
        }
        public override void AI()
        {
            d++;
            if (d > 16)
            {
                Projectile.velocity *= 0;
            }
            else
            {
                // 从上一次采样点朝当前位置插 10 个中间点，让裂缝看起来是连续撕开的
                Vector2 o = (points.Count > 0 ? points[points.Count - 1] : Projectile.Center - Projectile.velocity);
                Vector2 nv = Projectile.Center + RandomVec(4);
                for (float i = 0.1f; i <= 1; i += 0.1f)
                {
                    points.Add(Vector2.Lerp(o, nv, i));
                }
            }
        }
        /// <summary>命中判定：沿整条折线逐段做线段-矩形测试（线宽 30）</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (points.Count < 1)
            {
                return false;
            }
            for (int i = 1; i < points.Count; i++)
            {
                if (LineThroughRect(points[i - 1], points[i], targetHitbox, 30))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor) => false;
        /// <summary>
        /// 上屏绘制（由 EffectsSystem 在屏幕空间调用，不带视图矩阵，自行减去 screenPosition）：
        /// 沿折线逐段拉一条白线，线宽随线段在裂缝上的位置呈半圆起伏（CE 原式）
        /// </summary>
        public void DrawCrack()
        {
            if (points.Count < 1)
            {
                return;
            }
            Texture2D px = ModContent.Request<Texture2D>(Texture).Value;
            float lw = Projectile.timeLeft / 30f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 jv = Vector2.Zero;
                DrawLine(Main.spriteBatch, px, points[i - 1], points[i] + jv, Color.White,
                    1f * lw * new Vector2(-30, 0).RotatedBy(MathHelper.ToRadians(180 * ((float)i / points.Count))).Y, 3);
            }
        }
        /// <summary>CEUtils.drawLine 的等价实现：以白图为笔、按线段长度横向拉伸，起点为笔画中心</summary>
        private static void DrawLine(SpriteBatch spriteBatch, Texture2D px, Vector2 start, Vector2 end, Color color, float width, int wa)
        {
            spriteBatch.Draw(px, start - Main.screenPosition, null, color, (end - start).ToRotation(), new Vector2(0, 0.5f), new Vector2(Vector2.Distance(start, end) + wa, width), SpriteEffects.None, 0f);
        }
        /// <summary>CEUtils.randomVec 的等价实现：横纵各取 [-max, max] 的随机偏移</summary>
        private static Vector2 RandomVec(float max) => new Vector2(Main.rand.NextFloat(-max, max), Main.rand.NextFloat(-max, max));
        /// <summary>CEUtils.LineThroughRect 的等价实现：两端点落在矩形内，或线段与矩形相交</summary>
        private static bool LineThroughRect(Vector2 start, Vector2 end, Rectangle rect, int lineWidth)
        {
            float point = 0f;
            return rect.Contains((int)start.X, (int)start.Y) || rect.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(rect.TopLeft(), rect.Size(), start, end, lineWidth, ref point);
        }
    }
}
