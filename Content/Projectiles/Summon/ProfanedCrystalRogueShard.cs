using System;
using System.IO;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎之魂水晶·召唤师转化的"水晶螺旋"碎片（移植自灾厄 2.2.2 的 ProfanedCrystalRogueShard，
    /// 原版占用的是盗贼（Throwing）槽，本工程按用户口径改由召唤武器触发）。
    /// 前 120 帧原地旋转、速度每帧 ×0.985 且不可命中；之后开启追踪与命中（40/近距离 28 的期望速度按 14/15 惯性插值，
    /// 锁定目标后每帧 50% 概率续 1 帧存活）并撞地形。伤害按通用伤害折算
    /// （originalDamage 为未折算的基础值：环射 352 / 单片 440、强化档单片 250，由派发端写入）。
    /// 命中敌人时按 1/10（强化档 1/30）的概率滚一次"追加长矛"（rollBabSpears，见本类 ModifyHitNPC）。
    /// </summary>
    internal class ProfanedCrystalRogueShard:ModProjectile
    {
        /// <summary>注册对邪教徒类敌人的抗性减免与鞭标记 0.25 倍增伤</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.25f;
        }
        /// <summary>基础属性：34x34 碰撞箱、0.8 缩放、友方、全透明（淡入到 0）、穿透 1 次、不撞地形、存活 150 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 0.8f;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>复刻灾厄的 Dust.BetterCloneDust（该扩展方法已标记"不建议使用"）：新生成一颗与源粉尘各项参数一致的新粉尘</summary>
        private static Dust CloneDust(Dust source)
        {
            Dust clone = Dust.NewDustPerfect(source.position, source.type, source.velocity, source.alpha, source.color, source.scale);
            clone.fadeIn = source.fadeIn;
            clone.noGravity = source.noGravity;
            clone.rotation = source.rotation;
            clone.noLight = source.noLight;
            return clone;
        }
        /// <summary>
        /// 主体移动逻辑：每帧按通用伤害重算 damage；前 120 帧原地高速自转并减速（不可命中），
        /// 之后朝向跟随速度（逆挥方向额外补 90 度），先按主人右键锁定目标、否则全场就近索敌，
        /// 以 40（近距离 28）为期望速度按 14/15 惯性插值并额外加速 5%；无目标时改为撞地形。
        /// </summary>
        private void ai()
        {
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            if (Projectile.timeLeft > 120)
            {
                Projectile.rotation += 1f;
                Projectile.velocity.X *= 0.985f;
                Projectile.velocity.Y *= 0.985f;
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f);
                if (Projectile.spriteDirection == -1)
                    Projectile.rotation -= MathHelper.PiOver2;
                float num535 = Projectile.position.X;
                float num536 = Projectile.position.Y;
                float num537 = 2000f;
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
                    for (int num542 = 0; num542 < Main.maxNPCs; num542++)
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
                if (flag19) // 找到目标
                {
                    if (Main.rand.NextBool())
                        Projectile.timeLeft++;
                    Projectile.tileCollide = false;
                    float num550 = 40f;
                    Vector2 vector43 = Projectile.Center;
                    float num551 = num535 - vector43.X;
                    float num552 = num536 - vector43.Y;
                    float num553 = (float)Math.Sqrt((double)(num551 * num551 + num552 * num552));
                    if (num553 < 100f)
                        num550 = 28f;
                    num553 = num550 / num553;
                    num551 *= num553;
                    num552 *= num553;
                    Projectile.velocity.X = (Projectile.velocity.X * 14f + num551) / 15f;
                    Projectile.velocity.Y = (Projectile.velocity.Y * 14f + num552) / 15f;
                    Projectile.velocity *= 1.05f;
                }
                else
                {
                    Projectile.tileCollide = true;
                }
            }
        }
        /// <summary>同步 localAI[0] / localAI[1]（原版遗留字段，本弹幕内未被赋值，保留以对齐同步结构）</summary>
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
        /// <summary>
        /// AI：先跑移动逻辑，再按 ai[0] 取彩虹色系（原版用 ai[0] 当色相）做淡入（每帧 alpha -8）与照明；
        /// 期间沿自身朝向与反方向各喷一颗彩虹色粉尘，并额外在周围随机位置补一颗"本体 + 白色半尺寸复制体"。
        /// </summary>
        public override void AI()
        {
            ai();
            Color newColor2 = Main.hslToRgb(Projectile.ai[0], 1f, 0.5f);
            if (Projectile.alpha > 0)
                Projectile.alpha -= 8;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            if (Projectile.alpha == 0)
                Lighting.AddLight(Projectile.Center, newColor2.ToVector3() * 0.5f);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            for (int num979 = 0; num979 < 2; num979++)
            {
                if (Main.rand.NextBool(10))
                {
                    Vector2 value55 = Vector2.UnitY.RotatedBy((double)((float)num979 * MathHelper.Pi)).RotatedBy((double)Projectile.rotation);
                    Dust dust24 = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 225, newColor2, 1f)];
                    dust24.noGravity = true;
                    dust24.noLight = true;
                    dust24.scale = Projectile.Opacity * Projectile.localAI[0];   // localAI[0] 原版从未赋值，恒为 0（尺寸为 0 的不可见粉尘，保留原样）
                    dust24.position = Projectile.Center;
                    dust24.velocity = value55 * 2.5f;
                }
            }
            for (int num980 = 0; num980 < 2; num980++)
            {
                if (Main.rand.NextBool(10))
                {
                    Vector2 value56 = Vector2.UnitY.RotatedBy((double)((float)num980 * MathHelper.Pi));
                    Dust dust25 = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 225, newColor2, 1f)];
                    dust25.noGravity = true;
                    dust25.noLight = true;
                    dust25.scale = Projectile.Opacity * Projectile.localAI[0];
                    dust25.position = Projectile.Center;
                    dust25.velocity = value56 * 2.5f;
                }
            }
            if (Main.rand.NextBool(10))
            {
                float scaleFactor13 = 1f + Main.rand.NextFloat() * 2f;
                float fadeIn = 1f + Main.rand.NextFloat();
                float num981 = 1f + Main.rand.NextFloat();
                Vector2 vector136 = Utils.RandomVector2(Main.rand, -1f, 1f);
                if (vector136 != Vector2.Zero)
                    vector136.Normalize();
                vector136 *= 20f + Main.rand.NextFloat() * 100f;
                Vector2 vector137 = Projectile.Center + vector136;
                Point point3 = vector137.ToTileCoordinates();
                bool flag52 = true;
                if (!WorldGen.InWorld(point3.X, point3.Y, 0))
                    flag52 = false;
                if (flag52 && WorldGen.SolidTile(point3.X, point3.Y))
                    flag52 = false;
                if (flag52)
                {
                    Dust dust26 = Main.dust[Dust.NewDust(vector137, 0, 0, DustID.RainbowMk2, 0f, 0f, 127, newColor2, 1f)];
                    dust26.noGravity = true;
                    dust26.position = vector137;
                    dust26.velocity = -Vector2.UnitY * scaleFactor13 * (Main.rand.NextFloat() * 0.9f + 1.6f);
                    dust26.fadeIn = fadeIn;
                    dust26.scale = num981;
                    dust26.noLight = true;
                    Dust dust27 = CloneDust(dust26);
                    dust27.scale *= 0.65f;
                    dust27.fadeIn *= 0.65f;
                    dust27.color = new Color(255, 255, 255, 255);
                }
            }
        }
        /// <summary>
        /// 命中敌人：滚一次追加长矛（对齐 2.2.2）。ai[0] == 0 的环形爆碎片不滚；
        /// 单片刃（ai[0] == 1）在强化档要 30、平时要 10
        /// </summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            CalamityDemutationPlayer modPlayer = Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            bool empowered = modPlayer.pscState == (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Empowered;
            modPlayer.rollBabSpears(Projectile.ai[0] == 0f ? 0 : empowered ? 30 : 10, target.chaseable);
        }
        /// <summary>前 120 帧不可命中</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.timeLeft > 120)
                return false;
            return null;
        }
        /// <summary>PvP 命中判定与上面的 NPC 判定同步</summary>
        public override bool CanHitPvp(Player target)
        {
            return Projectile.timeLeft <= 120;
        }
        /// <summary>
        /// 自毁：播放原版 SoundID.Item27（水晶碎裂）并以弹幕中心为原点向四周喷两轮彩虹色粉尘
        /// （第一轮尺寸 2 带淡入，第二轮尺寸随机；每颗都额外生成一颗白色减半复制体）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);
            Vector2 spinningpoint = new Vector2(0f, -3f).RotatedByRandom(MathHelper.Pi);
            float num69 = (float)Main.rand.Next(7, 13);
            Vector2 value5 = new Vector2(2.1f, 2f);
            Color newColor = Main.hslToRgb(Projectile.ai[0], 1f, 0.5f);
            newColor.A = 255;
            for (float num70 = 0f; num70 < num69; num70++)
            {
                int num71 = Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 0, newColor, 1f);
                Main.dust[num71].position = Projectile.Center;
                Main.dust[num71].velocity = spinningpoint.RotatedBy((double)(MathHelper.TwoPi * num70 / num69)) * value5 * (0.8f + Main.rand.NextFloat() * 0.4f);
                Main.dust[num71].noGravity = true;
                Main.dust[num71].scale = 2f;
                Main.dust[num71].fadeIn = Main.rand.NextFloat() * 2f;
                Dust dust11 = CloneDust(Main.dust[num71]);
                dust11.scale /= 2f;
                dust11.fadeIn /= 2f;
                dust11.color = new Color(255, 255, 255, 255);
            }
            for (float num73 = 0f; num73 < num69; num73++)
            {
                int num74 = Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 0, newColor, 1f);
                Main.dust[num74].position = Projectile.Center;
                Main.dust[num74].velocity = spinningpoint.RotatedBy((double)(MathHelper.TwoPi * num73 / num69)) * value5 * (0.8f + Main.rand.NextFloat() * 0.4f);
                Main.dust[num74].velocity *= Main.rand.NextFloat() * 0.8f;
                Main.dust[num74].noGravity = true;
                Main.dust[num74].scale = Main.rand.NextFloat() * 1f;
                Main.dust[num74].fadeIn = Main.rand.NextFloat() * 2f;
                Dust dust12 = CloneDust(Main.dust[num74]);
                dust12.scale /= 2f;
                dust12.fadeIn /= 2f;
                dust12.color = new Color(255, 255, 255, 255);
            }
        }
        /// <summary>自绘透明度：alpha 全部转为亮度（A 通道为 0，纯叠加发光）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255 - Projectile.alpha, 255 - Projectile.alpha, 255 - Projectile.alpha, 0);
        }
    }
}
