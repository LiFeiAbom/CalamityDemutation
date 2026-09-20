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
using CalamityDemutation.Content.Items.Accessories.StatLife;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
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
            { ItemID.AcornAxe, 30 },
            { ItemID.SawtoothShark, 14 },
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
        };
        /// <summary>
        /// 原版攻速（ItemID -> 原版 useTime）。灾厄用 UseExact 同时调高了 useTime 与 useAnimation。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaUseTime = new()
        {
            { ItemID.BeesKnees, 23 },
            { ItemID.ChristmasTreeSword, 23 },
            { ItemID.CoinGun, 8 },
            { ItemID.DayBreak, 16 },
            { ItemID.Handgun, 15 },
            { ItemID.IceBoomerang, 20 },
            { ItemID.InfluxWaver, 20 },
            { ItemID.MoltenFury, 22 },
            { ItemID.PhoenixBlaster, 14 },
            { ItemID.Sandgun, 16 },
            { ItemID.StarCannon, 12 },
        };
        /// <summary>
        /// 原版价值（ItemID -> 原版 value；数值用 Item.sellPrice 按"售价"口径书写，便于与原版表对照）。
        /// 灾厄用 Worthless 把这些物品的价值归零。
        /// </summary>
        private static readonly Dictionary<int, int> VanillaValue = new()
        {
            { ItemID.Mushroom, Item.sellPrice(copper: 5) },
            { ItemID.GlowingMushroom, Item.sellPrice(copper: 10) },
            { ItemID.VileMushroom, Item.sellPrice(silver: 2, copper: 50) },
            { ItemID.ViciousMushroom, Item.sellPrice(silver: 2, copper: 50) },
            { ItemID.EncumberingStone, Item.sellPrice(gold: 1) },
            { ItemID.UncumberingStone, Item.sellPrice(gold: 1) },
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
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                if (calamity0.TryFind<ModItem>("PerforatorBag", out ModItem perforatorBag) && item.type == perforatorBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodyWormTooth>(), 1));
                }
                if(calamity0.TryFind<ModItem>("RavagerBag", out ModItem ravagerBag) && item.type == ravagerBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodPact>(), 1));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FleshTotem>(), 2));
                    if (BossSystem.Providence)
                    {
                        itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodflareCore>(), 1));
                    }
                }
                if (calamity0.TryFind<ModItem>("DevourerofGodsBag", out ModItem devourerofGodsBag) && item.type == devourerofGodsBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<NebulousCore>(), 1));
                    // 虚无双子系（CE 的 NihilityTwinBag，档位=月后，故挂到神明吞噬者）：无星之夜 1/5 概率一次 3 把，虚无碎片必掉 32~40
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<StarlessNight>(), 5, 1, 1, 3));
                    itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NihilityFragments>(), 1, 32, 40));
                    // 宙宇波能刃（灾厄原版 Excelsus 即神吞袋武器池掉落，本模组移植后按 1/3 概率挂回神吞袋）
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Excelsus>(), 3));
                }
                if (calamity0.TryFind<ModItem>("PolterghastBag", out ModItem polterghastBag) && item.type == polterghastBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Affliction>(), 1));
                }
                if (calamity0.TryFind<ModItem>("LeviathanBag", out ModItem leviathanLureBag) && item.type == leviathanLureBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LureofEnthrallment>(), 3));
                }
                if (calamity0.TryFind<ModItem>("BrimstoneElementalBag", out ModItem brimstoneElementalBag) && item.type == brimstoneElementalBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RoseStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Gehenna>(), 1));
                }
                if ((calamity0.TryFind<ModItem>("AquaticScourgeBag", out ModItem aquaticScourgeBag) && item.type == aquaticScourgeBag.Type) || (calamity0.TryFind<ModItem>("DesertScourgeBag", out ModItem desertScourgeBag) && item.type == desertScourgeBag.Type))
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<AeroStone>(), 10));
                }
                if (calamity0.TryFind<ModItem>("CryogenBag", out ModItem cryogenBag) && item.type == cryogenBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                }
                if (calamity0.TryFind<ModItem>("CalamitasCloneBag", out ModItem calamitasCloneBag) && item.type == calamitasCloneBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CalamityRing>(), 1));
                    // 先知系（CE 的 ProphetBag，血肉量≈灾厄之影）：符文之歌 1/5 概率一次 3 把
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RuneSong>(), 5, 1, 1, 3));
                }
                if (calamity0.TryFind<ModItem>("PlaguebringerGoliathBag", out ModItem plaguebringerGoliathBag) && item.type == plaguebringerGoliathBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                }
                if(calamity0.TryFind<ModItem>("HiveMindBag", out ModItem hiveMindBag) && item.type == hiveMindBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RottenBrain>(), 1));
                }
                if(calamity0.TryFind<ModItem>("CrabulonBag", out ModItem crabulonBag) && item.type == crabulonBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FungalClump>(), 1));
                }
                if (calamity0.TryFind<ModItem>("LeviathanBag", out ModItem leviathanBag) && item.type == leviathanBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LeviathanAmbergris>(), 1));
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<TheCommunity>(), 100));
                }
                if(item.type == ItemID.GolemBossBag)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DefenseBlade>(), 100));
                }
                if (calamity0.TryFind<ModItem>("YharonBag", out ModItem yharonBag) && item.type == yharonBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<DrewsWings>(), 1));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DragonRage>(), 3));
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("PerforatorBag", out ModItem perforatorBag) && item.type == perforatorBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodyWormTooth>(), 1));
                }
                if (calamity1.TryFind<ModItem>("RavagerBag", out ModItem ravagerBag) && item.type == ravagerBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodPact>(), 2));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FleshTotem>(), 2));
                    if (BossSystem.Providence)
                    {
                        itemLoot.Add(new CommonDrop(ModContent.ItemType<BloodflareCore>(), 1));
                    }
                }
                if (calamity1.TryFind<ModItem>("DevourerofGodsBag", out ModItem devourerofGodsBag) && item.type == devourerofGodsBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<NebulousCore>(), 1));
                    // 虚无双子系（CE 的 NihilityTwinBag，档位=月后，故挂到神明吞噬者）：无星之夜 1/5 概率一次 3 把，虚无碎片必掉 32~40
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<StarlessNight>(), 5, 1, 1, 3));
                    itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NihilityFragments>(), 1, 32, 40));
                    // 宙宇波能刃（灾厄原版 Excelsus 即神吞袋武器池掉落，本模组移植后按 1/3 概率挂回神吞袋）
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Excelsus>(), 3));
                }
                if (calamity1.TryFind<ModItem>("PolterghastBag", out ModItem polterghastBag) && item.type == polterghastBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Affliction>(), 1));
                }
                if (calamity1.TryFind<ModItem>("LeviathanBag", out ModItem leviathanLureBag) && item.type == leviathanLureBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LureofEnthrallment>(), 3));
                }
                if(calamity1.TryFind<ModItem>("BrimstoneWaifuBag", out ModItem brimstoneWaifuBag) && item.type == brimstoneWaifuBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RoseStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<Gehenna>(), 1));
                }
                if((calamity1.TryFind<ModItem>("AquaticScourgeBag", out ModItem aquaticScourgeBag) && item.type == aquaticScourgeBag.Type) || (calamity1.TryFind<ModItem>("DesertScourgeBag", out ModItem desertScourgeBag) && item.type == desertScourgeBag.Type))
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<AeroStone>(), 10));
                }
                if (calamity1.TryFind<ModItem>("CryogenBag", out ModItem cryogenBag) && item.type == cryogenBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                }
                if (calamity1.TryFind<ModItem>("CalamitasBag", out ModItem calamitasBag) && item.type == calamitasBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<CalamityRing>(), 1));
                    // 先知系（CE 的 ProphetBag，血肉量≈灾厄之影）：符文之歌 1/5 概率一次 3 把
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RuneSong>(), 5, 1, 1, 3));
                }
                if (calamity1.TryFind<ModItem>("PlaguebringerGoliathBag", out ModItem plaguebringerGoliathBag) && item.type == plaguebringerGoliathBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                }
                if (calamity1.TryFind<ModItem>("HiveMindBag", out ModItem hiveMindBag) && item.type == hiveMindBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<RottenBrain>(), 1));
                }
                if (calamity1.TryFind<ModItem>("CrabulonBag", out ModItem crabulonBag) && item.type == crabulonBag.Type)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<FungalClump>(), 1));
                }
                if (calamity1.TryFind<ModItem>("LeviathanBag", out ModItem leviathanBag) && item.type == leviathanBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<LeviathanAmbergris>(), 1));
                }
                if (item.type == ItemID.GolemBossBag)
                {
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DefenseBlade>(), 100));
                }
                if (calamity1.TryFind<ModItem>("YharonBag", out ModItem yharonsBag) && item.type == yharonsBag.Type)
                {
                    itemLoot.Add(ItemDropRule.ByCondition(new Conditions.IsHardmode(), ModContent.ItemType<DrewsWings>(), 1));
                    itemLoot.Add(new CommonDrop(ModContent.ItemType<DragonRage>(), 3));
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
                case ItemID.FeralClaws:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                    break;
                case ItemID.PowerGlove:
                case ItemID.BerserkerGlove:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                    break;
                case ItemID.MechanicalGlove:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
                    break;
                case ItemID.FireGauntlet:
                    player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
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
        /// 回退灾厄对原版护甲套装的削弱（WizardHat 魔法暴击、MagicHat 最大法力）。
        /// 注：SolarFlare 的 12% 减伤移除属 IL 编辑的 DR 重做，在此不处理（见 ILEditing）。
        /// </summary>
        public override void UpdateArmorSet(Player player, string set)
        {
            if (ConfigSystem.Instance?.RevertVanillaNerfs != true || !ModLoader.HasMod("CalamityMod"))
                return;
            if (set == "WizardHat")
                player.GetCritChance<MagicDamageClass>() += 6;
            else if (set == "MagicHat")
                player.statManaMax2 += 20;
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
                case BuffID.Clairvoyance:
                    player.GetDamage<MagicDamageClass>() += 0.02f;
                    player.GetCritChance<MagicDamageClass>() += 2;
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
                case BuffID.StarInBottle:
                    if (!player.manaRegenBuff)
                    {
                        player.manaRegenDelayBonus += 0.5f;
                        player.manaRegenBonus += 10;
                    }
                    break;
                case BuffID.Rabies:
                    player.GetDamage<GenericDamageClass>() += 0.2f;
                    break;
                case BuffID.Sharpened:
                    player.GetArmorPenetration<MeleeDamageClass>() += 7;
                    break;
                case BuffID.Panic:
                    player.moveSpeed += 0.6f;
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
            // 甲虫耐力（Beetle Shell）：灾厄把每级 multiplicative 减伤改成 10% 加算，这里补回 5% 近似原版 15%
            else if (type >= BuffID.BeetleEndurance1 && type <= BuffID.BeetleEndurance3 && player.beetleDefense)
            {
                int orbs = player.beetleOrbs < 0 ? 0 : player.beetleOrbs;
                if (orbs > 3)
                    orbs = 3;
                player.endurance += 0.05f * orbs;
            }
        }
    }
}
