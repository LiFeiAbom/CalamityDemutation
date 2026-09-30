using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
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
    /// 每挥一次把 <see cref="CalamityDemutationPlayer.shatteredFractalAtkType"/> 沿 0→1→2 循环，从而三式交替：
    /// 0 与 1 是左右两个方向的普通挥砍，2 是向前刺出（弹幕会额外射出 FractalShoot）。
    /// </summary>
    internal class ShatteredFractal:ModItem
    {
            public override void SetDefaults()
        {
            Item.damage = 25;
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
            /// <summary>
            /// 生成手持弹幕并把本次招式交给它（0 记作 -1），随后推进到下一式（2 之后回到 0）。
            /// 招式下标存在 <see cref="CalamityDemutationPlayer.shatteredFractalAtkType"/> 上（源为 ModItem 实例字段，联机下会互相串招）。
            /// </summary>
            public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            {
                CalamityDemutationPlayer mp = player.GetModPlayer<CalamityDemutationPlayer>();
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, mp.shatteredFractalAtkType == 0 ? -1 : mp.shatteredFractalAtkType);
                mp.shatteredFractalAtkType++;
                if (mp.shatteredFractalAtkType > 2)
                {
                    mp.shatteredFractalAtkType = 0;
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
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient<BrokenHilt>();
            recipe.AddIngredient(ItemID.WoodenSword);
            recipe.AddIngredient(ItemID.GoldBroadsword);
            recipe.AddIngredient(ItemID.LightsBane);
            recipe.AddIngredient(ItemID.EnchantedSword);
            recipe.AddIngredient(ItemID.Muramasa);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient<BrokenHilt>();
            recipe2.AddIngredient(ItemID.WoodenSword);
            recipe2.AddIngredient(ItemID.GoldBroadsword);
            recipe2.AddIngredient(ItemID.BloodButcherer);
            recipe2.AddIngredient(ItemID.EnchantedSword);
            recipe2.AddIngredient(ItemID.Muramasa);
            recipe2.AddTile(TileID.Anvils);
            recipe2.Register();
            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient<BrokenHilt>();
            recipe3.AddIngredient(ItemID.WoodenSword);
            recipe3.AddIngredient(ItemID.PlatinumBroadsword);
            recipe3.AddIngredient(ItemID.LightsBane);
            recipe3.AddIngredient(ItemID.EnchantedSword);
            recipe3.AddIngredient(ItemID.Muramasa);
            recipe3.AddTile(TileID.Anvils);
            recipe3.Register();
            Recipe recipe4 = CreateRecipe();
            recipe4.AddIngredient<BrokenHilt>();
            recipe4.AddIngredient(ItemID.WoodenSword);
            recipe4.AddIngredient(ItemID.PlatinumBroadsword);
            recipe4.AddIngredient(ItemID.BloodButcherer);
            recipe4.AddIngredient(ItemID.EnchantedSword);
            recipe4.AddIngredient(ItemID.Muramasa);
            recipe4.AddTile(TileID.Anvils);
            recipe4.Register();
        }
    }
}
