using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 苍华之庭·孢子云（PlantationStaffSporeCloud，移植自灾厄 2.0.3.9 的同名弹幕）——
    /// 树灵进入冲撞时朝四周喷出的 12 团减速绿云，随寿命渐隐，三种随机贴图。
    /// </summary>
    /// <remarks>
    /// 与源的差异：源每 10 帧还会生成一颗灾厄的 `SmallSmokeParticle`（走灾厄粒子系统），
    /// 本工程没有对应的烟类粒子，故只保留源里同步生成的中毒尘（`DustID.Poisoned`），视觉上等价。
    /// </remarks>
    internal class PlantationStaffSporeCloud:ModProjectile
    {
        /// <summary>随机贴图档：0 / 1 / 2 三套（源在生成时写进 ai[0]）</summary>
        public ref float RandomTexture => ref Projectile.ai[0];

        /// <summary>标记为召唤物射击弹幕</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        /// <summary>基础属性（照源）：32×32、600 帧寿命、无限穿透、同类型弹幕共享 30 帧命中冷却</summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.idStaticNPCHitCooldown = 30;
            Projectile.timeLeft = 600;
            Projectile.width = Projectile.height = 32;
            Projectile.penetrate = -1;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
        }
        public override void AI()
        {
            // 逐渐减速、随水平速度旋转，并在最后 180 帧里从全显渐隐到全透明
            Projectile.velocity *= 0.985f;
            Projectile.rotation += MathHelper.ToRadians(Projectile.velocity.X);
            Projectile.alpha = (int)Utils.Remap(Projectile.timeLeft, 180f, 0f, 0f, 255f);

            if (Main.rand.NextBool(10))
            {
                // 源写裸值 46 = DustID.Poisoned（中毒尘），照源同一编号
                Dust sporeDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Poisoned);
                sporeDust.noGravity = true;
                sporeDust.velocity = Vector2.Zero;
                sporeDust.alpha = (int)Utils.Remap(Projectile.timeLeft, 180f, 0f, 0f, 255f);
            }
        }
        /// <summary>自绘：按随机档换三套贴图之一</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            if (RandomTexture == 1f)
                tex = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/PlantationStaffSporeCloud2").Value;
            if (RandomTexture == 2f)
                tex = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/PlantationStaffSporeCloud3").Value;

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
