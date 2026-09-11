using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 友方硫磺火球 - 灾厄系魔法弹幕
    /// 本体不可见，仅以红色光尘表现；消失时生成硫磺火爆炸（HellfireExplosionFriendly）
    /// </summary>
    internal class BrimstoneHellfireballFriendly:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：12x12 全透明碰撞箱；友方、入水不减速、存活 300 帧；
        /// 无限穿透配合 15 帧独立命中冷却，实现持续灼烧而不会一击消失；魔法伤害
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;   // 无限穿透，靠独立命中冷却控制频率
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;   // 同一敌人每 15 帧最多受击一次
        }
        /// <summary>
        /// AI：持续提供橙红色光照；生成瞬间用 localAI[0] 保证只播放一次音效；每帧生成 5 粒随弹体移动的红色光尘拖尾
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, ((255 - Projectile.alpha) * 0.5f) / 255f, ((255 - Projectile.alpha) * 0.05f) / 255f, ((255 - Projectile.alpha) * 0.05f) / 255f);
            // 生成瞬间播放一次音效（用 localAI[0] 保证只播一次）
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            // 拖尾：生成 5 个随弹体移动的红色光尘
            for (int num457 = 0; num457 < 5; num457++)
            {
                int num458 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.LifeDrain, 0f, 0f, 100, default(Color), 1.2f);
                Main.dust[num458].noGravity = true;
                Main.dust[num458].velocity *= 0.5f;
                Main.dust[num458].velocity += Projectile.velocity * 0.1f;
            }
            return;
        }
        /// <summary>
        /// 命中敌人：对目标施加灾厄（现代版/经典版二选一）的硫磺火 debuff，持续 1000 帧（约 16.7 秒）
        /// </summary>
        // 命中时施加灾厄的硫磺火 debuff（约 16.7 秒）
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
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
        /// 消亡（自然超时/被击杀）：仅在主人端于当前位置生成硫磺火爆炸（HellfireExplosionFriendly），避免联机重复生成
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            // 消失时生成硫磺火爆炸（仅主人端，避免联机重复生成）
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<HellfireExplosionFriendly>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
            }
        }
    }
}
