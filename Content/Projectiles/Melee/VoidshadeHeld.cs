using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Effects;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 虚影薄锋手持弹幕（移植自 CalamityEntropy 的 VoidshadeHeld）：贴身绘制剑体，按 <c>ai[0]</c> 走两路招式。
    /// <para>
    /// <b>ai[0] == 0/1（左键挥砍）</b>：剑体贴住玩家转体挥出（ai[0] 决定初始角与旋向），
    /// 第 16 帧甩出一道 <see cref="VoidImpact"/>（全额伤害）；若玩家正处在「突刺命中强化」期
    /// （<c>voidshadeBoostTime &gt; 0</c>），出手瞬间把它清零并让本次伤害翻倍。
    /// 剑体缩放由自旋速度换算（<see cref="GetScale"/>），旋得越快剑越大。
    /// <b>ai[0] == 3（右键虚影突刺）</b>：前 9 帧推动玩家以 2 倍速冲向鼠标方向（撞到敌人后
    /// <see cref="OnHitNPC"/> 里把 <c>dash</c> 翻成 -32 并给玩家挂 90 帧强化、再把玩家速度反推一下），
    /// 第 20 帧在玩家处放出一道 <see cref="WohLaser"/>（伤害 ÷2，攻击类型继承本弹幕），整套动作 60 帧收招。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds"/>（antivoidhit→VoidshadeHit），
    /// 音高按既有口径取 CE 值减 1，<c>CEUtils.WeapSound</c> 按 1.0；
    /// ② 粒子换成 <see cref="SparkleParticle"/>（CE 的 PRT_SparkleCal）、<see cref="AltSparkParticle"/>
    /// （PRT_AltSpark）与 <see cref="LineParticleCal"/>（PRT_LineCal），调用次序与参数照抄；
    /// ③ <c>player.Entropy().voidshadeBoostTime</c> 走 <see cref="CalamityDemutationPlayer.voidshadeBoostTime"/>；
    /// ④ 突刺命中时 CE 用 InnoVault 的 <c>.ToProj().DamageType</c> 把 WohLaser 的攻击类型改成近战
    /// （它默认是远程），本模组没有该扩展，改按 <see cref="ArkoftheAncients"/> 的既有写法取回弹幕实例再赋值；
    /// ⑤ <c>CEUtils</c> 工具一律内联（<c>LineThroughRect</c>→<see cref="CDUtil.LineThroughRect"/>、
    /// <c>GetOwner</c>→<c>Main.player[owner]</c>、<c>normalize</c>→SafeNormalize）；
    /// ⑥ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin；
    /// ⑦ **结尾补上默认批次的恢复**（CE 把批次留在 Immediate + AlphaBlend + AnisotropicClamp 就不管了，
    /// 会污染它之后同帧的绘制，同 <see cref="VoidSlash"/> 的既有做法）；
    /// ⑧ CE 的 <c>goto drawBlade</c>（突刺式跳过刀光）改写为等价的 <c>if (attackType != 3)</c> 包住刀光段，
    /// 行为一致但不用 goto；随之删掉两处刀光循环里**永远走不到**的 <c>attackType == 3</c> 分支。
    /// </para>
    /// <para>
    /// <b>CE 原状、刻意照抄的一处</b>：命中火花里那三处 <c>Projectile.frame == 7</c> 三元判断永远取 false 分支
    /// —— 本弹幕从不给 <c>Projectile.frame</c> 赋值（CE 也没赋），这是 CE 遗留下来的旧动画版本条件，
    /// 移植时按 false 分支折叠成定值。
    /// </para>
    /// </summary>
    internal class VoidshadeHeld:ModProjectile
    {
        /// <summary>刀光底图（CE 的 CEExtraAssets.white）</summary>
        private const string WhiteTexture = "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>刀光底纹（CE 的 CEExtraAssets.SwordSlashTexture）</summary>
        private const string SwordSlashTexture = "CalamityDemutation/Assets/ExtraTextures/SwordSlashTexture";
        /// <summary>本次招式（<c>ai[0]</c>）：0/1 为左键连段、3 为右键突刺</summary>
        private int AttackType => (int)Projectile.ai[0];
        /// <summary>剑体位置历史（每帧补 1~3 个采样点），绘制时摊成三角带当刀光</summary>
        private readonly List<Vector2> oldPos = new List<Vector2>();
        /// <summary>与 <see cref="oldPos"/> 一一对应的旋转角历史</summary>
        private readonly List<float> oldRots = new List<float>();
        /// <summary>与 <see cref="oldPos"/> 一一对应的缩放历史</summary>
        private readonly List<float> oldScale = new List<float>();
        /// <summary>首帧标记：定初始朝向（左键还要按连段定初始角）</summary>
        private bool init = false;
        /// <summary>左键自旋速度（前 8 帧按连段方向加速，之后按攻速衰减）</summary>
        private float rotSpeed = 0;
        /// <summary>动作进度计数（按攻速累加）：左键每帧 +攻速、超过 60 收招</summary>
        private float counter = 0;
        /// <summary>突刺式剑体的自转速度累加器</summary>
        private float cspeed = 0;
        /// <summary>突刺式剑体的自转角度累加器</summary>
        private float c = 0;
        /// <summary>突刺式的冲刺余量：出手为 30，每帧按攻速递减，撞到敌人翻成 -32（转为收势）</summary>
        private float dash = 30;
        /// <summary>上一帧的剑体缩放，供历史点插值用</summary>
        private float LastScale = 0;
        /// <summary>刀光 UV 的滚动偏移（每帧 +0.06，让底纹动起来）</summary>
        private float trailOffset = 0;
        /// <summary>左键是否还没读取过强化计时（一次挥砍只读一次）</summary>
        private bool sb = true;
        /// <summary>本次挥砍是否吃到了强化（吃到后剑体缩放更大）</summary>
        private bool vsboost = false;
        /// <summary>左键是否已甩出 <see cref="VoidImpact"/>（第 16 帧一次）</summary>
        private bool st = true;
        /// <summary>突刺式是否已放出 <see cref="WohLaser"/>（第 20 帧一次）</summary>
        private bool spawnProj = true;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = 55;         // 55 帧内同一敌人不重复结算（挥砍不会连续蹭伤害）
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
        }
        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            if (Projectile.localAI[1]++ == 0)
            {
                // 首帧套用玩家的近战尺寸加成
                float meleeScale = Main.player[Projectile.owner].HeldItem.scale;
                Main.player[Projectile.owner].ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
            }
            Player player = Main.player[Projectile.owner];
            if (counter < 60)
            {
                player.itemTime = 2;
                player.itemAnimation = 2;
            }
            float speed = player.GetTotalAttackSpeed(Projectile.DamageType);
            if (AttackType == 3)
            {
                // 突刺式：先按余量推动玩家，再用自转把剑甩出去
                if (counter > 9)
                {
                    dash -= speed;
                    if (dash > 0)
                    {
                        player.velocity = Projectile.velocity * 2 * speed;
                    }
                    else
                    {
                        if (dash > -30)
                        {
                            player.velocity *= 0.88f;
                        }
                    }
                }
                if (counter >= 20 && spawnProj)
                {
                    spawnProj = false;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        int laser = Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, Projectile.velocity,
                            ModContent.ProjectileType<WohLaser>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 0, 0, 2);
                        // WohLaser 默认是远程系，这里改判为近战（CE 用 InnoVault 的 .ToProj().DamageType 做同一件事）
                        Main.projectile[laser].DamageType = Projectile.DamageType;
                    }
                }
                player.heldProj = Projectile.whoAmI;
                if (!init)
                {
                    init = true;
                    Projectile.direction = Projectile.velocity.X > 0 ? 1 : -1;
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(16 * Projectile.direction);
                }
                // 前 12 帧加速自转（把剑"甩"出去），之后缓慢收回
                if (counter < 12)
                {
                    cspeed += 0.01f * speed;
                }
                else
                {
                    cspeed -= 0.004f * speed;
                }
                Projectile.Center = player.Center + Projectile.rotation.ToRotationVector2() * c * 18 * GetScale() - Projectile.rotation.ToRotationVector2() * 60 * GetScale();
                c += cspeed * speed;
                Projectile.rotation += -0.003f * c * Projectile.direction * speed;
                counter += speed;
                if (counter > 60)
                {
                    Projectile.Kill();
                    player.itemTime = 1;
                    player.itemAnimation = 1;
                }
                if (counter < 44)
                {
                    // 突刺式只记一个采样点：剑尖位置（沿旋向后偏 10°）
                    oldPos.Add(Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.ToRadians(-10) * Projectile.direction) * 130 * GetScale() * Projectile.scale);
                    oldRots.Add(Projectile.rotation);
                    oldScale.Add(1);
                }
            }
            else
            {
                // 左键：突刺命中后的强化只吃一次，吃到就翻倍并标记（剑体也因此变大）
                if (counter >= 1 && sb && player.GetModPlayer<CalamityDemutationPlayer>().voidshadeBoostTime > 0)
                {
                    sb = false;
                    player.GetModPlayer<CalamityDemutationPlayer>().voidshadeBoostTime = 0;
                    vsboost = true;
                    Projectile.damage *= 2;
                }
                if (counter >= 16 && st)
                {
                    st = false;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, Projectile.velocity * 0.52f,
                            ModContent.ProjectileType<VoidImpact>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                }
                if (!init)
                {
                    init = true;
                    Projectile.direction = Projectile.velocity.X > 0 ? 1 : -1;
                    // 初始角按朝向与连段组合出四个方向，配合后面的自旋形成"左右交替挥砍"
                    if (Projectile.direction == 1)
                    {
                        Projectile.rotation = Projectile.velocity.ToRotation() + (AttackType == 0 ? MathHelper.ToRadians(160) : -MathHelper.ToRadians(160));
                    }
                    else
                    {
                        Projectile.rotation = Projectile.velocity.ToRotation() + (AttackType != 0 ? -MathHelper.ToRadians(160) : MathHelper.ToRadians(160));
                    }
                }
                counter += speed;
                // 前 8 帧加速自旋，之后按攻速衰减（攻速越高衰减越慢）
                if (counter < 8)
                {
                    rotSpeed += (AttackType == 0 ? -0.06f : 0.06f) * Projectile.direction * speed;
                }
                else
                {
                    rotSpeed *= (float)Math.Pow(0.9f, 1.0 / speed);
                }
                if (counter > 60)
                {
                    Projectile.Kill();
                    player.itemTime = 1;
                    player.itemAnimation = 1;
                }
                Projectile.rotation += rotSpeed * Projectile.direction;
                if (counter <= 50)
                {
                    // 前 50 帧才把剑举在手上
                    player.direction = Projectile.velocity.X > 0 ? 1 : -1;
                    player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
                }
                Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
                player.heldProj = Projectile.whoAmI;
                if (counter < 36)
                {
                    // 每帧补 3 个采样点（奇偶帧取玩家当前位或半速位置），并把旋转/缩放按 1/3、2/3 插值补齐
                    Vector2 sample = counter % 2 == 1 ? Projectile.Center : Projectile.Center + player.velocity / 2;
                    oldPos.Add(sample);
                    oldPos.Add(sample);
                    oldPos.Add(sample);
                    oldRots.Add(Projectile.rotation - rotSpeed * Projectile.direction - rotSpeed * Projectile.direction * 0.6666f);
                    oldScale.Add(float.Lerp(GetScale(), LastScale, 0.6666f) * Projectile.scale);
                    oldRots.Add(Projectile.rotation - rotSpeed * Projectile.direction - rotSpeed * Projectile.direction * 0.3333f);
                    oldScale.Add(float.Lerp(GetScale(), LastScale, 0.3333f) * Projectile.scale);
                    oldRots.Add(Projectile.rotation - rotSpeed * Projectile.direction);
                    oldScale.Add(GetScale() * Projectile.scale);
                    LastScale = GetScale();
                }
            }
            // 历史点整体收束：左键最多留 32 段（且过 8 帧才开始收），收招阶段（≥34 帧）则一路清空
            for (int i = 0; i < 3; i++)
            {
                if ((oldPos.Count > 32 && counter > 6) || (counter >= 34 && oldPos.Count > 0))
                {
                    oldPos.RemoveAt(0);
                    oldRots.RemoveAt(0);
                    oldScale.RemoveAt(0);
                }
            }
        }
        /// <summary>
        /// 剑体缩放：突刺式固定 2；左键按自旋速度换算（越接近静止越小、吃到强化时更大），
        /// 基准是 1.2 加上自旋速度与攻速之比的平方根（CE 原式）。
        /// </summary>
        private float GetScale()
        {
            if (AttackType == 3)
            {
                return 2;
            }
            return 1.2f + (float)Math.Sqrt(Math.Abs(rotSpeed / Main.player[Projectile.owner].GetTotalAttackSpeed(Projectile.DamageType) * 6 * (vsboost ? 1.6f : 1)));
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // 只在挥砍的中段（第 6~60 帧）判定，判定范围是从剑心沿朝向伸出的线段
            if (counter > 6 && counter < 60)
            {
                return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 92 * Projectile.scale * GetScale(), targetHitbox, 36);
            }
            return false;
        }
        /// <summary>
        /// 命中时：突刺式的首次命中把冲刺余量翻成收势、反推一下玩家速度并给玩家挂 90 帧挥砍强化；
        /// 左键则播命中音，在目标身上炸一颗 <see cref="SparkleParticle"/> 与一圈 32 颗火花
        /// （一半 <see cref="AltSparkParticle"/>、一半 <see cref="LineParticleCal"/>）。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (dash >= 0 && AttackType == 3)
            {
                dash = -32;
                Main.player[Projectile.owner].velocity *= -0.05f;
                Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().voidshadeBoostTime = 90;
            }
            else
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.VoidshadeHit with { Pitch = Main.rand.NextFloat(0.8f, 1.2f) - 1f }, target.Center);
                SparkleParticle sparkle = new SparkleParticle();
                DRKLoader.NewParticle(sparkle, target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero,
                    Color.LightBlue, Main.rand.NextFloat(1.4f, 1.6f));
                sparkle.Configure(Color.Blue, 8, 0, 2.5f);
                for (int i = 0; i < 32; i++)
                {
                    float p = Main.rand.NextFloat();
                    Vector2 sparkVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero).RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(12, 36 * (2 - p));
                    int sparkLifetime = (int)((2 - p) * 7);
                    float sparkScale = 0.6f + (1 - p);
                    Color sparkColor = Color.Lerp(Color.DeepSkyBlue, Color.Purple, p);
                    Vector2 sparkPos = target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f);
                    if (Main.rand.NextBool())
                    {
                        AltSparkParticle altSpark = new AltSparkParticle();
                        DRKLoader.NewParticle(altSpark, sparkPos, sparkVelocity, sparkColor, sparkScale * 1.4f);
                        altSpark.Configure(false, (int)(sparkLifetime * 1.2f));
                    }
                    else
                    {
                        LineParticleCal line = new LineParticleCal();
                        DRKLoader.NewParticle(line, sparkPos, sparkVelocity * 0.65f, Main.rand.NextBool() ? Color.AliceBlue : Color.SkyBlue, sparkScale);
                        line.Configure(false, sparkLifetime);
                    }
                }
            }
        }
        /// <summary>
        /// 自绘：左键先用两条三角带刀光（紫色底 + 白色芯，底图分别取 white / SwordSlashTexture，
        /// UV 按 <see cref="trailOffset"/> 滚动）在加法混合下画出身位轨迹，
        /// 然后按连段决定朝向画剑体（左下角或右下角为原点、差 50°/130°）；突刺式跳过刀光直接画剑体。
        /// 画完恢复默认批次。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            trailOffset += 0.06f;
            SpriteBatch spriteBatch = Main.spriteBatch;
            if (AttackType != 3)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                for (int i = 0; i < oldRots.Count; i++)
                {
                    Color b = Color.Lerp(Color.Purple * 0.01f, Color.Purple, i / (float)oldRots.Count);
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] * (1 - i / (float)oldRots.Count) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] - 60 * oldScale[i] * (1 - i / (float)oldRots.Count) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = ModContent.Request<Texture2D>(WhiteTexture).Value;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                ve = new List<ColoredVertex>();
                for (int i = 0; i < oldRots.Count; i++)
                {
                    Color b = Color.Lerp(Color.White * 0.01f, Color.White, i / (float)oldRots.Count);
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] * (1 - i / (float)oldRots.Count) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (36 * oldScale[i] + 60 * oldScale[i] - 60 * oldScale[i] * (1 - i / (float)oldRots.Count) * 0.5f),
                          new Vector3(i / (float)oldRots.Count + trailOffset, 0, 1),
                          b));
                }
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = ModContent.Request<Texture2D>(SwordSlashTexture).Value;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
            if (counter <= 60)
            {
                Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
                int dir = AttackType == 0 ? -1 : 1;
                if (AttackType == 3 && Projectile.velocity.X < 0)
                {
                    dir *= -1;
                }
                Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.rotation.ToRotationVector2() * -4 * GetScale() * Projectile.scale - Main.screenPosition, null, Color.White,
                    Projectile.rotation + (dir > 0 ? MathHelper.ToRadians(50) : MathHelper.ToRadians(130)),
                    dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height),
                    Projectile.scale * GetScale(),
                    dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
