using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿头盔（TarragonHelm） - 龙蒿套装的头部部件
    /// 近战向头：近战伤害 +10%、近战暴击 +10%、近战攻速 +15%、减伤 +5%，
    /// 另提供 +240 岩浆免疫时长、水下呼吸，并免疫诅咒地狱/着火了/诅咒/冷冻。
    /// 套装效果（逐条对应 player.setBonus 官方描述）：
    /// 1. 提升红心拾取范围
    /// 2. 敌人死亡时有概率掉落额外红心
    /// 3. 受到伤害时有 25% 概率获得生命回复增益
    /// 4. 按 Y 键用生命能量笼罩自身，10 秒内大幅降低敌人接触伤害
    /// 5. 该效果冷却 30 秒
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class TarragonHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 50, 0, 0);  // 价值 50 金
            Item.defense = 33; //98（括号内为原值记录，当前实际生效 33）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;  // 月后自定义稀有度 12 级（名称颜色覆盖见 CalamityDemutationGlobalItem）
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
        /// 套装效果：置位 tarraSet / tarraMelee 标记，并写入官方英文套装描述作为显示文本。
        /// 标记最终在 CalamityDemutationPlayer 中结算——tarraSet 负责红心磁吸
        /// （lifeMagnet）与红心掉落相关增益；tarraMelee 负责受击 25% 概率施加
        /// TarraLifeRegen 增益、以及按 Y 触发 tarraDefense（10 秒内接触伤害减半，
        /// 冷却 30 秒，见 CalamityDemutationPlayer 的 tarraDefenseTime/tarraCooldown）
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;    // 套装标记（红心拾取范围/额外红心掉落）
            modPlayer.tarraMelee = true;  // 近战侧标记（受击回血 buff / 按 Y 减伤）
            player.setBonus = "\n" +
                "Increased heart pickup range\n" +
                "Enemies have a chance to drop extra hearts on death\n" +
                "You have a 25% chance to gain a life regen buff when you take damage\n" +
                "Press Y to cloak yourself in life energy that heavily reduces enemy contact damage for 10 seconds\n" +
                "This has a 30 second cooldown";
        }
        /// <summary>
        /// 穿戴时的属性加成：近战面板、减伤、岩浆与水下生存、若干 debuff 免疫
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<MeleeDamageClass>() += 0.1f;       // 近战伤害 +10%
            player.GetCritChance<MeleeDamageClass>() += 10;      // 近战暴击率 +10%
            player.GetAttackSpeed<MeleeDamageClass>() += 0.15f;  // 近战攻速 +15%
            player.endurance += 0.05f;                           // 伤害减免 +5%
            player.lavaMax += 240;                               // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                           // 水中不受移速/跳跃惩罚
            player.buffImmune[BuffID.CursedInferno] = true;      // 免疫诅咒地狱
            player.buffImmune[BuffID.OnFire] = true;             // 免疫着火了
            player.buffImmune[BuffID.Cursed] = true;             // 免疫诅咒
            player.buffImmune[BuffID.Chilled] = true;            // 免疫冷冻
        }
        /// <summary>
        /// 配方：现代版与经典版灾厄材料不同，分别注册两套配方
        /// （均使用原版合成站 TileID.LunarCraftingStation）
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
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄配方（UeliaceBar + DivineGeode）
                if (calamity1.TryFind<ModItem>("UeliaceBar", out ModItem ueliaceBar)
                    && calamity1.TryFind<ModItem>("DivineGeode", out ModItem classicDivineGeode))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ueliaceBar.Type, 7);
                    recipe1.AddIngredient(classicDivineGeode.Type, 6);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
