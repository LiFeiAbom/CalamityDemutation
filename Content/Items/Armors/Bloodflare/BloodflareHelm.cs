using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Bloodflare
{
    /// <summary>
    /// 血饮魔精盔（BloodflareHelm，英文名 Bloodflare Imp Mask） - 血炎套装的**盗贼**向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；现代版对应 BloodflareHeadRogue）。
    /// 注意与同目录另外三颗区分：<see cref="BloodflareMask"/> 近战 / <see cref="BloodflareHornedHelm"/> 射手 /
    /// <see cref="BloodflareHelmet"/> 召唤 / <see cref="BloodflareHornedMask"/> 法师，经典版里这颗 <c>BloodflareHelm</c> 才是盗贼头。
    /// 单件：岩浆免疫时长 +240、水中自由移动、盗贼伤害与盗贼暴击各 +10%。
    /// 套装（逐条对应 player.setBonus 的官方描述，实现位置见括号）：
    /// 1. 极大幅提升生命再生（player.crimsonRegen）
    /// 2. 生命低于 50% 的敌怪被击中时有几率掉红心、高于 50% 时掉魔力星（bloodflareSet → CalamityDemutationGlobalNPC）
    /// 3. 血月期间被击杀的敌怪更易掉血珠（bloodflareSet → CalamityDemutationGlobalNPC.OnKill）
    /// 4. 生命高于 80% 时 +30 防御与 +5% 盗贼暴击；低于 80% 时 +10% 盗贼伤害
    ///    （bloodflareThrowing → CalamityDemutationPlayer.PostUpdateMiscEffects）
    /// 5. 盗贼暴击有 50% 几率治疗你（bloodflareThrowing → CalamityDemutationGlobalProjectile.OnHitNPC，源只回 1 点生命）
    /// 6. 潜行上限 135（套装方法里经 <see cref="CDUtil.GrantRogueStealth"/> 补给灾厄侧）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class BloodflareHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金（与其余血炎部件一致）
            Item.defense = 28;                        // 防御 28（经典版值，源码同行另留 //85 注释，系开发期遗留数字）
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
        /// 套装激活时的视觉表现：绘制细微残影（与经典版 BloodflareHelm 一致；职业头都不画轮廓线）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
        }
        /// <summary>
        /// 套装激活：置位 bloodflareSet 与 bloodflareThrowing 标记，开启猩红回血，写入官方效果描述（setBonus），
        /// 并把潜行上限 135 补给灾厄侧。
        /// bloodflareSet 与近战/射手/召唤/法师头共用（红心与魔力星掉落、血月血珠）；
        /// bloodflareThrowing 单独驱动两条盗贼向效果（生命阈值加成、暴击回血）。
        /// 与近战头的差别（经典版原样）：不抬仇恨，故此处不加 aggro。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodflareSet = true;
            modPlayer.bloodflareThrowing = true;
            player.crimsonRegen = true;  // 猩红回血：极大幅提升生命再生
            player.setBonus = this.GetLocalizedValue("SetBonus");
            CDUtil.GrantRogueStealth(player, 1.35f);   // 潜行上限 135（源 rogueStealthMax = 1.35f；内部值 1f = 显示 100 点）
        }
        /// <summary>
        /// 单件装备加成：岩浆免疫时长、水下行动与盗贼伤害/暴击（经典版 UpdateEquip 原样；
        /// 源里盗贼那一档写的是 <c>CalamityCustomThrowingDamagePlayer.throwingDamage += 0.1f / throwingCrit += 10</c>）。
        /// 现代版把这两项加到真·盗贼伤害类上；经典版没有盗贼 DamageClass，另走反射桥。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.lavaMax += 240;                             // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                         // 水中不受移动减速
            DamageClass rogue = CDUtil.GetRogueDamageClass();  // 现代版 = CalamityMod/RogueDamageClass；拿不到才退到 tML 的 Throwing
            player.GetDamage(rogue) += 0.1f;                   // 盗贼伤害 +10%
            player.GetCritChance(rogue) += 10;                 // 盗贼暴击率 +10%
            CDUtil.AddClassicThrowingStats(player, 0.1f, 10);  // 经典版：写进它的自定义投掷字段（反射）
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，各注册一条（均在远古操纵机合成，与其余血炎头同规矩）
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
