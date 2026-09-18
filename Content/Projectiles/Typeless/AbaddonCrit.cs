using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 硫磺爆炸（AbaddonCrit） - 阿巴顿饰品在暴击命中时生成的原地爆炸，由玩家命中钩子在命中点创建。
    /// 贴图不可见；判定是半径 300 的圆形（灾厄用 CollisionUtils.CircularHitboxCollision，本工程内联进 Colliding）；
    /// 伤害与击退由生成方给定（= 触发弹幕伤害的 3%，软上限 25）；存活 2 帧、每个敌人只命中一次。
    /// 命中附加 360 帧硫磺火，并喷 31 颗硫磺火尘（灾厄自定义尘，缺灾厄时回退 218 号原版尘）。
    /// </summary>
    internal class AbaddonCrit:ModProjectile
    {
        // ── 常量 ──
        /// <summary>爆炸判定半径（像素），灾厄原文写死 300</summary>
        private const float ExplosionRadius = 300f;
        /// <summary>命中附加的硫磺火时长（帧）</summary>
        private const int BrimstoneFrames = 360;
        /// <summary>回退尘：灾厄原文那个"1/4 概率分支"用的就是 218 号尘，缺灾厄时整圈都用它</summary>
        private const int FallbackDust = 218;
        // ── 静态字段 ──
        /// <summary>灾厄硫磺火尘的类型缓存（首次用时 TryFind 一次；-1 未初始化，0 表示灾厄未装或没有该尘）</summary>
        private static int brimstoneFlameDust = -1;
        // ── 属性 ──
        /// <summary>贴图取本工程的不可见占位图：爆炸外观全部交给尘</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>弹幕主人，用于取脸部饰品的染色（cFace）</summary>
        private Player Owner => Main.player[Projectile.owner];
        // ── 生命周期方法 ──
        /// <summary>
        /// 弹幕基础属性：300x300 判定框（实际判定走 Colliding 的圆形）、存活 2 帧、无限穿透、
        /// 不吃水、不与物块碰撞、每个敌人只命中一次（usesLocalNPCImmunity + localNPCHitCooldown = -1）。
        /// 伤害类型用通用职业（灾厄原文是它自己的 AverageDamageClass，本工程没有该职业）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 300;
            Projectile.height = 300;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        /// <summary>
        /// 诞生这一帧喷 31 颗硫磺火尘：1/4 概率用 218 号尘、其余用灾厄的硫磺火尘（缺灾厄时整圈都回退 218 号），
        /// 初速取 360 度随机方向 × 随机大小，统一按玩家脸部饰品染色，无重力。
        /// </summary>
        public override void AI()
        {
            int brimstone = GetBrimstoneFlameDust();
            for (int i = 0; i <= 30; i++)
            {
                int dustType = Main.rand.NextBool(4) || brimstone == 0 ? FallbackDust : brimstone;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, new Vector2(5, 5).RotatedByRandom(MathHelper.ToRadians(360)) * Main.rand.NextFloat(1.1f, 2.2f), 0, default, Main.rand.NextFloat(2.8f, 3.4f));
                dust.shader = GameShaders.Armor.GetSecondaryShader(Owner.cFace, Owner);
                dust.noGravity = true;
            }
        }
        /// <summary>
        /// 命中敌人：附加 360 帧硫磺火（灾厄现代版与经典版分别查找），并播放 89 号音效（音量 0.5、音高随机 0.4）。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "BrimstoneFlames", BrimstoneFrames);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", BrimstoneFrames);
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.5f, PitchVariance = 0.4f }, Projectile.Center);
        }
        /// <summary>
        /// 圆形判定（内联灾厄 CollisionUtils.CircularHitboxCollision）：圆心落在目标框内直接算命中；
        /// 否则取圆心到目标框四角的最小距离与半径比较（四角近似，与灾厄一致）。
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle center = new Rectangle((int)Projectile.Center.X, (int)Projectile.Center.Y, 1, 1);
            if (center.Intersects(targetHitbox))
                return true;
            float closest = Vector2.Distance(Projectile.Center, targetHitbox.TopLeft());
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.TopRight()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomLeft()));
            closest = Math.Min(closest, Vector2.Distance(Projectile.Center, targetHitbox.BottomRight()));
            return closest <= ExplosionRadius;
        }
        /// <summary>
        /// 不破坏物块（灾厄原文显式关掉）。
        /// </summary>
        public override bool? CanCutTiles() => false;
        // ── 私有工具 ──
        /// <summary>
        /// 取灾厄硫磺火尘的类型（懒加载一次 + 缓存）：灾厄未安装或没有该尘时返回 0，调用方据此回退到原版尘。
        /// </summary>
        private static int GetBrimstoneFlameDust()
        {
            if (brimstoneFlameDust < 0)
                brimstoneFlameDust = ModContent.TryFind("CalamityMod", "BrimstoneFlame", out ModDust dust) ? dust.Type : 0;
            return brimstoneFlameDust;
        }
    }
}
