using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 精魂烈焰（源内部名 <c>EssenceFlame2</c>）—— 暴政挥砍时在鼠标处迸发的追踪火焰，
    /// 整份照搬灾厄 2.0.3.9 的 <c>Projectiles/Melee/EssenceFlame2.cs</c>
    /// （贴图取自同版本 <c>Projectiles/Healing/EssenceFlame.png</c>，源也是这么复用的）。
    /// <para>
    /// 行为：初始完全透明，每帧 -5 alpha 淡入；4 帧循环动画（每 17 次更新推进一帧）；
    /// 放出约 7.5 帧后开始朝 760 像素内最近的敌人追踪（追踪速度 10、惯性 20，由本工程
    /// <c>CDUtil.HomeInOnNPC</c> 实现，等价于源调用的 <c>CalamityUtils.HomeInOnNPC</c>）；
    /// 只穿透 1 层、不碰墙；消亡时播放 Item74 并炸出暗影束尘（<c>DustID.ShadowbeamStaff</c>，即源里写的 173 号尘）。
    /// </para>
    /// <para>
    /// 联机：追踪与尘都在两端各跑一份，属于纯表现；伤害由生成包带过去，无需额外发包。
    /// </para>
    /// </summary>
    internal class EssenceFlame2 : ModProjectile
    {
        /// <summary>4 帧循环动画（源同）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        /// <summary>
        /// 基础属性：20×20 判定框、友方近战、穿透 1、不碰墙、初始全透明、
        /// 寿命 180（配 <c>extraUpdates = 3</c> 约合 45 帧 ≈ 0.75 秒）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 180;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 3;
        }
        /// <summary>淡入完成（timeLeft 掉到 150 以下）后才开始判定，且只打可被追踪的目标——源同</summary>
        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft < 150 && target.CanBeChasedBy(Projectile);
        /// <summary>淡入 + 4 帧动画；淡入结束后朝 760 像素内最近的敌人平滑追踪</summary>
        public override void AI()
        {
            Projectile.alpha -= 5;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 16)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            if (Projectile.timeLeft < 150)
                CDUtil.HomeInOnNPC(Projectile, true, 760f, 10f, 20f);
        }
        /// <summary>消亡表现：先播放 Item74，再把判定框吹到 50×50 后炸两圈暗影束尘（源原样，源写的 173 号）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            for (int i = 0; i < 5; i++)
            {
                int flameDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 2f);
                Main.dust[flameDust].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[flameDust].scale = 0.5f;
                    Main.dust[flameDust].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                }
            }
            for (int j = 0; j < 8; j++)
            {
                int flameDust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 3f);
                Main.dust[flameDust2].noGravity = true;
                Main.dust[flameDust2].velocity *= 5f;
                flameDust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 2f);
                Main.dust[flameDust2].velocity *= 2f;
            }
        }
    }
}
