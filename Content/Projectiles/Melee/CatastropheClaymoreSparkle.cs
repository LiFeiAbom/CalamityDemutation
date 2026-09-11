using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 灾变黏土剑火花：依据 ai[0] 切换粉尘与命中 debuff（霜焰/灵液/狱火）
    /// </summary>
    internal class CatastropheClaymoreSparkle:ModProjectile
    {
        public ref float ProjectileType => ref Projectile.ai[0];
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：20x20 碰撞箱；友方、近战伤害、单次穿透、存活 80 帧；ai[0]（ProjectileType）决定火花种类
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 80;
        }
        /// <summary>
        /// 依据 ai[0] 选择粉尘类型，持续生成拖尾火花
        /// </summary>
        public override void AI()
        {
            int dustType = ProjectileType == 2f ? 57 : ProjectileType == 1f ? 56 : 73; // Frostbite, Ichor, Hellfire respectively
            for (int i = 0; i < 3; i++)
            {
                Dust trail = Dust.NewDustPerfect(Projectile.Center, dustType);
                trail.noGravity = true;
                trail.scale = 1.35f;
                trail.velocity = Projectile.velocity * 0.5f;
            }
        }
        /// <summary>
        /// 命中敌人时依据 ai[0] 施加对应 debuff
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (ProjectileType)
            {
                case 1f:
                    target.AddBuff(BuffID.Frostburn2, 90);
                    break;
                case 2f:
                    target.AddBuff(BuffID.Ichor, 60);
                    break;
                default:
                    target.AddBuff(BuffID.OnFire3, 120);
                    break;
            }
        }
        /// <summary>
        /// 命中玩家（PvP）时依据 ai[0] 施加对应 debuff
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            switch (ProjectileType)
            {
                case 1f:
                    target.AddBuff(BuffID.Frostburn2, 90);
                    break;
                case 2f:
                    target.AddBuff(BuffID.Ichor, 60);
                    break;
                default:
                    target.AddBuff(BuffID.OnFire3, 120);
                    break;
            }
        }
        /// <summary>
        /// 消失时播放音效并迸发一圈对应颜色的火花
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            int dustType = ProjectileType == 2f ? 57 : ProjectileType == 1f ? 56 : 73; // Frostbite, Ichor, Hellfire respectively
            float effectiveVelocity = Projectile.velocity.Length() * Projectile.MaxUpdates;
            for (int i = 0; i < 60; i++)
            {
                Dust boom = Dust.NewDustPerfect(Projectile.Center, dustType);
                boom.noGravity = true;
                boom.scale = Main.rand.NextFloat(0.8f, 1.7f);
                boom.velocity = Vector2.UnitY.RotatedByRandom(MathHelper.Pi / 6f) * Main.rand.NextFloat(-2.4f, -1.2f) * effectiveVelocity;
            }
        }
    }
}
