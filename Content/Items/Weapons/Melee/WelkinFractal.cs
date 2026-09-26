using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 苍穹分形（WelkinFractal，移植自 CalamityEntropy）—— 分形系列的第三把武器，
    /// 由上一把「破碎分形」与星怒、养蜂人合成而来。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），挥砍交给手持弹幕 WelkinFractalHeld。
    /// </summary>
    internal class WelkinFractal:ModItem
    {
        /// <summary>本次挥砍的招式下标，0→1→2 循环；传给弹幕时 0 记作 -1，2 表示刺出式</summary>
        private int atkType = 0;
        public override void SetDefaults()
        {
            Item.damage = 36;
            Item.crit = 4;                                 // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                               // 贴图宽（像素）
            Item.height = 60;                              // 贴图高（像素）
            Item.useTime = Item.useAnimation = 18;         // 使用时间/动画时长 18 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 5;
            Item.value = Item.buyPrice(gold: 10);          // 价值 10 金
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WelkinFractalHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
        }
        /// <summary>生成手持弹幕并把本次招式交给它（0 记作 -1），伤害减半后再传（CE 原设定）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage / 2, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType++;
            if (atkType > 2)
            {
                atkType = 0;
            }
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>配方：破碎分形 + 星怒 + 养蜂人 @ 铁砧</summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<ShatteredFractal>()
                .AddIngredient(ItemID.Starfury)
                .AddIngredient(ItemID.BeeKeeper)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    /// <summary>
    /// 苍穹分形手持弹幕（移植自 CalamityEntropy 的 WelkinFractalHeld）：贴身绘制剑体并按三式循环挥动。
    /// <para>
    /// ai[0] 决定招式：<b>-1 / 1</b> 是左右两个方向的普通挥砍，期间按攻速累计召唤
    /// <see cref="FractalFeather"/> 羽毛从天而降；<b>2</b> 是向前刺出，一次射出 3 发
    /// <see cref="FractalBeam"/> 追踪光矛。命中时附加风寒类减益。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c>/<c>getTextureGlow()</c> 换成 <c>Main.player[owner]</c>、
    /// TextureAssets 与 ModContent.Request；③ <c>CEUtils.CustomLerp1</c> / <c>GetRepeatedCosFromZeroToOne</c>
    /// / <c>LineThroughRect</c> / <c>randomPointInCircle</c> 在 CE 侧属于工具库，这里内联；
    /// ④ 去掉 CE 里只记录、从不读取的 odr 旋转历史与 TrailingMode/TrailCacheLength 设置；
    /// ⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、
    /// 画完再恢复默认批次；⑥ <c>CEUtils.WeapSound</c> 按 1.0 处理；
    /// ⑦ 命中减益不新建 buff——现代版用灾厄的 WindChilled（风寒），经典版灾厄没有该 buff，退回原版霜火。
    /// </para>
    /// </summary>
    internal class WelkinFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/WelkinFractal";
        /// <summary>普通挥砍的拖尾贴图（CE 的 CEExtraAssets.SemiCircularSmear）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/SemiCircularSmear";
        /// <summary>命中附加的减益类型：现代版灾厄取 WindChilled（风寒），其余情况为原版霜火</summary>
        private static int chillDebuffType = BuffID.Frostburn;
        /// <summary>自身帧数计数（ai[0] 存的是招式下标，不是计时器）</summary>
        private float counter = 0f;
        /// <summary>绘制缩放</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>刺出式是否已射出光矛（每把剑只射一次）</summary>
        private bool shoot = true;
        /// <summary>羽毛召唤的攻速累计器，每满 16 点召一根</summary>
        private float spawnFeatherCounter = 0f;
        /// <summary>本次挥砍是否已播过命中音（一次挥砍只播一次）</summary>
        private bool playHitSound = true;
        public override void SetStaticDefaults()
        {
            // 风寒减益：现代版灾厄有 WindChilled 就直接用，经典版灾厄没有该 buff（也没有寒系 DoT），退回原版霜火
            chillDebuffType = BuffID.Frostburn;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && calamity.TryFind<ModBuff>("WindChilled", out ModBuff windChilled))
            {
                chillDebuffType = windChilled.Type;
            }
        }
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
            const float rotF = 4.4f;
            if (Projectile.ai[0] == 2)
            {
                // 刺出：一次射出 3 发追踪光矛，剑体沿朝向推出到极限后收回
                if (shoot)
                {
                    shoot = false;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                                Vector2.Normalize(Projectile.velocity) * 26 + RandomPointInCircle(10), ModContent.ProjectileType<FractalBeam>(),
                                Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                    }
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalShoot, Projectile.Center);
                }
                float l = (float)Math.Cos(progress * MathHelper.Pi - MathHelper.PiOver2);
                Projectile.rotation = Projectile.velocity.ToRotation();
                scale = 1f + l * 3f;
                alpha = l;
                Projectile.Center = owner.MountedCenter + Vector2.Normalize(Projectile.velocity) * (-34 + l * 34);
            }
            else
            {
                // 普通挥砍：按攻速累计召唤羽毛，每满 16 点从玩家上方 600 像素处放一根朝鼠标飞
                spawnFeatherCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
                if (spawnFeatherCounter >= 16f)
                {
                    spawnFeatherCounter -= 16f;
                    Vector2 spawnPos = Projectile.Center + new Vector2(0, 600) * Projectile.ai[0] + RandomPointInCircle(34);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, Vector2.Normalize(Main.MouseWorld - spawnPos) * 28,
                            ModContent.ProjectileType<FractalFeather>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat() * 6.28f);
                    }
                }
                alpha = 1f;
                scale = 1 * (1 + (float)Math.Cos(CustomLerp1(progress) * MathHelper.Pi - MathHelper.PiOver2) * 0.8f);
                Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * CustomLerp1(progress)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
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
        /// 自绘：普通挥砍先画一道加法混合的半圆拖尾（青转黄绿、随递归余弦起伏），
        /// 两种招式都会再叠一层同名的 Glow 贴图；最后按 dir 取贴图角为原点绘制剑体本身。
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
            float offsetY = Main.player[Projectile.owner].gfxOffY * Vector2.UnitY.Y;
            if (Projectile.ai[0] < 2)
            {
                Texture2D smear = ModContent.Request<Texture2D>(SmearTexture).Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(smear, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null,
                    Color.Lerp(new Color(50, 140, 160), new Color(200, 255, 66), counter / maxUpdateTime) * (float)Math.Cos(RepeatCos01(counter / maxUpdateTime, 3) * MathHelper.Pi - MathHelper.PiOver2) * 0.5f,
                    Projectile.rotation + MathHelper.ToRadians(32) * -dir, smear.Size() / 2f, Projectile.scale * 1.2f * scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Texture2D glowSwing = ModContent.Request<Texture2D>(Texture + "Glow").Value;
                Main.spriteBatch.Draw(glowSwing, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null,
                    Color.White * alpha, rot, origin, Projectile.scale * scale, effect, 0f);
            }
            else
            {
                Texture2D glowThrust = ModContent.Request<Texture2D>(Texture + "Glow").Value;
                Main.spriteBatch.Draw(glowThrust, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null,
                    Color.White * alpha * (float)(Math.Cos(RepeatCos01(counter / maxUpdateTime, 3) * MathHelper.Pi - MathHelper.PiOver2) * 0.5f + 0.5f),
                    rot, origin, Projectile.scale * scale * 1.4f * 0.34f, effect, 0f);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale, effect);
            return false;
        }
        /// <summary>命中判定是从玩家中心沿朝向伸出的一条线段（刺出式长度减半），不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * (86 * (Projectile.ai[0] == 2 ? 0.5f : 1f)) * Projectile.scale * scale;
            float point = 0f;
            return targetHitbox.Contains((int)start.X, (int)start.Y) || targetHitbox.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 64, ref point);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (86 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
        /// <summary>命中时附加风寒减益，播一次命中音，并按灾厄「真断钢」的粒子套路在敌怪中心炸一圈火花</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(chillDebuffType, 300);
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
        /// <summary>CEUtils.CustomLerp1 的等价实现：把余弦缓动除以 cos(0.6) 做归一化，使曲线两端恰好取到 0 与 1</summary>
        private static float CustomLerp1(float v)
        {
            const float j = 0.6f;
            return (float)((Math.Cos(v * (MathHelper.Pi + j) - MathHelper.Pi) * 0.5f + 0.5f) / Math.Cos(j));
        }
        /// <summary>CEUtils.GetRepeatedCosFromZeroToOne 的等价实现：把 [0,1] 的余弦缓动递归套用 repeat 次</summary>
        private static float RepeatCos01(float v, int repeat)
        {
            if (repeat <= 1)
                return (float)Math.Cos(v * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
            return (float)Math.Cos(RepeatCos01(v, repeat - 1) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
        }
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
}
