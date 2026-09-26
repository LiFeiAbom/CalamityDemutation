using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 星流射线（Exobeamold）—— 星流之刃挥砍时射出的青色光束（照搬 CI 的 <c>Exobeamold</c>，即改版前旧 Exobeam 的复刻）。
    /// 前 90 帧直飞、随后 60 帧进入第二段，然后掉头追踪"最近的玩家"
    /// （<c>Player.FindClosest</c> 返回的是玩家索引而不是 NPC，原码如此，不是笔误）；
    /// 距目标 30 像素内自爆，自爆时判定框撑到 192×192 再补一次伤害判定，播放爆裂音并迸发大量青色尘土。
    /// 命中敌人会挂上整套星云系减益（<see cref="CDUtil.ExoDebuffs"/>），并随机三选一撒出一颗彗星（<see cref="ExoComet"/>）。
    /// 本体是 CI 的 <c>Exobeamold</c>，命中效果取自 CI 的 <c>ExobeamoldExoLore</c>（本工程恒处 Lore 模式，故不留两份几乎相同的类）。
    /// </summary>
    internal class Exobeamold : ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>绕光束喷尘的节拍计数：每累计到 12 触发一次，随即归零</summary>
        private int counter;
        // ── 生命周期方法 ──
        /// <summary>残影缓存 10 点、TrailingMode 0（只记录位置）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        /// <summary>基础属性：16×16、友方近战、穿透 1、每帧更新 2 格（extraUpdates 1）、初始全透明、存活 600 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 600;
            Projectile.light = 1f;
        }
        /// <summary>出场音效、每 12 帧绕光束喷一圈青色尘、淡入、三段式飞行状态机、按速度方向摆正旋转角</summary>
        public override void AI()
        {
            if (Projectile.localAI[1] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item60, Projectile.position);   // localAI[1] 当"已出声"标记，保证只播一次
                Projectile.localAI[1] += 1f;
            }
            counter++;
            if (counter == 12)
            {
                counter = 0;
                // 绕光束中心画一圈 12 颗尘，尘初始沿光束中轴线排布再按旋转角整体转正
                for (int l = 0; l < 12; l++)
                {
                    Vector2 vector3 = Vector2.UnitX * -(float)Projectile.width / 2f;
                    vector3 += -Vector2.UnitY.RotatedBy(l * MathHelper.Pi / 6f) * new Vector2(8f, 16f);
                    vector3 = vector3.RotatedBy(Projectile.rotation - MathHelper.PiOver2);
                    int num9 = Dust.NewDust(Projectile.Center, 0, 0, DustID.TerraBlade, 0f, 0f, 160, new Color(0, 255, 255), 1f);
                    Main.dust[num9].scale = 1.1f;
                    Main.dust[num9].noGravity = true;
                    Main.dust[num9].position = Projectile.Center + vector3;
                    Main.dust[num9].velocity = Projectile.velocity * 0.1f;
                    Main.dust[num9].velocity = Vector2.Normalize(Projectile.Center - Projectile.velocity * 3f - Main.dust[num9].position) * 1.25f;
                }
            }
            Projectile.alpha -= 40;   // 初始 255，每帧减 40，约 7 帧淡入到不透明
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            // ai[0] 三段状态机：0 直飞 90 帧 → 1 再飞 60 帧 → 2 掉头追最近的玩家
            if (Projectile.ai[0] == 0f)
            {
                Projectile.localAI[0] += 1f;
                if (Projectile.localAI[0] >= 90f)
                {
                    Projectile.localAI[0] = 0f;
                    Projectile.ai[0] = 1f;
                    Projectile.netUpdate = true;
                }
            }
            else if (Projectile.ai[0] == 1f)
            {
                Projectile.localAI[0] += 1f;
                if (Projectile.localAI[0] >= 60f)
                {
                    Projectile.localAI[0] = 0f;
                    Projectile.ai[0] = 2f;
                    Projectile.ai[1] = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
                    Projectile.netUpdate = true;
                }
            }
            else if (Projectile.ai[0] == 2f)
            {
                // ai[1] 存的是 Player.FindClosest 返回的玩家索引（不是 NPC 索引），故直接用 Main.player 取值
                Vector2 vector70 = Main.player[(int)Projectile.ai[1]].Center - Projectile.Center;
                if (vector70.Length() < 30f)
                {
                    Projectile.Kill();
                    return;
                }
                vector70.Normalize();
                vector70 *= 14f;   // 目标速度：朝目标 14 像素/帧
                vector70 = Vector2.Lerp(Projectile.velocity, vector70, 0.6f);
                // 转向时始终保留一点向下的初速度，避免贴身时速度归零
                if (vector70.Y < 24f)
                    vector70.Y = 24f;
                float num804 = 0.4f;   // 每帧最多 ±0.4 的加减速；越过目标速度时额外再补一次，避免在零点附近抖动
                if (Projectile.velocity.X < vector70.X)
                {
                    Projectile.velocity.X = Projectile.velocity.X + num804;
                    if (Projectile.velocity.X < 0f && vector70.X > 0f)
                        Projectile.velocity.X = Projectile.velocity.X + num804;
                }
                else if (Projectile.velocity.X > vector70.X)
                {
                    Projectile.velocity.X = Projectile.velocity.X - num804;
                    if (Projectile.velocity.X > 0f && vector70.X < 0f)
                        Projectile.velocity.X = Projectile.velocity.X - num804;
                }
                if (Projectile.velocity.Y < vector70.Y)
                {
                    Projectile.velocity.Y = Projectile.velocity.Y + num804;
                    if (Projectile.velocity.Y < 0f && vector70.Y > 0f)
                        Projectile.velocity.Y = Projectile.velocity.Y + num804;
                }
                else if (Projectile.velocity.Y > vector70.Y)
                {
                    Projectile.velocity.Y = Projectile.velocity.Y - num804;
                    if (Projectile.velocity.Y > 0f && vector70.Y < 0f)
                        Projectile.velocity.Y = Projectile.velocity.Y - num804;
                }
            }
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + 0.785f;   // +π/4 修正贴图本身的斜向朝向
        }
        /// <summary>命中敌人：撒彗星 + 挂上整套星云系减益（原码里两行取玩家/玩家的无效局部变量已略去）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            OnHitEffects(target.Center);
            target.ExoDebuffs();
        }
        // ── 私有工具 ──
        /// <summary>
        /// 命中特效（取自 CI 的 <c>ExobeamoldExoLore</c>）：在命中位置随机三选一撒出一颗彗星（伤害 = 本次光束伤害 ×0.5）。
        /// 0 = 目标两侧齐射（<see cref="CDUtil.ProjectileBarrage"/>，横向 1000~1400、纵向 80~1400）；
        /// 1 = 目标下方升起、2 = 目标上方雨落（后两者走 <see cref="CDUtil.ProjectileRain"/>：横向 ±400、纵向 800~1500；
        /// ProjectileRain 内部按 <c>y = 目标.Y - 随机量</c> 取生成点，故负值那一组的生成点在目标下方，会朝上飞）。
        /// </summary>
        private void OnHitEffects(Vector2 targetPos)
        {
            int randomChoice = Main.rand.Next(3);
            var source = Projectile.GetSource_FromThis();
            switch (randomChoice)
            {
                case 0:
                    CDUtil.ProjectileBarrage(source, Projectile.Center, targetPos, Main.rand.NextBool(), 1000f, 1400f, 80f, 1400f, 25f, ModContent.ProjectileType<ExoComet>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner);
                    break;
                case 1:
                    CDUtil.ProjectileRain(source, targetPos, 400f, 0f, -1500f, -800f, 25f, ModContent.ProjectileType<ExoComet>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner);
                    break;
                case 2:
                    CDUtil.ProjectileRain(source, targetPos, 400f, 0f, 800f, 1500f, 25f, ModContent.ProjectileType<ExoComet>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner);
                    break;
            }
        }
        /// <summary>固定青色（0,255,255）着色，透明度跟随 alpha 淡入</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(0, 255, 255, Projectile.alpha);
        /// <summary>刚出场的 5 帧不画（等 alpha 降下来），之后画青色残影拖尾</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 595)
                return false;
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
        /// <summary>消亡（自爆或到寿）：判定框撑到 192×192 再补一次近战伤害判定，播放爆裂音并迸发两轮青色尘土</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;   // 以当前中心为新框中心，把判定框放大到 192×192（自爆范围）
            Projectile.width = Projectile.height = 192;
            Projectile.position.X = Projectile.position.X - Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y - Projectile.height / 2;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Zombie103, Projectile.Center);
            for (int num193 = 0; num193 < 3; num193++)
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
            for (int num194 = 0; num194 < 30; num194++)
            {
                int num195 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 0, new Color(0, 255, 255), 2.5f);
                Main.dust[num195].noGravity = true;
                Main.dust[num195].velocity *= 3f;
                num195 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                Main.dust[num195].velocity *= 2f;
                Main.dust[num195].noGravity = true;
            }
        }
    }
}
