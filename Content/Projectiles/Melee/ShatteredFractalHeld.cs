using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 破碎分形手持弹幕（移植自 CalamityEntropy 的 ShatteredFractalHeld）：贴身绘制剑体并按三式循环挥动。
    /// 挥砍总时长取自 <see cref="Player.itemTimeMax"/>，进度 progress 由 0 走到 1。
    /// <para>
    /// ai[0] 决定招式：<b>-1 / 1</b> 是左右两个方向的普通挥砍（旋转方向再乘以玩家朝向），
    /// 剑身后方会画一道半圆拖尾；<b>2</b> 是向前刺出，剑体由贴身推出到极限后收回，并在起手时射出 FractalShoot。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c>/<c>getTextureGlow()</c> 换成 <c>Main.player[owner]</c>、TextureAssets 与 ModContent.Request；
    /// ③ <c>CEUtils.CustomLerp2</c> 与 <c>CEUtils.LineThroughRect</c> 在 CE 侧属于工具库，这里内联；
    /// ④ 去掉 CE 里只记录、从不读取的 odr 旋转历史，以及配套的 TrailingMode/TrailCacheLength 设置；
    /// ⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、画完再恢复默认批次；
    /// ⑥ <c>CEUtils.WeapSound</c>（CE 的近战音量配置项）按 1.0 处理。
    /// </para>
    /// </summary>
    internal class ShatteredFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/ShatteredFractal";
        /// <summary>普通挥砍的拖尾贴图（CE 的 CEExtraAssets.SemiCircularSmear）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/SemiCircularSmear";
        /// <summary>自身帧数计数（ai[0] 存的是招式下标，不是计时器）</summary>
        private float counter = 0f;
        /// <summary>绘制缩放：普通挥砍时按 progress 起伏，刺出时按推出进度放大</summary>
        private float scale = 1f;
        /// <summary>绘制透明度：普通挥砍恒为 1，刺出时随推出进度由 0 到 1</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>刺出式是否已射出 FractalShoot（每把剑只射一次）</summary>
        private bool shoot = true;
        /// <summary>本次挥砍是否已播过命中音（一次挥砍只播一次）</summary>
        private bool playHitSound = true;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;                        // 命中判定完全由 Colliding 的线段接管，碰撞箱取最小
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = -1;         // 同一次挥砍对同一敌人只结算一次
            Projectile.timeLeft = 100000;                // 占位值，真正的结束判据是 counter
            Projectile.extraUpdates = 3;                 // 配合物品 18 帧使用时间 → 挥砍总帧数 72
        }
        /// <summary>
        /// 挥砍主体：普通式在玩家中心按余弦缓动划出弧线并张开；刺出式把剑体沿朝向推出，
        /// 缩放与透明度随推出进度同步变化，到中段达到最远后收回。
        /// </summary>
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            counter++;
            if (init)
            {
                float meleeScale = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
                if (Projectile.ai[0] == 2)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalThrust with { Volume = 0.6f }, Projectile.Center);
                }
                else
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = Projectile.ai[0] * 0.12f, Volume = 0.6f }, Projectile.Center);
                }
                init = false;
            }
            Projectile.timeLeft = 3;                     // 每帧续命，收尾交给下面的 counter 判据
            const float rotF = 5f;
            if (Projectile.ai[0] == 2)
            {
                if (shoot)
                {
                    shoot = false;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Vector2 forward = Vector2.Normalize(Projectile.velocity);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + forward * 100 * Projectile.scale, forward * 10,
                            ModContent.ProjectileType<FractalShoot>(), (int)(Projectile.damage * 1.5f), Projectile.knockBack, Projectile.owner);
                    }
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalShoot, Projectile.Center);
                }
                float l = (float)(Math.Cos(progress * MathHelper.Pi - MathHelper.PiOver2) * 0.5f + 0.5f);
                Projectile.rotation = Projectile.velocity.ToRotation();
                scale = 0.6f + l * 1.2f;
                alpha = l;
                Projectile.Center = owner.MountedCenter + Vector2.Normalize(Projectile.velocity) * (-34 + l * 34);
            }
            else
            {
                alpha = 1f;
                scale = 1 + (float)Math.Cos(CustomLerp2(progress) * MathHelper.Pi - MathHelper.PiOver2) * 0.5f;
                Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * CustomLerp2(progress)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                Projectile.Center = owner.MountedCenter;
            }
            owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            if (counter > maxUpdateTimes)
            {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
        }
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 自绘：普通挥砍先在剑身后方画一道加法混合的半圆拖尾（颜色由青转黄绿并随进度淡出），
        /// 刺出式则用同名的 Glow 贴图叠一层发光轮廓；最后按 dir 取贴图角为原点绘制剑体本身，
        /// 使剑柄始终握在手里（剑贴图朝右上，故额外转 45°）。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int dir = (int)Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
            if (Projectile.ai[0] == 2)
            {
                dir = Math.Sign(Projectile.velocity.X);
            }
            Vector2 origin = dir > 0 ? new Vector2(0, texture.Height) : new Vector2(texture.Width, texture.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;
            float maxUpdateTime = Main.player[Projectile.owner].itemTimeMax * Projectile.MaxUpdates;
            if (Projectile.ai[0] < 2)
            {
                Texture2D smear = ModContent.Request<Texture2D>(SmearTexture).Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(smear, Projectile.Center + Main.player[Projectile.owner].gfxOffY * Vector2.UnitY - Main.screenPosition, null,
                    Color.Lerp(new Color(50, 140, 160), new Color(200, 255, 66), counter / maxUpdateTime) * (1 - counter / maxUpdateTime) * 0.8f,
                    Projectile.rotation + MathHelper.ToRadians(32) * -dir, smear.Size() / 2f, Projectile.scale * 1.25f * scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            else
            {
                Texture2D glow = ModContent.Request<Texture2D>(Texture + "Glow").Value;
                Main.spriteBatch.Draw(glow, Projectile.Center + Main.player[Projectile.owner].gfxOffY * Vector2.UnitY - Main.screenPosition, null,
                    Color.White * alpha * 0.6f * (counter / maxUpdateTime), rot, origin, Projectile.scale * 1.4f, effect, 0f);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center + Main.player[Projectile.owner].gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale, effect);
            return false;
        }
        /// <summary>命中判定是从玩家中心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * SwordLength;
            float point = 0f;
            return targetHitbox.Contains((int)start.X, (int)start.Y) || targetHitbox.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 64, ref point);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * SwordLength, 84, DelegateMethods.CutTiles);
        }
        /// <summary>挥砍线段的长度（ai[0]==2 的刺出式长 24%）</summary>
        private float SwordLength => 86 * (Projectile.ai[0] == 2 ? 1.24f : 1) * Projectile.scale * scale;
        /// <summary>命中时播一次命中音，并按灾厄「真断钢」的粒子套路在敌怪中心炸一圈火花</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (playHitSound)
            {
                playHitSound = false;
                SoundEngine.PlaySound(Projectile.ai[0] == 2 ? CalamityDemutationSounds.FractalThrustHit : CalamityDemutationSounds.FractalSwingHit, Projectile.Center);
                if (Projectile.ai[0] != 2)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalImpact, Projectile.Center);
                }
            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TrueExcalibur, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
        }
        /// <summary>CEUtils.CustomLerp2 的等价实现：以 (1-p)³ 为权重的 1→0 插值</summary>
        private static float CustomLerp2(float p) => float.Lerp(1f, 0f, (1f - p) * (1f - p) * (1f - p));
    }
}
