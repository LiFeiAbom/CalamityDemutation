using CalamityDemutation.Common.Effects;
using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Players;
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
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 泓渊亡铭（Erebodrepanon，移植自 CalamityEntropy 的 <c>Content/Items/Weapons/Erebodrepanon.cs</c>）：
    /// 挥砍式巨镰——本体既不显示也不判定，持握与挥砍全部交给手持弹幕 <see cref="ErebodrepanonHeld"/>；
    /// 三次挥砍一轮，第三次（段位 2，计数器在玩家身上）伤害翻倍、判定更长，命中时还会在敌怪身上留下
    /// <see cref="ErebodrepanonMark"/>。命中一律施加生命压制（LifeOppress）。
    /// <para>
    /// 与 CE 原版的差异：
    /// ① 配方按用户要求重做（CE 原配方是 StarWrath + WyrmTooth×12 + FadingRunestone @ 深渊祭坛，那三样本模组都没有；
    ///    现改为 死神擢升 + 宇宙暗流 + 猎魂鲨牙×12 + 魔影锭×5 @ 嘉登熔炉，两版灾厄分别注册，见 <see cref="AddRecipes"/>）；
    /// ② 稀有度按用户口径上调到「魔影档」：CE 自研的 <c>AbyssalBlue</c>（#6A28BE）改成灾厄 <c>HotPink</c>
    ///    （Rarity16，魔影锭 / 魔影套 / Fabstaff 用的就是它）对应的本工程 <c>postMoonLordRarity = 16</c>——
    ///    名称染品红，与魔影套、天罚、中子系同档；价值仍照 CE 的 3 铂金 20 金（与同档的天罚同价）；
    /// ③ 源把三次挥击的计数放在 ModItem 字段 <c>UseCount</c> 上，而该字段是**全类型共享**的——单机无碍，
    ///    联机时两名玩家同拿一把镰刀会互相打乱段位。本工程把计数挪进
    ///    <c>CalamityDemutationPlayer.erebodrepanonUseCount</c>，按玩家各存一份（说明见那个 partial）。
    /// </para>
    /// </summary>
    internal class Erebodrepanon : ModItem
    {
        /// <summary>
        /// 物品基础属性：132×156、伤害 10000、暴击 +20%、22 帧出手、击退 7、自动挥舞；
        /// 本体无挥砍判定也不画本体，伤害全部由手持弹幕承担，月后稀有度 15（紫）
        /// </summary>
        public override void SetDefaults()
        {
            Item.damage = 10000;
            Item.crit = 20;
            Item.DamageType = DamageClass.Melee;
            Item.width = 132;
            Item.height = 156;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7;
            Item.rare = ItemRarityID.Red;                  // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.value = Item.buyPrice(3, 20, 0, 0);       // 3 铂金 20 金（照 CE）
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放（照 CE）
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<ErebodrepanonHeld>();
            Item.shootSpeed = 16f;
            Item.autoReuse = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;   // 月后稀有度 16：名称染品红（灾厄 HotPink，即魔影档）
        }
        /// <summary>
        /// 出手：把当前段位写进弹幕的 <c>ai[0]</c> 再生成手持弹幕，第三段伤害翻倍（照 CE）；
        /// 段位计数存在玩家身上（见类注释 ③），出手后自增并按 3 取模。
        /// 手持弹幕每帧会把 <c>itemTime/itemAnimation</c> 按在 3 上，所以上一把镰刀收招前挥不出下一把。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CalamityDemutationPlayer mp = player.GetModPlayer<CalamityDemutationPlayer>();
            Projectile.NewProjectile(source, position, velocity, type, damage * (mp.erebodrepanonUseCount == 2 ? 2 : 1), knockback, player.whoAmI, mp.erebodrepanonUseCount);
            mp.erebodrepanonUseCount++;
            if (mp.erebodrepanonUseCount >= 3)
            {
                mp.erebodrepanonUseCount = 0;
            }
            return false;
        }
        /// <summary>
        /// 注册配方：死神擢升 + 宇宙暗流 + 猎魂鲨牙（灾厄 ReaperTooth）×12 + 魔影锭（灾厄 ShadowspecBar）×5，
        /// 站在嘉登熔炉（灾厄 DraedonsForge）上合成。两版灾厄都有这两样材料，故按各自的 Mod 实例分别注册一条。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ReaperTooth", out ModItem reaperTooth)
                    && calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                    && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    CreateRecipe()
                        .AddIngredient<DeathsAscension>()
                        .AddIngredient<StreamGouge>()
                        .AddIngredient(reaperTooth.Type, 12)           // 现代版灾厄：猎魂鲨牙×12
                        .AddIngredient(shadowspecBar.Type, 5)          // 现代版灾厄：魔影锭×5
                        .AddTile(draedonsForge.Type)                   // 现代版灾厄：嘉登熔炉
                        .Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("ReaperTooth", out ModItem classicReaperTooth)
                    && classic.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    CreateRecipe()
                        .AddIngredient<DeathsAscension>()
                        .AddIngredient<StreamGouge>()
                        .AddIngredient(classicReaperTooth.Type, 12)    // 经典版灾厄：猎魂鲨牙×12
                        .AddIngredient(classicShadowspecBar.Type, 5)   // 经典版灾厄：魔影锭×5
                        .AddTile(classicDraedonsForge.Type)            // 经典版灾厄：嘉登熔炉
                        .Register();
                }
            }
        }
        /// <summary>允许该物品吃到近战前缀（CE 原样）</summary>
        public override bool MeleePrefix()
        {
            return true;
        }
    }
    /// <summary>
    /// 泓渊亡铭的手持弹幕（ErebodrepanonHeld，移植自 CalamityEntropy 同名类）——
    /// 三段挥砍共用一套动画：<c>ai[0]</c> 记这是第几段（0/1/2）。
    /// 前 108 帧（9×12）是抬镰蓄势，此间不参与判定；到点播一次挥砍音、开启蓝白拖影，之后每 4 帧记一次旋转角做残影，
    /// 角速度按段位不同地加速/衰减，段末或 300~480 帧后收招自杀。第 3 段（<c>style == 2</c>）判定更长（2.2 倍），
    /// 命中最多 3 个目标时各挂一枚 <see cref="ErebodrepanonMark"/>。命中一律挂生命压制并炸一圈星轨 + 闪光粒子。
    /// <para>
    /// 与 CE 原版的差异：
    /// ① <c>FriendlySetDefaults</c>（CE 工具）按净结果展开成 SetDefaults 里的逐条赋值；
    /// ② <c>SetHandRotWithDir</c> 内联为 <c>player.direction = dir</c> + <c>SetCompositeArmFront(None, rot - PiOver2)</c>；
    /// ③ <c>CEUtils.Parabola</c> / <c>randomRot</c> / <c>randomPointInCircle</c> 内联，<c>LineThroughRect</c> 改用本工程
    ///    <c>CDUtil.LineThroughRect</c>；<c>CEUtils.GetOwner()</c> → <c>Main.player[Projectile.owner]</c>；
    /// ④ <c>PRT_ShineParticle</c> / <c>PRT_StarTrailParticle</c> → 本工程同名移植版
    ///    <see cref="ShineParticle"/> / <see cref="StarTrailParticle"/>；
    /// ⑤ <c>target.AddBuff&lt;LifeOppress&gt;</c> → <c>AddBuff(ModContent.BuffType&lt;LifeOppress&gt;())</c>；
    /// ⑥ <c>CEUtils.PlaySound("scytheswing" / "WScytheHit")</c> → 本模组 <see cref="CalamityDemutationSounds.ErebodrepanonSwing"/> /
    ///    <see cref="CalamityDemutationSounds.ErebodrepanonHit"/>（音高按本工程既有口径取 CE 值减 1）；
    /// ⑦ 素材：<c>CEExtraAssets.CircularSmear</c>、<c>CircularSmearSmokey</c>、<c>Star2</c> 搬进 Assets/ExtraTextures 同名贴图；
    /// ⑧ <c>UseBlendState</c> / <c>UseAdditive</c> / <c>ExitShaderRegion</c> 按 CE 的实现（采样器与光栅化状态逐字对齐）
    ///    展开成 End + Begin；<c>CommonEffects.colorLerp</c>（CE 的 ColorLerp3.fx）一并搬进本工程 Effects/ 并由
    ///    <c>EffectLoader.ColorLerp</c> 托管；
    /// ⑨ 源在残影循环里算了个插值 <c>a</c> 但从未使用（且 <c>oldRot.Count == 1</c> 时还会除零），删掉。
    /// </para>
    /// </summary>
    internal class ErebodrepanonHeld : ModProjectile
    {
        /// <summary>镰刀贴图直接借用物品贴图（照 CE）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/Erebodrepanon";
        /// <summary>星芒贴图（CE 的 CEExtraAssets.Star2）</summary>
        private const string StarTexture = "CalamityDemutation/Assets/ExtraTextures/Star2";
        /// <summary>圆形烟拖尾贴图（CE 的 CEExtraAssets.CircularSmear）</summary>
        private const string CircularSmearTexture = "CalamityDemutation/Assets/ExtraTextures/CircularSmear";
        /// <summary>烟状圆形拖尾贴图（CE 的 CEExtraAssets.CircularSmearSmokey）</summary>
        private const string CircularSmearSmokeyTexture = "CalamityDemutation/Assets/ExtraTextures/CircularSmearSmokey";
        /// <summary>朝向（±1）：取出手时的水平速度方向，第 0 段整体反向（照 CE）</summary>
        private int dir => (Projectile.velocity.X > 0 ? 1 : -1) * (style == 0 ? -1 : 1);
        /// <summary>当前是第几段挥砍（0/1/2，由物品写进 <c>ai[0]</c>）</summary>
        private int style => (int)Projectile.ai[0];
        /// <summary>自身帧数计数（整套动画的时间轴）</summary>
        private int counter = 0;
        /// <summary>每帧叠加到 <c>Projectile.rotation</c> 的角速度</summary>
        private float rotateSpeed = 0;
        /// <summary>判定与绘制的额外尺寸（第 3 段 2.2 倍，其余 1.8 倍）</summary>
        private float ScaleExtra = 1;
        /// <summary>蓝白拖影（圆形烟 + 星芒）的透明度：第 108 帧点亮，之后按段位渐隐</summary>
        private float tAlpha = 0;
        /// <summary>每 4 帧记一次的旋转角（最多 20 个），PreDraw 拿它铺残影</summary>
        private List<float> oldRot = new List<float>();
        /// <summary>
        /// 基础属性：8×8 判定箱、近战伤害、友方、无限穿透、不撞物块、每帧可再命中（本地无敌帧 -1）、每帧多跑 10 趟 AI。
        /// 源先调 <c>FriendlySetDefaults(Melee, false, -1)</c>（12×12 / 本地无敌帧 12）再覆盖这三条，这里直接写净结果。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 10;
        }
        /// <summary>
        /// 主逻辑：钉在玩家身上 → 首帧吃近战尺寸加成并按攻速抬高 MaxUpdates → 蓄势/挥砍两段推进角速度 →
        /// 手臂跟着镰刀转 → 段末（或超时）收招自杀。返回的判定见 <see cref="CanDamage"/>
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float speed = player.GetTotalAttackSpeed(DamageClass.Melee);
            Projectile.Center = player.MountedCenter;
            player.itemTime = player.itemAnimation = 3;   // 按住不让玩家挥出下一把，直到本把收招
            player.heldProj = Projectile.whoAmI;
            // 首帧：套用玩家的近战尺寸加成，并把 MaxUpdates 按攻速抬高（照 CE）
            if (counter == 0)
            {
                float meleeScale = player.HeldItem.scale;
                player.ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
                Projectile.MaxUpdates = (int)Math.Ceiling(Projectile.MaxUpdates * speed);
            }
            if (style < 3)
            {
                ScaleExtra = style == 2 ? 2.2f : 1.8f;
                if (counter == 0)
                {
                    Projectile.rotation = Projectile.velocity.ToRotation() + dir * -3.2f;
                }
                // 第 108 帧（9×12）抡出去：播挥砍音，点亮拖影
                if (counter == 9 * 12)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.ErebodrepanonSwing with { Pitch = 1.6f - (style == 2 ? 0.22f : Main.rand.NextFloat(0, 0.1f)) - 1f }, Projectile.Center);
                    tAlpha = 1;
                }
                if (counter < 9 * 12)
                {
                    // 蓄势：角速度快速衰减并向反侧微调
                    rotateSpeed *= 0.86f;
                    rotateSpeed -= dir * 0.0004f;
                }
                else
                {
                    // 挥砍中：每 4 帧记一次旋转角，给 PreDraw 铺残影
                    if (counter % 4 == 0)
                    {
                        oldRot.Add(Projectile.rotation);
                        if (oldRot.Count > 20)
                        {
                            oldRot.RemoveAt(0);
                        }
                    }
                    if (style == 2)
                    {
                        if (counter < 11 * 12)
                        {
                            rotateSpeed += dir * 0.0061f;
                            rotateSpeed *= 0.93f;
                        }
                    }
                    else
                    {
                        if (counter < 10 * 12)
                        {
                            rotateSpeed *= 0.96f;
                            rotateSpeed += dir * 0.01f;
                        }
                    }
                    if (counter > (style == 2 ? 12 : 10) * 12)
                    {
                        // 收招：216 帧后拖影开始渐隐，到各自的上限帧数就结束
                        if (counter > 18 * 12)
                        {
                            tAlpha *= 0.97f;
                            if (tAlpha < 0.02f)
                            {
                                tAlpha = 0;
                            }
                        }
                        if (counter < (style == 2 ? 40 : 25) * 12)
                        {
                            rotateSpeed *= 0.984f;
                        }
                        else
                        {
                            player.itemTime = player.itemAnimation = 0;
                            Projectile.Kill();
                        }
                    }
                }
                Projectile.rotation += rotateSpeed;
                // 手臂跟着镰刀转（CE 的 SetHandRotWithDir 内联）
                player.direction = Projectile.velocity.X > 0 ? 1 : -1;
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None, Projectile.rotation - MathHelper.PiOver2);
            }
            counter++;
        }
        /// <summary>判定窗口：第 96~228 帧（8×12 ~ 19×12）之间镰刃才吃判定（照 CE）</summary>
        public override bool? CanDamage()
        {
            if (style <= 2)
            {
                return counter < 19 * 12 && counter > 8 * 12;
            }
            return null;
        }
        /// <summary>命中判定：从镰刀中心沿朝向伸出 <c>scale × ScaleExtra × 260</c> 的一条线段，线宽 160</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float l = Projectile.scale * ScaleExtra * 260;
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * l, targetHitbox, 160);
        }
        /// <summary>同一条线段顺手割草（照 CE）</summary>
        public override void CutTiles()
        {
            float l = Projectile.scale * ScaleExtra * 260;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * l, 160, DelegateMethods.CutTiles);
        }
        /// <summary>位置由 AI 每帧钉在玩家身上，只有第 4 段（不存在）才让引擎推位置（照 CE）</summary>
        public override bool ShouldUpdatePosition()
        {
            return style > 2;
        }
        /// <summary>
        /// 命中：第 3 段且本次挥砍命中数不足 3 时，在目标身上留一枚 <see cref="ErebodrepanonMark"/>（伤害取本次的 40%）；
        /// 一律挂 5 秒生命压制，播命中音并炸 3 颗闪光 + 18 组星轨（一组随机方向、一组沿镰刀方向散开）。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (style == 2)
            {
                if (Projectile.numHits < 3)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<ErebodrepanonMark>(), (int)(Projectile.damage * 0.4f), 0, Projectile.owner, 0, 0, target.whoAmI);
                }
            }
            target.AddBuff(ModContent.BuffType<LifeOppress>(), 5 * 60);
            SoundEngine.PlaySound(CalamityDemutationSounds.ErebodrepanonHit with { Pitch = Main.rand.NextFloat(1.4f, 1.7f) - 1f }, target.Center);
            // 命中闪光：一大两小（照 CE 的三颗 PRT_ShineParticle）
            ShineParticle shine1 = new ShineParticle();
            DRKLoader.NewParticle(shine1, target.Center, Vector2.Zero, Color.Blue, 1.8f);
            shine1.Configure(1, true, ShineParticle.DrawModeEnum.AdditiveBlend, 0, 10);
            ShineParticle shine2 = new ShineParticle();
            DRKLoader.NewParticle(shine2, target.Center, Vector2.Zero, Color.White, 1.2f);
            shine2.Configure(1, true, ShineParticle.DrawModeEnum.AdditiveBlend, 0, 12);
            ShineParticle shine3 = new ShineParticle();
            DRKLoader.NewParticle(shine3, target.Center, Vector2.Zero, Color.White, 1.2f);
            shine3.Configure(1, true, ShineParticle.DrawModeEnum.AdditiveBlend, 0, 12);
            for (int i = 0; i < 18; i++)
            {
                StarTrailParticle p1 = new StarTrailParticle();
                DRKLoader.NewParticle(p1, target.Center, RandomRot().ToRotationVector2() * Main.rand.NextFloat(12, 44), Main.rand.NextBool() ? new Color(160, 160, 255) : new Color(255, 160, 80), Main.rand.NextFloat(1.6f, 2f));
                p1.fadeOut = 6;
                p1.Configure(true, 0, 12);
                StarTrailParticle p2 = new StarTrailParticle();
                DRKLoader.NewParticle(p2, target.Center, Projectile.velocity.RotatedByRandom(0.5f).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(12, 64), Main.rand.NextBool() ? new Color(160, 160, 255) : new Color(255, 160, 80), Main.rand.NextFloat(1.6f, 2f));
                p2.fadeOut = 16;
                p2.Configure(true, 0, 24);
            }
        }
        /// <summary>
        /// 自绘（照 CE 的三段批次）：① 加法 + PointClamp 批次画 20 层残影与一圈蓝色背光；
        /// ② 回到默认批次画镰刀本体；③ 加法 + LinearWrap 批次挂上 colorLerp 着色器，画两张圆形烟拖影与镰尖星芒。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D star = ModContent.Request<Texture2D>(StarTexture).Value;
            Texture2D s3 = ModContent.Request<Texture2D>(CircularSmearTexture).Value;
            Texture2D s2 = ModContent.Request<Texture2D>(CircularSmearSmokeyTexture).Value;
            float rotation = Projectile.rotation + 1.047f * dir;
            SpriteEffects ef = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Vector2 origin = dir > 0 ? new Vector2(8, tex.Height - 8) : new Vector2(8, 8);
            float scale = Projectile.scale * ScaleExtra;
            float offsetY = Main.player[Projectile.owner].gfxOffY;
            Vector2 center = Projectile.Center - Main.screenPosition + Vector2.UnitY * offsetY;
            // ① CE 的 UseBlendState(BlendState.Additive, SamplerState.PointClamp)
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < oldRot.Count; i++)
            {
                Main.spriteBatch.Draw(tex, center + RandomPointInCircle(2) * scale, null, (Main.rand.NextBool(15) ? Color.Orange : new Color(120, 120, 255, 255)) * 0.04f * i, oldRot[i] + 1.047f * dir, origin, scale * 1.3f, ef, 0);
            }
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4 * 0.5f)
            {
                Main.spriteBatch.Draw(tex, center + i.ToRotationVector2() * 6, null, new Color(60, 60, 255, 255) * 0.7f, rotation, origin, scale * 1.3f, ef, 0);
            }
            // ② CE 的 ExitShaderRegion：回到默认批次画本体
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tex, center, null, Color.White, rotation, origin, scale * 1.3f, ef, 0);
            // ③ CE 的 UseAdditive()（Immediate + Additive + LinearWrap）再套 colorLerp 着色器
            Main.spriteBatch.End();
            Effect shader = EffectLoader.ColorLerp.Value;
            shader.Parameters["color"].SetValue((new Color(200, 200, 255) * tAlpha * 0.75f).ToVector4());
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            float ssc = 3.4f;
            Main.spriteBatch.Draw(s2, center, null, new Color(0, 0, 255) * tAlpha * 0.6f, rotation + 0.2f * dir, s2.Size() * 0.5f, scale * ssc, ef, 0);
            Main.spriteBatch.Draw(s3, center, null, new Color(2, 2, 255) * tAlpha * 0.6f, rotation + 0.4f * dir, s3.Size() * 0.5f, scale * ssc * 0.98f, ef, 0);
            // 镰尖星芒：沿镰刀方向偏移 (138, 104×dir) 处拉一颗被压扁的星
            Vector2 spos = Projectile.Center + new Vector2(138, 104 * dir).RotatedBy(Projectile.rotation) * scale;
            Main.spriteBatch.Draw(star, spos - Main.screenPosition, null, Color.Blue * tAlpha, 0, star.Size() * 0.5f, new Vector2(1, 0.4f) * scale * 3.2f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, spos - Main.screenPosition, null, Color.White * tAlpha, 0, star.Size() * 0.5f, new Vector2(1, 0.4f) * scale * 2.8f, SpriteEffects.None, 0);
            // 收尾：还原默认批次，避免影响同帧后面的绘制
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.randomRot 的等价实现：0~2π 的随机角</summary>
        private static float RandomRot() => Main.rand.NextFloat(MathHelper.TwoPi);
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => RandomRot().ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
    /// <summary>
    /// 泓渊亡铭标记（ErebodrepanonMark，移植自 CalamityEntropy 同名类）：
    /// 第 3 段挥砍命中时贴在敌怪身上的打击点——先在敌怪周围 600 像素处拉起 4 道星轨，随帧沿圆周收束；
    /// 第 40 帧炸开（播命中音 + 24 颗星轨），随后 <c>ai[1]</c> 置 1 开启 450×450 的判定，到第 49 帧消失。
    /// <para>
    /// 与 CE 原版的差异：① <c>ToNPC()</c> 内联为带边界检查的等效写法（源直接索引 <c>Main.npc</c>）；
    /// ② <c>PRT_StarTrailParticle</c> → 本工程 <see cref="StarTrailParticle"/>；③ <c>CEUtils.Parabola</c> 与
    /// <c>normalize()</c> 内联（后者即 <c>SafeNormalize(Vector2.Zero)</c>）；④ 贴图用本工程的 Assets/ExtraTextures/white
    /// 占位（源用 CEUtils.WhiteTexPath）；⑤ <c>CEExtraAssets.StarChromatic</c> 搬进 Assets/ExtraTextures 同名贴图，
    /// 绘制批次按 CE 的 <c>UseAdditive</c>/<c>ExitShaderRegion</c> 展开。
    /// </para>
    /// </summary>
    internal class ErebodrepanonMark : ModProjectile
    {
        /// <summary>本体不画贴图，全部由粒子与星芒表现（源用 CEUtils.WhiteTexPath）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>收束用的彩色星芒贴图（CE 的 CEExtraAssets.StarChromatic）</summary>
        private const string StarChromaticTexture = "CalamityDemutation/Assets/ExtraTextures/StarChromatic";
        /// <summary>四道收束星轨</summary>
        private List<StarTrailParticle> trails = new List<StarTrailParticle>();
        /// <summary>四道星轨的公共旋转角</summary>
        private float tRot = 0f;
        /// <summary>星轨的收束系数（1 → 0）</summary>
        private float tDist = 1f;
        /// <summary>
        /// 基础属性：450×450 判定箱、近战伤害、友方、无限穿透、不撞物块、每帧可再命中。
        /// 源同样先调 <c>FriendlySetDefaults(Melee, false, -1)</c> 再把判定箱改成 450，这里直接写净结果。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = Projectile.height = 450;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        /// <summary>只有炸开之后（<c>ai[1] &gt; 0</c>）才吃判定（照 CE）</summary>
        public override bool? CanDamage()
        {
            return Projectile.ai[1] > 0;
        }
        /// <summary>
        /// 主逻辑：跟着目标敌怪走（目标没了就停在原地）→ 首帧拉起 4 道星轨 → 第 40 帧炸开 →
        /// 收束期间每帧把星轨按圆周摆位、把速度对齐位移方向 → 第 49 帧自杀
        /// </summary>
        public override void AI()
        {
            int npcIndex = (int)Projectile.ai[2];
            NPC n = npcIndex >= 0 && npcIndex < Main.maxNPCs ? Main.npc[npcIndex] : null;
            if (n != null && n.active)
            {
                Projectile.Center = n.Center;
            }
            if (Projectile.ai[0] == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    StarTrailParticle t = new StarTrailParticle();
                    DRKLoader.NewParticle(t, Projectile.Center + (i * MathHelper.PiOver2).ToRotationVector2() * 600, Vector2.Zero, new Color(80, 80, 255), 2.5f);
                    t.maxLength = 14;
                    t.Configure(true, 0, 12);
                    trails.Add(t);
                }
            }
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 40)
            {
                // CE 原名 bne_hit，与分形之星裂开是同一个音效，本工程已注册过
                SoundEngine.PlaySound(CalamityDemutationSounds.FractalStarSplit, Projectile.Center);
                for (int i = 0; i < 24; i++)
                {
                    StarTrailParticle p = new StarTrailParticle();
                    DRKLoader.NewParticle(p, Projectile.Center, RandomRot().ToRotationVector2() * Main.rand.NextFloat(10, 44), Main.rand.NextBool() ? new Color(160, 160, 255) : new Color(255, 160, 80), Main.rand.NextFloat(1.6f, 2f));
                    p.fadeOut = 16;
                    p.Configure(true, 0, 28);
                }
            }
            if (Projectile.ai[0] > 40)
            {
                Projectile.ai[1] = 1;
            }
            else
            {
                // 收束：旋转角均匀推进，半径按抛物线从 1 收到 0
                tRot += Projectile.ai[0] * 0.008f + 0.03f;
                tDist = (1 - Projectile.ai[0] / 46f);
                tDist = 1 - Parabola(0.5f + tDist * 0.5f, 1);
                for (int i = 0; i < 4; i++)
                {
                    StarTrailParticle t = trails[i];
                    Vector2 op = t.Position;
                    t.Position = Projectile.Center + (i * MathHelper.PiOver2 + tRot).ToRotationVector2() * 600 * tDist;
                    t.Velocity = (t.Position - op).SafeNormalize(Vector2.Zero) * 4;
                    t.Lifetime = 12;
                    t.Scale = 7f * Projectile.ai[0] / 40f;
                }
            }
            if (Projectile.ai[0] >= 49)
            {
                Projectile.Kill();
            }
        }
        /// <summary>
        /// 自绘：收束期间一颗逐帧放大的彩色星芒（正十字 + 斜十字各两笔），炸开后 40→49 帧从 0.64 缩到 0
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // CE 的 UseAdditive()：Immediate + Additive + LinearWrap
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D s = ModContent.Request<Texture2D>(StarChromaticTexture).Value;
            float scale = Projectile.ai[0] / 40f * 0.2f;
            float a = Projectile.ai[0] / 40f;
            if (Projectile.ai[0] >= 40)
            {
                scale = Utils.Remap(Projectile.ai[0], 40, 49, 0.64f, 0);
            }
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, Color.White * a, 0, s.Size() * 0.5f, scale * 0.7f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, new Color(60, 60, 255) * a, 0, s.Size() * 0.5f, scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, Color.White * a, MathHelper.PiOver4, s.Size() * 0.5f, scale * 0.7f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(s, Projectile.Center - Main.screenPosition, null, new Color(60, 60, 255) * a, MathHelper.PiOver4, s.Size() * 0.5f, scale, SpriteEffects.None, 0);
            // CE 的 ExitShaderRegion：还原默认批次
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.Parabola 的等价实现：开口向下的抛物线，t=0.5 时取到 height</summary>
        private static float Parabola(float t, float height) => 4f * height * t * (1f - t);
        /// <summary>CEUtils.randomRot 的等价实现：0~2π 的随机角</summary>
        private static float RandomRot() => Main.rand.NextFloat(MathHelper.TwoPi);
    }
}
