using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿头盔（TarragonHelmet） - 龙蒿套装的**盗贼**向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；现代版对应 TarragonHeadRogue）。
    /// 注意与同目录另外两颗区分：<see cref="TarragonHelm"/> 是近战头、<see cref="TarragonMask"/> 是法师头，
    /// 经典版里这颗 <c>TarragonHelmet</c> 才是盗贼头。
    /// 单件：盗贼伤害 +10%、盗贼暴击 +10%、减伤 +5%，另有 +240 岩浆免疫时长、液体中自由移动，
    /// 并免疫诅咒地狱/着火了/诅咒/冷冻。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 降低敌怪的生成速率（tarraSet → CalamityDemutationPlayer 里 Player.calmed = !tarraMelee）
    /// 2. 红心拾取范围提升（tarraSet → Player.lifeMagnet）
    /// 3. 敌怪死亡时有几率掉落额外红心（tarraSet → CalamityDemutationGlobalNPC 的掉落判定）
    /// 4. 每 25 次盗贼暴击获得 5 秒免伤、冷却 30 秒
    ///    （tarraThrowing → 计数在 CalamityDemutationGlobalProjectile.OnHitNPC，触发在 PostUpdateMiscEffects）
    /// 5. 带减益时 +10% 盗贼伤害（tarraThrowing → PostUpdateMiscEffects；源按减益槽逐条累加，见那里的注释）
    /// 6. 潜行上限 130（套装方法里经 <see cref="CDUtil.GrantRogueStealth"/> 补给灾厄侧）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class TarragonHelmet:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 50, 0, 0);  // 价值 50 金（与其余龙蒿部件一致）
            Item.defense = 15;                        // 防御 15（经典版值，源码同行另留 //98 注释，系开发期遗留数字）
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
        /// 套装生效时的角色描边：开启柔和描边与轮廓线
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
            player.armorEffectDrawOutlines = true;
        }
        /// <summary>
        /// 套装效果：置位 tarraSet / tarraThrowing 标记，把潜行上限 130 补给灾厄侧，
        /// 并把本地化套装描述（hjson 的 SetBonus 键）写入显示文本。
        /// 潜行走 <see cref="CDUtil.GrantRogueStealth"/>（现代版官方 Mod.Call、经典版反射），
        /// 它与灾厄每帧清零的时机关系见该方法的注释。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;       // 套装标记（刷怪率/红心拾取范围/额外红心掉落）
            modPlayer.tarraThrowing = true;  // 盗贼侧标记（25 次暴击免伤 / 带减益加伤 / 潜行上限）
            player.setBonus = this.GetLocalizedValue("SetBonus");
            CDUtil.GrantRogueStealth(player, 1.3f);   // 潜行上限 130（源 rogueStealthMax = 1.3f；内部值 1f = 显示 100 点）
        }
        /// <summary>
        /// 穿戴时的属性加成：盗贼面板、减伤、岩浆与水下生存、若干 debuff 免疫
        /// （经典版 TarragonHelmet 的 UpdateEquip 原样；盗贼数值那一档源里写的是
        /// <c>CalamityCustomThrowingDamagePlayer.throwingDamage += 0.1f / throwingCrit += 10</c>）。
        /// 现代版把这两项加到真·盗贼伤害类上；经典版没有盗贼 DamageClass，另走反射桥。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            DamageClass rogue = CDUtil.GetRogueDamageClass();  // 现代版 = CalamityMod/RogueDamageClass；拿不到才退到 tML 的 Throwing
            player.GetDamage(rogue) += 0.1f;                   // 盗贼伤害 +10%
            player.GetCritChance(rogue) += 10;                 // 盗贼暴击率 +10%
            CDUtil.AddClassicThrowingStats(player, 0.1f, 10);  // 经典版：写进它的自定义投掷字段（反射）
            player.endurance += 0.05f;                         // 伤害减免 +5%
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
