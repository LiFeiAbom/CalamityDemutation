using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
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
    /// 深渊分形（AbyssFractal，移植自 CalamityEntropy）—— 分形系列的第五把武器，
    /// 由上一把「光辉分形」与死神镰刀、霜刃、水晶碎块合成而来。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），挥砍交给手持弹幕 AbyssFractalHeld。
    /// </summary>
    internal class AbyssFractal:ModItem
    {
        /// <summary>本次挥砍的朝向，1 与 -1 交替（传给弹幕时 0 记作 -1，本武器不会用到 2）</summary>
        private int atkType = 1;
        public override void SetDefaults()
        {
            Item.damage = 360;                             // 360 点近战伤害
            Item.crit = 7;                                 // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;                               // 贴图宽（像素）
            Item.height = 40;                              // 贴图高（像素）
            Item.useTime = Item.useAnimation = 24;         // 使用时间/动画时长 24 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 60);          // 价值 60 金
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AbyssFractalHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
            Item.ArmorPenetration = 15;                    // 护甲穿透 15 点
        }
        /// <summary>生成手持弹幕并把本次朝向交给它（0 记作 -1），然后在 1 与 -1 之间翻转（CE 原样，本武器不会传 2）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType *= -1;
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>配方：深渊分形 + 死神镰刀 + 霜刃 + 水晶碎块×8 @ 秘银砧</summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<BrilliantFractal>()
                .AddIngredient(ItemID.DeathSickle)
                .AddIngredient(ItemID.Frostbrand)
                .AddIngredient(ItemID.CrystalShard, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    /// <summary>
    /// 深渊分形手持弹幕（移植自 CalamityEntropy 的 AbyssFractalHeld）：贴身绘制剑体并左右挥砍。
    /// <para>
    /// ai[0] 决定挥砍朝向（<b>±1</b>，本武器只会传这两个值）。进度过 0.4 时朝挥砍的侧向扇形射出三发
    /// <see cref="AbyssalBullet"/> 渊水弹；进度过 0.46 时甩出一发 <see cref="FractalAbyssalBlade"/> 分形深渊刃
    /// （它会在起手结束时撕开一道 <see cref="AbyssalCrack"/>）。命中附加深海减益、炸一圈火花与深渊粒子。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（映射同前几把：
    /// powerwhip→FractalThrust、sf_use→FractalSwing、sf_hit→FractalSwingHit、FractalHit→FractalImpact）；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c> 换成 <c>Main.player[owner]</c> 与 TextureAssets，
    /// <c>CEExtraAssets.SemiCircularSmear</c> 换成本模组 Assets/ExtraTextures 下的同名贴图；
    /// ③ <c>CEUtils.CustomLerp2</c>、<c>RotateTowardsAngle</c>、<c>LineThroughRect</c>、<c>randomPointInCircle</c>、
    /// <c>GetOwner</c>、<c>randomPoint</c> 在 CE 侧属于工具库，这里内联，<c>CEUtils.WeapSound</c> 按 1.0 处理；
    /// ④ 去掉 CE 里只记录、从不读取的 odr 旋转历史与配套的 TrailingMode/TrailCacheLength 设置；
    /// ⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、
    /// 画完再恢复默认批次；⑥ 减益不新建——两版灾厄都有 <c>CrushDepth</c>（CE 挂的就是它），
    /// 各取各自现成的即可，经 <see cref="CalamityDemutationPlayer.ApplyCalamityBuff"/> 挂载；
    /// ⑦ CE 里 <c>ai[0] == 2</c> 的刺出式分支本武器永远走不到（物品只传 ±1），为对照 CE 保留原样。
    /// </para>
    /// </summary>
    internal class AbyssFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/AbyssFractal";
        /// <summary>普通挥砍的拖尾贴图（CE 的 CEExtraAssets.SemiCircularSmear）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/SemiCircularSmear";
        /// <summary>自身帧数计数（ai[0] 存的是朝向，不是计时器）</summary>
        private float counter = 0f;
        /// <summary>绘制缩放</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>本次挥砍是否已射出深渊刃（一次挥砍只射一发）</summary>
        private bool shoot = true;
        /// <summary>本次挥砍是否已射出渊水弹（一次挥砍只射一组）</summary>
        private bool spawnProj = true;
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
            Projectile.extraUpdates = 3;                 // 配合物品 24 帧使用时间 → 挥砍总帧数 96
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            counter++;
            // 进度刚过 0.4 时朝挥砍的侧向扇形射出三发渊水弹（一次挥砍只射一组）
            if (Main.myPlayer == Projectile.owner && spawnProj && progress > 0.4f)
            {
                int dir = (int)Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                spawnProj = false;
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 80,
                        Vector2.Normalize(Projectile.velocity.RotatedBy(dir * MathHelper.PiOver2 * 0.7f)) * 12 + RandomPointInCircle(5),
                        ModContent.ProjectileType<AbyssalBullet>(), Projectile.damage / 6, Projectile.knockBack, Projectile.owner);
                }
            }
            if (init)
            {
                float meleeScale = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
                if (Projectile.ai[0] == 2)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalThrust with { Volume = 0.6f }, Projectile.Center);
                }
                if (Projectile.ai[0] < 2)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = Projectile.ai[0] * 0.12f, Volume = 0.6f }, Projectile.Center);
                }
                init = false;
            }
            // 进度过 0.46 时甩出一发深渊刃（一次挥砍只射一发）
            if (progress > 0.46f && Projectile.owner == Main.myPlayer && shoot)
            {
                shoot = false;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * 0.16f,
                    ModContent.ProjectileType<FractalAbyssalBlade>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            Projectile.timeLeft = 3;                     // 每帧续命，收尾交给下面的 counter 判据
            const float rotF = 5.2f;
            alpha = 1f;
            scale = 1.6f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * CustomLerp2(progress)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
            Projectile.Center = owner.MountedCenter;
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
        /// 自绘：先按 dir 取贴图角为原点画剑体，再在加法混合下叠两层半圆拖尾
        /// （同为紫色，一层紧贴剑身、一层更大更淡，透明度按进度平方衰减），最后恢复默认批次。
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
            float progress = counter / (Main.player[Projectile.owner].itemTimeMax * Projectile.MaxUpdates);
            float offsetY = Main.player[Projectile.owner].gfxOffY;
            Main.EntitySpriteDraw(texture, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            Texture2D smear = ModContent.Request<Texture2D>(SmearTexture).Value;
            Color smearColor = new Color(100, 50, 200) * (1 - progress * progress) * 0.8f;
            Vector2 smearPos = Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(smear, smearPos, null, smearColor, RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.4f, false) + MathHelper.ToRadians(32) * -dir, smear.Size() / 2f, Projectile.scale * 1.74f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(smear, smearPos, null, smearColor * 0.6f, RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.1f, false) + MathHelper.ToRadians(32) * -dir, smear.Size() / 2f, Projectile.scale * 2f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>命中判定是从玩家中心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * 160 * Projectile.scale * scale;
            float point = 0f;
            return targetHitbox.Contains((int)start.X, (int)start.Y) || targetHitbox.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 64, ref point);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 156 * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
        /// <summary>
        /// 命中时附加深海减益、播一次命中音，并按灾厄「史莱姆变身」的粒子套路在敌怪中心炸一圈火花，
        /// 再从敌怪碰撞箱里撒出 64 颗深渊粒子（CE 原样，粒子会沿挥砍方向散开）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "CrushDepth", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 400);
            if (playHitSound)
            {
                playHitSound = false;
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwingHit, Projectile.Center);
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalImpact, Projectile.Center);
            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TownSlimeTransform, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
            for (int i = 0; i < 64; i++)
            {
                AbyssalParticle p = new AbyssalParticle();
                DRKLoader.NewParticle(p, RandomPoint(target.Hitbox), Projectile.velocity.RotatedByRandom(0.16f).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(8f, 48f), Color.White);
                p.vd = 0.92f;
                p.ad = 0.03f;
                p.Opacity = 0.38f * Main.rand.NextFloat(1.2f, 1.6f);
            }
        }
        /// <summary>CEUtils.CustomLerp2 的等价实现：以 (1-p)³ 为权重的 1→0 插值</summary>
        private static float CustomLerp2(float p) => float.Lerp(1f, 0f, (1f - p) * (1f - p) * (1f - p));
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
        /// <summary>CEUtils.randomPoint 的等价实现：矩形内均匀取一点</summary>
        private static Vector2 RandomPoint(Rectangle rect) => new Vector2(Main.rand.NextFloat(rect.X, rect.X + rect.Width), Main.rand.NextFloat(rect.Y, rect.Y + rect.Height));
        /// <summary>
        /// CEUtils.RotateTowardsAngle 的等价实现：把角度与目标角都折到 (-π, π] 后取最短转向量，
        /// useFixedSpeed 为真时限制每帧最大转角、为假时把转角按 rotateSpeed 比例缩放。
        /// </summary>
        private static float RotateTowardsAngle(float currentRadians, float targetRadians, float rotateSpeed, bool useFixedSpeed)
        {
            currentRadians = MathHelper.WrapAngle(currentRadians);
            targetRadians = MathHelper.WrapAngle(targetRadians);
            float turnAmount = MathHelper.WrapAngle(targetRadians - currentRadians);
            if (useFixedSpeed)
            {
                turnAmount = MathHelper.Clamp(turnAmount, -rotateSpeed, rotateSpeed);
            }
            else
            {
                turnAmount *= MathHelper.Clamp(rotateSpeed, 0f, 1f);
            }
            return currentRadians + turnAmount;
        }
    }
}
