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
    /// 银河弹 - 四季银河 / 宇宙方舟发射的追踪弹幕
    /// 携带旋转彩虹残影，命中敌人时依据环境触发对应 buff 与二次弹幕
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
        /// AI：周期性播放闪烁音效；透明度逐渐降为 0~150（变实，阈值取决于 ai[1] 代表的 y 坐标档位）；
        /// 随速度旋转；概率生成彩虹拖尾粉尘与星形残片；ai[1]=1（宇宙方舟群星）时额外发光、补喷粉尘/残片；
        /// 持续朝 1600 像素内最近敌人追踪
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
            // 透明度逐渐降低（变实），ai[1] 兼作颜色档位与透明度阈值
            Projectile.alpha -= 15;
            int num58 = 150;
            if (Projectile.Center.Y >= Projectile.ai[1])
            {
                num58 = 0;
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
            // ai[1] == 1 时（宇宙方舟发射的群星）附带发光与额外粉尘/残片特效
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
            // 向 1600 像素内的敌人追踪
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
            // 血月：给予"战斗"buff
            if (bloodMoon)
            {
                player.AddBuff(BuffID.Battle, 600);
            }
            if (snowMoon)
            {
                player.AddBuff(BuffID.RapidHealing, 600);
            }
            if (pumpkinMoon)
            {
                player.AddBuff(BuffID.WellFed, 600);
            }
            else if (jungle)
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
            else if (snow)
            {
                player.AddBuff(BuffID.Warmth, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.IceBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            else if (beach)
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
            else if (corrupt)
            {
                player.AddBuff(BuffID.Wrath, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.CursedFlameFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (crimson)
            {
                player.AddBuff(BuffID.Rage, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.GoldenShowerFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (dungeon)
            {
                target.AddBuff(BuffID.Frostburn, 1200);
                player.AddBuff(BuffID.Dangersense, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.WaterBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (desert)
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
            else if (glow)
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
            else if (hell)
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
            else if (holy)
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
            else if (nebula)
            {
                player.AddBuff(BuffID.MagicPower, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.NebulaBlaze1, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (stardust)
            {
                player.AddBuff(BuffID.Summoning, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.StardustCellMinionShot, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (solar)
            {
                player.AddBuff(BuffID.Titan, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.SolarWhipSwordExplosion, Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
            }
            else if (vortex)
            {
                player.AddBuff(BuffID.AmmoReservation, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.VortexBeaterRocket, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].localNPCHitCooldown = -1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else
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
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                var calamityPlayerType = calamity.Code.GetTypes()
                   .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                if (calamityPlayerType != null)
                {
                    var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", [])
                        ?.MakeGenericMethod(calamityPlayerType);
                    if (getModPlayerMethod != null)
                    {
                        if (getModPlayerMethod.Invoke(player, null) is ModPlayer calPlayer)
                        {
                            var prop = calamityPlayerType.GetProperty("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null)
                            {
                                bool ZoneAstral = (bool)prop.GetValue(calPlayer);
                                if (ZoneAstral)
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
            // 血月：给予"战斗"buff
            if (bloodMoon)
            {
                player.AddBuff(BuffID.Battle, 600);
            }
            if (snowMoon)
            {
                player.AddBuff(BuffID.RapidHealing, 600);
            }
            if (pumpkinMoon)
            {
                player.AddBuff(BuffID.WellFed, 600);
            }
            else if (jungle)
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
            else if (snow)
            {
                player.AddBuff(BuffID.Warmth, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.IceBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            else if (beach)
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
            else if (corrupt)
            {
                player.AddBuff(BuffID.Wrath, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.CursedFlameFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (crimson)
            {
                player.AddBuff(BuffID.Rage, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.GoldenShowerFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (dungeon)
            {
                target.AddBuff(BuffID.Frostburn, 1200);
                player.AddBuff(BuffID.Dangersense, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.WaterBolt, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (desert)
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
            else if (glow)
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
            else if (hell)
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
            else if (holy)
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
            else if (nebula)
            {
                player.AddBuff(BuffID.MagicPower, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.NebulaBlaze1, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else if (stardust)
            {
                player.AddBuff(BuffID.Summoning, 600);
                int ball = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.StardustCellMinionShot, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[ball].penetrate = 1;
                Main.projectile[ball].DamageType = DamageClass.Melee;
            }
            else if (solar)
            {
                player.AddBuff(BuffID.Titan, 600);
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.SolarWhipSwordExplosion, Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0.85f + Main.rand.NextFloat() * 1.15f);
            }
            else if (vortex)
            {
                player.AddBuff(BuffID.AmmoReservation, 600);
                int proj = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Projectile.velocity, ProjectileID.VortexBeaterRocket, Projectile.damage, Projectile.knockBack, Projectile.owner);
                Main.projectile[proj].usesLocalNPCImmunity = true;
                Main.projectile[proj].localNPCHitCooldown = -1;
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            else
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
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                var calamityPlayerType = calamity.Code.GetTypes()
                   .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                if (calamityPlayerType != null)
                {
                    var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", [])
                        ?.MakeGenericMethod(calamityPlayerType);
                    if (getModPlayerMethod != null)
                    {
                        if (getModPlayerMethod.Invoke(player, null) is ModPlayer calPlayer)
                        {
                            var prop = calamityPlayerType.GetProperty("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null)
                            {
                                bool ZoneAstral = (bool)prop.GetValue(calPlayer);
                                if (ZoneAstral)
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
