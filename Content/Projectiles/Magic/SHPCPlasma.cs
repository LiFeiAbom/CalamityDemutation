using System;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// SHPC 的等离子球（源：本体 2.0 <c>Projectiles/Magic/SHPB</c>）——左键打出、停在空中、引发大范围爆炸。
    /// <para>
    /// 观感：4 帧循环的等离子团（贴图 <c>SHPCPlasma.png</c>，50×192 竖排 4 帧），出场从全透明淡入并做
    /// 0.8 ↔ 1.2 的缩放脉动；速度每帧 ×0.985 迅速停滞。
    /// **被追踪距离（250 像素）内出现可追敌时**启动 120 帧倒计时，倒计时走完自行引爆（照源，用于"贴脸命中即爆"的手感）。
    /// 死亡时播原版 <c>Item105</c> 并在主人端生成 <see cref="SHPCExplosion"/>，伤害与击退照抄本弹幕。
    /// </para>
    /// <para>
    /// **追踪分支（SHPC 右键每 60 帧附带的那颗"相位球"）**：由 <c>ai[2] == 1</c> 打开，每帧调
    /// <see cref="CDUtil.HomeInOnNPC"/>（参数照源 CI：范围 1800 / 追踪速度 24 / 惯性 20）。
    /// 源里这颗球的追踪判定是**死代码**（同一段 AI 开头把 <c>ai[0]</c> 覆盖成随机数了），本工程改用
    /// <c>ai[2]</c> 这个源没碰过的槽位，不影响 <c>ai[0]/ai[1]</c> 原有的随机相位。
    /// </para>
    /// </summary>
    internal class SHPCPlasma : ModProjectile
    {
        /// <summary>邻近有可追敌时的引爆倒计时上限（照源 120 帧）</summary>
        private const int NearEnemyExplosionDelay = 120;
        /// <summary>判定"附近有敌人"的距离（照源 250 像素）</summary>
        private const float NearEnemyCheckDistance = 250f;
        /// <summary>4 帧动画（照源）</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = 4;
        /// <summary>追踪范围：照源 CI 那颗额外球的 <c>HomeInOnNPC(proj, true, 1800f, 24f, 20f)</c></summary>
        private const float HomingRange = 1800f;
        /// <summary>追踪速度（照源 24）</summary>
        private const float HomingVelocity = 24f;
        /// <summary>追踪惯性（照源 20，越大转向越柔和）</summary>
        private const float HomingInertia = 20f;
        /// <summary>判定 24×24、穿透 1、寿命 300 帧、法师伤害、出场全透明（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            // 邻近有可追敌时的引爆倒计时**初值**：源用类的实例字段 `explosionTimer = 120`，
            // 本工程改用 localAI[1]（弹幕实例字段），必须在这里给初值——否则默认 0，敌人一进 250 像素就立刻爆炸。
            Projectile.localAI[1] = NearEnemyExplosionDelay;
        }
        /// <summary>淡入、帧循环、缩放脉动、减速停滞、邻敌倒计时（照源，一行不动）</summary>
        public override void AI()
        {
            float lightScale = Main.rand.Next(90, 111) * 0.01f;
            lightScale *= Main.essScale;
            Lighting.AddLight(Projectile.Center, 1f * lightScale, 0.2f * lightScale, 0.75f * lightScale);
            Projectile.alpha -= 2;
            // 4 帧循环
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            // 源把 ai[0] / ai[1] 当"随机相位"用（后续没有任何读取点）
            Projectile.ai[0] = Main.rand.Next(-100, 101) * 0.0025f;
            Projectile.ai[1] = Main.rand.Next(-100, 101) * 0.0025f;
            // 缩放脉动：0.8 → 1.2 → 0.8
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale += 0.05f;
                if (Projectile.scale > 1.2)
                    Projectile.localAI[0] = 1f;
            }
            else
            {
                Projectile.scale -= 0.05f;
                if (Projectile.scale < 0.8)
                    Projectile.localAI[0] = 0f;
            }
            // 迅速停滞
            Projectile.velocity.X *= 0.985f;
            Projectile.velocity.Y *= 0.985f;
            // 附近有可追敌 → 启动倒计时（照源：不锁定具体哪个敌人）
            bool enemyNearby = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(Projectile, false) && Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                {
                    float distance = Math.Abs(Projectile.Center.X - npc.Center.X) + Math.Abs(Projectile.Center.Y - npc.Center.Y);
                    if (distance < NearEnemyCheckDistance)
                    {
                        enemyNearby = true;
                        break;
                    }
                }
            }
            if (enemyNearby)
            {
                Projectile.localAI[1]--;
                if (Projectile.localAI[1] <= 0f)
                    Projectile.Kill();
            }
            // ai[2] == 1：追踪型相位球（SHPC 右键周期附带的那颗），每帧朝最近的敌人转向
            if (Projectile.ai[2] == 1f)
                CDUtil.HomeInOnNPC(Projectile, true, HomingRange, HomingVelocity, HomingInertia);
        }
        /// <summary>彩虹色（照源：R 255 / G 随 Disco 走动 / B 155）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(255, Main.DiscoG, 155, Projectile.alpha);
        /// <summary>手绘 4 帧（照源，绕开引擎的帧表）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            int frameY = frameHeight * Projectile.frame;
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                new Rectangle?(new Rectangle(0, frameY, texture.Width, frameHeight)), Projectile.GetAlpha(lightColor),
                Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>死亡／引爆：原版 Item105 音 + 主人端生成 <see cref="SHPCExplosion"/>（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item105, Projectile.position);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<SHPCExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
            }
        }
    }
}
