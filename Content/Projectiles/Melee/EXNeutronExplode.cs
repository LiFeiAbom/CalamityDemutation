using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 中子大爆点（EXNeutronExplode） - 中子长戟右键手持弹幕（NeutronGlaiveHeld）充能满后触发的大范围爆点。
    /// 与 NeutronExplode（小爆点 200×200、33 层扭曲）不同，本弹幕 2000×2000、133 层扭曲，是右键大招的收尾爆发；
    /// 通过 IDrawWarp 参与屏幕扭曲，不移动、不常规绘制。
    /// CWR 原版用 EndlessDamageClass（固定伤害不吃加成），本模组不设 DamageType（沿用 Generic，与 NeutronExplode 一致）。
    /// </summary>
    internal class EXNeutronExplode : ModProjectile, IDrawWarp
    {
        /// <summary>
        /// 贴图取遮罩资源 DiffusionCircle（CalamityDemutation/Assets/Masking/DiffusionCircle.png），同时用作屏幕扭曲遮罩
        /// </summary>
        public override string Texture => CalamityDemutationConstant.Masking + "DiffusionCircle";
        /// <summary>
        /// 弹幕基础属性：2000×2000 大范围判定框、存活 20 帧、自定义 AI
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2000;  // 判定框边长（像素）
            Projectile.timeLeft = 20;                    // 存活 20 帧
            Projectile.aiStyle = -1;                     // -1 = 不使用原版 AI 模板
            Projectile.localNPCHitCooldown = 4;          // 同一敌人 4 帧内只受击一次
            Projectile.penetrate = -1;                   // -1 = 无限穿透
            Projectile.friendly = true;                  // 友方弹幕
            Projectile.netImportant = true;              // 联机中保证同步创建/销毁
            Projectile.tileCollide = false;              // 不与物块碰撞
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算免疫计时
        }
        /// <summary>
        /// 返回 false：本体不参与位置更新，爆炸固定在生成点
        /// </summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 视觉与生命周期逻辑：
        /// ai[2] 首帧喷 4×133 枚十字形火花并播放爆发音；之后每 6 帧在四个象限随机偏移处再喷火花；
        /// ai[0]/localAI[0]/ai[1] 分别驱动扭曲遮罩的旋转、缩放与不透明度
        /// </summary>
        public override void AI()
        {
            if (Projectile.ai[2] == 0)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.1f, Volume = 0.8f }, Projectile.Center);   // 爆发音效（原版爆炸，替代 CWR 的 Pecharge）
                for (int i = 0; i < 4; i++)
                {
                    float rot1 = MathHelper.PiOver2 * i;
                    Vector2 vr = rot1.ToRotationVector2();
                    for (int j = 0; j < 133; j++)
                    {
                        BaseParticle spark = new DRK_Spark(Projectile.Center, vr * (0.1f + j * 0.34f), false, 7, Main.rand.NextFloat(2.2f, 2.3f), Color.BlueViolet);
                        DRKLoader.AddParticle(spark);
                    }
                }
            }
            if (Projectile.ai[2] % 6 == 0)
            {
                float randvalue = Main.rand.NextFloat(MathHelper.TwoPi);
                float randvalue2 = Main.rand.NextFloat(0.3f, 1.6f);
                for (int z = 0; z < 4; z++)
                {
                    Vector2 rand = (MathHelper.PiOver2 * z + randvalue).ToRotationVector2() * 130 * randvalue2;
                    for (int i = 0; i < 4; i++)
                    {
                        float rot1 = MathHelper.PiOver2 * i;
                        Vector2 vr = rot1.ToRotationVector2();
                        for (int j = 0; j < 33; j++)
                        {
                            BaseParticle spark = new DRK_Spark(Projectile.Center + rand, vr * 0.24f, false, 13, Main.rand.NextFloat(0.9f, 1.3f), Color.CadetBlue);
                            DRKLoader.AddParticle(spark);
                        }
                    }
                }
            }
            Projectile.ai[0] += 0.25f;   // 扭曲遮罩累计旋转角
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
            Projectile.localAI[1] += 0.07f;
            Projectile.ai[1] = Math.Clamp(Projectile.ai[1], 0f, 1f);   // 不透明度钳制在 0~1
            Projectile.ai[2]++;
            Lighting.AddLight(Projectile.Center, new Vector3(1, 1, 1));   // 纯白强光
        }
        /// <summary>
        /// 返回 false：禁用常规贴图绘制，外观完全交给 IDrawWarp 的 Warp 扭曲管线
        /// </summary>
        public override bool PreDraw(ref Color lightColor) => false;
        /// <summary>
        /// 是否额外执行 costomDraw 绘制：本弹幕不需要，只走 Warp 扭曲
        /// </summary>
        public bool canDraw() => false;
        /// <summary>
        /// 空实现：canDraw() 返回 false，本弹幕不做额外自定义绘制
        /// </summary>
        public void costomDraw(SpriteBatch spriteBatch) { }
        /// <summary>
        /// 绘制屏幕扭曲遮罩：把 DiffusionCircle 贴图旋转着叠加 133 次，形成大范围同心扭曲场
        /// </summary>
        public void Warp()
        {
            Texture2D warpTex = TextureAssets.Projectile[Type].Value;
            Color warpColor = new Color(45, 45, 45) * Projectile.ai[1];
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 drawOrig = warpTex.Size() / 2;
            for (int i = 0; i < 133; i++)
            {
                Main.spriteBatch.Draw(warpTex, drawPos, null, warpColor, Projectile.ai[0] + i * 115f, drawOrig, Projectile.localAI[0] + i * 0.015f, SpriteEffects.None, 0f);
            }
        }
    }
}
