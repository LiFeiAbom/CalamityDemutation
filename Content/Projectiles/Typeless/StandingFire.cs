using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 站桩火焰 - 落地后原地持续燃烧的隐形火苗弹幕（希腊火/地面灼烧类效果）
    /// 本体不可见，仅以红色光尘表现；携带重力下落，触地前播放燃火音效；
    /// 燃烧期间反复命中并施放灾厄硫磺火 debuff，存活 240 帧（约 4 秒）。
    /// </summary>
    internal class StandingFire:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：6x12 碰撞箱；友方、无限穿透、存活 240 帧；未显式启用 local 免疫，靠改写目标免疫帧控制命中节奏
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
        }
        /// <summary>
        /// AI：NaN 纠偏 + 重力下落/贴地滑动模拟火苗落地；若本体类型处于希腊火区间则首次播放燃火音效；
        /// 每帧生成受速度影响的红色光尘，并限制最大下落速度 16
        /// </summary>
        public override void AI()
        {
            // 照抄经典版的 NaN 自愈分支：NaN × 负数仍是 NaN，无法纠偏（该分支实际无效，属反编译遗留）
            if (Projectile.velocity.X != Projectile.velocity.X)
            {
                Projectile.velocity.X = Projectile.velocity.X * -0.1f;
            }
            if (Projectile.velocity.X != Projectile.velocity.X)
            {
                Projectile.velocity.X = Projectile.velocity.X * -0.5f;
            }
            if (Projectile.velocity.Y != Projectile.velocity.Y && Projectile.velocity.Y > 1f)
            {
                Projectile.velocity.Y = Projectile.velocity.Y * -0.5f;
            }
            // ai[0] 前 5 帧为启动期；之后每帧施加 0.2 重力模拟下落
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;
                // 已贴地（Y 速度为 0）仍水平滑动时水平速度每帧 ×0.97，接近零则停住并同步网络
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;
            // 本体类型处于希腊火区间时，首次播放"火苗点燃"音效（ai[1] 保证只播一次）
            if (Projectile.ai[1] == 0f && Projectile.type >= ProjectileID.GreekFire1 && Projectile.type <= ProjectileID.GreekFire3)
            {
                Projectile.ai[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item13, Projectile.position);
            }
            int num199 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.LifeDrain, 0f, 0f, 100, default(Color), 1f);
            Dust expr_8976_cp_0 = Main.dust[num199];
            expr_8976_cp_0.position.X = expr_8976_cp_0.position.X - 2f;
            Dust expr_8994_cp_0 = Main.dust[num199];
            expr_8994_cp_0.position.Y = expr_8994_cp_0.position.Y + 2f;
            Main.dust[num199].scale += (float)Main.rand.Next(50) * 0.01f;
            Main.dust[num199].noGravity = true;
            Dust expr_89E7_cp_0 = Main.dust[num199];
            expr_89E7_cp_0.velocity.Y = expr_89E7_cp_0.velocity.Y - 2f;
            if (Main.rand.Next(2) == 0)
            {
                int num200 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.LifeDrain, 0f, 0f, 100, default(Color), 1f);
                Dust expr_8A4E_cp_0 = Main.dust[num200];
                expr_8A4E_cp_0.position.X = expr_8A4E_cp_0.position.X - 2f;
                Dust expr_8A6C_cp_0 = Main.dust[num200];
                expr_8A6C_cp_0.position.Y = expr_8A6C_cp_0.position.Y + 2f;
                Main.dust[num200].scale += 0.3f + (float)Main.rand.Next(50) * 0.01f;
                Main.dust[num200].noGravity = true;
                Main.dust[num200].velocity *= 0.1f;
            }
            // 下落速度处于 0.15~0.25 的"将落地"区间时水平速度衰减 20%，模拟触地摩擦
            if ((double)Projectile.velocity.Y < 0.25 && (double)Projectile.velocity.Y > 0.15)
            {
                Projectile.velocity.X = Projectile.velocity.X * 0.8f;
            }
            Projectile.rotation = -Projectile.velocity.X * 0.05f;
            // 限制最大下落速度为 16
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }
        }
        /// <summary>
        /// 命中敌人：改写目标对该主人的免疫帧为 7（限制单次命中窗口），并施加灾厄硫磺火 debuff 160 帧
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 7;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 160); }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 160); }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：施加灾厄硫磺火 debuff 160 帧
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 160); }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 160); }
            }
        }
        /// <summary>
        /// 碰撞地形：仅当穿透数耗尽（penetrate==0）时销毁弹幕；因本弹幕穿透为 -1，实际上永远不会由此触发销毁
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.penetrate == 0)
            {
                Projectile.Kill();
            }
            return false;
        }
    }
}
