using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
namespace CalamityDemutation.Content.Items.Accessories.Wings
{
    /// <summary>
    /// 炽天使翼靴（Seraph Tracers） - 本工程移速饰品链的顶层（第四级）。
    /// **名字口径（用户 2026-10-14 指定）**：中文名沿用「炽天使翼靴」（= 源显示名 Seraph Tracers；
    /// 靴子模式的 `TreadsName` 作「炽天使履带」）。这个名字在实装 2.2.2 里属于顶层那件——
    /// 本工程自持件与它**同名不同物**，属用户既定的"同名并存"口径（同太阳神杖那件）。
    /// **移植口径（同前三件）**：功能取 旧版灾厄、配方取 2.0、再按复合原则并入 1.4.4
    /// （= 实装 2.2.2 的 `SeraphTracers`）的增强。逐项对照：
    /// ① 旧版灾厄 `CelestialTracers` = 飞行 250 / 水平速度 12 / 倍率 3.0、跑速上限 10、移速 +24%、
    /// 冰面·水面·岩浆面行走、岩浆 240 帧、免「着火！」、免坠落伤害、
    /// 垂直速度 1.0/0.175/1.2/3.25/0.15、飞行粉尘 91（= `DustID.GemDiamond`）、稀有度 15 档（Violet）、
    /// 价值 2 铂金 40 金，**单次受伤 >200 点 → 额外 30 帧无敌**（源里的 `cTracers` 标记，
    /// 结算在 CalamityDemutationPlayer.PostHurt），配方 = 极乐履带 + 德鲁之翼 + 金源锭×5 @ 宇宙砧；
    /// ② 2.0 `CelestialTracers` = 同一套（`WingStats(250, 12, 3)` 等价写法）；
    /// ③ 2.0.4 `TracersSeraph` = 飞行 250 / 水平 11 / 倍率 2.8、跑速上限 9、移速 +18%、完全免岩浆、
    /// 垂直速度 0.95/0.16/1.2/2.9/0.145；
    /// ④ 1.4.4 `SeraphTracers` = 同上那套收过的值，另加 **可切换翅膀**（Shift+右键，靴子模式给火箭靴 2 与
    /// `angelTreads` 标记 = +10% 飞行时间）、稀有度升到 `BurnishedAuric`、
    /// 新增 `seraphTracers` 标记（免金源矿石的 Auric Rejection 与危险砖块接触效果），
    /// 同时**去掉**了"受伤 >200 免伤"那条。
    /// **复合结果**：飞行 250 / 水平速度 12（取大）/ 倍率 3.0（取大）/ 跑速上限 10（取大）/
    /// 移速 +24%（取大）/ 完全免岩浆（取强）/ 免「着火！」/ 免坠落伤害 /
    /// 垂直速度 1.0 / 0.175 / 1.2 / 3.25 / 0.15（取大）/ 价值 2 铂金 40 金、稀有度取高到 20 档
    /// （= 灾厄 `BurnishedAuric`，本工程金源套同档；源 旧版灾厄/2.0/2.0.4 是 15 档 Violet）/
    /// **受伤 >200 额外 30 帧无敌**（旧版灾厄/2.0 的 `cTracers`，本工程自持实现）/
    /// **免金源矿石排斥与危险砖块接触**（1.4.4 的 `seraphTracers`，镜像进灾厄侧同名字段）/
    /// **可切换翅膀**（1.4.4；靴子模式给火箭靴 2）。
    /// 未并进来的灾厄内部视觉通道（纯外观，按 §5 口径不硬塞）：`IBoots` / `elysianFire` / `cTracers`
    /// 在 CalamityPlayerDrawEffects 里的拖尾与火焰绘制、`tracersDust`、以及物品栏模式指示点——
    /// 本件自己的飞行粉尘已照源实现。
    /// 面板：尺寸 54×42；贴图取**旧图**（旧版灾厄/2.0/2.0.4 的物品图同为 54×42 的同一张 `0B0DD705`；
    /// 1.4.4 起换成另一张 54×42 的 `6F3EBEA5`）。
    /// **翅膀贴图按用户 2026-10-14 指定换成「德鲁之翼」那张**（本工程的 `DrewsWings_Wings.png`，86×248；
    /// 源自己的 `_Wings.png` 是另一张 86×248 的图）。
    /// 配方（双版本各注册一条，缺件即退化成单分支）：**现代**照 2.0 = 极乐翼靴 + 德鲁之翼 +
    /// 金源锭×5 @ 宇宙砧；**经典**照 经典灾厄 = 极乐翼靴 + 德鲁之翼 + 暗日碎片×5 + 金源矿石×25 @ 嘉登熔炉。
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    internal class SeraphTracers:ModItem
    {
        /// <summary>本类型被 AutoloadEquip 分配的翅膀槽位编号；关掉翅膀模式时把实例的 Item.wingSlot 置 −1</summary>
        private static int wingSlot;
        /// <summary>
        /// 翅膀模式开关：存在**物品实例**上（不是 ModItem 字段，联机时随物品同步），
        /// 靠 `Item.wingSlot` 是否为 −1 表示；写法照 1.4.4 的 SeraphTracers
        /// </summary>
        private bool toggleEnabled
        {
            get { return Item.wingSlot != -1; }
            set { Item.wingSlot = value ? wingSlot : -1; }
        }
        /// <summary>
        /// 注册翅膀数据：飞行时间 250（各版一致）、水平速度 12（旧版灾厄/2.0 的值，2.0.4/1.4.4 是 11）、
        /// 水平加速度倍率 3.0（旧版灾厄/2.0 的值，2.0.4/1.4.4 是 2.8）；
        /// 并记下本类型的 wingSlot 供模式开关用
        /// </summary>
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(250, 12f, 3f);
            wingSlot = Item.wingSlot;
        }
        /// <summary>
        /// 物品基础属性：54×42 贴图、价值 2 铂金 40 金、月后自定义稀有度 20（金源档）、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 54;                          // 贴图宽（像素）
            Item.height = 42;                         // 贴图高（像素）
            Item.value = Item.buyPrice(2, 40, 0, 0);  // 价值 2 铂金 40 金（照源 Rarity15BuyPrice / RarityVioletBuyPrice）
            Item.accessory = true;                    // 作为饰品装备
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;  // 月后稀有度 20（= 源的 BurnishedAuric / 本工程金源档）
        }
        /// <summary>
        /// 装备时：先画飞行粉尘（照 旧版灾厄；旧版灾厄/2.0 写 91，等于 `DustID.GemDiamond`），
        /// 再设置跑速/移速/三种行走免疫/免岩浆/免「着火！」/免坠落伤害与两项标记，
        /// 最后处理**靴子模式**——翅膀被关掉时给火箭靴 2 与 angelTreads 标记（+10% 飞行时间），
        /// 照 1.4.4 的 `if (!toggleEnabled)` 分支
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.controlJump && player.wingTime > 0f && player.jump == 0 && player.velocity.Y != 0f && !hideVisual && toggleEnabled)
            {
                int dustXOffset = 4;
                if (player.direction == 1)
                {
                    dustXOffset = -40;
                }
                int flightDust = Dust.NewDust(new Vector2(player.position.X + (float)(player.width / 2) + (float)dustXOffset, player.position.Y + (float)(player.height / 2) - 15f), 30, 30, DustID.GemDiamond, 0f, 0f, 100, default, 2.4f);
                Main.dust[flightDust].noGravity = true;      // 不受重力
                Main.dust[flightDust].velocity *= 0.3f;      // 减缓漂移
                if (Main.rand.NextBool(10))
                {
                    Main.dust[flightDust].fadeIn = 2f;       // 1/10 概率淡入
                }
                Main.dust[flightDust].shader = GameShaders.Armor.GetSecondaryShader(player.cWings, player);  // 随翅膀染色
            }
            player.accRunSpeed = 10f;         // 跑速上限（旧版灾厄/2.0 的 10；1.4.4 是 9，复合取大）
            player.moveSpeed += 0.24f;        // 移速 +24%（旧版灾厄/2.0 的值；1.4.4 是 +18%，复合取大）
            player.iceSkate = true;           // 冰面机动性
            player.waterWalk = true;          // 水面行走
            player.fireWalk = true;           // 岩浆面行走
            player.lavaImmune = true;         // 完全免岩浆（2.0.4/1.4.4；旧版灾厄/2.0 只有 240 帧，复合取强）
            player.buffImmune[BuffID.OnFire] = true;   // 免疫「着火了！」
            player.noFallDmg = true;          // 免坠落伤害
            player.GetModPlayer<CalamityDemutationPlayer>().seraphTracers = true;  // 免金源矿石排斥 / 危险砖块接触（镜像进灾厄侧）
            if (!toggleEnabled)               // 靴子模式：火箭靴 2 + 天使之靴的 +10% 飞行时间（照 1.4.4）
            {
                player.rocketBoots = player.vanityRocketBoots = 2;
                player.GetModPlayer<CalamityDemutationPlayer>().angelTreads = true;
            }
        }
        /// <summary>
        /// 垂直飞行参数：复合取大 = 旧版灾厄/2.0 那套（1.0 / 0.175 / 1.2 / 3.25 / 0.15；
        /// 1.4.4 是 0.95 / 0.16 / 1.2 / 2.9 / 0.145），行尾为原版默认值
        /// </summary>
        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 1f; //0.85
            ascentWhenRising = 0.175f; //0.15
            maxCanAscendMultiplier = 1.2f; //1
            maxAscentMultiplier = 3.25f; //3
            constantAscend = 0.15f; //0.135
        }
        /// <summary>
        /// 靴子模式（翅膀关掉）时去掉第一行「可以飞行并缓降」——照 1.4.4 的 SeraphTracers
        /// </summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!toggleEnabled)
                tooltips.RemoveAll(x => x.Name == "Tooltip0");
        }
        /// <summary>
        /// 切换开关：Shift+右键（照 1.4.4）；**不消耗物品**
        /// </summary>
        public override bool CanRightClick() => Main.keyState.PressingShift();
        public override void RightClick(Player player)
        {
            toggleEnabled = !toggleEnabled;
            Item.NetStateChanged();
        }
        public override bool ConsumeItem(Player player) => false;
        /// <summary>
        /// 模式存在物品实例上，需随存档与联机同步（四件套照 1.4.4 的 SeraphTracers）
        /// </summary>
        public override void SaveData(TagCompound tag)
        {
            tag.Add("toggleEffect", toggleEnabled);
        }
        public override void LoadData(TagCompound tag)
        {
            toggleEnabled = tag.GetBool("toggleEffect");
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(toggleEnabled);
        }
        public override void NetReceive(BinaryReader reader)
        {
            toggleEnabled = reader.ReadBoolean();
        }
        /// <summary>
        /// 模式不同名字不同：翅膀模式显示「炽天使翼靴」，靴子模式显示「炽天使履带」
        /// （照 1.4.4 用本地化键切名字；灾厄那枚物品栏模式指示点未移植）
        /// </summary>
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Item.SetNameOverride(Language.GetTextValue($"Mods.CalamityDemutation.Items.SeraphTracers.{(toggleEnabled ? "DisplayName" : "TreadsName")}"));
        }
        /// <summary>
        /// 配方（分版本，两分支各自 TryGetMod + TryFind 串联，缺材料时静默跳过该条）：
        /// 现代分支（灾厄本体在场）照灾厄 2.0；经典分支（经典灾厄在场）
        /// 照经典灾厄经典灾厄（多两味暗日碎片/金源矿石，站台换嘉登熔炉）。
        /// 两分支的前件都用**本工程自持件**：极乐翼靴（本模组）与德鲁之翼（德鲁之翼在实装 2.2.2 里已被删，
        /// 本模组自持，见 DrewsWings.cs）。
        /// </summary>
        public override void AddRecipes()
        {
            // 现代灾厄：极乐翼靴 + 德鲁之翼 + 金源锭×5 @ 宇宙砧
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<ElysianTracers>();
                    recipe.AddIngredient<DrewsWings>();
                    recipe.AddIngredient(auricBar.Type, 5);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            // 经典灾厄：极乐翼靴 + 德鲁之翼 + 暗日碎片×5 + 金源矿石×25 @ 嘉登熔炉
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if(classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && classic.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<ElysianTracers>();
                    recipeClassic.AddIngredient<DrewsWings>();
                    recipeClassic.AddIngredient(darksunFragment.Type, 5);
                    recipeClassic.AddIngredient(auricOre.Type, 25);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
