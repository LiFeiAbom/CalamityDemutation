using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 泰拉巨刃的大刀光（移植自大修 TerratomereBigSlashs）：小闪电命中后生成的大型刀光，
    /// 命中目标累计电击计数（TerratomereBoltOnHitNum），累计超过 5 次触发泰拉巨刃爆炸、累计 6 次清零。
    /// 用 ExobladePierce 着色器 + BlobbyNoise / Extra_189 画刀光。
    /// </summary>
    internal class TerratomereBigSlashs : ModProjectile
    {
        public int TargetIndex = -1;
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>拖尾缓存 28 点、TrailingMode 2</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
        }
        /// <summary>基础属性：12×12、穿透无限、不碰撞物块、存活 27 帧、本地免疫 -1</summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 27;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        /// <summary>尺寸随剩余寿命淡入</summary>
        public override void AI() => Projectile.scale = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, clamped: true);
        public float SlashWidthFunction(float _, Vector2 __) => Projectile.width * Projectile.scale * Utils.GetLerpValue(0f, 0.1f, _, clamped: true);
        public Color SlashColorFunction(float _, Vector2 __) => Color.Lime * Projectile.Opacity;
        /// <summary>命中：累计电击计数，超过 5 次触发爆炸（场上爆炸不超过 3 个时）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TargetIndex = target.whoAmI;
            CalamityDemutationGlobalNPC g = target.GetGlobalNPC<CalamityDemutationGlobalNPC>();
            g.TerratomereBoltOnHitNum++;
            if (g.TerratomereBoltOnHitNum > 6)
                g.TerratomereBoltOnHitNum = 0;
            target.netUpdate = true;
            if (g.TerratomereBoltOnHitNum > 5 && Main.player[Projectile.owner].ownedProjectileCounts[ModContent.ProjectileType<TerratomereExplosion>()] <= 3)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<TerratomereExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                if (Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
                Projectile.velocity *= 0.2f;
                Projectile.damage = 0;
                Projectile.netUpdate = true;
            }
        }
        /// <summary>死亡：若目标累计超过 5 次电击，召唤小刀光创造者（场上不超过 3 个）</summary>
        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner && TargetIndex >= 0 && Main.npc.IndexInRange(TargetIndex))
            {
                int types = ModContent.ProjectileType<TerratomereSlashCreator>();
                if (Main.npc[TargetIndex].GetGlobalNPC<CalamityDemutationGlobalNPC>().TerratomereBoltOnHitNum > 5 && Main.player[Projectile.owner].ownedProjectileCounts[types] < 3)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.npc[TargetIndex].Center, Vector2.Zero, types, Projectile.damage, Projectile.knockBack, Projectile.owner, TargetIndex, Main.rand.NextFloat(MathF.PI * 2f));
            }
        }
        /// <summary>ExobladePierce 着色器沿 oldPos 画 4 遍刀光（BlobbyNoise + Extra_189）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityDemutation:ExobladePierce"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/GreyscaleGradients/BlobbyNoise"));
            GameShaders.Misc["CalamityDemutation:ExobladePierce"].UseImage2("Images/Extra_189");
            GameShaders.Misc["CalamityDemutation:ExobladePierce"].UseColor(Terratomere.TerraColor1);
            GameShaders.Misc["CalamityDemutation:ExobladePierce"].UseSecondaryColor(Terratomere.TerraColor2);
            GameShaders.Misc["CalamityDemutation:ExobladePierce"].Apply();
            for (int i = 0; i < 4; i++)
                PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(SlashWidthFunction, SlashColorFunction, (float _, Vector2 _) => Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:ExobladePierce"]), 30);
            return false;
        }
        /// <summary>碰撞：取 oldPos[0] 到弹幕中心的线段</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.oldPos[0] == Vector2.Zero)
                return false;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.oldPos[0] + Projectile.Size * 0.5f, Projectile.Center);
        }
    }
}
