using CalamityDemutation.Effects;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 虚空斩（VoidSlash，移植自 CalamityEntropy）：虚空分形右键发动的一次突进斩（伤害是物品的 25 倍、护甲穿透 128）。
    /// 起手第 1 帧把摄像机拉慢并朝鼠标方向窜出，之后 16 帧里每帧沿路径补 10 个采样点、同时把玩家中心直接钉在弹幕中心
    /// （这就是"突进"本身）；过 16 帧后速度归零、停下收招。命中判定是逐段检查整条采样折线。
    /// 刀光是一整条三角带，用 <see cref="ColoredVertex"/> 直接送顶点。
    /// <para>
    /// 与 CE 原版的差异：① 贴图从 CE 共享白图换成 <c>Assets/ExtraTextures/white</c>，
    /// <c>CEExtraAssets.MegaStreakBacking2b</c> 换成本模组 Assets/ExtraTextures 下的同名贴图；
    /// ② CE 的 <c>UseBlendState</c> 本机 tML 没有，改用 End + 立即模式 Begin(Immediate, NonPremultiplied, LinearClamp)；
    /// <b>CE 的 PreDraw 结尾没有还原批次</b>（会把它之后同帧的绘制都留在 NonPremultiplied + LinearClamp 下），
    /// 本模组照前几把的既有做法补上默认批次的恢复；③ <c>CEUtils.LineThroughRect</c> 用
    /// <see cref="CDUtil.LineThroughRect"/>，<c>CEUtils.randomVec</c> 内联成 <see cref="RandomVec"/>。
    /// </para>
    /// <para>
    /// <b>未实现（留给第 10 把）</b>：CE 在 <c>ai[0] == 1</c> 时会朝两侧各生成一把 <c>FinalFractalBlade</c>
    /// （属于第 10 把幽邃分形的类）。本武器生成 VoidSlash 时不传 ai，永远走的是 <c>ai[0] == 0</c>，走不到那个分支；
    /// 第 10 把的 <c>FinalFractal</c> 会以 <c>ai[0] = 1</c> 生成 VoidSlash，届时再把这个分支补上。
    /// </para>
    /// </summary>
    internal class VoidSlash:ModProjectile
    {
        /// <summary>本体只用一张 1×1 白图当底，刀光由三角带自绘（CE 的 CEUtils.WhiteTexPath）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>刀光底纹：一张横向条纹贴图，靠顶点色染色（CE 的 CEExtraAssets.MegaStreakBacking2b）</summary>
        private const string SlashTexture = "CalamityDemutation/Assets/ExtraTextures/MegaStreakBacking2b";
        /// <summary>第二、三笔刀光用的纯色底，靠顶点色渐变出锋刃（CE 的 CEUtils.pixelTex）</summary>
        private const string PixelTexture = "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>突进路径的采样点，每帧沿上一点到当前位置补 10 个，命中判定逐段检查这条折线</summary>
        public List<Vector2> points = new List<Vector2>();
        /// <summary>自身帧数计数：第 1 帧起手、超过 16 帧停下（玩家侧钩子也读它判断突进是否还在进行中）</summary>
        public int d = 0;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;                        // 命中判定完全由 Colliding 的折线接管，碰撞箱取最小
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = -1;         // 同一次突进对同一敌人只结算一次
            Projectile.ArmorPenetration = 128;
            Projectile.timeLeft = 36;
        }
        public override void AI()
        {
            d++;
            if (d == 1)
            {
                // 起手：把摄像机拉慢（衬出突进的速度感），并先朝鼠标方向窜出一帧
                Main.SetCameraLerp(0.12f, 25);
                Projectile.Center += Projectile.velocity;
            }
            if (d > 16)
            {
                Projectile.velocity *= 0;
            }
            else
            {
                // 突进期间：沿路径补采样点，并把玩家中心钉在弹幕中心——"玩家被这一斩带过去"就是这句
                Vector2 o = (points.Count > 0 ? points[points.Count - 1] : Projectile.Center - Projectile.velocity);
                Vector2 nv = Projectile.Center + RandomVec(4);
                for (float i = 0.1f; i <= 1; i += 0.1f)
                {
                    points.Add(Vector2.Lerp(o, nv, i));
                }
                Main.player[Projectile.owner].Center = Projectile.Center;
            }
        }
        /// <summary>逐段检查整条突进折线，命中任意一段即算命中（线宽 30）</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (points.Count < 1)
            {
                return false;
            }
            for (int i = 1; i < points.Count; i++)
            {
                if (CDUtil.LineThroughRect(points[i - 1], points[i], targetHitbox, 30))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 把一条直线段摊成上下两条互相错开的包络线：偏移量按 <c>cos(i·2π - π)</c> 在两端归零、正中最大，
        /// 于是两条包络线中间夹出的正是"中间厚、两端尖"的梭形刀光。偏移量还乘了剩余寿命比，
        /// 让刀光随收招一起变薄。
        /// </summary>
        private List<Vector2> GetVPoints(Vector2 start, Vector2 end, int offset)
        {
            List<Vector2> rt = new List<Vector2>();
            for (float i = 0; i <= 1; i += 0.0025f)
            {
                rt.Add(Vector2.Lerp(start, end, i) + new Vector2(0, offset * (Projectile.timeLeft / 36f) * ((float)Math.Cos(i * MathHelper.TwoPi - MathHelper.Pi) + 1) * 0.5f).RotatedBy((end - start).ToRotation()));
            }
            return rt;
        }
        /// <summary>
        /// 把上下两条包络线喂成一条三角带交给显卡：顶点纹理坐标纵轴 1/0 分别对应上下两条线，
        /// 颜色则沿整条刀光从 <paramref name="topColor"/> 渐到 <paramref name="color"/>。
        /// 顶点直接送 <c>DrawUserPrimitives</c>，采样的是当前批次绑定的贴图（即 <c>Textures[0]</c>）。
        /// </summary>
        private static void DrawSlashPart(List<Vector2> l, List<Vector2> r, Texture2D tex, Color color, Color topColor)
        {
            List<ColoredVertex> vertex = new List<ColoredVertex>();
            for (int i = 0; i < l.Count; i++)
            {
                Color c = Color.Lerp(topColor, color, ((float)Math.Cos(i * MathHelper.TwoPi - MathHelper.Pi) + 1) * 0.5f);
                vertex.Add(new ColoredVertex(l[i] - Main.screenPosition, new Vector3(i / (l.Count - 1f), 1, 1), c));
                vertex.Add(new ColoredVertex(r[i] - Main.screenPosition, new Vector3(i / (l.Count - 1f), 0, 1), c));
            }
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.Textures[0] = tex;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertex.ToArray(), 0, vertex.Count - 2);
        }
        /// <summary>
        /// 自绘三笔刀光：① 宽 100 的底纹（紫色或亮紫，由 ai[0] 决定）；② 宽 26 的纯色锋刃（紫红→亮白渐变）；
        /// ③ 宽 26 的纯黑内芯。三笔叠在 NonPremultiplied + LinearClamp 批次下，画完恢复默认批次。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (points.Count < 1)
            {
                return false;
            }
            Vector2 start = points[0];
            Vector2 end = points[points.Count - 1];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Color bodyColor = Projectile.ai[0] == 0 ? new Color(180, 0, 255, 160) : new Color(240, 200, 255, 180);
            DrawSlashPart(GetVPoints(start, end, 100), GetVPoints(start, end, -100), ModContent.Request<Texture2D>(SlashTexture).Value, bodyColor, bodyColor);
            DrawSlashPart(GetVPoints(start, end, 26), GetVPoints(start, end, -26), ModContent.Request<Texture2D>(PixelTexture).Value, new Color(180, 55, 235), new Color(255, 200, 255, 0));
            DrawSlashPart(GetVPoints(Vector2.Lerp(start, end, 0.2f), Vector2.Lerp(end, start, 0.2f), 26), GetVPoints(Vector2.Lerp(start, end, 0.1f), Vector2.Lerp(end, start, 0.1f), -26), ModContent.Request<Texture2D>(PixelTexture).Value, Color.Black, Color.Black);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.randomVec 的等价实现：在 [-max, max] 的方框内均匀取一点</summary>
        private static Vector2 RandomVec(float max) => new Vector2(Main.rand.NextFloat(-max, max), Main.rand.NextFloat(-max, max));
    }
}
