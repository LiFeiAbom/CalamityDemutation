using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 中子长矛光束（NeutronGlaiveBeam） - 近战武器「中子长矛」（NeutronGlaive）的副手射出弹幕。
    /// 玩家挥矛时由武器 Item.shoot 以 shootSpeed 18 发射，伤害类型为近战。
    /// 弹幕本体不在普通管线绘制（PreDraw 恒返回 false），而是实现 IDrawWarp 接口：
    /// 由 Common/Effects/EffectsSystem 收集，用 Warp() 画扭曲遮罩驱动屏幕扭曲，
    /// 再用 costomDraw() 把本体画在扭曲结果之上。
    /// 消亡时调用 CDUtil.Explode 做一次 300 半径范围伤害，并另生成 NeutronExplode 爆炸视觉弹幕。
    /// </summary>
    internal class NeutronGlaiveBeam : ModProjectile, IDrawWarp
    {
        internal static Asset<Texture2D> warpTex;   // 扭曲遮罩贴图缓存（卸载时由 CalamityDemutation 置 null）
        /// <summary>
        /// IDrawWarp：允许 EffectsSystem 在扭曲结果之上再调用 costomDraw 绘制本体
        /// </summary>
        public bool canDraw() => true;
        /// <summary>
        /// 弹幕基础属性：32×32 判定箱、近战伤害、高速（MaxUpdates 3）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;   // 正方形判定箱
            Projectile.friendly = true;                  // 友方弹幕
            Projectile.DamageType = DamageClass.Melee;   // 吃近战伤害加成
            Projectile.timeLeft = 120;                   // 存活 120 帧
            Projectile.MaxUpdates = 3;                   // 每帧额外更新 3 次，飞行更快且判定更密
        }
        /// <summary>
        /// 主 AI：切换 6 帧动画、打白光、按速度旋转（+45° 校正斜向贴图），
        /// 并驱动三个扭曲参数（ai[0] 旋转、localAI[0] 缩放、ai[1] 不透明度）在末段淡出；
        /// 同时喷 Granite 尘埃环绕与四向 DRK_Spark 十字粒子。
        /// </summary>
        public override void AI()
        {
            CDUtil.ClockFrame(ref Projectile.frame, 5, 5);   // 每 5 帧切一帧，贴图共 6 帧（0~5）
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.3f);   // 白光 0.3
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;   // 贴图斜向，+45° 校正
            // ai[0]：扭曲遮罩的旋转量，从 0 渐增并钳制到 0.3
            Projectile.ai[0] += 0.05f;
            if (Projectile.ai[0] > 0.3f)
            {
                Projectile.ai[0] = 0.3f;
            }
            // 存活前 105 帧：扭曲渐强（localAI[0] 缩放→0.3、ai[1] 不透明度→0.3）
            if (Projectile.timeLeft > 15)
            {
                Projectile.localAI[0] += 0.15f;
                if (Projectile.localAI[0] > 0.3f)
                {
                    Projectile.localAI[0] = 0.3f;
                }
                Projectile.ai[1] += 0.2f;
                if (Projectile.ai[1] > 0.3f)
                {
                    Projectile.ai[1] = 0.3f;
                }
            }
            else
            {
                // 最后 15 帧：扭曲缩放与不透明度反向衰减，扭曲效果淡出
                Projectile.localAI[0] -= 0.03f;
                Projectile.ai[1] -= 0.066f;
            }
            Projectile.localAI[1] += 0.07f;   // 每帧累积（本文件内未再读取，未参与绘制/判定）
            // 每帧 2 颗 Granite 尘埃：环绕中心随机角度、沿切线方向外散
            float rot = Main.rand.NextFloat(6.282f);
            for (int i = 0; i < 2; i++)
            {
                Vector2 dir = rot.ToRotationVector2();
                Vector2 vel = dir.RotatedBy(1.57f) * Main.rand.NextFloat(1.3f, 2.5f) + Projectile.velocity;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dir * Main.rand.Next(3, 10)
                    , DustID.Granite, vel, Scale: Main.rand.NextFloat(1.4f, 1.6f));
                dust.noGravity = true;
                rot = Main.rand.NextFloat(MathHelper.TwoPi);
            }
            // 每 3 帧（localAI[2] 计数）在上下左右四个方向各生成 3 颗蓝紫 DRK_Spark 十字粒子
            if (++Projectile.localAI[2] > 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float rot1 = MathHelper.PiOver2 * i;
                    Vector2 vr = rot1.ToRotationVector2();
                    for (int j = 0; j < 3; j++)
                    {
                        BaseParticle spark = new DRK_Spark(Projectile.Center
                            , vr * (0.1f + i * 0.14f), false, 17, Main.rand.NextFloat(0.2f, 0.3f), Color.BlueViolet);
                        DRKLoader.AddParticle(spark);
                    }
                }
                Projectile.localAI[2] = 0;
            }
        }
        /// <summary>
        /// 消亡：先以 300 半径做一次范围伤害（CDUtil.Explode，音效 Item14 升调 0.45），
        /// 再把中心随机偏移一下，另生成 NeutronExplode 继承伤害，负责爆炸的扭曲视觉。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.Explode(300, SoundID.Item14 with { Pitch = 0.45f });   // 扩大判定箱结算一次范围伤害
            Projectile.Center += CDUtil.randVr(64);   // 爆炸视觉位置随机偏移
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero
                , ModContent.ProjectileType<NeutronExplode>(), Projectile.damage, 0);
        }
        /// <summary>
        /// 撞墙：以旧速度的 -0.6 倍反弹（不减速回弹），随机方向喷 73 颗浅蓝 DRK_Spark；
        /// 返回 false 表示不销毁弹幕，让它继续弹跳飞行。
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity * -0.6f;   // 反向并衰减为 60%，形成弹跳
            for (int j = 0; j < 73; j++)
            {
                BaseParticle spark = new DRK_Spark(Projectile.Center + oldVelocity
                    , oldVelocity.RotatedByRandom(0.3f) * -Main.rand.NextFloat(0.3f, 1.1f)
                    , false, 7, Main.rand.NextFloat(0.5f, 0.7f), Color.LightBlue);
                DRKLoader.AddParticle(spark);
            }
            return false;
        }
        /// <summary>
        /// 本体不走普通绘制管线：改由 IDrawWarp 的 costomDraw 画在屏幕扭曲结果之上
        /// </summary>
        public override bool PreDraw(ref Color lightColor) => false;
        /// <summary>
        /// IDrawWarp：绘制屏幕扭曲遮罩。用 Masking/DiffusionCircle 贴图叠画 3 层，
        /// 每层旋转角为 ai[0] + i×2、缩放取 localAI[0]、整体由 ai[1] 控制透明度，
        /// 由 EffectsSystem 经 WarpShader 应用到整屏。
        /// </summary>
        public void Warp()
        {
            warpTex ??= ModContent.Request<Texture2D>(CalamityDemutationConstant.Masking + "DiffusionCircle");   // 首次使用时懒加载并缓存
            Color warpColor = new Color(45, 45, 45) * Projectile.ai[1];   // 灰色遮罩，灰度由 ai[1] 淡出
            Vector2 orig = warpTex.Value.Size() / 2;
            for (int i = 0; i < 3; i++)
            {
                Main.spriteBatch.Draw(warpTex.Value, Projectile.Center - Main.screenPosition
                    , null, warpColor, Projectile.ai[0] + i * 2f, orig, Projectile.localAI[0], SpriteEffects.None, 0f);
            }
        }
        /// <summary>
        /// IDrawWarp：把弹幕本体画在扭曲结果之上；按 Projectile.frame 从 6 帧贴图取帧，
        /// 用 CDUtil.GetRec/GetOrig 计算切片矩形与旋转中心，纯白描边不受光照影响。
        /// </summary>
        public void costomDraw(SpriteBatch spriteBatch)
        {
            Texture2D value = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(value, Projectile.Center - Main.screenPosition, CDUtil.GetRec(value, Projectile.frame, 6)
                , Color.White, Projectile.rotation, CDUtil.GetOrig(value, 6), Projectile.scale, SpriteEffects.None, 0);
        }
    }
}
