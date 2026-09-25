using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Magic;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Summon;
using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 元素之心（HeartoftheElements） - 综合型饰品（五个 waifu 饰品的合集）
    /// 显示外观时召唤全部元素仆从并提供综合增益；隐藏外观时仅保留强化的基础属性。
    /// </summary>
    internal class HeartoftheElements:ModItem
    {
        /// <summary>
        /// 注册贴图动画：竖直逐帧滚动，并让其像元素之魂一样被绘制
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 逐帧竖直滚动贴图，营造元素之魂般的动画效果
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金
            Item.defense = 9;                         // 装备时 +9 防御
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            // 使用模组自定义的月后稀有度等级
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        /// <summary>
        /// 装备时：显示外观则置位元素之心标记并召唤全部五个元素仆从（附 HotE 增益与进度成长伤害）；
        /// 隐藏外观则仅保留基础属性标记、不召唤仆从
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().heartoftheElements = true;
            if(!hideVisual)
            {
                player.GetModPlayer<CalamityDemutationPlayer>().heartoftheElementshideVisual = false;
                // 元素之心是五个 waifu 配件的合集，必须置 allWaifus 为 true，否则召唤物与 HotE 增益会因存活检查失败被立即销毁
                player.GetModPlayer<CalamityDemutationPlayer>().allWaifus = true;
                // 召唤物生成/增益维护仅由本地玩家负责（与玫瑰石/真菌团块等组成件一致），避免多人下重复生成
                if (player.whoAmI == Main.myPlayer)
                {
                    // 召唤物基础伤害随游戏进度提升：月后 600、月前 200；击败噬神者后伤害翻倍
                    int damage = NPC.downedMoonlord ? 600 : 200;
                    float damageMult = BossSystem.DevourerOfGods ? 2f : 1f;
                    // 限数保护：若某类元素仆从异常超限（>1，多人/极端帧序导致），
                    // 直接销毁多余实例只保留一只，并正常维持 HotE 增益。
                    // 原实现仅靠"清 buff 不补"指望超限仆从自然消散，但仆从存活并不依赖该 buff，
                    // 清增益始终无效，故改为真正 Kill 多余仆从。
                    TrimExtraMinions(player, ModContent.ProjectileType<BigBustyRose>());
                    TrimExtraMinions(player, ModContent.ProjectileType<Projectiles.Summon.SirenLure>());
                    TrimExtraMinions(player, ModContent.ProjectileType<Projectiles.Summon.DrewsSandyWaifu>());
                    TrimExtraMinions(player, ModContent.ProjectileType<Projectiles.Summon.SandyWaifu>());
                    TrimExtraMinions(player, ModContent.ProjectileType<Projectiles.Summon.CloudyWaifu>());
                    if (player.FindBuffIndex(ModContent.BuffType<HotE>()) == -1)
                    {
                        player.AddBuff(ModContent.BuffType<HotE>(), 3600, true);
                    }
                    if (player.ownedProjectileCounts[ModContent.ProjectileType<BigBustyRose>()] < 1)
                    {
                        Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<BigBustyRose>(), (int)((float)damage * damageMult * player.GetTotalDamage<SummonDamageClass>().Multiplicative), 2f, Main.myPlayer, 0f, 0f);
                    }
                    if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SirenLure>()] < 1)
                    {
                        Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.SirenLure>(), (int)((float)damage * damageMult * player.GetTotalDamage<SummonDamageClass>().Multiplicative), 2f, Main.myPlayer, 0f, 0f);
                    }
                    if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.DrewsSandyWaifu>()] < 1)
                    {
                        Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.DrewsSandyWaifu>(), (int)((float)damage * damageMult * player.GetTotalDamage<SummonDamageClass>().Multiplicative), 2f, Main.myPlayer, 0f, 0f);
                    }
                    if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SandyWaifu>()] < 1)
                    {
                        Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.SandyWaifu>(), (int)((float)damage * damageMult * player.GetTotalDamage<SummonDamageClass>().Multiplicative), 2f, Main.myPlayer, 0f, 0f);
                    }
                    if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.CloudyWaifu>()] < 1)
                    {
                        Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.CloudyWaifu>(), (int)((float)damage * damageMult * player.GetTotalDamage<SummonDamageClass>().Multiplicative), 2f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
            else
            {
                player.GetModPlayer<CalamityDemutationPlayer>().heartoftheElementshideVisual = true;
            }
        }
        /// <summary>
        /// 与构成元素之心的五个 waifu 饰品互斥：已装备任一者时禁止再装备，避免召唤物重复叠加。
        /// </summary>
        public override bool CanEquipAccessory(Player player, int slot, bool modded)/* tModPorter Suggestion: Consider using new hook CanAccessoryBeEquippedWith */
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.wifeinaBottlewithBoobs || modPlayer.wifeinaBottle || modPlayer.lureofEnthrallment || modPlayer.eyeoftheStorm || modPlayer.roseStone)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 配方：由五个 waifu 饰品与四种元素石在月球锻造台合成
        /// （两版灾厄共用同一配方，材料全部为本模组物品）
        /// </summary>
        public override void AddRecipes()
        {
            // 由五个 waifu 饰品与四色元素石在月球锻造台合成
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<WifeinaBottle>();
                recipe.AddIngredient<WifeinaBottlewithBoobs>();
                recipe.AddIngredient<LureofEnthrallment>();
                recipe.AddIngredient<EyeoftheStorm>();
                recipe.AddIngredient<RoseStone>();
                recipe.AddIngredient<AeroStone>();
                recipe.AddIngredient<ChaosStone>();
                recipe.AddIngredient<CryoStone>();
                recipe.AddIngredient<BloomStone>();
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
        }
        /// <summary>
        /// 限数保护助手：销毁本玩家多于 keep 只的指定元素仆从（保留最早生成的 keep 只）。
        /// 由 UpdateAccessory 的本地玩家分支每帧调用；只在计数超限时扫描，代价可控。
        /// </summary>
        private static void TrimExtraMinions(Player player, int minionType, int keep = 1)
        {
            if (player.ownedProjectileCounts[minionType] <= keep)
                return;
            int alive = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == minionType)
                {
                    alive++;
                    if (alive > keep)
                        proj.Kill();
                }
            }
        }
    }
}
