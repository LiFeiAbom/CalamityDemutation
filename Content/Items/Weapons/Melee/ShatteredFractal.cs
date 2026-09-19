using CalamityDemutation.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 破碎分形（ShatteredFractal，移植自 CalamityEntropy）—— 分形系列的第二把武器，
    /// 由上一把「破碎剑柄」BrokenHilt 与木剑及若干早期剑类合成而来。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），挥砍交给手持弹幕 ShatteredFractalHeld；
    /// 每挥一次把 <see cref="atkType"/> 沿 0→1→2 循环，从而三式交替：
    /// 0 与 1 是左右两个方向的普通挥砍，2 是向前刺出（弹幕会额外射出 FractalShoot）。
    /// </summary>
    internal class ShatteredFractal:ModItem
    {
        /// <summary>本次挥砍的招式下标，0→1→2 循环；传给弹幕时 0 记作 -1（普通挥砍的方向标记），2 表示刺出式</summary>
        private int atkType = 0;
        public override void SetDefaults()
        {
            Item.damage = 25;                              // 25 点近战伤害
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;                               // 贴图宽（像素）
            Item.height = 60;                              // 贴图高（像素）
            Item.useTime = 18;                             // 使用时间 18 帧
            Item.useAnimation = 18;                        // 动画时长 18 帧
            Item.useStyle = ItemUseStyleID.Shoot;          // 举械姿势，实际挥砍由手持弹幕表现
            Item.knockBack = 5;
            Item.value = Item.buyPrice(gold: 5);           // 价值 5 金
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = null;                          // 挥砍音由手持弹幕播放
            Item.noMelee = true;                           // 本体不做挥砍判定
            Item.noUseGraphic = true;                      // 本体不画贴图
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ShatteredFractalHeld>();
            Item.shootSpeed = 12f;                         // 决定手持弹幕的朝向速度
        }
        /// <summary>生成手持弹幕并把本次招式交给它（0 记作 -1），随后推进到下一式（2 之后回到 0）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType++;
            if (atkType > 2)
            {
                atkType = 0;
            }
            return false;
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>
        /// 四个配方：破碎剑柄 + 木剑 + 附魔剑 + 村正 为公共部分，
        /// 由「金阔剑 / 铂金阔剑」二选一与「光之祸 / 血屠者」二选一组合出四种（对应世界生成的矿石与邪恶地形分支），均在铁砧合成。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient<BrokenHilt>()
                .AddIngredient(ItemID.WoodenSword)
                .AddIngredient(ItemID.GoldBroadsword)
                .AddIngredient(ItemID.LightsBane)
                .AddIngredient(ItemID.EnchantedSword)
                .AddIngredient(ItemID.Muramasa)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe().AddIngredient<BrokenHilt>()
                .AddIngredient(ItemID.WoodenSword)
                .AddIngredient(ItemID.GoldBroadsword)
                .AddIngredient(ItemID.BloodButcherer)
                .AddIngredient(ItemID.EnchantedSword)
                .AddIngredient(ItemID.Muramasa)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe().AddIngredient<BrokenHilt>()
                .AddIngredient(ItemID.WoodenSword)
                .AddIngredient(ItemID.PlatinumBroadsword)
                .AddIngredient(ItemID.LightsBane)
                .AddIngredient(ItemID.EnchantedSword)
                .AddIngredient(ItemID.Muramasa)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe().AddIngredient<BrokenHilt>()
                .AddIngredient(ItemID.WoodenSword)
                .AddIngredient(ItemID.PlatinumBroadsword)
                .AddIngredient(ItemID.BloodButcherer)
                .AddIngredient(ItemID.EnchantedSword)
                .AddIngredient(ItemID.Muramasa)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
