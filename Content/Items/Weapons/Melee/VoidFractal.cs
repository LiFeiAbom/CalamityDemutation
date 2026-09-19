using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.NPCs;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
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
    /// 虚空分形（VoidFractal，移植自 CalamityEntropy）—— 分形系列的第九把武器。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），招式全交给手持弹幕 <see cref="VoidFractalHeld"/>。
    /// 出手节奏是八段一轮：挥砍（-1）、挥砍（1）、挥砍（-1）、挥砍（1）、挥砍（-1）、挥砍（1）、投掷（2）、
    /// 全屏斩（3）；右键则改为发动一次虚空斩突进（<see cref="VoidSlash"/>，由混乱状态充当 10 秒冷却）。
    /// <para>
    /// 与 CE 原版的差异：① <c>ModContent.RarityType&lt;VoidPurple&gt;</c> 换成工程的月后稀有度体系
    /// <c>postMoonLordRarity = 15</c>（= 紫色，见 <c>CalamityDemutationGlobalItem.ModifyTooltips</c>），
    /// 基础稀有度统一填红；② 右键起手音走 <see cref="CalamityDemutationSounds.VoidSlashCharge"/>（CE 的 VoidAnticipation），
    /// 音高按既有口径取 CE 值减 1（CE 传 1 → 本模组用默认的 0）；③ 配方按「灾厄同级材料替代」重做，见 <see cref="AddRecipes"/>。
    /// </para>
    /// </summary>
    internal class VoidFractal:ModItem
    {
        /// <summary>本次挥砍的招式：0/2/4 → -1（左向挥砍）、1/3/5 → 1（右向挥砍）、6 → 2（投掷本剑）、7 → 3（全屏斩），走 0~7 循环</summary>
        private int atkType = 0;
        public override void SetDefaults()
        {
            Item.damage = 600;                              // 600 点近战伤害
            Item.crit = 10;                                 // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                                // 贴图宽（像素）
            Item.height = 60;                               // 贴图高（像素）
            Item.useTime = Item.useAnimation = 20;          // 使用时间/动画时长 20 帧
            Item.useStyle = ItemUseStyleID.Shoot;           // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 6;
            Item.value = Item.buyPrice(platinum: 2, gold: 40); // 价值 2 铂金 40 金
            Item.rare = ItemRarityID.Red;                   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15; // 月后稀有度 15：名称染紫
            Item.UseSound = null;                           // 挥砍音由手持弹幕播放
            Item.noMelee = true;                            // 本体不做挥砍判定
            Item.noUseGraphic = true;                       // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<VoidFractalHeld>();
            Item.shootSpeed = 12f;                          // 决定手持弹幕的朝向速度
            Item.ArmorPenetration = 30;                     // 护甲穿透 30 点
        }
        /// <summary>右键的虚空斩以玩家的混乱状态当冷却（CE 原样）：处于混乱状态期间不能再次发动</summary>
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2 && player.chaosState)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 左键：按 <see cref="atkType"/> 算出本次招式（-1/1/2/3）交给手持弹幕，<c>ai[2]</c> 取玩家到光标的距离 + 180
        /// （投掷式与旋挥式的抛物线高度），然后把计数推进一格。
        /// 右键：给玩家挂 10 秒混乱状态当冷却，并生成一次 <see cref="VoidSlash"/> 突进
        /// （伤害是物品的 25 倍，CE 原样）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.VoidSlashCharge, position);
                player.AddBuff(BuffID.ChaosState, 10 * 60);
                Projectile.NewProjectile(source, position, velocity * 4, ModContent.ProjectileType<VoidSlash>(), damage * 25, 0, player.whoAmI);
                return false;
            }
            int at = 2;
            if (atkType == 0 || atkType == 2 || atkType == 4)
            {
                at = -1;
            }
            if (atkType == 1 || atkType == 3 || atkType == 5)
            {
                at = 1;
            }
            if (atkType == 7)
            {
                at = 3;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, at, 0, Main.MouseWorld.Distance(position) + 180);
            atkType += 1;
            if (atkType > 7)
            {
                atkType = 0;
            }
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>启用右键（发动虚空斩）</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 配方（第三轮按「灾厄同级材料替代」落地）：CE 原配方是
        /// <c>聚魂分形 + VoidAnnihilate + VoidBar×5 @ VoidWell</c>，这三件都是 CE 自研物
        /// （出自它自己的「巡游者」宝袋与自研站台），按拍板口径一律不移植 ——
        /// <c>VoidBar×5</c> 用灾厄同档的 <c>AuricBar×5</c> 承接（经典版灾厄没有 AuricBar，改取 <c>AuricOre×25</c>，
        /// 即 5 锭 = 25 矿，同 <see cref="ArkoftheCosmos"/> 的写法），<c>VoidAnnihilate</c> 用 <c>CosmiliteBar×5</c> 承接，
        /// 站台 <c>VoidWell</c> 换成灾厄的月后站台（现代 <c>CosmicAnvil</c> / 经典 <c>DraedonsForge</c>）。
        /// 两版各注册一条、用到哪一版的材料就注册哪条（写法同 <see cref="StarlitFractal"/>）。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar)
                && calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                CreateRecipe().AddIngredient<SpiritFractal>()
                    .AddIngredient(auricBar.Type, 5)
                    .AddIngredient(cosmiliteBar.Type, 5)
                    .AddTile(cosmicAnvil.Type)
                    .Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1)
                && calamity1.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                && calamity1.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                CreateRecipe().AddIngredient<SpiritFractal>()
                    .AddIngredient(auricOre.Type, 25)
                    .AddIngredient(classicCosmiliteBar.Type, 5)
                    .AddTile(draedonsForge.Type)
                    .Register();
            }
        }
    }
    /// <summary>
    /// 虚空分形手持弹幕（移植自 CalamityEntropy 的 VoidFractalHeld）：贴身绘制剑体，按 <c>ai[0]</c> 走四路招式。
    /// <para>
    /// <b>ai[0] == ±1（左右挥砍）</b>：剑身贴住玩家挥舞（ai[0] 兼作旋向系数），进度过 0.2 时朝前甩出一发
    /// <see cref="VoidWave"/>（全额伤害）。
    /// <b>ai[0] == 2（投掷本剑）</b>：剑身绕玩家旋挥约 4 圈、并沿抛物线被甩到光标方向，全程按攻速刷新各敌人的
    /// 本地无敌帧（旋挥才能连续命中），中段按攻速累计、每满 15 点沿剑尖召一颗 <see cref="VoidStarF"/>
    /// （伤害 ÷3，攻击类型改判为近战）。这一式计数走半速（counter += 0.5），整套耗时翻倍。
    /// <b>ai[0] == 3（全屏斩）</b>：剑贴住玩家绕约 4.8 圈转一圈多，命中时从目标中心纵向喷出一道
    /// <see cref="FractalLaser"/>（伤害 ÷9），且本次命中的伤害被抬到 <c>SetCrit + 最终伤害×1.4</c>。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（映射同前几把：
    /// sf_use→FractalSwing、sf_hit→FractalSwingHit、FractalHit→FractalImpact；本把新增
    /// CastTriangles→VoidFractalThrow、VoidAttack→VoidStrikeHit），音高按既有口径取 CE 值减 1
    /// （0.6 → -0.4、0.75 → -0.25、1 + ai[0]·0.12 → ai[0]·0.12），<c>CEUtils.WeapSound</c> 按 1.0；
    /// ② 命中走的虚空侵蚀（CE 的 <c>EGlobalNPC.AddVoidTouch</c>）已按第一轮口径进
    /// <see cref="CalamityDemutationGlobalNPC.AddVoidTouch"/>，不新建 buff；
    /// ③ CE 生成 VoidStarF 时用 InnoVault 的 <c>.ToProj().DamageType</c> 把它从默认的魔法改判成近战
    /// （贴图与烟雾配色都会读这个分支），本模组没有该扩展，改按 <see cref="ArkoftheAncients"/> 的既有写法
    /// 取回弹幕实例再赋值；④ <c>CEUtils</c> 工具一律内联（<c>GetRepeatedCosFromZeroToOne</c>→<see cref="RepeatCos01"/>、
    /// <c>Parabola</c>→<see cref="Parabola"/>、<c>randomPointInCircle</c>→<see cref="RandomPointInCircle"/>、
    /// <c>normalize</c>→SafeNormalize、<c>GetOwner</c>→<c>Main.player[owner]</c>、<c>GetTexture</c>→TextureAssets、
    /// <c>getTextureGlow</c>→贴图路径加 Glow、<c>LineThroughRect</c>→<see cref="CDUtil.LineThroughRect"/>）；
    /// ⑤ 删掉 CE 里只写不读的 <c>odr</c> 旋转历史与配套的 TrailingMode/TrailCacheLength 设置；
    /// ⑥ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)，
    /// 画完再恢复默认批次。
    /// </para>
    /// </summary>
    internal class VoidFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/VoidFractal";
        /// <summary>刀光贴图（CE 的 CEExtraAssets.SemiCircularSmear）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/SemiCircularSmear";
        /// <summary>叠在半圆拖尾上的第二笔白色刀痕（CE 的 CEExtraAssets.SlashSmear）</summary>
        private const string SlashSmearTexture = "CalamityDemutation/Assets/ExtraTextures/SlashSmear";
        /// <summary>自身帧数计数（ai[0] 存的是招式，不是计时器）；投掷式每帧加半、全屏斩每帧加 0.7</summary>
        private float counter = 0f;
        /// <summary>绘制缩放（各招式自己的基准值）</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>挥砍式是否已甩出虚空波（一次挥砍只甩一发）</summary>
        private bool shoot = true;
        /// <summary>掷剑式的星辰召唤累计器，每满 15 点沿剑尖召一颗</summary>
        private float spawnProjCounter = 0f;
        /// <summary>本次挥砍是否已播过命中音（挥砍式首次命中播，投掷式每次命中都播）</summary>
        private bool playHitSound = true;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;                        // 命中判定完全由 Colliding 的线段接管，碰撞箱取最小
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;       // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = -1;          // 同一次挥砍对同一敌人只结算一次
            Projectile.timeLeft = 100000;                 // 占位值，真正的结束判据是 counter
            Projectile.extraUpdates = 9;                  // 配合物品 20 帧使用时间 → 挥砍总帧数 180
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            // 投掷式与全屏斩的计数走得慢，动作因此被拉长（CE 原样）
            counter += 1 * (Projectile.ai[0] == 3 ? 0.7f : (Projectile.ai[0] == 2 ? 0.5f : 1));
            if (init)
            {
                float meleeScale = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
                if (Projectile.ai[0] == 2)
                {
                    // 掷剑式：剑体放大 1.3 倍，并额外叠一记"掷出"音
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = -0.4f, Volume = 0.8f }, Projectile.Center);
                    Projectile.scale *= 1.3f;
                    SoundEngine.PlaySound(CalamityDemutationSounds.VoidFractalThrow, Projectile.Center);
                }
                if (Projectile.ai[0] < 2)
                {
                    // 两次挥砍音高不同（ai[0] 为 ±1），形成交替感
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = Projectile.ai[0] * 0.12f, Volume = 0.6f }, Projectile.Center);
                }
                if (Projectile.ai[0] == 3)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = -0.25f }, Projectile.Center);
                }
                init = false;
            }
            Projectile.timeLeft = 3;                     // 每帧续命，收尾交给下面的 counter 判据
            if (Projectile.ai[0] == 2)
            {
                // 掷剑式：全程把各敌人的本地无敌帧按攻速刷新，使旋挥能连续命中
                int immunity = (int)(Projectile.MaxUpdates * 4 / owner.GetTotalAttackSpeed(Projectile.DamageType));
                for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                {
                    if (Projectile.localNPCImmunity[i] == -1)
                    {
                        Projectile.localNPCImmunity[i] = immunity;
                    }
                }
                Projectile.localNPCHitCooldown = immunity;
                // 中段按攻速累计，每满 15 点沿剑尖方向 48 像素处召一颗虚空新星（阈值减 6 是 CE 原状，非笔误）
                const float rotF = MathHelper.Pi / 180f * 280f + MathHelper.TwoPi * 3;
                if (progress > 0.3f && progress < 0.7f)
                {
                    spawnProjCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
                }
                if (spawnProjCounter >= 15f)
                {
                    spawnProjCounter -= 6f;
                    Vector2 spawnPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 48 * scale * Projectile.scale;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        int star = Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, RandomPointInCircle(0.1f) + Projectile.rotation.ToRotationVector2() * 8,
                            ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
                        // VoidStarF 默认是魔法系，这里改判为近战（CE 用 InnoVault 的 .ToProj().DamageType 做同一件事）
                        Main.projectile[star].DamageType = DamageClass.Melee;
                    }
                }
                alpha = 1f;
                scale = 2.4f;
                // 从 -140° 起转，绕满 3 整圈 + 280°，按出手方向决定旋向
                Projectile.rotation = Projectile.velocity.ToRotation() + (MathHelper.ToRadians(-140f) + rotF * RepeatCos01(progress, 1)) * (Projectile.velocity.X > 0 ? 1 : -1);
                // 剑体不跟随速度方向，而是沿抛物线被甩到 ai[2] 指定的高度（relative 坐标，故 ShouldUpdatePosition 为假）
                Projectile.Center = owner.MountedCenter + Projectile.velocity.SafeNormalize(Vector2.Zero) * Parabola(progress, Projectile.ai[2]);
                owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - owner.Center).ToRotation() - MathHelper.PiOver2);
            }
            else if (Projectile.ai[0] == 3)
            {
                // 全屏斩：剑体贴住玩家、绕约 4.8 圈的慢挥
                const float rotF = 4.8f;
                alpha = 1f;
                scale = 3.5f;
                Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * RepeatCos01(progress, 4)) * -1 * (Projectile.velocity.X > 0 ? -1 : 1);
                Projectile.Center = owner.MountedCenter;
                owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            }
            else
            {
                // 普通挥砍：剑体贴住玩家，ai[0] 的符号决定从哪一侧挥出
                const float rotF = 4f;
                alpha = 1f;
                scale = 1.8f;
                Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * RepeatCos01(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                Projectile.Center = owner.MountedCenter;
                if (progress > 0.2f && shoot)
                {
                    // 进度过 0.2 时朝前甩出一发虚空波（一次挥砍只甩一发）
                    shoot = false;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                        Projectile.velocity.SafeNormalize(Vector2.Zero) * 12 + RandomPointInCircle(6),
                        ModContent.ProjectileType<VoidWave>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                if (progress < 0.6f)
                {
                    // 前 60% 才把剑举在手上，之后收招
                    owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
                    owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
                }
            }
            // 挥砍式前 60% 出手、投掷式与全屏斩全程出手（CE 原样）
            if (progress < 0.6f || Projectile.ai[0] > 1)
            {
                owner.heldProj = Projectile.whoAmI;
                owner.itemTime = 2;
                owner.itemAnimation = 2;
            }
            if (counter > maxUpdateTimes)
            {
                Projectile.Kill();
                if (Projectile.ai[0] <= 1)
                {
                    owner.itemTime = 2;
                    owner.itemAnimation = 2;
                }
            }
        }
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 命中时给敌怪叠虚空侵蚀、播命中音（挥砍式只播第一次命中，投掷式每次都播）；
        /// 若本次是全屏斩（ai[0] == 3），额外从目标中心纵向喷出一道 <see cref="FractalLaser"/>（伤害 ÷9）
        /// 并补一记全屏斩命中音。最后按灾厄「真断钢」的粒子套路炸一圈火花。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationGlobalNPC.AddVoidTouch(target, 40, 1.4f, 600, 16);
            if (playHitSound || Projectile.ai[0] == 2)
            {
                playHitSound = false;
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwingHit, Projectile.Center);
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalImpact, Projectile.Center);
                if (Projectile.ai[0] == 3)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, new Vector2(0, 8).RotatedByRandom(1),
                        ModContent.ProjectileType<FractalLaser>(), Projectile.damage / 9, 0, Projectile.owner);
                    SoundEngine.PlaySound(CalamityDemutationSounds.VoidStrikeHit, Projectile.Center);
                }
            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TrueExcalibur, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
        }
        /// <summary>全屏斩（ai[0] == 3）把本次命中抬成必定暴击、最终伤害与来源伤害各 ×1.4</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 3)
            {
                modifiers.SetCrit();
                modifiers.FinalDamage *= 1.4f;
                modifiers.SourceDamage *= 1.4f;
            }
        }
        /// <summary>
        /// 自绘：先按 dir 取贴图角为原点画剑体、再叠一层同尺寸的发光贴图
        /// （挥砍式两者按进度此消彼长、掷剑式发光恒定半透明、全屏斩只画发光层）；
        /// 随后在加法混合下叠半圆拖尾与白色刀痕两笔，最后恢复默认批次。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTime = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTime;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D phantom = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            float texAlpha = 1;
            float phantomAlpha = 0;
            if (Projectile.ai[0] <= 1)
            {
                texAlpha = 1 - progress;
                phantomAlpha = progress;
            }
            if (Projectile.ai[0] == 2)
            {
                phantomAlpha = 0.5f;
                texAlpha = 1;
            }
            if (Projectile.ai[0] == 3)
            {
                phantomAlpha = 1;
                texAlpha = 0;
            }
            int dir = (int)(Projectile.ai[0] <= 1 ? Projectile.ai[0] : -1) * (Projectile.velocity.X > 0 ? -1 : 1);
            if (Projectile.ai[0] == 2)
            {
                dir = Math.Sign(Projectile.velocity.X);
            }
            Vector2 origin = dir > 0 ? new Vector2(0, texture.Height) : new Vector2(texture.Width, texture.Height);
            if (Projectile.ai[0] == 2)
            {
                origin = texture.Size() * 0.5f;
            }
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;
            float offsetY = owner.gfxOffY;
            Vector2 center = Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition;
            Main.EntitySpriteDraw(texture, center, null, lightColor * alpha * texAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            Main.EntitySpriteDraw(phantom, center, null, Color.White * alpha * phantomAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            Texture2D bs = ModContent.Request<Texture2D>(SmearTexture).Value;
            Texture2D ss = ModContent.Request<Texture2D>(SlashSmearTexture).Value;
            Color color1 = new Color(20, 20, 255);
            Color color2 = new Color(102, 20, 255);
            float zScale = Projectile.ai[0] == 2 ? 0.5f : 1f;
            float zAlpha = (float)Math.Cos(RepeatCos01(counter / maxUpdateTime, 3) * MathHelper.Pi - MathHelper.PiOver2);
            if (Projectile.ai[0] == 2)
            {
                zAlpha = 1f;
            }
            float smearRot = Projectile.rotation + MathHelper.ToRadians(32f) * -dir;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(bs, center, null, Color.Lerp(color1, color2, counter / maxUpdateTime) * zAlpha, smearRot, bs.Size() / 2f, Projectile.scale * 1.9f * scale * zScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ss, center, null, Color.White * zAlpha, smearRot, ss.Size() / 2f, Projectile.scale * 1.9f * scale * zScale, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>命中判定是从剑心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 152 * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 160 * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
        /// <summary>CEUtils.GetRepeatedCosFromZeroToOne 的等价实现：把 0→1 的余弦波反复折 <paramref name="repeat"/> 次</summary>
        private static float RepeatCos01(float v, int repeat)
        {
            if (repeat <= 1)
            {
                return (float)Math.Cos(v * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
            }
            return (float)Math.Cos(RepeatCos01(v, repeat - 1) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
        }
        /// <summary>CEUtils.Parabola 的等价实现：在 t=0 与 t=1 归零、t=0.5 达到 height 的抛物线</summary>
        private static float Parabola(float t, float height) => 4f * height * t * (1f - t);
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
}
