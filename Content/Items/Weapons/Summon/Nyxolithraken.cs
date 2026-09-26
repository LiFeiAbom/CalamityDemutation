using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 沧溟龙契（Nyxolithraken，移植自 CalamityEntropy 的 Content/Items/Weapons/Nyxolithraken.cs）：
    /// 召唤一条「沧溟渊龙」为你作战的召唤杖，占 5 个仆从栏位，出场时朝鼠标处生成。
    /// <para>
    /// 与 CE 原版的差异：① 配方按用户要求重做（CE 原配方是 星尘细胞法杖 + 星尘之龙法杖 + WyrmTooth×10 + FadingRunestone @ 深渊祭坛，
    /// 后两样本模组都没有）：改为 寒霜九头蛇法杖（原版）+ 巨龙七星灯（本模组新移）+ 魔影锭×5 @ 嘉登熔炉，两版灾厄分别注册；
    /// ② CE 自研稀有度 <c>AbyssalBlue</c> 按「boss 档位」口径对齐本模组的 <c>postMoonLordRarity = 15</c>（名称染紫）。
    /// </para>
    /// </summary>
    internal class Nyxolithraken:ModItem
    {
        /// <summary>手柄整屏瞄准、锁定无视方块；并登记该法杖召唤物占 5 个仆从栏位（CE 原样）</summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 5;
        }
        public override void SetDefaults()
        {
            Item.damage = 1750;
            Item.crit = 0;
            Item.DamageType = DamageClass.Summon;
            Item.width = 90;
            Item.height = 88;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 2;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<NyxolithrakenDragon>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<NyxolithrakenBuff>();
            Item.rare = ItemRarityID.Red;   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>
        /// 出手时给主人挂上维系用的 buff，并在**鼠标处**生成沧溟渊龙（<c>ai[1] = 1</c> 是 CE 留给弹幕的标记）；
        /// 生成后把 <c>originalDamage</c> 钉到武器伤害，免得被后续的伤害重算改写。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;
            return false;
        }
        /// <summary>
        /// 注册配方：寒霜九头蛇法杖（原版 <c>ItemID.StaffoftheFrostHydra</c>）+ 巨龙七星灯 + 魔影锭（灾厄 ShadowspecBar）×5，
        /// 站在嘉登熔炉（灾厄 DraedonsForge）上合成。两版灾厄都注册一条（材料名相同，各取自己那版的实例）。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar) && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    CreateRecipe()
                        .AddIngredient(ItemID.StaffoftheFrostHydra)     // 寒霜九头蛇法杖
                        .AddIngredient<YharonSonStaff>()                // 巨龙七星灯
                        .AddIngredient(shadowspecBar.Type, 5)           // 现代版灾厄：魔影锭×5
                        .AddTile(draedonsForge.Type)                    // 现代版灾厄：嘉登熔炉
                        .Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    CreateRecipe()
                        .AddIngredient(ItemID.StaffoftheFrostHydra)               // 寒霜九头蛇法杖
                        .AddIngredient<YharonSonStaff>()                          // 巨龙七星灯
                        .AddIngredient(classicShadowspecBar.Type, 5)              // 经典版灾厄：魔影锭×5
                        .AddTile(classicDraedonsForge.Type)                       // 经典版灾厄：嘉登熔炉
                        .Register();
                }
            }
        }
    }
}
