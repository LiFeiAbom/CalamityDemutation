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
    /// 元素分形（ElementalFractal，移植自 CalamityEntropy）—— 分形系列的第七把武器，
    /// 由上一把「星熠分形」与真断钢剑、夜明锭、神圣锭、日耀碎片合成而来。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），两种招式轮流由手持弹幕表现。
    /// </summary>
    internal class ElementalFractal:ModItem
    {
        /// <summary>本次挥砍的招式：0 = 环绕旋挥，1 = 朝鼠标刺出（与 CE 一样在 0/1 之间交替）</summary>
        private int atkType = 0;
        public override void SetDefaults()
        {
            Item.damage = 480;                             // 480 点近战伤害
            Item.crit = 10;                                // 额外暴击率
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;                               // 贴图宽（像素）
            Item.height = 60;                              // 贴图高（像素）
            Item.useTime = Item.useAnimation = 50;         // 使用时间/动画时长 50 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 6;
            Item.value = Item.buyPrice(platinum: 1);       // 价值 1 铂金
            Item.rare = ItemRarityID.Red;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ElementalFractalHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
        }
        /// <summary>生成手持弹幕并把本次招式交给它，然后在两种招式之间翻转</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType);
            atkType = 1 - atkType;
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>配方：元素分形 + 真断钢剑 + 夜明锭×5 + 神圣锭×5 + 日耀碎片×5 @ 远古操纵机</summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<StarlitFractal>()
                .AddIngredient(ItemID.TrueNightsEdge)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddIngredient(ItemID.HallowedBar, 5)
                .AddIngredient(ItemID.FragmentSolar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    /// <summary>
    /// 元素分形手持弹幕（移植自 CalamityEntropy 的 ElementalFractalHeld）：贴身绘制剑体，按 ai[0] 走两种招式。
    /// <para>
    /// <b>ai[0] == 0（环绕旋挥）</b>：剑身绕玩家转约 4 圈；进度 0.18~0.82 期间每帧刷新各敌人的本地无敌帧
    /// （按攻速换算，让旋挥能连续命中），并按攻速累计在剑尖处吐 <see cref="FractalBlight"/> 元素光星
    /// （传 ai[1] = 1，于是它命中时会挂元素减益）；这一式的伤害在 <c>ModifyHitNPC</c> 里压到 40%。
    /// <b>ai[0] == 1（刺出）</b>：朝鼠标方向转体刺出，前 0.32 进度不能命中，进度过 0.46 时甩出一发
    /// <see cref="ElementalFractalThrown"/> 强力剑影。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（映射同前几把：
    /// sf_use→FractalSwing、sf_hit→FractalSwingHit、FractalHit→FractalImpact，新增 zypshot2→FractalThrow），
    /// 音高按既有口径取 CE 值减 1（0.6 → -0.4），<c>CEUtils.WeapSound</c> 按 1.0；
    /// ② CE 的 <c>owner.mouseWorld()</c>（它自研的各端可见鼠标坐标）走本模组同口径的
    /// <see cref="CalamityDemutationPlayer.GetMouseWorld"/>；③ <c>GetOwner()</c>/<c>GetTexture()</c> 换成 <c>Main.player[owner]</c> 与 TextureAssets，
    /// <c>CEExtraAssets.SemiCircularSmear</c>/<c>StarTexture</c> 换成本模组 Assets/ExtraTextures 下的同名贴图，
    /// 星芒贴图的静态缓存改为按需请求；④ <c>CEUtils.GetRepeatedCosFromZeroToOne</c> 与 <c>randomPointInCircle</c> 内联；
    /// ⑤ 去掉 CE 里只记录、从不读取的 odr 旋转历史与配套的 TrailingMode/TrailCacheLength 设置；
    /// ⑥ 减益不新建——现代版灾厄取 <c>ElementalMix</c>，经典版没有它则按经典版元素武器的口径用
    /// <c>HolyLight</c> + <c>GlacialState</c> + <c>BrimstoneFlames</c> + <c>Plague</c> 四元素等效
    /// （与 FractalBlight 同一口径）；⑦ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，
    /// 改用 End + 立即模式 Begin(Additive)、画完再恢复默认批次；⑧ CE 里 <c>ai[0] == 2</c> 的分支本武器走不到
    /// （物品只传 0/1），为对照 CE 保留原样。
    /// </para>
    /// </summary>
    internal class ElementalFractalHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/ElementalFractal";
        /// <summary>旋挥式的拖尾贴图（CE 的 CEExtraAssets.SemiCircularSmear）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/SemiCircularSmear";
        /// <summary>剑尖星芒贴图（CE 的 CEExtraAssets.StarTexture）</summary>
        private const string StarTexture = "CalamityDemutation/Assets/ExtraTextures/StarTexture";
        /// <summary>自身帧数计数（ai[0] 存的是招式，不是计时器）</summary>
        private float counter = 0f;
        /// <summary>绘制缩放</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播招式起手音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        /// <summary>刺出式是否已射出剑影（一次挥砍只射一发）</summary>
        private bool shoot = true;
        /// <summary>元素光星召唤的攻速累计器，每满 6 点召一颗</summary>
        private float spawnProjCounter = 0f;
        /// <summary>本次挥砍是否已播过命中音（旋挥式每次命中都播，刺出式只播一次）</summary>
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
            Projectile.localNPCHitCooldown = -1;         // 默认同一次挥砍对同一敌人只结算一次（旋挥式会临时改）
            Projectile.timeLeft = 100000;                // 占位值，真正的结束判据是 counter
            Projectile.extraUpdates = 3;                 // 配合物品 50 帧使用时间 → 挥砍总帧数 200
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
                if (Projectile.ai[0] == 0)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwing with { Pitch = -0.4f, Volume = 0.6f }, Projectile.Center);
                }
                init = false;
            }
            Projectile.timeLeft = 3;                     // 每帧续命，收尾交给下面的 counter 判据
            if (Projectile.ai[0] == 0)
            {
                // 环绕旋挥：中段把各敌人的本地无敌帧按攻速刷新，使旋挥能连续命中；其余时间回到「只结算一次」
                if (progress > 0.18f && progress < 0.82f)
                {
                    int immunity = (int)(Projectile.MaxUpdates * 4 / owner.GetTotalAttackSpeed(Projectile.DamageType));
                    for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                    {
                        if (Projectile.localNPCImmunity[i] == -1)
                        {
                            Projectile.localNPCImmunity[i] = immunity;
                        }
                    }
                    Projectile.localNPCHitCooldown = immunity;
                }
                else
                {
                    Projectile.localNPCHitCooldown = -1;
                }
                // 中段按攻速累计，每满 6 点沿剑尖方向 98 像素处召一颗元素光星（ai[1] = 1 → 命中挂元素减益）
                const float rotF = MathHelper.Pi / 180f * 140f + MathHelper.TwoPi * 4;
                if (progress > 0.3f && progress < 0.7f)
                {
                    spawnProjCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
                }
                if (spawnProjCounter >= 6f)
                {
                    spawnProjCounter -= 6f;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Vector2 spawnPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, RandomPointInCircle(0.1f) + Projectile.rotation.ToRotationVector2() * 6,
                            ModContent.ProjectileType<FractalBlight>(), Projectile.damage / 5, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat() * 6.28f, 1);
                    }
                }
                alpha = 1f;
                scale = 1.6f;
                Projectile.rotation = Projectile.velocity.ToRotation() + (MathHelper.ToRadians(-140) + rotF * RepeatCos01(progress, 1)) * -1 * (Projectile.velocity.X > 0 ? -1 : 1);
            }
            else
            {
                // 刺出：速度方向始终指向鼠标（取跨端可见的那份坐标，CE 用的是 owner.mouseWorld()），转体分三段（前摆 → 横扫 → 收势）
                Vector2 mouseWorld = owner.GetModPlayer<CalamityDemutationPlayer>().GetMouseWorld();
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((mouseWorld - Projectile.Center).ToRotation());
                float aim = (mouseWorld - Projectile.Center).ToRotation();
                int flip = Projectile.velocity.X > 0 ? -1 : 1;
                if (progress < 0.34f)
                {
                    float p = progress / 0.34f;
                    Projectile.rotation = aim + RepeatCos01(p, 2) * MathHelper.ToRadians(140) * flip;
                }
                else
                {
                    if (progress < 0.67f)
                    {
                        float p = (progress - 0.34f) / 0.33f;
                        Projectile.rotation = aim + (RepeatCos01(1 - p, 2) * MathHelper.ToRadians(280) - MathHelper.ToRadians(140)) * flip;
                    }
                    else
                    {
                        float p = (progress - 0.67f) / 0.33f;
                        Projectile.rotation = aim + (RepeatCos01(p, 2) * MathHelper.ToRadians(280) - MathHelper.ToRadians(140)) * flip;
                    }
                    if (progress > 0.46f && shoot)
                    {
                        shoot = false;
                        SoundEngine.PlaySound(CalamityDemutationSounds.FractalThrow, Projectile.Center);
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * 2,
                                ModContent.ProjectileType<ElementalFractalThrown>(), (int)(Projectile.damage * 1.6f), Projectile.knockBack * 2, Projectile.owner);
                        }
                    }
                }
                scale = 1.6f;
                alpha = 1f;
            }
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
        /// <summary>刺出式前 0.32 进度还不能命中（CE 原样的起手保护）</summary>
        public override bool? CanHitNPC(NPC target)
        {
            float progress = counter / (Main.player[Projectile.owner].itemTimeMax * Projectile.MaxUpdates);
            if (Projectile.ai[0] == 1 && progress < 0.32f)
            {
                return false;
            }
            return null;
        }
        /// <summary>旋挥式的伤害压到 40%（CE 原设定，靠它换取多段连续命中）</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 0)
            {
                modifiers.SourceDamage *= 0.4f;
            }
        }
        /// <summary>
        /// 命中时挂元素减益（两版各取现成的）、播命中音（旋挥式每次命中都播，刺出式只播一次），
        /// 并按灾厄「真断钢」的粒子套路在敌怪中心炸一圈火花
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GlacialState", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", 400);
            if (playHitSound || Projectile.ai[0] == 0)
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
        /// 自绘：先按 dir 取贴图角为原点画剑体；旋挥式再在加法混合下叠一层粉红渐变半圆拖尾
        /// 与剑尖两笔十字星芒（刺出式不加这些），最后恢复默认批次。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int dir = Projectile.velocity.X > 0 ? 1 : -1;
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
            if (Projectile.ai[0] == 0)
            {
                Texture2D smear = ModContent.Request<Texture2D>(SmearTexture).Value;
                Texture2D star = ModContent.Request<Texture2D>(StarTexture).Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(smear, Projectile.Center + offsetY * Vector2.UnitY - Main.screenPosition, null,
                    Color.Lerp(new Color(255, 200, 215), new Color(255, 140, 150), progress) * (float)Math.Cos(RepeatCos01(progress, 1) * MathHelper.Pi - MathHelper.PiOver2),
                    Projectile.rotation + MathHelper.ToRadians(32) * -dir, smear.Size() / 2f, Projectile.scale * 1.6f * scale, SpriteEffects.None, 0f);
                Vector2 starPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale - Main.screenPosition;
                float starAlpha = (float)Math.Cos(progress * MathHelper.TwoPi - MathHelper.Pi) * 0.5f + 0.5f;
                Main.spriteBatch.Draw(star, starPos, null, Color.LightGoldenrodYellow * 0.7f * starAlpha, 0f, star.Size() / 2f, 0.36f * Projectile.scale * new Vector2(2.8f, 0.5f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, starPos, null, Color.LightGoldenrodYellow * 0.7f * starAlpha, 0f, star.Size() / 2f, 0.36f * Projectile.scale * new Vector2(0.5f, 2.8f), SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }
        /// <summary>命中判定是从玩家中心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * (124 * (Projectile.ai[0] == 2 ? 0.9f : 1)) * Projectile.scale * scale;
            float point = 0f;
            return targetHitbox.Contains((int)start.X, (int)start.Y) || targetHitbox.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 46, ref point);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (130 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
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
