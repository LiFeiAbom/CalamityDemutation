using System;
using CalamityDemutation.Utilities;
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
    /// 迷你神圣守卫的圣光长矛（移植自灾厄 2.2.2 的 MiniGuardianSpear，贴图原为 HolySpear）：
    /// 由 MiniGuardianAttack 的长矛阶段发射，命中一次即消散，对傀儡类敌人有抗性减免并附带 0.5 倍鞭标记增伤。
    /// 由守卫发射时（ai[0] > 0）保持神圣色不随夜晚变色，且 ai[1] > 0 表示不需要那套"停帧→加速→索敌"的自导流程；
    /// 否则（神器本体发射）前 50 帧先悬停蓄势、再加速并自行索敌追击。
    /// </summary>
    internal class MiniGuardianSpear:ModProjectile
    {
        // ── 生命周期方法 ──
        /// <summary>注册 2 帧残影缓存、鞭标记 0.5 倍增伤，并声明对邪教徒类敌人有抗性减免</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 2;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.5f;
        }
        /// <summary>基础属性：30x30 碰撞箱、友方、穿透 1 次、半透明、存活 300 帧、额外更新 1 次、每帧 2 次更新</summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.alpha = 100;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 1;
            Projectile.minion = true;
            Projectile.scale = 0.9f;
            Projectile.DamageType = DamageClass.Summon;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 神圣配色（移植自灾厄 Providence.GetColorBasedOnEnrage 的白昼/夜晚两值版本）：
        /// 白昼橙红 (255,155,25)、夜晚青蓝 (100,200,250)；outline 为 true 时取描边色（白昼 (255,0,0)、夜晚 (100,250,200)）
        /// 并整体乘 0.1 压暗。
        /// </summary>
        internal static Color ProfanedColor(bool night, int alpha, bool outline = false)
        {
            Color finalColor = new Color(255, outline ? 0 : 155, outline ? 0 : 25, alpha); // 默认取白昼档
            if (night)
                finalColor = new Color(100, outline ? 250 : 200, outline ? 200 : 250, alpha);
            if (outline)
                finalColor *= 0.1f;
            return finalColor;
        }
        /// <summary>
        /// 十向偏移背光：以弹幕中心为基准绕一圈 10 个方向各叠画一次贴图形成发光描边
        /// （移植自灾厄 Projectile.DrawBackglow 的简化版：本弹幕是单帧贴图，直接取整张贴图绘制；
        /// 工程里多帧弹幕用的是 Utilities/DrawUtil 的同名扩展方法，那份会按 Main.projFrames 取当前帧）。
        /// </summary>
        private void DrawBackglow(Color backglowColor, float backglowArea, Texture2D texture)
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color backAfterimageColor = backglowColor * Projectile.Opacity;
            SpriteEffects spriteEffects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * backglowArea;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);
            }
        }
        /// <summary>
        /// 神器本体发射（miniGuardianPscAttack == false）时专用的自导流程：
        /// 第 300 帧先刹停（速度乘 0.01）、250~300 帧蓄势加速（每帧 1.1 倍）、第 250 帧再一次性提速 5 倍；
        /// 此后（timeLeft &lt;= 250）优先追主人右键锁定的目标，否则线性扫描全场最近的合法敌人，
        /// 以 24（近距离 28）为期望速度按 14/15 惯性插值修正速度，仅当 ai[1] == 0 时才允许转向（方向已被锁定）。
        /// </summary>
        private void handleAI(bool miniGuardianPscAttack)
        {
            if (!miniGuardianPscAttack)
            {
                if (Projectile.timeLeft == 300)
                {
                    Projectile.velocity *= 0.01f;
                }
                else if (Projectile.timeLeft > 250)
                {
                    Projectile.velocity *= 1.1f;
                }
                else if (Projectile.timeLeft == 250)
                {
                    Projectile.velocity *= 5f;
                }
                if (Projectile.timeLeft <= 250)
                {
                    float num535 = Projectile.position.X;
                    float num536 = Projectile.position.Y;
                    float num537 = 3000f;
                    bool flag19 = false;
                    NPC ownerMinionAttackTargetNPC2 = Projectile.OwnerMinionAttackTargetNPC;
                    if (ownerMinionAttackTargetNPC2 != null && ownerMinionAttackTargetNPC2.CanBeChasedBy(Projectile, false))
                    {
                        float num539 = ownerMinionAttackTargetNPC2.position.X + (float)(ownerMinionAttackTargetNPC2.width / 2);
                        float num540 = ownerMinionAttackTargetNPC2.position.Y + (float)(ownerMinionAttackTargetNPC2.height / 2);
                        float num541 = Math.Abs(Projectile.position.X + (float)(Projectile.width / 2) - num539) + Math.Abs(Projectile.position.Y + (float)(Projectile.height / 2) - num540);
                        if (num541 < num537)
                        {
                            num537 = num541;
                            num535 = num539;
                            num536 = num540;
                            flag19 = true;
                        }
                    }
                    if (!flag19)
                    {
                        for (int num542 = 0; num542 < Main.npc.Length; num542++)
                        {
                            if (Main.npc[num542].CanBeChasedBy(Projectile, false))
                            {
                                float num543 = Main.npc[num542].position.X + (float)(Main.npc[num542].width / 2);
                                float num544 = Main.npc[num542].position.Y + (float)(Main.npc[num542].height / 2);
                                float num545 = Math.Abs(Projectile.position.X + (float)(Projectile.width / 2) - num543) + Math.Abs(Projectile.position.Y + (float)(Projectile.height / 2) - num544);
                                if (num545 < num537)
                                {
                                    num537 = num545;
                                    num535 = num543;
                                    num536 = num544;
                                    flag19 = true;
                                }
                            }
                        }
                    }
                    if (flag19)
                    {
                        if (Projectile.ai[1] == 0f)
                        {
                            float num550 = 24f; // 期望速度 24（CE 原注释写的 12 是旧值，实际取 24）
                            Vector2 vector43 = Projectile.Center;
                            float num551 = num535 - vector43.X;
                            float num552 = num536 - vector43.Y;
                            float num553 = (float)Math.Sqrt((double)(num551 * num551 + num552 * num552));
                            if (num553 < 100f)
                            {
                                num550 = 28f; // 距离 100 以内时提速到 28（CE 原注释写的 14 是旧值）
                            }
                            num553 = num550 / num553;
                            num551 *= num553;
                            num552 *= num553;
                            Projectile.velocity.X = (Projectile.velocity.X * 14f + num551) / 15f;
                            Projectile.velocity.Y = (Projectile.velocity.Y * 14f + num552) / 15f;
                        }
                    }
                }
            }
        }
        // ── 覆写方法 ──
        /// <summary>
        /// AI：每帧按召唤伤害重算 damage 并原地留一颗静止的神圣色粉尘（形成拖尾弧光）；
        /// 由守卫发射时（ai[0] > 0）保持神圣色，不随夜晚改为青蓝；最后按速度方向校正贴图旋转（+90 度）。
        /// </summary>
        public override void AI()
        {
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            // 由守卫发射的矛（ai[0] > 0）不随夜晚变色，只有神器本体发射的那枚才按昼夜换配色
            var psc = Projectile.ai[0] > 0f;
            int num469 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, MiniGuardianHealer.HolyDustType(!Main.dayTime && psc), 0f, 0f, 100, default, !Main.dayTime && psc ? 0.5f : 1f);
            Main.dust[num469].noGravity = true;
            Main.dust[num469].velocity *= 0f;
            handleAI(psc && Projectile.ai[1] > 0f);
            Projectile.rotation = Projectile.velocity.ToRotation() + 1.57079637f;
        }
        /// <summary>蓄势期（timeLeft &gt; 250 且 ai[1] == 0）不可命中，之后才开启判定</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[1] == 0f && Projectile.timeLeft > 250)
                return false;
            return null;
        }
        /// <summary>
        /// 绘制：先按昼夜/来源取神圣色画十向背光，再用 CDUtil.DrawAfterimages 画 2 帧残影与本体
        /// （原为 CalamityUtils.DrawAfterimagesCentered + Projectile.DrawBackglow）。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            var psc = Projectile.ai[0] > 0f;
            DrawBackglow(ProfanedColor(!Main.dayTime && psc, Projectile.alpha, true), 4f, TextureAssets.Projectile[Type].Value);
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Type], ProfanedColor(!Main.dayTime && psc, Projectile.alpha), 1);
            return false;
        }
        /// <summary>
        /// 消亡：播放原版 SoundID.Item14（爆炸）；1/3 概率把判定框临时放大到 200 见方，
        /// 再按昼夜/来源喷两轮神圣色粉尘（夜晚少量小尺寸、白昼多量带 fadeIn）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            if (Main.rand.NextBool(3))
            {
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = Projectile.height = 200;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                var psc = Projectile.ai[0] > 0f;
                bool shouldAdjust = !Main.dayTime && psc;
                int dustID = MiniGuardianHealer.HolyDustType(shouldAdjust);
                for (int num621 = 0; num621 < 4; num621++)
                {
                    int num622 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 100, default, !Main.dayTime && psc ? 0.5f : 2f);
                    Main.dust[num622].velocity *= 3f;
                    if (Main.rand.NextBool())
                    {
                        Main.dust[num622].scale = 0.5f;
                        Main.dust[num622].fadeIn = shouldAdjust ? 0.9f : 1f + (float)Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int num623 = 0; num623 < (shouldAdjust ? 8 : 12); num623++)
                {
                    int num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 100, default, !Main.dayTime && psc ? 1.25f : 3f);
                    Main.dust[num624].noGravity = true;
                    Main.dust[num624].velocity *= 5f;
                    Main.dust[num624].fadeIn = shouldAdjust ? 0.9f : Main.dust[num624].fadeIn;
                    num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 100, default, !Main.dayTime && psc ? 1f : 2f);
                    Main.dust[num624].velocity *= 2f;
                    Main.dust[num624].fadeIn = shouldAdjust ? 0.9f : Main.dust[num624].fadeIn;
                }
            }
        }
    }
}
