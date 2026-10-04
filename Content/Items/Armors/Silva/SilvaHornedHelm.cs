using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Silva
{
    /// <summary>
    /// 始源林海角盔（SilvaHornedHelm） - 始源林海套装的射手向头部部件
    /// （按经典版灾厄同名件 1:1 移植；现代版灾厄已无此件，CI 对应件为 SilvaHeadRanged）。
    /// 单件：远程伤害 +13%、远程暴击 +13%。
    /// 套装（逐条对应 player.setBonus 的官方描述，实现位置见括号）：
    /// 1. 免疫几乎所有减益（silvaSet → CalamityDemutationPlayer.PostUpdateMiscEffects）
    /// 2. 所有弹幕命中敌人时生成治疗叶球（silvaSet → CalamityDemutationGlobalProjectile）
    /// 3. 最大奔跑速度与加速度 +5%（silvaSet → CalamityDemutationPlayer.PostUpdateRunSpeeds）
    /// 4. 生命被压到 1 点时 10 秒内不会因任何后续伤害死亡（silvaSet → CalamityDemutationPlayer.PreKill）
    /// 5. 该效果生效期间再次被压到 1 点生命会失去 100 最大生命
    /// 6. 每条命只触发一次，最大生命降至 400 时无敌效果停止
    /// 7. 死亡后最大生命恢复正常
    /// 8. 提高所有远程武器的射速（silvaRanged → CalamityDemutationPlayer.PostUpdateMiscEffects，+10% 远程攻速）
    /// 9. 始源林海无敌期间远程武器伤害 +40%（silvaRanged → CalamityDemutationPlayer.ModifyHitNPCWithProj）
    /// 与近战头的差别（经典版原样）：射手头不置 silvaMelee，故套装文本相应比近战头少三行近战专属项。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class SilvaHornedHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度（防御取经典版射手头的 36，比近战头的 52 低）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素，照经典版源码；贴图实际为 26×24）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 价值 90 金（与其余始源林海部件一致）
            Item.defense = 36;                        // 防御 36（经典版值，源码同行另留 //110 注释，系开发期遗留数字）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后自定义稀有度 15 级
        }
        /// <summary>
        /// 判定套装：头部 + 始源林海盔甲 + 始源林海护胫
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<SilvaArmor>() && legs.type == ModContent.ItemType<SilvaLeggings>();
        }
        /// <summary>
        /// 套装激活时的角色拖影特效（与近战头一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：置位 silvaSet（通用套装效果）与 silvaRanged（两条远程向效果）。
        /// 两者都在 CalamityDemutationPlayer / CalamityDemutationGlobalProjectile 中结算。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.silvaSet = true;     // 套装标记（免疫 debuff/免死无敌/吸血叶球/移速）
            modPlayer.silvaRanged = true;  // 远程侧标记（+10% 远程攻速 / 无敌期间 +40% 远程伤害）
            player.setBonus = this.GetLocalizedValue("SetBonus");
        }
        /// <summary>
        /// 穿戴时的属性加成：远程伤害 / 远程暴击各 +13%（与近战头同值、不同职业）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<RangedDamageClass>() += 0.13f;    // 远程伤害 +13%
            player.GetCritChance<RangedDamageClass>() += 13;   // 远程暴击率 +13%
        }
        /// <summary>
        /// 配方：现代版与经典版灾厄材料不同，分别注册两套配方，均需本模组材料 LeadCore。
        /// 与既有近战头逐字一致（经典分支照经典版源码；现代分支沿用工程既有写法）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：在 CosmicAnvil 处用 PlantyMush/EffulgentFeather/AscendantSpiritEssence 合成
                if (calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush)
                    && calamity.TryFind<ModItem>("EffulgentFeather", out ModItem effulgentFeather)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(plantyMush.Type, 30);
                    recipe.AddIngredient(effulgentFeather.Type, 8);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 2);
                    recipe.AddIngredient<LeadCore>();
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄：在 DraedonsForge 处用 DarksunFragment/EffulgentFeather/CosmiliteBar 等合成
                if (classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && classic.TryFind<ModItem>("EffulgentFeather", out ModItem classicEffulgentFeather)
                    && classic.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && classic.TryFind<ModItem>("Tenebris", out ModItem tenebris)
                    && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(darksunFragment.Type, 5);
                    recipeClassic.AddIngredient(classicEffulgentFeather.Type, 5);
                    recipeClassic.AddIngredient(cosmiliteBar.Type, 5);
                    recipeClassic.AddIngredient(tenebris.Type, 6);
                    recipeClassic.AddIngredient(nightmareFuel.Type, 14);
                    recipeClassic.AddIngredient(endothermicEnergy.Type, 14);
                    recipeClassic.AddIngredient<LeadCore>();
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
