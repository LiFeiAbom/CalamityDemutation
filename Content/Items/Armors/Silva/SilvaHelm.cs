using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Silva
{
    /// <summary>
    /// 林妖头盔（SilvaHelm） - 林妖套装的头部部件
    /// 纯近战头：近战伤害 +13%、近战暴击 +13%、近战攻速 +19%。
    /// 套装效果（逐条对应 player.setBonus 官方描述）：
    /// 1. 免疫几乎所有 debuff
    /// 2. 所有弹幕命中敌人时生成治疗叶球
    /// 3. 最大奔跑速度与加速度 +5%
    /// 4. 生命被压到 1 点时，10 秒内不会再被任何伤害杀死
    /// 5. 效果持续期间再次被压到 1 点，则减少 100 最大生命
    /// 6. 该效果每条命只触发一次，最大生命降到 400 后无敌效果终止
    /// 7. 死亡后最大生命恢复正常
    /// 8. 真近战攻击有 25% 概率造成五倍伤害
    /// 9. 林妖无敌结束后受到的接触伤害 -20%
    /// 10. 近战弹幕有 25% 概率短暂眩晕敌人
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class SilvaHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 售价 90 金
            Item.defense = 52; //110（括号内为原值记录，当前实际生效 52）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后自定义稀有度 15 级（名称颜色覆盖见 CalamityDemutationGlobalItem）
        }
        /// <summary>
        /// 判定是否凑齐林妖三件套（头/胸/腿均为林妖部件）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<SilvaArmor>() && legs.type == ModContent.ItemType<SilvaLeggings>();
        }
        /// <summary>
        /// 套装生效时的角色描边：开启不透明拖影
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：置位 silvaSet / silvaMelee 标记，并写入官方英文套装描述作为显示文本。
        /// 标记最终在 CalamityDemutationPlayer 中结算——silvaSet 负责移速/加速度 +5%、
        /// 免疫几乎所有 debuff，以及"1 生命免死"无敌（silvaCountdown / SilvaRevival 增益）；
        /// silvaMelee 负责真近战 25% 五倍伤害、近战弹幕施加 SilvaHysteresis 短暂眩晕，
        /// 以及无敌结束后的接触伤害 -20%；弹幕命中生成治疗叶球由
        /// CalamityDemutationGlobalProjectile 生成的 SilvaOrb 实现
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.silvaSet = true;    // 套装标记（移速/免疫 debuff/免死无敌）
            modPlayer.silvaMelee = true;  // 近战侧标记（五倍真近战/眩晕/接触减伤）
            player.setBonus = "\n" +
                "You are immune to almost all debuffs\n" +
                "All projectiles spawn healing leaf orbs on enemy hits\n" +
                "Max run speed and acceleration boosted by 5%\n" +
                "If you are reduced to 1 HP you will not die from any further damage for 10 seconds\n" +
                "If you get reduced to 1 HP again while this effect is active you will lose 100 max life\n" +
                "This effect only triggers once per life and if you are reduced to 400 max life the invincibility effect will stop\n" +
                "Your max life will return to normal if you die\n" +
                "True melee strikes have a 25% chance to do five times damage\n" +
                "After the silva invincibility is over you will take 20% less contact damage\n" +
                "Melee projectiles have a 25% chance to stun enemies for a very brief moment";
        }
        /// <summary>
        /// 穿戴时的属性加成：近战三连（伤害 / 暴击 / 攻速）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<MeleeDamageClass>() += 0.13f;       // 近战伤害 +13%
            player.GetCritChance<MeleeDamageClass>() += 13;      // 近战暴击率 +13%
            player.GetAttackSpeed<MeleeDamageClass>() += 0.19f;  // 近战攻速 +19%
        }
        /// <summary>
        /// 配方：现代版灾厄与经典版灾厄材料不同，分别注册两套配方，
        /// 两版均额外需要本模组材料 LeadCore
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：在 CosmicAnvil 处用 PlantyMush/EffulgentFeather/AscendantSpiritEssence 合成
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("PlantyMush").Type, 30);
                recipe.AddIngredient(calamity.Find<ModItem>("EffulgentFeather").Type, 8);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 2);
                recipe.AddIngredient<LeadCore>();
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：在 DraedonsForge 处用 DarksunFragment/EffulgentFeather/CosmiliteBar 等合成
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("DarksunFragment").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EffulgentFeather").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Tenebris").Type, 6);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 14);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 14);
                recipe1.AddIngredient<LeadCore>();
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
