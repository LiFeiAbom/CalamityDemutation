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
                if(calamity1.TryFind<ModItem>("AerialiteBar", out ModItem aerialiteBar2) && calamity1.TryFind<ModItem>("AstralBar", out ModItem astralBar2) && calamity1.TryFind<ModItem>("BarofLife", out ModItem barofLife) && calamity1.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar2) && calamity1.TryFind<ModItem>("CruptixBar", out ModItem cruptixBar) && calamity1.TryFind<ModItem>("CryoBar", out ModItem cryoBar) && calamity1.TryFind<ModItem>("DraedonBar", out ModItem draedonBar) && calamity1.TryFind<ModItem>("MeldiateBar", out ModItem meldiateBar) && calamity1.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar2) && calamity1.TryFind<ModItem>("UeliaceBar", out ModItem ueliaceBar) && calamity1.TryFind<ModItem>("VerstaltiteBar", out ModItem verstaltiteBar) && calamity1.TryFind<ModItem>("VictideBar", out ModItem victideBar) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge2))
                {
                    recipe1.AddIngredient(aerialiteBar2.Type);
                    recipe1.AddIngredient(astralBar2.Type);
                    recipe1.AddIngredient(barofLife.Type);
                    recipe1.AddIngredient(cosmiliteBar2.Type);
                    recipe1.AddIngredient(cruptixBar.Type);
                    recipe1.AddIngredient(cryoBar.Type);
                    recipe1.AddIngredient(draedonBar.Type);
                    recipe1.AddIngredient(meldiateBar.Type);
                    recipe1.AddIngredient(shadowspecBar2.Type);
                    recipe1.AddIngredient(ueliaceBar.Type);
                    recipe1.AddIngredient(verstaltiteBar.Type);
                    recipe1.AddIngredient(victideBar.Type);
                    recipe1.AddTile(draedonsForge2.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
