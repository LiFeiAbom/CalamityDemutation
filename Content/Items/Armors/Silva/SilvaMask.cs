using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Silva
{
    /// <summary>
    /// 始源林海面具（SilvaMask，英文名 Silva Mask） - 始源林海套装的**盗贼**向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；CI 对应件 SilvaHeadRogue）。
    /// 注意与同目录另外四颗区分：<see cref="SilvaHelm"/> 近战 / <see cref="SilvaHornedHelm"/> 射手 /
    /// <see cref="SilvaHelmet"/> 召唤 / <see cref="SilvaMaskedCap"/> 法师，经典版里这颗 <c>SilvaMask</c> 才是盗贼头。
    /// 单件：盗贼伤害 +13%、盗贼暴击 +13%。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1~7. 通用七条（免疫减益 / 治疗叶球 / 移速加速度 +5% / 免死无敌四连）——silvaSet，
    ///      与其余四颗头共用同一套实现（见 SilvaHelm / CalamityDemutationPlayer）
    /// 8. 生命高于 50% 时盗贼武器投掷更快（silvaThrowing → 本类 UpdateArmorSet）
    /// 9. 无敌窗口结束后盗贼武器伤害 +10%（silvaThrowing → CalamityDemutationPlayer.ModifyHitNPCWithProj）
    /// 10. 潜行上限 150（套装方法里经 <see cref="CDUtil.GrantRogueStealth"/> 补给灾厄侧）
    /// 另有一条**源里的隐藏项**（不在 tooltip 内）：生命 >50% 且盗贼暴击时伤害 ×1.25，
    /// 且被 `auricSet` 门控（见 CalamityDemutationGlobalProjectile.OnHitNPC 的注释）。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class SilvaMask:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素，照经典版源码；贴图实际为 26×24）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 价值 90 金（与其余始源林海部件一致）
            Item.defense = 30;                        // 防御 30（经典版值，源码同行另留 //110 注释，系开发期遗留数字）
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
        /// 套装激活时的角色拖影特效（与其余始源林海头一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：置位 silvaSet（通用套装效果）与 silvaThrowing（盗贼侧），把潜行上限 150 补给灾厄侧，
        /// 写入本地化套装描述；并实现「生命高于 50% 时盗贼武器投掷更快」。
        /// <para>
        /// 关于投掷速率：经典版写在 <c>UseTimeMultiplier</c> 里返回 **1.1f**（tML 里该值 &gt;1 表示"更慢"，
        /// 与 tooltip 的"faster"相反）。本工程沿用射手头当年的处置——**从 CI**：CI 的 SilvaHeadRogue
        /// 用 <c>GetAttackSpeed&lt;RogueDamageClass&gt;() += 0.1f</c>，此处按同样口径写盗贼攻速 +10%。
        /// 判据也照 CI：生命 &gt;50%、手持武器属于盗贼类、`useTime &gt; 3`。
        ///（注意经典版侧没有盗贼 DamageClass，只装经典版时手持无类型武器不会命中该判据。）
        /// </para>
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.silvaSet = true;       // 套装标记（免疫 debuff/免死无敌/吸血叶球/移速）
            modPlayer.silvaThrowing = true;  // 盗贼侧标记（投掷速率 / 无敌后加伤 / 隐藏暴击加伤）
            player.setBonus = this.GetLocalizedValue("SetBonus");
            CDUtil.GrantRogueStealth(player, 1.5f);   // 潜行上限 150（源 rogueStealthMax = 1.5f；内部值 1f = 显示 100 点）
            DamageClass rogue = CDUtil.GetRogueDamageClass();
            if (player.statLife > (int)(player.statLifeMax2 * 0.5) && player.HeldItem.DamageType == rogue && player.HeldItem.useTime > 3)
            {
                player.GetAttackSpeed(rogue) += 0.1f;   // 生命 >50% 且持盗贼武器：盗贼攻速 +10%（CI 口径）
            }
        }
        /// <summary>
        /// 穿戴时的属性加成：盗贼伤害与暴击各 +13%（经典版 UpdateEquip 原样；
        /// 源里写的是 <c>CalamityCustomThrowingDamagePlayer.throwingDamage += 0.13f / throwingCrit += 13</c>）。
        /// 现代版加在真·盗贼伤害类上；经典版没有盗贼 DamageClass，另走反射桥。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            DamageClass rogue = CDUtil.GetRogueDamageClass();  // 现代版 = CalamityMod/RogueDamageClass；拿不到才退到 tML 的 Throwing
            player.GetDamage(rogue) += 0.13f;                  // 盗贼伤害 +13%
            player.GetCritChance(rogue) += 13;                 // 盗贼暴击率 +13%
            CDUtil.AddClassicThrowingStats(player, 0.13f, 13); // 经典版：写进它的自定义投掷字段（反射）
        }
        /// <summary>
        /// 配方：与其余始源林海头部逐字一致（经典分支照经典版源码；现代分支沿用工程既有写法），均需本模组材料 LeadCore
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
