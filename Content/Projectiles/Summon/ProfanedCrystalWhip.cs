using System;
using System.Collections.Generic;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎之魂水晶·鞭转化的水晶鞭（移植自灾厄 2.2.2 的 ProfanedCrystalWhip）。
    /// 使用鞭类武器（SummonMeleeSpeedDamageClass）时额外甩出一条 40 节、射程 ×2.25 的水晶鞭，
    /// 命中敌人时给主人挂上水晶鞭增益 ProfanedCrystalWhipBuff（30 秒）并把该敌人设为主人的右键锁定目标。
    /// 挥砍推进与几何由本类自己负责（SetDefaults 里把 aiStyle 置 0 关掉原版鞭 AI，AI 末尾自增 Timer），
    /// 与灾厄自家的 BaseWhipProjectile 同一做法；这样也顺带避开了原版鞭 AI 首帧"强制主人重播武器动画"
    /// 的分支——本工程保留原武器使用，重播动画会让武器多打一次。
    /// 伤害按通用伤害折算（originalDamage 为未折算的基础值 500，由派发端写入）。
    /// </summary>
    internal class ProfanedCrystalWhip:ModProjectile
    {
        // ── 状态与属性 ──
        /// <summary>鞭身配色：白天强制取 Vanity 档（暖橙），夜晚取当前四态配色</summary>
        private Color specialColor = Color.Orange;
        /// <summary>当前鞭身绘制色</summary>
        public Color SpecialDrawColor => specialColor;
        // ── 生命周期方法 ──
        /// <summary>登记为鞭类弹幕（启用原版鞭的命中与控制点计算）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }
        /// <summary>
        /// 基础属性：走原版鞭模板（DefaultToWhip）拿到鞭类判定与控制点计算，40 节、射程 ×2.25，
        /// 伤害类型改成通用；最后把 aiStyle 置 0 关掉原版鞭 AI——它的首帧分支会强制主人重播武器动画
        /// （会让被保留使用的原武器多打一次），而挥砍推进改由 AI 末尾的 Timer++ 自行负责
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DefaultToWhip();
            Projectile.WhipSettings.Segments = 40;
            Projectile.WhipSettings.RangeMultiplier = 2.25f;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.aiStyle = 0;
        }
        // ── 覆写方法 ──
        /// <summary>每帧先刷新鞭身配色，再走正常鞭逻辑</summary>
        public override bool PreAI()
        {
            ExtraBehavior();
            return true;
        }
        /// <summary>配色：白天固定 Vanity 档暖橙、夜晚取当前四态配色（对齐 2.2.2 的 GetColorForPsc 调用）</summary>
        public void ExtraBehavior()
        {
            var player = Main.player[Projectile.owner];
            specialColor = ProfanedSoulCrystal.GetColorForPsc(Main.dayTime ? (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Vanity : player.GetModPlayer<CalamityDemutationPlayer>().pscState, Main.dayTime);
        }
        /// <summary>挥砍计时（= ai[0]）：由原版鞭 AI 每帧自增，归零即自毁</summary>
        private float Timer
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        /// <summary>
        /// AI：每帧按通用伤害重算 damage（抵消多次加成叠加）；随后是复刻的原版鞭 AI 主体
        /// （位置钉在手臂 + 速度 × 计时、朝向取速度方向、计时走完即自毁、挥到中点放音效、鞭梢附近喷粉尘）。
        /// 原灾厄在此处还有"首帧强制主人把当前武器动画重播一遍"（owner.ApplyItemAnimation）——
        /// 本工程保留原武器使用，重播动画会让武器多打一次，故整段删除；末尾补 Timer++ 自行推进挥砍
        /// （原版鞭 AI 已被 aiStyle = 0 关掉，灾厄的 BaseWhipProjectile 也是同样自增）。
        /// </summary>
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.damage = (int)owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * Timer;
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
            if (Timer >= swingTime)
            {
                Projectile.Kill();
                return;
            }
            owner.heldProj = Projectile.whoAmI;
            if (Timer == swingTime / 2)
            {
                List<Vector2> points = Projectile.WhipPointsForCollision;
                Projectile.FillWhipControlPoints(Projectile, points);
                SoundEngine.PlaySound(SoundID.Item153, points[points.Count - 1]);
            }
            float swingProgress = Timer / swingTime;
            if (Utils.GetLerpValue(0.1f, 0.7f, swingProgress, clamped: true) * Utils.GetLerpValue(0.9f, 0.7f, swingProgress, clamped: true) > 0.5f && !Main.rand.NextBool(3))
            {
                List<Vector2> points = Projectile.WhipPointsForCollision;
                points.Clear();
                Projectile.FillWhipControlPoints(Projectile, points);
                int pointIndex = points.Count - 1;
                Rectangle spawnArea = Utils.CenteredRectangle(points[pointIndex], new Vector2(30f, 30f));
                int dustType = DustID.VenomStaff;
                for (int i = 0; i < 2; i++)
                {
                    Dust dust = Dust.NewDustDirect(spawnArea.TopLeft(), spawnArea.Width, spawnArea.Height, dustType, 0f, 0f, 100, Color.White);
                    dust.position = points[pointIndex];
                    dust.fadeIn = 0.3f;
                    dust.scale = 2f;
                    Vector2 spinningpoint = points[pointIndex] - points[pointIndex - 1];
                    dust.noGravity = true;
                    dust.velocity *= 0.5f;
                    dust.velocity += spinningpoint.RotatedBy(owner.direction * ((float)Math.PI / 2f));
                    dust.velocity *= 0.5f;
                }
            }
            Timer++; // 自行推进挥砍计时（原版鞭 AI 已关掉，见 SetDefaults 注释）
        }
        /// <summary>把控制点串成一条用原版钓线贴图绘制的细线（鞭身的"水晶丝"）</summary>
        private void DrawLine(List<Vector2> list)
        {
            Texture2D texture = TextureAssets.FishingLine.Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = new Vector2(frame.Width / 2, 0);
            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 2; i++)
            {
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;
                float rotation = diff.ToRotation() - MathHelper.PiOver2;
                Vector2 scale = new Vector2(1, (diff.Length()) / frame.Height);
                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, SpecialDrawColor, rotation, origin, scale, SpriteEffects.None, 0);
                pos += diff;
            }
        }
        /// <summary>
        /// 绘制：先画水晶丝，再把鞭身贴图沿控制点逐段拼接——手柄固定取 (0,0,16,22)，
        /// 第 1/2/3 段依次取 y=38/70/102 高 18 的帧，末端取 y=126 高 34 的帧并按挥砍进度做 0.5~1.5 的缩放。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> list = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list);
            DrawLine(list);
            SpriteEffects flip = SpriteEffects.FlipHorizontally;
            Main.instance.LoadProjectile(Type);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 1; i++)
            {
                Rectangle frame = new Rectangle(0, 0, 16, 22); // 手柄尺寸（像素）
                Vector2 origin = new Vector2(5, 8);             // 握持点相对贴图左上角的偏移
                float scale = 1;
                if (i == list.Count - 2)
                {
                    frame.Y = 126;      // 末端帧在贴图中的起始高度
                    frame.Height = 34;  // 末端帧高度
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
                }
                else if (i > 6)
                {
                    frame.Y = 102;      // 第三段
                    frame.Height = 18;
                }
                else if (i > 3)
                {
                    frame.Y = 70;       // 第二段
                    frame.Height = 18;
                }
                else if (i > 0)
                {
                    frame.Y = 38;       // 第一段
                    frame.Height = 18;
                }
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;
                float rotation = diff.ToRotation() - MathHelper.PiOver2;
                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, Color.White, rotation, origin, scale, flip, 0);
                pos += diff;
            }
            return false;
        }
        /// <summary>
        /// 命中敌人：给主人挂 30 秒水晶鞭增益（守护者切入强化节奏）、给敌人挂 30 秒鞭痕 tag
        /// （ProfanedCrystalWhipDebuff：主人的仆从与转化弹幕打该敌人时多打 20%，强化档 40%），
        /// 并把该敌人设为主人的右键锁定目标；本弹幕自身伤害打 7 折作为多段命中的惩罚。
        /// 原版的 tag 加伤挂在灾厄内部的 SummonTag 体系（CalamityBuffSets + CalamityGlobalNPC）上，外部接不进去，
        /// 本工程自建等价物：tag 用原版的 BuffID.Sets.IsATagBuff 标记，加伤在 GlobalProjectile.ModifyHitNPC 里结算。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int buffTime = 1800; // 30 秒，留出换武器的时间
            Main.player[Projectile.owner].AddBuff(ModContent.BuffType<ProfanedCrystalWhipBuff>(), buffTime);
            target.AddBuff(ModContent.BuffType<ProfanedCrystalWhipDebuff>(), buffTime);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            Projectile.damage = (int)(Projectile.damage * 0.7f);
        }
    }
}
