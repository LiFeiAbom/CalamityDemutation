using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 金源灭却刃的光束弹幕（移植自大修 DivineSourceBladeProjectile）：左键射出的金色光束，
    /// 穿透 5 次，首次命中时在目标处召唤泰拉巨刃的小刀光创造者。拖尾用 TrailStreak + ScarletDevilStreak。
    /// </summary>
    internal class DivineSourceBladeProjectile : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/DivineSourceBeam";
        /// <summary>拖尾缓存 25 点、TrailingMode 2</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 25;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        /// <summary>基础属性：32×32、穿透 5、不碰撞物块、存活 600 帧、本地免疫 -1、MaxUpdates 2</summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.MaxUpdates = 2;
        }
        /// <summary>朝向对齐速度</summary>
        public override void AI() => Projectile.rotation = Projectile.velocity.ToRotation();
        /// <summary>首次命中（numHits==0）且手持金源灭却刃时，召唤小刀光创造者（寿命 30 帧）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            Item item = player.ActiveItem();
            if (Projectile.numHits == 0 && item.type == ModContent.ItemType<DivineSourceBlade>())
            {
                int proj = Projectile.NewProjectile(new EntitySource_ItemUse(player, item), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TerratomereSlashCreator>(), Projectile.damage, 0, Projectile.owner, target.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi));
                Main.projectile[proj].timeLeft = 30;
            }
        }
        /// <summary>拖尾颜色：暖金到白随时间正弦摆动</summary>
        public Color ColorFunction(float completionRatio, Vector2 _)
        {
            float amount = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos((0f - Main.GlobalTimeWrappedHourly) * 3f) * 0.5f + 0.5f);
            float num = Utils.GetLerpValue(1f, 0.64f, completionRatio, clamped: true) * Projectile.Opacity;
            Color value = Color.Lerp(new Color(255, 223, 186), new Color(255, 218, 185), (float)Math.Sin(completionRatio * MathF.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(new Color(255, 248, 220), value, amount) * num;
        }
        /// <summary>拖尾宽度：随完成度三次方衰减到 0</summary>
        public float WidthFunction(float completionRatio, Vector2 _)
        {
            float amount = (float)Math.Pow(1f - completionRatio, 3.0);
            return MathHelper.Lerp(0f, 22f * Projectile.scale * Projectile.Opacity, amount);
        }
        /// <summary>TrailStreak 着色器 + ScarletDevilStreak 贴图绘制拖尾，再画光束本体（左向翻转）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityDemutation:TrailStreak"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (float _, Vector2 _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:TrailStreak"]), 30);
            Texture2D mainValue = CDUtil.GetT2DValue(Texture);
            Main.EntitySpriteDraw(mainValue, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver2, CDUtil.GetOrig(mainValue), Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            return false;
        }
    }
}
