using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.OmegaBlue
{
    /// <summary>
    /// 奥米加蓝头盔（OmegaBlueHelmet） - 奥米加蓝套头部，召唤/近战辅助
    /// 单件：可在液体中自由移动、+12% 全伤害、+8% 暴击、+2 最大仆从。
    /// 套装效果（player.setBonus 官方描述逐条）：
    /// 护甲穿透 +50；伤害与暴击 +10%；近战范围的触手会吸取敌人生命为你治疗；
    /// 按 Y 激活「深渊疯狂」持续 5 秒；深渊疯狂提升伤害、暴击以及触手的攻击性与范围；
    /// 该效果有 30 秒冷却。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class OmegaBlueHelmet:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、稀有度、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                             // 贴图宽（像素）
            Item.height = 18;                            // 贴图高（像素）
            Item.value = Item.sellPrice(0, 35, 0, 0);    // 售价 35 金
            Item.rare = ItemRarityID.Red;                // 基础稀有度红色
            Item.defense = 19;                           // 防御 19
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13（荧光绿名）
        }
        /// <summary>
        /// 判定套装：头部 + 奥米加蓝胸甲 + 奥米加蓝护腿
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<OmegaBlueChestplate>() && legs.type == ModContent.ItemType<OmegaBlueLeggings>();
        }
        /// <summary>
        /// 套装激活时的轮廓特效
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawOutlines = true;  // 开启盔甲轮廓描边
        }
        /// <summary>
        /// 套装激活：+50 护甲穿透并置位 omegaBlueSet；
        /// 这里同时负责「深渊疯狂」冷却的推进——按 Y 的触发在 CalamityDemutationPlayer.ProcessTriggers，
        /// 置 1800（30 秒）冷却，冷却高于 1500 的这 300 帧（5 秒）内 omegaBlueHentai 为真。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalizedValue("SetBonus");
            player.GetArmorPenetration<GenericDamageClass>() += 50;  // 护甲穿透 +50
            // 置位套装标记；+10% 伤害/暴击与触手召唤在 CalamityDemutationPlayer 统一结算
            player.GetModPlayer<CalamityDemutationPlayer>().omegaBlueSet = true;
            if (player.GetModPlayer<CalamityDemutationPlayer>().omegaBlueCooldown > 0)
            {
                if (player.GetModPlayer<CalamityDemutationPlayer>().omegaBlueCooldown == 1)   // 冷却只剩最后一帧：喷粒子提示「深渊疯狂」已就绪
                {
                    // 冷却即将归零：爆发一圈净化粉粒子提示「深渊疯狂已就绪」
                    for (int i = 0; i < 66; i++)
                    {
                        int d = Dust.NewDust(player.position, player.width, player.height, DustID.PurificationPowder, 0, 0, 100, Color.Transparent, 2.6f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].fadeIn = 1f;
                        Main.dust[d].velocity *= 6.6f;
                    }
                }
                player.GetModPlayer<CalamityDemutationPlayer>().omegaBlueCooldown--;
            }
            if (player.GetModPlayer<CalamityDemutationPlayer>().omegaBlueCooldown > 1500)
            {
                // 冷却前 300 帧（5 秒）= 深渊疯狂持续时间，置位 omegaBlueHentai 使伤害/暴击翻倍、触手更凶
                player.GetModPlayer<CalamityDemutationPlayer>().omegaBlueHentai = true;
                int d = Dust.NewDust(player.position, player.width, player.height, DustID.PurificationPowder, 0, 0, 100, Color.Transparent, 1.6f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].fadeIn = 1f;
                Main.dust[d].velocity *= 3f;
            }
        }
        /// <summary>
        /// 单件属性：液体中自由移动、全伤害 / 暴击、最大仆从数
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.ignoreWater = true;   // 液体中自由移动（不受水阻与跳跃惩罚）
            const float damageUp = 0.12f;
            const int critUp = 8;
            player.GetDamage<GenericDamageClass>() += damageUp;   // 全伤害 +12%
            player.GetCritChance<GenericDamageClass>() += critUp;  // 全暴击 +8%
            player.maxMinions += 2;      // 最大仆从 +2
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，分别注册，均在原版月球工作台合成。
        /// 现代版用 ReaperTooth(3) + DepthCells(15) + RuinousSoul(1)；
        /// 经典版用 ReaperTooth(11) + Lumenite(5) + Tenebris(5) + RuinousSoul(2)。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ReaperTooth", out ModItem reaperTooth)
                    && calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells)
                    && calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(reaperTooth.Type, 3);
                    recipe.AddIngredient(depthCells.Type, 15);
                    recipe.AddIngredient(ruinousSoul.Type);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("ReaperTooth", out ModItem classicReaperTooth)
                    && calamity1.TryFind<ModItem>("Lumenite", out ModItem lumenite)
                    && calamity1.TryFind<ModItem>("Tenebris", out ModItem tenebris)
                    && calamity1.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(classicReaperTooth.Type, 11);
                    recipe1.AddIngredient(lumenite.Type, 5);
                    recipe1.AddIngredient(tenebris.Type, 5);
                    recipe1.AddIngredient(classicRuinousSoul.Type, 2);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
