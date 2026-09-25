using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 迷你神圣守卫神圣射线（移植自灾厄 2.2.2 的 MiniGuardianHolyRay）：由治愈者守卫发射的贯穿型长射线。
    /// 射线固定在主人身上（每帧把位置重置到主人中心），150 帧内以 sin 曲线先胀后缩，期间穿过地形取最大长度 2400；
    /// 绘制由"头部光球 + 循环中段拼接 + 末端封口"三段贴图组成，白天/夜晚使用两套贴图与配色。
    /// </summary>
    internal class MiniGuardianHolyRay:ModProjectile
    {
        // ── 状态与属性 ──
        /// <summary>白天用主贴图；夜晚的 MiniGuardianHolyRayNight 在 PreDraw 里按昼夜另行取用</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Summon/MiniGuardianHolyRay";
        /// <summary>弹幕主人：射线起点每帧钉在主人中心，主人死亡即消散</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>
        /// 射线配色：原为 ProvUtils.GetColorBasedOnEnrage(!Main.dayTime, 0)。白天橙金 (255,155,25)、夜晚青蓝 (100,200,250)；
        /// alpha 保持 0（Terraria 预乘 alpha 混合下属纯叠加发光，不会压暗背景）。
        /// </summary>
        internal static Color RayColor(bool night) => night ? new Color(100, 200, 250, 0) : new Color(255, 155, 25, 0);
        // ── 生命周期方法 ──
        /// <summary>射线极长，放宽屏幕外绘制剔除距离，避免末端被裁掉</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }
        /// <summary>基础属性：48x48 碰撞箱、友方、全透明（自绘）、无限穿透、不撞地形、存活 600 帧、每个敌人 3 帧独立命中冷却</summary>
        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;
        }
        // ── 网络同步 ──
        /// <summary>同步 localAI[0]（已存活帧数）与 localAI[1]（当前射线长度）</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }
        /// <summary>接收 localAI[0] / localAI[1] 的同步值</summary>
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
        // ── 覆写方法 ──
        /// <summary>
        /// AI：速度异常时兜底为向上；每帧把射线起点钉在主人身上并实时刷新伤害（主人死亡即消散）；
        /// localAI[0] 计时 150 帧后自毁，scale 按 sin 曲线从 0 胀到上限 0.66 再收回；
        /// 之后沿速度方向做激光扫描（本弹幕强制取满 2400 长度以穿墙）、沿射线喷粉尘并照亮途经图格。
        /// </summary>
        public override void AI()
        {
            Vector2? vector78 = null;
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;
            Player owner = Main.player[Projectile.owner];
            // 射线跟随主人：位置每帧重置到主人中心，伤害按主人召唤加成实时刷新；主人死亡/离场则自毁
            if (owner.active && !owner.dead)
            {
                Vector2 fireFrom = new Vector2(owner.Center.X, owner.Center.Y);
                Projectile.position = fireFrom - new Vector2(Projectile.width, Projectile.height) / 2f;
                Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            }
            else
                Projectile.Kill();
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;
            float num801 = 0.66f;
            Projectile.localAI[0] += 1f;
            // 存在 150 帧：头 75 帧胀大、后 75 帧收回，中段封顶在 0.66
            if (Projectile.localAI[0] >= 150f)
            {
                Projectile.Kill();
                return;
            }
            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * MathHelper.Pi / 150f) * 10f * num801;
            if (Projectile.scale > num801)
                Projectile.scale = num801;
            // 旋转由速度方向加上 ai[0] 的偏置角决定，再把速度重设为该方向（保持单位长度，只当方向用）
            float num804 = Projectile.velocity.ToRotation();
            num804 += Projectile.ai[0];
            Projectile.rotation = num804 - MathHelper.PiOver2;
            Projectile.velocity = num804.ToRotationVector2();
            float num805 = 3f;                  // 沿射线取 3 个采样点做激光扫描
            float num806 = Projectile.width;
            Vector2 samplingPoint = Projectile.Center;
            // vector78 恒为 null（CE 原样的死分支），采样点实际永远是自身中心
            if (vector78.HasValue)
                samplingPoint = vector78.Value;
            float[] array3 = new float[(int)num805];
            Collision.LaserScan(samplingPoint, Projectile.velocity, num806 * Projectile.scale, 2400f, array3);
            float num807 = 0f;
            for (int num808 = 0; num808 < array3.Length; num808++)
            {
                num807 += array3[num808];
            }
            num807 /= num805;
            // 扫描结果被丢弃：本弹幕固定取满 2400 长度，表现为穿墙的长射线
            num807 = 2400f;
            int dustType = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            // localAI[1] 平滑逼近 num807，得到当前射线长度（也是绘制与碰撞共用的长度）
            float amount = 0.5f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num807, amount);
            Vector2 vector79 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);
            // 射线末端每帧喷 2 颗垂直向外飞散的粉尘
            for (int num809 = 0; num809 < 2; num809++)
            {
                float num810 = Projectile.velocity.ToRotation() + (Main.rand.NextBool() ? -1f : 1f) * MathHelper.PiOver2;
                float num811 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector80 = new Vector2((float)Math.Cos(num810) * num811, (float)Math.Sin(num810) * num811);
                int num812 = Dust.NewDust(vector79, 0, 0, dustType, vector80.X, vector80.Y, 0, default, 1f);
                Main.dust[num812].noGravity = true;
                Main.dust[num812].scale = 1.7f;
            }
            // 1/5 概率在末端补一颗横向抖动、向上飘的粉尘
            if (Main.rand.NextBool(5))
            {
                Vector2 value29 = Projectile.velocity.RotatedBy(MathHelper.PiOver2, default) * ((float)Main.rand.NextDouble() - 0.5f) * Projectile.width;
                int num813 = Dust.NewDust(vector79 + value29 - Vector2.One * 4f, 8, 8, dustType, 0f, 0f, 100, default, 1.5f);
                Dust dust = Main.dust[num813];
                dust.velocity *= 0.5f;
                Main.dust[num813].velocity.Y = -Math.Abs(Main.dust[num813].velocity.Y);
            }
            // 沿射线把途经图格点亮（0.3/0.65/0.7 的冷色光）
            DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], Projectile.width * Projectile.scale, DelegateMethods.CastLight);
        }
        /// <summary>
        /// 绘制：白天用暖色三件套贴图（头 MiniGuardianHolyRay / 中段 MiniGuardianHolyRayMid / 末段 MiniGuardianHolyRayEnd），
        /// 夜晚换用带 Night 后缀的青蓝色版本；中段贴图按 36 像素高的 4 帧循环取样，沿射线方向无缝拼接。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;
            bool dayTime = Main.dayTime;
            Texture2D texture2D19 = dayTime ? ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value :
                ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/MiniGuardianHolyRayNight", AssetRequestMode.ImmediateLoad).Value;
            Texture2D texture2D20 = dayTime ? ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/MiniGuardianHolyRayMid", AssetRequestMode.ImmediateLoad).Value :
                ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/MiniGuardianHolyRayMidNight", AssetRequestMode.ImmediateLoad).Value;
            Texture2D texture2D21 = dayTime ? ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/MiniGuardianHolyRayEnd", AssetRequestMode.ImmediateLoad).Value :
                ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/MiniGuardianHolyRayEndNight", AssetRequestMode.ImmediateLoad).Value;
            float num223 = Projectile.localAI[1]; // 当前射线长度
            // 原为 ProvUtils.GetColorBasedOnEnrage(!Main.dayTime, 0)：日夜两套神圣配色，alpha 0 表示纯叠加发光
            Color color44 = RayColor(!Main.dayTime) * 0.9f;
            Vector2 vector = Projectile.Center - Main.screenPosition;
            Rectangle? sourceRectangle2 = null;
            // 先画头部光球（以自身中心为原点）
            Main.spriteBatch.Draw(texture2D19, vector, sourceRectangle2, color44, Projectile.rotation, texture2D19.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            num223 -= (texture2D19.Height / 2 + texture2D21.Height) * Projectile.scale;
            Vector2 value20 = Projectile.Center;
            value20 += Projectile.velocity * Projectile.scale * texture2D19.Height / 2f;
            // 中段：从头部之后开始按 36 像素一段循环取样拼接，直到剩余长度不足一段
            if (num223 > 0f)
            {
                float num224 = 0f;
                Rectangle rectangle7 = new Rectangle(0, 36 * (Projectile.timeLeft / 3 % 4), texture2D20.Width, 36);
                while (num224 + 1f < num223)
                {
                    if (num223 - num224 < rectangle7.Height)
                        rectangle7.Height = (int)(num223 - num224);
                    Main.spriteBatch.Draw(texture2D20, value20 - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle?(rectangle7), color44, Projectile.rotation, new Vector2(rectangle7.Width / 2, 0f), Projectile.scale, SpriteEffects.None, 0);
                    num224 += rectangle7.Height * Projectile.scale;
                    value20 += Projectile.velocity * rectangle7.Height * Projectile.scale;
                    rectangle7.Y += 36;
                    if (rectangle7.Y + rectangle7.Height > texture2D20.Height)
                        rectangle7.Y = 0;
                }
            }
            // 末端：以贴图顶端为原点封口
            Vector2 vector2 = value20 - Main.screenPosition;
            sourceRectangle2 = null;
            Main.spriteBatch.Draw(texture2D21, vector2, sourceRectangle2, color44, Projectile.rotation, texture2D21.Frame(1, 1, 0, 0).Top(), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>砍草：沿射线长度切割图格（攻击型弹幕）</summary>
        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 unit = Projectile.velocity;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * Projectile.localAI[1], Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
        }
        /// <summary>碰撞：命中箱相交即算命中，另外沿射线做 22×scale 宽的线段碰撞检测</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;
            float num6 = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], 22f * Projectile.scale, ref num6))
                return true;
            return false;
        }
        /// <summary>只有胀到一半以上（scale ≥ 0.5）时才具备伤害，避免刚生成的瞬间就跳伤害</summary>
        public override bool? CanDamage() => Projectile.scale >= 0.5f;
    }
}
