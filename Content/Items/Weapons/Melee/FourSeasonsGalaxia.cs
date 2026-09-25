using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 四季银河 - 顶级近战剑
    /// 发射自动追踪的银河弹（Galaxia），命中敌人时依据所在环境/事件获得对应 buff 与二次弹幕
    /// </summary>
    internal class FourSeasonsGalaxia:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 425、使用时间 16 帧、击退 9、红色稀有度；
        /// 主弹幕为自动追踪的银河弹（Galaxia），每次挥砍发射一次，月后稀有度 14。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 70;
            Item.damage = 425;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 9f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.height = 70;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<Galaxia>();
            Item.shootSpeed = 24f;
            Item.shootsEveryUse = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 射击逻辑：每次挥砍发射 1 颗带轻微随机速度偏转的银河弹
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int projectiles = 0; projectiles < 1; projectiles++)
            {
                // 在基础速度上叠加 ±0.4 单位的随机偏转
                float speedX = velocity.X + Main.rand.Next(-40, 41) * 0.01f;
                float speedY = velocity.Y + Main.rand.Next(-40, 41) * 0.01f;
                Projectile.NewProjectile(source, position, new Vector2(speedX, speedY), type, damage, knockback, player.whoAmI);
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置，偶尔扬起泥土粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(5))
            {
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Dirt);
            }
        }
        /// <summary>
        /// 命中敌人：根据玩家所在环境/月相事件为玩家提供对应 buff。
        /// 血月/霜月/南瓜月优先，其次按生物群系（if-else 链）依次判定。
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
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
            // 月相事件优先：三者各自独立判定，可同时生效
            if (bloodMoon)
            {
                player.AddBuff(BuffID.Battle, 600);         // 血月：战斗
            }
            if (snowMoon)
            {
                player.AddBuff(BuffID.RapidHealing, 600);   // 霜月：快速治疗
            }
            if (pumpkinMoon)
            {
                player.AddBuff(BuffID.WellFed, 600);        // 南瓜月：饱食
            }
            // 其后按生物群系走 if-else 链，只取第一个匹配到的环境
            else if (jungle)
            {
                player.AddBuff(BuffID.Thorns, 600);         // 丛林：荆棘
            }
            else if (snow)
            {
                player.AddBuff(BuffID.Warmth, 600);         // 雪原：温暖
            }
            else if (beach)
            {
                player.AddBuff(BuffID.Wet, 600);            // 沙滩：潮湿
            }
            else if (corrupt)
            {
                player.AddBuff(BuffID.Wrath, 600);          // 腐化：愤怒（增伤）
            }
            else if (crimson)
            {
                player.AddBuff(BuffID.Rage, 600);           // 猩红：暴怒（增暴击）
            }
            else if (dungeon)
            {
                player.AddBuff(BuffID.Dangersense, 600);    // 地牢：危险感知
            }
            else if (desert)
            {
                player.AddBuff(BuffID.Endurance, 600);      // 沙漠：耐力
            }
            else if (glow)
            {
                player.AddBuff(BuffID.Spelunker, 600);      // 发光蘑菇：洞穴探险
            }
            else if (hell)
            {
                player.AddBuff(BuffID.Inferno, 600);        // 地狱：狱火
            }
            else if (holy)
            {
                player.AddBuff(BuffID.Heartreach, 600);     // 神圣：心之共鸣（Heartreach）
            }
            else if (nebula)
            {
                player.AddBuff(BuffID.MagicPower, 600);     // 星云柱：魔能
            }
            else if (stardust)
            {
                player.AddBuff(BuffID.Summoning, 600);      // 星尘柱：召唤
            }
            else if (solar)
            {
                player.AddBuff(BuffID.Titan, 600);          // 日耀柱：泰坦
            }
            else if (vortex)
            {
                player.AddBuff(BuffID.AmmoReservation, 600);// 星旋柱：弹药储备
            }
            else
            {
                player.AddBuff(BuffID.DryadsWard, 600);     // 其余环境：树妖祝福
            }
            // 星陨（灾厄自定义生物群系）无法用原版 Zone 读到，故用反射取灾厄 ModPlayer 的 ZoneAstral
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
                                    if (calamity.TryFind<ModBuff>("GravityNormalizerBuff", out ModBuff gravityNormalizerBuff))
                                    {
                                        player.AddBuff(gravityNormalizerBuff.Type, 600);   // 星陨：重力正常化
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // 经典版灾厄：ModPlayer 类名是 CalamityPlayerPreTrailer（ZoneAstral 与 GravityNormalizerBuff 同名同义）
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                var calamityPlayerType = calamity1.Code.GetTypes()
                   .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
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
                                    if (calamity1.TryFind<ModBuff>("GravityNormalizerBuff", out ModBuff gravityNormalizerBuff))
                                    {
                                        player.AddBuff(gravityNormalizerBuff.Type, 600);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：与 NPC 版逻辑一致——按环境/事件为攻击者自身附加对应 buff（含星陨重力正常化）
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
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
                player.AddBuff(BuffID.Thorns, 600);
            }
            else if (snow)
            {
                player.AddBuff(BuffID.Warmth, 600);
            }
            else if (beach)
            {
                player.AddBuff(BuffID.Wet, 600);
            }
            else if (corrupt)
            {
                player.AddBuff(BuffID.Wrath, 600);
            }
            else if (crimson)
            {
                player.AddBuff(BuffID.Rage, 600);
            }
            else if (dungeon)
            {
                player.AddBuff(BuffID.Dangersense, 600);
            }
            else if (desert)
            {
                player.AddBuff(BuffID.Endurance, 600);
            }
            else if (glow)
            {
                player.AddBuff(BuffID.Spelunker, 600);
            }
            else if (hell)
            {
                player.AddBuff(BuffID.Inferno, 600);
            }
            else if (holy)
            {
                player.AddBuff(BuffID.Heartreach, 600);
            }
            else if (nebula)
            {
                player.AddBuff(BuffID.MagicPower, 600);
            }
            else if (stardust)
            {
                player.AddBuff(BuffID.Summoning, 600);
            }
            else if (solar)
            {
                player.AddBuff(BuffID.Titan, 600);
            }
            else if (vortex)
            {
                player.AddBuff(BuffID.AmmoReservation, 600);
            }
            else
            {
                player.AddBuff(BuffID.DryadsWard, 600);
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
                                    if (calamity.TryFind<ModBuff>("GravityNormalizerBuff", out ModBuff gravityNormalizerBuff))
                                    {
                                        player.AddBuff(gravityNormalizerBuff.Type, 600);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // 经典版灾厄：ModPlayer 类名是 CalamityPlayerPreTrailer（ZoneAstral 与 GravityNormalizerBuff 同名同义）
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                var calamityPlayerType = calamity1.Code.GetTypes()
                   .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
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
                                    if (calamity1.TryFind<ModBuff>("GravityNormalizerBuff", out ModBuff gravityNormalizerBuff))
                                    {
                                        player.AddBuff(gravityNormalizerBuff.Type, 600);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 配方（分版本）：Ω生物群系之剑 + 星辉矿锭 + 暗黑碎片（现代版）/ Phantoplasm（经典版），在宇宙砧/德雷顿熔炉合成
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：Ω生物群系之剑 + 星辉矿锭 ×8 + 暗黑碎片 ×8，宇宙砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<OmegaBiomeBlade>();        // Ω生物群系之剑（本模组下位）
                    recipe.AddIngredient(cosmiliteBar.Type, 8);     // 灾厄材料：星辉矿锭 ×8
                    recipe.AddIngredient(darksunFragment.Type, 8);  // 灾厄材料：暗黑碎片 ×8
                    recipe.AddTile(cosmicAnvil.Type);               // 宇宙砧
                    recipe.Register();
                }
            }
            // ── 经典版灾厄：材料改为 Phantoplasm、且改在德雷顿熔炉合成 ──
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && calamity1.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<OmegaBiomeBlade>();
                    recipe1.AddIngredient(classicCosmiliteBar.Type, 10);  // 经典版材料：星辉矿锭 ×10
                    recipe1.AddIngredient(phantoplasm.Type, 5);           // 经典版材料：幻影质 ×5
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
