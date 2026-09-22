using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 噬渊鞭挞的鞭身（YstralynProj，移植自 CalamityEntropy 的 Content/Projectiles/YstralynProj.cs）：
    /// 原版鞭模板（19 节、射程 ×4.8）＋一条深天蓝的拉伸白线（<see cref="DrawLine"/>），
    /// 另在挥砍中段用控制点尾巴连出一条"深渊裂纹"折线（<c>pointsCrack</c>），
    /// 由上屏管线（<see cref="Common.Effects.EffectsSystem"/> 的深渊分支）经 cabyss 着色器合成，不由本类 PreDraw 画。
    /// 命中时给敌人挂生命压制 + 噬渊标记、给主人挂虚无幻象（召唤幻影妖龙）、并把该敌人设为主人的召唤锁定目标。
    /// <para>
    /// 与 CE 原版的差异：① <c>CEUtils.drawLine</c> 内联成 <see cref="DrawLineSegment"/>；
    /// ② <c>Projectile.owner.ToPlayer()</c>→<c>Main.player[Projectile.owner]</c>；
    /// ③ <c>CEExtraAssets.white</c>→本模组 <c>Assets/ExtraTextures/white</c>；
    /// ④ <c>target.AddBuff&lt;T&gt;()</c>（CE 扩展）→<c>target.AddBuff(ModContent.BuffType&lt;T&gt;(), …)</c>；
    /// ⑤ CE 的 <c>ArmorCrunch</c> 是本模组没有的灾厄减益 → 走软依赖 <c>ApplyCalamityBuff</c> 分别对两版灾厄注册
    /// （现代版 "CalamityMod" / 经典版 "CalamityModClassicPreTrailer"，两版都有 ArmorCrunch）；
    /// ⑥ 命中粒子数：CE 写的是 <c>4 × (IsEmpowered() ? 6 : 1)</c>，而 <c>IsEmpowered()</c> 读的是 CE 的武器充能标记
    /// （<c>CEEmpowerGlobalProjectile</c>，只有 CE 那批蓄势武器会置位，噬渊鞭挞不在其中）——本模组没有这套体系，
    /// 故此处置为恒 false，即每次命中固定撒 4 颗；⑦ <c>Timer</c> 只保留 getter（CE 那个 setter 全工程无人调用）；
    /// ⑧ <c>CEUtils.PlaySound("ystn_hit", pitch, …)</c> 改成 <c>SoundEngine.PlaySound(… with { Pitch = CE 值 - 1, … })</c>
    /// （CE 内部就是 <c>s.Pitch = pitch - 1</c>，同时把它的 volume 0.76 / maxIns 3 一并写进 with）。
    /// </para>
    /// </summary>
    internal class YstralynProj:ModProjectile
    {
        /// <summary>
        /// 深渊裂纹折线：存的是相对玩家中心的偏移（CE 原样，绘制时再加回玩家中心），
        /// 上限 170 点、超出从头丢。由 <see cref="Common.Effects.EffectsSystem"/> 在深渊上屏分支里取用。
        /// </summary>
        public List<Vector2> pointsCrack = new List<Vector2>();
        /// <summary>本模组的纯白像素图（CE 的 CEExtraAssets.white）</summary>
        private const string WhitePixel = "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>注册为原版鞭：命中判定与控制点计算都靠这个标记</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }
        /// <summary>
        /// 基础属性：走原版鞭模板拿到鞭类判定与控制点计算，再把更新频率拉到 8 倍、19 节、射程 ×4.8。
        /// 本地无敌帧 -1 = 同一次挥击里同一敌人只吃一次。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DefaultToWhip();
            Projectile.MaxUpdates = 8;
            Projectile.WhipSettings.Segments = 19;
            Projectile.WhipSettings.RangeMultiplier = 4.8f;
            Projectile.usesLocalNPCImmunity = true;
            // 命中无敌帧：CE 原样是 -1（= 一次挥击对同一敌人只结算一次），2026-09-22 用户拍板改成 5 帧冷却，
            // 让一条 27 帧的挥击可以对同一敌人多段命中（最多约 5 次）。注意无敌帧按 tick 递减，与 MaxUpdates 无关
            Projectile.localNPCHitCooldown = 5;
        }
        /// <summary>挥砍计时：原版鞭 AI（165）每帧推进 <c>ai[0]</c>，这里只读</summary>
        private float Timer => Projectile.ai[0];
        /// <summary>
        /// 挥砍进度落在 0.46~0.9 区间时，把"控制点尾巴（鞭梢）相对玩家中心"的位置连成折线：
        /// 每帧往折线里补 10 个插值点（从上一个点线性插到本帧鞭梢），因此鞭梢扫过哪里，裂纹就画到哪里。
        /// 返回 true 交还给原版鞭 AI（位移与帧都是原版算的）。
        /// </summary>
        public override bool PreAI()
        {
            Player owner = Main.player[Projectile.owner];
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
            List<Vector2> points = Projectile.WhipPointsForCollision;
            points.Clear();
            Projectile.FillWhipControlPoints(Projectile, points);
            float swingProgress = Timer / swingTime;
            if (swingProgress > 0.46f && swingProgress < 0.9f)
            {
                Vector2 tip = points[points.Count - 1] - owner.Center;
                if (pointsCrack.Count > 0)
                {
                    Vector2 previous = pointsCrack[pointsCrack.Count - 1];
                    for (float i = 0.1f; i <= 1f; i += 0.1f)
                    {
                        pointsCrack.Add(Vector2.Lerp(previous, tip, i));
                        if (pointsCrack.Count > 170)
                        {
                            pointsCrack.RemoveAt(0);
                        }
                    }
                }
                else
                {
                    pointsCrack.Add(tip);
                }
            }
            return true;
        }
        /// <summary>
        /// 命中敌人：挂生命压制（4501 点/秒）、把该敌人设为主人的召唤锁定目标、
        /// 给主人挂虚无幻象（据此召唤幻影妖龙）、给敌人挂噬渊标记（仆从命中的 tag 加伤）、
        /// 再按软依赖挂灾厄的护甲碎裂，最后放命中音效并在敌人中心撒 4 颗深渊斩击线粒子。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<LifeOppress>(), 600);
            owner.AddBuff(ModContent.BuffType<WyrmPhantom>(), 480);
            target.AddBuff(ModContent.BuffType<WyrmWhipDebuff>(), 380);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ArmorCrunch", 600);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "ArmorCrunch", 600);
            owner.MinionAttackTargetNPC = target.whoAmI;
            SoundEngine.PlaySound(CalamityDemutationSounds.YstralynHit with { Pitch = Main.rand.NextFloat(0.86f, 1.2f) - 1f, Volume = 0.76f, MaxInstances = 3 }, target.Center);
            for (int i = 0; i < 4; i++)
            {
                AbyssalLineParticle particle = new AbyssalLineParticle();
                DRKLoader.NewParticle(particle, target.Center, Vector2.Zero, Color.White, 1f);
                particle.Configure(Main.rand.NextFloat(MathHelper.TwoPi));
                particle.LineScale = 1.2f;
                particle.XAdd = 1.2f;
            }
        }
        /// <summary>
        /// 把深渊裂纹折线画成一条条细线（**屏幕空间**：由上屏管线的 RT 批次调用，那套批次不带视图矩阵，
        /// 故这里按惯例减 <c>Main.screenPosition</c>）。线宽取 CE 原式：先按挥砍进度算一个 0~0.26 的亮度系数，
        /// 再乘 <c>RotatedBy</c> 的 Y 分量——那是条 -100~+100 的正弦摆动，故宽度会隔着取到负值（CE 原样，别"修"）。
        /// </summary>
        public void draw_crack()
        {
            if (pointsCrack.Count < 1)
            {
                return;
            }
            Player owner = Main.player[Projectile.owner];
            Texture2D pixel = ModContent.Request<Texture2D>(WhitePixel).Value;
            float widthScale = 1f - Timer / (owner.itemAnimationMax * Projectile.MaxUpdates);
            if (widthScale > 0.26f)
            {
                widthScale = 0.26f;
            }
            for (int i = 1; i < pointsCrack.Count; i++)
            {
                float width = widthScale * new Vector2(-100f, 0f).RotatedBy(MathHelper.ToRadians(180f * (i / (float)pointsCrack.Count))).Y;
                DrawLineSegment(Main.spriteBatch, pixel, pointsCrack[i - 1] + owner.Center, pointsCrack[i] + owner.Center, Color.White, width);
            }
        }
        /// <summary><c>CEUtils.drawLine</c> 的等价内联：以起点为轴拉一条长 <c>diff.Length() + 4</c>、宽 <paramref name="width"/> 的线段</summary>
        private static void DrawLineSegment(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            spriteBatch.Draw(pixel, start - Main.screenPosition, null, color, (end - start).ToRotation(), new Vector2(0f, 0.5f), new Vector2(Vector2.Distance(start, end) + 4f, width), SpriteEffects.None, 0);
        }
        /// <summary>
        /// 沿控制点串一条深天蓝的拉伸细线（CE 原样用白图拉长画，末段比其它段短 8 像素以免超出鞭梢）。
        /// </summary>
        private void DrawLine(List<Vector2> list)
        {
            Texture2D pixel = ModContent.Request<Texture2D>(WhitePixel).Value;
            Rectangle frame = pixel.Frame();
            Vector2 origin = new Vector2(0f, 0.5f);
            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 1; i++)
            {
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;
                Vector2 scale = new Vector2(diff.Length() + 2f, 2f);
                if (i == list.Count - 2)
                {
                    scale.X -= 8f;
                }
                Main.EntitySpriteDraw(pixel, pos - Main.screenPosition, frame, Color.DeepSkyBlue, diff.ToRotation(), origin, scale, SpriteEffects.None, 0);
                pos += diff;
            }
        }
        /// <summary>
        /// 绘制：先画那条深天蓝细线，再把鞭身贴图沿控制点逐段拼接——手柄取 (0,0,34,36)，
        /// 中段按奇偶交替取 y=36 / y=50 高 14 的两张帧，末端取 y=64 高 20 的帧并按挥砍进度做 0.5~1.5 的缩放。
        /// 朝向由原版鞭 AI 维护的 <c>spriteDirection</c> 决定。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> list = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list);
            DrawLine(list);
            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.instance.LoadProjectile(Type);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 1; i++)
            {
                Rectangle frame = new Rectangle(0, 0, 34, 36);
                Vector2 origin = new Vector2(17f, 16f);
                float scale = 1.5f;
                if (i == list.Count - 2)
                {
                    frame.Y = 64;
                    frame.Height = 20;
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true)) * 1.5f;
                    origin = new Vector2(17f, 0f);
                }
                else if (i > 0)
                {
                    frame.Y = i % 2 == 0 ? 36 : 50;
                    frame.Height = 14;
                    origin = new Vector2(17f, 0f);
                }
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;
                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, Color.White, diff.ToRotation() - MathHelper.PiOver2, origin, scale, flip, 0);
                pos += diff;
            }
            return false;
        }
    }
}
