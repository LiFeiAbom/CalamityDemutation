using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 能量球（PlanetaryAnnihilationProj） - 行星湮灭洒下的隐形能量球，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/PlanetaryAnnihilationProj.cs</c>（逐行照搬）。
    /// <para>
    /// **贴图是工程共用的隐形贴图**（源写 <c>Texture => "CalamityMod/Projectiles/InvisibleProj"</c>）——
    /// 它的全部视觉就是每帧撒的那 1 颗粒尘，**颜色由 <c>ai[1]</c>（0~6）决定**
    /// （源那串裸值是 15 / 74 / 73 / 162 / 90 / 173 / 57 ⇒ 魔镜 / 绿仙灵 / 粉仙灵 / 热射线 / 红宝石 /
    /// 暗影束 / 附魔金）。
    /// 判定 8×8、穿透 1、寿命 600 帧、**额外更新 1 帧**、算作"箭"；AI 里每帧撒 1 颗本体色的尘
    /// （缩放 2.2、无重力、速度清零）并**追踪 200 像素内最近的敌人**（追踪速度 12、惯性 20）。
    /// </para>
    /// <para>
    /// 消亡（照源，等同"小爆炸"）：一声 `Item14` → 判定框**扩大到 40×40**、穿透改 -1、
    /// 开本地无敌 10 帧、伤害 **÷2** 并**立刻自己结算一发伤害** → 撒 20 + 20 + 10 颗本体色尘。
    /// </para>
    /// <para>
    /// 与源的三点差别：① 源的 `CalamityGlobalProjectile.HomeInOnNPC` 换成工程既有的同签名
    /// <c>CDUtil.HomeInOnNPC</c>；② 源用 <c>Projectile.MaxUpdates</c>（1.3 口径）算尘埃初速，
    /// 本工程按 `extraUpdates + 1` 换算；③ 源的尘色是 AI 里赋值的实例字段、`Kill` 里直接用，
    /// 本工程改成"按 <c>ai[1]</c> 查同一张表"（正常路径完全一致，且不会出现字段未赋值时退化成尘 0）。
    /// </para>
    /// </summary>
    internal class PlanetaryAnnihilationProj : ModProjectile
    {
        /// <summary>
        /// 七色尘表：下标＝弹幕的 <c>ai[1]</c>（照源那串裸值 15 / 74 / 73 / 162 / 90 / 173 / 57 翻译成名字）
        /// </summary>
        private static readonly int[] DustTypes =
        {
            DustID.MagicMirror,      // 0
            DustID.GreenFairy,       // 1
            DustID.PinkFairy,        // 2
            DustID.HeatRay,          // 3
            DustID.GemRuby,          // 4
            DustID.ShadowbeamStaff,  // 5
            DustID.Enchanted_Gold,   // 6
        };
        /// <summary>每帧那颗尘的缩放（照源 2.2）</summary>
        private const float TrailDustScale = 2.2f;
        /// <summary>追踪半径 / 追踪速度 / 惯性（照源 200 / 12 / 20）</summary>
        private const float HomingRange = 200f;
        private const float HomingVelocity = 12f;
        private const float HomingInertia = 20f;
        /// <summary>消亡时判定框扩大到多少（照源 40×40）</summary>
        private const int DeathHitboxSize = 40;
        /// <summary>消亡的三组尘的缩放（照源 2.1 / 1.1 / 2.5）与枚数（20 / 20 / 10）</summary>
        private const float DeathDustScaleBig = 2.1f;
        private const float DeathDustScaleSmall = 1.1f;
        private const float DeathDustScaleHuge = 2.5f;
        private const int DeathDustCountPerKind = 20;
        private const int DeathDustCountRadial = 10;
        /// <summary>贴图用工程共用的隐形图（照源）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：8×8 判定、穿透 1、寿命 600 帧、额外更新 1 帧、算作"箭"（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 600;
            Projectile.arrow = true;
        }
        /// <summary>本体色（按 <c>ai[1]</c> 查表，越界时夹回表内）</summary>
        private int DustType => DustTypes[Utils.Clamp((int)Projectile.ai[1], 0, DustTypes.Length - 1)];
        /// <summary>AI：撒 1 颗本体色的尘（原地、无重力）+ 追踪 200 像素内最近的敌人（照源）</summary>
        public override void AI()
        {
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustType, 0f, 0f, 100, default, TrailDustScale);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0f;

            CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, HomingRange, HomingVelocity, HomingInertia);
        }
        /// <summary>
        /// 消亡：一声 `Item14` → 把判定框撑到 <see cref="DeathHitboxSize"/> 见方并**当场结算一发半伤伤害**
        /// → 撒三组本体色尘（照源）
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            int dustType = DustType;
            Vector2 sprayDirection = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 velocityOffset = sprayDirection * Projectile.velocity.Length() * (Projectile.extraUpdates + 1);

            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            // 判定框原位换成 40×40 的大框（照源：先取 Center 当准心，改完尺寸再钉回中心）
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = DeathHitboxSize;
            Projectile.Center = Projectile.position;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.damage /= 2;
            Projectile.Damage();

            for (int i = 0; i < DeathDustCountPerKind; i++)
            {
                int bigDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 200, default, DeathDustScaleBig);
                Main.dust[bigDust].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                Main.dust[bigDust].noGravity = true;
                Main.dust[bigDust].velocity *= 3f;
                Main.dust[bigDust].velocity += velocityOffset * Main.rand.NextFloat();

                int smallDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, DeathDustScaleSmall);
                Main.dust[smallDust].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                Main.dust[smallDust].velocity *= 2f;
                Main.dust[smallDust].noGravity = true;
                Main.dust[smallDust].fadeIn = 1f;
                Main.dust[smallDust].velocity += velocityOffset * Main.rand.NextFloat();
            }

            for (int i = 0; i < DeathDustCountRadial; i++)
            {
                int radialDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 0, default, DeathDustScaleHuge);
                Main.dust[radialDust].position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
                Main.dust[radialDust].noGravity = true;
                Main.dust[radialDust].velocity *= 0.5f;
                Main.dust[radialDust].velocity += velocityOffset * (0.6f + 0.6f * Main.rand.NextFloat());
            }
        }
    }
}
