using System;
using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Particles;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 弑神者冲刺（移植自灾厄 2.0.x 的 CalPlayer/Dashes/GodslayerArmorDash 及其 PlayerDashEffect 框架）。
    /// 与灾厄实现的两点区别：
    /// 1) 不依赖灾厄的 dash 框架与反射——状态、位移、命中、阻拦原版冲刺全由本模组结算；
    /// 2) 冷却改用一个 ModBuff（<see cref="GodSlayerDashCooldown"/>，45 秒），不使用灾厄的 cooldown 系统。
    /// 冲刺为八方向（朝向由方向键组合决定，与灾厄一致），起步 80、自持上限 40，命中 3000 基础伤害（吃通用职业加成）
    /// 并附加 300 帧 GodSlayerInferno。视觉与音效已按灾厄原文完整移植：
    /// 起手 1 个 DirectionalPulseRing + 16 颗尘，前 20 帧每帧一对 Jaws（洋红 + 青色）、
    /// 全程环形尘环 + 每帧 2 颗火花，第 21 帧补两层 DirectionalPulseRing；音效为吞噬者死亡/冲击音。
    /// </summary>
    internal partial class CalamityDemutationPlayer : ModPlayer
    {
        // ── 常量 ──
        /// <summary>冲刺起步速度（像素/帧）</summary>
        private const float DashStartSpeed = 80f;
        /// <summary>冲刺自持速度上限；超过它按 DashSpeedDeceleration 衰减，否则按跑步速度档衰减</summary>
        private const float DashMidSpeed = 40f;
        /// <summary>速度高于 DashMidSpeed 时的每帧衰减系数</summary>
        private const float DashSpeedDeceleration = 0.985f;
        /// <summary>速度降到跑步速度区间后的每帧衰减系数</summary>
        private const float RunSpeedDeceleration = 0.8f;
        /// <summary>冲刺持续帧数，走满后进入收尾</summary>
        private const int DashDuration = 25;
        /// <summary>冲刺收尾时写回的原版 dashDelay（一小段"刚冲刺过"的窗口）</summary>
        private const int DashEndDelay = 30;
        /// <summary>冷却时长（帧）：45 秒</summary>
        private const int DashCooldownFrames = 45 * 60;
        /// <summary>命中基础伤害（未计任何加成）</summary>
        private const int DashHitDamage = 3000;
        /// <summary>命中基础击退</summary>
        private const float DashHitKnockback = 15f;
        /// <summary>命中后的冲刺免疫帧数（玩家与被打的敌人各一份）</summary>
        private const int DashHitImmunityFrames = 12;
        /// <summary>命中附加的 GodSlayerInferno 时长（帧）</summary>
        private const int DashInfernoFrames = 300;
        /// <summary>龙颚粒子的初始缩放，每帧递减 0.04</summary>
        private const float DashJawsStartSize = 2.2f;
        // ── 实例字段 ──
        /// <summary>本帧请求冲刺：由 ProcessTriggers 置位，GodSlayerDashMovement 消费</summary>
        private bool godSlayerDashQueued;
        /// <summary>冲刺已进行的帧数（用于判定 25 帧时长）；0 表示当前没有在冲刺</summary>
        private int godSlayerDashElapsed;
        /// <summary>灾厄原文的 Time：驱动龙颚/尺寸/中段脉冲的相位，中段脉冲触发后会跳到 111 以只触发一次</summary>
        private int godSlayerDashTime;
        /// <summary>龙颚粒子的当前缩放，每帧递减 0.04</summary>
        private float godSlayerDashSize;
        /// <summary>本次冲刺是否已播放过命中音效（每次冲刺只播一次）</summary>
        private bool godSlayerDashHitSounded;
        /// <summary>起手音效的播放句柄，用于每帧把声源位置跟到玩家身上</summary>
        private SlotId godSlayerDashSoundSlot;
        // ── 公开方法 ──
        /// <summary>
        /// 冲刺请求入口，由 ProcessTriggers 在按下 GodslayerDashHotKey 时调用。
        /// 闸门照抄灾厄：需穿着弑神者套（godSlayer 标记）、正按住任一方向键、
        /// 非滑轮/钩爪/被舌卷/坐骑，原版 dashDelay 为 0，且不在本模组的冲刺冷却中。
        /// </summary>
        public void RequestGodSlayerDash()
        {
            if (!godSlayer || Player.whoAmI != Main.myPlayer)
                return;
            if (Player.HasBuff(ModContent.BuffType<GodSlayerDashCooldown>()))
                return;
            if (Player.pulley || Player.grappling[0] != -1 || Player.tongued || Player.mount.Active)
                return;
            if (Player.dashDelay != 0)
                return;
            if (!Player.controlUp && !Player.controlDown && !Player.controlLeft && !Player.controlRight)
                return;
            godSlayerDashQueued = true;
        }
        /// <summary>
        /// 冲刺状态机，由 PostUpdateRunSpeeds 每帧调用（灾厄同样把它的 ModDashMovement 挂在该钩子）。
        /// 流程：消费请求 → 起步 → 冲刺中（阻拦原版冲刺、命中判定、拖尾表现、速度衰减）→ 收尾。
        /// </summary>
        public void GodSlayerDashMovement()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (godSlayerDashQueued)
            {
                godSlayerDashQueued = false;
                StartGodSlayerDash();
            }
            if (godSlayerDashElapsed <= 0)
                return;
            BlockVanillaDash();
            GodSlayerDashHits();
            GodSlayerDashEffects();
            godSlayerDashElapsed++;
            if (godSlayerDashElapsed > DashDuration)
                EndGodSlayerDash();
        }
        // ── 私有工具 ──
        /// <summary>
        /// 冲刺起步：把方向键组合归一化成八方向单位向量作为朝向（与灾厄一致），
        /// 速度直接置为 DashStartSpeed，并把 Player.dashDelay 置 -1 表示"冲刺中"；
        /// 随后播放吞噬者死亡音效（句柄留用以便每帧跟随玩家）、喷 16 颗暗紫尘与 1 个紫色定向脉冲环，并挂上冷却 buff。
        /// 与灾厄一致：若正前方有实心块，水平速度减半，避免一头撞墙飞出。
        /// </summary>
        private void StartGodSlayerDash()
        {
            Vector2 direction = Vector2.Zero;
            if (Player.controlUp)
                direction.Y -= 1f;
            if (Player.controlDown)
                direction.Y += 1f;
            if (Player.controlLeft)
                direction.X -= 1f;
            if (Player.controlRight)
                direction.X += 1f;
            direction = direction.SafeNormalize(new Vector2(Player.direction, 0f));
            Player.velocity = direction * DashStartSpeed;
            Player.dashDelay = -1;
            godSlayerDashElapsed = 1;
            godSlayerDashTime = 0;
            godSlayerDashSize = DashJawsStartSize;
            godSlayerDashHitSounded = false;
            Player.AddBuff(ModContent.BuffType<GodSlayerDashCooldown>(), DashCooldownFrames);
            godSlayerDashSoundSlot = SoundEngine.PlaySound(CalamityDemutationSounds.DevourerDeath, Player.Center);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center, Vector2.Zero, Color.Orchid
                , new Vector2(2f, 2f), Main.rand.NextFloat(12f, 25f), 0.1f, 12f, 18));
            for (int i = 0; i <= 15; i++)
            {
                Dust dust = Dust.NewDustPerfect(Player.position, DustID.GiantCursedSkullBolt, -Player.velocity.RotatedByRandom(MathHelper.ToRadians(35f)) * Main.rand.NextFloat(0.3f, 0.9f), 0, default, Main.rand.NextFloat(3.1f, 3.9f));
                dust.noGravity = false;
            }
            // 正前方是实心块时把水平速度减半，防止贴墙冲刺时被弹飞
            Point ahead = (Player.Center + new Vector2(MathHelper.Clamp(direction.X, -1f, 1f) * Player.width / 2f + 2f, 0f)).ToTileCoordinates();
            if (WorldGen.SolidOrSlopedTile(ahead.X, ahead.Y))
                Player.velocity.X /= 2f;
        }
        /// <summary>
        /// 冲刺期间的每帧表现（顺序与灾厄 MidDashEffects 一致）：
        /// 音效跟随玩家 → Time 自增、龙颚尺寸每帧 -0.04、垂直下落放宽到 50 →
        /// 前 20 帧每帧一对龙颚（洋红在下、青色在上且略小）→ 环形尘环（第 2~2.5 帧淡入到满半径）+ 随体尘 →
        /// 每帧 2 颗火花（缩放随龙颚尺寸 ×1.3）→ 第 21 帧补两层定向脉冲环并把 Time 推到 111 使其只触发一次 →
        /// 按速度档做两级衰减。
        /// </summary>
        private void GodSlayerDashEffects()
        {
            if (SoundEngine.TryGetActiveSound(godSlayerDashSoundSlot, out var dashSound) && dashSound.IsPlaying)
                dashSound.Position = Player.Center;
            godSlayerDashTime++;
            godSlayerDashSize -= 0.04f;
            Player.maxFallSpeed = 50f;
            if (godSlayerDashTime < 20)
            {
                GeneralParticleHandler.SpawnParticle(new Jaws(Player.Center + Player.velocity * 0.5f, Player.velocity, Color.Fuchsia
                    , new Vector2(0.8f, 1f), Player.velocity.ToRotation() + MathHelper.PiOver2, godSlayerDashSize, godSlayerDashSize, 2));
                GeneralParticleHandler.SpawnParticle(new Jaws(Player.Center + Player.velocity * 0.45f, Player.velocity, Color.Aqua
                    , new Vector2(0.8f, 1f), Player.velocity.ToRotation() + MathHelper.PiOver2, godSlayerDashSize - 0.3f, godSlayerDashSize - 0.3f, 2));
            }
            float radiusFactor = MathHelper.Lerp(0f, 1f, Utils.GetLerpValue(2f, 2.5f, godSlayerDashTime, true));
            for (int i = 0; i < 9; i++)
            {
                float offsetRotationAngle = Player.velocity.ToRotation() + godSlayerDashTime / 20f;
                float radius = (30f + (float)Math.Cos(godSlayerDashTime / 3f) * 24f) * radiusFactor;
                Vector2 ringPosition = Player.Center + Player.velocity * 0.8f;
                ringPosition += offsetRotationAngle.ToRotationVector2().RotatedBy(i / 5f * MathHelper.TwoPi) * radius;
                Dust ring = Dust.NewDustPerfect(ringPosition, Main.rand.NextBool(5) ? DustID.GiantCursedSkullBolt : DustID.CorruptTorch);
                ring.noGravity = true;
                ring.velocity = Player.velocity * 0.5f;
                ring.scale = Main.rand.NextFloat(2.7f, 3f);
                Dust trail = Dust.NewDustPerfect(Player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-15f, 15f)) + Player.velocity * 0.5f
                    , Main.rand.NextBool(14) ? DustID.DungeonSpirit : DustID.CorruptTorch, -Player.velocity.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.1f, 0.8f), 0, default, Main.rand.NextFloat(2.7f, 3.9f));
                trail.noGravity = true;
            }
            float sparkScale = godSlayerDashSize * 1.3f;
            Vector2 sparkVelocity = Player.velocity.RotatedBy(Player.direction * -4) * 0.08f - Player.velocity / 2f;
            DRKLoader.AddParticle(new DRK_Spark(Player.Center + Player.velocity.RotatedBy(2f * Player.direction) * 1.2f, sparkVelocity, false, Main.rand.Next(11, 13), sparkScale, Main.rand.NextBool(3) ? Color.Aqua : Color.Fuchsia));
            Vector2 sparkVelocity2 = Player.velocity.RotatedBy(Player.direction * 4) * 0.08f - Player.velocity / 2f;
            DRKLoader.AddParticle(new DRK_Spark(Player.Center + Player.velocity.RotatedBy(-2f * Player.direction) * 1.2f, sparkVelocity2, false, Main.rand.Next(11, 13), sparkScale, Main.rand.NextBool(3) ? Color.Aqua : Color.Fuchsia));
            if (godSlayerDashTime > 20 && godSlayerDashTime < 100)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center - Player.velocity * 0.52f, Player.velocity / 1.5f, Color.Fuchsia
                    , new Vector2(1f, 2f), Player.velocity.ToRotation(), 0.82f, 0.32f, 60));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center - Player.velocity * 0.4f, Player.velocity / 1.5f * 0.9f, Color.Aqua
                    , new Vector2(0.8f, 1.5f), Player.velocity.ToRotation(), 0.58f, 0.28f, 50));
                godSlayerDashTime = 111;
            }
            float runSpeed = Math.Max(Player.accRunSpeed, Player.maxRunSpeed);
            if (Player.velocity.Length() > DashMidSpeed)
                Player.velocity *= DashSpeedDeceleration;
            else if (Player.velocity.Length() > runSpeed)
                Player.velocity *= RunSpeedDeceleration;
        }
        /// <summary>
        /// 冲刺命中判定：以「玩家位置 + 速度的一半」为中心构造略大于玩家的判定框，
        /// 对每个可攻击且不在冲刺免疫中的敌人结算一次伤害——
        /// 伤害与击退按通用职业加成换算、暴击按通用暴击率掷骰，命中后敌人与玩家各获得 12 帧冲刺免疫，
        /// 并给敌人挂上 300 帧 GodSlayerInferno（按灾厄双版本分别查找）。
        /// 每次命中都喷 26 颗电光/凋灵尘；本次冲刺的首次命中还会播放吞噬者死亡冲击音效。
        /// </summary>
        private void GodSlayerDashHits()
        {
            Rectangle hitArea = new Rectangle((int)(Player.position.X + Player.velocity.X * 0.5f - 4f), (int)(Player.position.Y + Player.velocity.Y * 0.5f - 4f), Player.width + 8, Player.height + 8);
            int hitDirection = Player.direction;
            if (Player.velocity.X != 0f)
                hitDirection = Math.Sign(Player.velocity.X);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (Player.dontHurtCritters && NPCID.Sets.CountsAsCritter[npc.type])
                    continue;
                if (npc.dontTakeDamage || npc.friendly || npc.immune[Player.whoAmI] > 0)
                    continue;
                if (!hitArea.Intersects(npc.getRect()) || !(npc.noTileCollide || Player.CanHit(npc)))
                    continue;
                int dashDamage = (int)Player.GetTotalDamage<GenericDamageClass>().ApplyTo(DashHitDamage);
                float dashKnockback = Player.GetTotalKnockback<GenericDamageClass>().ApplyTo(DashHitKnockback);
                bool critical = Main.rand.Next(100) < Player.GetTotalCritChance<GenericDamageClass>();
                Player.ApplyDamageToNPC(npc, dashDamage, dashKnockback, hitDirection, critical, DamageClass.Generic);
                if (npc.immune[Player.whoAmI] < DashHitImmunityFrames)
                    npc.immune[Player.whoAmI] = DashHitImmunityFrames;
                Player.GiveImmuneTimeForCollisionAttack(DashHitImmunityFrames);
                ApplyCalamityBuff(npc, "CalamityMod", "GodSlayerInferno", DashInfernoFrames);
                ApplyCalamityBuff(npc, "CalamityModClassicPreTrailer", "GodSlayerInferno", DashInfernoFrames);
                if (!godSlayerDashHitSounded)
                {
                    godSlayerDashHitSounded = true;
                    SoundEngine.PlaySound(CalamityDemutationSounds.DevourerDeathImpact, Player.Center);
                }
                for (int i = 0; i <= 25; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Player.position, Main.rand.NextBool(3) ? DustID.Electric : DustID.WitherLightning, Player.velocity.RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.1f, 0.5f), 0, default, Main.rand.NextFloat(2.1f, 2.9f));
                    dust.noGravity = false;
                }
            }
        }
        /// <summary>
        /// 冲刺期间阻拦原版冲刺（照抄灾厄 ForceVariousEffects 的做法）：
        /// 把 dashType 清 0，并复位克苏鲁之盾的 eocHit/eocDash，防止借冲刺无敌帧刷 Shield of Cthulhu。
        /// </summary>
        private void BlockVanillaDash()
        {
            Player.dashType = 0;
            Player.eocHit = -1;
            if (Player.eocDash != 0)
                Player.eocDash = 0;
        }
        /// <summary>
        /// 冲刺收尾：写回 30 帧的原版 dashDelay 并把速度乘 0.2 快速刹停，随后清空冲刺状态。
        /// 真正的 45 秒冷却由 GodSlayerDashCooldown buff 承担，这里只是原版状态复位。
        /// </summary>
        private void EndGodSlayerDash()
        {
            Player.dashDelay = DashEndDelay;
            Player.velocity *= 0.2f;
            godSlayerDashElapsed = 0;
            godSlayerDashTime = 0;
            godSlayerDashHitSounded = false;
        }
    }
}
