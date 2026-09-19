using CalamityDemutation.Effects;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 无星之夜手持弹幕（StarlessNightProj，移植自 CalamityEntropy）：举剑蓄势、起手两段挥砍音，
    /// 随后加速旋转甩出一整圈刀光，收尾阶段朝鼠标重新定向并减速收招。
    /// 刀光本体是一个按历史角度逐帧记录出来的三角带（<see cref="odr"/>/<see cref="ods"/>），
    /// 用 <c>SlashTrans</c> 着色器（EnchantedPass）采样噪声贴图与配色图合成，
    /// 再叠两笔 <c>MotionTrail2</c> 让它有实体感。
    /// 命中敌人会在其身上炸出虚空新星（<see cref="VoidStarF"/>），每个敌人最多吃两次。
    /// <para>
    /// 剑体不由本弹幕的 <see cref="PreDraw"/> 绘制——CE 把 <c>drawSword()</c> 放在它的全局绘制层里调用，
    /// 好让剑体压在所有弹幕之上，本模组照搬同一分层，由 <c>EffectsSystem</c> 在屏幕捕获末尾调用
    /// <see cref="DrawSword"/>。
    /// <para>
    /// 与 CE 原版的差异：① CE 命中时额外生成的 <c>VoidExplode</c> 是个空壳
    /// （无 AI、不绘制、0 伤害、连贴图都没有，只在 PvP 挂一个 CE 自己的减益），省掉；
    /// ② <c>CEUtils.PlaySound</c> 换成本模组 <see cref="CalamityDemutationSounds"/>，音高按 CE 的
    /// <c>pitch - 1</c> 口径换算；③ <c>CEUtils.WeapSound</c> 按 1.0 处理；
    /// ④ <c>CEUtils.RotateTowardsAngle</c>/<c>LineThroughRect</c> 走本模组 <see cref="CDUtil"/> 里的同名移植版，
    /// <c>randomRot</c> 内联为 <c>Main.rand.NextFloat(MathHelper.TwoPi)</c>；
    /// ⑤ <c>CEEffectAssets.SlashTrans</c> 走本模组的 <see cref="CDShaders.SlashTransShader"/>，
    /// <c>CEExtraAssets</c> 的几张额外贴图走本模组 Assets/ExtraTextures 下的同名文件；
    /// ⑥ <c>glowalpha</c> 在 CE 原版里从未被赋值（恒为 0，Glow 层实际不显示），此处照抄未改。
    /// </para>
    /// </summary>
    internal class StarlessNightProj:ModProjectile
    {
        /// <summary>刀光噪声贴图（CE 的 CEExtraAssets.GradientNoise）</summary>
        private const string GradientNoiseTexture = "CalamityDemutation/Assets/ExtraTextures/GradientNoise";
        /// <summary>刀光条纹贴图（CE 的 CEExtraAssets.MotionTrail2）</summary>
        private const string MotionTrailTexture = "CalamityDemutation/Assets/ExtraTextures/MotionTrail2";
        /// <summary>刀光配色图，喂给 SlashTrans 的 uTransformImage（CE 的 Assets/Extra/sn_colormap）</summary>
        private const string ColormapTexture = "CalamityDemutation/Assets/ExtraTextures/sn_colormap";
        /// <summary>剑体辉光贴图（CE 的 CEExtraAssets.StarlessNightGlow）</summary>
        private const string SwordGlowTexture = "CalamityDemutation/Content/Items/Weapons/Melee/StarlessNightGlow";
        /// <summary>旋转角历史（每帧一条，与 <see cref="ods"/> 一一对应，最多留 84 条）</summary>
        private readonly List<float> odr = new List<float>();
        /// <summary>与旋转角历史对应的缩放历史</summary>
        private readonly List<float> ods = new List<float>();
        /// <summary>每个敌人已命中次数：超过 1 次就不再判定（一次挥砍里同个敌人最多吃两下）</summary>
        public int[] NPCHitCounts = new int[Main.npc.Length];
        /// <summary>还能放几轮虚空新星（每命中一次消耗一轮，一轮 6 颗）</summary>
        private int spawnVoidStarCount = 5;
        /// <summary>绘制缩放系数（CE 里全程恒为 0.64f，未随进度变化）</summary>
        private float scaleD = 0.64f;
        /// <summary>当前每帧的旋转角速度（联机时需要同步，见 <see cref="SendExtraAI"/>）</summary>
        private float rotSpeed = 0f;
        /// <summary>剑体辉光的透明度（CE 原版从未赋值，恒 0 → Glow 层实际不显示，照抄）</summary>
        private float glowalpha = 0;
        /// <summary>起手两段挥砍音各只播一次</summary>
        private bool playsound1 = true;
        private bool playsound2 = true;
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/StarlessNight";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;                        // 命中判定完全由 Colliding 的线段接管
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 36;
            Projectile.ArmorPenetration = 80;
            Projectile.timeLeft = 100000;                // 占位值，真正的结束判据是 ai[0] 的计时
            Projectile.extraUpdates = 3;
        }
        /// <summary>联机同步旋转速度：它由本端按攻速累加，不传的话别的端转不起来</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(rotSpeed);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            rotSpeed = reader.ReadSingle();
        }
        /// <summary>命中时：计数、震屏、播命中音，并消耗一轮放 6 颗近战伤害的虚空新星</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            NPCHitCounts[target.whoAmI]++;
            CalamityDemutation.ScreenShakeAmp = 6f;
            SoundEngine.PlaySound((Main.rand.NextBool() ? CalamityDemutationSounds.StarlessNightHit1 : CalamityDemutationSounds.StarlessNightHit3)
                with { Pitch = Main.rand.NextFloat(0.7f, 1.3f) - 1f, Volume = 0.7f }, Projectile.Center);
            if (spawnVoidStarCount > 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 16;
                    int index = Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, vel,
                        ModContent.ProjectileType<VoidStarF>(), Projectile.damage / 5, 1, Projectile.owner);
                    Main.projectile[index].DamageType = DamageClass.Melee;
                }
                spawnVoidStarCount--;
            }
        }
        public override void AI()
        {
            float updates = Projectile.MaxUpdates + 1;
            if (Projectile.ai[0] == 0)
            {
                Projectile.direction = Projectile.velocity.X > 0 ? 1 : -1;
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.rotation -= 2.42f * Projectile.direction;
            }
            Player owner = Main.player[Projectile.owner];
            float meleeSpeed = owner.GetTotalAttackSpeed(Projectile.DamageType);
            if (Projectile.ai[0] >= 64 * updates && playsound1)
            {
                playsound1 = false;
                SoundEngine.PlaySound(CalamityDemutationSounds.StarlessNightSwing with { Pitch = Main.rand.NextFloat(0.6f, 0.8f) - 1f }, Projectile.Center);
            }
            if (Projectile.ai[0] >= 74 * updates && playsound2)
            {
                playsound2 = false;
                SoundEngine.PlaySound(CalamityDemutationSounds.StarlessNightSwing with { Pitch = Main.rand.NextFloat(0.8f, 1f) - 1f }, Projectile.Center);
            }
            Projectile.Center = owner.MountedCenter + owner.gfxOffY * Vector2.UnitY;
            Projectile.rotation += rotSpeed * meleeSpeed;
            if (Projectile.ai[0] < 60 * updates)
            {
                // 起手：把计时器直接顶到 60 帧处蓄势
                Projectile.ai[0] = 60 * updates;
            }
            else
            {
                if (Projectile.ai[0] < 86 * updates)
                {
                    rotSpeed += 0.00121f * Projectile.direction * meleeSpeed;
                }
                else
                {
                    // 收招：转速按攻速的倒数衰减；90 帧后朝鼠标重新定向，100 帧掐断
                    rotSpeed *= (float)Math.Pow(0.94, 1.0 / meleeSpeed);
                    if (Projectile.ai[0] > 90 * updates)
                    {
                        if (Projectile.owner == Main.myPlayer)
                        {
                            Projectile.direction = (Main.MouseWorld - owner.Center).X > 0 ? 1 : -1;
                            float targetrot = (Main.MouseWorld - owner.Center).ToRotation() - 2.42f * Projectile.direction;
                            Projectile.rotation = CDUtil.RotateTowardsAngle(Projectile.rotation, targetrot, 0.07f * meleeSpeed, false);
                        }
                    }
                    if (Projectile.ai[0] > 100 * updates)
                    {
                        owner.itemTime = 0;
                        owner.itemAnimation = 0;
                        Projectile.Kill();
                        return;
                    }
                }
            }
            Projectile.ai[0] += meleeSpeed;
            // 每帧往历史里压一条「朝当前朝向靠拢」的角度，使刀光带平滑地跟上剑的转动
            if (odr.Count > 0)
            {
                odr.Add(CDUtil.RotateTowardsAngle(odr[odr.Count - 1], Projectile.rotation, 0.5f, false));
                ods.Add(scaleD);
            }
            odr.Add(Projectile.rotation);
            ods.Add(scaleD);
            if (odr.Count > 84)
            {
                odr.RemoveAt(0);
                ods.RemoveAt(0);
            }
            owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
        }
        public override bool ShouldUpdatePosition() => false;
        /// <summary>同一个敌人最多命中两次（一次挥砍里手可以扫过同一目标两遍）</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (NPCHitCounts[target.whoAmI] > 1)
            {
                return false;
            }
            return base.CanHitNPC(target);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // 这里只画刀光；剑体交给 EffectsSystem 在所有弹幕画完之后补（CE 也是把 drawSword 放在那一层调的）
            DrawSlash(withTrail: true);
            return false;
        }
        /// <summary>
        /// 画剑体本身：先 Glow 层（CE 的 glowalpha 恒 0，实际不显示），再本体，最后把批次还原成默认。
        /// <para>
        /// CE 在它的全局绘制层里逐个弹幕调这个方法，好让剑体压在所有弹幕之上；本模组照搬同一分层，
        /// 由 <c>EffectsSystem</c> 在屏幕捕获末尾调用——所以要对外可见。
        /// </para>
        /// </summary>
        public void DrawSword()
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>(SwordGlowTexture).Value;
            Vector2 pos = Main.player[Projectile.owner].MountedCenter - Main.screenPosition;
            float rot = Projectile.rotation + MathHelper.PiOver4;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(glow, pos, null, new Color(180, 180, 255) * glowalpha * 0.8f, rot, new Vector2(32, 168), Projectile.scale * 3f * scaleD, SpriteEffects.None);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(texture, pos, null, Color.White, rot, new Vector2(0, texture.Height), Projectile.scale * 2.86f * scaleD, SpriteEffects.None);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        /// <summary>
        /// 画刀光：把旋转角历史摊成一条三角带（内侧顶点都在剑心、外侧顶点沿历史角度伸出 680 像素），
        /// 用 SlashTrans 的 EnchantedPass 采样噪声贴图当底、sn_colormap 当配色合成；
        /// <paramref name="withTrail"/> 为真时同一组顶点再用 MotionTrail2 叠两笔实体拖尾。
        /// </summary>
        private void DrawSlash(bool withTrail)
        {
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Texture2D tail = ModContent.Request<Texture2D>(MotionTrailTexture).Value;
            Texture2D tail2 = ModContent.Request<Texture2D>(GradientNoiseTexture).Value;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();
            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(255, 255, 255) * (i / (float)odr.Count);
                // 外侧顶点（纹理坐标 y=1）与内侧顶点（y=0）交替，三角带自然把两者连成一片扇形
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(680 * ods[i] * Projectile.scale, 0).RotatedBy(odr[i])),
                    new Vector3(i / (float)odr.Count, 1, 1), b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(0 * ods[i] * Projectile.scale, 0).RotatedBy(odr[i])),
                    new Vector3(i / (float)odr.Count, 0, 1), b));
            }
            if (ve.Count >= 3)
            {
                Effect shader = CDShaders.SlashTransShader.Value;
                gd.Textures[1] = ModContent.Request<Texture2D>(ColormapTexture).Value;
                shader.CurrentTechnique.Passes["EnchantedPass"].Apply();
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, shader, Main.GameViewMatrix.TransformationMatrix);
                gd.Textures[0] = tail2;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                if (withTrail)
                {
                    gd.Textures[0] = tail;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    gd.Textures[0] = tail;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, sb.GraphicsDevice.BlendState, sb.GraphicsDevice.SamplerStates[0], sb.GraphicsDevice.DepthStencilState, sb.GraphicsDevice.RasterizerState, null, Main.GameViewMatrix.TransformationMatrix);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        /// <summary>命中判定是从玩家中心朝当前朝向伸出的一条长线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 628 * Projectile.scale * scaleD, targetHitbox, 64);
        }
    }
}
