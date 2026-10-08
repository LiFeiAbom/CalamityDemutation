using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 震爆手雷的手雷本体（照灾厄 2.0 的 <c>ShockGrenadeProjectile</c>）：
    /// 10×10 判定、穿透 1、存活 180 帧；受重力（每帧 +0.1，封顶 16）并随速度自转；
    /// 撞物块时先在 <c>OnTileCollide</c> 里把反弹方向记进 localAI 再 <c>Kill()</c>，
    /// 由 <c>OnKill</c> 按那对方向撒出闪电。
    /// <para>
    /// <c>OnKill</c> 里的一切生成**只在主人端做**（第 5 节联机口径）：弹幕会随同步包发到其他端，
    /// 不在主人端生成就不会有重复伤害。音效不加密，两端都响。
    /// </para>
    /// <para>
    /// 贴图直接借用物品贴图（源也是这么写的：<c>Texture =&gt; ".../Items/Weapons/Rogue/ShockGrenade"</c>），
    /// 另叠一层同名 Glow。
    /// </para>
    /// </summary>
    internal class ShockGrenadeProjectile : ModProjectile
    {
        /// <summary>物品贴图宽度（glow 的绘制原点用）</summary>
        private const int SpriteWidth = 14;
        /// <summary>物品贴图高度（glow 的绘制原点用）</summary>
        private const int SpriteHeight = 30;

        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/ShockGrenade";

        /// <summary>10×10 判定、穿透 1、存活 180 帧，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>每帧：加重力（封顶 16）+ 随水平方向自转</summary>
        public override void AI()
        {
            Projectile.velocity.Y += 0.1f;
            if (Projectile.velocity.Y >= 16f)
                Projectile.velocity.Y = 16f;
            Projectile.rotation += Projectile.direction * 0.2f;
        }
        /// <summary>
        /// 撞物块：先扬起碎块，然后把"撞在哪个方向"记进 localAI[0]（水平 ±1）/ localAI[1]（垂直 ±1），
        /// 再手动 <c>Kill()</c>（会触发 <c>OnKill</c>，那里按这对方向撒闪电）。
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Projectile.localAI[0] = 0f;
            Projectile.localAI[1] = 0f;
            if (Projectile.velocity.X != oldVelocity.X)
            {
                if (oldVelocity.X < 0f)
                    Projectile.localAI[0] = 1f;
                if (oldVelocity.X > 0f)
                    Projectile.localAI[0] = -1f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                if (oldVelocity.Y < 0f)
                    Projectile.localAI[1] = 1f;
                if (oldVelocity.Y > 0f)
                    Projectile.localAI[1] = -1f;
            }
            Projectile.Kill();
            return false;
        }
        /// <summary>手绘物品贴图（物品贴图 14×30，比 10×10 的判定框大，故不走隐式贴图）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>叠加发光层</summary>
        public override void PostDraw(Color lightColor)
        {
            Vector2 origin = new Vector2(SpriteWidth / 2f, SpriteHeight / 2f);
            Main.EntitySpriteDraw(ModContent.Request<Texture2D>(Texture + "Glow").Value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
        }
        /// <summary>
        /// 爆炸：5~10 道闪电（伤害 = 手雷伤害 ÷ 2）；潜行打击时闪电带追踪（ai[1] = 1），
        /// 并额外留一个电气光环（伤害 = 手雷伤害 ÷ 4）；最后总是来一发本体爆炸（全额伤害）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item94 with { Volume = SoundID.Item94.Volume * 0.75f }, Projectile.position);
            if (Projectile.owner != Main.myPlayer)
                return;
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            int boltCount = Main.rand.Next(5, 11);
            for (int i = 0; i < boltCount; i++)
            {
                const float boltScatter = 1f;
                Vector2 boltVelocity = new Vector2(Main.rand.NextFloat(-boltScatter, boltScatter), Main.rand.NextFloat(-boltScatter * 2f, boltScatter * 2f));
                if (Projectile.localAI[0] != 0f)
                    boltVelocity.X *= -1f;
                if (Projectile.localAI[1] != 0f)
                    boltVelocity.Y *= -1f;
                boltVelocity.X += Projectile.localAI[0];
                boltVelocity.Y += Projectile.localAI[1] * 2f;
                boltVelocity.Normalize();
                boltVelocity *= 10f;
                int boltType = Main.rand.Next(0, 2);
                int boltDamage = Projectile.damage / 2;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, boltVelocity, ModContent.ProjectileType<ShockGrenadeBolt>(), boltDamage, 0f, Projectile.owner, boltType, stealthStrike ? 1f : 0f);
            }
            if (stealthStrike)
            {
                int auraDamage = Projectile.damage / 4;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShockTeslaAura>(), auraDamage, 1f, Projectile.owner, 0f, 0f);
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = SoundID.Item93.Volume * 0.5f }, Projectile.position);
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShockGrenadeExplosion>(), Projectile.damage, 3f, Projectile.owner, 0f, 0f);
        }
    }
}
