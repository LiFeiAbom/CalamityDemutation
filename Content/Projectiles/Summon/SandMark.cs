using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 沙之印记（SandMark） - 由沙之矢（SandBolt）落地生成的原地印记，持续旋转喷沙，
    /// 第 60 帧（约 1 秒）在自身位置释放一道沙尘龙卷（<see cref="SandTornado"/>），第 120 帧自毁。
    /// <para>
    /// 时间轴全部由一个 <c>localAI[1]</c> 计数器驱动（<c>timeLeft</c> 的 900 帧只是兜底）：
    /// 0~30 帧抛洒沙尘、33 帧内淡入、60 帧放龙卷、103 帧后淡出、120 帧 <c>Kill</c>。
    /// </para>
    /// </summary>
    internal class SandMark:ModProjectile
    {
        // ── 覆写属性 ──
        /// <summary>用不可见贴图占位（本体完全由 <see cref="PreDraw"/> 自绘）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        // ── 生命周期方法 ──
        /// <summary>
        /// 基础属性：14x14 碰撞箱、不可见（alpha 255）、不撞地形、无限穿透、存活 900 帧（实际 120 帧就自毁）；
        /// 命中冷却以 30 帧打底，随 Boss 进度递减（歌利亚 -5、月总 -5、噬神者 -4、犽戎 -3）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 900;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30 -
                (NPC.downedGolemBoss ? 5 : 0) -
                (NPC.downedMoonlord ? 5 : 0) -
                (BossSystem.DevourerOfGods ? 4 : 0) -
                (BossSystem.Yharon ? 3 : 0);
        }
        /// <summary>
        /// 印记 AI：播放落地音效、前 30 帧抛洒沙尘、旋转本体、淡入淡出，
        /// 第 60 帧生成沙尘龙卷，第 120 帧自毁。
        /// </summary>
        public override void AI()
        {
            Color newColor3 = new Color(255, 255, 255);   // 亮白色，只用于给印记的点光源上色
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = -1;   // 置 -1 防重复播放
                SoundEngine.PlaySound(SoundID.Item60, Projectile.Center);
            }
            if (Projectile.localAI[1] < 30f)
            {
                // 前 30 帧：每帧抛一颗沙尘，落点沿印记外缘随机分布（有横向漂移，形成"沙在流"的感觉）
                for (int num1134 = 0; num1134 < 1; num1134++)
                {
                    float value79 = -0.5f;
                    float value80 = 0.9f;
                    float amount4 = Main.rand.NextFloat();
                    Vector2 value81 = new Vector2(MathHelper.Lerp(0.1f, 1f, Main.rand.NextFloat()), MathHelper.Lerp(value79, value80, amount4));
                    value81.X *= MathHelper.Lerp(2.2f, 0.6f, amount4);
                    value81.X *= -1f;
                    Vector2 value82 = new Vector2(2f, 10f);
                    Vector2 position4 = Projectile.Center + new Vector2(60f, 200f) * value81 * 0.5f + value82;
                    Dust dust34 = Main.dust[Dust.NewDust(position4, 0, 0, DustID.Sandnado, 0f, 0f, 0, default(Color), 0.5f)];
                    dust34.position = position4;
                    dust34.customData = Projectile.Center + value82;
                    dust34.fadeIn = 1f;
                    dust34.scale = 0.3f;
                    if (value81.X > -1.2f)
                    {
                        dust34.velocity.X = 1f + Main.rand.NextFloat();
                    }
                    dust34.velocity.Y = Main.rand.NextFloat() * -0.5f - 1f;
                }
            }
            if (Projectile.localAI[0] == 0f)
            {
                // 首帧初始化：localAI[0] 既是"已初始化"标记，也兼作后续粉尘/缩放的强度系数
                Projectile.localAI[0] = 0.8f;
                Projectile.direction = 1;
                Point point9 = Projectile.Center.ToTileCoordinates();
                Projectile.Center = new Vector2((float)(point9.X * 16 + 8), (float)(point9.Y * 16 + 8));   // 吸附到格子中心
            }
            Projectile.rotation = Projectile.localAI[1] / 40f * 6.28318548f * (float)Projectile.direction;   // 每 40 帧转满一圈
            if (Projectile.localAI[1] < 33f)
            {
                // 出场淡入：每帧 -8 alpha
                if (Projectile.alpha > 0)
                {
                    Projectile.alpha -= 8;
                }
                if (Projectile.alpha < 0)
                {
                    Projectile.alpha = 0;
                }
            }
            if (Projectile.localAI[1] > 103f)
            {
                // 收尾淡出：每帧 +16 alpha，到 120 帧正好接近全透明后自毁
                if (Projectile.alpha < 255)
                {
                    Projectile.alpha += 16;
                }
                if (Projectile.alpha > 255)
                {
                    Projectile.alpha = 255;
                }
            }
            if (Projectile.alpha == 0)
            {
                Lighting.AddLight(Projectile.Center, newColor3.ToVector3() * 0.5f);   // 完全显形时才发光
            }
            // 两圈沙尘的抛射方向：第一圈的方向随印记自转一起旋转，第二圈固定在竖直十字方向
            for (int num1135 = 0; num1135 < 2; num1135++)
            {
                if (Main.rand.Next(10) == 0)
                {
                    Vector2 value83 = Vector2.UnitY.RotatedBy((double)((float)num1135 * 3.14159274f), default(Vector2)).RotatedBy((double)Projectile.rotation, default(Vector2));
                    Dust dust35 = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.Sandnado, 0f, 0f, 225, newColor3, 1f)];
                    dust35.noGravity = true;
                    dust35.noLight = true;
                    dust35.scale = Projectile.Opacity * Projectile.localAI[0];
                    dust35.position = Projectile.Center;
                    dust35.velocity = value83 * 2.5f;
                }
            }
            for (int num1136 = 0; num1136 < 2; num1136++)
            {
                if (Main.rand.Next(10) == 0)
                {
                    Vector2 value84 = Vector2.UnitY.RotatedBy((double)((float)num1136 * 3.14159274f), default(Vector2));
                    Dust dust36 = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.Sandnado, 0f, 0f, 225, newColor3, 1.5f)];
                    dust36.noGravity = true;
                    dust36.noLight = true;
                    dust36.scale = Projectile.Opacity * Projectile.localAI[0];
                    dust36.position = Projectile.Center;
                    dust36.velocity = value84 * 2.5f;
                }
            }
            if (Projectile.localAI[1] < 33f || Projectile.localAI[1] > 87f)
            {
                Projectile.scale = Projectile.Opacity / 2f * Projectile.localAI[0];   // 淡入/淡出期间同步缩放，其余时间保持上一帧的值
            }
            Projectile.velocity = Vector2.Zero;   // 印记钉死在原地
            Projectile.localAI[1] += 1f;
            // 存在 1 秒时释放沙尘龙卷（标记为召唤物），之后等待 2 秒自毁
            if (Projectile.localAI[1] == 60f && Projectile.owner == Main.myPlayer)
            {
                // 只有主人端生成（localAI 不同步，其余端本地也走到同一个计数，故必须靠 owner 判据去重）
                int storm = Projectile.NewProjectile(Projectile.GetSource_FromThis(null), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SandTornado>(), Projectile.damage, 2f, Projectile.owner, 1f, 0f);
                Main.projectile[storm].minion = true;
            }
            if (Projectile.localAI[1] >= 120f)
            {
                Projectile.Kill();
                return;
            }
        }
        // ── 覆写方法 ──
        /// <summary>
        /// 自定义绘制：以多圈旋转的沙环表现印记外观（本体贴图不可见，全在此处手绘）。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Microsoft.Xna.Framework.Color color25 = Lighting.GetColor((int)((double)Projectile.position.X + (double)Projectile.width * 0.5) / 16, (int)(((double)Projectile.position.Y + (double)Projectile.height * 0.5) / 16.0));
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;   // 朝左时水平镜像
            }
            Vector2 vector38 = Projectile.position + new Vector2((float)Projectile.width, (float)Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
            Texture2D texture2D27 = TextureAssets.Projectile[Projectile.type].Value;
            Microsoft.Xna.Framework.Rectangle rectangle11 = texture2D27.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Microsoft.Xna.Framework.Color alpha5 = Projectile.GetAlpha(color25);
            Vector2 origin7 = rectangle11.Size() / 2f;
            Microsoft.Xna.Framework.Color color47 = Main.hslToRgb(0.25f, 1f, 1f).MultiplyRGBA(new Microsoft.Xna.Framework.Color(255, 255, 255, 0));   // hsl 色相 0.25 = 沙黄
            Main.spriteBatch.Draw(texture2D27, vector38, new Microsoft.Xna.Framework.Rectangle?(rectangle11), color47, 0f, origin7, new Vector2(1f, 5f) * Projectile.scale * 2f, spriteEffects, 0f);   // 第一层：竖直 5 倍拉伸的黄色背光
            Main.spriteBatch.Draw(texture2D27, vector38, new Microsoft.Xna.Framework.Rectangle?(rectangle11), alpha5, Projectile.rotation, origin7, Projectile.scale, spriteEffects, 0f);   // 第二层：带自转的本体
            Main.spriteBatch.Draw(texture2D27, vector38, new Microsoft.Xna.Framework.Rectangle?(rectangle11), alpha5, 0f, origin7, new Vector2(1f, 8f) * Projectile.scale, spriteEffects, 0f);   // 第三层：竖直 8 倍拉伸的柔光外沿
            return false;
        }
    }
}
