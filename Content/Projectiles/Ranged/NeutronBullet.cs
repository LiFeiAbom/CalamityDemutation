using CalamityDemutation.Common.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 中子弹（NeutronBullet） - 中子枪（NeutronGun）左右键射出的弹体，移植自 CWR main 版的同名类。
    /// 本体只画一张等离子长矛贴图（不再走残影拖尾），另由 IDrawWarp 交给中子星扭曲管线做引力透镜扰动。
    /// 命中时原地炸一个小爆点（<see cref="NeutronExplosionRanged"/>），并从目标四周朝内落下 3 道中子光束。
    /// <para>
    /// 与上一版（CWR 0.5.0.1.7）的区别：贴图从灾厄的 GodSlayerSlugBlue 换成 CWR 自带的
    /// <c>Masking/Line</c>；判定框 4→14、存活 60→160 帧；绘制从"6 段残影叠加"改成单体绘制；
    /// 命中落光束从"蓄力弹才落、且限 8 次"改成"每发都落"。
    /// </para>
    /// </summary>
    internal class NeutronBullet : ModProjectile, IDrawWarp
    {
        /// <summary>本弹幕的贴图（从 CWR 的 Assets/Masking/Line.png 复制而来，32×256 的白色等离子长矛）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Ranged/NeutronBullet";
        /// <summary>
        /// 静态设置：残影缓存 6 段、追踪模式 0。当前绘制已不用残影，保留是为了与 CWR 原版一致
        /// （它同样保留了这两行）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        /// <summary>
        /// 基础属性：14×14 判定箱、远程伤害、透明度从全隐淡入、每帧更新 6 次（飞得极快）、
        /// 无限穿透且对同一敌人只结算一次（localNPCHitCooldown = -1）、存活 160 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.MaxUpdates = 6;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 160;
        }
        /// <summary>
        /// 生成时立即把贴图转到速度方向：本弹幕由持握弹幕在 AI 里生成，若首帧未及跑 AI（rotation 仍是 0），
        /// 竖直长矛会在枪口闪一帧竖着的白光（即"枪口左右、消亡快"的怪光束）；这里提前设置，保证第一帧就水平。
        /// </summary>
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>
        /// 每帧：淡入（alpha 每帧 -5），并把贴图转到与速度对齐（长矛贴图竖直，故 +90°）
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 5;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>
        /// 命中敌怪：原地炸一个小爆点，并从目标四周朝圆心落下 3 道中子光束
        /// （出生点 = 圆心 + 随机方向 ×10 倍半径，速度取反即朝内飞）。
        /// 生成动作只在主人端做，避免各客户端各生成一份幽灵弹幕
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero
                , ModContent.ProjectileType<NeutronExplosionRanged>(), Projectile.damage, 0f);
            for (int i = 0; i < 3; i++)
            {
                // CWR 的 VaultUtils.RandVr(16, 18)：随机单位方向 × 16~17
                Vector2 randVer = Main.rand.NextVector2Unit() * Main.rand.Next(16, 18);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center + randVer * 10f
                    , -randVer, ModContent.ProjectileType<NeutronLaser>(), Projectile.damage, 0f);
            }
        }
        /// <summary>本体不走常规绘制，改由 IDrawWarp 的 costomDraw 画</summary>
        public override bool PreDraw(ref Color lightColor) => false;
        // ── 公开方法 ──
        /// <summary>IDrawWarp：允许 EffectsSystem 在扭曲结果之上再调用 costomDraw 绘制本体</summary>
        public bool canDraw() => true;
        /// <summary>
        /// IDrawWarp：把等离子长矛贴图以自身中心为原点、按速度朝向画一张（A 通道归零，走加色观感）
        /// </summary>
        public void costomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null
                , Color.White with { A = 0 }, Projectile.rotation, texture.Size() / 2
                , Projectile.scale, SpriteEffects.None, 0f);
        }
        /// <summary>
        /// IDrawWarp：绘制屏幕扭曲遮罩。用"引力透镜"技法一次性生成位移场
        /// （CWR 口径：80×80 的场、强度 0.3、进度恒 1、UV 半径 0.4），取代旧版叠 3 层遮罩贴图的写法
        /// </summary>
        public void Warp() => NeutronWarpHelper.DrawWarp(Projectile.Center
            , screenWidth: 80f, screenHeight: 80f, intensity: 0.3f, progress: 1f
            , rotation: Projectile.rotation, technique: "GravitationalLens", radius: 0.4f);
    }
}
