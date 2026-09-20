using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 泰拉巨刃的刀光创造者（移植自灾厄 TerratomereSlashCreator）：
    /// 隐形无伤害的生成源，附着在目标 NPC 上，每 9 帧向随机方向放出一道小刀光 <see cref="TerratomereSlash"/>。
    /// 由 TerratomereHoldout / TerratomereBigSlashs 命中、以及金源灭却刃首击召唤。
    /// </summary>
    internal class TerratomereSlashCreator : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>目标 NPC（存于 ai[0]）</summary>
        public NPC Target => Main.npc[(int)Projectile.ai[0]];
        /// <summary>刀光朝向（存于 ai[1]；超过 π 视为完全随机朝向）</summary>
        public float SlashDirection => Projectile.ai[1] > MathHelper.Pi
            ? Main.rand.NextFloatDirection()
            : Projectile.ai[1] + Main.rand.NextFloatDirection() * 0.2f;
        /// <summary>基础属性：2×2 隐形生成源、不碰撞物块、穿透无限、存活 45 帧、MaxUpdates 2、禁用附魔视觉</summary>
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.MaxUpdates = 2;
            Projectile.noEnchantmentVisuals = true;
        }
        /// <summary>
        /// 每 9 帧（灾厄 Terratomere.SmallSlashCreationRate）播放急速斩击音并在目标中心随机偏移处
        /// 放出一道伤害 ×0.4（SmallSlashDamageFactor）的小刀光；偏移上限 300 像素、初速 0.1 贴近目标。
        /// </summary>
        public override void AI()
        {
            if (Projectile.timeLeft % 9 == 0)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.SwiftSliceSound, Projectile.Center);
                if (Main.myPlayer == Projectile.owner)
                {
                    float maxOffset = Target.width * 0.4f;
                    if (maxOffset > 300f)
                        maxOffset = 300f;
                    Vector2 spawnOffset = SlashDirection.ToRotationVector2() * Main.rand.NextFloatDirection() * maxOffset;
                    Vector2 sliceVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * 0.1f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Target.Center + spawnOffset, sliceVelocity
                        , ModContent.ProjectileType<TerratomereSlash>(), (int)(Projectile.damage * 0.4f), 0f, Projectile.owner);
                }
            }
        }
        /// <summary>本体是生成源，不造成伤害</summary>
        public override bool? CanDamage() => false;
    }
}
