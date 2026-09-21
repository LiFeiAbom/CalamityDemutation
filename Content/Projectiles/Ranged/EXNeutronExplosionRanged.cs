using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 中子枪大爆点（EXNeutronExplosionRanged） - 中子枪右键蓄力满 80 时在鼠标位置炸开的大爆点，
    /// 移植自 CWR 的 EXNeutronExplosionRanged。
    /// 与随命中触发的小爆点 <see cref="NeutronExplosionRanged"/> 相比，判定框 200×200、
    /// 存活 30 帧、粒子数翻四倍、扭曲遮罩叠 5 层，并且带 80 点护甲穿透。
    /// 不移动、不做常规贴图绘制，全程走 IDrawWarp 的屏幕扭曲管线。
    /// </summary>
    internal class EXNeutronExplosionRanged : ModProjectile, IDrawWarp
    {
        /// <summary>贴图取星形遮罩，同时用作屏幕扭曲的遮罩</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/StarTexture";
        /// <summary>
        /// 基础属性：200×200 判定框、存活 30 帧、自定义 AI、同一敌人 1 帧冷却、无限穿透、80 点护甲穿透
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 200;
            Projectile.timeLeft = 30;
            Projectile.aiStyle = -1;
            Projectile.localNPCHitCooldown = 1;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ArmorPenetration = 80;
        }
        /// <summary>返回 false：本体不参与位置更新，爆点固定在生成点</summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 视觉与生命周期：ai[2] 作一次性开关，首帧向四个正交方向各喷 133 颗火花（速度逐颗递增，形成拖尾）；
        /// ai[0]/localAI[0]/ai[1] 分别驱动扭曲遮罩的旋转、缩放与不透明度（前 10 帧膨胀、其后收缩淡出）
        /// </summary>
        public override void AI()
        {
            if (Projectile.ai[2] == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    float rot1 = MathHelper.PiOver2 * i;
                    Vector2 vr = rot1.ToRotationVector2();
                    for (int j = 0; j < 133; j++)
                    {
                        BaseParticle spark = new DRK_Spark(Projectile.Center
                            , vr * (0.1f + j * 0.24f), false, 30, Main.rand.NextFloat(1.2f, 2.3f), Color.BlueViolet);
                        DRKLoader.AddParticle(spark);
                    }
                }
                Projectile.ai[2]++;
            }
            Projectile.ai[0] += 0.25f;
            if (Projectile.timeLeft > 20)
            {
                Projectile.localAI[0] += 0.25f;
                Projectile.ai[1] += 0.2f;
            }
            else
            {
                Projectile.localAI[0] -= 0.13f;
                Projectile.ai[1] -= 0.066f;
            }
            Projectile.localAI[1] += 0.07f;                          // 通用计时器，本弹幕未读取
            Projectile.ai[1] = Math.Clamp(Projectile.ai[1], 0f, 1f);
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 1f, 1f));
        }
        /// <summary>返回 false：禁用常规贴图绘制，外观完全交给扭曲管线</summary>
        public override bool PreDraw(ref Color lightColor) => false;
        // ── 公开方法 ──
        /// <summary>IDrawWarp：本弹幕不做扭曲结果之上的额外绘制</summary>
        public bool canDraw() => false;
        /// <summary>IDrawWarp：空实现，配合 canDraw() 返回 false</summary>
        public void costomDraw(SpriteBatch spriteBatch) { }
        /// <summary>
        /// IDrawWarp：绘制扭曲遮罩。把星形遮罩叠画 5 层（每层不额外旋转），
        /// 缩放取 localAI[0]、透明度取 ai[1]，层数比小爆点多两层，扭曲范围更厚
        /// </summary>
        public void Warp()
        {
            Texture2D warpTex = TextureAssets.Projectile[Type].Value;
            Color warpColor = new Color(45, 45, 45) * Projectile.ai[1];
            for (int i = 0; i < 5; i++)
            {
                Main.spriteBatch.Draw(warpTex, Projectile.Center - Main.screenPosition
                    , null, warpColor, 0f, warpTex.Size() / 2, Projectile.localAI[0], SpriteEffects.None, 0f);
            }
        }
    }
}
