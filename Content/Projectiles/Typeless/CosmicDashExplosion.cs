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
    /// 宇宙冲刺爆炸（CosmicDashExplosion） - 阿斯加德之庇护盾牌冲刺撞击时在玩家中心生成的不可见判定弹幕。
    /// 140×140 判定框、存活 3 帧、无限穿透，伤害与击退完全由生成方给定（冲刺中为 1000 伤害 / 20 击退）；
    /// 消亡时播放体节碎裂与 62 号音效，并按玩家护盾染色喷出两圈尘（35 颗环状 + 14 组 272/226 双色拖尾）。
    /// </summary>
    internal class CosmicDashExplosion : ModProjectile
    {
        // ── 属性 ──
        /// <summary>贴图取本工程的不可见占位图：判定框弹幕没有实体外观，表现全部交给消亡时的尘</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>弹幕主人，用于取护盾染色（cShield）</summary>
        private Player Owner => Main.player[Projectile.owner];
        // ── 生命周期方法 ──
        /// <summary>
        /// 弹幕基础属性：140×140 判定框、存活 3 帧、无限穿透、不吃水不与物块碰撞，
        /// 每个敌人 4 帧独立免疫（usesLocalNPCImmunity + localNPCHitCooldown）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 140;
            Projectile.height = 140;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }
        /// <summary>
        /// 消亡时：播放体节碎裂（音高随机 0.3）与 62 号音效（音量 0.5、音高随机 0.3），
        /// 再以 181 号冷焰尘喷 35 颗环状尘、14 组 272/226 双色拖尾尘，全部按玩家护盾染色。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.DevourerSegmentBreak1 with { PitchVariance = 0.3f }, Projectile.position);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f, PitchVariance = 0.3f }, Projectile.position);
            for (int i = 0; i < 35; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GiantCursedSkullBolt, new Vector2(4.5f, 4.5f).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.9f), 0, default, Main.rand.NextFloat(1.5f, 2.8f));
                dust.shader = GameShaders.Armor.GetSecondaryShader(Owner.cShield, Owner);
                dust.noGravity = true;
            }
            for (int j = 0; j < 14; j++)
            {
                Vector2 dustVel = new Vector2(6, 6).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dustVel * 2, DustID.WitherLightning, dustVel, 0, default, 1f);
                dust.shader = GameShaders.Armor.GetSecondaryShader(Owner.cShield, Owner);
                Dust dust2 = Dust.NewDustPerfect(Projectile.Center + dustVel * 2, DustID.Electric, dustVel, 0, default, 1f);
                dust2.shader = GameShaders.Armor.GetSecondaryShader(Owner.cShield, Owner);
            }
        }
    }
}
