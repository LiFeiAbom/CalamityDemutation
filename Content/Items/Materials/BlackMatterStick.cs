using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 黑物质棒（BlackMatterStick） - 材料物品。
    /// 由四柱碎片 + 原版全部矿锭 + 对应版本灾厄的全部矿锭合成（现代版在宇宙砧、经典版在德拉贡锻造台），
    /// 目前是 NeutronGlaive 的制作材料（每件需 12 个）。物品使用灵魂式的垂直逐帧动画贴图。
    /// </summary>
    internal class BlackMatterStick : ModItem
    {
        /// <summary>
        /// 静态属性：研究解锁数量、以及动画/灵魂类物品标记，使贴图逐帧播放并像"魂"一样浮动。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 9999;                                   // 研究所解锁所需数量
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 18));// 垂直动画：18 帧、每 5 tick 换一帧
            ItemID.Sets.AnimatesAsSoul[Type] = true;                           // 按灵魂类物品处理（浮动/发光表现）
        }
        /// <summary>
        /// 基础属性：尺寸 25、堆叠 99、青柠稀有度、售价 1 铂金；设置了挥击使用动画（本身无使用效果）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = Item.height = 25;                       // 贴图宽高（像素）
            Item.maxStack = 99;                                  // 最大堆叠
            Item.rare = ItemRarityID.Lime;                       // 稀有度：青柠
            Item.value = Terraria.Item.sellPrice(platinum:1);    // 售价 1 铂金
            Item.useAnimation = Item.useTime = 15;               // 使用动画/间隔（tick）
            Item.useStyle = ItemUseStyleID.Swing;                // 使用样式：挥击
        }
        /// <summary>
        /// 注册合成配方：两版灾厄的材料不同，分别注册——
        /// 现代版(CalamityMod)用宇宙砧，经典版(CalamityModClassicPreTrailer)用德拉贡锻造台。
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))// 现代版灾厄已加载
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.FragmentSolar);
                recipe.AddIngredient(ItemID.FragmentVortex);
                recipe.AddIngredient(ItemID.FragmentNebula);
                recipe.AddIngredient(ItemID.FragmentStardust);
                // 原版全部矿锭
                recipe.AddIngredient(ItemID.CopperBar);
                recipe.AddIngredient(ItemID.TinBar);
                recipe.AddIngredient(ItemID.IronBar);
                recipe.AddIngredient(ItemID.LeadBar);
                recipe.AddIngredient(ItemID.SilverBar);
                recipe.AddIngredient(ItemID.TungstenBar);
                recipe.AddIngredient(ItemID.GoldBar);
                recipe.AddIngredient(ItemID.PlatinumBar);
                recipe.AddIngredient(ItemID.DemoniteBar);
                recipe.AddIngredient(ItemID.CrimtaneBar);
                recipe.AddIngredient(ItemID.HellstoneBar);
                recipe.AddIngredient(ItemID.CobaltBar);
                recipe.AddIngredient(ItemID.PalladiumBar);
                recipe.AddIngredient(ItemID.MythrilBar);
                recipe.AddIngredient(ItemID.OrichalcumBar);
                recipe.AddIngredient(ItemID.AdamantiteBar);
                recipe.AddIngredient(ItemID.TitaniumBar);
                recipe.AddIngredient(ItemID.HallowedBar);
                recipe.AddIngredient(ItemID.ChlorophyteBar);
                recipe.AddIngredient(ItemID.ShroomiteBar);
                recipe.AddIngredient(ItemID.SpectreBar);
                recipe.AddIngredient(ItemID.LunarBar);
                // 灾厄现代版全部矿锭
                recipe.AddIngredient(calamity.Find<ModItem>("AerialiteBar").Type);
                recipe.AddIngredient(calamity.Find<ModItem>("AstralBar").Type);
                recipe.AddIngredient(calamity.Find<ModItem>("AuricBar").Type);
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type);
                recipe.AddIngredient(calamity.Find<ModItem>("CryonicBar").Type);
                recipe.AddIngredient(calamity.Find<ModItem>("PerennialBar").Type);
                recipe.AddIngredient(calamity.Find<ModItem>("ScoriaBar").Type);
                recipe.AddIngredient(calamity.Find<ModItem>("UelibloomBar").Type);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))// 经典版灾厄已加载
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.FragmentSolar);
                recipe1.AddIngredient(ItemID.FragmentVortex);
                recipe1.AddIngredient(ItemID.FragmentNebula);
                recipe1.AddIngredient(ItemID.FragmentStardust);
                // 原版全部矿锭
                recipe1.AddIngredient(ItemID.CopperBar);
                recipe1.AddIngredient(ItemID.TinBar);
                recipe1.AddIngredient(ItemID.IronBar);
                recipe1.AddIngredient(ItemID.LeadBar);
                recipe1.AddIngredient(ItemID.SilverBar);
                recipe1.AddIngredient(ItemID.TungstenBar);
                recipe1.AddIngredient(ItemID.GoldBar);
                recipe1.AddIngredient(ItemID.PlatinumBar);
                recipe1.AddIngredient(ItemID.DemoniteBar);
                recipe1.AddIngredient(ItemID.CrimtaneBar);
                recipe1.AddIngredient(ItemID.HellstoneBar);
                recipe1.AddIngredient(ItemID.CobaltBar);
                recipe1.AddIngredient(ItemID.PalladiumBar);
                recipe1.AddIngredient(ItemID.MythrilBar);
                recipe1.AddIngredient(ItemID.OrichalcumBar);
                recipe1.AddIngredient(ItemID.AdamantiteBar);
                recipe1.AddIngredient(ItemID.TitaniumBar);
                recipe1.AddIngredient(ItemID.HallowedBar);
                recipe1.AddIngredient(ItemID.ChlorophyteBar);
                recipe1.AddIngredient(ItemID.ShroomiteBar);
                recipe1.AddIngredient(ItemID.SpectreBar);
                recipe1.AddIngredient(ItemID.LunarBar);
                // 灾厄经典版全部矿锭
                recipe1.AddIngredient(calamity1.Find<ModItem>("AerialiteBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("AstralBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("BarofLife").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CruptixBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CryoBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DraedonBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("MeldiateBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("UeliaceBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("VerstaltiteBar").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("VictideBar").Type);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
