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
    /// 亵渎之魂水晶·远程转化的陨石（移植自灾厄 2.2.2 的 ProfanedCrystalRangedHuges）。
    /// 使用远程武器时额外发射：Enraged 及以上 100% 触发、否则 50% 触发；其中 20%/30% 是小陨石、
    /// 小陨石里再 5% 是"加厚"版本（ai[0] = 1：体型 1.5 倍、碰撞箱 +25，命中时向四周炸出 6~9 枚陨星）。
    /// 存活 175 帧（加厚/陨星版 200 帧），前 30 帧不撞地形；带 3 帧残影与 3 帧动画。
    /// 伤害按通用伤害折算（originalDamage 为未折算的基础值 600，由派发端写入）。
    /// </summary>
    internal class ProfanedCrystalRangedHuges:ModProjectile
    {
        /// <summary>夜晚贴图路径（白天直接用类名对应的贴图）</summary>
        private const string NightTexture = "CalamityDemutation/Content/Projectiles/Summon/ProfanedCrystalRangedHugesNight";
        /// <summary>是否为"陨星"（由加厚陨石命中时炸出的追尾弹）</summary>
        bool boomerSwarm = false;
        /// <summary>陨星失去目标后是否已准备自毁</summary>
        bool kill = false;
        /// <summary>本次追踪锁定的唯一目标</summary>
        NPC target = null;
        /// <summary>
        /// 陨星追踪 AI：全场就近索敌（优先主人右键锁定的目标），锁定后以 40（近距离 28）为期望速度按 14/15 惯性插值，
        /// 期间不撞地形；目标死亡后本弹幕改回撞地形，若曾锁定过目标则直接自毁。剩余存活不足 20 帧时会续到 50 帧并标记自毁。
        /// </summary>
        private void swarmAI()
        {
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
                Projectile.tileCollide = false;
                if (Projectile.timeLeft < 20)
                {
                    Projectile.timeLeft = 50;
                    kill = true;
                }
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
            }
            else
            {
                if (kill)
                    Projectile.Kill();
                Projectile.tileCollide = true;
            }
        }
        /// <summary>
        /// 加厚陨石命中时炸出的陨星群：在目标上方 500~700 像素、横向 ±500 的随机位置生成 6~9 枚，
        /// 以 25 的速度俯冲回目标附近，伤害是母弹的 1.5 倍（折算后回写 originalDamage），并把 boomerSwarm 置位。
        /// </summary>
        private void swarm(Vector2 targetPos)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                float swarmAmount = Main.rand.Next(6, 10); // 6~9
                int swarmBaseDamage = (int)(Projectile.originalDamage * 1.5);
                int swarmDamage = (int)Main.player[Projectile.owner].GetTotalDamage<GenericDamageClass>().ApplyTo(swarmBaseDamage);
                for (float i = 0; i < swarmAmount; i++)
                {
                    float x = targetPos.X + Main.rand.Next(-500, 501);
                    float y = targetPos.Y - 500 + Main.rand.Next(-200, 1);
                    Vector2 pos = new Vector2(x, y);
                    Vector2 correctedVelocity = Projectile.position - pos;
                    correctedVelocity.Normalize();
                    correctedVelocity *= 25f;
                    int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, correctedVelocity, ModContent.ProjectileType<ProfanedCrystalRangedHuges>(), swarmDamage, Projectile.knockBack, Projectile.owner, 2);
                    if (Main.projectile.IndexInRange(proj))
                    {
                        Main.projectile[proj].DamageType = DamageClass.Generic;
                        Main.projectile[proj].originalDamage = swarmBaseDamage;
                        ((ProfanedCrystalRangedHuges)Main.projectile[proj].ModProjectile).boomerSwarm = true;
                    }
                }
            }
        }
        /// <summary>注册 3 帧残影缓存、3 帧动画、鞭标记 0.4 倍增伤</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.4f;
        }
        /// <summary>基础属性：60x50 碰撞箱、友方、无视水、不撞地形、额外更新 1 次、穿透 1 次、存活 175 帧、每敌人一次独立命中</summary>
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 175;
            Projectile.localNPCHitCooldown = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>出生首帧处理加厚档（ai[0] >= 1）：体型放大到 1.5 倍、碰撞箱各 +25；陨星档（ai[0] == 2）把存活续到 200 帧</summary>
        public override bool PreAI()
        {
            bool begin = Projectile.timeLeft == 175;
            if (Projectile.ai[0] >= 1 && begin)
            {
                Projectile.scale = 1.5f;
                Projectile.width += 25;
                Projectile.height += 25;
                boomerSwarm = Projectile.ai[0] == 2;
                if (boomerSwarm && Projectile.ai[1] == 0f)
                {
                    Projectile.timeLeft = 200;
                    Projectile.ai[1] = 1f;
                }
            }
            return true;
        }
        /// <summary>
        /// AI：出生时若为加厚档则播放原版 SoundID.DD2_BetsyFireballShot（火焰球发射）；
        /// 每帧按通用伤害重算 damage、3 帧循环动画、存活 145 帧时开启地形碰撞、朝向跟随速度并持续加速
        /// （陨星 ×1.03、普通 ×1.02），原地留一颗神圣色粉尘；陨星额外走追踪 AI。
        /// </summary>
        public override void AI()
        {
            if (Projectile.timeLeft == 175 && Projectile.scale == 1.5f)
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.Center);
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 8)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 2)
                Projectile.frame = 0;
            if (Projectile.timeLeft == 145 && Projectile.ai[0] < 2f)
                Projectile.tileCollide = true;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= boomerSwarm ? 1.03f : 1.02f;
            int dust = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            int num469 = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, 1f);
            Main.dust[num469].noGravity = true;
            Main.dust[num469].velocity *= 0f;
            if (boomerSwarm)
                swarmAI();
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
        /// <summary>
        /// 命中筛选：锁定目标之后只认那一个目标（原版对"亡灵棺"系列有秒杀豁免，该系列为灾厄 NPC，本工程无同名敌人故省略）。
        /// </summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (this.target != null && target != this.target)
                return false;
            return null;
        }
        /// <summary>
        /// 命中敌人：滚一次追加长矛（对齐 2.2.2）——加厚档（ai[0] == 1）只要 1、普通陨石要 10；
        /// 若为加厚档（scale 1.5 且非陨星）则炸出陨星群
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(Projectile.ai[0] == 1f ? 1 : 10, target.chaseable);
            if (Projectile.scale == 1.5f && Projectile.ai[0] != 2f)
                swarm(target.Center);
        }
        /// <summary>命中玩家（PvP）：逻辑同命中敌人，可追击恒真</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(Projectile.ai[0] == 1f ? 1 : 10, true);
            if (Projectile.scale == 1.5f && Projectile.ai[0] != 2f)
                swarm(target.Center);
        }
        /// <summary>
        /// 自毁：播放原版 SoundID.Item14（爆炸）并按昼夜喷两轮粉尘；加厚档额外喷三轮共 12 块原版血腥碎块（Gore 61~63）。
        /// </summary>
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
            if (Projectile.scale != 1.5f)
                return;
            if (!Main.dedServ)
            {
                for (int num625 = 0; num625 < 3; num625++)
                {
                    float scaleFactor10 = 0.33f;
                    if (num625 == 1)
                        scaleFactor10 = 0.66f;
                    if (num625 == 2)
                        scaleFactor10 = 1f;
                    for (int i = 0; i < 4; i++)
                    {
                        int num626 = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2) - 24f, Projectile.position.Y + (float)(Projectile.height / 2) - 24f), default, Main.rand.Next(61, 64), 0.75f);
                        Gore gore = Main.gore[num626];
                        gore.velocity *= scaleFactor10;
                        if (i == 0 || i == 2)
                            gore.velocity.X += 1f;
                        else
                            gore.velocity.X -= 1f;
                        if (i < 2)
                            gore.velocity.Y += 1f;
                        else
                            gore.velocity.Y -= 1f;
                    }
                }
            }
        }
    }
}
