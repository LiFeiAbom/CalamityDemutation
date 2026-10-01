using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 庇护巨刃（DefenseBladeProj，移植自灾厄大修 0.4.0.1.3 的 <c>AegisBladeProj</c>）——
    /// 庇护之刃右键掷出的巨刃，三段式：
    /// ① 升起（ai[1]=0）：以 -12 的纵向初速抛出，随减速逐渐展开；
    /// ② 蓄力悬停（ai[1]=1）：悬在玩家上方缓慢放大（上限 2.5 倍），按住右键持续蓄力，
    ///    伤害按帧递增（上限为原始伤害的 45 倍）；松手或超时即进入下一段；
    /// ③ 掷出（ai[1]=2）：锁定 6000 像素内最近的敌人飞去，命中后用半径 1200 的爆炸收尾；
    ///    若巨刃放大超过 1.6 倍，消亡时还会抛洒 12 团防御烈焰（<see cref="DefenseFlame"/>，伤害 ×0.5）。
    /// <para>
    /// 与源的差异：① CWR 的 LightParticle 换成本工程自研的 <see cref="GlowSpark"/>；
    /// ② 输入只在主人端读取、靠 netUpdate 同步阶段（源的 <c>PressKey</c> 在别的客户端恒为 false，
    /// 会让那边的巨刃提前进入掷出阶段）。
    /// </para>
    /// </summary>
    internal class DefenseBladeProj : ModProjectile
    {
        /// <summary>复用手持贴图：巨刃各阶段都画同一张剑身</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/DefenseBlade";
        /// <summary>
        /// 拖尾缓存：13 个历史采样点；TrailingMode=2 同时记录 oldPos 与 oldRot，
        /// 供掷出阶段的残影绘制使用。
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 13;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        /// <summary>基础属性：32×32 判定框、不撞物块、无限穿透、存活 600 帧。</summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }
        /// <summary>
        /// 三段式状态机（ai[1] 为阶段、ai[0] 为帧计数兼蓄力计时）：
        /// 升起 → 蓄力悬停（主人端读右键决定何时松手）→ 锁敌掷出。
        /// 掷出阶段只由主人端决定是否转向，其余端按同步来的 ai 跟随。
        /// </summary>
        public override void AI()
        {
            bool localOwner = Projectile.IsOwnedByLocalPlayer();
            Player owner = Main.player[Projectile.owner];
            // 未进入蓄力悬停前，剑身朝向速度方向
            if (Projectile.ai[1] != 1f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            // 首帧：以 -12 的纵向初速把巨刃抛上天
            if (Projectile.ai[0] == 0f)
                Projectile.velocity = new Vector2(0f, -12f);
            // ① 升起：逐渐展开，减速到近乎静止后进入蓄力
            if (Projectile.ai[1] == 0f)
            {
                Projectile.scale += 0.01f;
                Projectile.velocity *= 0.97f;
                Projectile.position += owner.velocity;
                if (Projectile.velocity.LengthSquared() < 9f)
                {
                    Projectile.ai[1] = 1f;
                    Projectile.ai[0] = 1f;
                    Projectile.netUpdate = true;
                }
            }
            // ② 蓄力悬停：放大 + 金色火花 + 伤害累加，主人端按右键状态决定何时掷出
            if (Projectile.ai[1] == 1f)
            {
                if (Projectile.scale < 2.5f)
                {
                    Projectile.scale += 0.02f;
                    // 从巨刃外圈向中心收束的金色火花（源为 CWR 的 LightParticle）
                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 pos = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.Next(133, 140) * Projectile.scale;
                            Vector2 particleSpeed = pos.To(Projectile.Center).UnitVector() * 17f;
                            DRKLoader.NewParticle(new GlowSpark { grav = false, Lifetime = 16 }, pos, particleSpeed, Color.Gold, Main.rand.NextFloat(0.3f, 0.5f));
                        }
                    }
                }
                else if (!Main.dedServ)
                {
                    // 到达上限后改喷向外的暗金色火花
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 randomDirection = Main.rand.NextVector2Unit();
                        Vector2 pos = Projectile.Center + randomDirection * Main.rand.Next(3, 14) * Projectile.scale;
                        Vector2 particleSpeed = randomDirection * 19f;
                        DRKLoader.NewParticle(new GlowSpark { grav = false, Lifetime = Main.rand.Next(16, 18) }, pos, particleSpeed, Color.DarkGoldenrod, Main.rand.NextFloat(0.1f, 0.6f));
                    }
                }
                if (Projectile.damage < Projectile.originalDamage * 45)
                    Projectile.damage += 35;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation += 0.2f;
                Projectile.position += owner.velocity;
                if (localOwner)
                {
                    bool holding = owner.PressKey(false);   // 只在主人端读取右键，其余端按同步来的 ai 跟随
                    if (holding)
                    {
                        Projectile.timeLeft = 300;
                        if (Projectile.ai[0] > 55f)
                            Projectile.ai[0] = 55f;
                    }
                    if (Projectile.ai[0] > 60f || !holding)
                    {
                        Projectile.ai[1] = 2f;
                        Projectile.netUpdate = true;
                    }
                }
            }
            // ③ 掷出：锁定最近敌人高速飞出；没有目标则直接自毁
            if (Projectile.ai[1] == 2f)
            {
                NPC npc = Projectile.Center.FindClosestNPC(6000f, true, true);
                if (npc != null)
                {
                    Projectile.ChasingBehavior(npc.Center, 56f);
                    Projectile.penetrate = 1;
                }
                else
                {
                    Projectile.Kill();
                }
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            }
            Projectile.ai[0]++;
        }
        /// <summary>只有掷出阶段的巨刃才有伤害判定（升起与蓄力阶段不碰人）。</summary>
        public override bool? CanDamage() => Projectile.ai[1] != 2f ? false : base.CanDamage();
        /// <summary>
        /// 消亡：先把伤害恢复成初始面板，再以半径 1200 的爆炸结算一次范围伤害；
        /// 随后补齐金色火花，并在放大超过 1.6 倍时向四周抛出 12 团防御烈焰（伤害 ×0.5）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.damage = Projectile.originalDamage;
            Projectile.Explode(1200);
            if (!Main.dedServ)
            {
                for (int i = 0; i < 156; i++)
                {
                    Vector2 particleSpeed = Main.rand.NextVector2Unit() * Main.rand.Next(13, 34);
                    DRKLoader.NewParticle(new GlowSpark { grav = false, Lifetime = 30 }, Projectile.Center, particleSpeed, Color.Gold, Main.rand.NextFloat(0.5f, 1.3f));
                }
            }
            // 只有主人端生成子弹幕，避免联机下重复生成
            if (Projectile.IsOwnedByLocalPlayer() && Projectile.scale > 1.6f)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity.UnitVector() * 13f, velocity, ModContent.ProjectileType<DefenseFlame>(), (int)(Projectile.damage * 0.5f), 0f, Projectile.owner, 0f, 0f);
                }
            }
        }
        /// <summary>屏蔽默认绘制：掷出阶段额外铺一层随历史位置衰减的残影。</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            if (Projectile.ai[1] == 2f)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Main.EntitySpriteDraw(texture, Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2f, null, Color.White * (1f - i * 0.1f), Projectile.rotation, texture.Size() / 2f, Projectile.scale - i * 0.1f, SpriteEffects.None, 0f);
                }
            }
            return false;
        }
    }
}
