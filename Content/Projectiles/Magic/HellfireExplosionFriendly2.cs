using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 友方硫磺火爆炸（变体 2）- 硫磺火球 2 消失时生成的区域性魔法爆炸
    /// 本体不可见，以更密更大的红色光尘扩散表现；命中改写目标免疫帧并施加硫磺火 debuff
    /// </summary>
    internal class HellfireExplosionFriendly2:ModProjectile
    {
        /// <summary>使用隐形贴图，视觉效果完全由粉尘承担</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：130x130 无碰撞命中箱，友方、入水不减速、无限穿透、魔法伤害、存活 300 帧。
        /// ai[0] 兼作"喷尘计时器"（每帧 +4，超过约 230 即自毁）；ai[0]/ai[1] 另被当作"爆炸起点"坐标，
        /// 但本模组生成时速度为 0，那段越界自毁判定实际不触发（详见 AI 注释）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 130;
            Projectile.height = 130;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>
        /// AI：生成瞬间播放一次音效；每帧按 ai[0]/ai[1] 判定是否越过爆炸起点（越过即自毁）；
        /// 以 ai[0] 计数递减喷尘数量，并向四周扩散更大范围（±15 随机）的红色光尘，模拟爆炸扩散
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, ((255 - Projectile.alpha) * 0.75f) / 255f, ((255 - Projectile.alpha) * 0.05f) / 255f, ((255 - Projectile.alpha) * 0.05f) / 255f);
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            // 越界自毁判定：若爆炸沿某轴飞出自己的"起点"，该轴置位；两轴都越界即自毁。
            // 本模组生成此弹幕时 velocity 为 0，两个 flag 恒为 false，故此段实际是死分支（保留自源码）
            bool flag15 = false;   // X 轴越界
            bool flag16 = false;   // Y 轴越界
            if (Projectile.velocity.X < 0f && Projectile.position.X < Projectile.ai[0])
            {
                flag15 = true;
            }
            if (Projectile.velocity.X > 0f && Projectile.position.X > Projectile.ai[0])
            {
                flag15 = true;
            }
            if (Projectile.velocity.Y < 0f && Projectile.position.Y < Projectile.ai[1])
            {
                flag16 = true;
            }
            if (Projectile.velocity.Y > 0f && Projectile.position.Y > Projectile.ai[1])
            {
                flag16 = true;
            }
            if (flag15 && flag16)
            {
                Projectile.Kill();
            }
            float num461 = 25f;                       // 每帧喷出的光尘数量上限（再乘 0.7）
            if (Projectile.ai[0] > 180f)
            {
                num461 -= (Projectile.ai[0] - 180f) / 2f;   // 后期逐渐减少喷尘
            }
            if (num461 <= 0f)
            {
                num461 = 0f;
                Projectile.Kill();                    // 喷尘量归零即自毁（约 ai[0]=230 处）
            }
            num461 *= 0.7f;                           // 实际喷尘量为上限的 70%
            Projectile.ai[0] += 4f;                   // 计时器推进
            int num462 = 0;
            while ((float)num462 < num461)
            {
                float num463 = (float)Main.rand.Next(-15, 16);   // 随机方向 X（±15）
                float num464 = (float)Main.rand.Next(-15, 16);   // 随机方向 Y（±15）
                float num465 = (float)Main.rand.Next(5, 12);     // 该颗光尘的速度（5~11）
                float num466 = (float)Math.Sqrt((double)(num463 * num463 + num464 * num464));
                num466 = num465 / num466;                        // 归一化系数
                num463 *= num466;
                num464 *= num466;
                int num467 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.LifeDrain, 0f, 0f, 100, default(Color), 0.75f);
                Main.dust[num467].noGravity = true;
                Main.dust[num467].position.X = Projectile.Center.X;   // 强制从中心出发，忽略上面的随机初始位置
                Main.dust[num467].position.Y = Projectile.Center.Y;
                Dust expr_149DF_cp_0 = Main.dust[num467];
                expr_149DF_cp_0.position.X = expr_149DF_cp_0.position.X + (float)Main.rand.Next(-10, 11);   // 中心 ±10 的抖动
                Dust expr_14A09_cp_0 = Main.dust[num467];
                expr_14A09_cp_0.position.Y = expr_14A09_cp_0.position.Y + (float)Main.rand.Next(-10, 11);
                Main.dust[num467].velocity.X = num463;
                Main.dust[num467].velocity.Y = num464;
                num462++;
            }
            return;
        }
        /// <summary>
        /// 命中敌人：直接设置目标对该主人的无敌帧为 7（限制单次命中窗口），并施加灾厄硫磺火 debuff 1000 帧
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 7;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1000); }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1000); }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：对玩家施加灾厄硫磺火 debuff 1000 帧
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1000); }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1000); }
            }
        }
    }
}
