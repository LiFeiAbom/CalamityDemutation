using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 日耀圣火（SarosSunfire，按灾厄 2.0 的 `Projectiles/Summon/SarosSunfire` 移植）——
    /// 星律之握览那道辐光光环洒出的高速火种（<see cref="SarosAura"/>）。
    /// 10×10 隐形判定 + 自发光 1，穿地形、边飞边追 1750 像素内的敌人（速度 20、19:1 插值）；
    /// 命中/消失时把判定框撑到 60、伤害除 3 后再结算一次（＝小范围爆炸）。
    /// </summary>
    internal class SarosSunfire:ModProjectile
    {
        /// <summary>贴图复用工程共用的隐形占位图（源同样指 CalamityMod/Projectiles/InvisibleProj）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>标记为仆从射弹（照源）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：10×10 判定、友方、自发光 1、存活 300 帧、仆从射弹、穿地形、召唤伤害类型（源原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 300;
            Projectile.minion = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// AI：每帧在自身位置喷一粒火尘（源用灾厄的 ProfanedFire 尘，本工程没有该尘 → 改用原版 Torch 尘），
        /// 并在 1750 像素内追敌（速度 20、按 19:1 插值平滑转向）。
        /// </summary>
        public override void AI()
        {
            int fireDust = Dust.NewDust(Projectile.Center, 1, 1, DustID.Torch);
            Main.dust[fireDust].velocity = Vector2.One.RotatedByRandom(MathHelper.TwoPi);
            Main.dust[fireDust].noGravity = true;
            NPC potentialTarget = Projectile.Center.MinionHoming(1750f, Main.player[Projectile.owner]);
            if (potentialTarget != null)
            {
                Projectile.velocity = (Projectile.velocity * 19f + Projectile.SafeDirectionTo(potentialTarget.Center) * 20f) / 20f;
            }
        }
        /// <summary>
        /// 消亡：把判定框撑到 60、改成无限穿透 + 每名敌人 10 帧局部无敌，伤害除以 3 后再结算一次总伤害
        ///（相当于一次小范围爆炸），播 `SoundID.Item14` 并喷 20 粒火尘（源原样）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.ExpandHitboxBy(60);
            Projectile.maxPenetrate = Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.damage /= 3;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            for (int i = 0; i < 20; i++)
            {
                int blastDust = Dust.NewDust(Projectile.Center, 1, 1, DustID.Torch);
                Main.dust[blastDust].velocity = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 6f);
                Main.dust[blastDust].noGravity = true;
            }
        }
    }
}
