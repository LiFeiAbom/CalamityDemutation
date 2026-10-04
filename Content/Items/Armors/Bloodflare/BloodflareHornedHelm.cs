using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Bloodflare
{
    /// <summary>
    /// 血弑魔颅盔（BloodflareHornedHelm） - 血炎套装的射手向头部部件
    /// （按经典版灾厄同名件 1:1 移植；现代版类名 BloodflareHeadRanged，现名 Bloodflare Demon Helm）。
    /// 单件：岩浆免疫时长 +240、水中自由移动、远程伤害与暴击率各 +10%。
    /// 套装（逐条对应 player.setBonus 的官方描述，实现位置见括号）：
    /// 1. 极大幅提升生命再生 / 敌怪掉落红心与魔力星 / 血月血珠（bloodflareSet → CalamityDemutationPlayer 与 GlobalNPC）
    /// 2. 按 [键] 释放波尔特加斯特的迷失灵魂（bloodflareRanged → CalamityDemutationPlayer 的按键块，
    ///    一次喷出 16 枚 BloodflareSoul，30 秒冷却）
    /// 3. 远程武器有几率射出血液爆炸光球（bloodflareRanged → CalamityDemutationGlobalItem.Shoot 里 2% 概率追加 BloodBomb）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class BloodflareHornedHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金（与其余血炎部件一致）
            Item.defense = 34;                        // 防御 34（经典版值，源码同行另留 //85 注释，系开发期遗留数字）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后自定义稀有度 13 级
        }
        /// <summary>
        /// 判定是否集齐血炎套三件（胸甲 + 护腿）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BloodflareBodyArmor>() && legs.type == ModContent.ItemType<BloodflareCuisses>();
        }
        /// <summary>
        /// 套装激活时的视觉表现：绘制细微残影（与经典版 BloodflareHornedHelm 一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
        }
        /// <summary>
        /// 套装激活：置位 bloodflareSet 与 bloodflareRanged 标记，并写入官方效果描述（setBonus）。
        /// bloodflareSet 与近战头共用（红心/魔力星计时、血月血珠掉落）；
        /// bloodflareRanged 单独驱动两条远程向效果（按键灵魂爆发、射击追加血液爆炸光球）。
        /// 与近战头的差别：经典版远程头的套装文本没有「敌人更倾向以你为目标」，故此处不抬仇恨。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodflareSet = true;
            modPlayer.bloodflareRanged = true;
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.TarragonKeyDisplay);
            player.crimsonRegen = true;  // 猩红回血：提升生命回复
        }
        /// <summary>
        /// 单件装备加成：岩浆免疫时长、水下行动与远程伤害/暴击（经典版 UpdateEquip 原样）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.lavaMax += 240;                            // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                        // 水中不受移动减速
            player.GetDamage<RangedDamageClass>() += 0.1f;    // 远程伤害 +10%
            player.GetCritChance<RangedDamageClass>() += 10;  // 远程暴击率 +10%
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，各注册一条（均在远古操纵机合成，与近战头同规矩）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄材料：Bloodstone×25、BloodOrb×10、RuinousSoul×2
                if (calamity.TryFind<ModItem>("Bloodstone", out ModItem bloodstone)
                    && calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb)
                    && calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(bloodstone.Type, 25);
                    recipe.AddIngredient(bloodOrb.Type, 10);
                    recipe.AddIngredient(ruinousSoul.Type, 2);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄材料：BloodstoneCore×11、RuinousSoul×2
                if (classic.TryFind<ModItem>("BloodstoneCore", out ModItem bloodstoneCore)
                    && classic.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(bloodstoneCore.Type, 11);
                    recipeClassic.AddIngredient(classicRuinousSoul.Type, 2);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
