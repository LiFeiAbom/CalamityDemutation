using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 银河弹（Galaxia） - 银河 / 鸿蒙方舟发射的追踪弹幕，移植自灾厄经典版（CalamityModClassicPreTrailer 1.4.2.101）的同名弹幕。
    /// <para>
    /// 携带旋转彩虹残影，命中敌人/玩家时依据主人所处的生物群系、月相事件（血月 / 霜月 / 南瓜月）、
    /// 四柱环境触发对应 buff 与二次弹幕；另用反射读取现代版灾厄的 <c>CalamityPlayer.ZoneAstral</c>，补上"星陨之地"一套效果。
    /// </para>
    /// <para>
    /// 两处调用方给的 ai[1] 不同：银河不传（=0），鸿蒙方舟传 <c>Main.rand.Next(3)</c>（0~2），
    /// 后者正是源码用来开启"群星"额外发光/粉尘的档位（见 <see cref="AI"/>）。
    /// </para>
    /// </summary>
    internal class Galaxia:ModProjectile
    {
        /// <summary>
        /// 静态属性：预留 6 格残影缓存（配合 PreDraw 的残影绘制）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6; // 残影数量
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>
        /// 基础属性：24x24 碰撞箱；友方、无视地形与入水减速、初始半透明（alpha 50）、近战伤害、
        /// 可穿透 3 个敌人、存活 280 帧；每个敌人独立 10 帧命中冷却
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.tileCollide = false; // 无视地形
            Projectile.ignoreWater = true;
            Projectile.alpha = 50;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 280;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        /// <summary>
        /// AI：周期性播放闪烁音效；透明度逐帧降 15（变实），但被 num58 这个下限夹住——下限由 ai[1] 决定
        /// （源码把 ai[1] 兼作"亮度档位"与 Y 阈值两用，实际只传 0~2，恒小于弹幕 Y，故通常压到 0，即全实）；
        /// 随速度旋转；概率生成彩虹拖尾粉尘与星形残片；ai[1]=1 时额外发光并补喷粉尘/残片
        /// （鸿蒙方舟的群星只有 ai[1]==1 的那 1/3 命中此档）；最后朝 1600 像素内最近敌人追踪
        /// </summary>
        public override void AI()
        {
            // 周期性播放闪烁音效（20~60 帧随机间隔，1/5 概率）
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 20 + Main.rand.Next(40);
                if (Main.rand.NextBool(5))
                {
                    SoundEngine.PlaySound(SoundID.Item9, Projectile.position);
                }
            }
            // 透明度逐渐降低（变实），但夹在一个下限 num58 上
            Projectile.alpha -= 15;
            int num58 = 150;   // 默认下限 150：在 ai[1] 之上只做到半透明
            if (Projectile.Center.Y >= Projectile.ai[1])
            {
                num58 = 0;   // 飞到 ai[1] 这条线之下（Y 更大）才允许全实
            }
            if (Projectile.alpha < num58)
            {
                Projectile.alpha = num58;
            }
            // 根据飞行速度让弹幕自身旋转
            Projectile.localAI[0] += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * Projectile.direction;
            Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * Projectile.direction;
            // 1/8 概率生成彩虹拖尾粉尘
            if (Main.rand.NextBool(8))
            {
                Vector2 value3 = Vector2.UnitX.RotatedByRandom(1.5707963705062866).RotatedBy((double)Projectile.velocity.ToRotation(), default);
                int num59 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.2f);
                Main.dust[num59].noGravity = true;
                Main.dust[num59].velocity = value3 * 0.66f;
                Main.dust[num59].position = Projectile.Center + value3 * 12f;
            }
            // 1/24 概率生成星形残片
            if (Main.rand.NextBool(24))
            {
                int goreIndex = Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * 0.2f, 16, 1f);
                Main.gore[goreIndex].velocity *= 0.66f;
                Main.gore[goreIndex].velocity += Projectile.velocity * 0.3f;
            }
            // ai[1] == 1 时（鸿蒙方舟发射的群星）附带发光与额外粉尘/残片特效
            if (Projectile.ai[1] == 1f)
            {
                Projectile.light = 0.9f;
                if (Main.rand.NextBool(5))
                {
                    int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.2f);
                    Main.dust[dustIndex].noGravity = true;
                }
                if (Main.rand.NextBool(10))
                {
                    Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position, Projectile.velocity * 0.2f, Main.rand.Next(16, 18), 1f);
                }
            }
            // 追踪 1600 像素内最近的敌人：追踪速度 30、惯性 20
            Projectile.HomeInNPC(1600f, 30f, 20f);
        }
        /// <summary>
        /// 命中敌人：依据玩家所在环境/月相事件施加 debuff、给予玩家 buff，
        /// 并生成对应的二次弹幕；陨星环境下还会追加陨星感染 debuff 与星弹。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            // 记录当前所在环境与事件状态
            bool jungle = player.ZoneJungle;
            bool snow = player.ZoneSnow;
            bool beach = player.ZoneBeach;
            bool corrupt = player.ZoneCorrupt;
            bool crimson = player.ZoneCrimson;
            bool dungeon = player.ZoneDungeon;
            bool desert = player.ZoneDesert;
            bool glow = player.ZoneGlowshroom;
            bool hell = player.ZoneUnderworldHeight;
            bool holy = player.ZoneHallow;
            bool nebula = player.ZoneTowerNebula;
            bool stardust = player.ZoneTowerStardust;
            bool solar = player.ZoneTowerSolar;
            bool vortex = player.ZoneTowerVortex;
            bool bloodMoon = Main.bloodMoon;
            bool snowMoon = Main.snowMoon;
            bool pumpkinMoon = Main.pumpkinMoon;
            // 三个事件是三个并列 if（不是 else-if）：血月/霜月可与下面的群系分支同时生效；
            // 但南瓜月接在群系链首的 if 上，一旦南瓜月成立，其后的群系分支（else if …）就整条跳过
            // 血月：给予"战斗"buff（600 帧 = 10 秒）
            if (bloodMoon)
            {
                player.AddBuff(BuffID.Battle, 600);
            }
            if (snowMoon)
            {
                // 霜月：给予快速治疗 buff
                player.AddBuff(BuffID.RapidHealing, 600);
            }
            if (pumpkinMoon)
            {
                // 南瓜月：给予吃饱 buff
                player.AddBuff(BuffID.WellFed, 600);
            }
            else if (jungle)   // 丛林：给敌人剧毒 + 瘟疫；给玩家荆棘，并额外射出叶刃
            {
                target.AddBuff(BuffID.Venom, 1200);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 1200); }
                }
                player.AddBuff(BuffID.Thorns, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.Leaf, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (snow)   // 雪原：给玩家温暖，并额外射出冰锥
            {
                player.AddBuff(BuffID.Warmth, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.IceBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            else if (beach)   // 海滩：给敌人沉海（CrushDepth）；给玩家潮湿，并射出减速到 25% 的海泡
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 1200); }
                }
                player.AddBuff(BuffID.Wet, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity * 0.25f, ProjectileID.FlaironBubble, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (corrupt)   // 腐化：给玩家怒气，并射出穿透 1 的咒火
            {
                player.AddBuff(BuffID.Wrath, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.CursedFlameFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (crimson)   // 血腥：给玩家暴怒，并射出穿透 1 的黄金雨
            {
                player.AddBuff(BuffID.Rage, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.GoldenShowerFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (dungeon)   // 地牢：给敌人霜冻；给玩家危险感，并射出穿透 1 的水球
            {
                target.AddBuff(BuffID.Frostburn, 1200);
                player.AddBuff(BuffID.Dangersense, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.WaterBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (desert)   // 沙漠：给敌人圣火；给玩家耐力，并射出黑矢
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 1200); }
                }
                player.AddBuff(BuffID.Endurance, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.BlackBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (glow)   // 发光蘑菇地：给敌人时之悲；给玩家洞穴探险，并射出蘑菇
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 1200); }
                }
                player.AddBuff(BuffID.Spelunker, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.Mushroom, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (hell)   // 地狱：给敌人硫磺火；给玩家狱炎，并射出火球
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1200); }
                }
                player.AddBuff(BuffID.Inferno, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.BallofFire, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (holy)   // 神圣：给敌人圣火；给玩家心之距离，并射出彩虹水晶爆（每敌只命中一次）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 1200); }
                }
                player.AddBuff(BuffID.Heartreach, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.RainbowCrystalExplosion, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].localNPCHitCooldown = -1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (nebula)   // 星云柱：给玩家魔能，并射出星云烈焰
            {
                player.AddBuff(BuffID.MagicPower, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.NebulaBlaze1, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (stardust)   // 星尘柱：给玩家召唤，并射出穿透 1 的星尘细胞弹
            {
                player.AddBuff(BuffID.Summoning, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.StardustCellMinionShot, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (solar)   // 日耀柱：给玩家泰坦，并射出日耀鞭剑爆（末位参数 0.85~2.0 是随机伤害缩放）
            {
                player.AddBuff(BuffID.Titan, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.SolarWhipSwordExplosion, Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
            }
            else if (vortex)   // 涡流柱：给玩家弹药储备，并射出涡流火箭（每敌只命中一次）
            {
                player.AddBuff(BuffID.AmmoReservation, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.VortexBeaterRocket, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].localNPCHitCooldown = -1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else   // 其它（森林等）：给敌人甲壳破碎；给玩家树妖祝福，并射出穿透 1 的泰拉刃光束
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 1200); }
                }
                player.AddBuff(BuffID.DryadsWard, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.TerrarianBeam, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].penetrate = 1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            // 现代版灾厄的"星陨之地"判定：本工程对 CalamityPlayer 无编译期引用，故全程走反射
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                var calamityPlayerType = calamity.Code.GetTypes()
                   .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                if (calamityPlayerType != null)
                {
                    var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", [])
                        ?.MakeGenericMethod(calamityPlayerType);   // 取 Player.GetModPlayer<CalamityPlayer>()
                    if (getModPlayerMethod != null)
                    {
                        if (getModPlayerMethod.Invoke(player, null) is ModPlayer calPlayer)
                        {
                            // ZoneAstral 在灾厄里是只读属性（不是字段），故用 GetProperty
                            var prop = calamityPlayerType.GetProperty("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null)
                            {
                                bool ZoneAstral = (bool)prop.GetValue(calPlayer);
                                if (ZoneAstral)   // 星陨之地：额外给敌人星陨感染、给玩家重力正常化，并射出一枚灾厄的星陨之星
                                {
                                    if (calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection)) { target.AddBuff(astralInfection.Type, 1200); }
                                    if (calamity.TryFind<ModBuff>("GravityNormalizerBuff", out ModBuff gravityNormalizer)) { player.AddBuff(gravityNormalizer.Type, 600); }
                                    if (calamity.TryFind<ModProjectile>("AstralStar", out ModProjectile astralStar))
                                    {
                                        int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, astralStar.Type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                                        Main.projectile[proj].DamageType = DamageClass.Melee;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：效果与命中 NPC 相同（依据环境/事件触发 buff 与二次弹幕）
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Player player = Main.player[Projectile.owner];
            // 记录当前所在环境与事件状态
            bool jungle = player.ZoneJungle;
            bool snow = player.ZoneSnow;
            bool beach = player.ZoneBeach;
            bool corrupt = player.ZoneCorrupt;
            bool crimson = player.ZoneCrimson;
            bool dungeon = player.ZoneDungeon;
            bool desert = player.ZoneDesert;
            bool glow = player.ZoneGlowshroom;
            bool hell = player.ZoneUnderworldHeight;
            bool holy = player.ZoneHallow;
            bool nebula = player.ZoneTowerNebula;
            bool stardust = player.ZoneTowerStardust;
            bool solar = player.ZoneTowerSolar;
            bool vortex = player.ZoneTowerVortex;
            bool bloodMoon = Main.bloodMoon;
            bool snowMoon = Main.snowMoon;
            bool pumpkinMoon = Main.pumpkinMoon;
            // 三个事件是三个并列 if（不是 else-if）：血月/霜月可与下面的群系分支同时生效；
            // 但南瓜月接在群系链首的 if 上，一旦南瓜月成立，其后的群系分支（else if …）就整条跳过
            // 血月：给予"战斗"buff（600 帧 = 10 秒）
            if (bloodMoon)
            {
                player.AddBuff(BuffID.Battle, 600);
            }
            if (snowMoon)
            {
                // 霜月：给予快速治疗 buff
                player.AddBuff(BuffID.RapidHealing, 600);
            }
            if (pumpkinMoon)
            {
                // 南瓜月：给予吃饱 buff
                player.AddBuff(BuffID.WellFed, 600);
            }
            else if (jungle)   // 丛林：给敌人剧毒 + 瘟疫；给玩家荆棘，并额外射出叶刃
            {
                target.AddBuff(BuffID.Venom, 1200);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 1200); }
                }
                player.AddBuff(BuffID.Thorns, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.Leaf, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (snow)   // 雪原：给玩家温暖，并额外射出冰锥
            {
                player.AddBuff(BuffID.Warmth, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.IceBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            else if (beach)   // 海滩：给敌人沉海（CrushDepth）；给玩家潮湿，并射出减速到 25% 的海泡
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 1200); }
                }
                player.AddBuff(BuffID.Wet, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity * 0.25f, ProjectileID.FlaironBubble, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (corrupt)   // 腐化：给玩家怒气，并射出穿透 1 的咒火
            {
                player.AddBuff(BuffID.Wrath, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.CursedFlameFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (crimson)   // 血腥：给玩家暴怒，并射出穿透 1 的黄金雨
            {
                player.AddBuff(BuffID.Rage, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.GoldenShowerFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (dungeon)   // 地牢：给敌人霜冻；给玩家危险感，并射出穿透 1 的水球
            {
                target.AddBuff(BuffID.Frostburn, 1200);
                player.AddBuff(BuffID.Dangersense, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.WaterBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (desert)   // 沙漠：给敌人圣火；给玩家耐力，并射出黑矢
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 1200); }
                }
                player.AddBuff(BuffID.Endurance, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.BlackBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (glow)   // 发光蘑菇地：给敌人时之悲；给玩家洞穴探险，并射出蘑菇
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 1200); }
                }
                player.AddBuff(BuffID.Spelunker, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.Mushroom, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (hell)   // 地狱：给敌人硫磺火；给玩家狱炎，并射出火球
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 1200); }
                }
                player.AddBuff(BuffID.Inferno, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.BallofFire, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (holy)   // 神圣：给敌人圣火；给玩家心之距离，并射出彩虹水晶爆（每敌只命中一次）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 1200); }
                }
                player.AddBuff(BuffID.Heartreach, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.RainbowCrystalExplosion, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].localNPCHitCooldown = -1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (nebula)   // 星云柱：给玩家魔能，并射出星云烈焰
            {
                player.AddBuff(BuffID.MagicPower, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.NebulaBlaze1, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (stardust)   // 星尘柱：给玩家召唤，并射出穿透 1 的星尘细胞弹
            {
                player.AddBuff(BuffID.Summoning, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.StardustCellMinionShot, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (solar)   // 日耀柱：给玩家泰坦，并射出日耀鞭剑爆（末位参数 0.85~2.0 是随机伤害缩放）
            {
                player.AddBuff(BuffID.Titan, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.SolarWhipSwordExplosion, Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
            }
            else if (vortex)   // 涡流柱：给玩家弹药储备，并射出涡流火箭（每敌只命中一次）
            {
                player.AddBuff(BuffID.AmmoReservation, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.VortexBeaterRocket, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].localNPCHitCooldown = -1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else   // 其它（森林等）：给敌人甲壳破碎；给玩家树妖祝福，并射出穿透 1 的泰拉刃光束
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 1200); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 1200); }
                }
                player.AddBuff(BuffID.DryadsWard, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.TerrarianBeam, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].penetrate = 1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            // 现代版灾厄的"星陨之地"判定：本工程对 CalamityPlayer 无编译期引用，故全程走反射
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                var calamityPlayerType = calamity.Code.GetTypes()
                   .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                if (calamityPlayerType != null)
                {
                    var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", [])
                        ?.MakeGenericMethod(calamityPlayerType);   // 取 Player.GetModPlayer<CalamityPlayer>()
                    if (getModPlayerMethod != null)
                    {
                        if (getModPlayerMethod.Invoke(player, null) is ModPlayer calPlayer)
                        {
                            // ZoneAstral 在灾厄里是只读属性（不是字段），故用 GetProperty
                            var prop = calamityPlayerType.GetProperty("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null)
                            {
                                bool ZoneAstral = (bool)prop.GetValue(calPlayer);
                                if (ZoneAstral)   // 星陨之地：额外给敌人星陨感染、给玩家重力正常化，并射出一枚灾厄的星陨之星
                                {
                                    if (calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection)) { target.AddBuff(astralInfection.Type, 1200); }
                                    if (calamity.TryFind<ModBuff>("GravityNormalizerBuff", out ModBuff gravityNormalizer)) { player.AddBuff(gravityNormalizer.Type, 600); }
                                    if (calamity.TryFind<ModProjectile>("AstralStar", out ModProjectile astralStar))
                                    {
                                        int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, astralStar.Type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                                        Main.projectile[proj].DamageType = DamageClass.Melee;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 自定义绘制：绘制当前派对色（彩虹迪斯科色）的残影拖尾
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>
        /// 使用派对色（Main.Disco 三通道）着色，透明度跟随 alpha
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB, Projectile.alpha);
        }
    }
}
