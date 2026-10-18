using System;
using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Magic
{
    /// <summary>
    /// 光之舞（Dance of Light）—— 机制移植自 CI Beta1.12 的
    /// <c>DanceofLightLegacy</c>，物品贴图使用现代版 <c>TheDanceofLight.png</c>（40×42）。
    /// <para>
    /// 面板取 CI（伤害按用户 2026-10-18 点名改为 200）：魔力 9、使用/动画 15 帧、击退 4、弹速 30、
    /// 月后 16 档（CI 的 <c>DonatorPink</c>）/ 2 铂金 80 金（CI 的 <c>RarityPriceDonatorPink</c>）、
    /// <c>Item105</c> 使用音。判定框按现代贴图取 40×42，持握偏移用现代版的零偏移。
    /// </para>
    /// <para>
    /// **配方按用户 2026-10-18 指定的 2.0 版**：
    /// 天界破晓 <c>SkyFracture</c> + 月耀书 <c>LunarFlareBook</c> + <see cref="WrathoftheAncients"/>
    /// + 魔影锭 <c>ShadowspecBar</c>×5 @ 嘉登熔炉 <c>DraedonsForge</c>。
    /// 现代 / 经典两条分支的材料与站台各自软依赖；宿主已有 <c>WrathoftheAncients</c> 时优先用宿主件，
    /// 没有（如实装 2.2.2）时用本工程自持件。
    /// </para>
    /// <para>
    /// **数值膨胀**：用户 2026-10-18 指定 **基础 200 / 膨胀 700**。
    /// </para>
    /// </summary>
    internal class DanceofLight : ModItem
    {
        /// <summary>膨胀态面板（用户 2026-10-18 指定）</summary>
        private const float InflatedDamage = 700f;
        /// <summary>当前生效的面板基础伤害：开则 700，否则维持用户指定的基础值 200</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性取 CI（伤害按用户点名 200）；判定框按现代版 40×42 贴图</summary>
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 42;
            Item.damage = 200;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 9;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item105;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WhiteFlame>();
            Item.shootSpeed = 30f;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(2, 80, 0, 0);
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
        }
        /// <summary>持握偏移照现代版（零偏移）</summary>
        public override Vector2? HoldoutOffset() => Vector2.Zero;
        /// <summary>数值膨胀：面板基础伤害改读 <see cref="BaseDamage"/>，弹幕走 Shoot 的 damage 参数自动跟随</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>
        /// 出手照 CI：4~6 枚白焰从玩家上方不同高度朝鼠标坠落；
        /// 每枚弹幕命中挂圣火，消亡后再分裂 2 枚全局追踪火焰。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float shotSpeed = Item.shootSpeed;
            Vector2 origin = player.RotatedRelativePoint(player.MountedCenter, true);
            float targetX = Main.mouseX + Main.screenPosition.X - origin.X;
            float targetY = Main.mouseY + Main.screenPosition.Y - origin.Y;
            if (player.gravDir == -1f)
            {
                targetY = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - origin.Y;
            }
            float distance = (float)Math.Sqrt(targetX * targetX + targetY * targetY);
            if ((float.IsNaN(targetX) && float.IsNaN(targetY)) || (targetX == 0f && targetY == 0f))
            {
                targetX = player.direction;
                targetY = 0f;
                distance = shotSpeed;
            }
            else
            {
                distance = shotSpeed / distance;
            }

            int flameCount = Main.rand.Next(4, 7);
            for (int i = 0; i < flameCount; i++)
            {
                origin = new Vector2(
                    player.position.X + player.width * 0.5f
                        + Main.rand.Next(201) * -player.direction
                        + (Main.mouseX + Main.screenPosition.X - player.position.X),
                    player.MountedCenter.Y - 600f);
                origin.X = (origin.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
                origin.Y -= 100f * i;

                targetX = Main.mouseX + Main.screenPosition.X - origin.X;
                targetY = Main.mouseY + Main.screenPosition.Y - origin.Y;
                if (targetY < 0f)
                {
                    targetY *= -1f;
                }
                if (targetY < 20f)
                {
                    targetY = 20f;
                }
                distance = (float)Math.Sqrt(targetX * targetX + targetY * targetY);
                distance = shotSpeed / distance;
                targetX *= distance;
                targetY *= distance;

                float speedX = targetX + Main.rand.Next(-30, 31) * 0.02f;
                float speedY = targetY + Main.rand.Next(-30, 31) * 0.02f;
                Projectile.NewProjectile(source, origin.X, origin.Y, speedX, speedY, type, damage, knockback, player.whoAmI, 0f, Main.rand.Next(15));
            }
            return false;
        }
        /// <summary>
        /// 2.0 配方（现代 / 经典各一条）：天界破晓 + 月耀书 + 远古之怒 + 魔影锭×5 @ 嘉登熔炉。
        /// 宿主已有远古之怒时优先用宿主件，避免同功能件重复占用配方。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                    && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.SkyFracture);
                    recipe.AddIngredient(ItemID.LunarFlareBook);
                    if (calamity.TryFind<ModItem>("WrathoftheAncients", out ModItem modernWrath))
                    {
                        recipe.AddIngredient(modernWrath.Type);
                    }
                    else
                    {
                        recipe.AddIngredient(ModContent.ItemType<WrathoftheAncients>());
                    }
                    recipe.AddIngredient(shadowspecBar.Type, 5);
                    recipe.AddTile(draedonsForge.Type);
                    recipe.Register();
                }
                else
                {
                    Mod.Logger.Warn("光之舞：现代版灾厄里找不到 ShadowspecBar / DraedonsForge，现代配方未注册。");
                }
            }

            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicForge))
            {
                Recipe classicRecipe = CreateRecipe();
                classicRecipe.AddIngredient(ItemID.SkyFracture);
                classicRecipe.AddIngredient(ItemID.LunarFlareBook);
                if (classic.TryFind<ModItem>("WrathoftheAncients", out ModItem classicWrath))
                {
                    classicRecipe.AddIngredient(classicWrath.Type);
                }
                else
                {
                    classicRecipe.AddIngredient(ModContent.ItemType<WrathoftheAncients>());
                }
                classicRecipe.AddIngredient(classicShadowspecBar.Type, 5);
                classicRecipe.AddTile(classicForge.Type);
                classicRecipe.Register();
            }
        }
    }
}
