using CalamityDemutation.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 无星之夜（StarlessNight，移植自 CalamityEntropy）：虚无双子系武器，由神明吞噬者宝袋掉落。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），全部表现交给手持弹幕 <see cref="StarlessNightProj"/>。
    /// <para>
    /// 与 CE 原版的差异：① CE 用自研的 <c>NihilityBlue</c> 稀有度（纯蓝 + 粒子描边），本模组没有 ModRarity 体系，
    /// 按「神明吞噬者档」对齐工程的 <c>postMoonLordRarity = 14</c>（GodSlayer 套装 / 四季银河同档，名称染蓝）；
    /// ② CE 原版没有配方（AddRecipes 空），本模组同样由宝袋掉落获得，见 <c>CalamityDemutationGlobalItem.ModifyItemLoot</c>；
    /// ③ <c>Item.value</c> 取 60 金——CE 原值是裸写的 12000（= 1 银 20 铜，与它周边武器清一色的
    /// <c>buyPrice(gold: N)</c> 写法明显不一致），已按同宝袋的 NebulousCore 与第 5 把深渊分形的 60 金对齐。
    /// </para>
    /// </summary>
    internal class StarlessNight:ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 2160;
            Item.crit = 10;
            Item.DamageType = DamageClass.Melee;
            Item.width = 86;
            Item.height = 86;
            Item.noUseGraphic = true;
            Item.useTime = 80;
            Item.useAnimation = 80;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Red;                  // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
            Item.UseSound = null;                  // 挥砍音由手持弹幕播放
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<StarlessNightProj>();
            Item.shootSpeed = 16f;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>虽用 Swing 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
    }
}
