using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 真·环境能量（TrueBiomeOrb） - 真·环境之刃发射的自动追踪弹幕，
    /// 移植自灾厄经典版（CalamityModClassicPreTrailer）同名弹幕并按本工程口径重制。
    /// <para>
    /// 颜色与命中 debuff 依据玩家所在生物群系动态变化，命中 debuff 一律 360 帧（6 秒，比环境能量的 90 帧更长）；
    /// 相对经典版的重制点：借用原版光束 AI 模板、把粉尘类型与颜色提成字段、补上 4 格残影绘制与 400 像素内的短程追踪，
    /// 另外兼容现代版灾厄的"星陨之地"配色。
    /// </para>
    /// </summary>
    internal class TrueBiomeOrb : ModProjectile
    {
        /// <summary>当前渲染颜色：每帧按主人所在生物群系刷新，<see cref="GetAlpha"/> 直接返回它</summary>
        Color color = default;
        /// <summary>当前使用的粉尘类型：每帧按主人所在生物群系刷新（默认 3 = 森林，防止残留上一群系的粉尘）</summary>
        private int dustType = 3;
        /// <summary>
        /// 静态属性：预留 4 格残影缓存（配合 PreDraw 的残影绘制）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4; // 残影数量
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>
        /// 基础属性：20x20 碰撞箱；借用光束 AI 模板、友方、近战伤害、单次穿透、存活 300 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = ProjAIStyleID.Beam;
            AIType = ProjectileID.LightBeam;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Melee;
        }
        /// <summary>
        /// AI：每帧读取主人所在生物群系，刷新粉尘类型与渲染颜色并生成粉尘；
        /// 支持现代版灾厄"星陨之地"配色；最后朝 400 像素内最近敌人追踪
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            // 记录当前所在环境
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
            // 依据玩家所在生物群系选择粉尘类型与弹幕颜色
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
            else
            {
                dustType = 3;                                    // 森林等：恢复默认粉尘类型，避免残留上一群系的粉尘
                color = new Color(0, 128, 0, Projectile.alpha);
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
                            // ZoneAstral 在灾厄里是只读属性（不是字段），必须用 GetProperty 才读得到
                            var prop = calamityPlayerType.GetProperty("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null)
                            {
                                bool ZoneAstral = (bool)prop.GetValue(calPlayer);
                                if (ZoneAstral)   // 星陨之地：改用灾厄的 AstralOrange 粉尘与橙色配色
                                {
                                    if (calamity.TryFind<ModDust>("AstralOrange", out ModDust astralOrange)) { dustType = astralOrange.Type; }
                                    color = new Color(255, 127, 80, Projectile.alpha);
                                }
                            }
                        }
                    }
                }
            }
            // 按当前群系粉尘类型生成一粒受弹体速度 10% 影响的拖尾粉尘，构成"彩色尾巴"外观
            int num458 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 1.2f);
            Main.dust[num458].noGravity = true;
            Main.dust[num458].velocity *= 0.5f;
            Main.dust[num458].velocity += Projectile.velocity * 0.1f;
            CDUtil.HomeInNPC(Projectile, 400f, 24f, 20f);   // 追踪 400 像素内最近的敌人：追踪速度 24、惯性 20
        }
        /// <summary>
        /// 命中敌人：根据玩家所在生物群系施加对应 debuff（持续 360 帧）。
        /// 例外：发光蘑菇地的 TemporalSadness 现代版只持续 120 帧。
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
            if (jungle)   // 丛林：剧毒 + 瘟疫
            {
                target.AddBuff(BuffID.Venom, 360);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 360); }
                }
            }
            else if (snow)   // 雪原：霜冻
            {
                target.AddBuff(BuffID.Frostburn, 360);
            }
            else if (beach)   // 海滩：沉海（CrushDepth）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 360); }
                }
            }
            else if (dungeon)   // 地牢：霜冻
            {
                target.AddBuff(BuffID.Frostburn, 360);
            }
            else if (desert || holy)   // 沙漠 / 神圣：圣火（现代版 HolyFlames，经典版 HolyLight）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 360); }
                }
            }
            else if (glow)   // 发光蘑菇地：时之悲（现代版 120 帧，经典版 360 帧）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 120); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 360); }
                }
            }
            else if (hell)   // 地狱：硫磺火
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 360); }
                }
            }
            else   // 其它（森林等）：甲壳破碎
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 360); }
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
                            // ZoneAstral 在灾厄里是只读属性（不是字段），必须用 GetProperty 才读得到
                            var prop = calamityPlayerType.GetProperty("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null)
                            {
                                bool ZoneAstral = (bool)prop.GetValue(calPlayer);
                                if (ZoneAstral)   // 星陨之地：追加星陨感染
                                {
                                    if (calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection)) { target.AddBuff(astralInfection.Type, 360); }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：同样依据生物群系施加 debuff
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
            if (jungle)   // 丛林：剧毒 + 瘟疫
            {
                target.AddBuff(BuffID.Venom, 360);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 360); }
                }
            }
            else if (snow)   // 雪原：霜冻
            {
                target.AddBuff(BuffID.Frostburn, 360);
            }
            else if (beach)   // 海滩：沉海（CrushDepth）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth)) { target.AddBuff(crushDepth.Type, 360); }
                }
            }
            else if (dungeon)   // 地牢：霜冻
            {
                target.AddBuff(BuffID.Frostburn, 360);
            }
            else if (desert || holy)   // 沙漠 / 神圣：圣火（现代版 HolyFlames，经典版 HolyLight）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 360); }
                }
            }
            else if (glow)   // 发光蘑菇地：时之悲（现代版 120 帧，经典版 360 帧）
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 120); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("TemporalSadness", out ModBuff temporalSadness)) { target.AddBuff(temporalSadness.Type, 360); }
                }
            }
            else if (hell)   // 地狱：硫磺火
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 360); }
                }
            }
            else   // 其它（森林等）：甲壳破碎
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 360); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("ArmorCrunch", out ModBuff armorCrunch)) { target.AddBuff(armorCrunch.Type, 360); }
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
                            // ZoneAstral 在灾厄里是只读属性（不是字段），必须用 GetProperty 才读得到
                            var prop = calamityPlayerType.GetProperty("ZoneAstral",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null)
                            {
                                bool ZoneAstral = (bool)prop.GetValue(calPlayer);
                                if (ZoneAstral)   // 星陨之地：追加星陨感染
                                {
                                    if (calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection)) { target.AddBuff(astralInfection.Type, 360); }
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
            // 反编译风格的循环：循环末把 num3 赋成 num795，等价于 num795++；num795 从 4 走到 30，共 27 组
            for (int num795 = 4; num795 < 31; num795 = num3 + 1)
            {
                // 沿旧速度的反方向把生成点往回推 30/num795 像素——num795 越大推得越近，即由远及近铺开
                float num796 = Projectile.oldVelocity.X * (30f / num795);
                float num797 = Projectile.oldVelocity.Y * (30f / num795);
                // 每组撒两粒当前群系色尘：大的（1.8）留一半速度，小的（1.4）几乎不动，形成碎裂感
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
        /// 自定义绘制：生成后前 5 帧不绘制避免跳变，随后绘制当前群系颜色的残影拖尾
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // 生成后前 5 帧不绘制
            if (Projectile.timeLeft > 295)
                return false;
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>
        /// 使用按生物群系刷新的 color 字段着色（含 alpha）
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return color;
        }
    }
}
