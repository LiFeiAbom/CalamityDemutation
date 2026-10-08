using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// 震爆手雷（Shock Grenade）—— 「超新星」那条链的**最下位材料**（超新星 2.0 的配方要它 ×200）。
    /// <para>
    /// 为什么本模组要自备：它在灾厄 **1.4.4-release 起被删除**（本机实装 2.2.2 的 .tmod 文件表里
    /// `ShockGrenade*` 一条都没有），而 2.0 / 2.0.3.9 / 2.0.4 / 2.0.7.2 都还在。本件按老规矩取 **2.0**：
    /// 14×30、伤害 90、击退 1、使用 18 帧、黄档、价值 1 金、堆叠 999、消耗品；
    /// 2.0.3.9 起被本体抬到 108 伤害 / 堆叠 9999 / 每次合成 150 —— 本件刻意保留 2.0 的 90 / 999 / 100。
    /// </para>
    /// <para>
    /// 效果：投出一枚手雷，落地或命中时炸成 5~10 道闪电，闪电与爆炸都挂「带电」减益；
    /// **潜行打击**额外让闪电自动追踪目标，并在原地留下一个 4 秒的电气光环。
    /// </para>
    /// <para>
    /// 配方照 2.0（四版完全一致，且**全是原版材料**，因此现代/经典两个分支共用同一条、不写分支）：
    /// 手榴弹×20 + 火星管道板×5 + 纳米机器人×5 @ 工作台 → 每次合成 100 枚。
    /// </para>
    /// <para>
    /// 潜行打击走 <see cref="CDUtil.CanStealthStrike"/> / <see cref="CDUtil.SetStealthStrike"/>
    /// （现代版是灾厄官方 ModCall，只装经典版时恒不触发——经典版没有潜行打击这套机制，
    /// 它的盗贼走自定义投掷倍率）。<c>Shoot</c> 返回 false 的那一支即潜行打击，与源实现一致。
    /// </para>
    /// <para>
    /// 音效全为原版音（爆炸 <c>SoundID.Item94</c>、光环 <c>SoundID.Item93</c>），
    /// 不引入新的音频资源，避免 `Sounds/` 同名双扩展名那类加载期硬错。
    /// </para>
    /// </summary>
    internal class ShockGrenade : ModItem
    {
        /// <summary>研究解锁一份即可（源 2.0 写 SacrificeTotal = 99，1.4.4 对应字段是 ResearchUnlockCount）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }
        /// <summary>
        /// 基础属性：14×30、伤害 90、击退 1、使用/动画各 18 帧、消耗品叠 999、黄档 1 金；
        /// 射出手雷、弹速 12.5，伤害类型取**真·盗贼类**（只装经典版时回退 tML 的 Throwing）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 30;
            Item.damage = 90;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.consumable = true;
            Item.useAnimation = Item.useTime = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 1f;
            Item.autoReuse = true;
            Item.maxStack = 999;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<ShockGrenadeProjectile>();
            Item.shootSpeed = 12.5f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>
        /// 潜行打击就绪时自己生成一枚、并把它标记成潜行打击（灾厄的 <c>projectile.Calamity().stealthStrike</c>）；
        /// 否则返回 true 走默认投掷路径。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int grenade = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (grenade >= 0 && grenade < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[grenade]);
            return false;
        }
        /// <summary>世界中的物品绘制发光层（14×30 的 ShockGrenadeGlow）</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Rogue/ShockGrenadeGlow").Value);
        }
        /// <summary>
        /// 配方照 2.0：手榴弹×20 + 火星管道板×5 + 纳米机器人×5 @ 工作台 → 每次 100 枚
        /// （全原版材料，现代/经典两分支通用，故只注册一条）
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe(100).
                AddIngredient(ItemID.Grenade, 20).
                AddIngredient(ItemID.MartianConduitPlating, 5).
                AddIngredient(ItemID.Nanites, 5).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}
