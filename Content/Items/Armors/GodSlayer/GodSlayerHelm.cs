using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 神裁者头盔（GodSlayerHelm） - 弑神者套头部，近战特化
    /// 单件：+14% 近战伤害、+14% 近战暴击、+20% 近战攻击速度。
    /// 套装效果（player.setBonus 官方描述逐条）：
    /// 致命伤时不会死亡并回复 300 生命，该效果每 45 秒只能触发一次；
    /// 该效果冷却期间获得 +10% 全伤害；
    /// 单次受到超过 80 伤害时会释放一群高伤害的弑神飞镖；
    /// 敌人攻击你时会受到大量反伤；
    /// 原本伤害不超过 80 的攻击会被削减到 1。
    /// 弑神者冲刺（默认 H 键）由本模组自持实现，见 CalamityDemutationPlayer.GodSlayerDash.cs。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class GodSlayerHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 75, 0, 0);  // 价值 75 金
            Item.defense = 48; //96                  // 48 为当前防御值，//96 为原值记录（保留原义）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后稀有度 14（蓝色名）
        }
        /// <summary>
        /// 判定套装：头部 + 神裁者胸甲 + 神裁者护腿
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<GodSlayerChestplate>() && legs.type == ModContent.ItemType<GodSlayerLeggings>();
        }
        /// <summary>
        /// 套装激活时的拖影特效
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;  // 开启盔甲拖影
        }
        /// <summary>
        /// 套装激活：置位 godSlayer（致命保护 + 45 秒冷却增伤，同时作为弑神者冲刺的穿戴闸门）
        /// 与 godSlayerMelee（受击超 80 释放弑神飞镖），两者均在 CalamityDemutationPlayer 中结算；同时提升反伤。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.godSlayer = true;
            modPlayer.godSlayerMelee = true;
            player.setBonus = "\n" +
                "You will survive fatal damage and will be healed 300 HP if an attack would have killed you\n" +
                "This effect can only occur once every 45 seconds\n" +
                "While the cooldown for this effect is active you gain a 10% increase to all damage\n" +
                "Taking over 80 damage in one hit will cause you to release a swarm of high-damage god killer darts\n" +
                "Enemies take a lot of damage when they hit you\n" +
                "An attack that would deal 80 damage or less will have its damage reduced to 1\n" +
                $"Press [{KeybindsSystem.GodslayerDashKeyDisplay}] to dash towards the cursor\n" +
                "Dealing great damage and inflicting the God Slayer Inferno debuff on contact\n" +
                "This effect has a 45-second cooldown\n";
            player.thorns += 2.5f;  // 反伤倍率 +2.5（与胸甲的 +0.9 叠加）
        }
        /// <summary>
        /// 单件属性：近战伤害 / 近战暴击 / 近战攻击速度
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<MeleeDamageClass>() += 0.14f;      // +14% 近战伤害
            player.GetCritChance<MeleeDamageClass>() += 14;      // +14% 近战暴击
            player.GetAttackSpeed<MeleeDamageClass>() += 0.2f;   // +20% 近战攻速
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，分别注册。
        /// 现代版用 CosmiliteBar(10) + AscendantSpiritEssence(2)，于 CosmicAnvil（宇宙砧）合成；
        /// 经典版用 CosmiliteBar(14) + NightmareFuel(8) + EndothermicEnergy(8)，于 DraedonsForge（德雷顿熔炉）合成。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 10);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 2);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 14);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 8);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 8);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
