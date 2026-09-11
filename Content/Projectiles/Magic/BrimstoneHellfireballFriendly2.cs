using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 友方硫磺火球（变体 2）- 第二种硫磺火球弹幕（对应 HellfireExplosionFriendly2 爆炸）
    /// 本体不可见，仅以红色光尘表现；无限穿透直线飞行，存活 600 帧（约 10 秒）；
    /// 命中直接改写目标免疫帧并施加硫磺火 debuff，消失时生成大号硫磺火爆炸
    /// </summary>
    internal class BrimstoneHellfireballFriendly2:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：12x12 全透明碰撞箱；友方、入水不减速、无限穿透、魔法伤害、存活 600 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>
        /// AI：持续提供橙红色光照；生成瞬间用 localAI[0] 保证只播放一次音效；每帧生成 5 粒小型红色光尘拖尾
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, ((255 - Projectile.alpha) * 0.5f) / 255f, ((255 - Projectile.alpha) * 0.05f) / 255f, ((255 - Projectile.alpha) * 0.05f) / 255f);
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            // 每帧生成 5 粒随弹体移动的小型红色光尘作为拖尾外观
            for (int num457 = 0; num457 < 5; num457++)
            {
                int num458 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.LifeDrain, 0f, 0f, 100, default(Color), 0.6f);
                Main.dust[num458].noGravity = true;
                Main.dust[num458].velocity *= 0.5f;
                Main.dust[num458].velocity += Projectile.velocity * 0.1f;
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
        /// <summary>
        /// 消亡：仅在主人端于当前位置生成大号硫磺火爆炸（HellfireExplosionFriendly2），避免联机重复生成
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<HellfireExplosionFriendly2>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
            }
        }
    }
}
