using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 天使之靴（Angel Treads） - 移速饰品链的最底一档（本工程链条下位件）。
    /// 移植口径（用户 2026-10-14 指定）：**功能取灾厄 1.3-release 的 AngelTreads，配方取 2.0 那一批**。
    /// 功能（1.3-release）：跑速上限 7.5、移速 +12%、火箭靴 3 级、冰面/水面/岩浆面行走、
    /// 岩浆免疫 240 帧（4 秒）、免疫「着火了！」减益；并按 1.3-release 里 <c>CalamityPlayer.angelTreads</c>
    /// 的语义额外给 +10% 飞行时间（本工程记在 CalamityDemutationPlayer.angelTreads，
    /// 在 PostUpdateMiscEffects 里对 wingTimeMax ×1.1，与同处 soaring 的写法一致）。
    /// **未**采用 1.4.4 世系（= 实装 2.2.2 那件）的加强：那边岩浆免疫 420 帧（7 秒）、删掉了免「着火！」、
    /// 另加 lavaRose 减伤与显式的 FlightTimeBoost 字段；1.3-release 与 2.0 的功能本身是同一套。
    /// 面板：尺寸 36x34、价值 35 金（照源 Rarity6BuyPrice）、稀有度浅紫 LightPurple；
    /// 1.3-release 源码里写的 36x32 与它自带贴图 36x34 不符，此处按贴图取。
    /// 贴图取 1.3-release 的旧图（实装 2.2.2 里的同名件是 32x36 的新图，两者并存时外观可分辨）。
    /// 配方（双版本各注册一条，缺件即退化成单分支）：现代分支照 2.0 = 泰拉闪耀靴 + 鸟妖指环 +
    /// 日光精华×5 + 力量/视域/恐惧之魂 @ 秘银砧；经典分支照经典世系 cal-1.4.2.101 = 霜火之靴 + 熔岩涉水靴 +
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
        /// 与岩浆时限、免「着火！」——数值逐条照 1.3-release 的 UpdateAccessory。
        /// 1.3-release / 2.0 都不写 vanityRocketBoots（那是 2.0.3.9 之后才补的），此处同样不写。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().angelTreads = true;  // +10% 飞行时间（在 PostUpdateMiscEffects 结算）
            player.accRunSpeed = 7.5f;        // 跑速上限
            player.rocketBoots = 3;           // 火箭靴等级（给予飞行）
            player.moveSpeed += 0.12f;        // 移速 +12%
            player.iceSkate = true;           // 冰面机动性
            player.waterWalk = true;          // 水面行走
            player.fireWalk = true;           // 岩浆面行走
            player.lavaMax += 240;            // 岩浆免疫 240 帧（4 秒）
            player.buffImmune[BuffID.OnFire] = true;  // 免疫「着火了！」
        }
        /// <summary>
        /// 配方（分版本，两分支各自 TryGetMod + TryFind 串联，缺材料时静默跳过该条）：
        /// 现代分支（CalamityMod 在场）照灾厄 2.0 那批；经典分支（CalamityModClassicPreTrailer 在场）
        /// 照经典世系 cal-1.4.2.101，材料换成各自版本里的件。
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：泰拉闪耀靴 + 鸟妖指环 + 日光精华×5 + 三魂 @ 秘银砧（照 2.0 源码）
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
            // 经典版灾厄：霜火之靴 + 熔岩涉水靴 + 鸟妖指环 + 余烬精华×5 + 天空合金锭×20 + 三魂 @ 秘银砧
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
