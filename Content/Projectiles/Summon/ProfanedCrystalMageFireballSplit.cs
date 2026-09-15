using System;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎之魂水晶·魔法转化的裂片（移植自灾厄 2.2.2 的 ProfanedCrystalMageFireballSplit，贴图源自 HolyFire2）。
    /// 由 ProfanedCrystalMageFireball 炸裂生成：前 50 帧减速预热且伤害置 0，第 550 帧起开启追踪与命中判定
    /// （先按主人右键锁定目标，否则全场就近索敌，以 24（近距离 28）为期望速度按 14/15 惯性插值）。
    /// 追踪期间只认锁定的那一个目标，碰到其它敌人会累计 hits，累计 25 次自毁。存活 600 帧（每帧更新 2 次，合计约 300 帧）。
    /// 命中敌人时按 1/35 的概率让主人的进攻守护者喷一圈"追加长矛"（rollBabSpears，见本类 OnHitNPC）。
    /// </summary>
    internal class ProfanedCrystalMageFireballSplit:ModProjectile
    {
        /// <summary>夜晚贴图路径（白天直接用类名对应的贴图）</summary>
        private const string NightTexture = "CalamityDemutation/Content/Projectiles/Summon/ProfanedCrystalMageFireballSplitNight";
        /// <summary>预热结束时锁存的伤害（0~50 帧期间 Projectile.damage 被压成 0）</summary>
        private int damage;
        /// <summary>撞到非锁定目标的次数，累计 25 次自毁</summary>
        private int hits = 0;
        /// <summary>本次追踪锁定的唯一目标</summary>
        private NPC target = null;
        /// <summary>注册 4 帧动画、对邪教徒类敌人有抗性减免、可被右键锁定目标、鞭标记 0.3 倍增伤</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.3f;
        }
        /// <summary>基础属性：26x26 碰撞箱、友方、无视水、穿透 -1（预热期不可命中）、不撞地形、额外更新 1 次、存活 600 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 600;
            Projectile.minion = true;
            Projectile.gfxOffY = -25f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>
        /// AI：每帧按通用伤害重算 damage（再用单独字段锁存/压制）；出生首帧把伤害锁进字段并压成 0，
        /// 预热 50 帧（速度每帧 ×0.95），第 550 帧起恢复伤害、穿透改为 1 并开启索敌追踪。
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.5f, 0.1f, 0f);
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            if (Projectile.timeLeft == 600)
            {
                damage = Projectile.damage;
                Projectile.damage = 0;
            }
            if (Projectile.timeLeft > 550)
                Projectile.velocity *= 0.95f;
            int num469 = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, MiniGuardianHealer.HolyDustType(!Main.dayTime), 0f, 0f, 100, default, Main.dayTime ? 1f : 0.75f);
            Main.dust[num469].noGravity = true;
            Main.dust[num469].velocity *= 0f;
            if (Projectile.timeLeft <= 550)
            {
                if (Projectile.penetrate == -1)
                    Projectile.damage = damage;
                Projectile.penetrate = 1;
                if (Projectile.timeLeft > 500)
                    Projectile.velocity *= 1.06f;
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
                        target = ownerMinionAttackTargetNPC2;
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
                                target = Main.npc[num542];
                            }
                        }
                    }
                }
                if (flag19)
                {
                    if (Projectile.ai[1] == 0f)
                    {
                        float num550 = 24f;
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
                    }
                }
            }
        }
        /// <summary>昼夜配色（白天橙红 / 夜晚青蓝）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha);
        }
        /// <summary>绘制：夜晚换用 Night 贴图，先画十向背光再按当前帧画本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Main.dayTime ? TextureAssets.Projectile[Type].Value : ModContent.Request<Texture2D>(NightTexture, AssetRequestMode.ImmediateLoad).Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            int frameY = frameHeight * Projectile.frame;
            Rectangle frame = new Rectangle(0, frameY, texture.Width, frameHeight);
            Projectile.DrawBackglow(MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha, true), 4f, texture);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), frame, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>命中敌人：小概率滚一次追加长矛（对齐 2.2.2 的 rollBabSpears(35, chaseable)）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(35, target.chaseable);
        }
        /// <summary>命中玩家（PvP）：同上，可追击恒真</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(35, true);
        }
        /// <summary>
        /// 命中筛选：锁定目标之后只认那一个目标，撞到其它敌人时累计 hits（含"亡灵棺"系列的秒杀豁免，
        /// 该系列为灾厄 NPC，本工程无同名敌人，故仅保留锁定目标这条主逻辑），累计 25 次自毁。
        /// </summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (this.target != null && target != this.target)
            {
                if (Projectile.getRect().Intersects(target.getRect()))
                {
                    hits++;
                    if (hits >= 25)
                        Projectile.Kill();
                }
                return false;
            }
            return null;
        }
        /// <summary>自毁：播放原版 SoundID.Item14（爆炸）并把判定框临时放大到 200 见方，按昼夜喷两轮粉尘</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = Projectile.height = 200;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            int dust = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            for (int num621 = 0; num621 < 4; num621++)
            {
                int num622 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[num622].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[num622].scale = 0.5f;
                    Main.dust[num622].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                }
            }
            for (int num623 = 0; num623 < 12; num623++)
            {
                int num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 3f : 0.75f);
                Main.dust[num624].noGravity = true;
                Main.dust[num624].velocity *= 5f;
                num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[num624].velocity *= 2f;
            }
        }
    }
}
