using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Bloodflare
{
    /// <summary>
    /// 血魇九头盔（BloodflareHornedMask，英文名 Bloodflare Hydra Hood） - 血炎套装的法师向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；现代版对应 BloodflareHeadMagic）。
    /// 单件：岩浆免疫时长 +240、水中自由移动、魔法伤害与魔法暴击各 +10%、最大法力 +100。
    /// 套装（逐条对应 player.setBonus 的官方描述，实现位置见括号）：
    /// 1. 极大幅提升生命再生（player.crimsonRegen）
    /// 2. 生命低于 50% 的敌怪被击中时有几率掉红心、高于 50% 时掉魔力星（bloodflareSet → CalamityDemutationGlobalNPC）
    /// 3. 血月期间被击杀的敌怪更易掉血珠（bloodflareSet → CalamityDemutationGlobalNPC.OnKill）
    /// 4. 魔法武器射击时有 5% 几率追加幽灵魔弹（bloodflareMage → CalamityDemutationGlobalItem.Shoot）
    /// 5. 魔法暴击每 2 秒引发一次三连火焰爆炸（bloodflareMage → CalamityDemutationGlobalProjectile.OnHitNPC）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class BloodflareHornedMask:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素，照经典版源码）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金（与其余血炎部件一致）
            Item.defense = 22;                        // 防御 22（经典版值，源码同行另留 //85 注释，系开发期遗留数字）
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
        /// 套装激活时的视觉表现：绘制细微残影（与经典版 BloodflareHornedMask 一致；职业头都不画轮廓线）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
        }
        /// <summary>
        /// 套装激活：置位 bloodflareSet 与 bloodflareMage 标记，开启猩红回血，并写入官方效果描述（setBonus）。
        /// bloodflareSet 与近战/射手/召唤头共用（红心与魔力星掉落、血月血珠）；
        /// bloodflareMage 单独驱动两条法师向效果（幽灵魔弹、魔法暴击火焰爆炸）。
        /// 与近战头的差别（经典版原样）：不抬仇恨，故此处不加 aggro，也不置 bloodflareMelee。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodflareSet = true;
            modPlayer.bloodflareMage = true;
            player.crimsonRegen = true;  // 猩红回血：极大幅提升生命再生
            player.setBonus = this.GetLocalizedValue("SetBonus");
        }
        /// <summary>
        /// 单件装备加成：岩浆免疫时长、水下行动与魔法伤害/暴击/法力（经典版 UpdateEquip 原样）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.lavaMax += 240;                          // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                      // 水中不受移动减速
            player.GetDamage<MagicDamageClass>() += 0.1f;   // 魔法伤害 +10%
            player.GetCritChance<MagicDamageClass>() += 10; // 魔法暴击率 +10%
            player.statManaMax2 += 100;                     // 最大法力 +100
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
