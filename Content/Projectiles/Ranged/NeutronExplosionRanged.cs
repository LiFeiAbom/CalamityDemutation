using CalamityDemutation.Common.Effects;
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
    /// 中子弹小爆点（NeutronExplosionRanged） - 中子弹（<see cref="NeutronBullet"/>）命中敌怪时在原地触发的爆点，
    /// 移植自 CWR 的 NeutronExplosionRanged（第 0.5.0.1.7 版口径）。
    /// 不移动、不做常规贴图绘制，靠 IDrawWarp 参与屏幕扭曲管线，用一个缓慢旋转放大的遮罩圆造成空间扭曲。
    /// <para>
    /// 与近战侧既有的 <c>NeutronExplode</c> 并存而非合并：后者移植自 CWR 0.4.0.3.5（200×200、33 层扭曲），
    /// 本类来自 0.5.0.1.7（100×100、3 层扭曲），CWR 自身也是近战/远程各留一套。
    /// </para>
    /// </summary>
    internal class NeutronExplosionRanged : ModProjectile, IDrawWarp
    {
        /// <summary>贴图取星形遮罩，同时用作屏幕扭曲的遮罩</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/StarTexture";
        /// <summary>
        /// 基础属性：100×100 判定框、存活 20 帧、自定义 AI、同一敌人 6 帧内只受击一次、无限穿透
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 100;
            Projectile.timeLeft = 20;
            Projectile.aiStyle = -1;
            Projectile.localNPCHitCooldown = 6;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
        }
        /// <summary>返回 false：本体不参与位置更新，爆点固定在生成点</summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 视觉与生命周期：ai[2] 作一次性开关，首帧向四个正交方向各喷 33 颗火花；
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
                    for (int j = 0; j < 33; j++)
                    {
                        BaseParticle spark = new DRK_Spark(Projectile.Center
                            , vr * 0.24f, false, 30, Main.rand.NextFloat(1.2f, 2.3f), Color.CadetBlue);
                        DRKLoader.AddParticle(spark);
                    }
                }
                Projectile.ai[2]++;
            }
            Projectile.ai[0] += 0.15f;
            if (Projectile.timeLeft > 10)
            {
                Projectile.localAI[0] += 0.06f;
                Projectile.ai[1] += 0.1f;
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
        /// IDrawWarp：绘制扭曲遮罩。用"冲击波环"技法一次性生成位移场——
        /// 场尺寸 300×300 并按 localAI[0] 缩放、强度取 ai[1]×0.65、进度与旋转取 ai[1]/ai[0]。
        /// 取代旧版把遮罩贴图叠 33 层的 CPU 写法（CWR 原文注释即写作"NeutronWarp 替 CPU 叠绘"）
        /// </summary>
        public void Warp()
        {
            float scale = Math.Max(Projectile.localAI[0], 0.01f);
            NeutronWarpHelper.DrawWarp(Projectile.Center
                , screenWidth: 300f * scale, screenHeight: 300f * scale
                , intensity: Projectile.ai[1] * 0.65f, progress: Projectile.ai[1]
                , rotation: Projectile.ai[0], technique: "ShockwaveRing");
        }
    }
}
