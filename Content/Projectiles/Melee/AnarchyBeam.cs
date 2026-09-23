using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 混乱之刃的光束弹幕（移植自灾厄大修 0.4.0.1.3 的 <c>AnarchyBeam</c>）。
    /// 贴图直接复用灾厄的 <c>BrimlashProj</c>（本工程存为 AnarchyBeam.png）；
    /// 飞行中一边减速一边变大，速度低于 3 时自毁；消亡时朝四周甩出 13 颗硫磺爆炸。
    /// </summary>
    internal class AnarchyBeam : ModProjectile
    {
        /// <summary>
        /// 灾厄的硫磺火焰尘类型（<c>CalamityMod.Dusts.BrimstoneFlame</c>）。
        /// 灾厄源码里用的是硬编码枚举 <c>CalamityDusts.Brimstone = 235</c>，那是"Calamity 先加载"前提下的固定值，
        /// 本工程软依赖取不到那个编号，改成按类名找 <see cref="ModDust"/>；找不到时退回原版火尘。
        /// </summary>
        public static int BrimstoneDustType => ModContent.TryFind("CalamityMod", "BrimstoneFlame", out ModDust brimstoneFlame) ? brimstoneFlame.Type : DustID.Torch;
        /// <summary>灾厄的硫磺爆炸弹幕类型（<c>CalamityMod.Projectiles.Melee.BrimstoneBoom</c>）；未加载时返回 0</summary>
        public static int BrimstoneBoomType() => ModContent.TryFind("CalamityMod", "BrimstoneBoom", out ModProjectile brimstoneBoom) ? brimstoneBoom.Type : 0;
        /// <summary>预留 4 格残影缓存并启用残影绘制</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>基础属性：20×20、友方、穿水、无限穿透、存活 300 帧、近战伤害、本地无敌帧 15 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }
        /// <summary>朝速度方向旋转 45°，速度每帧 ×0.99、体积每帧 +0.007；速度低于 3 时自毁；持续洒落硫磺尘</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.velocity *= 0.99f;
            Projectile.scale += 0.007f;
            if (Projectile.velocity.Length() < 3f)
                Projectile.Kill();
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0.05f / 255f, (255 - Projectile.alpha) * 0.05f / 255f);
            int brimDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, BrimstoneDustType, 0f, 0f, 100, default, 1f);
            Main.dust[brimDust].noGravity = true;
            Main.dust[brimDust].velocity *= 0.5f;
            Main.dust[brimDust].velocity += Projectile.velocity * 0.1f;
        }
        /// <summary>固定画成纯白（颜色交给拖尾与着色器）</summary>
        public override Color? GetAlpha(Color lightColor) => Color.White;
        /// <summary>刚生成的 5 帧不画（等淡入），之后画星形拖尾 + 残影</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 295)
                return false;
            DrawStarTrail(Projectile, Color.Red, Color.White);
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>消亡：沿路径洒落硫磺尘，并由拥有者端甩出 13 颗伤害等于本弹幕的硫磺爆炸</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            int inc;
            for (int i = 4; i < 31; i = inc + 1)
            {
                float dustX = Projectile.oldVelocity.X * (30f / i);
                float dustY = Projectile.oldVelocity.Y * (30f / i);
                int deathDust = Dust.NewDust(new Vector2(Projectile.oldPosition.X - dustX, Projectile.oldPosition.Y - dustY), 8, 8, BrimstoneDustType, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, default, 1.8f);
                Main.dust[deathDust].noGravity = true;
                Dust dust = Main.dust[deathDust];
                dust.velocity *= 0.5f;
                deathDust = Dust.NewDust(new Vector2(Projectile.oldPosition.X - dustX, Projectile.oldPosition.Y - dustY), 8, 8, BrimstoneDustType, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, default, 1.4f);
                dust = Main.dust[deathDust];
                dust.velocity *= 0.05f;
                inc = i;
            }
            int boom = BrimstoneBoomType();
            if (Projectile.owner != Main.myPlayer || boom <= 0)
                return;
            for (int i = 0; i < 13; i++)
                Projectile.NewProjectile(new EntitySource_Parent(Projectile), Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.Next(0, 222), Vector2.Zero, boom, Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
        /// <summary>命中敌人：附加硫磺火 180 帧（灾厄两版都没有该 buff 时退回原版着火了）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "BrimstoneFlames", 180, BuffID.OnFire);
        }
        /// <summary>
        /// 星形拖尾（移植自灾厄 <c>CalamityUtils.DrawStarTrail</c>）：外圈 3 片绕弹体自转的光翼，
        /// 内圈两层按时间脉冲缩放的核心；贴图取自灾厄的 <c>Projectiles/StarTrail.png</c>。
        /// </summary>
        private static void DrawStarTrail(Projectile projectile, Color outer, Color inner, float auraHeight = 10f)
        {
            Texture2D aura = ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/Trails/StarTrail").Value;
            Vector2 offsets = new Vector2(0f, projectile.gfxOffY) - Main.screenPosition;
            Rectangle auraRec = new Rectangle(0, 0, aura.Width, aura.Height);
            float auraRotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 auraOrigin = new Vector2(auraRec.Width / 2f, auraHeight);
            Vector2 drawStartOuter = offsets + projectile.Center + projectile.velocity;
            Vector2 spinPoint = -Vector2.UnitY * auraHeight;
            float time = Main.player[projectile.owner].miscCounter % 216000f / 60f;
            Color outerColor = outer * 0.2f;
            outerColor.A = 0;
            float rotation = MathHelper.TwoPi * time;
            for (int o = 0; o < 6; o += 2)
            {
                Vector2 spinStart = drawStartOuter + spinPoint.RotatedBy(rotation - MathHelper.Pi * o / 3f);
                float scaleMultOuter = 1.5f - o * 0.1f;
                Main.EntitySpriteDraw(aura, spinStart, auraRec, outerColor, auraRotation, auraOrigin, scaleMultOuter, SpriteEffects.None, 0);
            }
            Vector2 drawStartInner = offsets + projectile.Center - projectile.velocity * 0.5f;
            Color innerColor = inner * 0.5f;
            innerColor.A = 0;
            for (float i = 0f; i < 1f; i += 0.5f)
            {
                float scaleMult = time % 0.5f / 0.5f;
                scaleMult = (scaleMult + i) % 1f;
                float colorMult = scaleMult * 2f;
                if (colorMult > 1f)
                    colorMult = 2f - colorMult;
                Main.EntitySpriteDraw(aura, drawStartInner, auraRec, innerColor * colorMult, auraRotation, auraOrigin, 0.3f + scaleMult * 0.5f, SpriteEffects.None, 0);
            }
        }
    }
}
