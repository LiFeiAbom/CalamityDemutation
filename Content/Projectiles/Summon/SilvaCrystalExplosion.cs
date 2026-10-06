using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 叶棱晶爆裂（SilvaCrystalExplosion） - 远古叶棱晶（SilvaCrystal）射出的生命能量爆裂
    ///（按经典版灾厄 Projectiles/Summon/SilvaCrystalExplosion.cs 1:1 移植）。
    /// 无位移、就地停留 60 帧：期间朝本体所在方向画出一道光带（ai[1] = 本体索引，-1 表示本体已消失），
    /// 喷彩尘并点亮周围；寿命结束时把判定框撑到 60×60 并手工 <c>Damage()</c> 结算一次。
    /// 颜色由生成方给的 ai[0]（Hue）决定，用来配合主人的彩虹描边。
    /// </summary>
    public class SilvaCrystalExplosion : ModProjectile
    {
        /// <summary>
        /// 标记为召唤物的射击（MinionShot），便于召唤套装增伤等判据识别
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：14×14 碰撞箱；初始全透明、穿地形、入水不减速、存活 900 帧（实际 60 帧后自行结束）、按召唤职业结算
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
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// AI：校验本体是否仍在场（不在则断开光带）；淡入、旋转、按 2 帧一批喷彩尘；
        /// localAI[1] 计到 60 帧即自我了结
        /// </summary>
        public override void AI()
        {
            Color newColor2 = Main.hslToRgb(Projectile.ai[0], 1f, 0.5f);
            int crystalIndex = (int)Projectile.ai[1];
            if (crystalIndex < 0 || crystalIndex >= Main.maxProjectiles
                || (!Main.projectile[crystalIndex].active && Main.projectile[crystalIndex].type != ModContent.ProjectileType<SilvaCrystal>()))
            {
                Projectile.ai[1] = -1f;
            }
            else
            {
                DelegateMethods.v3_1 = newColor2.ToVector3() * 0.5f;
                Utils.PlotTileLine(Projectile.Center, Main.projectile[crystalIndex].Center, 8f, DelegateMethods.CastLight);
            }
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = Main.rand.NextFloat() * 0.8f + 0.8f;
                Projectile.direction = Main.rand.Next(2) > 0 ? 1 : -1;
            }
            Projectile.rotation = Projectile.localAI[1] / 40f * MathHelper.TwoPi * Projectile.direction;
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
                Lighting.AddLight(Projectile.Center, newColor2.ToVector3() * 0.5f);
            }
            // 两批各 2 次、每次 1/10 概率的贴身彩尘（源原样：一批各自叠甲方向）
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.Next(10) == 0)
                {
                    Vector2 dustVel = Vector2.UnitY.RotatedBy(i * MathHelper.Pi).RotatedBy(Projectile.rotation);
                    Dust silvaDust = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 225, newColor2, 1.5f)];
                    silvaDust.noGravity = true;
                    silvaDust.noLight = true;
                    silvaDust.scale = Projectile.Opacity * Projectile.localAI[0];
                    silvaDust.position = Projectile.Center;
                    silvaDust.velocity = dustVel * 2.5f;
                }
            }
            for (int j = 0; j < 2; j++)
            {
                if (Main.rand.Next(10) == 0)
                {
                    Vector2 dustVel2 = Vector2.UnitY.RotatedBy(j * MathHelper.Pi);
                    Dust silvaDust2 = Main.dust[Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 225, newColor2, 1.5f)];
                    silvaDust2.noGravity = true;
                    silvaDust2.noLight = true;
                    silvaDust2.scale = Projectile.Opacity * Projectile.localAI[0];
                    silvaDust2.position = Projectile.Center;
                    silvaDust2.velocity = dustVel2 * 2.5f;
                }
            }
            if (Main.rand.Next(10) == 0)
            {
                // 外围随机洒落的"绿叶"尘：只在空中的非实心格生成，落地即跳过
                float dustVelMod = 1f + Main.rand.NextFloat() * 2f;
                float fadeIn = 1f + Main.rand.NextFloat();
                float dustScale = 1f + Main.rand.NextFloat();
                Vector2 randVector = Utils.RandomVector2(Main.rand, -1f, 1f);
                if (randVector != Vector2.Zero)
                {
                    randVector.Normalize();
                }
                randVector *= 20f + Main.rand.NextFloat() * 100f;
                Vector2 dustPos = Projectile.Center + randVector;
                Point dustCoords = dustPos.ToTileCoordinates();
                bool shouldSpawnDust = WorldGen.InWorld(dustCoords.X, dustCoords.Y, 0) && !WorldGen.SolidTile(dustCoords.X, dustCoords.Y);
                if (shouldSpawnDust)
                {
                    Dust rainbowDust = Main.dust[Dust.NewDust(dustPos, 0, 0, DustID.RainbowMk2, 0f, 0f, 127, newColor2, 1f)];
                    rainbowDust.noGravity = true;
                    rainbowDust.position = dustPos;
                    rainbowDust.velocity = -Vector2.UnitY * dustVelMod * (Main.rand.NextFloat() * 0.9f + 1.6f);
                    rainbowDust.fadeIn = fadeIn;
                    rainbowDust.scale = dustScale;
                    rainbowDust.noLight = true;
                    Dust rainbowDust2 = Dust.CloneDust(rainbowDust);
                    rainbowDust2.scale *= 0.65f;
                    rainbowDust2.fadeIn *= 0.65f;
                    rainbowDust2.color = new Color(255, 255, 255, 255);
                }
            }
            Projectile.scale = Projectile.Opacity / 2f * Projectile.localAI[0];
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[1] += 1f;
            if (Projectile.localAI[1] >= 60f)
            {
                Projectile.Kill();
            }
        }
        /// <summary>
        /// 自定义绘制：以自身颜色画本体（旋转 + 不旋转两层），并朝本体方向拉出一条半透明的光带
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Color colorArea = Lighting.GetColor((int)(Projectile.position.X + Projectile.width * 0.5) / 16, (int)((Projectile.position.Y + Projectile.height * 0.5) / 16.0));
            Vector2 projPos = Projectile.position + new Vector2(Projectile.width, Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Color colorAlpha = Projectile.GetAlpha(colorArea);
            Vector2 halfFrame = frame.Size() / 2f;
            Color projColor = Main.hslToRgb(Projectile.ai[0], 1f, 0.5f).MultiplyRGBA(new Color(255, 255, 255, 0));
            Main.EntitySpriteDraw(texture, projPos, frame, projColor, Projectile.rotation, halfFrame, Projectile.scale * 2f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, projPos, frame, projColor, 0f, halfFrame, Projectile.scale * 2f, SpriteEffects.None, 0);
            if (Projectile.ai[1] != -1f && Projectile.Opacity > 0.3f)
            {
                Vector2 projToCrystal = Main.projectile[(int)Projectile.ai[1]].Center - Projectile.Center;
                Vector2 beamScale = new Vector2(1f, projToCrystal.Length() / texture.Height);
                float drawRotation = projToCrystal.ToRotation() + MathHelper.PiOver2;
                float colorClamp = MathHelper.Clamp(MathHelper.Distance(30f, Projectile.localAI[1]) / 20f, 0f, 1f);
                if (colorClamp > 0f)
                {
                    Main.EntitySpriteDraw(texture, projPos + projToCrystal / 2f, frame, projColor * colorClamp, drawRotation, halfFrame, beamScale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture, projPos + projToCrystal / 2f, frame, colorAlpha * colorClamp, drawRotation, halfFrame, beamScale / 2f, SpriteEffects.None, 0);
                }
            }
            return false;
        }
        /// <summary>
        /// 绘制颜色：本体完全透明（只靠 PreDraw 的颜色层显形）
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255 - Projectile.alpha, 255 - Projectile.alpha, 255 - Projectile.alpha, 0);
        }
        /// <summary>
        /// 消亡：喷两圈彩尘；主人端把判定框撑到 60×60（以原中心为锚点）后手工结算一次伤害再还原
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            Vector2 spinningpoint = new Vector2(0f, -3f).RotatedByRandom(MathHelper.Pi);
            float rando = Main.rand.Next(7, 13);
            Vector2 dustVel = new Vector2(2.1f, 2f);
            Color newColor = Main.hslToRgb(Projectile.ai[0], 1f, 0.5f);
            newColor.A = 255;
            for (float i = 0f; i < rando; i += 1f)
            {
                int dustID = Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 0, newColor, 1f);
                Main.dust[dustID].position = Projectile.Center;
                Main.dust[dustID].velocity = spinningpoint.RotatedBy(MathHelper.TwoPi * i / rando) * dustVel * (0.8f + Main.rand.NextFloat() * 0.4f);
                Main.dust[dustID].noGravity = true;
                Main.dust[dustID].scale = 2f;
                Main.dust[dustID].fadeIn = Main.rand.NextFloat() * 2f;
                Dust dustCloning = Dust.CloneDust(dustID);
                dustCloning.scale /= 2f;
                dustCloning.fadeIn /= 2f;
                dustCloning.color = new Color(255, 255, 255, 255);
            }
            for (float j = 0f; j < rando; j += 1f)
            {
                int dustID = Dust.NewDust(Projectile.Center, 0, 0, DustID.RainbowMk2, 0f, 0f, 0, newColor, 1f);
                Main.dust[dustID].position = Projectile.Center;
                Main.dust[dustID].velocity = spinningpoint.RotatedBy(MathHelper.TwoPi * j / rando) * dustVel * (0.8f + Main.rand.NextFloat() * 0.4f);
                Main.dust[dustID].velocity *= Main.rand.NextFloat() * 0.8f;
                Main.dust[dustID].noGravity = true;
                Main.dust[dustID].scale = Main.rand.NextFloat() * 1f;
                Main.dust[dustID].fadeIn = Main.rand.NextFloat() * 2f;
                Dust dustCloning2 = Dust.CloneDust(dustID);
                dustCloning2.scale /= 2f;
                dustCloning2.fadeIn /= 2f;
                dustCloning2.color = new Color(255, 255, 255, 255);
            }
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.friendly = true;
                int width = Projectile.width;
                int height = Projectile.height;
                int penetrateClone = Projectile.penetrate;
                Projectile.position = Projectile.Center;
                Projectile.width = Projectile.height = 60;
                Projectile.Center = Projectile.position;
                Projectile.penetrate = -1;
                Projectile.maxPenetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = 10;
                Projectile.Damage();
                Projectile.penetrate = penetrateClone;
                Projectile.position = Projectile.Center;
                Projectile.width = width;
                Projectile.height = height;
                Projectile.Center = Projectile.position;
            }
        }
    }
}
