using CalamityDemutation.Content.Particles;
using CalamityDemutation.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Content.Projectiles.Melee.Core;
using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityDemutation.Content.Buffs.NegativeBuffs;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 巨龙之怒手持弹幕（移植自 CWR 的 DragonRageHeld）：7 个挥砍/突刺模式。
    /// 从简：TrueMeleeDamageClass→Melee、灾厄音效→原版、FuckYou→原版爆炸、DR/钢铁材质/BrimstoneHeart 检查裁剪。
    /// </summary>
    internal class DragonRageHeld : BaseSwingCO
    {
        /// <summary>
        /// 内部挥砍攻速乘数（等效攻速加速，绕过 MeleeNoSpeed 的攻速免疫）。
        /// 原值 1f；2026-09-08 改为 1.4f 实现整体 ×1.4 加速，若要回退改回 1f。
        /// 仅作用于走 <see cref="speedUp"/> 的主体挥砍（ai[0]=0/1/2/4/5），
        /// ai[0]=3 突刺与 ai[0]=6 蓄力旋不使用 speedUp，不受此值影响。
        /// </summary>
        internal const float SwingAtkSpeed = 1.4f;
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/DragonRageStaff";
        public override string trailTexturePath => CalamityDemutationConstant.Masking + "MotionTrail3";
        public override string gradientTexturePath => CalamityDemutationConstant.ColorBar + "DragonRage_Bar";
        /// <summary>
        /// 挥砍基础属性初始化：由基类 <see cref="BaseSwingCO.SetDefaults"/> 在 <see cref="BaseSwingCO.PreSetSwingProperty"/> 返回 true 后回调。
        /// 这里克隆原版长矛弹幕模板，并设定额外更新次数 3、伤害类型为近战无攻速、尺寸与本地无敌帧（5 帧），
        /// 以及刀光相关参数（中心距 125、弧光顶部宽度 90、长度 80）。
        /// </summary>
        public override void SetSwingProperty()
        {
            Projectile.CloneDefaults(ProjectileID.Spear);
            Projectile.extraUpdates = 3;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            distanceToOwner = 125;
            drawTrailTopWidth = 90;
            Length = 80;
        }
        /// <summary>
        /// 主体挥舞逻辑：由基类 <see cref="BaseSwingCO.PreUpdate"/> 在 <see cref="BaseSwingCO.InOwner"/> 之后、<see cref="BaseSwingCO.UpdateCaches"/> 之前调用。
        /// 按 <c>Projectile.ai[0]</c> 分七套模式：0/1/2 为左右挥砍（长度与转速先增后减），3 为直线突刺，
        /// 4/5 为大范围重挥（额外放大尺寸），6 为按住右键的蓄力旋转。
        /// speedUp 由攻速与 <see cref="SwingAtkSpeed"/> 共同求出，它同时压缩 Time 阈值与转速衰减，
        /// 从而在绕过 MeleeNoSpeed 攻速免疫的前提下实现整体加速；ai[0]=3 与 6 不乘 speedUp。
        /// 方法末尾根据模式决定是否绘制刀光（突刺不画）以及是否做对角线翻转（模式 1/5）。
        /// </summary>
        public override void SwingAI()
        {
            float speedUp = 1 / (Owner.GetAttackSpeed(DamageClass.MeleeNoSpeed) * SwingAtkSpeed);
            if (Projectile.ai[0] == 0)
            {
                if (Time == 0)
                {
                    startVector = RodingToVer(1, Projectile.velocity.ToRotation() - MathHelper.PiOver2 * Projectile.spriteDirection);
                    speed = MathHelper.ToRadians(6) / speedUp;
                }
                if (Time < 10 * speedUp)
                {
                    Length *= 1 + 0.1f / updateCount;
                    Rotation += speed * Projectile.spriteDirection;
                    speed *= 1 + 0.2f / updateCount;
                    vector = startVector.RotatedBy(Rotation) * Length;
                }
                else
                {
                    Length *= 1 - 0.01f / updateCount;
                    Rotation += speed * Projectile.spriteDirection;
                    speed *= 1 - 0.2f / updateCount / speedUp;
                    vector = startVector.RotatedBy(Rotation) * Length;
                }
                if (Time >= 22 * updateCount * speedUp)
                {
                    Projectile.Kill();
                }
                if (Time % updateCount == updateCount - 1)
                {
                    Length = MathHelper.Clamp(Length, 120, 160);
                }
            }
            else if (Projectile.ai[0] == 1)
            {
                if (Time == 0)
                {
                    startVector = RodingToVer(1, Projectile.velocity.ToRotation() + MathHelper.PiOver2 * Projectile.spriteDirection);
                    speed = MathHelper.ToRadians(6) / speedUp;
                }
                if (Time < 10 * speedUp)
                {
                    Length *= 1 + 0.1f / updateCount;
                    Rotation -= speed * Projectile.spriteDirection;
                    speed *= 1 + 0.2f / updateCount;
                    vector = startVector.RotatedBy(Rotation) * Length;
                }
                else
                {
                    Length *= 1 - 0.01f / updateCount;
                    Rotation -= speed * Projectile.spriteDirection;
                    speed *= 1 - 0.2f / updateCount / speedUp;
                    vector = startVector.RotatedBy(Rotation) * Length;
                }
                if (Time >= 22 * updateCount * speedUp)
                {
                    Projectile.Kill();
                }
                if (Time % updateCount == updateCount - 1)
                {
                    Length = MathHelper.Clamp(Length, 110, 120);
                }
            }
            else if (Projectile.ai[0] == 2)
            {
                if (Time == 0)
                {
                    startVector = RodingToVer(1, Projectile.velocity.ToRotation() - MathHelper.PiOver2 * Projectile.spriteDirection);
                    speed = MathHelper.ToRadians(6) / speedUp;
                }
                if (Time < 10 * speedUp)
                {
                    Length *= 1 + 0.11f / updateCount;
                    Rotation += speed * Projectile.spriteDirection;
                    speed *= 1 + 0.3f / updateCount;
                    vector = startVector.RotatedBy(Rotation) * Length;
                }
                else
                {
                    Length *= 1 - 0.01f / updateCount;
                    Rotation += speed * Projectile.spriteDirection;
                    speed *= 1 - (0.13f * speedUp * 1.15f) / updateCount / speedUp;
                    vector = startVector.RotatedBy(Rotation) * Length;
                }
                if (Time >= 26 * updateCount * speedUp)
                {
                    Projectile.Kill();
                }
                if (Time % updateCount == updateCount - 1)
                {
                    Length = MathHelper.Clamp(Length, 60, 120);
                }
            }
            else if (Projectile.ai[0] == 3)
            {
                if (Time == 0)
                {
                    startVector = RodingToVer(1, Projectile.velocity.ToRotation());
                    speed = 1 + 0.6f / updateCount;
                }
                if (Time < 6 * updateCount)
                {
                    Vector2 position = Projectile.Center + startVector * Projectile.scale;
                    Dust dust = Main.dust[Dust.NewDust(Owner.position, Owner.width, Owner.height, DustID.CopperCoin)];
                    dust.position = position;
                    dust.velocity = Projectile.velocity.RotatedBy(1.57) * 0.33f + Projectile.velocity / 4f * Projectile.scale;
                    dust.position += Projectile.velocity.RotatedBy(1.57);
                    dust.scale = Projectile.scale * 3;
                    dust.fadeIn = 0.5f;
                    dust.noGravity = true;
                    dust = Main.dust[Dust.NewDust(Owner.position, Owner.width, Owner.height, DustID.CopperCoin)];
                    dust.position = position;
                    dust.velocity = Projectile.velocity.RotatedBy(-1.57) * 0.33f + Projectile.velocity / 4f * Projectile.scale;
                    dust.position += Projectile.velocity.RotatedBy(-1.57);
                    dust.scale = Projectile.scale * 3;
                    dust.fadeIn = 0.5f;
                    dust.noGravity = true;
                    Vector2 spanSparkPos = Projectile.Center + Projectile.velocity.UnitVector() * Length;
                    DRKLoader.AddParticle(new DRK_Spark(spanSparkPos, Projectile.velocity, false, 6, 4.26f, Color.Gold, Owner));
                }
                Length *= speed;
                vector = startVector * Length;
                speed -= 0.015f / updateCount;
                if (Time >= 26 * updateCount)
                {
                    Projectile.Kill();
                }
                float toTargetSengs = Projectile.Center.To(Owner.Center).Length();
                Projectile.scale = 0.8f + toTargetSengs / 520f;
                if (Time % updateCount == updateCount - 1)
                {
                    Length = MathHelper.Clamp(Length, 30, 260);
                }
            }
            else if (Projectile.ai[0] == 4)
            {
                if (Time == 0)
                {
                    distanceToOwner = 105;
                    drawTrailTopWidth = 190;
                    InitializeCaches();
                    startVector = RodingToVer(1, Projectile.velocity.ToRotation() - MathHelper.PiOver2 * Projectile.spriteDirection);
                    speed = MathHelper.ToRadians(6) / speedUp;
                    Rotation = MathHelper.ToRadians(-30 * Projectile.spriteDirection);
                }
                if (Time < 10 * speedUp)
                {
                    Length *= 1 + 0.1f / updateCount;
                    Rotation += speed * Projectile.spriteDirection;
                    speed *= 1 + 0.2f / updateCount;
                    vector = startVector.RotatedBy(Rotation) * Length;
                    Projectile.scale += 0.03f;
                }
                else
                {
                    Length *= 1 - 0.01f / updateCount;
                    Rotation += speed * Projectile.spriteDirection;
                    speed *= 1 - 0.2f / updateCount / speedUp;
                    vector = startVector.RotatedBy(Rotation) * Length;
                    if (Time >= 20 * updateCount)
                    {
                        Projectile.scale -= 0.001f;
                    }
                }
                if (Time >= 22 * updateCount * speedUp)
                {
                    Projectile.Kill();
                }
                if (Time % updateCount == updateCount - 1)
                {
                    Length = MathHelper.Clamp(Length, 120, 260);
                }
            }
            else if (Projectile.ai[0] == 5)
            {
                if (Time == 0)
                {
                    distanceToOwner = 105;
                    drawTrailTopWidth = 190;
                    InitializeCaches();
                    startVector = RodingToVer(1, Projectile.velocity.ToRotation() - MathHelper.PiOver2 * Projectile.spriteDirection);
                    speed = MathHelper.ToRadians(6) / speedUp;
                    Rotation = MathHelper.ToRadians(-110 * Projectile.spriteDirection);
                }
                if (Time < 10 * speedUp)
                {
                    Length *= 1 + 0.1f / updateCount;
                    Rotation -= speed * Projectile.spriteDirection;
                    speed *= 1 + 0.2f / updateCount;
                    vector = startVector.RotatedBy(Rotation) * Length;
                    Projectile.scale += 0.03f;
                }
                else
                {
                    Length *= 1 - 0.01f / updateCount;
                    Rotation -= speed * Projectile.spriteDirection;
                    speed *= 1 - 0.2f / updateCount / speedUp;
                    vector = startVector.RotatedBy(Rotation) * Length;
                    if (Time >= 20 * updateCount)
                    {
                        Projectile.scale -= 0.001f;
                    }
                }
                if (Time >= 22 * updateCount * speedUp)
                {
                    Projectile.Kill();
                }
                if (Time % updateCount == updateCount - 1)
                {
                    Length = MathHelper.Clamp(Length, 120, 260);
                }
            }
            else if (Projectile.ai[0] == 6)
            {
                canFormOwnerSetDir = false;
                canSetOwnerArmBver = false;
                if (Time == 0)
                {
                    distanceToOwner = 155;
                    drawTrailTopWidth = 60;
                    InitializeCaches();
                    Projectile.spriteDirection = Owner.direction;
                    startVector = RodingToVer(1, Projectile.velocity.ToRotation() - MathHelper.PiOver2 * Projectile.spriteDirection);
                    speed = MathHelper.ToRadians(6);
                }
                if (Time < 10)
                {
                    Length *= 1 + 0.11f / updateCount;
                    Rotation += speed * Projectile.spriteDirection;
                    speed *= 1 + 0.3f / updateCount;
                    vector = startVector.RotatedBy(Rotation) * Length;
                    Projectile.scale += 0.011f;
                }
                else
                {
                    Rotation += speed * Projectile.spriteDirection;
                    if (!DownRight)
                    {
                        speed *= 1 - 0.01f / updateCount;
                        if (Time >= 60 * updateCount)
                        {
                            Length *= 1 - 0.01f / updateCount;
                            Projectile.scale -= 0.001f;
                        }
                    }
                    else
                    {
                        if (Time > 30 * updateCount)
                        {
                            Time = 30 * updateCount;
                        }
                        if (Projectile.soundDelay <= 0)
                        {
                            SoundEngine.PlaySound(CalamityDemutationSounds.CatastropheSwing with { MaxInstances = 6, Volume = 0.45f }, Owner.Center);
                            Projectile.soundDelay = 30 * updateCount;
                        }
                    }
                    Owner.ChangeDir(Math.Sign(ToMouse.X));
                    Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter
                        , Owner.direction < 0 ? MathHelper.PiOver4 : MathHelper.PiOver4 + MathHelper.Pi + MathHelper.PiOver2);
                    vector = startVector.RotatedBy(Rotation) * Length;
                    if (Time % updateCount == 0)
                    {
                        SpawnDust(Owner, Owner.direction);
                    }
                }
                if (Time >= 90 * updateCount && !DownRight)
                {
                    Projectile.Kill();
                }
                if (Time % updateCount == updateCount - 1)
                {
                    Length = MathHelper.Clamp(Length, 60, 220);
                }
            }
            if (Time > 1)
            {
                Projectile.alpha = 0;
            }
            CanDrawSlashTrail = Projectile.ai[0] != 3;
            inDrawFlipdiagonally = Projectile.ai[0] == 1 || Projectile.ai[0] == 5;
        }
        /// <summary>
        /// 命中敌怪：ai[0]=3 的突刺会在命中点叠加两层 bloom 粒子、额外发射一枚 FireBall（伤害 1/4）并施加血炎爆炸 debuff 300 帧；
        /// ai[0]=6 的蓄力旋转则在特殊世界种子（zenithWorld/getGoodWorld/drunkWorld）下，每累计 3 次命中向四周抛出 3 颗 DragonRageFireOrb。
        /// 最后统一调用 <see cref="HitEffect"/> 播放命中火花。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int type = ModContent.ProjectileType<DragonRageFireOrb>();
            if (Projectile.ai[0] == 3)
            {
                float orbSize = Main.rand.NextFloat(0.5f, 0.8f) * Projectile.numHits;
                if (orbSize > 2.2f)
                {
                    orbSize = 2.2f;
                }
                GeneralParticleHandler.SpawnParticle(new GenericBloom(target.Center, Vector2.Zero, Color.OrangeRed, orbSize + 0.6f, 8, true));
                GeneralParticleHandler.SpawnParticle(new GenericBloom(target.Center, Vector2.Zero, Color.White, orbSize + 0.2f, 8, true));
                // 仅在主人端生成伤害弹幕，避免多人下各端各生一枚造成重复伤害（同方法下方分支亦如此判定）
                if (Projectile.IsOwnedByLocalPlayer())
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<FireBall>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
                target.AddBuff(ModContent.BuffType<HellfireExplosion>(), 300);
            }
            else if (Projectile.ai[0] == 6 && Projectile.IsOwnedByLocalPlayer() && Projectile.numHits % 3 == 0 && (Main.zenithWorld || Main.getGoodWorld || Main.drunkWorld))
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vr = (MathHelper.TwoPi / 3f * i + Main.GameUpdateCount * 0.1f).ToRotationVector2();
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center + vr * Main.rand.Next(22, 38), vr.RotatedByRandom(0.32f) * 3
                        , type, Projectile.damage / 6, Projectile.knockBack, Projectile.owner, 0f, rotSpeed * 0.1f);
                }
            }
            HitEffect(target);
        }

        /// <summary>
        /// 命中玩家（PvP）时的对应处理：施加血炎爆炸 debuff 300 帧；ai[0]=3 突刺同样额外发射一枚 FireBall，随后调用 <see cref="HitEffect"/>。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<HellfireExplosion>(), 300);
            if (Projectile.ai[0] == 3)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<FireBall>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
            }
            HitEffect(target);
        }

        /// <summary>
        /// 命中修正：仅 ai[0]=3 的直线突刺生效——把护甲有效度乘 0（<c>DefenseEffectiveness = 0</c>），即突刺完全无视目标防御。
        /// </summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 3)
            {
                modifiers.DefenseEffectiveness *= 0f;
            }
        }

        /// <summary>
        /// 自定义碰撞：不用默认矩形碰撞盒，而是取「玩家中心指向弹幕中心」的方向，从弹幕中心向外延伸
        /// <c>Length * scale * 1.3</c> 得到刀尖端点，再以宽度 <c>25 * scale</c> 的线段与目标 AABB 做碰撞检测。
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            float rotding = Owner.Center.To(Projectile.Center).ToRotation();
            Vector2 endPos = rotding.ToRotationVector2() * Length * Projectile.scale * 1.3f + Projectile.Center;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, endPos, 25 * Projectile.scale, ref point);
        }

        /// <summary>
        /// 绘制刀光顶点带：由基类 <see cref="BaseSwingCO.DrawTrailHander"/>（经 <see cref="BaseSwingCO.DrawSlashTrail"/>）回调。
        /// 使用 <c>noEffects/KnifeRendering</c> 着色器，绑定变换矩阵、流形贴图与颜色条贴图，每个 pass 先正常画一遍，
        /// 再切到加色混合叠画一遍，得到高亮刀光效果。
        /// </summary>
        public override void DrawTrail(List<VertexPositionColorTexture> bars)
        {
            Effect effect = CalamityDemutation.Instance.Assets.Request<Effect>(CalamityDemutationConstant.noEffects + "KnifeRendering").Value;
            effect.Parameters["transformMatrix"].SetValue(GetTransfromMaxrix());
            effect.Parameters["sampleTexture"].SetValue(TrailTexture);
            effect.Parameters["gradientTexture"].SetValue(GradientTexture);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, bars.ToArray(), 0, bars.Count - 2);
                Main.graphics.GraphicsDevice.BlendState = BlendState.Additive;
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, bars.ToArray(), 0, bars.Count - 2);
            }
        }

        /// <summary>
        /// 绘制武器本体：由基类 <see cref="BaseSwingCO.PreDraw"/> 在 <see cref="BaseSwingCO.DrawSlashTrail"/> 之后调用。
        /// ai[0]=6 的蓄力旋转先在玩家中心叠加一张红色加色光晕（Particles/Light），随后调 base.DrawSwing 走通用绘制，
        /// 保证其余模式的绘制行为不变。
        /// </summary>
        public override void DrawSwing(SpriteBatch spriteBatch, Color lightColor)
        {
            if (Projectile.ai[0] == 6)
            {
                Texture2D value = ModContent.Request<Texture2D>("CalamityDemutation/Particles/Light").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.EntitySpriteDraw(value, Owner.Center - Main.screenPosition, null, Color.Red * 0.9f, Projectile.rotation, value.Size() * 0.5f, Projectile.scale * 2.15f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
            base.DrawSwing(spriteBatch, lightColor);
        }

        /// <summary>
        /// 蓄力旋转（ai[0]=6）的尘土表现：沿刀光弧线端点抛撒铜币尘（DustID.CopperCoin），
        /// 并按四个偏移方向生成向外飞散的尘，营造剑刃撕裂空气的效果。
        /// </summary>
        private void SpawnDust(Player player, int direction)
        {
            float rot = Projectile.rotation - MathF.PI / 4f * direction;
            Vector2 vector = Projectile.Center + (rot + (direction == -1 ? MathF.PI : 0f)).ToRotationVector2() * 200 * Projectile.scale;
            Vector2 vector2 = rot.ToRotationVector2();
            Vector2 vector3 = vector2.RotatedBy(MathF.PI / 2f * Projectile.spriteDirection);
            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustDirect(vector - new Vector2(5f), 10, 10, DustID.CopperCoin, player.velocity.X, player.velocity.Y, 150);
                dust.velocity = Projectile.SafeDirectionTo(dust.position) * 0.1f + dust.velocity * 0.1f;
            }
            for (int i = 0; i < 4; i++)
            {
                float speedRands = 1f;
                float modeRands = 1f;
                switch (i)
                {
                    case 1:
                        modeRands = -1f;
                        break;
                    case 2:
                        modeRands = 1.25f;
                        speedRands = 0.5f;
                        break;
                    case 3:
                        modeRands = -1.25f;
                        speedRands = 0.5f;
                        break;
                }
                if (!Main.rand.NextBool(6))
                {
                    Dust dust2 = Dust.NewDustDirect(Projectile.position, 0, 0, DustID.CopperCoin, 0f, 0f, 100);
                    dust2.position = Projectile.Center + vector2 * (180 * Projectile.scale + Main.rand.NextFloat() * 20f) * modeRands;
                    dust2.velocity = vector3 * (4f + 4f * Main.rand.NextFloat()) * modeRands * speedRands;
                    dust2.noGravity = true;
                    dust2.noLight = true;
                    dust2.scale = 0.5f;
                    if (Main.rand.NextBool(4))
                    {
                        dust2.noGravity = false;
                    }
                }
            }
        }

        /// <summary>
        /// 命中特效反馈：播放拔刀命中音效（MurasamaHitOrganic，音高 1.25），并按弹幕挥砍方向在目标处抛出 DRK_Spark 火花粒子。
        /// 火花基数 13，会随场上火花总数分档下调（120/220/350/500 以上分别降为 10/8/6/3），避免粒子过多导致卡顿。
        /// </summary>
        private void HitEffect(Entity target)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.MurasamaHitOrganic with { Pitch = 1.25f }, target.Center);
            int sparkCount = 13;
            Vector2 toTarget = Owner.Center.To(target.Center);
            Vector2 norlToTarget = toTarget.GetNormalVector();
            int ownerToTargetSetDir = Math.Sign(toTarget.X);
            ownerToTargetSetDir = ownerToTargetSetDir != DirSign ? -1 : 1;
            if (rotSpeed > 0)
            {
                norlToTarget *= -1;
            }
            if (rotSpeed < 0)
            {
                norlToTarget *= 1;
            }
            float rotToTargetSpeedSengs = rotSpeed * 3 * ownerToTargetSetDir;
            Vector2 rotToTargetSpeedTrengsVumVer = norlToTarget.RotatedBy(-rotToTargetSpeedSengs) * 13;
            if (Projectile.ai[0] == 3)
            {
                rotToTargetSpeedTrengsVumVer = Projectile.velocity.RotatedBy(rotToTargetSpeedSengs);
            }
            int pysCount = DRKLoader.GetParticlesCount(DRKLoader.GetParticleType(typeof(DRK_Spark)));
            if (pysCount > 120)
            {
                sparkCount = 10;
            }
            if (pysCount > 220)
            {
                sparkCount = 8;
            }
            if (pysCount > 350)
            {
                sparkCount = 6;
            }
            if (pysCount > 500)
            {
                sparkCount = 3;
            }
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 sparkVelocity2 = rotToTargetSpeedTrengsVumVer.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.3f, 1.6f);
                int sparkLifetime2 = Main.rand.Next(18, 30);
                float sparkScale2 = Main.rand.NextFloat(0.65f, 1.2f);
                Color sparkColor2 = Main.rand.NextBool(3) ? Color.OrangeRed : Color.DarkRed;
                if (Projectile.ai[0] == 0 || Projectile.ai[0] == 1)
                {
                    sparkVelocity2 *= 0.8f;
                    sparkScale2 *= 0.9f;
                    sparkLifetime2 = Main.rand.Next(13, 25);
                }
                else if (Projectile.ai[0] == 3)
                {
                    sparkVelocity2 *= 1.28f;
                }
                else if (Projectile.ai[0] == 4 || Projectile.ai[0] == 5)
                {
                    sparkVelocity2 *= 1.28f;
                    sparkScale2 *= 1.19f;
                    sparkLifetime2 = Main.rand.Next(23, 35);
                }
                DRKLoader.AddParticle(new DRK_Spark(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f)
                    + Projectile.velocity * 1.2f, sparkVelocity2, false, (int)(sparkLifetime2 * 1.2f), sparkScale2 * 1.4f, sparkColor2));
            }
        }

    }
}
