using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 至圣神圣爆炸（HolyExplosionSupreme） - 极乐之庇护盾牌冲刺撞击时在玩家中心生成的不可见判定弹幕。
    /// 90×90 判定框、存活 3 帧、无限穿透，伤害与击退完全由生成方给定（冲刺中为 500 伤害 / 15 击退）；
    /// 消亡时播放亵渎天神神圣爆破冲击音，并按玩家护盾染色喷出两圈尘（24 颗环状 + 12 组 259 号拖尾）。
    /// </summary>
    internal class HolyExplosionSupreme : ModProjectile
    {
        // ── 属性 ──
        /// <summary>贴图取本工程的不可见占位图：判定框弹幕没有实体外观，表现全部交给消亡时的尘</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>弹幕主人，用于取护盾染色（cShield）</summary>
        private Player Owner => Main.player[Projectile.owner];
        // ── 生命周期方法 ──
        /// <summary>
        /// 弹幕基础属性：90×90 判定框、存活 3 帧、无限穿透、不吃水不与物块碰撞，
        /// 每个敌人 4 帧独立免疫（usesLocalNPCImmunity + localNPCHitCooldown）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }
        /// <summary>
        /// 消亡时：播放亵渎天神神圣爆破冲击音（音量 0.6），
        /// 再以 228 号神圣尘喷 24 颗环状尘、12 组 259 号拖尾尘，全部按玩家护盾染色。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.ProvidenceHolyBlastImpact, Projectile.Center);
            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, new Vector2(4.5f, 4.5f).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.9f), 0, default, Main.rand.NextFloat(1.5f, 2.8f));
                dust.shader = GameShaders.Armor.GetSecondaryShader(Owner.cShield, Owner);
                dust.noGravity = true;
            }
            for (int j = 0; j < 12; j++)
            {
                Vector2 dustVel = new Vector2(6, 6).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dustVel * 2, DustID.SolarFlare, dustVel, 0, default, 1f);
                dust.shader = GameShaders.Armor.GetSecondaryShader(Owner.cShield, Owner);
            }
        }
    }
}
