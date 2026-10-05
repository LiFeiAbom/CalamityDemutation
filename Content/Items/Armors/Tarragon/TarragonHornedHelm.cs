using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿角盔（TarragonHornedHelm） - 龙蒿套装的召唤向头部部件
    ///（按经典版灾厄同名件 1:1 移植，对应现代版 TarragonHeadSummon）。
    /// 单件：仆从上限 +3、伤害减免 +5%，另给 +240 岩浆免疫时长、液体中自由移动，
    /// 并免疫诅咒地狱/着火了/诅咒/冷冻。
    /// 另有用户 2026-10-05 指定的三条额外单件属性：召唤伤害 +10%、鞭子攻击范围 +10%、鞭子攻击速度 +10%
    ///（作为"召唤头"的统一口径，后续同类件照此办理）。
    /// 防御按用户 2026-10-05 指定取 **7**（经典版与现代版源码都写 `Item.defense = 3; //98`，未照搬 3）。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 召唤伤害 +50%（本类 UpdateArmorSet 内直接加，经典版原样）
    /// 2. 降低敌怪的生成速率（tarraSet → CalamityDemutationPlayer 里 Player.calmed = !tarraMelee）
    /// 3. 红心拾取范围提升（tarraSet → Player.lifeMagnet）
    /// 4. 敌怪死亡时有几率掉落额外红心（tarraSet → CalamityDemutationGlobalNPC 的掉落判定）
    /// 5. 满血时额外 +2 仆从上限与 +10% 召唤伤害（tarraSummon → PostUpdateMiscEffects）
    /// 6. 召唤环绕自身的生命光环，持续伤害附近敌怪（tarraSummon → PostUpdateMiscEffects，每 80 帧结算一次）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class TarragonHornedHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 50, 0, 0);  // 价值 50 金（与其余龙蒿部件一致）
            Item.defense = 7;                         // 防御 7（用户指定；源经典/现代都是 3）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;  // 月后自定义稀有度 12 级
        }
        /// <summary>
        /// 判定是否凑齐龙蒿三件套（头/胸/腿均为龙蒿部件）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<TarragonBreastplate>() && legs.type == ModContent.ItemType<TarragonLeggings>();
        }
        /// <summary>
        /// 套装生效时的角色描边：开启柔和描边与轮廓线（与经典版 TarragonHornedHelm 一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
            player.armorEffectDrawOutlines = true;
        }
        /// <summary>
        /// 套装效果：置位 tarraSet / tarraSummon 标记，给 +50% 召唤伤害，
        /// 并把本地化套装描述（hjson 的 SetBonus 键）写入显示文本。
        /// tarraSet 负责刷怪率、红心磁吸与红心掉落；tarraSummon 负责绿色光照、生命光环与满血加成。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;     // 套装标记（刷怪率/红心拾取范围/额外红心掉落）
            modPlayer.tarraSummon = true;  // 召唤侧标记（绿色光照 / 生命光环 / 满血加成）
            player.GetDamage<SummonDamageClass>() += 0.5f;  // 召唤伤害 +50%（经典版 UpdateArmorSet 原样）
            player.setBonus = this.GetLocalizedValue("SetBonus");
        }
        /// <summary>
        /// 穿戴时的属性加成：仆从上限、减伤、岩浆与水下生存、若干 debuff 免疫
        /// （经典版 TarragonHornedHelm 的 UpdateEquip 原样）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 3;                           // 仆从上限 +3
            player.GetDamage<SummonDamageClass>() += 0.1f;    // 召唤伤害 +10%（用户指定的额外单件属性）
            player.whipRangeMultiplier += 0.1f;               // 鞭子攻击范围 +10%（同上）
            player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.1f;  // 鞭子攻击速度 +10%（同上；鞭子走 SummonMeleeSpeed 攻速类）
            player.endurance += 0.05f;                        // 伤害减免 +5%
            player.lavaMax += 240;                            // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                        // 水中不受移速/跳跃惩罚
            player.buffImmune[BuffID.CursedInferno] = true;   // 免疫诅咒地狱
            player.buffImmune[BuffID.OnFire] = true;          // 免疫着火了
            player.buffImmune[BuffID.Cursed] = true;          // 免疫诅咒
            player.buffImmune[BuffID.Chilled] = true;         // 免疫冷冻
        }
        /// <summary>
        /// 配方：现代版与经典版灾厄材料名不同，各注册一条（均使用原版合成站 TileID.LunarCraftingStation）。
        /// 与其余龙蒿头同规矩：现代分支取 1.4.4 公开源码的数量（锭 ×12），经典分支照经典版源码（锭 ×7）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄配方（UelibloomBar + DivineGeode）
                if (calamity.TryFind<ModItem>("UelibloomBar", out ModItem uelibloomBar)
                    && calamity.TryFind<ModItem>("DivineGeode", out ModItem divineGeode))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(uelibloomBar.Type, 12);
                    recipe.AddIngredient(divineGeode.Type, 6);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄配方（UeliaceBar + DivineGeode）
                if (classic.TryFind<ModItem>("UeliaceBar", out ModItem ueliaceBar)
                    && classic.TryFind<ModItem>("DivineGeode", out ModItem classicDivineGeode))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ueliaceBar.Type, 7);
                    recipeClassic.AddIngredient(classicDivineGeode.Type, 6);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
