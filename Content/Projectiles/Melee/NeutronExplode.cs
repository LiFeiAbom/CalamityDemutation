using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 中子爆裂（NeutronExplode） - 中子长戟弹幕（NeutronGlaiveBeam）消亡时触发的原地爆点。
    /// 由 NeutronGlaiveBeam.OnKill 在自身中心生成，不移动、不做常规贴图绘制，
    /// 而是通过 IDrawWarp 接口参与屏幕扭曲管线，用一个旋转的遮罩圆造成空间扭曲爆炸。
    /// </summary>
    internal class NeutronExplode : ModProjectile, IDrawWarp
    {
        // ── 属性 ──
        /// <summary>
        /// 贴图取遮罩资源 DiffusionCircle（CalamityDemutation/Assets/Masking/DiffusionCircle.png），
        /// 该贴图同时用作屏幕扭曲的遮罩。
        /// </summary>
        public override string Texture => CalamityDemutationConstant.Masking + "DiffusionCircle";
        // ── 生命周期方法 ──
        /// <summary>
        /// 弹幕基础属性：200×200 的大范围判定框、存活 20 帧、自定义 AI（aiStyle=-1）。
        /// 未显式设置 DamageType，沿用弹幕默认伤害类型。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 200;  // 判定框边长（像素）
            Projectile.timeLeft = 20;                    // 存活 20 帧
            Projectile.aiStyle = -1;                     // -1 = 不使用原版 AI 模板，走自定义 AI()
            Projectile.localNPCHitCooldown = 4;          // 同一敌人 4 帧内只受击一次
            Projectile.penetrate = -1;                   // -1 = 无限穿透
            Projectile.friendly = true;                  // 友方弹幕，只伤害敌怪
            Projectile.netImportant = true;              // 联机中保证同步创建/销毁
            Projectile.tileCollide = false;              // 不与物块碰撞
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算免疫计时
        }
        /// <summary>
        /// 返回 false：本体不参与位置更新，爆炸固定在生成点。
        /// </summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 视觉与生命周期逻辑：
        /// ai[2] 为粒子生成的一次性开关（首帧向四个方向喷出大量火花粒子）；
        /// ai[0]/localAI[0]/ai[1] 分别驱动扭曲遮罩的旋转、缩放与不透明度；localAI[1] 本文件未使用。
        /// </summary>
        public override void AI()
        {
            // ai[2] 作一次性开关：仅在诞生首帧喷出粒子
            if (Projectile.ai[2] == 0)
            {
                // 沿四个正交方向（0/90/180/270 度）各喷 133 枚火花，共 532 枚，构成十字形爆发
                for (int i = 0; i < 4; i++)
                {
                    float rot1 = MathHelper.PiOver2 * i;   // 每次递增 90 度
                    Vector2 vr = rot1.ToRotationVector2();
                    for (int j = 0; j < 133; j++)
                    {
                        // DRK_Spark：相对位置、速度、是否受重力(false)、寿命 7、随机缩放、颜色
                        BaseParticle spark = new DRK_Spark(Projectile.Center
                            , vr * (0.1f + i * 0.24f), false, 7, Main.rand.NextFloat(1.2f, 2.3f), Color.BlueViolet);
                        DRKLoader.AddParticle(spark);
                    }
                }
                Projectile.ai[2]++;
            }
            Projectile.ai[0] += 0.25f;   // 扭曲遮罩的累计旋转角
            // 前 5 帧（timeLeft 20→15）为膨胀期：遮罩放大、不透明度上升；之后进入收缩消散期
            if (Projectile.timeLeft > 15)
            {
                Projectile.localAI[0] += 0.25f;   // 遮罩缩放增长
                Projectile.ai[1] += 0.2f;         // 不透明度增长
            }
            else
            {
                Projectile.localAI[0] -= 0.13f;   // 遮罩缩放收缩
                Projectile.ai[1] -= 0.066f;       // 不透明度衰减
            }
            Projectile.localAI[1] += 0.07f;   // 本弹幕未使用该值（保留的通用计时器）
            Projectile.ai[1] = Math.Clamp(Projectile.ai[1], 0f, 1f);   // 不透明度钳制在 0~1
            Lighting.AddLight(Projectile.Center, new Vector3(1, 1, 1));   // 纯白强光
        }
        /// <summary>
        /// 命中敌怪：附加虚空侵蚀（VoidErosion）减益 1200 tick（20 秒）。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<VoidErosion>(), 1200);
        }
        /// <summary>
        /// 命中玩家（PvP）：附加虚空侵蚀（VoidErosion）减益 1200 tick（20 秒），与 OnHitNPC 对称。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<VoidErosion>(), 1200);
        }
        /// <summary>
        /// 返回 false：禁用常规贴图绘制，外观完全交给 IDrawWarp 的 Warp 扭曲管线。
        /// </summary>
        public override bool PreDraw(ref Color lightColor) => false;
        // ── 公开方法 ──
        /// <summary>
        /// 是否额外执行 costomDraw 绘制：本弹幕不需要，只走 Warp 扭曲。
        /// </summary>
        public bool canDraw() => false;
        /// <summary>
        /// 空实现：canDraw() 返回 false，本弹幕不做额外自定义绘制。
        /// </summary>
        public void costomDraw(SpriteBatch spriteBatch) { }
        /// <summary>
        /// 绘制屏幕扭曲遮罩：把 DiffusionCircle 贴图旋转着叠加 33 次，
        /// 形成同心圆环状的扭曲场，不透明度由 ai[1] 控制，膨胀期越画越大、消散期逐渐透明。
        /// </summary>
        public void Warp()
        {
            Texture2D warpTex = TextureAssets.Projectile[Type].Value;   // 取本弹幕贴图（即 DiffusionCircle 遮罩）
            Color warpColor = new Color(45, 45, 45) * Projectile.ai[1]; // 深灰底色 × 不透明度
            for (int i = 0; i < 33; i++)
            {
                // 每层额外旋转 i*2 弧度并统一缩放 localAI[0]，堆叠出多层扭曲
                Main.spriteBatch.Draw(warpTex, Projectile.Center - Main.screenPosition
                    , null, warpColor, Projectile.ai[0] + i * 2f, warpTex.Size() / 2, Projectile.localAI[0], SpriteEffects.None, 0f);
            }
        }
    }
}
