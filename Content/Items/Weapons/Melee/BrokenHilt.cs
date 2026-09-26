using CalamityDemutation.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 破碎剑柄（BrokenHilt，移植自 CalamityEntropy）—— 分形系列的第一把武器，
    /// 后续的 ShatteredFractal 等都以它为起点。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），挥砍交给手持弹幕 BrokenHiltHeld；
    /// 每挥一次把 <see cref="atkType"/> 取反，让弹幕左右交替挥动。
    /// </summary>
    internal class BrokenHilt:ModItem
    {
        /// <summary>本次挥砍的方向标记（1 / -1 交替），作为 ai[0] 传给手持弹幕</summary>
        private int atkType = 1;
        /// <summary>基础属性：18 近战伤害、24 帧使用、4.6 击退、绿色稀有度、2 金价值；本体不画贴图也不做判定</summary>
        public override void SetDefaults()
        {
            Item.damage = 18;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                               // 贴图宽（像素）
            Item.height = 60;                              // 贴图高（像素）
            Item.useTime = 24;                             // 使用时间 24 帧
            Item.useAnimation = 24;                        // 动画时长 24 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 4.6f;
            Item.value = Item.buyPrice(gold: 2);           // 价值 2 金
            Item.rare = ItemRarityID.Green;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BrokenHiltHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
            Item.scale *= 1.2f;                            // 手持弹幕会读取 HeldItem.scale 来放大自己
        }
        /// <summary>生成手持弹幕并把本次的挥砍方向交给它，之后翻转方向供下一挥使用</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType);
            atkType *= -1;
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>配方：铜/锡锭×6 + 石块×4 + 铁/铅锭×4 @ 铁砧（对应世界生成的两种矿石分支）</summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.CopperBar, 6)
                .AddIngredient(ItemID.StoneBlock, 4)
                .AddIngredient(ItemID.IronBar, 4)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe().AddIngredient(ItemID.CopperBar, 6)
                .AddIngredient(ItemID.StoneBlock, 4)
                .AddIngredient(ItemID.LeadBar, 4)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe().AddIngredient(ItemID.TinBar, 6)
                .AddIngredient(ItemID.StoneBlock, 4)
                .AddIngredient(ItemID.IronBar, 4)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe().AddIngredient(ItemID.TinBar, 6)
                .AddIngredient(ItemID.StoneBlock, 4)
                .AddIngredient(ItemID.LeadBar, 4)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
