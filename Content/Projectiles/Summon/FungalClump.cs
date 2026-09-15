using CalamityDemutation.Content.Projectiles.Healing;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 真菌团块召唤物 - 由"真菌团块"饰品（modPlayer.fungalClump）召唤的飞行仆从
    /// 借用原版乌鸦 AI 模板（AIType = Raven）但未调用 base.AI()，实际不主动移动/索敌；
    /// 接触命中时按 25% 伤害转化为吸血量，生成真菌治疗珠（FungalHeal）飞回玩家补血。
    /// </summary>
    internal class FungalClump:ModProjectile
    {
        /// <summary>
        /// 静态属性：标记为可右键锁定目标、可牺牲的召唤物
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：40x40 碰撞箱；友方、不消耗召唤栏、无限穿透、存活约 25 分钟（18000×5 帧）；
        /// 借用乌鸦 AI 模板、召唤物标志、每个敌人独立 10 帧命中冷却、不碰撞地形
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.minionSlots = 0f;
            Projectile.aiStyle = ProjAIStyleID.Raven;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            AIType = ProjectileID.Raven;
            Projectile.tileCollide = false;
        }
        /// <summary>
        /// AI：未持有真菌团块/大杂烩召唤源（modPlayer.fungalClump）时直接消散；
        /// 主人存活期间持续刷新存活时间实现常驻（召唤增益 Update 据 ownedProjectileCounts
        /// 判定团块在场并续期，无需额外的"在场"ModPlayer 字段）；
        /// 出生时迸发一圈蓝色妖精粉尘，此后以 1/16 概率持续喷尘
        /// </summary>
        public override void AI()
        {
            bool flag64 = Projectile.type == ModContent.ProjectileType<Projectiles.Summon.FungalClump>();
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 未装备真菌团块/大杂烩饰品时直接消失，避免残留仆从
            if (!modPlayer.fungalClump)
            {
                Projectile.active = false;
                return;
            }
            if (flag64)
            {
                // 主人死亡：不再续命，仆从随 timeLeft（当前约 2 帧）自然消散
                if (!player.dead)
                {
                    // 主人存活且召唤源在身：持续刷新存活时间，实现常驻跟随
                    Projectile.timeLeft = 2;
                }
            }
            // 出生瞬间迸发一圈蓝色妖精粉尘作为登场特效（localAI[0] 保证只执行一次）
            if (Projectile.localAI[0] == 0f)
            {
                int num226 = 36;
                for (int num227 = 0; num227 < num226; num227++)
                {
                    // SafeNormalize：速度为 0 时回退到 UnitY，避免 Normalize 产生 NaN 粉尘
                    Vector2 vector6 = Projectile.velocity.SafeNormalize(Vector2.UnitY) * new Vector2((float)Projectile.width / 2f, (float)Projectile.height) * 0.75f;
                    vector6 = vector6.RotatedBy((double)((float)(num227 - (num226 / 2 - 1)) * 6.28318548f / (float)num226), default(Vector2)) + Projectile.Center;
                    Vector2 vector7 = vector6 - Projectile.Center;
                    int num228 = Dust.NewDust(vector6 + vector7, 0, 0, DustID.BlueFairy, vector7.X * 1.5f, vector7.Y * 1.5f, 100, default(Color), 1.4f);
                    Main.dust[num228].noGravity = true;
                    Main.dust[num228].noLight = true;
                    Main.dust[num228].velocity = vector7;
                }
                Projectile.localAI[0] += 1f;
            }
            // 之后每帧以 1/16 概率在体表持续喷出少量蓝尘，保持"真菌孢子"的观感
            if (Main.rand.NextBool(16))
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.BlueFairy, Projectile.velocity.X * 0.05f, Projectile.velocity.Y * 0.05f);
            }
        }
        /// <summary>
        /// 命中敌人：按实际造成伤害的 25% 扣除归属玩家（弹幕主人）的吸血额度（lifeSteal），
        /// 在敌人位置生成真菌治疗珠（FungalHeal），其 ai[0]=主人编号、ai[1]=本次吸血量
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 吸血量 = 造成伤害 × 25%；不足 1 点则忽略
            float num = (float)hit.Damage * 0.25f;
            if ((int)num == 0)
            {
                return;
            }
            // 归属玩家（弹幕主人，而非本地观察者）吸血额度耗尽时不再吸血
            Player ownerPlayer = Main.player[Projectile.owner];
            if (ownerPlayer.lifeSteal <= 0f)
            {
                return;
            }
            ownerPlayer.lifeSteal -= num;
            int num2 = Projectile.owner;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.position.X, target.position.Y, 0f, 0f, ModContent.ProjectileType<FungalHeal>(), 0, 0f, Projectile.owner, (float)num2, num);
        }
        /// <summary>
        /// 命中玩家（PvP）：逻辑与命中敌人相同——按 25% 伤害吸血并生成真菌治疗珠
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // 吸血量 = 造成伤害 × 25%
            float num = (float)info.Damage * 0.25f;
            if ((int)num == 0)
            {
                return;
            }
            // 归属玩家（弹幕主人，而非本地观察者）吸血额度耗尽时不再吸血
            Player ownerPlayer = Main.player[Projectile.owner];
            if (ownerPlayer.lifeSteal <= 0f)
            {
                return;
            }
            ownerPlayer.lifeSteal -= num;
            int num2 = Projectile.owner;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.position.X, target.position.Y, 0f, 0f, ModContent.ProjectileType<FungalHeal>(), 0, 0f, Projectile.owner, (float)num2, num);
        }
    }
}
