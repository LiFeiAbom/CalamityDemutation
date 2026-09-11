using CalamityDemutation.Content.Items.Accessories.Attack;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Summon;
using CalamityDemutation.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 大杂烩 - 顶级综合型专家饰品，由合体大脑 + 虚空灭绝 + 利维坦龙涎香 + 真菌团块组合而成
    /// 集四者效果于一体并强化：+30% 通用伤害、+15% 通用暴击、1/4 概率闪避攻击；
    /// 受击时召唤灵气雨/烈焰反击并周期性喷发地狱火，获得水下/岩浆作战与真菌团块仆从。
    /// 与合体大脑、虚空灭绝、灾厄之戒、真菌团块、利维坦龙涎香互相排斥。
    /// </summary>
    internal class TheAmalgam:ModItem
    {
        /// <summary>
        /// 注册物品动画：逐帧竖直滚动贴图，以"灵魂"方式渲染
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(9, 6));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>
        /// 基础属性：中尺寸贴图、专家限定、价值 90 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.value = Item.buyPrice(0, 90, 0, 0);
            Item.expert = true;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时：同时置位大杂烩与真菌团块标记，并（仅本地玩家侧）维持一只高伤真菌团块仆从
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().theAmalgam = true;
            player.GetModPlayer<CalamityDemutationPlayer>().fungalClump = true;
            // 仅在本地玩家侧生成，避免多人模式下重复生成
            if (player.whoAmI == Main.myPlayer)
            {
                // buff 仅作存活状态标记，真正的仆从由下方弹幕实现
                if (player.FindBuffIndex(ModContent.BuffType<Buffs.SummonBuffs.FungalClump>()) == -1)
                {
                    player.AddBuff(ModContent.BuffType<Buffs.SummonBuffs.FungalClump>(), 3600, true);
                }
                // 数量不足 1 时补生成（基准伤害 330，为单真菌团块 50 的强化版）
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.FungalClump>()] < 1)
                {
                    Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.FungalClump>(), (int)(330f * player.GetDamage<SummonDamageClass>().Multiplicative), 1f, Main.myPlayer, 0f, 0f);
                }
            }
        }
        /// <summary>
        /// 互斥判定：大杂烩已包含四个组件饰品的全部效果，禁止再单独装备其中任意一个；
        /// 另因虚空灭绝已并入灾厄之戒效果，灾厄之戒同样不得与其同装，避免单向叠加漏洞
        /// </summary>
        public override bool CanEquipAccessory(Player player, int slot, bool modded)/* tModPorter Suggestion: Consider using new hook CanAccessoryBeEquippedWith */
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.amalgamatedBrain || modPlayer.voidofExtinction || modPlayer.calamityRing || modPlayer.fungalClump || modPlayer.levianthanAmbergris)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 配方：四件组件饰品 + 星辉/暗物质系材料，分别在现代版（宇宙砧）与经典版（德雷顿熔炉）注册
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<AmalgamatedBrain>();
                recipe.AddIngredient<VoidofExtinction>();
                recipe.AddIngredient<LeviathanAmbergris>();
                recipe.AddIngredient<FungalClump>();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 5);
                recipe.AddIngredient(calamity.Find<ModItem>("Necroplasm").Type, 5);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<AmalgamatedBrain>();
                recipe1.AddIngredient<VoidofExtinction>();
                recipe1.AddIngredient<LeviathanAmbergris>();
                recipe1.AddIngredient<FungalClump>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 5);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
