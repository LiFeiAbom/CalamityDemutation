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
    /// 洛希之弦（NeutronBow） - 月后终局远程武器，由中子锭在德雷顿熔炉上打造（移植自灾厄大修 0.4.0.1.3）。
    /// 物品本体既不出图也不挥砍，左右键都只负责把持弓弹幕 <see cref="NeutronBowHoldout"/> 拿出来；
    /// 瞄准、搭箭、蓄力、充能条全由该弹幕自行结算。
    /// <para>
    /// 大修 0.4.0.1.3 与 0.5.0.1.7 在这件上只差三处（用户点名前者）：
    /// 物品/弓体贴图 16 帧 vs 7 帧、粒子走 CWRParticle vs BasePRT、充能条取 NeutronGlaiveHeld vs NeutronGlaiveHeldAlt。
    /// 本工程照 0.4.0.1.3：贴图用 16 帧那张，粒子用工程既有的 <c>DRK_HeavenfallStar</c>
    /// （与 0.4.0.1.3 的 HeavenfallStarParticle 逐行等价，只是新旧命名不同），充能条正好对上既有类名。
    /// </para>
    /// </summary>
    internal class NeutronBow:ModItem
    {
        /// <summary>
        /// 物品外观动画：按灵魂类物品处理，贴图竖直滚动 16 帧、每 5 tick 换一帧
        /// （0.4.0.1.3 的贴图就是 16 帧的 50×1952）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.AnimatesAsSoul[Type] = true;                          // 按灵魂类物品处理
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 16));// 垂直动画：16 帧、每 5 tick 换一帧
        }
        /// <summary>
        /// 物品基础属性：照搬大修 0.4.0.1.3（初速 16、暴击 +20%、消耗箭矢）。
        /// 射速按用户 2026-09-25 点名由源码的 20 帧提到 **10 帧**（须与持弓体的 FireTime 保持一致）。
        /// channel + noUseGraphic + useStyle = Shoot：本体不出图也不挥砍，瞄准与射击全交给持弓弹幕；
        /// useAmmo 供持弓弹幕里的 Player.PickAmmo 识别弹药类别（本武器会把任何箭强制转成中子箭）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = Item.height = 54;
            Item.damage = 152;
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = Item.useTime = 10;
            Item.knockBack = 2.5f;
            Item.shootSpeed = 16f;
            Item.UseSound = SoundID.Item5;
            Item.useAmmo = AmmoID.Arrow;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(13, 33, 75, 0);
            Item.crit = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<NeutronBowHoldout>();
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;   // 与本工程中子之刃/中子脉冲同档的月后稀有度
        }
        /// <summary>
        /// 允许右键副功能：右键是三级蓄力（弓身搭 1→3 支箭，蓄满自动射出三发引力箭矢）
        /// </summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 持弓弹幕已在场时不再允许"再次使用"，其后由 channel 维持（与本工程中子枪同款闸门）
        /// </summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>
        /// 左右键共用同一个持弓弹幕，故两侧都在这里手动生成、并拦截默认发射。
        /// 不赌 tML 的 channel 生成机制——本武器左右键共用一件物品，channel 状态不可靠
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<NeutronBowHoldout>()] < 1)
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NeutronBowHoldout>(), damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>
        /// 注册配方：与中子枪同档 —— 中子锭×25 + 德雷顿熔炉，兼容灾厄现代版与经典版
        /// （两版熔炉同名不同物，故分别注册）。
        /// 大修原版是"中子锭×25 @ 转化物质台"的特种合成，本工程无那张台子，数量照源码、台子换成本工程惯例
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：德雷顿熔炉（DraedonsForge）
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<BlackMatterStick>(25);
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
                    recipe1.AddIngredient<BlackMatterStick>(25);
                    recipe1.AddTile(classicDraedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
