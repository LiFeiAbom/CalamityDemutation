using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿面具（TarragonMask） - 龙蒿套装的法师向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；现代版对应 TarragonHeadMagic）。
    /// 单件：魔法伤害 +10%、魔法暴击 +10%、减伤 +5%、最大法力 +100，
    /// 另有 +240 岩浆免疫时长、液体中自由移动，并免疫诅咒地狱/着火了/诅咒/冷冻。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 降低敌怪的生成速率（tarraSet → CalamityDemutationPlayer 里 Player.calmed = !tarraMelee）
    /// 2. 红心拾取范围提升（tarraSet → Player.lifeMagnet）
    /// 3. 敌怪死亡时有几率掉落额外红心（tarraSet → CalamityDemutationGlobalNPC 的掉落判定）
    /// 4. 每第 5 次魔法暴击射出一阵叶暴风（tarraMage → 计数在 CalamityDemutationGlobalProjectile.OnHitNPC，
    ///    触发在 CalamityDemutationGlobalItem.Shoot）
    /// 5. 魔法弹幕命中敌人时按伤害回血（tarraMage → CalamityDemutationGlobalProjectile.OnHitNPC；
    ///    源为 90 帧冷却——tooltip 的「50% 几率」在经典版源码里没有对应随机骰，本工程照源实现、保留原文案）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class TarragonMask:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素，照经典版源码；贴图实际为 22×30）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 50, 0, 0);  // 价值 50 金（与其余龙蒿部件一致）
            Item.defense = 14;                        // 防御 14（用户 2026-10-06 指定；源经典/现代都是 10，源码同行另留 //98 注释）
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
        /// 套装生效时的角色描边：开启柔和描边与轮廓线（与经典版 TarragonMask 一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
            player.armorEffectDrawOutlines = true;
        }
        /// <summary>
        /// 套装效果：置位 tarraSet / tarraMage 标记，并把本地化套装描述（hjson 的 SetBonus 键）写入显示文本。
        /// tarraSet 负责刷怪率、红心磁吸与红心掉落；tarraMage 负责叶暴风计数与法弹命中回血。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;    // 套装标记（刷怪率/红心拾取范围/额外红心掉落）
            modPlayer.tarraMage = true;   // 法师侧标记（叶暴风 / 法弹回血）
            player.setBonus = this.GetLocalizedValue("SetBonus");
        }
        /// <summary>
        /// 穿戴时的属性加成：魔法面板、减伤、法力、岩浆与水下生存、若干 debuff 免疫
        /// （经典版 TarragonMask 的 UpdateEquip 原样）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<MagicDamageClass>() += 0.1f;     // 魔法伤害 +10%
            player.GetCritChance<MagicDamageClass>() += 10;    // 魔法暴击率 +10%
            player.endurance += 0.05f;                         // 伤害减免 +5%
            player.statManaMax2 += 100;                        // 最大法力 +100
            player.lavaMax += 240;                             // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                         // 水中不受移速/跳跃惩罚
            player.buffImmune[BuffID.CursedInferno] = true;    // 免疫诅咒地狱
            player.buffImmune[BuffID.OnFire] = true;           // 免疫着火了
            player.buffImmune[BuffID.Cursed] = true;           // 免疫诅咒
            player.buffImmune[BuffID.Chilled] = true;          // 免疫冷冻
        }
        /// <summary>
        /// 配方：现代版与经典版灾厄材料不同，各注册一条（均使用原版合成站 TileID.LunarCraftingStation，
        /// 与其余龙蒿头同规矩）
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
