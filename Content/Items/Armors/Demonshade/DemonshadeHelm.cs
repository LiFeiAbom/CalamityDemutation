using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 恶魔之影头盔（DemonshadeHelm） - 恶魔之影套（Demonshade）头部防具
    /// 提供召唤栏、通用伤害与暴击加成；套装激活后置位 demonshadeSetBonus、redDevil，
    /// 并维持一只友方红魔，套装效果最终在 CalamityDemutationPlayer 与
    /// CalamityDemutationGlobalNPC 中结算。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class DemonshadeHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(5, 0, 0, 0);  // 售价 5 铂金
            Item.defense = 55; //15
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后稀有度 16 级，名称颜色为品红
        }
        /// <summary>
        /// 判定是否集齐恶魔之影套三件（胸甲 + 护腿）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DemonshadeBreastplate>() && legs.type == ModContent.ItemType<DemonshadeGreaves>();
        }
        /// <summary>
        /// 套装激活时的视觉表现：绘制残影与外描边
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
            player.armorEffectDrawOutlines = true;
        }
        /// <summary>
        /// 单件装备加成：召唤上限、通用伤害与暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 10;                            // 仆从栏上限 +10
            player.maxTurrets += 10;                            // 哨兵栏上限 +10
            player.GetDamage<GenericDamageClass>() += 0.5f;     // 全类型伤害 +50%
            player.GetCritChance<GenericDamageClass>() += 50;   // 全类型暴击率 +50%
        }
        /// <summary>
        /// 套装激活：置位 demonshadeSetBonus 与 redDevil 标记，补上红魔 buff 并召唤红魔，
        /// 最后额外叠加 +100% 通用伤害。
        /// 标记最终结算位置：demonshadeSetBonus 在 CalamityDemutationPlayer（潜行、受击反击、
        /// 命中附加 debuff 等）与 CalamityDemutationGlobalNPC（命中附带恶魔火焰等）中消费；
        /// redDevil 用于维持红魔弹幕存活。
        /// setBonus 逐条含义：伤害提升 100%；所有攻击附加恶魔火焰与脆弱诅咒类 debuff；
        /// 受击时天降暗影光束与恶魔镰刀；一只友方红魔会跟随你；
        /// 按 Y 键以黑暗魔法激怒附近敌人 10 秒，使其伤害提高 25%，但承受的伤害提高 125%。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            int redDevilDamage = (int)player.GetDamage<GenericDamageClass>().ApplyTo(10000);  // 红魔弹幕伤害：以玩家通用伤害对 10000 基准换算
            player.setBonus = "\n" +
                "100% increased damage\n" +
                "All attacks inflict the demon flame debuff and vulnerability hex debuffs\n" +
                "Shadowbeams and demon scythes will fire down when you are hit\n" +
                "A friendly red devil follows you around\n" +
                "Press Y to enrage nearby enemies with a dark magic spell for 10 seconds\n" +
                "This makes them do 25% more damage but they also take 125% more damage";
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.demonshadeSetBonus = true;  // 置位套装总标记
            modPlayer.redDevil = true;            // 置位红魔标记，供红魔弹幕判断去留
            if (player.FindBuffIndex(ModContent.BuffType<Buffs.SummonBuffs.RedDevil>()) == -1)
            {
                player.AddBuff(ModContent.BuffType<Buffs.SummonBuffs.RedDevil>(), 3600, true);  // 无红魔 buff 时补上（3600 帧 = 60 秒）
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.RedDevil>()] < 1)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.RedDevil>(), redDevilDamage, 0f, Main.myPlayer, 0f, 0f);  // 场上无红魔时召唤一只
            }
            player.GetDamage<GenericDamageClass>() += 1f;  // 全类型伤害 +100%
        }
        /// <summary>
        /// 注册配方：现代版灾厄（CalamityMod）与经典预发布版灾厄（CalamityModClassicPreTrailer）
        /// 材料相同（ShadowspecBar×40），但对应各自的暗影合金锭与德雷顿熔炉，故分别注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("ShadowspecBar").Type, 40);  // 现代版灾厄：ShadowspecBar×40
                recipe.AddTile(calamity.Find<ModTile>("DraedonsForge").Type);            // 现代版灾厄：德雷顿熔炉
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("ShadowspecBar").Type, 40);  // 经典版灾厄：ShadowspecBar×40
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);            // 经典版灾厄：德雷顿熔炉
                recipe1.Register();
            }
        }
    }
}
