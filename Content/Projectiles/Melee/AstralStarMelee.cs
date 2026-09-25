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
    /// 星陨（AstralStarMelee，移植自 CalamityEntropy 的同名弹幕，本体是原版「星怒落星」那套写法的改色版）：
    /// 分形星影与星熠分形命中时从天上 900 像素处砸下的拖尾星辰，边落边朝 500 像素内的敌怪归位，
    /// 沿途撒彩色尘与肉块、撞墙即碎并炸出一圈尘。
    /// <para>
    /// 与 CE 原版的差异：① 贴图按本工程「与类同名同目录」惯例放到 Content/Projectiles/Melee/AstralStarMelee.png
    /// （CE 放在它自己的 Assets/Extra/Ports/AstralStar），因此不再写 Texture 覆盖；
    /// ② <c>CEUtils.HomeInOnNPC</c> 内联：锁定期间把 <c>extraUpdates</c> +1、失锁还原，CE 把原始值暂存在
    /// <c>EGlobalProjectile.StoredEU</c>，本工程只此一处用到，改为类内私有字段；同时省掉 CE 那版里我们用不到的
    /// <c>respectIFrames</c> 分支与护甲穿透权重；③ <c>CEUtils.DrawAfterimagesCentered</c> 只内联本粒子用到的 mode 0
    /// （其余 mode、护甲着色器与 shrink 分支省略）；④ 减益不新建——现代版灾厄取
    /// <c>AstralInfectionDebuff</c>，经典版没有该系减益则退回原版 <c>BuffID.CursedInferno</c>（与 FractalStarBlade 同款写法）；
    /// ⑤ <c>Projectile.WithinRange</c> 用显式距离比较替代。
    /// </para>
    /// </summary>
    internal class AstralStarMelee:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>命中附加的减益：现代版灾厄取 AstralInfectionDebuff，其余情况为原版诅咒狱火</summary>
        private static int astralDebuffType = BuffID.CursedInferno;
        /// <summary>归位前的原始 extraUpdates（CE 暂存在 EGlobalProjectile.StoredEU），-1 表示还没记过</summary>
        private int storedExtraUpdates = -1;
        // ── 生命周期方法 ──
        /// <summary>残影缓存 6 点、TrailingMode 0（只记位置）；并解析命中减益：装了灾厄就用它的 AstralInfectionDebuff</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            astralDebuffType = BuffID.CursedInferno;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection))
            {
                astralDebuffType = astralInfection.Type;
            }
        }
        /// <summary>基础属性：24×24、友方近战、穿透 1、不碰撞物块、不受水减速；未设 timeLeft（靠撞墙/命中自然消失）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        /// <summary>自转 + 撒流星尘/火把尘/肉块，末尾每帧尝试朝 500 像素内的敌怪归位</summary>
        public override void AI()
        {
            Projectile.ai[1] += 1f;   // ai[1] 仅作帧计数（CE 原样，本类内未再读回）
            Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * Projectile.direction;   // 转速与速度大小成正比，方向随 spriteDirection
            if (Main.rand.NextBool(8))
            {
                // 每 8 帧撒两颗流星色尘（第二颗是压暗压小并提亮的克隆尘）
                for (int i = 0; i < 2; i++)
                {
                    Color newColor = Main.hslToRgb(0.5f, 1f, 0.5f);
                    int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, 0, newColor);
                    Main.dust[dustIndex].position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.5f;
                    Main.dust[dustIndex].velocity *= Main.rand.NextFloat() * 0.8f;
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].fadeIn = 0.6f + Main.rand.NextFloat();
                    Main.dust[dustIndex].velocity += Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3f;
                    Main.dust[dustIndex].scale = 0.7f;
                    if (dustIndex != 6000)   // 6000 = Main.maxDust：Dust.NewDust 找不到空闲槽时返回它，此时取不到尘
                    {
                        Dust dust = Dust.CloneDust(dustIndex);
                        dust.scale /= 2f;
                        dust.fadeIn *= 0.85f;
                        dust.color = new Color(255, 255, 255, 255);
                    }
                }
                Vector2 vector = Vector2.UnitX.RotatedByRandom(MathHelper.PiOver2).RotatedBy(Projectile.velocity.ToRotation());
                int torchDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f, 150);
                Main.dust[torchDust].velocity = vector * 0.33f;
                Main.dust[torchDust].position = Projectile.Center + vector * 6f;
            }
            if (Main.rand.NextBool(24) && Main.netMode != NetmodeID.MultiplayerClient)   // 肉块只在服务端/单机生成，避免各客户端重复
            {
                int goreIndex = Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity * 0.1f, 16);
                Main.gore[goreIndex].velocity *= 0.66f;
                Main.gore[goreIndex].velocity += Projectile.velocity * 0.15f;
            }
            Projectile.light = 0.9f;   // 自带 0.9 强度照明
            if (Main.rand.NextBool(5))
            {
                Color newColor2 = Main.hslToRgb(1f, 1f, 0.5f);
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, 0, newColor2);
                Main.dust[dustIndex].position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.5f;
                Main.dust[dustIndex].velocity *= Main.rand.NextFloat() * 0.8f;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].fadeIn = 0.6f + Main.rand.NextFloat();
                Main.dust[dustIndex].velocity += Projectile.velocity * 0.25f;
                Main.dust[dustIndex].scale = 0.7f;
                if (dustIndex != 6000)
                {
                    Dust dust2 = Dust.CloneDust(dustIndex);
                    dust2.scale /= 2f;
                    dust2.fadeIn *= 0.85f;
                    dust2.color = new Color(255, 255, 255, 255);
                }
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f, 150);
            }
            if (Main.rand.NextBool(10) && Main.netMode != NetmodeID.MultiplayerClient)   // 同上，肉块仅非客户端生成
            {
                Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.position, Projectile.velocity * 0.1f, Main.rand.Next(16, 18));
            }
            // ignoreTiles 直接沿用 tileCollide（本弹幕为 false）→ 要求与目标之间视线通畅；500 为归位半径、15 为归位速度、20 为惯性
            HomeInOnNPC(Projectile.tileCollide, 500f, 15f, 20f);
        }
        /// <summary>命中：附加太空感染减益</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(astralDebuffType, 180);
        }
        /// <summary>本体按固定紫色绘制（CE 原样，不吃环境光）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(200, 100, 250, Projectile.alpha);
        /// <summary>只交给残影绘制，本体也由它沿 oldPos 逐帧重画（返回 false 关掉默认绘制）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            DrawAfterimages(lightColor);
            return false;
        }
        /// <summary>撞到方块：炸出尘并播一声挖掘音，然后消失</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            return true;
        }
        /// <summary>消散：先把碰撞箱撑大 50 再炸一圈彩色尘与肉块（CE 原样）</summary>
        public override void OnKill(int timeLeft)
        {
            // CE 的 ExpandHitboxBy(50)（InnoVault 扩展）内联：以中心为基准把碰撞箱撑大 50
            Projectile.position -= new Vector2(25f);
            Projectile.width += 50;
            Projectile.height += 50;
            for (int i = 0; i < 3; i++)
            {
                Color newColor = Main.hslToRgb(1f, 1f, 0.5f);
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, 0, newColor);
                Main.dust[dustIndex].position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height);
                Main.dust[dustIndex].velocity *= Main.rand.NextFloat() * 2.4f;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].fadeIn = 0.6f + Main.rand.NextFloat();
                Main.dust[dustIndex].scale = 1.4f;
                if (dustIndex != 6000)
                {
                    Dust dust = Dust.CloneDust(dustIndex);
                    dust.scale /= 2f;
                    dust.fadeIn *= 0.85f;
                    dust.color = new Color(255, 255, 255, 255);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100);
                Main.dust[dustIndex].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[dustIndex].scale = 0.5f;
                    Main.dust[dustIndex].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default(Color), 1.5f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 5f;
                dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100);
                Main.dust[dustIndex].velocity *= 2f;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.position, Projectile.velocity * 0.05f, Main.rand.Next(16, 18));
                }
            }
        }
        // ── 私有工具 ──
        /// <summary>
        /// CEUtils.DrawAfterimagesCentered 的 mode 0 分支内联：沿 oldPos 逐个重画本体，
        /// 越靠后的残影越淡（CE 那把通用函数还带其他 mode / 护甲着色器 / 收缩分支，本粒子都没用到）。
        /// </summary>
        private void DrawAfterimages(Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            SpriteEffects effect = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 centerOffset = Projectile.Size / 2f;
            Color alphaColor = Projectile.GetAlpha(lightColor);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 drawPos = Projectile.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                float interpolant = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.spriteBatch.Draw(texture, drawPos, frame, alphaColor * interpolant, Projectile.rotation, origin, Projectile.scale, effect, 0f);
            }
        }
        /// <summary>
        /// CEUtils.HomeInOnNPC 的等价实现：取最近的可攻击敌怪（可选要求视线通畅），
        /// 锁定时把 extraUpdates 提一级并按「惯性 + 归位速度」重算速度，失锁则还原 extraUpdates。
        /// </summary>
        private void HomeInOnNPC(bool ignoreTiles, float distanceRequired, float homingVelocity, float inertia)
        {
            if (!Projectile.friendly)
            {
                return;
            }
            if (storedExtraUpdates == -1)
            {
                storedExtraUpdates = Projectile.extraUpdates;
            }
            Vector2 destination = Projectile.Center;
            bool locatedTarget = false;
            float npcDistCompare = 25000f;
            int index = -1;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float extraDistance = (npc.width / 2) + (npc.height / 2);
                if (!npc.CanBeChasedBy(Projectile, false) || Vector2.Distance(npc.Center, Projectile.Center) > distanceRequired + extraDistance)
                {
                    continue;
                }
                float currentNPCDist = Vector2.Distance(npc.Center, Projectile.Center);
                if (currentNPCDist < npcDistCompare && (ignoreTiles || Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1)))
                {
                    npcDistCompare = currentNPCDist;
                    index = npc.whoAmI;
                }
            }
            if (index != -1)
            {
                destination = Main.npc[index].Center;
                locatedTarget = true;
            }
            if (locatedTarget)
            {
                Projectile.extraUpdates = storedExtraUpdates + 1;
                Vector2 homeDirection = (destination - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = (Projectile.velocity * inertia + homeDirection * homingVelocity) / (inertia + 1f);
            }
            else
            {
                Projectile.extraUpdates = storedExtraUpdates;
            }
        }
    }
}
