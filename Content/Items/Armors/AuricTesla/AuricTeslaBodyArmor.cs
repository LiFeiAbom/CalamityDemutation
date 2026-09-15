using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    /// <summary>
    /// 金之特斯拉胸甲（Auric Tesla Body Armor） - 金之特斯拉套（AuricTesla）身体防具
    /// 提供生命/魔力上限、通用伤害与暴击、移速，并置位霜冻屏障与弑神者反射标记；
    /// 另注册背面装备贴图（_Back），穿齐后显示披风。套装判定见 AuricTeslaHelm。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class AuricTeslaBodyArmor:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 44, 0, 0);   // 价值 1 铂金 44 金
            Item.defense = 48;                          // 防御 +48
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;   // 月后稀有度 20 级
        }
        /// <summary>
        /// 加载背面装备贴图（_Back），服务端不需要，故非服务器端才注册
        /// </summary>
        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
                EquipLoader.AddEquipTexture(Mod, Texture + "_Back", EquipType.Back, this);
        }
        /// <summary>
        /// 穿戴该胸甲时把玩家的背部外观切换为本装备的 _Back 贴图（即披风）
        /// </summary>
        public override void EquipFrameEffects(Player player, EquipType type)
        {
            if (player.body == Item.bodySlot)
                player.back = (sbyte)EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);
        }
        /// <summary>
        /// 单件装备加成：生命/魔力上限、移速、通用伤害与暴击，
        /// 并置位霜冻屏障（frostBarrier）与弑神者反射（godSlayerReflect）标记
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.frostBarrier = true;        // 置位霜冻屏障标记（对应经典版 fBarrier）
            modPlayer.godSlayerReflect = true;    // 置位弑神者反射标记，供受击反弹结算
            player.statLifeMax2 += 400;           // 最大生命 +400
            player.statManaMax2 += 400;           // 最大魔力 +400
            player.moveSpeed += 0.25f;            // 移动速度 +25%
            player.GetCritChance<GenericDamageClass>() += 30;   // 通用暴击率 +30%
            player.GetDamage<GenericDamageClass>() += 0.3f;     // 通用伤害 +30%
        }
        /// <summary>
        /// 注册配方：现代版灾厄用金锭×20 + 宇宙砧，经典版灾厄用矿石与魂材 + 德雷顿熔炉，
        /// 两分支均以四套月后胸甲为坯料，经典版另需一件霜冻屏障（本模组自有的 FrostBarrier）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：四套胸甲 + AuricBar×20（宇宙砧）
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonBreastplate>();
                    recipe.AddIngredient<BloodflareBodyArmor>();
                    recipe.AddIngredient<SilvaArmor>();
                    recipe.AddIngredient<GodSlayerChestplate>();
                    recipe.AddIngredient(auricBar.Type, 20);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：四套胸甲 + 金矿石×100 + 各类月后魂材（德雷顿熔炉）
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
                    recipe1.AddIngredient<TarragonBreastplate>();
                    recipe1.AddIngredient<BloodflareBodyArmor>();
                    recipe1.AddIngredient<SilvaArmor>();
                    recipe1.AddIngredient<GodSlayerChestplate>();
                    recipe1.AddIngredient(auricOre.Type, 100);              // 金矿石×100
                    recipe1.AddIngredient(endothermicEnergy.Type, 30);      // 吸热能量×30
                    recipe1.AddIngredient(nightmareFuel.Type, 30);          // 梦魇燃料×30
                    recipe1.AddIngredient(phantoplasm.Type, 20);            // 幻影质×20
                    recipe1.AddIngredient(darksunFragment.Type, 15);        // 暗阳碎片×15
                    recipe1.AddIngredient(barofLife.Type, 10);              // 生命锭×10
                    recipe1.AddIngredient(hellcasterFragment.Type, 7);      // 地狱施法者碎片×7
                    recipe1.AddIngredient(coreofCalamity.Type, 5);          // 灾厄核心×5
                    recipe1.AddIngredient(galacticaSingularity.Type, 3);    // 银河奇点×3
                    recipe1.AddIngredient<FrostBarrier>();                                            // 霜冻屏障×1
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
