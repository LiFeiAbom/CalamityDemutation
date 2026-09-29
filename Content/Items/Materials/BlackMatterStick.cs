using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 中子锭（BlackMatterStick，移植自灾厄大修 0.4.0.1.3 的同名材料）—— 中子系列武器的高级材料。
    /// 大修原版没有任何普通配方（它挂在 Supertable 超级工作台体系上：OmigaSnyContent = FullItems5、售价 999 金），
    /// 本模组改写为普通配方：四柱碎片 + 原版全部矿锭 + 对应版本灾厄的全部矿锭 @ 德雷顿熔炉，售价 1 铂金。
    /// 目前是中子之刃（NeutronGlaive）/中子脉冲（NeutronGun）各 12 个、洛希之弦（NeutronBow）25 个的合成材料。
    /// 贴图使用灵魂式的垂直逐帧动画。
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
        /// 注册合成配方（本模组自加，大修原版没有普通配方）：两版灾厄的矿锭清单不同，分别注册——
        /// 现代版(CalamityMod)和经典版(CalamityModClassicPreTrailer)均使用德雷顿熔炉。
        /// 注意两版都要求清单里的矿锭全部命中（TryFind 全成功）才注册，因此铜/锡、铁/铅、银/钨、金/铂、
        /// 魔矿/猩红矿这些成对的世界专属矿石会被同时要求。
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
                if(calamity.TryFind<ModItem>("AerialiteBar", out ModItem aerialiteBar1) && calamity.TryFind<ModItem>("AstralBar", out ModItem astralBar1) && calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar1) && calamity.TryFind<ModItem>("CryonicBar", out ModItem cryonicBar) && calamity.TryFind<ModItem>("PerennialBar", out ModItem perennialBar) && calamity.TryFind<ModItem>("ScoriaBar", out ModItem scoriaBar) && calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar1) && calamity.TryFind<ModItem>("UelibloomBar", out ModItem uelibloomBar) && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge1))
                {
                    recipe.AddIngredient(aerialiteBar1.Type);
                    recipe.AddIngredient(astralBar1.Type);
                    recipe.AddIngredient(auricBar.Type);
                    recipe.AddIngredient(cosmiliteBar1.Type);
                    recipe.AddIngredient(cryonicBar.Type);
                    recipe.AddIngredient(perennialBar.Type);
                    recipe.AddIngredient(scoriaBar.Type);
                    recipe.AddIngredient(shadowspecBar1.Type);
                    recipe.AddIngredient(uelibloomBar.Type);
                    recipe.AddTile(draedonsForge1.Type);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))// 经典版灾厄已加载
            {
                Recipe recipeClassic = CreateRecipe();
                recipeClassic.AddIngredient(ItemID.FragmentSolar);
                recipeClassic.AddIngredient(ItemID.FragmentVortex);
                recipeClassic.AddIngredient(ItemID.FragmentNebula);
                recipeClassic.AddIngredient(ItemID.FragmentStardust);
                // 原版全部矿锭
                recipeClassic.AddIngredient(ItemID.CopperBar);
                recipeClassic.AddIngredient(ItemID.TinBar);
                recipeClassic.AddIngredient(ItemID.IronBar);
                recipeClassic.AddIngredient(ItemID.LeadBar);
                recipeClassic.AddIngredient(ItemID.SilverBar);
                recipeClassic.AddIngredient(ItemID.TungstenBar);
                recipeClassic.AddIngredient(ItemID.GoldBar);
                recipeClassic.AddIngredient(ItemID.PlatinumBar);
                recipeClassic.AddIngredient(ItemID.DemoniteBar);
                recipeClassic.AddIngredient(ItemID.CrimtaneBar);
                recipeClassic.AddIngredient(ItemID.HellstoneBar);
                recipeClassic.AddIngredient(ItemID.CobaltBar);
                recipeClassic.AddIngredient(ItemID.PalladiumBar);
                recipeClassic.AddIngredient(ItemID.MythrilBar);
                recipeClassic.AddIngredient(ItemID.OrichalcumBar);
                recipeClassic.AddIngredient(ItemID.AdamantiteBar);
                recipeClassic.AddIngredient(ItemID.TitaniumBar);
                recipeClassic.AddIngredient(ItemID.HallowedBar);
                recipeClassic.AddIngredient(ItemID.ChlorophyteBar);
                recipeClassic.AddIngredient(ItemID.ShroomiteBar);
                recipeClassic.AddIngredient(ItemID.SpectreBar);
                recipeClassic.AddIngredient(ItemID.LunarBar);
                // 灾厄经典版全部矿锭
                if(classic.TryFind<ModItem>("AerialiteBar", out ModItem aerialiteBar2) && classic.TryFind<ModItem>("AstralBar", out ModItem astralBar2) && classic.TryFind<ModItem>("BarofLife", out ModItem barofLife) && classic.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar2) && classic.TryFind<ModItem>("CruptixBar", out ModItem cruptixBar) && classic.TryFind<ModItem>("CryoBar", out ModItem cryoBar) && classic.TryFind<ModItem>("DraedonBar", out ModItem draedonBar) && classic.TryFind<ModItem>("MeldiateBar", out ModItem meldiateBar) && classic.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar2) && classic.TryFind<ModItem>("UeliaceBar", out ModItem ueliaceBar) && classic.TryFind<ModItem>("VerstaltiteBar", out ModItem verstaltiteBar) && classic.TryFind<ModItem>("VictideBar", out ModItem victideBar) && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge2))
                {
                    recipeClassic.AddIngredient(aerialiteBar2.Type);
                    recipeClassic.AddIngredient(astralBar2.Type);
                    recipeClassic.AddIngredient(barofLife.Type);
                    recipeClassic.AddIngredient(cosmiliteBar2.Type);
                    recipeClassic.AddIngredient(cruptixBar.Type);
                    recipeClassic.AddIngredient(cryoBar.Type);
                    recipeClassic.AddIngredient(draedonBar.Type);
                    recipeClassic.AddIngredient(meldiateBar.Type);
                    recipeClassic.AddIngredient(shadowspecBar2.Type);
                    recipeClassic.AddIngredient(ueliaceBar.Type);
                    recipeClassic.AddIngredient(verstaltiteBar.Type);
                    recipeClassic.AddIngredient(victideBar.Type);
                    recipeClassic.AddTile(draedonsForge2.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
