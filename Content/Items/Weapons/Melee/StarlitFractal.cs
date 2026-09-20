using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
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
    /// 星熠分形（StarlitFractal，移植自 CalamityEntropy）—— 分形系列的第六把武器，
    /// 由上一把「深渊分形」与叶绿大剑、真断钢剑、穿星之光、叶绿锭、星辉鳞尘合成而来。
    /// 本体既不显示也不判定（noUseGraphic / noMelee）：每两次挥砍额外出射一颗
    /// <see cref="FractalStar"/>，挥砍交给手持弹幕 StarlitFractalHeld。
    /// </summary>
    internal class StarlitFractal:ModItem
    {
        /// <summary>本次挥砍的朝向，1 与 -1 交替（传给弹幕时 0 记作 -1，本武器不会用到 2）</summary>
        private int atkType = 1;
        /// <summary>使用次数计数：每两次（奇数第 2、4、6…次）额外出射一颗分形之星</summary>
        private int useCount = 0;
        public override void SetDefaults()
        {
            Item.damage = 383;                             // 383 点近战伤害
            Item.crit = 7;                                 // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                               // 贴图宽（像素）
            Item.height = 60;                              // 贴图高（像素）
            Item.useTime = Item.useAnimation = 20;         // 使用时间/动画时长 20 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 20);          // 价值 20 金
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<StarlitFractalHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
        }
        /// <summary>每两次挥砍额外出射一颗分形之星，再生成手持弹幕并把本次朝向交给它</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (useCount++ % 2 == 1)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<FractalStar>(), damage, knockback, player.whoAmI);
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType *= -1;
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>
        /// 配方：星熠分形 + 叶绿大剑 + 真断钢剑 + 穿星之光 + 叶绿锭×4 + 星辉鳞尘×16 @ 秘银砧。
        /// CE 的「星辉鳞尘」是本工程没移植的材料（它承接灾厄的 StarblightSoot），按用户口径换成灾厄同级同名物：
        /// 现代版取 <c>StarblightSoot</c>（它的 <c>[LegacyName("Stardust")]</c> 正说明两版是同一件材料的换代改名）、
        /// 经典版取 <c>Stardust</c>，缺哪个版本就不注册哪条配方（写法同 NecklaceofVexation）。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && calamity.TryFind<ModItem>("StarblightSoot", out ModItem starblightSoot))
            {
                CreateRecipe().AddIngredient<AbyssFractal>()
                    .AddIngredient(ItemID.ChlorophyteClaymore)
                    .AddIngredient(ItemID.TrueExcalibur)
                    .AddIngredient(ItemID.PiercingStarlight)
                    .AddIngredient(ItemID.ChlorophyteBar, 4)
                    .AddIngredient(starblightSoot.Type, 16)
                    .AddTile(TileID.MythrilAnvil)
                    .Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1) && calamity1.TryFind<ModItem>("Stardust", out ModItem stardust))
            {
                CreateRecipe().AddIngredient<AbyssFractal>()
                    .AddIngredient(ItemID.ChlorophyteClaymore)
                    .AddIngredient(ItemID.TrueExcalibur)
                    .AddIngredient(ItemID.PiercingStarlight)
                    .AddIngredient(ItemID.ChlorophyteBar, 4)
                    .AddIngredient(stardust.Type, 16)
                    .AddTile(TileID.MythrilAnvil)
                    .Register();
            }
        }
    }
    /// <summary>
    /// 星熠分形手持弹幕（移植自 CalamityEntropy 的 StarlitFractalHeld）：贴身绘制剑体并左右挥砍。
    /// <para>
    /// ai[0] 决定挥砍朝向（<b>±1</b>，本武器只会传这两个值）。进度过 0.4 时朝挥砍的侧向甩出一发
    /// <see cref="FractalStarBlade"/> 分形星影；命中时挂太空感染减益、从天上砸下两颗 <see cref="AstralStarMelee"/> 星陨，
    /// 并按灾厄「真断钢」的粒子套路炸一圈火花、再撒 24 颗灾厄版发光火花。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（映射同前几把：
    /// powerwhip→FractalThrust、sf_use→FractalSwing、sf_hit→FractalSwingHit、FractalHit→FractalImpact）；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c> 换成 <c>Main.player[owner]</c> 与 TextureAssets，
    /// <c>CEExtraAssets.SemiCircularSmear</c> 换成本模组 Assets/ExtraTextures 下的同名贴图；
    /// ③ <c>CEUtils.CustomLerp2</c>、<c>RotateTowardsAngle</c>、<c>LineThroughRect</c>、<c>randomPointInCircle</c>、
    /// <c>normalize</c> 在 CE 侧属于工具库，这里内联，<c>CEUtils.WeapSound</c> 按 1.0 处理；
    /// ④ 去掉 CE 里只记录、从不读取的 odr 旋转历史与只写不读的 shoot 字段，以及配套的
    /// TrailingMode/TrailCacheLength 设置；⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，
    /// 改用 End + 立即模式 Begin(Additive)、画完再恢复默认批次；⑥ 减益不新建——现代版灾厄取
    /// <c>AstralInfectionDebuff</c>，经典版退回原版 <c>BuffID.CursedInferno</c>（与 FractalStarBlade 同款写法）；
    /// ⑦ 粒子换成 <see cref="GlowSparkCal"/>；⑧ CE 里 <c>ai[0] == 2</c> 的刺出式分支本武器永远走不到
    /// （物品只传 ±1），为对照 CE 保留原样。
    /// </para>
    /// </summary>
    internal class StarlitFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/StarlitFractal";
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
        /// <summary>本次挥砍是否已射出星影（一次挥砍只射一发）</summary>
        private bool spawnProj = true;
        /// <summary>本次挥砍是否已播过命中音（一次挥砍只播一次）</summary>
        private bool playHitSound = true;
        /// <summary>命中附加的减益：现代版灾厄取 AstralInfectionDebuff，其余情况为原版诅咒狱火</summary>
        private static int astralDebuffType = BuffID.CursedInferno;
        public override void SetStaticDefaults()
        {
            astralDebuffType = BuffID.CursedInferno;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection))
            {
                astralDebuffType = astralInfection.Type;
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
            Projectile.extraUpdates = 3;                 // 配合物品 20 帧使用时间 → 挥砍总帧数 80
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            counter++;
            // 进度刚过 0.4 时朝挥砍的侧向甩出一发星影（一次挥砍只射一发）
            if (Main.myPlayer == Projectile.owner && spawnProj && progress > 0.4f)
            {
                int dir = (int)Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                spawnProj = false;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 49,
                    Vector2.Normalize(Projectile.velocity.RotatedBy(dir * MathHelper.PiOver2)) * 3 + RandomPointInCircle(2),
                    ModContent.ProjectileType<FractalStarBlade>(), (int)(Projectile.damage * 0.925f), Projectile.knockBack * 4, Projectile.owner, Projectile.rotation, dir);
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
        /// 自绘：先按 dir 取贴图角为原点画剑体，再在加法混合下叠两层半圆拖尾（一层紫色、一层由白转蓝，
        /// 透明度都按进度平方衰减），最后恢复默认批次。
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
            float fade = 1f - progress * progress;
            Vector2 smearPos = Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(smear, smearPos, null, new Color(100, 50, 200) * fade * 0.7f, RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.64f, false) + MathHelper.ToRadians(36) * -dir, smear.Size() / 2f, Projectile.scale * 1.74f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(smear, smearPos, null, Color.Lerp(Color.White, Color.Blue, progress) * fade * 0.7f, RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.24f, false) + MathHelper.ToRadians(36) * -dir, smear.Size() / 2f, Projectile.scale * 1.56f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>命中判定是从玩家中心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * 126 * Projectile.scale * scale;
            float point = 0f;
            return targetHitbox.Contains((int)start.X, (int)start.Y) || targetHitbox.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 64, ref point);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 132 * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
        /// <summary>
        /// 命中时附加太空感染减益、从上方砸下两颗星陨、播一次命中音，并按灾厄「真断钢」的粒子套路
        /// 在敌怪中心炸一圈火花、再撒 24 颗灾厄版发光火花（沿挥砍方向扇形散开）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(astralDebuffType, 460);
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = target.Center + new Vector2(0, -900) + RandomPointInCircle(400);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, (target.Center - pos).SafeNormalize(Vector2.Zero) * 42,
                    ModContent.ProjectileType<AstralStarMelee>(), Projectile.damage / 4, Projectile.owner);
            }
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
            for (int i = 0; i < 24; i++)
            {
                GlowSparkCal spark = new GlowSparkCal();
                DRKLoader.NewParticle(spark, target.Center, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(1.4f) * Main.rand.NextFloat(8f, 42f), Color.BlueViolet, Main.rand.NextFloat(0.02f, 0.08f));
                spark.Configure(false, 14, new Vector2(0.4f, 1));
            }
        }
        /// <summary>CEUtils.CustomLerp2 的等价实现：以 (1-p)³ 为权重的 1→0 插值</summary>
        private static float CustomLerp2(float p) => float.Lerp(1f, 0f, (1f - p) * (1f - p) * (1f - p));
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
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
