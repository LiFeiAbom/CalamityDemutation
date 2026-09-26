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
    /// 鸿蒙方舟 - 终局近战剑（月后稀有度 15）
    /// 挥砍发射远古光束（EonBeam）与群星（Galaxia），命中敌人时依据所处环境/月相获得对应 buff
    /// </summary>
    internal class ArkoftheCosmos:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 140、使用时间 14 帧、击退 9.5、无转向（useTurn=false）；
        /// 主弹幕为远古光束（EonBeam），每次挥砍发射一次，月后稀有度 15。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 102;
            Item.damage = 140;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 9.5f;
            Item.UseSound = SoundID.Item60;
            Item.autoReuse = true;
            Item.height = 102;
            Item.value = Item.buyPrice(2, 50, 0, 0);
            Item.shoot = ModContent.ProjectileType<EonBeam>();
            Item.shootSpeed = 14f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>
        /// 射击逻辑：主弹幕为远古光束（不碰撞、随机色彩、限时 160 帧），
        /// 并在玩家与鼠标连线中点处斜向散射 4 颗群星（Galaxia）弹幕
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile proj = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
            proj.timeLeft = 160;          // 光束存活 160 帧
            proj.tileCollide = false;     // 穿透地形
            proj.ai[1] = Main.rand.Next(1, 5);  // 随机光束颜色编号
            int fireOffset = -100;        // 每次散射的纵向间距（像素）
            Vector2 mousePos = Main.MouseWorld;
            int totalFire = 4;
            int firePosX = (int)(mousePos.X + player.Center.X) / 2;
            int firePosY = (int)player.Center.Y;
            for (int fireCount = 0; fireCount < totalFire; fireCount++)
            {
                // 从玩家中心上方依次错开生成散射起点
                Vector2 finalPos = new(firePosX, firePosY + fireOffset * fireCount);
                Vector2 direction = mousePos - finalPos;
                direction.Normalize();
                direction = direction.RotatedByRandom(MathHelper.ToRadians(15)); // ±15° 随机偏转
                Vector2 newVelocity = direction * velocity.Length();
                int projectileFire = Projectile.NewProjectile(source, finalPos, newVelocity, ModContent.ProjectileType<Galaxia>(), damage, knockback, player.whoAmI, 0f, Main.rand.Next(3));
                Main.projectile[projectileFire].timeLeft = 160;
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置，生成彩虹色无重力粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(5))
            {
                int num250 = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.RainbowTorch, player.direction * 2, 0f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.3f);
                Main.dust[num250].velocity *= 0.2f;
                Main.dust[num250].noGravity = true;
            }
        }
        /// <summary>
        /// 命中敌人：依据玩家所在环境（丛林/雪原/血月等）获得对应的增益 buff，
        /// 以及星陨环境下的重力正常化 buff（南瓜月成立时会跳过整条生物群系 if-else 链；
        /// 源版此处丛林那一支是独立 if，会与月相 buff 叠加）
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
                player.AddBuff(BuffID.WellFed3, 600);       // 南瓜月：三级饱食（WellFed3，比银河的普通饱食更高档）
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
                player.AddBuff(BuffID.WellFed3, 600);       // 南瓜月：三级饱食（WellFed3，比银河的普通饱食更高档）
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
        /// 武器暴击率 +16%（原版对 SetDefaults 中过高的暴击值处理异常，故放到此回调里加成）
        /// </summary>
        // Terraria seems to really dislike high crit values in SetDefaults
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 16;
        /// <summary>
        /// 配方（分版本）：银河 + 元素方舟 + AuricBar ×5 @ 宇宙砧（现代版，与灾厄 1.4.4 同名武器配方一致）；
        /// 经典版没有 AuricBar/宇宙砧，改用噩梦燃料/吸热能量/地狱施法者碎片/暗黑碎片 + AuricOre ×25 @ 德雷顿熔炉
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：银河 + 元素方舟 + AuricBar ×5，宇宙砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<FourSeasonsGalaxia>();   // 银河（本模组下位）
                    recipe.AddIngredient<ArkoftheElements>();     // 元素方舟（本模组下位）
                    recipe.AddIngredient(auricBar.Type, 5);       // 灾厄材料：AuricBar ×5
                    recipe.AddTile(cosmicAnvil.Type);             // 宇宙砧
                    recipe.Register();
                }
            }
            // ── 经典版灾厄：改用一堆后期暗黑材料，且在德雷顿熔炉合成 ──
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && calamity1.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && calamity1.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment)
                    && calamity1.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && calamity1.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                    && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<FourSeasonsGalaxia>();
                    recipe1.AddIngredient<ArkoftheElements>();
                    recipe1.AddIngredient(nightmareFuel.Type, 5);       // 经典版材料：噩梦燃料 ×5
                    recipe1.AddIngredient(endothermicEnergy.Type, 5);   // 经典版材料：吸热能量 ×5
                    recipe1.AddIngredient(hellcasterFragment.Type, 3);  // 经典版材料：地狱施法者碎片 ×3
                    recipe1.AddIngredient(darksunFragment.Type, 5);     // 经典版材料：暗黑碎片 ×5
                    recipe1.AddIngredient(auricOre.Type, 25);           // 经典版材料：AuricOre ×25
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
