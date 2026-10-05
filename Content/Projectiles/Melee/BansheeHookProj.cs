using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 女妖之爪·手持体（BansheeHookProj） - 移植自灾厄大修（CalamityOverhaul）0.4.0.1.3 的重制版，
    /// 长矛 AI 本体照灾厄 <c>BaseSpearProjectile</c> 的 <c>GhastlyGlaiveSpear</c> 分支逐行重写（本工程不引用灾厄类型）。
    /// <para>
    /// <c>ai[1] == 0</c>（左键）：按长矛的"画圈突刺"轨迹挥舞；挥到一半朝前方散出 4 枚哀怨之镰，
    /// 收钩瞬间再甩出一枚 75% 伤害的镰刀，命中目标时炸出女妖爆裂。
    /// </para>
    /// <para>
    /// <c>ai[1] == 1</c>（右键引导）：先原地蓄能 60 帧（每 20 帧环射 7 枚镰刀、每 10 帧撒 7 枚鬼火），
    /// 蓄满后爪子跟手悬停，每 20 帧从鼠标位置拉出 3 枚追杀敌人的惊惧巨镰、每 15 帧在爪尖补鬼火，
    /// 蓄能耗尽自动收招。
    /// </para>
    /// <para>
    /// 联机口径：只有主人端做鼠标读取、蓄能读写与弹幕生成，位置/ai 随弹幕自身的同步包走；
    /// 蓄能条与爪尖星光只在主人端绘制（源把蓄能存在物品上，本工程按规范改存 ModPlayer）。
    /// </para>
    /// </summary>
    internal class BansheeHookProj : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/BansheeHookProj";
        /// <summary>长矛挥出的整体位移量（对应源的 TravelSpeed）</summary>
        private const float TravelSpeed = 22f;
        /// <summary>蓄能上限（对应源的 MeleeCharge 满值 500）</summary>
        private const float MaxCharge = 500f;
        /// <summary>每帧蓄能量：60 帧正好蓄满（源 8.333f）</summary>
        private const float ChargePerFrame = 8.333f;
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 90;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.ownerHitCheck = true;      // 命中判定需要视线（照源）
            Projectile.hide = true;               // 不参与常规绘制层，由 DrawBehind 留空
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.alpha = 255;
        }
        private Player Owner => Main.player[Projectile.owner];
        /// <summary>当前手持物确实是女妖之爪（引导模式用它挡掉其它物品意外发射同型弹幕，照源）</summary>
        private bool HoldingHook => Owner != null && Owner.active && Owner.HeldItem.type == ModContent.ItemType<BansheeHook>();
        private CalamityDemutationPlayer ModPlayer => Owner.GetModPlayer<CalamityDemutationPlayer>();
        /// <summary>蓄能条淡入用的本地透明度（0~255，每帧 +5）</summary>
        private int drawUIalp = 0;
        public override void AI()
        {
            if (Projectile.ai[1] == 0f)
                SpearBehavior();
            else
                ChannelBehavior();
        }
        /// <summary>
        /// 左键长矛轨迹（对应源 BaseSpearProjectile 的 GhastlyGlaiveSpear 分支）+ 重制版的两处镰刀散射。
        /// </summary>
        private void SpearBehavior()
        {
            Player player = Owner;
            if (player == null || !player.active)
            {
                Projectile.Kill();
                return;
            }
            Vector2 relativePoint = player.RotatedRelativePoint(player.MountedCenter, true);
            Projectile.direction = player.direction;
            player.heldProj = Projectile.whoAmI;
            Projectile.Center = relativePoint;
            if (player.dead)
            {
                Projectile.Kill();
                return;
            }
            if (!player.frozen)
            {
                // 收钩瞬间的一次性特效：甩出一枚哀怨之镰（源的 EffectBeforeReelback，源为 75% 伤害，
                // 用户 2026-10-03 指定改为 100% 面板伤害）
                if (player.itemAnimation < player.itemAnimationMax / 3 && Projectile.localAI[0] == 0f && Projectile.owner == Main.myPlayer)
                {
                    Projectile.localAI[0] = 1f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity * 0.5f, Projectile.velocity * 0.8f,
                        ModContent.ProjectileType<BansheeHookScythe>(), Projectile.damage, Projectile.knockBack * 0.85f, Projectile.owner);
                }
                Projectile.spriteDirection = Projectile.direction = player.direction;
                if (Projectile.alpha > 0)
                {
                    Projectile.alpha -= 127;
                    if (Projectile.alpha < 0)
                        Projectile.alpha = 0;
                }
                if (Projectile.localAI[0] > 0f)
                    Projectile.localAI[0] -= 1f;
                float inverseAnimationCompletion = 1f - player.itemAnimation / (float)player.itemAnimationMax;
                float velocityDirection = Projectile.velocity.ToRotation();
                float velocitySpeed = Projectile.velocity.Length();
                // 位置沿"假想圆"推进，ai[0] 提供侧向摆幅，形成长矛的画圈突刺
                Vector2 flatVelocity = Vector2.UnitX.RotatedBy(MathHelper.Pi + inverseAnimationCompletion * MathHelper.TwoPi)
                    * new Vector2(velocitySpeed, Projectile.ai[0]);
                Projectile.position += flatVelocity.RotatedBy(velocityDirection)
                    + new Vector2(velocitySpeed + TravelSpeed, 0f).RotatedBy(velocityDirection);
                Vector2 destination = relativePoint + flatVelocity.RotatedBy(velocityDirection)
                    + velocityDirection.ToRotationVector2() * (velocitySpeed + TravelSpeed + 40f);
                Projectile.rotation = player.AngleTo(destination) + MathHelper.PiOver4 * player.direction;
                if (Projectile.spriteDirection == -1)
                    Projectile.rotation += MathHelper.Pi;
            }
            // 重制版：挥到一半时朝正前方 ±20° 散出 4 枚哀怨之镰
            if (player.itemAnimation == player.itemAnimationMax / 2 && Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 vr = Projectile.velocity.UnitVector().RotatedBy(MathHelper.ToRadians(-20 + 10 * i)) * 10f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vr,
                        ModContent.ProjectileType<BansheeHookScythe>(), Projectile.damage, Projectile.knockBack, Main.myPlayer);
                }
            }
            if (player.itemAnimation == 2)
            {
                Projectile.Kill();
                player.reuseDelay = 2;
            }
        }
        /// <summary>
        /// 右键引导：ai[2] = 0 蓄能（原地转爪）→ ai[2] = 1 输出（跟手悬停 + 从鼠标处拉出惊惧巨镰）。
        /// </summary>
        private void ChannelBehavior()
        {
            Projectile.velocity = Vector2.Zero;
            Player owner = Owner;
            if (owner == null || !owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            if (!HoldingHook)
            {
                Projectile.Kill();
                return;
            }
            Projectile.localAI[1]++;
            owner.heldProj = Projectile.whoAmI;
            if (Projectile.owner == Main.myPlayer)
            {
                // 手臂朝爪子方向摆出（照源的手部姿势计算）
                float safeGravDir = Math.Sign(owner.gravDir);
                float rot = (MathHelper.PiOver2 * safeGravDir - owner.Center.To(Projectile.Center).ToRotation())
                    * owner.direction * safeGravDir * safeGravDir;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rot * -owner.direction * safeGravDir);
                owner.direction = owner.Center.To(Projectile.Center).X > 0 ? 1 : -1;
                if (PlayerInput.Triggers.Current.MouseRight)
                    Projectile.timeLeft = 2;      // 按住右键就不结束
            }
            if (Projectile.ai[2] == 0)
            {
                Projectile.Center = CDUtil.GetPlayerStabilityCenter(owner);
                Projectile.rotation += MathHelper.ToRadians(25);
                drawUIalp += 5;
                if (drawUIalp > 255)
                    drawUIalp = 255;
                if (Projectile.owner == Main.myPlayer)
                {
                    ModPlayer.bansheeHookCharge += ChargePerFrame;
                    // 环射镰刀：源为每 20 帧 7 枚、50% 伤害；用户 2026-10-03 指定改成 10 枚、100% 伤害
                    if (Projectile.localAI[1] % 20 == 0)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce, Projectile.Center);
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 vr = RandomVectorInDegrees(0f, 360f, 25f);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, vr,
                                ModContent.ProjectileType<BansheeHookScythe>(), Projectile.damage, 0f, owner.whoAmI);
                        }
                    }
                    // 鬼火环：源为每 10 帧 7 枚；用户 2026-10-03 指定改成 10 枚（均分角度同步改为 10 份）
                    if (Projectile.localAI[1] % 10 == 0)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 vr = (MathHelper.TwoPi / 10 * i).ToRotationVector2() * 10f;
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.Center, vr,
                                ModContent.ProjectileType<SpiritFlame>(), Projectile.damage / 3, 0f, owner.whoAmI, 1f);
                        }
                    }
                }
                if (Projectile.localAI[1] > 60)
                {
                    ModPlayer.bansheeHookCharge = MaxCharge;
                    Projectile.ai[2] = 1;
                    Projectile.localAI[1] = 0;
                    Projectile.netUpdate = true;
                }
            }
            if (Projectile.ai[2] == 1 && Projectile.owner == Main.myPlayer)
            {
                Vector2 stabilityCenter = CDUtil.GetPlayerStabilityCenter(owner);
                Vector2 toMous = stabilityCenter.To(ModPlayer.GetMouseWorld()).UnitVector();
                Vector2 hoverPos = toMous * 56f + stabilityCenter;
                Projectile.Center = Vector2.Lerp(hoverPos, Projectile.Center, 0.01f);
                Projectile.rotation = toMous.ToRotation();
                Projectile.localAI[2]++;
                ModPlayer.bansheeHookCharge--;
                // 从鼠标位置拉出惊惧巨镰（源为面板的 50%，用户 2026-10-03 指定改为 100%）
                if (Projectile.localAI[1] > 10 && Projectile.localAI[1] % 20 == 0)
                {
                    SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce with { Pitch = 0.35f, Volume = 0.7f }, Projectile.Center);
                    int damages = owner.GetWeaponDamage(owner.HeldItem);
                    Vector2 mouseWorld = ModPlayer.GetMouseWorld();
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spanPos = mouseWorld + RandomVectorInDegrees(0f, 360f, 160f);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), spanPos, spanPos.To(mouseWorld).UnitVector() * 15f,
                            ModContent.ProjectileType<AbominateHookScythe>(), damages, 0f, owner.whoAmI);
                    }
                }
                // 爪尖补鬼火
                if (Projectile.localAI[1] > 10 && Projectile.localAI[1] % 15 == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 45f * Projectile.scale
                            + RandomVectorInDegrees(0f, 360f, Main.rand.Next(2, 16));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Vector2.Zero,
                            ModContent.ProjectileType<SpiritFlame>(), Projectile.damage / 2, 0f, owner.whoAmI);
                    }
                }
                if (ModPlayer.bansheeHookCharge <= 0)
                {
                    Projectile.ai[2] = 0;
                    Projectile.localAI[1] = 0;
                    Projectile.netUpdate = true;
                    ModPlayer.bansheeHookCharge = 0f;
                    SoundEngine.PlaySound(CalamityDemutationSounds.MeatySlashSound, Projectile.Center);
                    SoundEngine.PlaySound(CalamityDemutationSounds.BloodflareRangerActivation, Projectile.Center);
                }
            }
        }
        /// <summary>
        /// 长矛轨迹之外的余韵尘土：爪身周围上飘的尘 + 按爪身朝向拖出的一串尘（源放在 BaseSpearProjectile.ExtraBehavior 里）
        /// </summary>
        public override void PostAI()
        {
            if (Projectile.ai[1] != 0f)
                return;
            Player player = Owner;
            Vector2 vector = player.RotatedRelativePoint(player.MountedCenter);
            float num = player.itemAnimation / (float)player.itemAnimationMax;
            float num2 = (1f - num) * (MathF.PI * 2f);
            float num3 = Projectile.velocity.ToRotation();
            float num4 = Projectile.velocity.Length();
            Vector2 spinningpoint = Vector2.UnitX.RotatedBy(MathF.PI + num2) * new Vector2(num4, Projectile.ai[0]);
            Vector2 destination = vector + spinningpoint.RotatedBy(num3) + new Vector2(num4 + TravelSpeed + 40f, 0f).RotatedBy(num3);
            Vector2 vector2 = player.SafeDirectionTo(destination, Vector2.UnitX * player.direction);
            Vector2 vector3 = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.Center, 14, 14, DustID.RedTorch, 0f, 0f, 110);
                dust.velocity = player.SafeDirectionTo(dust.position) * 2f;
                dust.position = Projectile.Center + vector3.RotatedBy(num2 * 2f + i / 2f * (MathF.PI * 2f)) * 10f;
                dust.scale = 1f + Main.rand.NextFloat(0.6f);
                dust.velocity += vector3 * 3f;
                dust.noGravity = true;
            }
            if (Main.rand.NextBool(3))
            {
                Dust dust2 = Dust.NewDustDirect(Projectile.Center, 20, 20, DustID.RedTorch, 0f, 0f, 110);
                dust2.velocity = player.SafeDirectionTo(dust2.position) * 2f;
                dust2.position = Projectile.Center + vector2 * -110f;
                dust2.scale = 0.45f + Main.rand.NextFloat(0.4f);
                dust2.fadeIn = 0.7f + Main.rand.NextFloat(0.4f);
                dust2.noGravity = true;
                dust2.noLight = true;
            }
        }
        /// <summary>
        /// 爪身绘制：左键模式按长矛贴图（左右朝向取 Alt 贴图），右键模式绕爪心旋转 45° 绘制。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Projectile.spriteDirection == -1
                ? ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Melee/BansheeHookAlt").Value
                : ModContent.Request<Texture2D>(Texture).Value;
            if (Projectile.ai[1] == 0)
            {
                Vector2 position = Projectile.position + new Vector2(Projectile.width, Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
                Vector2 origin = new Vector2(Projectile.spriteDirection == 1 ? texture.Width + 8f : -8f, -8f);
                Main.EntitySpriteDraw(texture, position, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }
            else
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
                    Projectile.rotation + MathHelper.PiOver4, texture.Size() / 2f, Projectile.scale, SpriteEffects.None);
            }
            return false;
        }
        /// <summary>
        /// 发光层 + 蓄能条 + 爪尖星光（蓄能条与星光只在主人端画，见类注释）
        /// </summary>
        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = Projectile.spriteDirection == -1
                ? ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Melee/BansheeHookAltGlow").Value
                : ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Melee/BansheeHookProjGlow").Value;
            if (Projectile.ai[1] == 0)
            {
                Vector2 position = Projectile.position + new Vector2(Projectile.width, Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
                Vector2 origin = new Vector2(Projectile.spriteDirection == 1 ? texture.Width + 8f : -8f, -8f);
                Main.EntitySpriteDraw(texture, position, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None);
            }
            else
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
                    Projectile.rotation + MathHelper.PiOver4, texture.Size() / 2f, Projectile.scale, SpriteEffects.None);
            }
            if (Projectile.owner == Main.myPlayer)
            {
                DrawChargeBar();
                DrawStar();
            }
        }
        /// <summary>爪尖旋转星光：三层不同转速/缩放的红/白/金五角星，叠加混合绘制</summary>
        private void DrawStar()
        {
            if (Projectile.localAI[2] == 0)
                return;
            Texture2D star = ModContent.Request<Texture2D>(CalamityDemutationConstant.Masking + "StarTexture_White").Value;
            Vector2 pos = CDUtil.GetPlayerStabilityCenter(Owner) + Projectile.rotation.ToRotationVector2() * 45f * Projectile.scale - Main.screenPosition;
            int time = (int)Projectile.localAI[2];
            int slp = Math.Min(time * 5, 255);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < 5; i++)
                Main.spriteBatch.Draw(star, pos, null, Color.Red, MathHelper.ToRadians(time * 5 + i * 17), star.Size() / 2f, slp / 1755f, SpriteEffects.None, 0);
            for (int i = 0; i < 5; i++)
                Main.spriteBatch.Draw(star, pos, null, Color.White, MathHelper.ToRadians(time * 6 + i * 17), star.Size() / 2f, slp / 2055f, SpriteEffects.None, 0);
            for (int i = 0; i < 5; i++)
                Main.spriteBatch.Draw(star, pos, null, Color.Gold, MathHelper.ToRadians(time * 9 + i * 17), star.Size() / 2f, slp / 2355f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        /// <summary>蓄能条：在主人头顶 135 像素处按蓄能比例填充中间的条（贴图取 CWR 的 FrightEnergyCharge 三件套）</summary>
        private void DrawChargeBar()
        {
            if (Projectile.ai[1] != 1)
                return;
            Texture2D back = ModContent.Request<Texture2D>(CalamityDemutationConstant.UI + "FrightEnergyChargeBack").Value;
            Texture2D bar = ModContent.Request<Texture2D>(CalamityDemutationConstant.UI + "FrightEnergyChargeBar").Value;
            Texture2D top = ModContent.Request<Texture2D>(CalamityDemutationConstant.UI + "FrightEnergyChargeTop").Value;
            float scale = 3f;
            int offsetWidth = 4;
            Vector2 drawPos = Owner.Center + new Vector2(bar.Width / -2f * scale, 135f) - Main.screenPosition;
            float alpha = drawUIalp / 255f;
            float chargeRatio = MathHelper.Clamp(ModPlayer.bansheeHookCharge / MaxCharge, 0f, 1f);
            Rectangle barRec = new Rectangle(offsetWidth, 0, (int)((bar.Width - offsetWidth * 2) * chargeRatio), bar.Height);
            Main.EntitySpriteDraw(back, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bar, drawPos + new Vector2(offsetWidth, 0f) * scale, barRec, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(top, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
        /// <summary>
        /// 命中盒：从爪心朝"爪尖"方向拉一条 -95 像素的线段，宽度按爪身缩放（照源的 Colliding）
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float rotation = Projectile.rotation - MathF.PI / 4f * Math.Sign(Projectile.velocity.X) + (Projectile.spriteDirection == -1 ? MathF.PI : 0f);
            float length = -95f;
            float collisionPoint = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + rotation.ToRotationVector2() * length, (TravelSpeed + 1f) * Projectile.scale, ref collisionPoint))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 命中时在目标处炸出女妖爆裂（25% 伤害，威力倍率随机 0.85~2.0），只在主人端生成
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<BansheeHookBoom>(), (int)(hit.Damage * 0.25), 10f, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
            }
        }
        /// <summary>PvP 命中：与 OnHitNPC 同构——在玩家身上炸出一枚女妖爆裂（伤害取本次命中的 25%）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<BansheeHookBoom>(), (int)(info.Damage * 0.25), 10f, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
            }
        }
        /// <summary>按角度区间随机取方向向量（对应源的 CWRUtils.GetRandomVevtor）</summary>
        private static Vector2 RandomVectorInDegrees(float startAngle, float targetAngle, float length)
        {
            float radians = MathHelper.ToRadians(startAngle + (targetAngle - startAngle) * Main.rand.NextFloat());
            return new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * length;
        }
    }
}
