using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 天狼星·星爆（SiriusExplosion，移植自灾厄 2.0 的 Projectiles/Summon/SiriusExplosion）——
    /// 光束命中敌人时在原地炸出的那颗星：先旋转放大 60 帧，随后在 <c>Kill</c> 里把碰撞箱临时撑到 60×60
    /// 打一发范围伤害并施加**夜凋**；<c>ai[0]</c> 是色相（亮度档），<c>ai[1]</c> 是"从哪发弹幕炸出来的"
    /// （用来朝那发弹幕画一道连接光带）。
    /// </summary>
    /// <remarks>
    /// 贴图沿用源里的 `CalamityMod/Projectiles/StarProj`（72×72，本工程拷成本件同名贴图 `SiriusExplosion.png`，
    /// 这样软依赖下不依赖灾厄资源）。
    /// </remarks>
    internal class SiriusExplosion:ModProjectile
    {
        /// <summary>标记为召唤物射击弹幕</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性（照源 2.0）：14×14 起手碰撞箱（<c>Kill</c> 时临时放大）、初始全透明、穿透 1、
        /// 不撞地形、900 帧兜底寿命、**默认 friendly = false**（伤害只在 <c>Kill</c> 那一瞬结算）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 900;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// 星爆 AI：淡入 → 定速旋转放大到 60 帧上限（到点即 <c>Kill</c>），沿途喷四向光点与漂浮星屑，
        /// 并朝 <c>ai[1]</c> 指的那发弹幕方向持续点亮地面。
        /// </summary>
        public override void AI()
        {
            Color starColor = Main.hslToRgb(0.5f, 1f, Projectile.ai[0]);
            int sourceBeam = (int)Projectile.ai[1];
            // ai[1] 指向生成它的那发光束；那发弹幕没了就把连接标记清掉（源这里还顺带判了灾厄的 SilvaCrystal，本工程按"还活着就画"处理）
            if (sourceBeam < 0 || sourceBeam >= Main.maxProjectiles || !Main.projectile[sourceBeam].active)
            {
                Projectile.ai[1] = -1f;
            }
            else
            {
                DelegateMethods.v3_1 = starColor.ToVector3() * 0.5f;
                Utils.PlotTileLine(Projectile.Center, Main.projectile[sourceBeam].Center, 8f, DelegateMethods.CastLight);
            }
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = Main.rand.NextFloat() * 0.8f + 0.8f;   // 尺寸基准 0.8~1.6
                Projectile.direction = (Main.rand.Next(2) > 0) ? 1 : -1;
            }
            Projectile.rotation = Projectile.localAI[1] / 40f * 6.28318548f * (float)Projectile.direction;
            // 淡入：alpha 每帧 -8 直到全显
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 8;
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            if (Projectile.alpha == 0)
            {
                Lighting.AddLight(Projectile.Center, starColor.ToVector3() * 0.5f);
            }
            // 上下两向零星甩出光点
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextBool(10))
                {
                    Vector2 dir = Vector2.UnitY.RotatedBy((double)((float)i * 3.14159274f), default).RotatedBy((double)Projectile.rotation, default);
                    Dust d = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.LastPrism, 0f, 0f, 225, starColor, 1.5f)];
                    d.noGravity = true;
                    d.noLight = true;
                    d.scale = Projectile.Opacity * Projectile.localAI[0];
                    d.position = Projectile.Center;
                    d.velocity = dir * 2.5f;
                }
            }
            // 再甩一圈不随旋转的光点
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextBool(10))
                {
                    Vector2 dir = Vector2.UnitY.RotatedBy((double)((float)i * 3.14159274f), default);
                    Dust d = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.LastPrism, 0f, 0f, 225, starColor, 1.5f)];
                    d.noGravity = true;
                    d.noLight = true;
                    d.scale = Projectile.Opacity * Projectile.localAI[0];
                    d.position = Projectile.Center;
                    d.velocity = dir * 2.5f;
                }
            }
            // 周围空间里偶尔升起一颗缓慢上飘的星屑（避开实体方块）
            if (Main.rand.NextBool(10))
            {
                float velScale = 1f + Main.rand.NextFloat() * 2f;
                float fadeIn = 1f + Main.rand.NextFloat();
                float scale = 1f + Main.rand.NextFloat();
                Vector2 offset = Utils.RandomVector2(Main.rand, -1f, 1f);
                if (offset != Vector2.Zero)
                {
                    offset.Normalize();
                }
                offset *= 20f + Main.rand.NextFloat() * 100f;
                Vector2 dustPos = Projectile.Center + offset;
                Point tilePos = dustPos.ToTileCoordinates();
                bool canSpawn = true;
                if (!WorldGen.InWorld(tilePos.X, tilePos.Y, 0))
                {
                    canSpawn = false;
                }
                if (canSpawn && WorldGen.SolidTile(tilePos.X, tilePos.Y))
                {
                    canSpawn = false;
                }
                if (canSpawn)
                {
                    Dust d = Main.dust[Dust.NewDust(dustPos, 0, 0, DustID.LastPrism, 0f, 0f, 127, starColor, 1f)];
                    d.noGravity = true;
                    d.position = dustPos;
                    d.velocity = -Vector2.UnitY * velScale * (Main.rand.NextFloat() * 0.9f + 1.6f);
                    d.fadeIn = fadeIn;
                    d.scale = scale;
                    d.noLight = true;
                    Dust d2 = Dust.CloneDust(d);
                    d2.scale *= 0.65f;
                    d2.fadeIn *= 0.65f;
                    d2.color = new Color(255, 255, 255, 255);
                }
            }
            Projectile.scale = Projectile.Opacity / 2f * Projectile.localAI[0];
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[1] += 1f;
            if (Projectile.localAI[1] >= 60f)
            {
                Projectile.Kill();
                return;
            }
        }
        /// <summary>自绘：两层星形贴图（一层随自身旋转、一层不转），并在 <c>ai[1]</c> 有效时朝那发弹幕画一道拉伸光带。</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Color light = Lighting.GetColor((int)((double)Projectile.position.X + (double)Projectile.width * 0.5) / 16, (int)(((double)Projectile.position.Y + (double)Projectile.height * 0.5) / 16.0));
            Vector2 drawPos = Projectile.position + new Vector2((float)Projectile.width, (float)Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Color alphaColor = Projectile.GetAlpha(light);
            Vector2 origin = frame.Size() / 2f;
            Color starColor = Main.hslToRgb(0.5f, 1f, Projectile.ai[0]).MultiplyRGBA(new Color(255, 255, 255, 0));
            Main.EntitySpriteDraw(tex, drawPos, frame, starColor, Projectile.rotation, origin, Projectile.scale * 2f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, drawPos, frame, starColor, 0f, origin, Projectile.scale * 2f, SpriteEffects.None, 0);
            if (Projectile.ai[1] != -1f && Projectile.Opacity > 0.3f)
            {
                // 朝光源弹幕拉一条光带：长度 = 两心距离，粗细随时间收缩
                Vector2 beamOffset = Main.projectile[(int)Projectile.ai[1]].Center - Projectile.Center;
                Vector2 beamScale = new Vector2(1f, beamOffset.Length() / (float)tex.Height);
                float beamRotation = beamOffset.ToRotation() + 1.57079637f;
                float beamOpacity = MathHelper.Distance(30f, Projectile.localAI[1]) / 20f;
                beamOpacity = MathHelper.Clamp(beamOpacity, 0f, 1f);
                if (beamOpacity > 0f)
                {
                    Main.EntitySpriteDraw(tex, drawPos + beamOffset / 2f, frame, starColor * beamOpacity, beamRotation, origin, beamScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(tex, drawPos + beamOffset / 2f, frame, alphaColor * beamOpacity, beamRotation, origin, beamScale / 2f, SpriteEffects.None, 0);
                }
            }
            return false;
        }
        /// <summary>
        /// 消失时（tML 的 <c>OnKill</c>，源里写的过时钩子 <c>Kill</c>）：喷一圈星屑，然后在**主人端**把碰撞箱临时撑成 60×60、穿透改 -1、逐敌 10 帧独立冷却，
        /// 调一次 <c>Projectile.Damage()</c> 结算范围伤害，再把所有数值原样还回去（源写法）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            Vector2 spin = new Vector2(0f, -3f).RotatedByRandom(3.1415927410125732);
            float dustCount = (float)Main.rand.Next(7, 13);
            Vector2 velBase = new Vector2(2.1f, 2f);
            Color starColor = Main.hslToRgb(0.5f, 1f, Projectile.ai[0]);
            starColor.A = 255;
            for (float i = 0f; i < dustCount; i += 1f)
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.LastPrism, 0f, 0f, 0, starColor, 1f);
                Main.dust[d].position = Projectile.Center;
                Main.dust[d].velocity = spin.RotatedBy((double)(6.28318548f * i / dustCount), default) * velBase * (0.8f + Main.rand.NextFloat() * 0.4f);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 2f;
                Main.dust[d].fadeIn = Main.rand.NextFloat() * 2f;
                Dust d2 = Dust.CloneDust(d);
                d2.scale /= 2f;
                d2.fadeIn /= 2f;
                d2.color = new Color(255, 255, 255, 255);
            }
            for (float i = 0f; i < dustCount; i += 1f)
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.LastPrism, 0f, 0f, 0, starColor, 1f);
                Main.dust[d].position = Projectile.Center;
                Main.dust[d].velocity = spin.RotatedBy((double)(6.28318548f * i / dustCount), default) * velBase * (0.8f + Main.rand.NextFloat() * 0.4f);
                Main.dust[d].velocity *= Main.rand.NextFloat() * 0.8f;
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = Main.rand.NextFloat() * 1f;
                Main.dust[d].fadeIn = Main.rand.NextFloat() * 2f;
                Dust d2 = Dust.CloneDust(d);
                d2.scale /= 2f;
                d2.fadeIn /= 2f;
                d2.color = new Color(255, 255, 255, 255);
            }
            // 范围伤害只在主人端结算（源写 Main.myPlayer == Projectile.owner）
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.friendly = true;
                int oldWidth = Projectile.width;
                int oldHeight = Projectile.height;
                int oldPenetrate = Projectile.penetrate;
                Projectile.position = Projectile.Center;
                Projectile.width = Projectile.height = 60;
                Projectile.Center = Projectile.position;
                Projectile.penetrate = -1;
                Projectile.maxPenetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = 10;
                Projectile.Damage();
                Projectile.penetrate = oldPenetrate;
                Projectile.position = Projectile.Center;
                Projectile.width = oldWidth;
                Projectile.height = oldHeight;
                Projectile.Center = Projectile.position;
            }
        }
        /// <summary>自绘用染色：全白渐入（源的 alpha 通道写成 0，靠加法叠亮）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255 - Projectile.alpha, 255 - Projectile.alpha, 255 - Projectile.alpha, 0);
        }
        /// <summary>命中敌人：施加夜凋 180 帧（灾厄两版都没有该 buff 时退回原版暗影炎）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "Nightwither", 180, BuffID.ShadowFlame);
        }
    }
}
