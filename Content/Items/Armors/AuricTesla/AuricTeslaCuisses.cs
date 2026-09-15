using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    /// <summary>
    /// 金之特斯拉护腿（Auric Tesla Cuisses） - 金之特斯拉套（AuricTesla）腿部防具
    /// 提供大幅移速（含魔毯悬浮）、通用伤害与暴击。套装判定见 AuricTeslaHelm。
    /// </summary>
    [AutoloadEquip(EquipType.Legs)]
    internal class AuricTeslaCuisses:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 8, 0, 0);   // 价值 1 铂金 8 金
            Item.defense = 44;                         // 防御 +44
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;   // 月后稀有度 20 级
        }
        /// <summary>
        /// 单件装备加成：移速 +50%、获得魔毯悬浮、通用伤害 +14% 与通用暴击 +14%
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.5f;                            // 移动速度 +50%
            player.carpet = true;                                // 获得魔毯悬浮能力
            player.GetDamage<GenericDamageClass>() += 0.14f;     // 通用伤害 +14%
            player.GetCritChance<GenericDamageClass>() += 14;    // 通用暴击率 +14%
        }
        /// <summary>
        /// 注册配方：两分支均以四套月后护腿 + 魔毯为坯料，现代版用金锭×15（宇宙砧），
        /// 经典版用金矿石与各类魂材（德雷顿熔炉）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：四套护腿 + 魔毯 + AuricBar×15（宇宙砧）
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonLeggings>();
                    recipe.AddIngredient<BloodflareCuisses>();
                    recipe.AddIngredient<SilvaLeggings>();
                    recipe.AddIngredient<GodSlayerLeggings>();
                    recipe.AddIngredient(ItemID.FlyingCarpet);
                    recipe.AddIngredient(auricBar.Type, 15);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：四套护腿 + 金矿石×80 + 各类魂材 + 魔毯（德雷顿熔炉）
                if (calamity1.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                    && calamity1.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && calamity1.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && calamity1.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && calamity1.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && calamity1.TryFind<ModItem>("BarofLife", out ModItem barofLife)
                    && calamity1.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment)
                    && calamity1.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity)
                    && calamity1.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity)
                    && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<TarragonLeggings>();
                    recipe1.AddIngredient<BloodflareCuisses>();
                    recipe1.AddIngredient<SilvaLeggings>();
                    recipe1.AddIngredient<GodSlayerLeggings>();
                    recipe1.AddIngredient(auricOre.Type, 80);              // 金矿石×80
                    recipe1.AddIngredient(endothermicEnergy.Type, 20);      // 吸热能量×20
                    recipe1.AddIngredient(nightmareFuel.Type, 20);          // 梦魇燃料×20
                    recipe1.AddIngredient(phantoplasm.Type, 15);            // 幻影质×15
                    recipe1.AddIngredient(darksunFragment.Type, 10);        // 暗阳碎片×10
                    recipe1.AddIngredient(barofLife.Type, 8);               // 生命锭×8
                    recipe1.AddIngredient(hellcasterFragment.Type, 6);      // 地狱施法者碎片×6
                    recipe1.AddIngredient(coreofCalamity.Type, 3);          // 灾厄核心×3
                    recipe1.AddIngredient(galacticaSingularity.Type, 2);    // 银河奇点×2
                    recipe1.AddIngredient(ItemID.FlyingCarpet);
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
