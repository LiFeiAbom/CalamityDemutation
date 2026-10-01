using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 死神擢升（Death's Ascension，移植自灾厄 2.0.4 的 <c>DeathsAscension</c>）——
    /// 左键：引导式巨镰挥砍（<see cref="DeathsAscensionSwing"/>），按住左键持续挥砍、松手收招；
    /// 右键：普通挥砍并打开剑身判定，每次甩出 4 把追踪飞镰（<see cref="DeathsAscensionProjectile"/>，伤害 ×0.125，±9 随机散布）。
    /// 1200 伤害（数值膨胀开关开启时面板回调到 1350）、24 帧、击退 9、射速 12；
    /// 纯绿稀有度（灾厄 Rarity 13）对应本工程月后稀有度 13（荧光绿）。
    /// </summary>
    internal class DeathsAscension:ModItem
    {
        /// <summary>静态属性：研究所解锁数量设为 1；允许右键连续触发（甩镰可连续使用）。</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：70×70、伤害 1200（源值；数值膨胀开关开启时面板回调到 1350）、
        /// 24 帧使用、击退 9、射速 12；
        /// 默认是左键的引导状态（<c>channel + Shoot</c>、无剑身判定、不出贴图），
        /// 实际配置在 <see cref="CanUseItem"/> 里按左右键切换。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 70;
            Item.height = 70;
            Item.damage = 1200;
            Item.knockBack = 9f;
            Item.useTime = Item.useAnimation = 24;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;                            // 左键：伤害全交给引导挥砍弹幕
            Item.channel = true;                            // 左键按住持续挥砍
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shoot = ModContent.ProjectileType<DeathsAscensionSwing>();
            Item.shootSpeed = 12f;
            Item.value = Item.buyPrice(1, 75, 0, 0);
            Item.rare = ItemRarityID.Red;                   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13（荧光绿，同 Bloodflare/OmegaBlue）
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：死神擢升 1200 → 1350）。
        /// </summary>
        private const float InflatedDamage = 1350f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 1200。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 引导挥砍体与 4 把飞镰（<c>damage * 0.125</c>）都走传进来的 <c>damage</c>，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>右键可用（甩镰）</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 按左右键切换两套状态（照抄灾厄 2.0.4 的 <c>CanUseItem</c>）：
        /// 右键 = 普通挥砍（开剑身判定、出贴图、不引导、Item71 音效、可自动连挥）；
        /// 左键 = 引导挥砍（关剑身判定、不出贴图、静音、不可自动连挥、按住持续）。
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.UseSound = SoundID.Item71;
                Item.useTurn = true;
                Item.autoReuse = true;
                Item.noMelee = false;
                Item.noUseGraphic = false;
                Item.channel = false;
                Item.shoot = ModContent.ProjectileType<DeathsAscensionProjectile>();
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.UseSound = null;
                Item.useTurn = false;
                Item.autoReuse = false;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.channel = true;
                Item.shoot = ModContent.ProjectileType<DeathsAscensionSwing>();
            }
            return base.CanUseItem(player);
        }
        /// <summary>
        /// 出手：左键召唤引导挥砍弹幕；右键甩出 4 把追踪飞镰，伤害为面板的 1/8（×0.125），带 ±9 的随机散布。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<DeathsAscensionSwing>(), damage, knockback, player.whoAmI, 0f, 0f);
                return false;
            }
            int spreadfactor = 9;
            for (int index = 0; index < 4; ++index)
            {
                float SpeedX = velocity.X + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1);
                float SpeedY = velocity.Y + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1);
                Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, type, (int)(damage * 0.125f), knockback, player.whoAmI, 0f, 0f);
            }
            return false;
        }
        /// <summary>
        /// 配方：死神镰刀 + 毁灭之魂×4 + 扭曲虚空×1 + 暗影之魂×15，在远古操纵台合成。
        /// 四个材料在现代版 / 经典版灾厄中均存在，故分两条同名配方注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul)
                    && calamity.TryFind<ModItem>("TwistingNether", out ModItem twistingNether))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.DeathSickle);
                    recipe.AddIngredient(ruinousSoul.Type, 4);
                    recipe.AddIngredient(twistingNether.Type);
                    recipe.AddIngredient(ItemID.SoulofNight, 15);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul)
                    && classic.TryFind<ModItem>("TwistingNether", out ModItem classicTwistingNether))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ItemID.DeathSickle);
                    recipeClassic.AddIngredient(classicRuinousSoul.Type, 4);
                    recipeClassic.AddIngredient(classicTwistingNether.Type);
                    recipeClassic.AddIngredient(ItemID.SoulofNight, 15);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
