using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 太阳神杖（Sun God Staff）—— 「太阳神杖 / 天狼星」召唤链的**链顶**，吃链中间的太阳之灵法杖。
    /// 老规矩取灾厄 2.0：72x72 判定（贴图同为 72x72）、伤害 60、魔力 10、击退 1.25、使用 25 帧、
    /// 浅紫档、价值 48 金、音效 SoundID.Item44，召唤太阳神（SolarGod），全场只能同时存在一只。
    /// </summary>
    /// <remarks>
    /// 本件在现代版灾厄 2.0.3.9 起已被删除（换成 VengefulSunStaff），故按"旧版回归"自持。
    /// <para>
    /// 与源的差异：源用灾厄 CalamityUtils.KillShootProjectiles 清场，本工程是软依赖、不引用灾厄类型，
    /// 照经典版写等价的"先杀掉自己在场的同类再召唤"循环。
    /// </para>
    /// </remarks>
    internal class SunGodStaff:ModItem
    {
        /// <summary>研究解锁 1 个（照源 2.0 的 SacrificeTotal）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：照源 2.0（伤害 60 / 魔力 10 / 击退 1.25 / 使用 25 帧 / 浅紫档 / 48 金）</summary>
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.mana = 10;
            Item.width = 72;
            Item.height = 72;
            Item.useTime = Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 1.25f;
            Item.value = Item.buyPrice(0, 48, 0, 0);
            Item.rare = ItemRarityID.LightPurple;
            Item.UseSound = SoundID.Item44;
            Item.shoot = ModContent.ProjectileType<SolarGod>();
            Item.DamageType = DamageClass.Summon;
        }
        /// <summary>场上已有自己的太阳神时不可再召唤（源用 ownedProjectileCounts 卡单一灵体）</summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>
        /// 召唤太阳神：先清掉自己在场的同类（源靠 CalamityUtils，这里照经典版写等价循环），
        /// 在玩家的使用位置生成弹幕并把原始面板伤害写进 originalDamage（供召唤加成结算）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 清场：自己名下的同类召唤物全部移除，保证"只有一个灵体"
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                {
                    p.Kill();
                }
            }
            int idx = Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI);
            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].originalDamage = Item.damage;
            }
            return false;
        }
        /// <summary>
        /// 配方（用户 2026-10-08 拍板）：两分支各注册一条，缺件即退化成单分支——
        /// 现代分支照源 2.0 = 太阳之灵法杖 + 日光精华×5 + 三魂各×3 @ 秘银砧；
        /// 经典分支照源 cal-1.4.2.101 = 太阳之灵法杖 + 烬核×5（用本模组自持件，不借经典版的 EssenceofCinder）
        /// + 三魂各×3 @ 秘银砧。
        /// </summary>
        public override void AddRecipes()
        {
            bool modern = TryAddRecipeFrom("CalamityMod", "EssenceofSunlight");
            bool classic = TryAddRecipeFrom("CalamityModClassicPreTrailer", null);
            if (!modern && !classic)
                Mod.Logger.Warn("太阳神杖：两版灾厄都不在场，配方未注册。");
        }
        /// <summary>
        /// 从指定灾厄版本注册一条配方：精华名非空时按源取该版本的同名精华（现代分支的日光精华），
        /// 为空时改用本模组自持的烬核（经典分支）；缺精华或缺模组即返回 false，不落半条配方。
        /// </summary>
        private bool TryAddRecipeFrom(string calamityModName, string essenceName)
        {
            if (!ModLoader.TryGetMod(calamityModName, out Mod calamity))
                return false;
            int essenceType = ModContent.ItemType<CoreofCinder>();
            if (essenceName != null)
            {
                if (!calamity.TryFind<ModItem>(essenceName, out ModItem essence))
                    return false;
                essenceType = essence.Type;
            }
            CreateRecipe().
                AddIngredient<SunSpiritStaff>().
                AddIngredient(essenceType, 5).
                AddIngredient(ItemID.SoulofMight, 3).
                AddIngredient(ItemID.SoulofSight, 3).
                AddIngredient(ItemID.SoulofFright, 3).
                AddTile(TileID.MythrilAnvil).
                Register();
            return true;
        }
    }
}
