using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿面甲（TarragonVisage） - 龙蒿套装的射手向头部部件（按经典版灾厄同名件 1:1 移植，对应现代版 TarragonHeadRanged）。
    /// 单件：远程伤害 +10%、远程暴击 +10%、减伤 +5%，另给 +240 岩浆免疫时长、液体中自由移动，
    /// 并免疫诅咒地狱/着火了/诅咒/冷冻。
    /// 与 1.4.4 公开源码的差异：按用户 2026-10-04 口径**不吸收**该版的「弹药消耗 -25%」与「暴击 +7」，
    /// 保持与其余职业头同一套口径（防御/价值同样取经典版值）。
    /// 套装效果（逐条对应 player.setBonus 的官方描述，实现位置见括号）：
    /// 1. 降低敌怪的生成速率（tarraSet → CalamityDemutationPlayer 里 Player.calmed = !tarraMelee）
    /// 2. 红心拾取范围提升（tarraSet → Player.lifeMagnet）
    /// 3. 敌怪死亡时有几率掉落额外红心（tarraSet → CalamityDemutationGlobalNPC 的掉落判定）
    /// 4. 远程暴击命中引发一场树叶爆炸（tarraRanged → CalamityDemutationPlayer.OnHitNPCWithProj）
    /// 5. 远程弹幕消失时有几率分裂出生命能量（tarraRanged → CalamityDemutationGlobalProjectile.OnKill）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class TarragonVisage:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 50, 0, 0);  // 价值 50 金（与其余龙蒿部件一致）
            Item.defense = 21;                        // 防御 21（经典版值，源码同行另留 //98 注释，系开发期遗留数字）
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
        /// 套装生效时的角色描边：开启柔和描边与轮廓线（与经典版 TarragonVisage 一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
            player.armorEffectDrawOutlines = true;
        }
        /// <summary>
        /// 套装效果：置位 tarraSet / tarraRanged 标记，并把本地化套装描述（hjson 的 SetBonus 键）写入显示文本。
        /// tarraSet 负责刷怪率、红心磁吸与红心掉落；tarraRanged 负责远程暴击的树叶爆炸与远程弹幕消失时的生命能量分裂。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;     // 套装标记（刷怪率/红心拾取范围/额外红心掉落）
            modPlayer.tarraRanged = true;  // 远程侧标记（暴击树叶爆炸 / 弹幕消失分裂生命能量）
            player.setBonus = this.GetLocalizedValue("SetBonus");
        }
        /// <summary>
        /// 穿戴时的属性加成：远程面板、减伤、岩浆与水下生存、若干 debuff 免疫（经典版 TarragonVisage 的 UpdateEquip 原样）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<RangedDamageClass>() += 0.1f;    // 远程伤害 +10%
            player.GetCritChance<RangedDamageClass>() += 10;  // 远程暴击率 +10%
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
        /// 与近战头同规矩：现代分支取 1.4.4 公开源码的数量（锭 ×12），经典分支照经典版源码（锭 ×7）
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
