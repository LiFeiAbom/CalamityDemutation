using CalamityDemutation.Content.Items.Accessories.Movement;
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
    /// 天界翼靴（Infinity Boots） - 天使之靴的上一档（本工程链条第二级）。
    /// **名字口径（用户 2026-10-14 指定）**：中文名取「天界翼靴」（当天先定「无限之靴」，随即改为现名；
    /// 英文名保持 `Infinity Boots` = 照源类名）——旧世系里这一档的显示名其实就叫
    /// Seraph Tracers（类名 `InfinityBoots`：1.3-release 源码里是 `DisplayName.SetDefault("Seraph Tracers")`，
    /// 经典版 cal-1.4.2.101 的本地化也是 `DisplayName: Seraph Tracers`）；到 2.0.3.9 该名字被顶层那件借走，
    /// 2.2.2 里归顶层 `SeraphTracers` 所有，故本件另起中文名，不与本体顶层同名。
    /// **移植口径（用户 2026-10-14 指定）：功能取 1.3-release、配方取 2.0 那一批、再按复合原则并入
    /// 1.4.4 世系（= 实装 2.2.2 的 MoonWalkers）的增强。** 逐项对照：
    /// ① 1.3-release 底子 = 飞行 140 / 水平速度 9 / 加速倍率 2.5、跑速上限 8.5、移速 +16%、
    /// 冰面·水面·岩浆面行走、岩浆 240 帧、免「着火！」、免坠落伤害、飞行粉尘（不写 `WingStats`，
    /// 而是 `wingTimeMax = 140` + `HorizontalWingSpeeds(9, ×2.5)` + `VerticalWingSpeeds`）；
    /// ② 2.0 = 同一套，只把加速倍率写成 2.6（配方里 `BarofLife` → `LifeAlloy`）；
    /// ③ 2.0.4 = 跑速上限 8、移速 +14%、改成完全免岩浆（配方去掉灾厄核心与生命合金）；
    /// ④ 1.4.4 = 飞行时间 **160**、跑速上限 8、移速 +14%、完全免岩浆、
    /// **可切换翅膀**（Shift+右键；切到靴子模式时给火箭靴 4 与 `angelTreads` 标记 = +10% 飞行时间）、
    /// 物品图换成双帧。
    /// **复合结果**：飞行 160（取大）/ 水平速度 9 / 加速倍率 2.6（取大）/ 跑速上限 8.5（取大）/
    /// 移速 +16%（取大）/ 完全免岩浆（取强）/ 免「着火！」/ 免坠落伤害 / 火箭靴 4（1.4.4）/
    /// 可切换翅膀（1.4.4）。
    /// 未并进来的两项都是**灾厄内部视觉通道**，本工程无对应机制、属纯外观，按 §5 口径不硬塞：
    /// ① 1.3-release/2.0 挂的 `CalamityPlayer.IBoots`/`sTracers`（在 CalamityPlayerDrawEffects 里
    /// 画移动拖尾尘埃 229）——本件自己的飞行粉尘已照源实现；
    /// ② `CalamityUtils.DrawInventoryDot` 的模式点与双帧贴图切换（本件用旧单帧图）。
    /// 面板：尺寸 48×36、价值 1 铂金（照源 `Rarity10BuyPrice`）、稀有度红；
    /// 贴图取 1.3-release 的旧图（cal-1.4.2.101 到 2.0.7.2 这一档用的是同一张 48×36 图 +
    /// 同一张 `_Shoes.png` + 同一张 80×248 翅膀图，2.2.2 的月行者才换新图）。
    /// 配方（双版本各注册一条，缺件即退化成单分支）：**现代**照 2.0 = 天使之靴 + 任意翅膀 +
    /// 灾厄核心×3 + 生命合金×5 + 月锭×5 @ 远古操纵机；**经典**照 cal-1.4.2.101 = 天使之靴 + 任意翅膀 +
    /// 灾厄核心×3 + 生命之锭×5 + 月锭×5 @ 远古操纵机。
    /// </summary>
    [AutoloadEquip(EquipType.Wings, EquipType.Shoes)]
    internal class InfinityBoots:ModItem
    {
        /// <summary>本类型被 AutoloadEquip 分配的翅膀槽位编号；关掉翅膀模式时把实例的 Item.wingSlot 置 −1</summary>
        private static int wingSlot;
        /// <summary>
        /// 翅膀模式开关：存在**物品实例**上（不是 ModItem 字段，联机时随物品同步），
        /// 靠 `Item.wingSlot` 是否为 −1 表示；写法照 1.4.4 的 MoonWalkers
        /// </summary>
        private bool toggleEnabled
        {
            get { return Item.wingSlot != -1; }
            set { Item.wingSlot = value ? wingSlot : -1; }
        }
        /// <summary>
        /// 注册翅膀数据：飞行时间 160（复合取 1.4.4 的值；1.3-release 是 140）、水平速度 9、
        /// 水平加速度倍率 2.6（1.3-release 是 2.5）；并记下本类型的 wingSlot 供模式开关用
        /// </summary>
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(160, 9f, 2.6f);
            wingSlot = Item.wingSlot;
        }
        /// <summary>
        /// 物品基础属性：48×36 贴图、价值 1 铂金、稀有度红、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 48;                          // 贴图宽（像素）
            Item.height = 36;                         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 0, 0, 0);   // 价值 1 铂金（照源 Rarity10BuyPrice）
            Item.rare = ItemRarityID.Red;             // 稀有度：红
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时：先画飞行粉尘（照 1.3-release；2.0/2.0.4/1.4.4 用的是同一颗 DustID.TerraBlade），
        /// 再设置跑速/移速/三种行走免疫/免岩浆/免「着火！」/免坠落伤害；
        /// 最后处理**靴子模式**——翅膀被关掉时给火箭靴 4 与 angelTreads 标记（+10% 飞行时间），
        /// 照 1.4.4 的 `if (!toggleEnabled)` 分支（翅膀模式下不给，免得翅膀吃自己的飞行加成）
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
                int flightDust = Dust.NewDust(new Vector2(player.position.X + (float)(player.width / 2) + (float)dustXOffset, player.position.Y + (float)(player.height / 2) - 15f), 30, 30, DustID.TerraBlade, 0f, 0f, 100, default, 2.4f);
                Main.dust[flightDust].noGravity = true;      // 不受重力
                Main.dust[flightDust].velocity *= 0.3f;      // 减缓漂移
                if (Main.rand.NextBool(10))
                {
                    Main.dust[flightDust].fadeIn = 2f;       // 1/10 概率淡入
                }
                Main.dust[flightDust].shader = GameShaders.Armor.GetSecondaryShader(player.cWings, player);  // 随翅膀染色
            }
            player.accRunSpeed = 8.5f;        // 跑速上限（1.3-release/2.0 的 8.5；1.4.4 是 8，复合取大）
            player.moveSpeed += 0.16f;        // 移速 +16%（1.3-release/2.0 的值；1.4.4 是 +14%，复合取大）
            player.iceSkate = true;           // 冰面机动性
            player.waterWalk = true;          // 水面行走
            player.fireWalk = true;           // 岩浆面行走
            player.lavaImmune = true;         // 完全免岩浆（1.4.4；1.3-release/2.0 只有 240 帧，复合取强）
            player.buffImmune[BuffID.OnFire] = true;  // 免疫「着火了！」
            player.noFallDmg = true;          // 免坠落伤害
            if (!toggleEnabled)               // 靴子模式：火箭靴 4 + 天使之靴的 +10% 飞行时间（照 1.4.4）
            {
                player.rocketBoots = player.vanityRocketBoots = 4;
                player.GetModPlayer<CalamityDemutationPlayer>().angelTreads = true;
            }
        }
        /// <summary>
        /// 垂直飞行参数：与 1.3-release、1.4.4 逐值一致（行尾为原版默认值）
        /// </summary>
        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.75f; //0.85
            ascentWhenRising = 0.15f; //0.15
            maxCanAscendMultiplier = 1f; //1
            maxAscentMultiplier = 2.5f; //3
            constantAscend = 0.125f; //0.135
        }
        /// <summary>
        /// 靴子模式（翅膀关掉）时去掉第一行「可以飞行并缓降」——照 1.4.4 的 MoonWalkers
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
        /// 模式存在物品实例上，需随存档与联机同步（四件套照 1.4.4 的 MoonWalkers）
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
        /// 模式不同名字不同：翅膀模式显示「天界翼靴」，靴子模式显示「天界履带」
        /// （照 1.4.4 的 MoonWalkers 用本地化键切名字；灾厄那枚物品栏模式指示点未移植）
        /// </summary>
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Item.SetNameOverride(Language.GetTextValue($"Mods.CalamityDemutation.Items.InfinityBoots.{(toggleEnabled ? "DisplayName" : "TreadsName")}"));
        }
        /// <summary>
        /// 配方（分版本，两分支各自 TryGetMod + TryFind 串联，缺材料时静默跳过该条）：
        /// 现代分支（CalamityMod 在场）照灾厄 2.0；经典分支（CalamityModClassicPreTrailer 在场）照经典世系
        /// cal-1.4.2.101（只是把生命合金换回生命之锭）。
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：天使之靴 + 任意翅膀 + 灾厄核心×3 + 生命合金×5 + 月锭×5 @ 远古操纵机
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity)
                    && calamity.TryFind<ModItem>("LifeAlloy", out ModItem lifeAlloy))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<AngelTreads>();
                    AddAnyWings(recipe);
                    recipe.AddIngredient(coreofCalamity.Type, 3);
                    recipe.AddIngredient(lifeAlloy.Type, 5);
                    recipe.AddIngredient(ItemID.LunarBar, 5);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            // 经典版灾厄：天使之靴 + 任意翅膀 + 灾厄核心×3 + 生命之锭×5 + 月锭×5 @ 远古操纵机
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if(classic.TryFind<ModItem>("CoreofCalamity", out ModItem classicCoreofCalamity)
                    && classic.TryFind<ModItem>("BarofLife", out ModItem barofLife))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<AngelTreads>();
                    AddAnyWings(recipeClassic);
                    recipeClassic.AddIngredient(classicCoreofCalamity.Type, 3);
                    recipeClassic.AddIngredient(barofLife.Type, 5);
                    recipeClassic.AddIngredient(ItemID.LunarBar, 5);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
        /// <summary>
        /// 加「任意翅膀」这一味：优先用灾厄自己的配方组（**实装 2.2.2 = `AnyWings`**——本机直读其 dll
        /// 只有这个组名、没有 `WingsGroup`；经典版 = `WingsGroup`），两个组都不在时就不放翅膀这味
        /// （只留核心/合金/月锭），与其它件的"缺件即退化"同款口径
        /// </summary>
        private static void AddAnyWings(Recipe recipe)
        {
            if (RecipeGroup.recipeGroupIDs.ContainsKey("AnyWings"))
                recipe.AddRecipeGroup("AnyWings");
            else if (RecipeGroup.recipeGroupIDs.ContainsKey("WingsGroup"))
                recipe.AddRecipeGroup("WingsGroup");
        }
    }
}
