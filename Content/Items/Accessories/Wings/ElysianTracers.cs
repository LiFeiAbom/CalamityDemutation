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
    /// 极乐翼靴（Elysian Tracers） - 天界翼靴的上一档（本工程链条第三级）。
    /// **名字口径（用户 2026-10-14 指定）**：中文名「极乐翼靴」（英文名照源 = `Elysian Tracers`；
    /// 靴子模式的 `TreadsName` 取「极乐履带」）——旧版灾厄这一档就叫 Elysian Tracers，
    /// 2.0.4 改名 `TracersElysian`、1.4.4 起换成 `VoidStriders`（显示名 Void Striders）。
    /// **移植口径（同前两件）：功能取 旧版灾厄、配方取 2.0 那一批、再按复合原则并入 1.4.4
    /// （= 实装 2.2.2 的 VoidStriders）的增强。** 逐项对照：
    /// ① 旧版灾厄 = 飞行 180 / 水平速度 10.5 / 倍率 2.75、跑速上限 9.25、移速 +20%、
    /// 冰面·水面·岩浆面行走、岩浆 240 帧、免「着火！」、免坠落伤害、垂直速度 0.95/0.15/1.1/3.15/0.135、
    /// 飞行粉尘（206/173 随机）、稀有度 14 档（DarkBlue）价值 2 铂金，
    /// 且装备栏**只有 Wings**（无 Shoes，与全部版本一致，故本件不需要 `_Shoes.png`）；
    /// ② 2.0 = 同一套（`WingStats(180, 10.5, 2.75)` 等价写法，配方前件改 `SeraphTracers`）；
    /// ③ 2.0.4 = 飞行 180 / 水平 10 / 倍率 2.7、跑速上限 8.5、移速 +16%、改成完全免岩浆、
    /// 垂直速度收成 0.85/0.15/1.1/2.7/0.135；
    /// ④ 1.4.4（VoidStriders）= **飞行 200**、水平 10、倍率 2.7、跑速上限 8.5、移速 +16%、完全免岩浆、
    /// 新增 `socialShadowRocketBoots`、**可切换翅膀**（Shift+右键；靴子模式时给火箭靴 1 与
    /// `angelTreads` 标记 = +10% 飞行时间）、稀有度降到 13 档（PureGreen，价值 = `Rarity13BuyPrice`）。
    /// **复合结果**：飞行 200（取大）/ 水平速度 10.5（取大）/ 倍率 2.75（取大）/ 跑速上限 9.25（取大）/
    /// 移速 +20%（取大）/ 完全免岩浆（取强）/ 免「着火！」/ 免坠落伤害 /
    /// 垂直速度 0.95 / 0.15 / 1.1 / 3.15 / 0.135（取大）/ 稀有度与价值取 14 档 2 铂金（取高）/
    /// `socialShadowRocketBoots`（1.4.4）/ 火箭靴 1（1.4.4 靴子模式）/ 可切换翅膀（1.4.4）。
    /// 未并进来的仍是**灾厄内部视觉通道**（纯外观，按 §5 口径不硬塞）：`CalamityPlayer.IBoots` /
    /// `elysianFire` / `eTracers`（2.0 及更早在 CalamityPlayerDrawEffects 里画的拖尾与火焰）、
    /// `tracersDust`（2.0.4/1.4.4）以及 `CalamityUtils.DrawInventoryDot` 的模式点——本件自己的飞行粉尘
    /// 已照源实现。
    /// 面板：尺寸 48×36、价值 2 铂金、月后自定义稀有度 14（蓝）；贴图取 旧版灾厄 的旧图
    /// （48×36 物品图 91B0A089 + 86×248 翅膀图 D535C02E，这一套从 经典灾厄一直用到 2.0.4；
    /// 1.4.4 的 VoidStriders 只换了物品图、翅膀图仍是同一张）。
    /// 配方（双版本各注册一条，缺件即退化成单分支）：**现代**照 2.0 = 天界翼靴 + 极乐之翼 +
    /// 宇宙锭×5 + 登仙精华×4 @ 宇宙砧；**经典**照 经典灾厄 = 天界翼靴 + 极乐之翼 +
    /// 宇宙锭×5 + 幻魂×5 @ 嘉登熔炉。
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    internal class ElysianTracers:ModItem
    {
        /// <summary>本类型被 AutoloadEquip 分配的翅膀槽位编号；关掉翅膀模式时把实例的 Item.wingSlot 置 −1</summary>
        private static int wingSlot;
        /// <summary>
        /// 翅膀模式开关：存在**物品实例**上（不是 ModItem 字段，联机时随物品同步），
        /// 靠 `Item.wingSlot` 是否为 −1 表示；写法照 1.4.4 的 VoidStriders
        /// </summary>
        private bool toggleEnabled
        {
            get { return Item.wingSlot != -1; }
            set { Item.wingSlot = value ? wingSlot : -1; }
        }
        /// <summary>
        /// 注册翅膀数据：飞行时间 200（复合取 1.4.4 的值；旧版灾厄/2.0/2.0.4 是 180）、
        /// 水平速度 10.5（旧版灾厄/2.0 的值，2.0.4/1.4.4 是 10）、
        /// 水平加速度倍率 2.75（旧版灾厄/2.0 的值，2.0.4/1.4.4 是 2.7）；
        /// 并记下本类型的 wingSlot 供模式开关用
        /// </summary>
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(200, 10.5f, 2.75f);
            wingSlot = Item.wingSlot;
        }
        /// <summary>
        /// 物品基础属性：48×36 贴图、价值 2 铂金、月后自定义稀有度 14（蓝）、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 48;                          // 贴图宽（像素）
            Item.height = 36;                         // 贴图高（像素）
            Item.value = Item.buyPrice(2, 0, 0, 0);   // 价值 2 铂金（照源 Rarity14BuyPrice）
            Item.accessory = true;                    // 作为饰品装备
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后稀有度 14（蓝，= 源的 DarkBlue 档）
        }
        /// <summary>
        /// 装备时：先画飞行粉尘（照 旧版灾厄；各版都用 206/173 两颗随机），
        /// 再设置跑速/移速/三种行走免疫/免岩浆/免「着火！」/免坠落伤害与 socialShadowRocketBoots，
        /// 最后处理**靴子模式**——翅膀被关掉时给火箭靴 1 与 angelTreads 标记（+10% 飞行时间），
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
                int flightDust = Dust.NewDust(new Vector2(player.position.X + (float)(player.width / 2) + (float)dustXOffset, player.position.Y + (float)(player.height / 2) - 15f), 30, 30, Main.rand.NextBool() ? 206 : 173, 0f, 0f, 100, default, 2.4f);
                Main.dust[flightDust].noGravity = true;      // 不受重力
                Main.dust[flightDust].velocity *= 0.3f;      // 减缓漂移
                if (Main.rand.NextBool(10))
                {
                    Main.dust[flightDust].fadeIn = 2f;       // 1/10 概率淡入
                }
                Main.dust[flightDust].shader = GameShaders.Armor.GetSecondaryShader(player.cWings, player);  // 随翅膀染色
            }
            player.accRunSpeed = 9.25f;       // 跑速上限（旧版灾厄/2.0 的 9.25；1.4.4 是 8.5，复合取大）
            player.moveSpeed += 0.2f;         // 移速 +20%（旧版灾厄/2.0 的值；1.4.4 是 +16%，复合取大）
            player.iceSkate = true;           // 冰面机动性
            player.waterWalk = true;          // 水面行走
            player.fireWalk = true;           // 岩浆面行走
            player.lavaImmune = true;         // 完全免岩浆（2.0.4/1.4.4；旧版灾厄/2.0 只有 240 帧，复合取强）
            player.buffImmune[BuffID.OnFire] = true;   // 免疫「着火了！」
            player.noFallDmg = true;          // 免坠落伤害
            player.socialShadowRocketBoots = true;     // 1.4.4 新增：时装栏火箭靴也渲染残影
            if (!toggleEnabled)               // 靴子模式：火箭靴 1 + 天使之靴的 +10% 飞行时间（照 1.4.4）
            {
                player.rocketBoots = player.vanityRocketBoots = 1;
                player.GetModPlayer<CalamityDemutationPlayer>().angelTreads = true;
            }
        }
        /// <summary>
        /// 垂直飞行参数：复合取大 = 旧版灾厄 那套（0.95 / 0.15 / 1.1 / 3.15 / 0.135；
        /// 1.4.4 是 0.85 / 0.15 / 1.1 / 2.7 / 0.135），行尾为原版默认值
        /// </summary>
        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.95f; //0.85
            ascentWhenRising = 0.15f; //0.15
            maxCanAscendMultiplier = 1.1f; //1
            maxAscentMultiplier = 3.15f; //3
            constantAscend = 0.135f; //0.135
        }
        /// <summary>
        /// 靴子模式（翅膀关掉）时去掉第一行「可以飞行并缓降」——照 1.4.4 的 VoidStriders
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
        /// 模式存在物品实例上，需随存档与联机同步（四件套照 1.4.4 的 VoidStriders）
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
        /// 模式不同名字不同：翅膀模式显示「极乐翼靴」，靴子模式显示「极乐履带」
        /// （照 1.4.4 用本地化键切名字；灾厄那枚物品栏模式指示点未移植）
        /// </summary>
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Item.SetNameOverride(Language.GetTextValue($"Mods.CalamityDemutation.Items.ElysianTracers.{(toggleEnabled ? "DisplayName" : "TreadsName")}"));
        }
        /// <summary>
        /// 配方（分版本，两分支各自 TryGetMod + TryFind 串联，缺材料时静默跳过该条）：
        /// 现代分支（灾厄本体在场）照灾厄 2.0；经典分支（经典灾厄在场）
        /// 照经典灾厄经典灾厄（末味由登仙精华×4 @ 宇宙砧 换成 幻魂×5 @ 嘉登熔炉）。
        /// </summary>
        public override void AddRecipes()
        {
            // 现代灾厄：天界翼靴 + 极乐之翼 + 宇宙锭×5 + 登仙精华×4 @ 宇宙砧
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("ElysianWings", out ModItem elysianWings)
                    && calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<InfinityBoots>();
                    recipe.AddIngredient(elysianWings.Type);
                    recipe.AddIngredient(cosmiliteBar.Type, 5);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 4);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            // 经典灾厄：天界翼靴 + 极乐之翼 + 宇宙锭×5 + 幻魂×5 @ 嘉登熔炉
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if(classic.TryFind<ModItem>("ElysianWings", out ModItem classicElysianWings)
                    && classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<InfinityBoots>();
                    recipeClassic.AddIngredient(classicElysianWings.Type);
                    recipeClassic.AddIngredient(classicCosmiliteBar.Type, 5);
                    recipeClassic.AddIngredient(phantoplasm.Type, 5);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
