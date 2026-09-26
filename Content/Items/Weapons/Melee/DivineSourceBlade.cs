using CalamityDemutation.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 金源灭却刃（Divine Source Blade）—— 月后神吞+档近战（移植自灾厄大修 0.4.0.1.3 Extras 自创武器）。
    /// 左键射出光束（DivineSourceBladeProjectile，首击召唤小刀光创造者），挥舞时额外放出一道剑气（DivineSourceBeam）。
    /// 配方（现代版灾厄）：灾厄 AuricBar×5 + 本模组泰拉巨刃 + 本模组宙宇波能刃 @ 灾厄宇宙砧；
    /// 经典版灾厄没有 AuricBar/宇宙砧，按本模组惯例改用 AuricOre×25（5 锭 = 25 矿）@ 德雷顿熔炉。
    /// 伤害 2160 = 大修原值 480 × 4.5（本模组 CE 分形膨胀口径）。
    /// </summary>
    internal class DivineSourceBlade : ModItem
    {
        /// <summary>物品基础属性：154×154、伤害 2160、15 帧挥砍、击退 5.5、纯弹幕（noMelee）、红稀有度</summary>
        public override void SetDefaults()
        {
            Item.height = 154;
            Item.width = 154;
            Item.damage = 2160;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 15;
            Item.scale = 1;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 5.5f;
            Item.UseSound = SoundID.Item60;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 33, 15, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<DivineSourceBladeProjectile>();
            Item.shootSpeed = 18f;
        }
        /// <summary>暴击 +10</summary>
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10;
        /// <summary>挥舞时额外朝鼠标方向放出一道剑气（伤害 ×1.25）</summary>
        public override void UseAnimation(Player player)
        {
            int types = ModContent.ProjectileType<DivineSourceBeam>();
            Vector2 vector2 = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitY) * 3;
            Vector2 position = player.Center;
            // 源为 CWR 的 player.parent()，等价于 new EntitySource_Parent(player)
            Projectile.NewProjectile(new EntitySource_Parent(player), position, vector2, types, (int)(Item.damage * 1.25f), Item.knockBack, player.whoAmI);
        }
        /// <summary>
        /// 配方（分版本）：现代版 AuricBar×5 + 泰拉巨刃 + 宙宇波能刃 @ CosmicAnvil；
        /// 经典版 AuricOre×25 + 泰拉巨刃 + 宙宇波能刃 @ DraedonsForge（经典版无 AuricBar，按 5 锭 = 25 矿折算）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(auricBar.Type, 5);
                    recipe.AddIngredient<Terratomere>();
                    recipe.AddIngredient<Excelsus>();
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("AuricOre", out ModItem auricOre) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(auricOre.Type, 25);
                    recipe1.AddIngredient<Terratomere>();
                    recipe1.AddIngredient<Excelsus>();
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
