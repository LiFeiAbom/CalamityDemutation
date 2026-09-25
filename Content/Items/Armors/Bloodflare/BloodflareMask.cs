using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Bloodflare
{
    /// <summary>
    /// 炎血面具（BloodflareMask） - 炎血套（Bloodflare）头部防具
    /// 防御与近战输出向头部，提供岩浆免疫时长、水下行动自如与近战加成；
    /// 集齐头/胸/腿后由 UpdateArmorSet 置位 bloodflareSet、bloodflareMelee，
    /// 套装效果最终在 CalamityDemutationPlayer 与 CalamityDemutationGlobalNPC 中结算。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class BloodflareMask:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金
            Item.defense = 49;  // 防御 49（与经典版灾厄同值，源码同行另留 //85 注释，系开发期遗留数字）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13 级，名称颜色为荧光绿
        }
        /// <summary>
        /// 判定是否集齐炎血套三件（胸甲 + 护腿）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BloodflareBodyArmor>() && legs.type == ModContent.ItemType<BloodflareCuisses>();
        }
        /// <summary>
        /// 套装激活时的视觉表现：绘制细微残影
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
        }
        /// <summary>
        /// 套装激活：置位 bloodflareSet 与 bloodflareMelee 标记，并写入官方效果描述（setBonus）。
        /// 标记最终结算位置：bloodflareSet 在 CalamityDemutationPlayer 中负责红心/魔力星计时与回血，
        /// 并在 CalamityDemutationGlobalNPC 的命中/击杀回调中触发掉落；
        /// bloodflareMelee 在 CalamityDemutationPlayer 中累计真近战命中次数。
        /// setBonus 逐条含义（首行 "+" 仅为显示用符号）：
        /// 极大提升生命回复；敌人更倾向于以你为目标；
        /// 生命低于 50% 的敌人被击中时有几率掉落红心，高于 50% 时有几率掉落魔力星；
        /// 血月期间击杀的敌人有远高于平常的几率掉落血珠（Blood Orbs）；
        /// 真近战攻击会为你治疗；真近战命中敌人 15 次后进入 5 秒血之狂怒，
        /// 期间 +25% 近战伤害与暴击率，且受到的接触伤害减半，该效果有 30 秒冷却。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodflareSet = true;
            modPlayer.bloodflareMelee = true;
            player.setBonus = this.GetLocalizedValue("SetBonus");
            player.crimsonRegen = true;  // 猩红回血：提升生命回复
            player.aggro += 900;         // 大幅提高仇恨，敌人更倾向以你为目标
        }
        /// <summary>
        /// 单件装备加成：岩浆免疫时长、水下行动与近战伤害/暴击/攻速
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.lavaMax += 240;                              // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                          // 水中不受移动减速
            player.GetDamage<MeleeDamageClass>() += 0.1f;       // 近战伤害 +10%
            player.GetCritChance<MeleeDamageClass>() += 10;     // 近战暴击率 +10%
            player.GetAttackSpeed<MeleeDamageClass>() += 0.18f; // 近战攻速 +18%
        }
        /// <summary>
        /// 注册配方：现代版灾厄（CalamityMod）与经典预发布版灾厄（CalamityModClassicPreTrailer）
        /// 材料不同，故分别注册两套配方，均在远古操纵机（LunarCraftingStation）处合成。
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
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄材料：BloodstoneCore×11、RuinousSoul×2
                if (calamity1.TryFind<ModItem>("BloodstoneCore", out ModItem bloodstoneCore)
                    && calamity1.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(bloodstoneCore.Type, 11);
                    recipe1.AddIngredient(classicRuinousSoul.Type, 2);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
