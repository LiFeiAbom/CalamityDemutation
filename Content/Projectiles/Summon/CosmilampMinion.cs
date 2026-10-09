using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 宇宙灯笼（照灾厄 2.0.3.9 <c>Projectiles/Summon/CosmilampMinion.cs</c> 移植）：
    /// 20×20 判定、**一盏吃 2 格召唤栏**（<see cref="Cosmilamp.LanternSummonCost"/>）、
    /// `MaxUpdates = 2`、**自身无接触伤害**；平时按"阵型"悬在主人头顶（多盏时从左到右均匀排开、
    /// 各自带正弦上下浮动），每盏按 105 帧的节拍（相位由阵型位置决定）朝 1360 像素内的目标甩一道
    /// <see cref="CosmilampBeam"/>。
    /// </summary>
    /// <remarks>
    /// 贴图照源**复用 Signus Boss 的宇宙灯笼图**（源写 `Texture => "CalamityMod/NPCs/Signus/CosmicLantern"`），
    /// 本工程把它原样拷成同名贴图 `Content/Projectiles/Summon/CosmilampMinion.png`（26×176 = 4 帧 ×26×44），
    /// 因而不用写显式 `Texture` 覆盖。灯具本身不造成接触伤害，伤害都记在光束上。
    /// </remarks>
    internal class CosmilampMinion:ModProjectile
    {
        /// <summary>主人</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>阵型序号（出手时由武器写进 `ai[0]`）</summary>
        public int HoverOffsetIndex => (int)Projectile.ai[0];
        /// <summary>
        /// 阵型插值：只有一盏时固定取 0.5（居中，免得别扭地偏在左边），
        /// 多盏时按序号在 0~1 之间均分（照源）。
        /// </summary>
        public float HoverOffsetInterpolant
        {
            get
            {
                float projectileCounts = Owner.ownedProjectileCounts[Type];
                if (projectileCounts <= 1f)
                    return 0.5f;

                return HoverOffsetIndex / (projectileCounts - 1f);
            }
        }
        /// <summary>通用计时器（`ai[1]`）；也是出手时被武器归零的那个</summary>
        public ref float Timer => ref Projectile.ai[1];
        /// <summary>主人的本模组玩家实例（读写 <c>cLamp</c> 标志）</summary>
        public CalamityDemutationPlayer ModdedOwner => Owner.GetModPlayer<CalamityDemutationPlayer>();

        /// <summary>4 帧动画；可牺牲、可被右键标记目标；残影缓存 8 格、模式 2（照源）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>基础属性：照源（20×20、占 2 栏、`MaxUpdates = 2`、不撞地形、不穿水）</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.minionSlots = Cosmilamp.LanternSummonCost;
            Projectile.timeLeft = 90000;
            Projectile.penetrate = -1;
            Projectile.minion = true;
            Projectile.MaxUpdates = 2;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>续命／动画／悬停／计时／开火的五步（照源按方法拆开，这里同样分方法）</summary>
        public override void AI()
        {
            HandleMinionBools();
            DecideFrames();
            HoverInPlace();
            Timer++;

            NPC potentialTarget = Projectile.Center.MinionHoming(Cosmilamp.MaxTargetingDistance, Owner);
            if (potentialTarget is not null)
            {
                int wrappedAttackTimer = (int)(Timer % Cosmilamp.BeamShootRate);
                // 每盏灯笼的相位不同，于是阵型里各盏是"依次开火"的观感（照源）
                if (wrappedAttackTimer == (int)(HoverOffsetInterpolant * (Cosmilamp.BeamShootRate - 18f)))
                {
                    SoundEngine.PlaySound(SoundID.Item158, Projectile.Center);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Vector2 beamVelocity = Projectile.SafeDirectionTo(potentialTarget.Center).RotatedByRandom(0.32f) * Main.rand.NextFloat(9.4f, 11f);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, beamVelocity, ModContent.ProjectileType<CosmilampBeam>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                }
            }
        }
        /// <summary>挂增益：主人死亡时清标志，标志还在就给自己压到 2 帧续命</summary>
        public void HandleMinionBools()
        {
            Owner.AddBuff(ModContent.BuffType<CosmilampBuff>(), 3600);
            if (Owner.dead)
                ModdedOwner.cLamp = false;

            if (ModdedOwner.cLamp)
                Projectile.timeLeft = 2;
        }
        /// <summary>4 帧动画：每 8 帧翻一帧（照源）</summary>
        public void DecideFrames()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 8 % Main.projFrames[Projectile.type];
        }
        /// <summary>
        /// 悬停：目标点在主人头顶（横向按阵型插值在 -100~100 之间均分、纵向 -80），
        /// 再叠一个随阵型与计时器走的正弦上下浮动；用"先插值再 MoveTowards"的写法贴过去（照源）。
        /// </summary>
        public void HoverInPlace()
        {
            Vector2 hoverDestination = Owner.Top + new Vector2(MathHelper.Lerp(-100f, 100f, HoverOffsetInterpolant), -80f);
            hoverDestination.Y += ((float)Math.Sin(MathHelper.TwoPi * HoverOffsetInterpolant + Timer / 50f) * 0.5f + 0.5f) * 40f;

            Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestination, 0.02f).MoveTowards(hoverDestination, 8f);
        }
        /// <summary>自绘：位置残影（品红→青）+ 8 点青蓝背光 + 本体（照源）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // 位置残影：越靠后的残影越淡
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float afterimageFade = (1f - i / (float)Projectile.oldPos.Length);
                Color afterimageDrawColor = Color.Lerp(Color.Fuchsia, Color.Cyan, afterimageFade) with { A = 25 } * Projectile.Opacity * afterimageFade * 0.6f;
                Vector2 afterimageDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, afterimageDrawPosition, frame, afterimageDrawColor, Projectile.rotation, origin, Projectile.scale, direction, 0);
            }

            // 一圈青蓝背光：把本体轮廓往外"糊"出来
            for (int i = 0; i < 8; i++)
            {
                Color afterimageDrawColor = Color.Cyan with { A = 25 } * Projectile.Opacity * 0.4f;
                Vector2 afterimageDrawPosition = Projectile.Center - Main.screenPosition + (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3f;
                Main.EntitySpriteDraw(texture, afterimageDrawPosition, frame, afterimageDrawColor, Projectile.rotation, origin, Projectile.scale, direction, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, direction, 0);
            return false;
        }
        /// <summary>灯具本身不造成接触伤害（照源：伤害只是"存着"给光束用）</summary>
        public override bool? CanDamage() => false;
    }
}
