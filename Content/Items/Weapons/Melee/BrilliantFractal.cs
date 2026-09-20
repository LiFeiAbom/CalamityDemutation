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
    /// 光辉分形（BrilliantFractal，移植自 CalamityEntropy）—— 分形系列的第四把武器，
    /// 由上一把「苍穹分形」与破坏者巨剑、断钢剑、炽焰巨剑合成而来。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），挥砍交给手持弹幕 BrilliantFractalHeld。
    /// </summary>
    internal class BrilliantFractal:ModItem
    {
        /// <summary>本次挥砍的朝向，1 与 -1 交替（传给弹幕时 0 记作 -1，本武器不会用到 2）</summary>
        private int atkType = 1;
        public override void SetDefaults()
        {
            Item.damage = 325;                             // 325 点近战伤害
            Item.crit = 5;                                 // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                               // 贴图宽（像素）
            Item.height = 60;                              // 贴图高（像素）
            Item.useTime = Item.useAnimation = 26;         // 使用时间/动画时长 26 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 20);          // 价值 20 金
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BrilliantFractalHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
            Item.ArmorPenetration = 5;                     // 护甲穿透 5 点
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
        /// <summary>配方：光辉分形 + 破坏者巨剑 + 断钢剑 + 炽焰巨剑 @ 秘银砧</summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<WelkinFractal>()
                .AddIngredient(ItemID.BreakerBlade)
                .AddIngredient(ItemID.Excalibur)
                .AddIngredient(ItemID.FieryGreatsword)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    /// <summary>
    /// 光辉分形手持弹幕（移植自 CalamityEntropy 的 BrilliantFractalHeld）：贴身绘制剑体并左右挥砍。
    /// <para>
    /// ai[0] 决定挥砍朝向（<b>±1</b>，本武器只会传这两个值）。挥砍中段（进度 0.36~0.64）按攻速累计，
    /// 每满 6 点沿剑尖方向 98 像素处召唤一颗 <see cref="FractalBlight"/> 光星；进度过 0.4 时朝挥砍的
    /// 侧向甩出一发 <see cref="FractalShadow"/> 剑影。命中时在敌怪中心炸一圈真断钢火花。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（映射同前几把：
    /// powerwhip→FractalThrust、sf_use→FractalSwing、sf_hit→FractalSwingHit、FractalHit→FractalImpact）；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c> 换成 <c>Main.player[owner]</c> 与 TextureAssets，
    /// <c>CEExtraAssets.SemiCircularSmear</c>/<c>StarTexture</c> 换成本模组 Assets/ExtraTextures 下的同名贴图，
    /// 星芒贴图的静态缓存改为按需请求；③ <c>CEUtils.GetRepeatedCosFromZeroToOne</c>、<c>LineThroughRect</c>、
    /// <c>randomPointInCircle</c>、<c>normalize</c> 在 CE 侧属于工具库，这里内联，<c>CEUtils.WeapSound</c> 按 1.0 处理；
    /// ④ 去掉 CE 里只记录、从不读取的 odr 旋转历史与 shoot 字段，以及配套的 TrailingMode/TrailCacheLength 设置；
    /// ⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、
    /// 画完再恢复默认批次；⑥ CE 里 <c>ai[0] == 2</c> 的刺出式分支本武器永远走不到（物品只传 ±1），为对照 CE 保留原样。
    /// </para>
    /// </summary>
    internal class BrilliantFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/BrilliantFractal";
        /// <summary>普通挥砍的拖尾贴图（CE 的 CEExtraAssets.SemiCircularSmear）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/SemiCircularSmear";
        /// <summary>剑尖星芒贴图（CE 的 CEExtraAssets.StarTexture）</summary>
        private const string StarTexture = "CalamityDemutation/Assets/ExtraTextures/StarTexture";
        /// <summary>自身帧数计数（ai[0] 存的是朝向，不是计时器）</summary>
        private float counter = 0f;
        /// <summary>绘制缩放</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>本次挥砍是否已射出剑影（一次挥砍只射一发）</summary>
        private bool spawnProj = true;
        /// <summary>光星召唤的攻速累计器，每满 6 点召一颗</summary>
        private float spawnProjCounter = 0f;
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
            Projectile.extraUpdates = 3;                 // 配合物品 26 帧使用时间 → 挥砍总帧数 104
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            counter++;
            // 进度刚过 0.4 时朝挥砍的侧向甩出一发剑影（一次挥砍只射一发）
            if (Main.myPlayer == Projectile.owner && spawnProj && progress > 0.4f)
            {
                int dir = (int)Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                spawnProj = false;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 49,
                    Vector2.Normalize(Projectile.velocity.RotatedBy(dir * MathHelper.PiOver2)) * 6 + RandomPointInCircle(2),
                    ModContent.ProjectileType<FractalShadow>(), Projectile.damage, Projectile.knockBack * 4, Projectile.owner, Projectile.rotation, dir);
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
            Projectile.timeLeft = 3;                     // 每帧续命，收尾交给下面的 counter 判据
            // 挥砍中段按攻速累计，每满 6 点沿剑尖方向 98 像素处召一颗光星
            if (progress > 0.36f && progress < 0.64f)
            {
                spawnProjCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
            }
            if (spawnProjCounter >= 6f)
            {
                spawnProjCounter -= 6f;
                if (Main.myPlayer == Projectile.owner)
                {
                    Vector2 spawnPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, RandomPointInCircle(0.1f),
                        ModContent.ProjectileType<FractalBlight>(), Projectile.damage / 6, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat() * 6.28f);
                }
            }
            const float rotF = 4f;
            alpha = 1f;
            scale = 1.6f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * RepeatCos01(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
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
        /// 自绘：先按 dir 取贴图角为原点画剑体，再在加法混合下叠一层半圆拖尾（白转金黄色、随递归余弦起伏）
        /// 与剑尖处两笔互相垂直的星芒，最后恢复默认批次。
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
            Texture2D star = ModContent.Request<Texture2D>(StarTexture).Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(smear, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null,
                Color.Lerp(Color.White, Color.LightGoldenrodYellow, progress) * (float)Math.Cos(RepeatCos01(progress, 3) * MathHelper.Pi - MathHelper.PiOver2),
                Projectile.rotation + MathHelper.ToRadians(32) * -dir, smear.Size() / 2f, Projectile.scale * 1.4f * scale, SpriteEffects.None, 0f);
            // 剑尖星芒：同一位置叠两笔窄条，分别在横向与纵向拉长
            Vector2 starPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale - Main.screenPosition;
            float starAlpha = (float)Math.Cos(progress * MathHelper.TwoPi - MathHelper.Pi) * 0.5f + 0.5f;
            Main.spriteBatch.Draw(star, starPos, null, Color.LightGoldenrodYellow * 0.7f * starAlpha, 0f, star.Size() / 2f, 0.36f * Projectile.scale * new Vector2(2.8f, 0.5f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(star, starPos, null, Color.LightGoldenrodYellow * 0.7f * starAlpha, 0f, star.Size() / 2f, 0.36f * Projectile.scale * new Vector2(0.5f, 2.8f), SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>命中判定是从玩家中心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * 110 * Projectile.scale * scale;
            float point = 0f;
            return targetHitbox.Contains((int)start.X, (int)start.Y) || targetHitbox.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 64, ref point);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 110 * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
        /// <summary>命中时播一次命中音，并按灾厄「真断钢」的粒子套路在敌怪中心炸一圈火花</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (playHitSound)
            {
                playHitSound = false;
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwingHit, Projectile.Center);
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalImpact, Projectile.Center);
            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TrueExcalibur, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
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
