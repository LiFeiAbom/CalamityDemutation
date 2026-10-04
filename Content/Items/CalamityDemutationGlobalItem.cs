using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Content.Items.Accessories.Attack;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Magic;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Summon;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using CalamityDemutation.Content.Items.Accessories.Wings;
namespace CalamityDemutation.Content.Items
{
    /// <summary>
    /// 全局物品类，管理自定义稀有度系统（postMoonLordRarity）和原版装备数值回调
    /// （回退现代版灾厄对原版武器/护甲/饰品的伤害、攻速、弹速、防御、斧力、价值削弱）。
    /// </summary>
    internal class CalamityDemutationGlobalItem : GlobalItem
    {
        // ── 静态字段 ──
        /// <summary>
        /// 原版斧力（ItemID -> 原版 axe 内部值 = 显示百分比 / 5）。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaAxe = new()
        {
            // Item.axe 以 5% 为单位（显示值 = axe × 5），注释里的百分数即可读值
            { ItemID.AcornAxe, 30 },              // 150%
            { ItemID.AdamantiteChainsaw, 20 },    // 100%（灾厄 90%）
            { ItemID.MoltenHamaxe, 30 },          // 150%（灾厄 125%）
            { ItemID.MythrilChainsaw, 17 },       // 85%（灾厄 80%）
            { ItemID.OrichalcumChainsaw, 18 },    // 90%（灾厄 80%）
            { ItemID.PalladiumChainsaw, 15 },     // 75%（灾厄 70%）
            { ItemID.SawtoothShark, 14 },         // 70%
            { ItemID.TitaniumChainsaw, 21 },      // 105%（灾厄 90%）
        };
        /// <summary>
        /// 原版镐力（ItemID -> 原版 item.pick）。tModLoader.xml 口径：pick 的显示值就等于字段值。
        /// 只收灾厄**调低**的条目（灾厄调高的——如钴/秘银钻头镐、噩梦镐、银镐——不回退）。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaPick = new()
        {
            { ItemID.LaserDrill, 230 },           // 灾厄 220
            { ItemID.LeadPickaxe, 43 },           // 灾厄 40
            { ItemID.OrichalcumDrill, 165 },      // 灾厄 160
            { ItemID.OrichalcumPickaxe, 165 },    // 灾厄 160
            { ItemID.PlatinumPickaxe, 59 },       // 灾厄 55
            { ItemID.TitaniumDrill, 190 },        // 灾厄 180
            { ItemID.TitaniumPickaxe, 190 },      // 灾厄 180
        };
        /// <summary>
        /// 原版锤力（ItemID -> 原版 item.hammer）。tModLoader.xml 口径：hammer 的显示值等于字段值。
        /// 只收灾厄**调低**的条目（灾厄调高的——铁/铅/银/金/铜/陨石/血肉/熔岩锤等——不回退）。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaHammer = new()
        {
            { ItemID.AshWoodHammer, 45 },         // 灾厄 25
            { ItemID.BorealWoodHammer, 35 },      // 灾厄 25
            { ItemID.EbonwoodHammer, 40 },        // 灾厄 25
            { ItemID.PalmWoodHammer, 35 },        // 灾厄 25
            { ItemID.PearlwoodHammer, 55 },       // 灾厄 25
            { ItemID.RichMahoganyHammer, 35 },    // 灾厄 25
            { ItemID.Rockfish, 70 },              // 灾厄 50
            { ItemID.ShadewoodHammer, 40 },       // 灾厄 25
            { ItemID.TinHammer, 38 },             // 灾厄 35
        };
        /// <summary>
        /// 原版伤害（ItemID -> 原版 damage）。灾厄用 DamageExact/DamageRatio 调低了这些武器/弹药的伤害。
        /// 含少量「混合条目」里被调低的伤害（如 Flamethrower、RainbowRod、Seedler）。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaDamage = new()
        {
            { ItemID.AquaScepter, 27 },
            { ItemID.BeesKnees, 23 },
            { ItemID.BlizzardStaff, 58 },
            { ItemID.Boomstick, 14 },
            { ItemID.ChristmasTreeSword, 86 },
            { ItemID.Code2, 54 },
            { ItemID.CrystalBullet, 9 },
            { ItemID.DaedalusStormbow, 38 },
            { ItemID.DayBreak, 150 },
            { ItemID.DD2SquireBetsySword, 180 },
            { ItemID.DemonBow, 14 },
            { ItemID.DemonScythe, 35 },
            { ItemID.EmpressBlade, 90 },
            { ItemID.Flamarang, 49 },
            { ItemID.Flamethrower, 35 },
            { ItemID.FlowerofFire, 48 },
            { ItemID.Gradient, 49 },
            { ItemID.IchorBullet, 13 },
            { ItemID.InfluxWaver, 100 },
            { ItemID.Kraken, 95 },
            { ItemID.LaserMachinegun, 60 },
            { ItemID.MagicMissile, 35 },
            { ItemID.LastPrism, 100 },
            { ItemID.Minishark, 6 },
            { ItemID.MoonlordBullet, 20 },
            { ItemID.MoonlordTurretStaff, 100 },
            { ItemID.Musket, 31 },
            { ItemID.NailGun, 85 },
            { ItemID.RainbowRod, 50 },
            { ItemID.RavenStaff, 55 },
            { ItemID.Razorpine, 48 },
            { ItemID.RedsYoyo, 70 },
            { ItemID.ScourgeoftheCorruptor, 70 },
            { ItemID.Seedler, 50 },
            { ItemID.SilverBullet, 9 },
            { ItemID.StardustDragonStaff, 40 },
            { ItemID.TendonBow, 19 },
            { ItemID.Terragrim, 17 },
            { ItemID.Terrarian, 190 },
            { ItemID.TheEyeOfCthulhu, 115 },
            { ItemID.TheUndertaker, 19 },
            { ItemID.ThunderStaff, 20 },
            { ItemID.Tsunami, 53 },
            { ItemID.TungstenBullet, 9 },
            { ItemID.UnholyTrident, 88 },
            { ItemID.ValkyrieYoyo, 70 },
            { ItemID.Yelets, 60 },
        };
        /// <summary>
        /// 原版防御（ItemID -> 原版 defense）。灾厄用 DefenseDelta 调低了 Valhalla Knight 套装的防御。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaDefense = new()
        {
            { ItemID.SquireAltHead, 20 },
            { ItemID.SquireAltPants, 24 },
            { ItemID.SquireAltShirt, 24 },
            { ItemID.SquireGreatHelm, 13 },
            { ItemID.SquireGreaves, 18 },
            { ItemID.SquirePlating, 27 },
        };
        /// <summary>
        /// 原版弹速（ItemID -> 原版 shootSpeed）。
        /// </summary>
        private static readonly Dictionary<int, float> VanillaShootSpeed = new()
        {
            { ItemID.IceBoomerang, 11.5f },
            { ItemID.SpiritFlame, 3f },
        };
        /// <summary>
        /// 原版攻速（ItemID -> 原版 useTime）。灾厄用 UseExact 同时调高了 useTime 与 useAnimation。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaUseTime = new()
        {
            { ItemID.AdamantiteWaraxe, 8 },
            { ItemID.Beenade, 15 },
            { ItemID.BeesKnees, 23 },
            { ItemID.ChlorophyteClaymore, 26 },
            { ItemID.ChlorophyteJackhammer, 4 },
            { ItemID.ChristmasTreeSword, 23 },
            { ItemID.CoinGun, 8 },
            { ItemID.DayBreak, 16 },
            { ItemID.Handgun, 15 },
            { ItemID.IceBoomerang, 20 },
            { ItemID.InfluxWaver, 20 },
            { ItemID.MoltenFury, 22 },
            { ItemID.MythrilWaraxe, 10 },
            { ItemID.NebulaDrill, 2 },
            { ItemID.OrichalcumWaraxe, 9 },
            { ItemID.PhoenixBlaster, 14 },
            { ItemID.PsychoKnife, 8 },
            { ItemID.Sandgun, 16 },
            { ItemID.SolarFlareDrill, 2 },
            { ItemID.StarCannon, 12 },
            { ItemID.StardustDrill, 2 },
            { ItemID.TitaniumPickaxe, 7 },
            { ItemID.TitaniumWaraxe, 7 },
            { ItemID.VortexDrill, 2 },
        };
        /// <summary>
        /// 原版价值（ItemID -> 原版 value；数值用 Item.sellPrice 按"售价"口径书写，便于与原版表对照）。
        /// 灾厄用 Worthless 把这些物品的价值归零。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaValue = new()
        {
            { ItemID.Mushroom, Item.sellPrice(silver: 2, copper: 50) },   // 桌面版 1.3.0.1 起为 2银50铜（5铜是旧主机版的值）
            { ItemID.GlowingMushroom, Item.sellPrice(copper: 10) },
            { ItemID.VileMushroom, Item.sellPrice(copper: 10) },          // 腐化蘑菇实为 10 铜（原误写成 2银50铜）
            { ItemID.ViciousMushroom, Item.sellPrice(copper: 10) },       // 血腥蘑菇同为 10 铜
            { ItemID.EncumberingStone, Item.sellPrice(gold: 1) },
            { ItemID.UncumberingStone, Item.sellPrice(gold: 1) },
            { ItemID.PortableStool, Item.sellPrice(silver: 50) },         // 便携凳（Step Stool）：原版售价 50 银，灾厄 Worthless 归零
        };
        // ── 实例字段 ──
        /// <summary>
        /// 月后自定义稀有度等级（0=未设置，12~22=各级对应不同颜色）
        /// </summary>
        public int postMoonLordRarity = 0;
        // ── 属性 ──
        /// <summary>
        /// 联机同步时按实例克隆，保证稀有度等级随物品单独传输
        /// </summary>
        protected override bool CloneNewInstances
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// 稀有度等级按物品实例独立保存，故需按实体实例化
        /// </summary>
        public override bool InstancePerEntity
        {
            get
            {
                return true;
            }
        }
        // ── 生命周期方法 ──
        /// <summary>
        /// 将自定义稀有度物品的基础稀有度统一设为红色（真正的名称颜色覆盖由 ModifyTooltips 完成）
        /// </summary>
        public override void SetDefaults(Item entity)
        {
            if (postMoonLordRarity != 0 && entity.rare != ItemRarityID.Red)
                entity.rare = ItemRarityID.Red;
            // 回退灾厄对原版玩家装备的削弱：仅 Config 开启 + 现代版灾厄已加载时恢复原版数值
            if (ConfigSystem.Instance?.RevertVanillaNerfs == true && ModLoader.HasMod("CalamityMod"))
                RevertVanillaNerf(entity);
        }
        /// <summary>
        /// 血炎射手套装（bloodflareRanged）的「远程武器有几率射出血液爆炸光球」：
        /// 每次射击 2% 概率追加一枚 BloodBomb，伤害 = 本次射击伤害 ×1.6（穿金源套时 ×2.2），
        /// 口径照经典版 CalamityGlobalItem.Shoot（该版另判 !rogue，本工程无盗贼职业故省略）。
        /// 这是全物品钩子：不满足条件时直接放行原弹幕。
        /// </summary>
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.bloodflareRanged && item.CountsAsClass<RangedDamageClass>() && Main.rand.Next(0, 100) >= 98)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    int bombDamage = (int)(damage * (modPlayer.auricSet ? 2.2f : 1.6f));
                    Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<BloodBomb>(), bombDamage, 2f, player.whoAmI);
                }
            }
            // 弑神者射手套装（godSlayerRanged）的「发射远程武器时有几率射出弑神者破片弹」：
            // 每次射击 5% 概率追加一枚 GodSlayerShrapnelRound，伤害 = 本次射击伤害 ×2.1（穿金源套时 ×3.2）、弹速 ×1.25，
            // 口径照经典版 CalamityGlobalItem.Shoot（该版另判 !rogue，本工程无盗贼职业故省略）。
            if (modPlayer.godSlayerRanged && item.CountsAsClass<RangedDamageClass>() && Main.rand.Next(0, 100) >= 95)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    int roundDamage = (int)(damage * (modPlayer.auricSet ? 3.2f : 2.1f));
                    Projectile.NewProjectile(source, position, velocity * 1.25f, ModContent.ProjectileType<GodSlayerShrapnelRound>(), roundDamage, 2f, player.whoAmI);
                }
            }
            return true;
        }
        /// <summary>
        /// 保存/同步自定义稀有度等级（SaveData 存档、NetSend 联机同步）
        /// </summary>
        public override void SaveData(Item item, TagCompound tag)
        {
            tag.Add("rarity", postMoonLordRarity);
        }
        /// <summary>
        /// 从存档读回自定义稀有度等级
        /// </summary>
        public override void LoadData(Item item, TagCompound tag)
        {
            postMoonLordRarity = tag.GetInt("rarity");
        }
        /// <summary>
        /// 联机同步：写出自定义稀有度等级
        /// </summary>
        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(postMoonLordRarity);
        }
        /// <summary>
        /// 联机同步：读入自定义稀有度等级
        /// </summary>
        public override void NetReceive(Item item, BinaryReader reader)
        {
            postMoonLordRarity = reader.ReadInt32();
        }
        /// <summary>
        /// 根据稀有度等级覆盖物品名称颜色（仅改名字颜色，不影响其他提示行）。
        /// 等级 12~22 对应不同的自定义"月后稀有度"色板（由 SetDefaults 统一把稀有度设为红）。
        /// </summary>
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (postMoonLordRarity == 0)
                return;
            TooltipLine nameLine = tooltips.FirstOrDefault(x => x.Name == "ItemName" && x.Mod == "Terraria");
            if (nameLine != null)
            {
                nameLine.OverrideColor = postMoonLordRarity switch
                {
                    12 => new Color(0, 255, 200),      // 12：青绿色
                    13 => new Color(0, 255, 0),        // 13：荧光绿
                    14 => new Color(43, 96, 222),      // 14：蓝
                    15 => new Color(108, 45, 199),     // 15：紫
                    16 => new Color(255, 0, 255),      // 16：品红
                    17 => GetLegendaryWeaponColor(item.type), // 17：传奇武器（按具体武器给专属色）
                    18 => new Color(Main.DiscoR, 100, 255), // 18：闪烁紫
                    19 => new Color(0, 0, 255),        // 19：纯蓝
                    20 => new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), // 20：彩虹闪烁
                    21 => new Color(139, 0, 0),        // 21：暗红
                    22 => new Color(255, 140, 0),      // 22：橙
                    _ => default,                      // 其余等级无颜色覆盖
                };
            }
        }
        /// <summary>
        /// 向灾厄各 Boss 宝藏袋注入本模组的饰品掉落，兼容现代版（CalamityMod）与经典预发布版（CalamityModClassicPreTrailer）两套灾厄
        /// </summary>
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("PerforatorBag", out ModItem perforatorBag) && item.type == perforatorBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodyWormTooth>(), 1));
                }
                if(calamity.TryFind<ModItem>("RavagerBag", out ModItem ravagerBag) && item.type == ravagerBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodPact>(), 1));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FleshTotem>(), 2));
                    if (BossSystem.Providence)
                    {
                        itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodflareCore>(), 1));
                    }
                }
                if (calamity.TryFind<ModItem>("DevourerofGodsBag", out ModItem devourerofGodsBag) && item.type == devourerofGodsBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<NebulousCore>(), 1));
                    // 宙宇波能刃（灾厄原版 Excelsus 即神吞袋武器池掉落，本模组移植后按 1/3 概率挂回神吞袋）
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Excelsus>(), 3));
                }
                if (calamity.TryFind<ModItem>("PolterghastBag", out ModItem polterghastBag) && item.type == polterghastBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Affliction>(), 1));
                    // 女妖之爪（CWR 重制版）：源版波尔提斯袋里它与另外 6 把武器同属一组，
                    // 走 DropHelper.CalamityStyle(1/3) 的"每把 1/3、全空则保底随机给一把"抽取；
                    // 本工程沿用既有简化口径（同 YharonBag / DevourerofGodsBag 的 1/3），挂在宝袋上
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BansheeHook>(), 3));
                }
                if (calamity.TryFind<ModItem>("LeviathanBag", out ModItem leviathanLureBag) && item.type == leviathanLureBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LureofEnthrallment>(), 3));
                }
                if (calamity.TryFind<ModItem>("BrimstoneElementalBag", out ModItem brimstoneElementalBag) && item.type == brimstoneElementalBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RoseStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Gehenna>(), 1));
                }
                if ((calamity.TryFind<ModItem>("AquaticScourgeBag", out ModItem aquaticScourgeBag) && item.type == aquaticScourgeBag.Type) || (calamity.TryFind<ModItem>("DesertScourgeBag", out ModItem desertScourgeBag) && item.type == desertScourgeBag.Type))
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<AeroStone>(), 10));
                }
                if (calamity.TryFind<ModItem>("CryogenBag", out ModItem cryogenBag) && item.type == cryogenBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                }
                if (calamity.TryFind<ModItem>("CalamitasCloneBag", out ModItem calamitasCloneBag) && item.type == calamitasCloneBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CalamityRing>(), 1));
                }
                if (calamity.TryFind<ModItem>("PlaguebringerGoliathBag", out ModItem plaguebringerGoliathBag) && item.type == plaguebringerGoliathBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                }
                if(calamity.TryFind<ModItem>("HiveMindBag", out ModItem hiveMindBag) && item.type == hiveMindBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RottenBrain>(), 1));
                }
                if(calamity.TryFind<ModItem>("CrabulonBag", out ModItem crabulonBag) && item.type == crabulonBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FungalClump>(), 1));
                }
                if (calamity.TryFind<ModItem>("LeviathanBag", out ModItem leviathanBag) && item.type == leviathanBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LeviathanAmbergris>(), 1));
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<TheCommunity>(), 100));
                }
                if(item.type == ItemID.GolemBossBag)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DefenseBlade>(), 100));
                }
                if (calamity.TryFind<ModItem>("YharonBag", out ModItem yharonBag) && item.type == yharonBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<DrewsWings>(), 1));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DragonRage>(), 3));
                    // 焚灭天惩（源版里它与 DragonRage 同属犽戎宝袋那组 8 把武器、走 DropHelper.CalamityStyle 保底抽取；
                    // 本工程沿用既有简化口径给 1/3，与 DragonRage 一致。现代版与经典版灾厄的犽戎宝袋都掉了这把）
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<TheBurningSky>(), 3));
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("PerforatorBag", out ModItem perforatorBag) && item.type == perforatorBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodyWormTooth>(), 1));
                }
                if (classic.TryFind<ModItem>("RavagerBag", out ModItem ravagerBag) && item.type == ravagerBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodPact>(), 2));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FleshTotem>(), 2));
                    if (BossSystem.Providence)
                    {
                        itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodflareCore>(), 1));
                    }
                }
                if (classic.TryFind<ModItem>("DevourerofGodsBag", out ModItem devourerofGodsBag) && item.type == devourerofGodsBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<NebulousCore>(), 1));
                    // 宙宇波能刃（灾厄原版 Excelsus 即神吞袋武器池掉落，本模组移植后按 1/3 概率挂回神吞袋）
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Excelsus>(), 3));
                }
                if (classic.TryFind<ModItem>("PolterghastBag", out ModItem polterghastBag) && item.type == polterghastBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Affliction>(), 1));
                    // 女妖之爪（CWR 重制版）：经典版波尔提斯袋里它本就是 CommonDrop(..., 3)，这里是精确复刻
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BansheeHook>(), 3));
                }
                if (classic.TryFind<ModItem>("LeviathanBag", out ModItem leviathanLureBag) && item.type == leviathanLureBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LureofEnthrallment>(), 3));
                }
                if(classic.TryFind<ModItem>("BrimstoneWaifuBag", out ModItem brimstoneWaifuBag) && item.type == brimstoneWaifuBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RoseStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Gehenna>(), 1));
                }
                if((classic.TryFind<ModItem>("AquaticScourgeBag", out ModItem aquaticScourgeBag) && item.type == aquaticScourgeBag.Type) || (classic.TryFind<ModItem>("DesertScourgeBag", out ModItem desertScourgeBag) && item.type == desertScourgeBag.Type))
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<AeroStone>(), 10));
                }
                if (classic.TryFind<ModItem>("CryogenBag", out ModItem cryogenBag) && item.type == cryogenBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                }
                if (classic.TryFind<ModItem>("CalamitasBag", out ModItem calamitasBag) && item.type == calamitasBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CalamityRing>(), 1));
                }
                if (classic.TryFind<ModItem>("PlaguebringerGoliathBag", out ModItem plaguebringerGoliathBag) && item.type == plaguebringerGoliathBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                }
                if (classic.TryFind<ModItem>("HiveMindBag", out ModItem hiveMindBag) && item.type == hiveMindBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RottenBrain>(), 1));
                }
                if (classic.TryFind<ModItem>("CrabulonBag", out ModItem crabulonBag) && item.type == crabulonBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FungalClump>(), 1));
                }
                if (classic.TryFind<ModItem>("LeviathanBag", out ModItem leviathanBag) && item.type == leviathanBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LeviathanAmbergris>(), 1));
                }
                if (item.type == ItemID.GolemBossBag)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DefenseBlade>(), 100));
                }
                if (classic.TryFind<ModItem>("YharonBag", out ModItem yharonsBag) && item.type == yharonsBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<DrewsWings>(), 1));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DragonRage>(), 3));
                    // 焚灭天惩（源版里它与 DragonRage 同属犽戎宝袋那组 8 把武器、走 DropHelper.CalamityStyle 保底抽取；
                    // 本工程沿用既有简化口径给 1/3，与 DragonRage 一致。现代版与经典版灾厄的犽戎宝袋都掉了这把）
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<TheBurningSky>(), 3));
                }
            }
        }
        public override bool OnPickup(Item item, Player player)
        {
            if (item.type == ItemID.Heart || item.type == ItemID.CandyApple || item.type == ItemID.CandyCane)
            {
                // 解除BOSS至尊灾厄的限制
                bool boostedHeart = player.GetModPlayer<CalamityDemutationPlayer>().photosynthesis;
                if (boostedHeart)
                {
                    player.statLife += 5;
                    if (Main.myPlayer == player.whoAmI)
                    {
                        player.HealEffect(5, true);
                    }
                    if (player.statLife > player.statLifeMax2)
                        player.statLife = player.statLifeMax2;
                }
            }
            return true;
        }
        /// <summary>
        /// PvP：近战武器直接挥砍命中玩家时，按攻击者的装备 / 套装 / 身上的 buff 给受害者施加效果，
        /// 效果集合与 OnHitNPCWithItem 对齐：
        /// - 亚利姆徽章：随机 120/240/360 帧神圣火（现代版）与圣光（经典版）；
        /// - 元素手套：全套元素 debuff 各 120 帧（原版五毒 + 灾厄现代/经典两版本）；
        /// - 神圣之怒 buff（HolyWrath）：120 帧神圣火与圣光；
        /// - omega 蓝胸甲（omegaBlueChestplate）：240 帧 HadopelagicPressure / CrushDepth；
        /// - 恶魔残影套装（demonshadeSetBonus）：随机 360/240/120 帧恶魔烈焰；
        /// - 血焰套装（bloodflareMelee）/ 弑神近战（godSlayerMelee）：作用在攻击者自身
        ///   （累计命中并本端小额回血 / 生成弑神飞镖）。
        /// 仅近战挥击触发（文档明确 "melee weapon hits a player"），近战弹幕走 GlobalProjectile.OnHitPlayer。
        /// </summary>
        public override void OnHitPvp(Item item, Player player, Player target, Player.HurtInfo hurtInfo)
        {
            // player = 攻击者 A，target = 被打中的 B
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 亚利姆徽章 / 元素手套：直接查 A 的实际装备（不依赖 ModPlayer 标志位）
            if (CalamityDemutationPlayer.IsAccessoryEquipped(player, ModContent.ItemType<YharimsInsignia>()))
            {
                // 亚利姆徽章：给受害者施加圣焰（现代版）/圣光（经典版），随机时长
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", Main.rand.NextBool(4) ? 360 : Main.rand.NextBool(2) ? 240 : 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", Main.rand.NextBool(4) ? 360 : Main.rand.NextBool(2) ? 240 : 120);
            }
            if (CalamityDemutationPlayer.IsAccessoryEquipped(player, ModContent.ItemType<ElementalGauntlet>()))
            {
                // 元素手套：给受害者施加全套元素 debuff（原版 + 两灾厄变体）
                target.AddBuff(BuffID.Poisoned, 120, false);
                target.AddBuff(BuffID.OnFire, 120, false);
                target.AddBuff(BuffID.CursedInferno, 120, false);
                target.AddBuff(BuffID.Frostburn, 120, false);
                target.AddBuff(BuffID.Ichor, 120, false);
                target.AddBuff(BuffID.Venom, 120, false);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Voidfrost", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Nightwither", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Plague", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "SulphuricPoisoning", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "AbyssalFlames", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", 120);
                if (Main.rand.NextBool(5))
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GlacialState", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 120);
            }
            // omega 蓝套装：与 NPC 侧（OnHitNPCWithItem）判定口径统一，取胸甲单件标记 omegaBlueChestplate
            if (modPlayer.omegaBlueChestplate)
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HadopelagicPressure", 240);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 240);
            }
            // 神圣之怒（HolyWrath）：给受害者施加 120 帧神圣火（现代版）与圣光（经典版）。
            // holyWrath 由 HolyWrath buff 置位（非饰品），故查攻击者的 buff 列表（跨端同步），不用 IsAccessoryEquipped
            if (player.HasBuff(ModContent.BuffType<HolyWrath>()))
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
            }
            // 恶魔残影套装（demonshadeSetBonus）：随机 360/240/120 帧恶魔烈焰
            if (modPlayer.demonshadeSetBonus)
            {
                if (Main.rand.NextBool(4))
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 360, false);
                else if (Main.rand.NextBool(2))
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 240, false);
                else
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 120, false);
            }
            // 血焰套装（bloodflareMelee）：累计命中数并在本端小额回血（与 OnHitNPCWithItem 同构）
            if (modPlayer.bloodflareMelee)
            {
                if (modPlayer.bloodflareMeleeHits < 15 && modPlayer.bloodflareFrenzyTimer <= 0 && modPlayer.bloodflareFrenzyCooldown <= 0)
                {
                    modPlayer.bloodflareMeleeHits++;
                }
                if (player.whoAmI == Main.myPlayer)
                {
                    int healAmount = Main.rand.Next(3) + 1;
                    player.statLife += healAmount;
                    player.HealEffect(healAmount);
                    if (player.statLife > player.statLifeMax2)
                        player.statLife = player.statLifeMax2;
                }
            }
            // 弑神近战（godSlayerMelee）：生成弑神飞镖（与 OnHitNPCWithItem 同构）
            if (modPlayer.godSlayerMelee && modPlayer.godSlayerMeleefireCD <= 0)
            {
                int finalDamage = 500 + player.HeldItem.damage / 2;
                Vector2 spawnPos = new(player.Center.X, player.Center.Y);
                Vector2 velocity = CDUtil.GiveVelocity(200f);
                Projectile.NewProjectile(player.GetSource_FromThis(), spawnPos, velocity * 4f, ModContent.ProjectileType<GodSlayerDart>(), finalDamage, 0f, player.whoAmI);
                modPlayer.godSlayerMeleefireCD = 60;
            }
            if (modPlayer.armorShattering || modPlayer.armorCrumbling)
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ArmorCrunch", 240);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "ArmorCrunch", 240);
            }
        }
        // ── 原版削弱回调 ──
        /// <summary>
        /// 回退灾厄对原版饰品的削弱（侦察镜/狙击镜/手套系/日月石系）。
        /// </summary>
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (ConfigSystem.Instance?.RevertVanillaNerfs != true || !ModLoader.HasMod("CalamityMod"))
                return;
            switch (item.type)
            {
                case ItemID.ReconScope:
                    player.GetCritChance<RangedDamageClass>() += 5;
                    break;
                case ItemID.SniperScope:
                    player.GetDamage<RangedDamageClass>() += 0.1f;
                    break;
                // 手套系：灾厄对这套是「先减后按等级重发」的替换（不是单纯减法）——
                // CalamityGlobalItem.cs:1142-1170 每件先 -0.12（抵掉本体 12%），
                // CalamityPlayerMiscEffects.cs:3638-3642 再按最高级手套重发：
                // 野性爪 10%、力量/狂战士 12%、机械 12%、烈火 14%、元素手套 15%。
                // 所以在 1.4.4 下「相对本体 12%」只有野性爪净亏 2%；
                // 力量/狂战士/机械仍是 12%、烈火反而是 14%（加强）。
                // 这四件一律不能再补 —— 补上就是 22%~26%（烈火手套曾实测 26%）。
                case ItemID.FeralClaws:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.02f;
                    break;
                case ItemID.SunStone:
                    if (Main.dayTime)
                        player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                    break;
                case ItemID.MoonStone:
                    if (!Main.dayTime || Main.eclipse)
                        player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                    break;
                case ItemID.CelestialStone:
                case ItemID.CelestialShell:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                    break;
            }
        }
        /// <summary>
        /// 回退灾厄对原版护甲套装的削弱，按 tML 的原版套装名字符串分支
        /// （灾厄的 VanillaArmorChanges/ 各套装削弱逐条对应，见各 case 注释）。
        /// 未处理：Nebula 的 IL 阈值改动、Spectre 的 ghostDmg 逐帧累加、
        /// Frost/SpectreHealing/Gi/Monk 系属「重做」而非纯削弱。
        /// </summary>
        public override void UpdateArmorSet(Player player, string set)
        {
            if (ConfigSystem.Instance?.RevertVanillaNerfs != true || !ModLoader.HasMod("CalamityMod"))
                return;
            switch (set)
            {
                case "WizardHat":
                    player.GetCritChance<MagicDamageClass>() += 6;
                    break;
                case "MagicHat":
                    player.statManaMax2 += 20;
                    break;
                case "Adamantite":                    // AdamantiteArmorSetChange.cs:44-47：仅近战头在套装内扣 5% 近战攻速
                    if (player.armor[0].type == ItemID.AdamantiteHelmet)
                        player.GetAttackSpeed<MeleeDamageClass>() += 0.05f;
                    break;
                case "Cobalt":                        // 近战攻速 10%→5%
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.05f;
                    break;
                case "Jungle":                        // 套装：法力消耗 -16% 被抬到 -10%
                    player.manaCost -= 0.06f;
                    break;
                case "Molten":                        // 近战伤害 10%→7%
                    player.GetDamage<MeleeDamageClass>() += 0.03f;
                    break;
                case "SolarFlare":                    // 套装 12% 减伤被直接扣掉
                    player.endurance += 0.12f;
                    break;
            }
        }
        /// <summary>
        /// 回退灾厄对原版护甲单件的削弱（魔法帽/宝石长袍/武道服/蘑菇矿胸甲/侍从衣裤/日耀头/星旋头）。
        /// </summary>
        public override void UpdateEquip(Item item, Player player)
        {
            if (ConfigSystem.Instance?.RevertVanillaNerfs != true || !ModLoader.HasMod("CalamityMod"))
                return;
            switch (item.type)
            {
                case ItemID.MagicHat:
                    player.GetDamage<MagicDamageClass>() += 0.06f;
                    break;
                case ItemID.JungleHat:                // JungleArmorSetChange.cs:30-31 头件扣 20 法力 / 3 魔法暴击
                case ItemID.AncientCobaltHelmet:      // 头件的替代件（远古钴头盔）同款削弱
                    player.statManaMax2 += 20;
                    player.GetCritChance<MagicDamageClass>() += 3;
                    break;
                case ItemID.JunglePants:              // JungleArmorSetChange.cs:34 腿件扣 3 魔法暴击
                case ItemID.AncientCobaltLeggings:
                    player.GetCritChance<MagicDamageClass>() += 3;
                    break;
                case ItemID.AmethystRobe:
                    player.manaCost -= 0.01f;
                    break;
                case ItemID.TopazRobe:
                    player.statManaMax2 += 20;
                    player.manaCost -= 0.02f;
                    break;
                case ItemID.SapphireRobe:
                    player.manaCost -= 0.03f;
                    break;
                case ItemID.EmeraldRobe:
                    player.statManaMax2 += 20;
                    player.manaCost -= 0.04f;
                    break;
                case ItemID.RubyRobe:
                case ItemID.AmberRobe:
                    player.manaCost -= 0.05f;
                    break;
                case ItemID.DiamondRobe:
                    player.statManaMax2 += 20;
                    player.manaCost -= 0.06f;
                    break;
                case ItemID.Gi:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                    break;
                case ItemID.ShroomiteBreastplate:
                    player.GetDamage<RangedDamageClass>() += 0.05f;
                    player.GetCritChance<RangedDamageClass>() += 5;
                    break;
                case ItemID.SquireAltShirt:
                    player.GetDamage<SummonDamageClass>() += 0.1f;
                    break;
                case ItemID.SquireAltPants:
                    player.GetCritChance<MeleeDamageClass>() += 5;
                    player.GetDamage<SummonDamageClass>() += 0.05f;
                    break;
                case ItemID.SolarFlareHelmet:
                    player.GetCritChance<MeleeDamageClass>() += 6;
                    break;
                case ItemID.VortexHelmet:
                    player.GetDamage<RangedDamageClass>() += 0.06f;
                    player.GetCritChance<RangedDamageClass>() += 2;
                    break;
            }
        }
        public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            float flightSpeedMult = 1f
                                    + (modPlayer.holyWrath ? 0.05f : 0f)
                                    + (modPlayer.soaring ? 0.1f : 0f)
                                    + (modPlayer.profanedRage ? 0.05f : 0f)
                                    + (modPlayer.draconicSurge ? 0.15f : 0f);
            float flightAccMult = 1f + (modPlayer.draconicSurge ? 0.15f : 0f);
            speed *= flightSpeedMult;
            acceleration *= flightAccMult;
        }
        /// <summary>
        /// 回退灾厄对原版翅膀「上升速度」的削弱：CalamityGlobalItem.VerticalWingSpeeds 里
        /// ButterflyWings `maxAscentMultiplier *= 0.6667f`、GhostWings `*= 0.6625f`
        /// （同处的 constantAscend ×5 是灾厄的加强，不回退），这里除以同一系数还原。
        /// </summary>
        public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            if (ConfigSystem.Instance?.RevertVanillaNerfs != true || !ModLoader.HasMod("CalamityMod"))
                return;
            if (item.type == ItemID.ButterflyWings)
                maxAscentMultiplier /= 0.6667f;
            else if (item.type == ItemID.GhostWings)
                maxAscentMultiplier /= 0.6625f;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 17 级传奇武器名称颜色：按具体武器返回其专属颜色（本模组自有的传奇武器）。
        /// </summary>
        private static Color GetLegendaryWeaponColor(int itemType)
        {
            if (itemType == ModContent.ItemType<DefenseBlade>())
                return new Color(255, Main.DiscoG, 53);
            return default;
        }
        /// <summary>
        /// 按物品类型回退灾厄的字段削弱：把伤害/攻速/弹速/防御/斧力/价值恢复为原版值。
        /// </summary>
        private static void RevertVanillaNerf(Item item)
        {
            if (VanillaDamage.TryGetValue(item.type, out int damage))
                item.damage = damage;
            if (VanillaUseTime.TryGetValue(item.type, out int useTime))
            {
                item.useTime = useTime;
                item.useAnimation = useTime;
            }
            if (VanillaShootSpeed.TryGetValue(item.type, out float shootSpeed))
                item.shootSpeed = shootSpeed;
            if (VanillaDefense.TryGetValue(item.type, out int defense))
                item.defense = defense;
            if (VanillaAxe.TryGetValue(item.type, out int axe))
                item.axe = axe;
            if (VanillaPick.TryGetValue(item.type, out int pick))
                item.pick = pick;
            if (VanillaHammer.TryGetValue(item.type, out int hammer))
                item.hammer = hammer;
            if (VanillaValue.TryGetValue(item.type, out int value))
                item.value = value;
        }
    }
    /// <summary>
    /// 全局增益类：回退现代版灾厄（CalamityMod）对原版增益 buff 的数值削弱。
    /// 灾厄在 Buffs/CalamityGlobalBuff.cs 的 Update 里对部分原版 buff 做了削弱，这里反向抵消恢复原版效果。
    /// </summary>
    internal class CalamityDemutationGlobalBuff : GlobalBuff
    {
        /// <summary>
        /// tModLoader 的 GlobalBuff.Update 钩子：每帧对持有该增益的玩家调用，type 为增益 ID。
        /// 仅当开启"回退原版削弱"配置且安装现代版灾厄（CalamityMod）时生效，否则直接返回。
        /// 按 type 分支，用 +X 精确抵消灾厄 CalamityGlobalBuff 对原版增益的削弱——
        /// 箭术、魔法力量、洞察、糖冲刺、挖矿、迅捷、微醺、饱食/熟食等，
        /// 逐条恢复原版的伤害/暴击/攻速/移速/挖速加成。经典版灾厄不在处理范围。
        /// </summary>
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (ConfigSystem.Instance?.RevertVanillaNerfs != true || !ModLoader.HasMod("CalamityMod"))
                return;
            switch (type)
            {
                case BuffID.Archery:
                    player.arrowDamage /= 0.955f;
                    break;
                case BuffID.MagicPower:
                    player.GetDamage<MagicDamageClass>() += 0.1f;
                    break;
                case BuffID.SugarRush:
                    player.moveSpeed += 0.1f;
                    player.pickSpeed -= 0.1f;
                    break;
                case BuffID.Mining:
                    player.pickSpeed -= 0.1f;
                    break;
                case BuffID.Swiftness:
                    player.moveSpeed += 0.1f;
                    break;
                case BuffID.Tipsy:
                    player.GetCritChance<MeleeDamageClass>() += 2;
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.2f;
                    player.GetDamage<MeleeDamageClass>() += 0.1f;
                    break;
                case BuffID.WellFed:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.05f;
                    player.moveSpeed += 0.15f;
                    break;
                case BuffID.WellFed2:
                    player.pickSpeed -= 0.025f;
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.075f;
                    player.moveSpeed += 0.225f;
                    break;
                case BuffID.WellFed3:
                    player.pickSpeed -= 0.05f;
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.1f;
                    player.moveSpeed += 0.3f;
                    break;
                case BuffID.Werewolf:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.051f;
                    break;
                // 星瓶（StarInBottle）不处理：灾厄把它的原版加成整体换成「提供法力再生药水效果」
                // （CalamityGlobalBuff.cs:89-95：置 manaRegenBuff = true 并 -0.5/-10），
                // 属"重做/加强"而非削弱 —— 按用户口径「对原版加强就不动」。
                case BuffID.Rabies:
                    player.GetDamage<GenericDamageClass>() += 0.2f;
                    break;
                case BuffID.NebulaUpDmg1:
                    player.GetDamage<MagicDamageClass>() += 0.075f;
                    break;
                case BuffID.NebulaUpDmg2:
                    player.GetDamage<MagicDamageClass>() += 0.15f;
                    break;
                case BuffID.NebulaUpDmg3:
                    player.GetDamage<MagicDamageClass>() += 0.225f;
                    break;
                case BuffID.NebulaUpLife1:
                    player.lifeRegen += 2;
                    break;
                case BuffID.NebulaUpLife2:
                    player.lifeRegen += 4;
                    break;
                case BuffID.NebulaUpLife3:
                    player.lifeRegen += 6;
                    break;
            }
            // 甲虫攻击（Beetle Might）：灾厄把每级近战攻速从 10% 减到 5%，这里补回
            if (type >= BuffID.BeetleMight1 && type <= BuffID.BeetleMight3 && player.beetleOffense)
            {
                int orbs = player.beetleOrbs < 0 ? 0 : player.beetleOrbs;
                if (orbs > 3)
                    orbs = 3;
                player.GetAttackSpeed<MeleeDamageClass>() += 0.05f * orbs;
            }
            // 甲虫耐力（Beetle Shell）：灾厄把原版的「每球 15% 乘算减伤」整条移除，改成
            // 「每球 10% 加算」（CalamityGlobalBuff.cs:98-104 + BalancingConstants.BeetleShellDRPerBeetle = 0.1f）。
            // 单纯补 5%/球虽然能凑出 15/30/45 的数字，但加算 ≠ 乘算：玩家另有减伤时会超补
            // （例：其他减伤 30% + 3 球 → 加算 75% vs 原版乘算 61.5%）。
            // 这里按乘算等效还原：目标 = 1 - (1 - 其他减伤) × (1 - 15% × 球数)。
            // 依赖「灾厄先加载先跑」：本模组弱引用 CalamityMod，故此刻 player.endurance 里已含灾厄那 10%/球。
            else if (type >= BuffID.BeetleEndurance1 && type <= BuffID.BeetleEndurance3 && player.beetleDefense)
            {
                int orbs = player.beetleOrbs < 0 ? 0 : player.beetleOrbs;
                if (orbs > 3)
                    orbs = 3;
                if (orbs > 0)
                {
                    float other = player.endurance - 0.1f * orbs;   // 扣掉灾厄那 10%/球 的加算部分
                    if (other < 0f)
                        other = 0f;
                    float target = 1f - (1f - other) * (1f - 0.15f * orbs);
                    player.endurance += target - player.endurance;
                }
            }
        }
    }
}
