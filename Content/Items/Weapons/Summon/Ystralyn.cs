using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 噬渊鞭挞（Ystralyn，移植自 CalamityEntropy 的 Content/Items/Weapons/Whips/Ystralyn.cs）：
    /// 原版鞭模板的召唤师武器，挥砍命中的敌人会被挂上噬渊标记（仆从打它多吃 90 平伤 + 15% 乘算 + 1/8 暴击）
    /// 与生命压制（4501 点/秒），同时给主人挂虚无幻象以召唤幻影妖龙。
    /// <para>
    /// 与 CE 原版的差异：⓪ 伤害做 ×8 膨胀（CE 900 → 本模组 7200；同日试过的 ×0.6 已回滚，见 <see cref="SetDefaults"/>）；
    /// ① 配方按用户要求重做（CE 原配方是 WyrmTooth×12 + FadingRunestone @ 深渊祭坛，
    /// 那三样本模组都没有；现改为 万花筒 + 猎魂鲨牙×12 + 魔影锭×5 @ 嘉登熔炉，两版灾厄分别注册，见 <see cref="AddRecipes"/>）；
    /// ② tooltip 的 <c>{0}</c> 格式参数：CE 传的是 <c>DragonWhipDebuff.TagDamage</c>（= 15，与它实际挂的
    /// WyrmWhipDebuff 对不上，是 CE 的笔误），本模组改传真实的 <c>WyrmWhipDebuff.TagDamage</c>（= 90）；
    /// ③ CE 自研稀有度 <c>AbyssalBlue</c> 按「boss 档位」口径对齐本模组的 <c>postMoonLordRarity = 15</c>（名称染紫）。
    /// </para>
    /// </summary>
    internal class Ystralyn:ModItem
    {
        /// <summary>幻影妖龙的伤害基数（CE 原值 1600，实际生成时按主人的召唤伤害加成折算）</summary>
        public static int PhantomDamage = 1600;
        /// <summary>tooltip 里的 <c>{0}</c> 取噬渊标记的平伤 tag 值</summary>
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(WyrmWhipDebuff.TagDamage);
        /// <summary>
        /// 原版鞭模板（参数序：弹幕、伤害、击退、出手速度、挥砍总帧数）：击退 2 / 出手速度 4 / 挥砍 27 帧。
        /// 伤害按 2026-09-22 用户拍板做 ×8 膨胀（CE 原值 900 → 7200）。同日曾试过再 ×0.6 削到 4320，
        /// 用户实测"伤害少了点"，当天回滚回 7200。口径同既有的 13 件 CE 武器：**只改这个 Item.damage**，
        /// 幻影妖龙的 1600、噬渊标记的 90 平伤、生命压制的 4501 点/秒都是写死的绝对数值，保持原样。
        /// </summary>
        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<YstralynProj>(), 7200, 2, 4, 27);
            Item.rare = ItemRarityID.Red;                  // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15; // 月后稀有度 15：名称染紫
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.autoReuse = true;
        }
        /// <summary>
        /// 出手时自行生成鞭弹幕并把挥砍方向写进 <c>ai[1]</c>（原版鞭 AI 据此决定挥砍摆向）：
        /// 基准 0.6~1.0 的随机值，另 1/3 概率翻到反侧并放大 2.5 倍（CE 原样）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float swingDirection = 0.6f + (0.4f * Main.rand.NextFloat());
            if (Main.rand.NextBool(3))
            {
                swingDirection *= -2.5f;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, swingDirection);
            return false;
        }
        /// <summary>
        /// 注册配方：万花筒（原版 RainbowWhip）+ 猎魂鲨牙（灾厄 ReaperTooth）×12 + 魔影锭（灾厄 ShadowspecBar）×5，
        /// 站在嘉登熔炉（灾厄 DraedonsForge）上合成。两版灾厄都有这三样，故按各自的 Mod 实例分别注册一条。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ReaperTooth", out ModItem reaperTooth) && calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar) && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    CreateRecipe()
                        .AddIngredient(ItemID.RainbowWhip)             // 万花筒
                        .AddIngredient(reaperTooth.Type, 12)           // 现代版灾厄：猎魂鲨牙×12
                        .AddIngredient(shadowspecBar.Type, 5)          // 现代版灾厄：魔影锭×5
                        .AddTile(draedonsForge.Type)                   // 现代版灾厄：嘉登熔炉
                        .Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("ReaperTooth", out ModItem classicReaperTooth) && calamity1.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    CreateRecipe()
                        .AddIngredient(ItemID.RainbowWhip)                   // 万花筒
                        .AddIngredient(classicReaperTooth.Type, 12)          // 经典版灾厄：猎魂鲨牙×12
                        .AddIngredient(classicShadowspecBar.Type, 5)         // 经典版灾厄：魔影锭×5
                        .AddTile(classicDraedonsForge.Type)                  // 经典版灾厄：嘉登熔炉
                        .Register();
                }
            }
        }
        /// <summary>允许该物品吃到近战前缀（CE 原样）</summary>
        public override bool MeleePrefix()
        {
            return true;
        }
    }
}
