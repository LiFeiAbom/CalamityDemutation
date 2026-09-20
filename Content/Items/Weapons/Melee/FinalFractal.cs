using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Effects;
using CalamityDemutation.NPCs;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
    /// 最终分形（FinalFractal，移植自 CalamityEntropy）—— 分形系列的第十把，也是收官的一把，
    /// 由原版天顶剑、上一把「虚空分形」与至尊档材料合成。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），招式全交给手持弹幕 <see cref="FinalFractalHeld"/>。
    /// 出手节奏是十段一轮：四次左向挥砍（-1）、四次右向挥砍（1）、一次投掷本剑（2）、一次锁链全屏斩（3）；
    /// 右键则改为发动一次幽冥斩（<see cref="VoidSlash"/>，带 <c>ai[0] = 1</c>，往前突进并在途中朝两侧各甩出剑影，
    /// 由混乱状态充当 10 秒冷却）。
    /// <para>
    /// 与 CE 原版的差异：① <c>ModContent.RarityType&lt;AbyssalBlue&gt;</c> 换成工程的月后稀有度体系
    /// <c>postMoonLordRarity = 16</c>（= 品红，见 <c>CalamityDemutationGlobalItem.ModifyTooltips</c>），
    /// 基础稀有度统一填红；② 起手音走本模组的 <see cref="CalamityDemutationSounds"/>，音高按既有口径取 CE 值减 1，
    /// <c>CEUtils.WeapSound</c> 按 1.0；③ 配方按「灾厄同级材料替代」重做，见下一轮（配方与本地化同一轮落地）。
    /// </para>
    /// </summary>
    internal class FinalFractal:ModItem
    {
        /// <summary>本次挥砍的招式：0/2/4/6 → -1（左向挥砍）、1/3/5/7 → 1（右向挥砍）、8 → 2（投掷本剑）、9 → 3（锁链全屏斩），走 0~9 循环</summary>
        private int atkType = 0;
        public override void SetDefaults()
        {
            Item.damage = 4000;                            // 4000 点近战伤害
            Item.crit = 35;                                 // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                                // 贴图宽（像素）
            Item.height = 60;                               // 贴图高（像素）
            Item.useTime = Item.useAnimation = 22;          // 使用时间/动画时长 22 帧
            Item.useStyle = ItemUseStyleID.Shoot;           // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 7;
            Item.value = Item.buyPrice(platinum: 2, gold: 40); // 价值 2 铂金 40 金
            Item.rare = ItemRarityID.Red;                   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16; // 月后稀有度 16：名称染品红
            Item.UseSound = null;                           // 挥砍音由手持弹幕播放
            Item.noMelee = true;                            // 本体不做挥砍判定
            Item.noUseGraphic = true;                       // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FinalFractalHeld>();
            Item.shootSpeed = 12f;                          // 决定手持弹幕的朝向速度
            Item.ArmorPenetration = 100;                    // 护甲穿透 100 点
        }
        /// <summary>右键的幽冥斩以玩家的混乱状态当冷却（CE 原样）：处于混乱状态期间不能再次发动</summary>
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
        /// （投掷式与锁链斩的抛物线高度），然后把计数推进一格。
        /// 右键：给玩家挂 10 秒混乱状态当冷却，并生成一次带 <c>ai[0] = 1</c> 的 <see cref="VoidSlash"/> 幽冥斩
        /// （伤害是物品的 10 倍，CE 原样；这个 ai 值正是"突进途中朝两侧甩剑影"的开关）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.VoidSlashCharge, position);
                player.AddBuff(BuffID.ChaosState, 10 * 60);
                Projectile.NewProjectile(source, position, velocity * 4, ModContent.ProjectileType<VoidSlash>(), damage * 10, 0, player.whoAmI, 1);
                return false;
            }
            int at = 2;
            if (atkType == 0 || atkType == 2 || atkType == 4 || atkType == 6)
            {
                at = -1;
            }
            if (atkType == 1 || atkType == 3 || atkType == 5 || atkType == 7)
            {
                at = 1;
            }
            if (atkType == 9)
            {
                at = 3;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, at, 0, Main.MouseWorld.Distance(position) + 180);
            atkType += 1;
            if (atkType > 9)
            {
                atkType = 0;
            }
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>启用右键（发动幽冥斩）</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 配方：CE 原配方是 <c>Zenith + VoidFractal + FadingRunestone @ VoidWell</c>，其中 CE 自研的
        /// <c>FadingRunestone</c> 是「VoidScales×5 + VoidBar×10 @ VoidWell」的虚空材料堆，而
        /// <c>VoidScales</c>/<c>VoidBar</c> 全出自它自己的「巡游者」宝袋 —— 按拍板口径一律不移植，
        /// 改用灾厄至尊档的 <c>ShadowspecBar×10</c> 承接（量取 10 是照它原本 5 鳞 + 10 锭的堆量），
        /// 站台换成现代版 <c>CosmicAnvil</c> / 经典版 <c>DraedonsForge</c>。
        /// 两版灾厄**都有** <c>ShadowspecBar</c>（现代 <c>Items/Materials/ShadowspecBar.cs</c>、
        /// 经典 <c>Items/ShadowspecBar.cs</c>，后者被恶魔阴影套、Animus、Apotheosis 等 20 条配方引用），
        /// 所以两版配方只差站台。两版各注册一条、用到哪一版的材料就注册哪条（写法同 <see cref="VoidFractal"/>）。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                CreateRecipe().AddIngredient(ItemID.Zenith)
                    .AddIngredient<VoidFractal>()
                    .AddIngredient(shadowspecBar.Type, 10)
                    .AddTile(cosmicAnvil.Type)
                    .Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1)
                && calamity1.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar)
                && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                CreateRecipe().AddIngredient(ItemID.Zenith)
                    .AddIngredient<VoidFractal>()
                    .AddIngredient(classicShadowspecBar.Type, 10)
                    .AddTile(draedonsForge.Type)
                    .Register();
            }
        }
    }
    /// <summary>
    /// 最终分形手持弹幕（移植自 CalamityEntropy 的 FinalFractalHeld）：贴身绘制剑体，按 <c>ai[0]</c> 走四路招式。
    /// <para>
    /// <b>ai[0] == ±1（左右挥砍）</b>：剑身贴住玩家挥舞（ai[0] 兼作旋向系数），进度过 0 时一次性从光标周围
    /// 1200~2400 像素的环形上召出 <b>6 把</b> <see cref="FinalFractalBlade"/>（全额伤害）朝光标飞去。
    /// <b>ai[0] == 2（投掷本剑）</b>：剑身沿抛物线被甩向光标方向（<c>scale</c> 与 <c>alpha</c> 随进度先胀后收），
    /// 出手瞬间在 <c>Center + 归一化速度 × 1100</c> 处生出一道 <see cref="FractalLaser"/>（伤害 ÷2）并闪一记全屏白
    /// （<c>FlashEffectStrength = 0.3</c>）。计数走 1/帧（普通挥砍是 2.5/帧），于是这一式明显更慢。
    /// <b>ai[0] == 3（锁链全屏斩）</b>：剑体先沿抛物线甩到 <c>ai[2]</c> 指定的远处，命中任一敌人后就把它记进
    /// <see cref="OnNPC"/> 并开始 140 帧的"钉住"：这期间计数冻结（改成 <see cref="OnNPCTime"/> 倒计时）、剑体贴住目标
    /// 并实时更新链长，同时把 <c>counter</c> 强行拉到动作中段；绘制时用 <see cref="DrawChain"/> 从剑心到玩家拉一条锁链。
    /// 该式的本地无敌帧被 <see cref="CanHitNPC"/> 抬到 20 帧，所以能对同一个目标连续多段命中。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（映射：sf_use→FractalSwing、
    /// sf_shoot→FractalShoot、sf_hit→FractalSwingHit、FractalHit→FractalImpact、runesonghit→RuneSongHit），
    /// 音高按既有口径取 CE 值减 1（0.6 → -0.4、0.75 → -0.25、1 + 0×0.12 → 0），<c>CEUtils.WeapSound</c> 按 1.0；
    /// ② 命中挂的虚空侵蚀（CE 的 <c>EGlobalNPC.AddVoidTouch</c>）走 <see cref="CalamityDemutationGlobalNPC.AddVoidTouch"/>
    /// （第九把时就已按第一轮口径进工程），不新建 buff；③ 粒子换成第一轮落地的
    /// <see cref="ShineParticle"/> 与 <see cref="LightParticle"/>；
    /// ④ <c>CalamityEntropy.FlashEffectStrength</c> → <c>CalamityDemutation.FlashEffectStrength</c>（工程已有）；
    /// ⑤ 刀光着色器走 <see cref="CDShaders.FinalFracShader"/>：CE 是先在 Immediate 批次里手动
    /// <c>pass.Apply()</c> 再 <c>DrawUserPrimitives</c>（不是把 Effect 传给 Begin），本模组照抄这个次序，
    /// 并用 End + 默认批次 Begin 替代 CE 的 <c>ExitShaderRegion</c>；
    /// ⑥ 投掷式召剑影时 CE 读 <c>owner.mouseWorld()</c>（那边自带归属守卫）→ 直读 <c>Main.MouseWorld</c> 即等价；
    /// ⑦ <c>CEUtils</c> 工具一律内联（<c>GetRepeatedCosFromZeroToOne</c>→<see cref="RepeatCos01"/>、
    /// <c>Parabola</c>→<see cref="Parabola"/>、<c>normalize</c>→SafeNormalize、<c>GetOwner</c>→<c>Main.player[owner]</c>、
    /// <c>getDistance</c>→<c>Vector2.Distance</c>、<c>randomRot</c>→<c>MathHelper.TwoPi</c> 随机角、
    /// <c>drawChain</c>→<see cref="DrawChain"/>、<c>LineThroughRect</c>→<see cref="CDUtil.LineThroughRect"/>）；
    /// ⑧ 删掉 CE 里从未被读取的 <c>spawnProjCounter</c> 与 <c>spawnProj</c> 两个字段，以及 PreDraw 里两个算完没用到的局部量。
    /// </para>
    /// <para>
    /// <b>CE 原状、刻意照抄不改的一处</b>：PreDraw 末尾的 <c>phantomAlpha</c> 恒为 0（<c>texAlpha</c> 恒为 1），
    /// 也就是说加亮贴图层实际画不出来 —— 这与 CE 的 BrilliantFractal 画星体时把 alpha 归零同源，属 CE 原状，不是移植笔误。
    /// </para>
    /// </summary>
    internal class FinalFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/FinalFractal";
        /// <summary>锁链贴图（CE 的 Assets/Extra/FFChain），只有锁链全屏斩（ai[0] == 3）会画</summary>
        private const string ChainTexture = "CalamityDemutation/Assets/ExtraTextures/FFChain";
        /// <summary>刀光底图（CE 的 CEExtraAssets.MotionTrail2），交给 FinalFrac 着色器当权重图采样</summary>
        private const string TrailTexture = "CalamityDemutation/Assets/ExtraTextures/MotionTrail2";
        /// <summary>剑体旋转角历史：普通挥砍式每帧记一笔、只留最近 20 笔，绘制时摊成三角带当刀光</summary>
        private readonly List<float> odr = new List<float>();
        /// <summary>自身帧数计数（ai[0] 存的是招式，不是计时器）；普通挥砍每帧加 2.5、投掷式与锁链斩加 1</summary>
        private float counter = 0f;
        /// <summary>绘制缩放（各招式自己的基准值）</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>本式是否已出手（挥砍式甩 6 把剑影、投掷式射激光，各只触发一次）</summary>
        private bool shoot = true;
        /// <summary>锁链斩的抛物线高度（初始 1200，钉住敌人后改成剑到玩家的实时距离）</summary>
        private float length = 1200f;
        /// <summary>锁链斩钉住的敌人（首个命中者），未钉住时为 null</summary>
        private NPC OnNPC = null;
        /// <summary>钉住状态的剩余帧数（每帧 -1），归零后解除钉住、继续走完动作</summary>
        private int OnNPCTime = 140;
        /// <summary>本式是否已播过命中音（挥砍式首次命中播一次）</summary>
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
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = -1;         // 默认同一次动作对同一敌人只结算一次（锁链斩会临时改）
            Projectile.timeLeft = 100000;                // 占位值，真正的结束判据是 counter
            Projectile.extraUpdates = 9;                 // 配合物品 22 帧使用时间 → 挥砍总帧数 198
            Projectile.light = 1f;
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            // 锁链斩钉住敌人的这 140 帧里计数被冻结，改成给钉住计时器倒计时（CE 原样）
            if (!(Projectile.ai[0] == 3 && OnNPC != null && OnNPCTime > 0))
            {
                // 普通挥砍计数走得最快（2.5/帧），投掷式与锁链斩是 1/帧，动作因此被拉长
                counter += 1 * (Projectile.ai[0] == 3 ? 1f : Projectile.ai[0] == 2 ? 1 : 2.5f);
            }
            else
            {
                OnNPCTime--;
            }
            if (init)
            {
                float meleeScale = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
                if (Projectile.ai[0] == 2)
                {
                    // 掷剑式：轻一点的挥剑音 + 一记"掷出"音
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = -0.4f, Volume = 0.8f }, Projectile.Center);
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
                // 掷剑式：出手瞬间朝面前 1100 像素处甩出一道分形激光，并闪一记全屏白
                if (shoot)
                {
                    shoot = false;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 1100,
                            Projectile.velocity.SafeNormalize(Vector2.Zero) * 10, ModContent.ProjectileType<FractalLaser>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
                    }
                    CalamityDemutation.FlashEffectStrength = 0.3f;
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalShoot, Projectile.Center);
                }
                // 剑体沿抛物线被甩出去（l 是 0→1→0 的余弦包络，控制"胀出去再收回"）
                float l = (float)Math.Cos(progress * MathHelper.Pi - MathHelper.PiOver2);
                Projectile.rotation = Projectile.velocity.ToRotation();
                scale = 1f + l * 2f;
                alpha = l;
                Projectile.Center = owner.MountedCenter + Projectile.velocity.SafeNormalize(Vector2.Zero) * (-34 + l * 34);
            }
            else
            {
                if (Projectile.ai[0] == 3)
                {
                    // 锁链斩：先沿抛物线甩到 ai[2] 指定的远处，钉住敌人后改为贴住目标并实时量链长
                    Projectile.Resize(64, 64);
                    Projectile.Center = owner.MountedCenter + Projectile.velocity.SafeNormalize(Vector2.Zero) * Parabola(progress, length);
                    if (OnNPC != null && OnNPCTime > 0)
                    {
                        Projectile.Center = OnNPC.Center;
                        length = Vector2.Distance(owner.Center, OnNPC.Center);
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((OnNPC.Center - owner.Center).ToRotation());
                        if (!OnNPC.active)
                        {
                            OnNPCTime = 0;
                        }
                        // 把进度强行拉到动作中段，于是钉住期间剑体一直悬在目标身上
                        counter = maxUpdateTimes / 2f;
                    }
                    Projectile.rotation = (Projectile.Center - owner.Center).ToRotation();
                    alpha = 1f;
                    scale = 2f;
                }
                else
                {
                    // 普通挥砍：出手瞬间从光标周围的环形上召 6 把剑影，朝光标飞去
                    if (shoot)
                    {
                        shoot = false;
                        if (Projectile.owner == Main.myPlayer)
                        {
                            int type = ModContent.ProjectileType<FinalFractalBlade>();
                            for (int i = 0; i < 6; i++)
                            {
                                Vector2 spawnPos = Main.MouseWorld + Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.Next(1200, 2400);
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, (Main.MouseWorld - spawnPos) * 0.01f * Main.rand.NextFloat(0.8f, 1.2f), type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                            }
                        }
                    }
                    const float rotF = 4f;
                    alpha = 1f;
                    scale = 1.8f;
                    Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * RepeatCos01(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                    Projectile.rotation = Projectile.rotation.ToRotationVector2().ToRotation();
                    Projectile.Center = owner.MountedCenter;
                    odr.Add(Projectile.rotation);
                    if (odr.Count > 20)
                    {
                        odr.RemoveAt(0);
                    }
                }
            }
            owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            if (counter > maxUpdateTimes)
            {
                Projectile.Kill();
                owner.itemTime = 1;
                owner.itemAnimation = 1;
            }
        }
        public override bool ShouldUpdatePosition() => false;
        /// <summary>锁链斩把本地无敌帧抬到 20 帧，好让它贴住敌人时连续多段命中（CE 原样，写在判据里）</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[0] == 3)
            {
                Projectile.localNPCHitCooldown = 2 * 10;
            }
            return null;
        }
        /// <summary>
        /// 命中时：炸一颗 <see cref="ShineParticle"/>；若本次是锁链斩，则把首个命中的敌人记为钉住目标、
        /// 在目标处炸 6 颗 <see cref="LightParticle"/> 并补一记命中音；随后给敌人叠虚空侵蚀，
        /// 挥砍式只播一次命中音（投掷式不播，CE 原样），最后炸一圈真断钢火花。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ShineParticle shine = new ShineParticle();
            DRKLoader.NewParticle(shine, target.Center, Vector2.Zero, new Color(225, 200, 255), 0.6f);
            shine.Configure(1f, true, ShineParticle.DrawModeEnum.AdditiveBlend, 0f, 12);
            if (Projectile.ai[0] == 3 && OnNPCTime > 0 && OnNPC == null)
            {
                OnNPC = target;
            }
            if (Projectile.ai[0] == 3)
            {
                scale = 2f;
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 ver = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-12, 12);
                        LightParticle light = new LightParticle();
                        DRKLoader.NewParticle(light, target.Center, ver, new Color(220, 180, 255), Main.rand.NextFloat(1.3f, 1.7f));
                        light.Configure(0.15f, lifetime: 60);
                    }
                }
                SoundEngine.PlaySound(CalamityDemutationSounds.RuneSongHit with { Pitch = Main.rand.NextFloat(0.6f, 1.4f) - 1f }, target.Center);
            }
            CalamityDemutationGlobalNPC.AddVoidTouch(target, 40, 1.4f, 600, 16);
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
        /// <summary>
        /// 自绘：锁链斩先拉一条锁链；然后若剑体旋转角历史够长（普通挥砍式才记），就在 Immediate 批次下
        /// 手动应用 FinalFrac 着色器、把历史角摊成三角带当刀光画出去；
        /// 最后按 dir 取贴图角为原点画剑体与加亮层（后者 CE 原状为全透明），并恢复默认批次。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D phantom = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            if (Projectile.ai[0] == 3)
            {
                DrawChain(Projectile.Center, owner.Center, 18, ModContent.Request<Texture2D>(ChainTexture).Value);
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(220, 200, 255);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + new Vector2(240 * Projectile.scale, 0).RotatedBy(odr[i]),
                      new Vector3(i / (odr.Count - 1f), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition,
                      new Vector3(i / (odr.Count - 1f), 0, 1),
                      b));
            }
            if (ve.Count >= 3)
            {
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = CDShaders.FinalFracShader.Value;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue(new Color(220, 200, 255).ToVector4());
                shader.Parameters["color1"].SetValue(new Color(100, 100, 150).ToVector4());
                shader.CurrentTechnique.Passes["EffectPass"].Apply();
                GraphicsDevice gd = Main.spriteBatch.GraphicsDevice;
                gd.Textures[0] = ModContent.Request<Texture2D>(TrailTexture).Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            float texAlpha = 1;
            float phantomAlpha = 0;
            int dir = (int)(Projectile.ai[0] <= 1 ? Projectile.ai[0] : -1) * (Projectile.velocity.X > 0 ? -1 : 1);
            if (Projectile.ai[0] == 2)
            {
                dir = Math.Sign(Projectile.velocity.X);
            }
            Vector2 origin = dir > 0 ? new Vector2(0, texture.Height) : new Vector2(texture.Width, texture.Height);
            if (Projectile.ai[0] == 3)
            {
                origin = texture.Size() / 2f;
            }
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;
            Vector2 center = Projectile.Center + owner.gfxOffY * Vector2.UnitY - Main.screenPosition;
            Main.EntitySpriteDraw(texture, center, null, lightColor * alpha * texAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            Main.EntitySpriteDraw(phantom, center, null, Color.White * alpha * phantomAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            return false;
        }
        /// <summary>命中判定是从剑心沿朝向伸出的一条线段（长度按缩放放大），不是碰撞箱自带的圆形判定；锁链斩交给默认判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[0] == 3)
            {
                return null;
            }
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
        /// <summary>
        /// CEUtils.drawChain 的等价实现：沿两点连线按 <paramref name="spacing"/> 的间隔逐节画锁链贴图，
        /// 每一节都按所在位置取一次地块光照（所以锁链会随环境明暗变化）。
        /// </summary>
        private static void DrawChain(Vector2 startPos, Vector2 endPos, int spacing, Texture2D texture)
        {
            int distance = (int)Math.Sqrt(Math.Pow(endPos.X - startPos.X, 2) + Math.Pow(endPos.Y - startPos.Y, 2));
            float rot = (endPos - startPos).ToRotation();
            int num = distance / spacing;
            Vector2 addVec = new Vector2((endPos.X - startPos.X) / num, (endPos.Y - startPos.Y) / num);
            addVec.Normalize();
            Vector2 drawPos = startPos;
            for (int i = 0; i <= num; i++)
            {
                Color color = Lighting.GetColor((drawPos / 16).ToPoint());
                Main.EntitySpriteDraw(texture, drawPos - Main.screenPosition, null, color, rot, new Vector2(texture.Width / 2f, texture.Height / 2f), Vector2.One, SpriteEffects.None, 0);
                drawPos.X += addVec.X * spacing;
                drawPos.Y += addVec.Y * spacing;
            }
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
    }
}
