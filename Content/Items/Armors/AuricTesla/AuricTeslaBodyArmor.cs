using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    /// <summary>
    /// 金之特斯拉胸甲（Auric Tesla Body Armor） - 金之特斯拉套（AuricTesla）身体防具
    /// 提供生命上限、通用伤害与暴击、移速，并置位霜冻屏障与弑神者反射标记；
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
        /// 单件装备加成：生命上限、移速、通用伤害与暴击，
        /// 并置位霜冻屏障（frostBarrier）与弑神者反射（godSlayerReflect）标记
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.frostBarrier = true;        // 置位霜冻屏障标记（对应经典版 fBarrier）
            // 置位弑神者反射标记：FreeDodge 结算完全免伤（膨胀开启 5%、关闭源值 2%）；
            // 低伤压制（≤80 → 1）跟随整套，由近战头的套装方法置 godSlayerDamage
            modPlayer.godSlayerReflect = true;
            bool legacy = ConfigSystem.StatInflationEnabled;
            player.statLifeMax2 += legacy ? LegacyStatLifeMax2 : 100;   // 最大生命：常态 +100 / 膨胀 +400（削弱前旧值）
            if (legacy)
            {
                player.statManaMax2 += LegacyStatManaMax2;              // 最大魔力 +400（削弱时整条删除，膨胀时恢复）
            }
            player.moveSpeed += 0.25f;            // 移动速度 +25%
            player.GetCritChance<GenericDamageClass>() += legacy ? LegacyCritChance : 22;   // 通用暴击率：常态 +22% / 膨胀 +30%
            player.GetDamage<GenericDamageClass>() += legacy ? LegacyDamageBonus : 0.22f;    // 通用伤害：常态 +22% / 膨胀 +30%
        }
        /// <summary>数值膨胀开关开启时恢复的旧版生命上限（2026-09-27「400 → 100」那次削弱的原值）</summary>
        private const int LegacyStatLifeMax2 = 400;
        /// <summary>数值膨胀开关开启时恢复的旧版魔力上限（2026-09-27 削弱时整条删除的原值）</summary>
        private const int LegacyStatManaMax2 = 400;
        /// <summary>数值膨胀开关开启时恢复的旧版通用暴击率（2026-09-27「30 → 22」那次削弱的原值）</summary>
        private const int LegacyCritChance = 30;
        /// <summary>数值膨胀开关开启时恢复的旧版通用伤害（2026-09-27「30% → 22%」那次削弱的原值）</summary>
        private const float LegacyDamageBonus = 0.30f;
        /// <summary>
        /// 说明文字：数值膨胀开启时换成 TooltipInflated（最大生命 +400、最大魔力 +400、
        /// 伤害与暴击 30%、5% 完全免伤），关闭时用 Tooltip 的源值（+100、22%、2%）。
        /// 本件防御不随膨胀变化，故不传 defenseBonus。
        /// </summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.ApplyInflatedTooltip(this);
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
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄：四套胸甲 + 金矿石×100 + 各类月后魂材（德雷顿熔炉）
                if (classic.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                    && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && classic.TryFind<ModItem>("BarofLife", out ModItem barofLife)
                    && classic.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment)
                    && classic.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity)
                    && classic.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<TarragonBreastplate>();
                    recipeClassic.AddIngredient<BloodflareBodyArmor>();
                    recipeClassic.AddIngredient<SilvaArmor>();
                    recipeClassic.AddIngredient<GodSlayerChestplate>();
                    recipeClassic.AddIngredient(auricOre.Type, 100);              // 金矿石×100
                    recipeClassic.AddIngredient(endothermicEnergy.Type, 30);      // 吸热能量×30
                    recipeClassic.AddIngredient(nightmareFuel.Type, 30);          // 梦魇燃料×30
                    recipeClassic.AddIngredient(phantoplasm.Type, 20);            // 幻影质×20
                    recipeClassic.AddIngredient(darksunFragment.Type, 15);        // 暗阳碎片×15
                    recipeClassic.AddIngredient(barofLife.Type, 10);              // 生命锭×10
                    recipeClassic.AddIngredient(hellcasterFragment.Type, 7);      // 地狱施法者碎片×7
                    recipeClassic.AddIngredient(coreofCalamity.Type, 5);          // 灾厄核心×5
                    recipeClassic.AddIngredient(galacticaSingularity.Type, 3);    // 银河奇点×3
                    recipeClassic.AddIngredient<FrostBarrier>();                                            // 霜冻屏障×1
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
