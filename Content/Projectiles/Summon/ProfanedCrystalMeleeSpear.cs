using System;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎之魂水晶·近战转化的圣光长矛（移植自灾厄 2.2.2 的 ProfanedCrystalMeleeSpear，贴图源自 HolySpear）。
    /// 使用近战武器时额外发射：平时每 6 次使用一枚（Enraged 及以上档每 4 次），每 30 次（Enraged 及以上 20 次）改为
    /// 一轮 5 枚扇形霰射。ai[1] == 1 表示霰射那一路（穿透 3 次）；ai[0] == 2 表示命中后炸开的回旋矛
    /// （不受前 133 帧的蓄势期限制，鞭标记概率也更高，见各自的注释）。
    /// 伤害按通用伤害折算（originalDamage 为未折算的基础值：霰射 700 / 主矛 500，由派发端写入）。
    /// </summary>
    internal class ProfanedCrystalMeleeSpear:ModProjectile
    {
        /// <summary>注册 2 帧残影缓存、鞭标记 0.6 倍增伤（tag 加伤在 GlobalProjectile.ModifyHitNPC 结算）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 2;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.6f;
        }
        /// <summary>基础属性：30x30 碰撞箱、友方、无视水、不撞地形、穿透 1 次、存活 50 帧、半透明、每敌人一次独立命中</summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 50;
            Projectile.minion = true;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>
        /// AI：每帧按通用伤害重算 damage（抵消多次加成叠加，originalDamage 由派发端写入），
        /// 持续加速（每帧 ×1.06）并按速度方向校正贴图旋转（+90 度），同时给自身补一点圣光照明。
        /// </summary>
        public override void AI()
        {
            Projectile.velocity *= 1.06f;
            Projectile.rotation = Projectile.velocity.ToRotation() + 1.57079637f;
            Lighting.AddLight(Projectile.Center, 1f, 0.2f, 0f);
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
        }
        /// <summary>出生首帧若是霰射那一路（ai[1] == 1）则把穿透提升到 3 次</summary>
        public override bool PreAI()
        {
            if (Projectile.timeLeft == 50 && Projectile.ai[1] == 1f)
                Projectile.penetrate = 3;
            return true;
        }
        /// <summary>前 133 帧只有"命中后炸开的回旋矛"（ai[0] >= 2）能命中，普通矛要飞完蓄势期才开启判定</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.timeLeft > 133 && Projectile.ai[0] < 2f)
                return false;
            return null;
        }
        /// <summary>PvP 命中判定与上面的 NPC 判定同步</summary>
        public override bool CanHitPvp(Player target)
        {
            return Projectile.timeLeft <= 133 || Projectile.ai[0] == 2f;
        }
        /// <summary>
        /// 命中前处理：ai[0] == 1（主矛）且只穿透 1 次时触发 handleSpecialHit（命中点炸出一圈回旋矛）；
        /// 随后按 ai[0] 与剩余穿透给主人的进攻守护者滚一次"追加长矛"（rollBabSpears，见下方调用）。
        /// </summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 1f && Projectile.penetrate == 1)
                handleSpecialHit(target.Center);
            // 命中时按穿透数滚一次追加长矛（对齐 2.2.2）：回旋矛（ai[0] == 2）要 20、普通长矛按剩余穿透 ×10
            int chance = Projectile.ai[0] == 2f ? 20 : 10 * Projectile.penetrate;
            Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(chance, target.chaseable);
        }
        /// <summary>
        /// 命中点炸出 10 枚回旋矛：在目标周围 450~500 像素的随机方向上生成，
        /// 以 15~18 的速度朝目标中心回旋，ai[0] 置 2（无视蓄势期、鞭标记概率更高），伤害为主矛的 1/4。
        /// </summary>
        private void handleSpecialHit(Vector2 targCenter)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                // 回旋矛的基础伤害为主矛基础值的 1/4，折算后回写 originalDamage（漏写会导致整轮零伤害）
                int subBaseDamage = (int)(0.25f * Projectile.originalDamage);
                int subDamage = (int)Main.player[Projectile.owner].GetTotalDamage<GenericDamageClass>().ApplyTo(subBaseDamage);
                for (int i = 0; i < 10; ++i)
                {
                    float startDist = Main.rand.NextFloat(450f, 500f);
                    Vector2 startDir = Main.rand.NextVector2Unit();
                    Vector2 startPoint = targCenter + (startDir * startDist);
                    float speed = Main.rand.NextFloat(15f, 18f);
                    Vector2 velocity = startDir * (-speed);
                    int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), startPoint, velocity, ModContent.ProjectileType<ProfanedCrystalMeleeSpear>(), subDamage, 0f, Projectile.owner, 2f, 0f);
                    if (Main.projectile.IndexInRange(proj))
                    {
                        Main.projectile[proj].DamageType = DamageClass.Generic;
                        Main.projectile[proj].originalDamage = subBaseDamage;
                    }
                }
            }
        }
        /// <summary>
        /// 绘制：先按昼夜取神圣色画十向背光，再用 CDUtil.DrawAfterimages 画 2 帧残影与本体
        /// （原为 Projectile.DrawBackglow + CalamityUtils.DrawAfterimagesCentered）。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawBackglow(MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha, true), 4f, TextureAssets.Projectile[Type].Value);
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Type], MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha), 1);
            return false;
        }
        /// <summary>
        /// 命中表现：播放原版 SoundID.Item74（长矛命中）；随机（1/2 或 1/3 概率）把判定框临时放大到 200 见方，
        /// 再按昼夜喷两轮神圣色粉尘（白天更多更大、夜晚少量小尺寸）。
        /// </summary>
        private void onHit()
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
            if (Main.rand.NextBool() || Main.rand.NextBool(3)) // 不是精确的 1/2，也不是稳定的 1/3
            {
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
        /// <summary>命中敌人播放上面的命中表现</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            onHit();
        }
        /// <summary>命中玩家（PvP）播放上面的命中表现</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            onHit();
        }
    }
}
