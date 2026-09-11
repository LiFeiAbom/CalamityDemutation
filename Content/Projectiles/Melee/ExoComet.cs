using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 星云（Exo）彗星 - 星云武器发射的青色追踪彗星弹幕
    /// 带 5 帧动画与拖尾，前 40 帧（timeLeft>320）为减速蓄力阶段，之后加速转向索敌；
    /// 命中敌人施加全套"星云系"debuff（ExoDebuffs），消失时迸发青色爆尘并播放声响。
    /// </summary>
    internal class ExoComet:ModProjectile
    {
        /// <summary>
        /// 静态属性：注册 5 帧动画；预留 10 格残影缓存并启用残影绘制
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>
        /// 基础属性：12x12 碰撞箱；友方、入水不减速、无视地形、近战伤害、单次穿透；
        /// 初始半透明（alpha 50），存活 360 帧（约 6 秒）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.alpha = 50;
            Projectile.timeLeft = 360;
        }
        /// <summary>
        /// AI：播帧动画；前 30 帧从 alpha 50 逐渐变实；alpha 低于 40 时喷青色尾迹尘；
        /// 前 40 帧（timeLeft&gt;320）逐渐减速（蓄力），随后随时间累积转向速度并朝 1500 像素内敌人加速追踪
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 4)
                Projectile.frame = 0;
            // 存在的前 30 帧内透明度逐渐减小（半透明变清晰）；若弹体卡在实心块内则透明度锁定为 128
            if (Projectile.timeLeft > 30 && Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.timeLeft > 30 && Projectile.alpha < 128 && Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                Projectile.alpha = 128;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            // 弹体足够清晰时，在尾迹位置生成青色星云尘构成彗星拖尾
            if (Projectile.alpha < 40)
            {
                int exo = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X * 4f + 2f, Projectile.position.Y + 2f - Projectile.velocity.Y * 4f), 8, 8, DustID.TerraBlade, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, new Color(0, 255, 255), 0.5f);
                Main.dust[exo].velocity *= -0.25f;
                exo = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X * 4f + 2f, Projectile.position.Y + 2f - Projectile.velocity.Y * 4f), 8, 8, DustID.TerraBlade, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, new Color(0, 255, 255), 0.5f);
                Main.dust[exo].velocity *= -0.25f;
                Main.dust[exo].position -= Projectile.velocity * 0.5f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0f, 0.5f, 0.5f);
            // 存在的前 40 帧（timeLeft>320）：速度每帧 ×0.95 缓慢减速，模拟蓄力发射
            if (Projectile.timeLeft > 320)
            {
                Projectile.velocity *= 0.95f;
            }
            // 40 帧后进入追踪阶段：转向速度随 ai[0] 累积递增（封顶 20），朝 1500 像素内敌人加速追踪
            if (Projectile.timeLeft < 320)
            {
                float maxSpeed = 20f;
                float acceleration = 0.02f * 12f;
                float homeInSpeed = MathHelper.Clamp(Projectile.ai[0] += acceleration, 0f, maxSpeed);
                CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, 1500f, homeInSpeed, 15f, 5f);
            }
        }
        /// <summary>
        /// 命中敌人：调用扩展方法 ExoDebuffs() 施加全套星云系 debuff（灼烧/霜火/诅咒/灵液等）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.ExoDebuffs();
        }
        /// <summary>
        /// 命中玩家（PvP）：施加原版四元素 debuff 各 300 帧；
        /// 现代版灾厄再补四种 debuff 各 500 帧（HolyFlames/MiracleBlight/BrimstoneFlames/Plague）；
        /// 经典版以 1/30 概率补 ExoFreeze，另加四种 debuff 各 300 帧（BrimstoneFlames/GlacialState/Plague/HolyLight）。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(BuffID.CursedInferno, 300);
            target.AddBuff(BuffID.Ichor, 300);
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 500); }
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight)) { target.AddBuff(miracleBlight.Type, 500); }
                if (calamity.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 500); }
                if (calamity.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 500); }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (Main.rand.NextBool(30))
                {
                    if (calamity1.TryFind<ModBuff>("ExoFreeze", out ModBuff exoFreeze)) { target.AddBuff(exoFreeze.Type, 300); }
                }
                if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 300); }
                if (calamity1.TryFind<ModBuff>("GlacialState", out ModBuff glacialState)) { target.AddBuff(glacialState.Type, 300); }
                if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 300); }
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 300); }
            }
        }
        /// <summary>
        /// 消亡：播放僵尸系爆裂音效，把碰撞箱临时扩大到 80x80，随后向外迸发两轮青色星云爆尘
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Zombie103, Projectile.position);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 80;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            for (int i = 0; i < 2; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
            }
            for (int j = 0; j < 20; j++)
            {
                int exoDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 0, new Color(0, 255, 255), 2.5f);
                Main.dust[exoDust].noGravity = true;
                Main.dust[exoDust].velocity *= 3f;
                exoDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                Main.dust[exoDust].velocity *= 2f;
                Main.dust[exoDust].noGravity = true;
            }
        }
        /// <summary>
        /// 绘制青色残影拖尾（替换默认绘制）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>
        /// 固定青色（0,255,255）着色，透明度跟随 alpha 淡入淡出
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(0, 255, 255, Projectile.alpha);
        }
        /// <summary>
        /// 命中判定：仅剩余时间不足 240 帧（已结束蓄力并开始追踪）后，且目标可被弹幕追逐时才可命中
        /// </summary>
        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft < 240 && target.CanBeChasedBy(Projectile);
    }
}
