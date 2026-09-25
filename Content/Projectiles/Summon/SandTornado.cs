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
    /// 沙尘龙卷（SandTornado） - 由沙之印记释放的贯穿地形的竖直龙卷，前 30 帧造成伤害。
    /// <para>
    /// 时间轴由两个计数器分担：<c>ai[0]</c> 是存活计时（上限 900 帧，AI 内自管，与 <c>timeLeft</c> 的 1200 帧无关），
    /// <c>localAI[0]</c> 只是"前 30 帧有伤害"的威力计时（满 30 帧后 <c>damage</c> 置 0）。
    /// 一旦伤害归零或龙卷看不到主人，<c>ai[0]</c> 会被"对齐"到 780 起的收尾段，让它自然卷完再消失。
    /// </para>
    /// </summary>
    internal class SandTornado:ModProjectile
    {
        // ── 覆写属性 ──
        /// <summary>复用工程内的通用龙卷贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/TornadoProj";
        // ── 生命周期方法 ──
        /// <summary>
        /// 基础属性：10x10 的初始碰撞箱（每帧由 AI 按地形重算成贯穿地形的长条）；召唤物、不撞地形、
        /// 击杀月总后无限穿透否则可穿透 3 个敌人、兜底存活 1200 帧；命中冷却随 Boss 进度递减。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.penetrate = NPC.downedMoonlord ? -1 : 3;
            Projectile.timeLeft = 1200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20 -
                (NPC.downedGolemBoss ? 5 : 0) -
                (NPC.downedMoonlord ? 5 : 0) -
                (BossSystem.DevourerOfGods ? 4 : 0) -
                (BossSystem.Yharon ? 3 : 0);
        }
        /// <summary>
        /// 龙卷 AI：播放音效、推进两个计时器、按地形把碰撞箱重算成竖直长条，
        /// 并在伤害归零或（主人端判定）完全看不到主人时把存活计时对接到收尾段。
        /// </summary>
        public override void AI()
        {
            float num1125 = 900f;   // 本弹幕自管的存活上限（实际由下面的对齐逻辑提前拉到收尾段）
            // 生成瞬间播放一次龙卷风声（soundDelay 置 -1 防止重复）
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = -1;
                SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
            }
            // ai[0]：存活计时；localAI[0]：威力计时（满 30 帧后伤害归零，只保留视觉/推挤）
            Projectile.ai[0] += 1f;
            Projectile.localAI[0] += 1f;
            if (Projectile.ai[0] >= num1125)
            {
                Projectile.Kill();
            }
            // 前 30 帧造成伤害，之后伤害归零；若剩余时间不足 120 帧则不再延续，把计时对齐到收尾段
            if (Projectile.localAI[0] >= 30f)
            {
                Projectile.damage = 0;
                if (Projectile.ai[0] < num1125 - 120f)
                {
                    float num1126 = Projectile.ai[0] % 60f;
                    Projectile.ai[0] = num1125 - 120f + num1126;
                    Projectile.netUpdate = true;
                }
            }
            // 以弹幕所在列为基准，向上/下各延伸 15 格找到空腔范围，据此把龙卷拉成贯穿地形的竖直长条
            float num1127 = 15f;
            float num1128 = 15f;
            Point point8 = Projectile.Center.ToTileCoordinates();
            int num1129;
            int num1130;
            Collision.ExpandVertically(point8.X, point8.Y, out num1129, out num1130, (int)num1127, (int)num1128);
            num1129++;
            num1130--;
            Vector2 value72 = new Vector2((float)point8.X, (float)num1129) * 16f + new Vector2(8f);
            Vector2 value73 = new Vector2((float)point8.X, (float)num1130) * 16f + new Vector2(8f);
            Vector2 vector146 = Vector2.Lerp(value72, value73, 0.5f);   // 长条的几何中心
            Vector2 value74 = new Vector2(0f, value73.Y - value72.Y);
            value74.X = value74.Y * 0.2f;   // 龙卷宽:高 约为 1:5
            Projectile.width = (int)(value74.X * 0.65f);   // 判定宽度只取几何宽度的 65%，避免边缘误判
            Projectile.height = (int)value74.Y;
            Projectile.Center = vector146;
            // 仅主人端校验：龙卷覆盖范围与主人之间无视线遮挡时正常持续；若完全看不到玩家则提前进入收尾
            if (Projectile.owner == Main.myPlayer)
            {
                bool flag74 = false;
                Vector2 center16 = Main.player[Projectile.owner].Center;
                Vector2 top = Main.player[Projectile.owner].Top;
                for (float num1131 = 0f; num1131 < 1f; num1131 += 0.05f)
                {
                    Vector2 position2 = Vector2.Lerp(value72, value73, num1131);
                    if (Collision.CanHitLine(position2, 0, 0, center16, 0, 0) || Collision.CanHitLine(position2, 0, 0, top, 0, 0))
                    {
                        flag74 = true;
                        break;
                    }
                }
                if (!flag74 && Projectile.ai[0] < num1125 - 120f)
                {
                    float num1132 = Projectile.ai[0] % 60f;
                    Projectile.ai[0] = num1125 - 120f + num1132;
                    Projectile.netUpdate = true;
                }
            }
            if (Projectile.ai[0] < num1125 - 120f)
            {
                return;   // 未进入收尾段就提前返回（此处已是方法末尾，等价于空操作）
            }
        }
        // ── 覆写方法 ──
        /// <summary>
        /// 自定义绘制：沿龙卷高度每 5.1 像素画一段旋转的沙环，凑成螺旋外观；本体贴图不可见，全在此处手绘。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            float num226 = 600f;   // 淡出曲线的时间基准（与 AI 里的实际存活上限 num1125 = 900 不一致）
            float num227 = 15f;
            float num228 = 15f;
            float num229 = Projectile.ai[0];
            float scale5 = MathHelper.Clamp(num229 / 30f, 0f, 1f);   // 出场 30 帧内从 0 淡到 1
            if (num229 > num226 - 60f)
            {
                scale5 = MathHelper.Lerp(1f, 0f, (num229 - (num226 - 60f)) / 60f);   // 最后 60 帧淡出
            }
            Microsoft.Xna.Framework.Point point5 = Projectile.Center.ToTileCoordinates();
            int num230;
            int num231;
            Collision.ExpandVertically(point5.X, point5.Y, out num230, out num231, (int)num227, (int)num228);
            num230++;
            num231--;
            float num232 = 0.2f;   // 与 AI 一致的宽:高比
            Vector2 value32 = new Vector2((float)point5.X, (float)num230) * 16f + new Vector2(8f);   // 长条顶端中心
            Vector2 value33 = new Vector2((float)point5.X, (float)num231) * 16f + new Vector2(8f);   // 长条底端中心
            Vector2.Lerp(value32, value33, 0.5f);
            Vector2 vector33 = new Vector2(0f, value33.Y - value32.Y);
            vector33.X = vector33.Y * num232;
            new Vector2(value32.X - vector33.X / 2f, value32.Y);
            Texture2D texture2D23 = TextureAssets.Projectile[Projectile.type].Value;
            Microsoft.Xna.Framework.Rectangle rectangle9 = texture2D23.Frame(1, 1, 0, 0);
            Vector2 origin3 = rectangle9.Size() / 2f;
            float num233 = -0.06283186f * num229;   // 整体自转角速度（每 100 帧一圈，方向与螺旋相反）
            Vector2 spinningpoint2 = Vector2.UnitY.RotatedBy((double)(num229 * 0.1f), default(Vector2));   // 螺旋基准方向，随时间缓慢旋转
            float num234 = 0f;   // 已绘制的弧长
            float num235 = 5.1f;   // 每段沙环的高度间距
            Microsoft.Xna.Framework.Color value34 = new Microsoft.Xna.Framework.Color(225, 225, 100);   // 沙黄本体色
            for (float num236 = (float)((int)value33.Y); num236 > (float)((int)value32.Y); num236 -= num235)
            {
                // 自下而上逐段绘制：num237 是当前段在整条龙卷里的高度比例（0 底 → 1 顶）
                num234 += num235;
                float num237 = num234 / vector33.Y;
                float num238 = num234 * 6.28318548f / -20f;   // 螺旋相位：每 20 像素错开一整圈
                float num239 = num237 - 0.15f;
                Vector2 vector34 = spinningpoint2.RotatedBy((double)num238, default(Vector2));
                Vector2 value35 = new Vector2(0f, num237 + 1f);
                value35.X = value35.Y * num232;
                // 颜色：中段（num237 接近 0.5）最亮，向两端线性隐没
                Microsoft.Xna.Framework.Color color39 = Microsoft.Xna.Framework.Color.Lerp(Microsoft.Xna.Framework.Color.Transparent, value34, num237 * 2f);
                if (num237 > 0.5f)
                {
                    color39 = Microsoft.Xna.Framework.Color.Lerp(Microsoft.Xna.Framework.Color.Transparent, value34, 2f - num237 * 2f);
                }
                color39.A = (byte)((float)color39.A * 0.5f);
                color39 *= scale5;   // 再整体乘出场/收尾的淡入淡出系数
                vector34 *= value35 * 100f;
                vector34.Y = 0f;
                vector34.X = 0f;
                vector34 += new Vector2(value33.X, num236) - Main.screenPosition;   // 定点到该段所在的屏幕坐标
                Main.spriteBatch.Draw(texture2D23, vector34, new Microsoft.Xna.Framework.Rectangle?(rectangle9), color39, num233 + num238, origin3, 1f + num239, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
