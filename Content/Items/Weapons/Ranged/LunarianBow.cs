using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 月光弓（Lunarian Bow） - 射手弓，**天堂之风整条合成链的最底一件**（入料只有原版弓 + 净化凝胶）。
    /// <para>
    /// **口径（用户 2026-10-14 逐条拍板）**：中文名 **月光弓**、面板 / 木箭判定 / 弹幕 / 分支处理**全取 2.0**。
    /// 源 tooltip（英文）：<c>Fires two arrows at once</c> / <c>Converts wooden arrows into sliding energy bolts</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：32×62 / 伤害 32 / 使用·动画 22 帧 / 击退 2 / 弹速字段 8（实际出手速度在
    /// <c>Shoot</c> 里归一化后 ×<see cref="ShotSpeed"/>）/ 箭类弹药 / 使用音 `Item75` / 稀有度 LightRed
    /// （原版档，**不涉月后档**）/ 价值 12 金。2.2.2 只是把伤害压到 27、把木箭判定换成
    /// <c>CalamityUtils.CheckWoodenAmmo</c>，本件按用户口径**不取**。
    /// </para>
    /// <para>
    /// 一次射出 **2 支**（±π/10 = 18°）：木箭 → <see cref="LunarBolt"/>；非木箭 → 照原弹种射出、
    /// 伤害不打折、但标记 <c>noDropItem</c>（不掉落）。贴墙时两支都往回收（照源的 <c>canHit</c> 判据）。
    /// </para>
    /// </summary>
    internal class LunarianBow : ModItem
    {
        /// <summary>一次射出的箭数（照 2.0）</summary>
        private const int ArrowsPerShot = 2;
        /// <summary>两支之间的夹角（照源 <c>MathHelper.Pi / 10</c>）</summary>
        private const float ArrowSpread = MathHelper.Pi / 10f;
        /// <summary>实际出手速度：源把速度归一化后固定乘 15（与 <c>Item.shootSpeed = 8</c> 无关）</summary>
        private const float ShotSpeed = 15f;
        /// <summary>配方里的净化凝胶数量（照源）</summary>
        private const int PurifiedGelCount = 10;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：32×62 / 伤害 32 / 使用·动画 22 帧 / 击退 2 / 弹速 8 / 箭类弹药 /
        /// 使用音 `Item75` / 浅红稀有度 / 12 金（全照 2.0）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 32;                          // 贴图宽（像素）
            Item.height = 62;                         // 贴图高（像素）
            Item.damage = 32;                         // 面板伤害（照 2.0；2.2.2 是 27）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 12, 0, 0);  // 价值 12 金
            Item.rare = ItemRarityID.LightRed;        // 浅红（原版档）
            Item.UseSound = SoundID.Item75;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LunarBolt>();
            Item.shootSpeed = 8f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 一次射出 <see cref="ArrowsPerShot"/> 支：速度为"归一到 1 再 ×15"，两支夹角 ±18°；
        /// 木箭换成 <see cref="LunarBolt"/>，非木箭照原弹种射出并标记不掉落（照 2.0 的 <c>Shoot</c>）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);

            velocity.Normalize();
            velocity *= ShotSpeed;
            bool canHit = Collision.CanHit(origin, 0, 0, origin + velocity, 0, 0);

            for (int i = 0; i < ArrowsPerShot; i++)
            {
                float offsetAmount = i - (ArrowsPerShot - 1f) / 2f;
                Vector2 offset = velocity.RotatedBy(ArrowSpread * offsetAmount);
                if (!canHit)
                    offset -= velocity;   // 贴墙时把两支都收回玩家身上（照源）

                if (type == ProjectileID.WoodenArrowFriendly)
                {
                    Projectile.NewProjectile(source, origin + offset, velocity, ModContent.ProjectileType<LunarBolt>(), damage, knockback, player.whoAmI);
                }
                else
                {
                    int arrow = Projectile.NewProjectile(source, origin + offset, velocity, type, damage, knockback, player.whoAmI);
                    if (Main.projectile.IndexInRange(arrow))
                        Main.projectile[arrow].noDropItem = true;
                }
            }
            return false;
        }
        /// <summary>
        /// 配方（照 2.0 的两条，@恶魔祭坛）：（恶魔弓 **或** 肌腱弓）+ 蜂膝弓 + 熔火之怒 + 净化凝胶×10。
        /// 唯一的外部件是**净化凝胶**，按工程口径走软依赖取名——本链两版灾厄的同名件内容一致，
        /// 故"有现代用现代、否则退经典"，避免两版都装时出现四条一模一样的配方。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("PurifiedGel", out ModItem purifiedGel))
            {
                RegisterRecipes(purifiedGel.Type);
            }
            else if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("PurifiedGel", out ModItem classicPurifiedGel))
            {
                RegisterRecipes(classicPurifiedGel.Type);
            }
            else
            {
                Mod.Logger.Warn("月光弓：两版灾厄里都找不到 PurifiedGel，配方未注册。");
            }
        }
        /// <summary>按给定的净化凝胶类型注册两条配方（恶魔弓版 / 肌腱弓版，其余三味相同）</summary>
        private void RegisterRecipes(int purifiedGelType)
        {
            CreateRecipe().
                AddIngredient(ItemID.DemonBow).
                AddIngredient(ItemID.BeesKnees).
                AddIngredient(ItemID.MoltenFury).
                AddIngredient(purifiedGelType, PurifiedGelCount).
                AddTile(TileID.DemonAltar).
                Register();

            CreateRecipe().
                AddIngredient(ItemID.TendonBow).
                AddIngredient(ItemID.BeesKnees).
                AddIngredient(ItemID.MoltenFury).
                AddIngredient(purifiedGelType, PurifiedGelCount).
                AddTile(TileID.DemonAltar).
                Register();
        }
    }
}
