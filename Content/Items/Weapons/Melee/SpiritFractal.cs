using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
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
    /// 聚魂分形（SpiritFractal，移植自 CalamityEntropy）—— 分形系列的第八把武器，
    /// 由上一把「元素分形」与无星之夜、原版星辰之怒、符文之歌、虚无碎片合成而来。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），招式全交给手持弹幕表现。
    /// 出手节奏是五段一轮：挥砍（-1）、挥砍（1）、挥砍（-1）、挥砍（1）、投掷（2），
    /// 前三把的「每两次挥砍出射」在这里变成「每四次挥舞后把剑本身掷出」。
    /// </summary>
    internal class SpiritFractal:ModItem
    {
        /// <summary>本次挥砍的招式：0/2 → -1（左向挥砍）、1/3 → 1（右向挥砍）、4 → 2（投掷本剑），与 CE 一样走 0~4 循环</summary>
        private int atkType = 0;
        public override void SetDefaults()
        {
            Item.damage = 1590;                            // 1590 点近战伤害
            Item.crit = 10;                                // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                               // 贴图宽（像素）
            Item.height = 60;                              // 贴图高（像素）
            Item.useTime = Item.useAnimation = 20;         // 使用时间/动画时长 20 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 7;
            Item.value = Item.buyPrice(gold: 20);          // 价值 20 金
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SpiritFractalHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
        }
        /// <summary>
        /// 生成手持弹幕并把本次招式交给它，然后在五段之间翻转。
        /// <c>ai[2]</c> = 玩家到光标的距离 + 180，投掷式的抛物线高度（CE 原样，取本地鼠标坐标即可——
        /// 出手的是本地玩家）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int at = 2;
            if (atkType == 0 || atkType == 2)
            {
                at = -1;
            }
            if (atkType == 1 || atkType == 3)
            {
                at = 1;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, at, 0, Main.MouseWorld.Distance(position) + 180);
            atkType += 1;
            if (atkType > 4)
            {
                atkType = 0;
            }
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>配方：聚魂分形 + 无星之夜 + 星辰之怒 + 符文之歌 + 虚无碎片×4 @ 远古操纵机</summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<ElementalFractal>()
                .AddIngredient<StarlessNight>()
                .AddIngredient(ItemID.StarWrath)
                .AddIngredient<RuneSong>()
                .AddIngredient<NihilityFragments>(4)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    /// <summary>
    /// 聚魂分形手持弹幕（移植自 CalamityEntropy 的 SpiritFractalHeld）：贴身绘制剑体，按 ai[0] 走两路招式。
    /// <para>
    /// <b>ai[0] == 2（投掷本剑）</b>：剑身绕玩家转约 4 圈、并沿抛物线被甩到光标方向，
    /// 中段每帧刷新各敌人的本地无敌帧（按攻速换算，让旋挥能连续命中），并按攻速累计在剑尖召
    /// <see cref="GhastlySoulLarge"/>（伤害 ÷7，传 ai[1] = 1）。这一式计数走半速（counter += 0.5），
    /// 于是整套动作的耗时是普通挥砍的两倍。
    /// <b>ai[0] == ±1（左右挥砍）</b>：剑身贴住玩家挥舞（ai[0] 同时是旋向系数），进度过 0.2 时朝前甩出
    /// 两发 <see cref="FractalGhostBlade"/>（全额伤害）。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（映射同前几把：
    /// sf_use→FractalSwing、sf_hit→FractalSwingHit、FractalHit→FractalImpact），音高按既有口径取 CE 值减 1
    /// （0.6 → -0.4，1 + ai[0]·0.12 → ai[0]·0.12），<c>CEUtils.WeapSound</c> 按 1.0；
    /// ② 命中减益不新建——CE 挂的是它自研的 <c>SoulDisorder</c>（+15 穿甲 / +5% 易伤），两版灾厄都没有这件 buff，
    /// 按既有口径改用两版都有的 <c>ArmorCrunch</c>（同 <see cref="RuneSongHeld"/>）；
    /// ③ <c>CEUtils</c> 工具一律内联（<c>GetRepeatedCosFromZeroToOne</c>→<see cref="RepeatCos01"/>、
    /// <c>Parabola</c>→<see cref="Parabola"/>、<c>randomPointInCircle</c>→<see cref="RandomPointInCircle"/>、
    /// <c>GetOwner</c>→<c>Main.player[owner]</c>、<c>GetTexture</c>→TextureAssets、
    /// <c>LineThroughRect</c>→<see cref="CDUtil.LineThroughRect"/>）；
    /// ④ 删掉 CE 里只写不读的 <c>odr</c> 旋转历史、从未被读取的 <c>spawnProj</c> 字段，以及配套的
    /// TrailingMode/TrailCacheLength 设置；⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，
    /// 改用 End + 立即模式 Begin(Immediate, Additive, Main.DefaultSamplerState, CullNone)（CE 的单参
    /// <c>UseBlendState</c> 在 CE 侧默认就是 <c>Main.DefaultSamplerState</c>），画完再恢复默认批次。
    /// </para>
    /// </summary>
    internal class SpiritFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/SpiritFractal";
        /// <summary>刀光贴图（CE 的 CEExtraAssets.SemiCircularSmear）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/SemiCircularSmear";
        /// <summary>自身帧数计数（ai[0] 存的是招式，不是计时器）；投掷式每帧加半</summary>
        private float counter = 0f;
        /// <summary>绘制缩放</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>挥砍式是否已射出剑影（一次挥砍只射一发）</summary>
        private bool shoot = true;
        /// <summary>聚魂弹召唤的攻速累计器，每满 6 点召一颗</summary>
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
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = -1;         // 默认同一次挥砍对同一敌人只结算一次（投掷式会临时改）
            Projectile.timeLeft = 100000;                // 占位值，真正的结束判据是 counter
            Projectile.extraUpdates = 3;                 // 配合物品 20 帧使用时间 → 挥砍总帧数 80（投掷式 160）
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            counter += Projectile.ai[0] == 2 ? 0.5f : 1f;
            if (init)
            {
                float meleeScale = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
                if (Projectile.ai[0] == 2)
                {
                    // 投掷式更重更响，并把剑体放大 1.3 倍
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = -0.4f, Volume = 0.8f }, Projectile.Center);
                    Projectile.scale *= 1.3f;
                }
                if (Projectile.ai[0] < 2)
                {
                    // 两次挥砍音高不同（ai[0] 为 ±1），形成交替感
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = Projectile.ai[0] * 0.12f, Volume = 0.6f }, Projectile.Center);
                }
                init = false;
            }
            Projectile.timeLeft = 3;                     // 每帧续命，收尾交给下面的 counter 判据
            if (Projectile.ai[0] == 2)
            {
                // 投掷式：全程把各敌人的本地无敌帧按攻速刷新，使旋挥能连续命中
                int immunity = (int)(Projectile.MaxUpdates * 4 / owner.GetTotalAttackSpeed(Projectile.DamageType));
                for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                {
                    if (Projectile.localNPCImmunity[i] == -1)
                    {
                        Projectile.localNPCImmunity[i] = immunity;
                    }
                }
                Projectile.localNPCHitCooldown = immunity;
                // 中段按攻速累计，每满 6 点沿剑尖方向 48 像素处召一颗聚魂弹（ai[1] = 1 → 走追踪分支）
                const float rotF = MathHelper.Pi / 180f * 280f + MathHelper.TwoPi * 3;
                if (progress > 0.3f && progress < 0.7f)
                {
                    spawnProjCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
                }
                if (spawnProjCounter >= 6f)
                {
                    spawnProjCounter -= 6f;
                    Vector2 spawnPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 48 * scale * Projectile.scale;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, RandomPointInCircle(0.1f) + Projectile.rotation.ToRotationVector2() * 8,
                            ModContent.ProjectileType<GhastlySoulLarge>(), Projectile.damage / 7, Projectile.knockBack, Projectile.owner, 0, 1);
                    }
                }
                alpha = 1f;
                scale = 1.8f;
                // 从 -140° 起转，绕满 3 整圈 + 280°，按出手方向决定旋向
                Projectile.rotation = Projectile.velocity.ToRotation() + (MathHelper.ToRadians(-140f) + rotF * RepeatCos01(progress, 1)) * (Projectile.velocity.X > 0 ? 1 : -1);
                // 剑体不跟随速度方向，而是沿抛物线被甩到 ai[2] 指定的高度（relative 坐标，故 ShouldUpdatePosition 为假）
                Projectile.Center = owner.MountedCenter + Projectile.velocity.SafeNormalize(Vector2.Zero) * Parabola(progress, Projectile.ai[2]);
                owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - owner.Center).ToRotation() - MathHelper.PiOver2);
            }
            else
            {
                // 挥砍式：剑体贴住玩家，ai[0] 的符号决定从哪一侧挥出
                const float rotF = 4f;
                alpha = 1f;
                scale = 1.8f;
                Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * RepeatCos01(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                Projectile.Center = owner.MountedCenter;
                if (progress > 0.2f && shoot)
                {
                    shoot = false;
                    for (int i = 0; i < 2; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                            Projectile.velocity.SafeNormalize(Vector2.Zero) * 28 + RandomPointInCircle(8),
                            ModContent.ProjectileType<FractalGhostBlade>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                }
                owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            }
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
        /// <summary>命中时挂灵魂紊乱（CE 原版就是它）、播命中音，并按灾厄「真断钢」的粒子套路炸一圈火花</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 460);
            if (playHitSound || Projectile.ai[0] == 2)
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
        /// 自绘：先按 dir 取贴图角为原点画剑体；再在加法混合下叠两笔粉红渐变半圆刀光
        /// （投掷式的 zScale 减半、zAlpha 恒为 1），最后恢复默认批次。
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
            if (Projectile.ai[0] == 2)
            {
                origin = texture.Size() * 0.5f;
            }
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;
            float maxUpdateTime = Main.player[Projectile.owner].itemTimeMax * Projectile.MaxUpdates;
            float offsetY = Main.player[Projectile.owner].gfxOffY;
            Main.EntitySpriteDraw(texture, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            Texture2D smear = ModContent.Request<Texture2D>(SmearTexture).Value;
            Color color1 = new Color(48, 52, 79);
            Color color2 = new Color(242, 201, 190);
            float zScale = Projectile.ai[0] == 2 ? 0.5f : 1f;
            float zAlpha = (float)Math.Cos(RepeatCos01(counter / maxUpdateTime, 3) * MathHelper.Pi - MathHelper.PiOver2);
            if (Projectile.ai[0] == 2)
            {
                zAlpha = 1f;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(smear, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null,
                Color.Lerp(color1, color2, counter / maxUpdateTime) * zAlpha, Projectile.rotation + MathHelper.ToRadians(32f) * -dir, smear.Size() / 2f, Projectile.scale * 1.9f * scale * zScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(smear, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null,
                Color.Lerp(color2, color1, counter / maxUpdateTime) * zAlpha, Projectile.rotation + MathHelper.ToRadians(32f) * -dir, smear.Size() / 2f, Projectile.scale * 1.7f * scale * zScale, SpriteEffects.None, 0f);
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
        /// <summary>CEUtils.GetRepeatedCosFromZeroToOne 的等价实现：把 [0,1] 的余弦缓动递归套用 repeat 次</summary>
        private static float RepeatCos01(float v, int repeat)
        {
            if (repeat <= 1)
                return (float)Math.Cos(v * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
            return (float)Math.Cos(RepeatCos01(v, repeat - 1) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
        }
        /// <summary>CEUtils.Parabola 的等价实现：t∈[0,1] 的抛物线，顶点为 height</summary>
        private static float Parabola(float t, float height) => 4 * height * t * (1 - t);
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
}
