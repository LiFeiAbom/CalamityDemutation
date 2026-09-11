using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// Ω生物群系之球 - Ω生物群系之剑发射的追踪弹幕
    /// 颜色与命中 debuff 依据生物群系及月相变化（含四柱环境）
    /// </summary>
    internal class OmegaBiomeOrb : ModProjectile
    {
        Color color = default;      // 当前渲染颜色
        private int dustType = 3;   // 当前使用的粉尘类型
        /// <summary>
        /// 静态属性：预留 6 格残影缓存（配合 PreDraw 的残影绘制）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6; // 残影数量
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>
        /// 基础属性：20x20 碰撞箱；借用光束 AI 模板、友方、单次穿透、存活 120 帧、近战伤害
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = ProjAIStyleID.Beam;
            AIType = ProjectileID.LightBeam;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Melee;
        }
        /// <summary>
        /// AI：每帧读取主人所在生物群系与四柱环境，刷新粉尘类型与渲染颜色并生成粉尘；
        /// 支持现代版灾厄"星陨之地"配色；最后朝 200 像素内最近敌人短距离追踪
        /// </summary>
        public override void AI()
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
            bool sky = player.ZoneSkyHeight;
            bool holy = player.ZoneHallow;
            bool nebula = player.ZoneTowerNebula;
            bool stardust = player.ZoneTowerStardust;
            bool solar = player.ZoneTowerSolar;
            bool vortex = player.ZoneTowerVortex;
            // 依据生物群系/四柱环境选择粉尘类型与弹幕颜色
            if (jungle)
            {
                dustType = 39;
                color = new Color(128, 255, 128, Projectile.alpha);
            }
            else if (snow)
            {
                dustType = 51;
                color = new Color(128, 255, 255, Projectile.alpha);
            }
            else if (beach)
            {
                dustType = 33;
                color = new Color(0, 0, 128, Projectile.alpha);
            }
            else if (corrupt)
            {
                dustType = 14;
                color = new Color(128, 64, 255, Projectile.alpha);
            }
            else if (crimson)
            {
                dustType = 5;
                color = new Color(128, 0, 0, Projectile.alpha);
            }
            else if (dungeon)
            {
                dustType = 29;
                color = new Color(64, 0, 128, Projectile.alpha);
            }
            else if (desert)
            {
                dustType = 32;
                color = new Color(255, 255, 128, Projectile.alpha);
            }
            else if (glow)
            {
                dustType = 56;
                color = new Color(0, 255, 255, Projectile.alpha);
            }
            else if (hell)
            {
                dustType = 6;
                color = new Color(255, 128, 0, Projectile.alpha);
            }
            else if (sky)
            {
                dustType = 213;
                color = new Color(255, 255, 255, Projectile.alpha);
            }
            else if (holy)
            {
                dustType = 57;
                color = new Color(255, 255, 0, Projectile.alpha);
            }
            else if (nebula)
            {
                dustType = 242;
                color = new Color(255, 0, 255, Projectile.alpha);
            }
            else if (stardust)
            {
                dustType = 206;
                color = new Color(0, 255, 255, Projectile.alpha);
            }
            else if (solar)
            {
                dustType = 244;
                color = new Color(255, 128, 0, Projectile.alpha);
            }
            else if (vortex)
            {
                dustType = 107;
                color = new Color(0, 255, 0, Projectile.alpha);
            }
            else
            {
                dustType = 3;                                    // 森林等：恢复默认粉尘类型，避免残留上一群系的粉尘
                color = new Color(0, 128, 0, Projectile.alpha);
            }
            // 现代版灾厄专用：反射读取 CalamityPlayer.ZoneAstral，处于星陨之地时改用橙色星尘配色
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
                            var field = calamityPlayerType.GetField("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                bool ZoneAstral = (bool)field.GetValue(calPlayer);
                                if (ZoneAstral)
                                {
                                    if (calamity.TryFind<ModDust>("AstralOrange", out ModDust astralOrange)) { dustType = astralOrange.Type; }
                                    color = new Color(255, 127, 80, Projectile.alpha);
                                }
                            }
                        }
                    }
                }
            }
            // 按当前群系粉尘类型生成一粒受弹体速度影响的拖尾粉尘
            int num458 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 1.2f);
            Main.dust[num458].noGravity = true;
            Main.dust[num458].velocity *= 0.5f;
            Main.dust[num458].velocity += Projectile.velocity * 0.1f;
            Projectile.HomeInNPC(200f, 24f, 20f);   // 短距离追踪（200 像素内）
        }
        /// <summary>
        /// 命中敌人：依据生物群系施加 debuff，并根据血月/霜月/南瓜月给予玩家 buff
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            bool jungle = player.ZoneJungle;
            bool snow = player.ZoneSnow;
            bool beach = player.ZoneBeach;
            bool dungeon = player.ZoneDungeon;
            bool desert = player.ZoneDesert;
            bool glow = player.ZoneGlowshroom;
            bool hell = player.ZoneUnderworldHeight;
            bool holy = player.ZoneHallow;
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
                target.AddBuff(BuffID.Venom, 600);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 600); }
                }
            }
            else if (snow)
            {
                // 修正：霜冻是负面 debuff，应施加给目标而非玩家自己
                target.AddBuff(BuffID.Frostburn, 600);
            }
            else if (beach)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 600); }
                }
            }
            else if (dungeon)
            {
                target.AddBuff(BuffID.Frostburn, 600);
            }
            else if (desert || holy)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 600); }
                }
            }
            else if (glow)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 600); }
                }
            }
            else if (hell)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 600); }
                }
            }
            else
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 600); }
                }
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
                            var field = calamityPlayerType.GetField("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                bool ZoneAstral = (bool)field.GetValue(calPlayer);
                                if (ZoneAstral)
                                {
                                    if (calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection)) { target.AddBuff(astralInfection.Type, 600); }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：效果与命中 NPC 相同
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Player player = Main.player[Projectile.owner];
            bool jungle = player.ZoneJungle;
            bool snow = player.ZoneSnow;
            bool beach = player.ZoneBeach;
            bool dungeon = player.ZoneDungeon;
            bool desert = player.ZoneDesert;
            bool glow = player.ZoneGlowshroom;
            bool hell = player.ZoneUnderworldHeight;
            bool holy = player.ZoneHallow;
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
                target.AddBuff(BuffID.Venom, 600);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 600); }
                }
            }
            else if (snow)
            {
                // 修正：霜冻是负面 debuff，应施加给目标而非玩家自己
                target.AddBuff(BuffID.Frostburn, 600);
            }
            else if (beach)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 600); }
                }
            }
            else if (dungeon)
            {
                target.AddBuff(BuffID.Frostburn, 600);
            }
            else if (desert || holy)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 600); }
                }
            }
            else if (glow)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 600); }
                }
            }
            else if (hell)
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 600); }
                }
            }
            else
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 600); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 600); }
                }
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
                            var field = calamityPlayerType.GetField("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                bool ZoneAstral = (bool)field.GetValue(calPlayer);
                                if (ZoneAstral)
                                {
                                    if (calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection)) { target.AddBuff(astralInfection.Type, 600); }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 消亡：沿旧位置与旧速度方向散布两档（1.8 / 1.4）当前群系颜色的粉尘，模拟碎裂消散
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            int num3;
            for (int num795 = 4; num795 < 31; num795 = num3 + 1)
            {
                float num796 = Projectile.oldVelocity.X * (30f / num795);
                float num797 = Projectile.oldVelocity.Y * (30f / num795);
                int num798 = Dust.NewDust(new Vector2(Projectile.oldPosition.X - num796, Projectile.oldPosition.Y - num797), 8, 8, dustType, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, default, 1.8f);
                Main.dust[num798].noGravity = true;
                Dust dust = Main.dust[num798];
                dust.velocity *= 0.5f;
                num798 = Dust.NewDust(new Vector2(Projectile.oldPosition.X - num796, Projectile.oldPosition.Y - num797), 8, 8, dustType, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, default, 1.4f);
                dust = Main.dust[num798];
                dust.velocity *= 0.05f;
                num3 = num795;
            }
        }
        /// <summary>
        /// 自定义绘制：生成后前 15 帧不绘制避免跳变，随后绘制当前群系颜色的残影拖尾
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // 生成后前 15 帧不绘制
            if (Projectile.timeLeft > 105)
                return false;
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>
        /// 使用按生物群系/四柱环境刷新的 color 字段着色（含 alpha）
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return color;
        }
    }
}
