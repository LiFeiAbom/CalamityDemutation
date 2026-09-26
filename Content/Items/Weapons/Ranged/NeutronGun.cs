using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 中子枪（NeutronGun） - 月后终局远程武器，由中子锭在灾厄德雷顿熔炉上打造（移植自 CWR 0.5.0.1.7）。
    /// 物品本体既不出图也不挥砍，左右键都只负责把持握弹幕 <see cref="NeutronGunHoldout"/> 拿出来；
    /// 瞄准、开火、蓄力、充能条全由该弹幕自行结算。
    /// </summary>
    internal class NeutronGun:ModItem
    {
        /// <summary>
        /// 物品外观动画：按灵魂类物品处理（浮动/发光），贴图竖直滚动 7 帧、每 5 tick 换一帧
        /// </summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.AnimatesAsSoul[Type] = true;                          // 按灵魂类物品处理
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 7));// 垂直动画：7 帧、每 5 tick 换一帧
        }
        /// <summary>
        /// 物品基础属性：伤害按用户拍板 ×3.5 膨胀（580→2030），其余照搬 CWR（射速 5 帧、暴击 +2%、消耗子弹）。
        /// channel + noUseGraphic + useStyle = Shoot：本体不出图也不挥砍，靠 channel 维持持握弹幕常驻；
        /// useAmmo 供持握弹幕里的 Player.PickAmmo 识别弹药类别（本武器会把任何子弹强制转成中子弹）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = Item.height = 34;
            Item.damage = 2030;
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = Item.useTime = 5;
            Item.knockBack = 1.5f;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(13, 83, 5, 0);
            Item.crit = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<NeutronGunHoldout>();
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;   // 与本工程中子长戟同档的月后稀有度
        }
        /// <summary>
        /// 允许右键副功能：左右键都要能把持握弹幕拿出来（具体打法是弹幕自己按鼠标键区分的）
        /// </summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 持握弹幕已在场时不再允许"再次使用"，其后由 channel 维持（与本工程 DragonRage / 元素圣剑同款闸门）
        /// </summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>
        /// 左右键共用同一个持握弹幕，故两侧都在这里手动生成、并拦截默认发射。
        /// 不赌 tML 的 channel 生成机制——本武器左右键共用一件物品，channel 状态不可靠
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<NeutronGunHoldout>()] < 1)
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NeutronGunHoldout>(), damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>
        /// 注册配方：与中子长戟同档 —— 中子锭×12 + 德雷顿熔炉，兼容灾厄现代版与经典版
        /// （两版熔炉同名不同物，故分别注册）
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：德雷顿熔炉（DraedonsForge）
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<BlackMatterStick>(12);
                    recipe.AddTile(draedonsForge.Type);
                    recipe.Register();
                }
            }
            // 经典版灾厄：德雷顿熔炉（DraedonsForge）
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<BlackMatterStick>(12);
                    recipe1.AddTile(classicDraedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
