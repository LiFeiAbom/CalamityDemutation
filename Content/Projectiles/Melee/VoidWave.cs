using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 虚空波（VoidWave，移植自 CalamityEntropy）：虚空分形挥砍时朝前甩出的追踪波，伤害与本体相同。
    /// 一路朝 4000 范围内最近的目标以 0.01 的强度贴过去，寿命剩 200 帧起整体淡出。
    /// 本体只画一张贴图，表现靠 32 帧位置历史摊成的白色残影（越旧越淡）。
    /// <para>
    /// 与 CE 原版的差异：① 索敌沿用 tML 自带的 <c>Projectile.FindTargetWithinRange</c>（CE 调的是
    /// <c>CEUtils.FindTarget_HomingProj</c>，同一套语义）；② 转向直接用本工程既有的
    /// <see cref="CDUtil.ChasingBehavior2"/>（= CE 的 <c>CEUtils.SmoothHomingBehavior</c>，逐字同构）；
    /// ③ 删掉 CE 里从未被读取的 <c>init</c> / <c>counter</c> / <c>pg</c> / <c>playSound</c> 四个字段
    /// （前者只写不读，后三者连写都没有，纯残留）。
    /// </para>
    /// </summary>
    internal class VoidWave:ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 32;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.timeLeft = 1 * 60 * 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 4;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            if (Projectile.timeLeft < 200)
            {
                Projectile.Opacity = Projectile.timeLeft / 200f;
            }
            NPC target = Projectile.FindTargetWithinRange(4000f, false);
            if (target != null)
            {
                Projectile.ChasingBehavior2(target.Center, 1f, 0.01f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        /// <summary>本体不直接绘制，而是把 32 帧位置历史摊成一串白色残影（越旧越淡），再在中心叠一笔亮些的本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
            {
                float prog = i / (float)ProjectileID.Sets.TrailCacheLength[Type];
                Color clr = Color.White * 0.4f * (1 - prog);
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, clr, Projectile.oldRot[i]);
            }
            Draw(Projectile.Center, Color.White * 0.8f, Projectile.rotation);
            return false;
        }
        private void Draw(Vector2 pos, Color lightColor, float rotation)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, pos - Main.screenPosition, null, lightColor * Projectile.Opacity, rotation, texture.Size() * 0.5f, Projectile.scale * 2, SpriteEffects.None);
        }
    }
}
