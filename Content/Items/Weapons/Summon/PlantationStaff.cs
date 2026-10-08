using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 苍华之庭（Plantation Staff）—— 「归虚之灵」链的**最低下位**，吃它的是元素之斧。
    /// 老规矩取灾厄 2.0.3.9：46×48、伤害 **58**、击退 1、魔力 10、使用/动画 **20 帧**、
    /// **黄档**、价值 **60 金**（`Rarity8BuyPrice`）、音 `SoundID.Item76`，召唤一头小型世纪之花
    /// （<see cref="PlantationStaffSummon"/>），全场只能同时存在一头。
    /// </summary>
    /// <remarks>
    /// 为什么要自持：本件在现代版里**仍在**，但被重制过——2.0.4 就把尺寸改成 50×50、加了世界发光贴图、
    /// 换掉价码写法，1.4.4 更把使用帧改成 36、占用改成 3 栏、配方里去掉刃杖。本工程按
    /// "旧版回归 + 同名不同物"的老做法自持一份 2.0.3.9 版，供元素之斧当材料。
    /// <para>
    /// 战斗节奏（源写法）：树灵平时跟在主人身边；有目标时进入四状态循环——
    /// **荆棘球**（每 90 帧朝目标甩一颗会钉在敌人身上的荆棘球，共 2 颗）→
    /// **撒种**（每 10 帧朝预判点射一枚种子、3 枚一波共 3 波）→
    /// **冲撞**（先蓄力 15 帧，再以 35 的速度反复冲锋 240 帧；进入冲撞那一刻喷 12 团孢子云并挂 6 条触手，
    /// 冲撞期间命中伤害 ×2，离开冲撞后触手会从宿主身上脱落自行追击）。
    /// </para>
    /// <para>
    /// 配方（照源 2.0.3.9）：夜眼 `EyeOfNight` + 刃杖 `ItemID.Smolstar` + 生命碎片 `LivingShard`×12 @ 秘银砧。
    /// **经典分支（`CalamityModClassicPreTrailer`）里没有夜眼**，那一味怎么补等用户拍板，故暂只注册现代分支。
    /// </para>
    /// </remarks>
    internal class PlantationStaff:ModItem
    {
        // ── 源里写在物品上的可调参数（弹幕直接读这几个静态字段，照源保留）──
        /// <summary>索敌半径（源值 1600）</summary>
        public static float EnemyDistanceDetection = 1600f;
        /// <summary>追击/冲撞速度（源值 35）</summary>
        public static float ChargingSpeed = 35f;
        /// <summary>每轮甩出的荆棘球数量（源值 2）</summary>
        public static int ThornballAmount = 2;
        /// <summary>荆棘球发射间隔（帧，源值 90）</summary>
        public static float ThornballFireRate = 90f;
        /// <summary>荆棘球速度（源值 20）</summary>
        public static float ThornballSpeed = 20f;
        /// <summary>进入撒种状态后多久开始撒（帧，源值 30）</summary>
        public static float SeedBurstDelay = 30f;
        /// <summary>同一波内两枚种子的间隔（帧，源值 10）</summary>
        public static float SeedBetweenBurstDelay = 10f;
        /// <summary>种子速度（源值 25）</summary>
        public static float SeedSpeed = 25f;
        /// <summary>每波的种子数（源值 3）</summary>
        public static int SeedAmountPerBurst = 3;
        /// <summary>撒种波数（源值 3）</summary>
        public static int SeedBurstAmount = 3;
        /// <summary>孢子云初速（源值 3）</summary>
        public static float SporeStartVelocity = 3f;
        /// <summary>冲撞前的蓄力时长（帧，源值 15）</summary>
        public static float TimeBeforeRamming = 15f;
        /// <summary>冲撞持续时长（帧，源值 240）</summary>
        public static float RamTime = 240f;
        /// <summary>触手追击速度（源值 25）</summary>
        public static float TentacleSpeed = 25f;

        /// <summary>研究解锁一份（源 2.0.3.9 由 tML 自动按稀有度决定，这里显式给 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：照源 2.0.3.9（46×48、伤害 58、击退 1、使用 20 帧、黄档、60 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 46;
            Item.height = 48;
            Item.damage = 58;
            Item.DamageType = DamageClass.Summon;
            Item.shoot = ModContent.ProjectileType<PlantationStaffSummon>();
            Item.knockBack = 1f;
            Item.mana = 10;
            Item.useTime = Item.useAnimation = 20;
            Item.noMelee = true;
            Item.value = Item.buyPrice(0, 60, 0, 0);   // 源用 Rarity8BuyPrice = buyPrice(0, 60, 0, 0)
            Item.rare = ItemRarityID.Yellow;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item76;
        }
        /// <summary>场上已有自己的树灵时不可再召唤（源用 ownedProjectileCounts 卡单只）</summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>在鼠标处召唤树灵，并给一点点随机初速（源写法：左右键分支里只有左键生效，照抄）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                Projectile.NewProjectile(source, Main.MouseWorld, Main.rand.NextVector2Circular(2f, 2f), type, damage, knockback, player.whoAmI);
            }
            return false;
        }
        /// <summary>
        /// 配方（照源 2.0.3.9）：夜眼 `EyeOfNight` + 刃杖 `ItemID.Smolstar` + 生命碎片 `LivingShard`×12 @ 秘银砧。
        /// 暂只注册现代分支——经典版没有夜眼，那一味待用户拍板后再补。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("EyeOfNight", out ModItem eyeOfNight) &&
                calamity.TryFind<ModItem>("LivingShard", out ModItem livingShard))
            {
                CreateRecipe().
                    AddIngredient(eyeOfNight.Type).
                    AddIngredient(ItemID.Smolstar).
                    AddIngredient(livingShard.Type, 12).
                    AddTile(TileID.MythrilAnvil).
                    Register();
                return;
            }
            Mod.Logger.Warn("苍华之庭：找不到 夜眼 / 生命碎片（当前可能是经典版灾厄），配方未注册。");
        }
    }
}
