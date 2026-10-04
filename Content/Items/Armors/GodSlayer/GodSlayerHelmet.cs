using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 弑神者战盔（GodSlayerHelmet） - 弑神者套装的射手向头部部件
    /// （按经典版灾厄同名件 1:1 移植；现代版类名 GodSlayerHeadRanged，显示名同为 God Slayer Helmet）。
    /// 单件：远程伤害与暴击率各 +14%。
    /// 套装（逐条对应 player.setBonus 的官方描述，实现位置见括号）：
    /// 1. 致命伤不会死亡并回复生命 / 每 45 秒一次 / 冷却期间 +10% 全伤害（godSlayer → CalamityDemutationPlayer.PreKill）
    /// 2. 远程暴击有几率再次暴击、造成 4 倍伤害（godSlayerRanged → CalamityDemutationPlayer.ModifyHitNPCWithProj）
    /// 3. 发射远程武器时有几率射出弑神者破片弹（godSlayerRanged → CalamityDemutationGlobalItem.Shoot，5% 概率、伤害 ×2.1）
    /// 4. 弑神者冲刺（默认 H，见 CalamityDemutationPlayer.GodSlayerDash.cs）
    /// 与近战头的差别（经典版原样）：射手头不置 godSlayerDamage（≤80 压制）也不加荆棘，
    /// 故套装文本相应比近战头少两行。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class GodSlayerHelmet:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度（防御取经典版射手头的 35，比近战头的 48 低）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 75, 0, 0);  // 价值 75 金（与其余弑神者部件一致）
            Item.defense = 35;                        // 防御 35（经典版值，源码同行另留 //96 注释，系开发期遗留数字）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后稀有度 14
        }
        /// <summary>
        /// 判定套装：头部 + 弑神者胸甲 + 弑神者护腿
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<GodSlayerChestplate>() && legs.type == ModContent.ItemType<GodSlayerLeggings>();
        }
        /// <summary>
        /// 套装激活时的拖影特效（与近战头一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装激活：置位 godSlayer（致命保护 + 冲刺闸门）与 godSlayerRanged（两条远程向效果）。
        /// 两者都在 CalamityDemutationPlayer / CalamityDemutationGlobalItem 中结算。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.godSlayer = true;
            modPlayer.godSlayerRanged = true;
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay);
        }
        /// <summary>
        /// 单件属性：远程伤害 / 远程暴击各 +14%（与近战头同值、不同职业）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<RangedDamageClass>() += 0.14f;    // +14% 远程伤害
            player.GetCritChance<RangedDamageClass>() += 14;   // +14% 远程暴击
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，各注册一条。
        /// 现代分支与既有近战头、1.4.4 公开源码一致（CosmiliteBar×10 + AscendantSpiritEssence×2 @ 宇宙砧）；
        /// 经典分支照经典版源码（CosmiliteBar×14 + NightmareFuel×8 + EndothermicEnergy×8 @ 德雷顿熔炉）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(cosmiliteBar.Type, 10);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 2);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(classicCosmiliteBar.Type, 14);
                    recipeClassic.AddIngredient(nightmareFuel.Type, 8);
                    recipeClassic.AddIngredient(endothermicEnergy.Type, 8);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
