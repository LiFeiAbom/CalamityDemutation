using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 天使之靴（Angel Treads） - 移速饰品链的最底一档（本工程链条下位件）。
    /// 移植口径（用户 2026-10-14 指定）：**功能取灾厄 旧版灾厄 的 AngelTreads，配方取 2.0 那一批**；
    /// 当天追加：**按工程"复合"原则把 1.4.4 世系（= 实装 2.2.2）的增强也并进来**。
    /// 功能（旧版灾厄 底 + 1.4.4 增强的并集）：跑速上限 7.5、移速 +12%、火箭靴 3 级、
    /// 冰面/水面/岩浆面行走、岩浆免疫 **420 帧（7 秒，取 1.4.4 的值；旧版灾厄/2.0 只有 240）**、
    /// 免疫「着火了！」减益（1.4.4 那边删了，按复合并集保留）、<c>lavaRose</c> 岩浆灼烧减伤（1.4.4 新增）；
    /// 另有 <c>CalamityPlayer.angelTreads</c> 语义的 +10% 飞行时间——旧版灾厄 用该标记给 0.1、
    /// 1.4.4 用 <c>AngelTreads.FlightTimeBoost</c> 给同一个值，**两版同义，只算一次**：
    /// 本工程记在 CalamityDemutationPlayer.angelTreads，在 PostUpdateMiscEffects 里对 wingTimeMax ×1.1。
    /// 面板：尺寸 36x34、价值 35 金（照源 Rarity6BuyPrice）、稀有度浅紫 LightPurple；
    /// 旧版灾厄 源码里写的 36x32 与它自带贴图 36x34 不符，此处按贴图取。
    /// 贴图取 旧版灾厄 的旧图（实装 2.2.2 里的同名件是 32x36 的新图，两者并存时外观可分辨）。
    /// 配方（双版本各注册一条，缺件即退化成单分支）：现代分支照 2.0 = 泰拉闪耀靴 + 鸟妖指环 +
    /// 日光精华×5 + 力量/视域/恐惧之魂 @ 秘银砧；经典分支照经典灾厄经典灾厄 = 霜火之靴 + 熔岩涉水靴 +
    /// 鸟妖指环 + 余烬精华×5 + 天空合金锭×20 + 三魂 @ 秘银砧。
    /// </summary>
    [AutoloadEquip(EquipType.Shoes)]
    internal class AngelTreads:ModItem
    {
        /// <summary>
        /// 物品基础属性：36x34 贴图、价值 35 金、稀有度浅紫、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 36;                          // 贴图宽（像素）
            Item.height = 34;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 35, 0, 0);  // 价值 35 金（照源 Rarity6BuyPrice）
            Item.rare = ItemRarityID.LightPurple;     // 稀有度：浅紫
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时：置 angelTreads 标记（用于 +10% 飞行时间），并直接设置跑步/移动/火箭靴/三种行走免疫
        /// 与岩浆时限、免「着火！」、岩浆减伤——旧版灾厄 的 UpdateAccessory 逐条照抄，
        /// 岩浆时限换成 1.4.4 的 420、另加 1.4.4 的 lavaRose（复合口径）。
        /// vanityRocketBoots 与 UpdateVanity 照 1.4.4（旧版灾厄/2.0 没有这两处，纯外观项，按复合一并取）。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().angelTreads = true;  // +10% 飞行时间（在 PostUpdateMiscEffects 结算）
            player.accRunSpeed = 7.5f;        // 跑速上限
            player.rocketBoots = player.vanityRocketBoots = 3;  // 火箭靴等级（给予飞行）；vanity 同步照 1.4.4 的写法
            player.moveSpeed += 0.12f;        // 移速 +12%
            player.iceSkate = true;           // 冰面机动性
            player.waterWalk = true;          // 水面行走
            player.fireWalk = true;           // 岩浆面行走
            player.lavaMax += 420;            // 岩浆免疫 420 帧（7 秒；复合进 1.4.4 的值，旧版灾厄/2.0 是 240）
            player.lavaRose = true;           // 降低岩浆灼烧伤害（1.4.4 新增，原版字段）
            player.buffImmune[BuffID.OnFire] = true;  // 免疫「着火了！」
        }
        /// <summary>
        /// 时装栏也置火箭靴等级：照 1.4.4 的 <c>UpdateVanity</c>（2.0.3.9 起才有；旧版灾厄/2.0 没有），
        /// 纯外观——挂在时装栏时靴子照样渲染火焰
        /// </summary>
        public override void UpdateVanity(Player player)
        {
            player.vanityRocketBoots = 3;
        }
        /// <summary>
        /// 配方（分版本，两分支各自 TryGetMod + TryFind 串联，缺材料时静默跳过该条）：
        /// 现代分支（灾厄本体在场）照灾厄 2.0 那批；经典分支（经典灾厄在场）
        /// 照经典灾厄经典灾厄，材料换成各自版本里的件。
        /// </summary>
        public override void AddRecipes()
        {
            // 现代灾厄：泰拉闪耀靴 + 鸟妖指环 + 日光精华×5 + 三魂 @ 秘银砧（照 2.0 源码）
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("HarpyRing", out ModItem harpyRing)
                    && calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.TerrasparkBoots);
                    recipe.AddIngredient(harpyRing.Type);
                    recipe.AddIngredient(essenceofSunlight.Type, 5);
                    recipe.AddIngredient(ItemID.SoulofMight);
                    recipe.AddIngredient(ItemID.SoulofSight);
                    recipe.AddIngredient(ItemID.SoulofFright);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            // 经典灾厄：霜火之靴 + 熔岩涉水靴 + 鸟妖指环 + 余烬精华×5 + 天空合金锭×20 + 三魂 @ 秘银砧
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if(classic.TryFind<ModItem>("HarpyRing", out ModItem classicHarpyRing)
                    && classic.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder)
                    && classic.TryFind<ModItem>("AerialiteBar", out ModItem aerialiteBar))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ItemID.FrostsparkBoots);
                    recipeClassic.AddIngredient(ItemID.LavaWaders);
                    recipeClassic.AddIngredient(classicHarpyRing.Type);
                    recipeClassic.AddIngredient(essenceofCinder.Type, 5);
                    recipeClassic.AddIngredient(aerialiteBar.Type, 20);
                    recipeClassic.AddIngredient(ItemID.SoulofMight);
                    recipeClassic.AddIngredient(ItemID.SoulofSight);
                    recipeClassic.AddIngredient(ItemID.SoulofFright);
                    recipeClassic.AddTile(TileID.MythrilAnvil);
                    recipeClassic.Register();
                }
            }
        }
    }
}
