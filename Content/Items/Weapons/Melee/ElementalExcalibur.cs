using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    internal class ElementalExcalibur : ModItem
    {
        private int BeamType = 0;
        private const int alpha = 50;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.damage = 10000;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 14;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.width = 112;
            Item.height = 112;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.shoot = ModContent.ProjectileType<ElementalExcaliburBeam>();
            Item.shootSpeed = 12f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
        }
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10;
        /// <summary>
        /// 按左右键切换整套武器状态（对齐灾厄 PrismaticBreaker 的双模式写法）：
        /// 左键 = 普通挥砍 + 彩虹光束；
        /// 右键 = 由元素圣剑手持弹幕驱动（蓄力 → 魔力阵 → 激光），必须开 channel 才能让手持弹幕常驻，
        /// 并关掉本体挥砍（noMelee）与手持贴图（noUseGraphic，改由手持弹幕自己画武器）。
        /// <para>
        /// 关键：<see cref="Item.shoot"/> 也要按键切换——右键时必须指向手持弹幕，否则 channel 机制会去生成
        /// 左键的光束、手持弹幕根本不出现（本工程 DragonRage 就是这么接的）。
        /// </para>
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useTurn = false;
                Item.autoReuse = true;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.channel = true;
                Item.shootsEveryUse = false;
                Item.UseSound = CalamityDemutationSounds.CrystylCharge;
                Item.shoot = ModContent.ProjectileType<ElementalExcaliburBreakerHoldout>();
                // 手持弹幕已在场时不再允许"再次使用"，其后由 channel 维持（与 DragonRage 同款闸门）
                if (player.ownedProjectileCounts[Item.shoot] > 0)
                    return false;
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTurn = false;
                Item.autoReuse = true;
                Item.noMelee = false;
                Item.noUseGraphic = false;
                Item.channel = false;
                Item.shootsEveryUse = true;
                Item.UseSound = SoundID.Item1;
                Item.shoot = ModContent.ProjectileType<ElementalExcaliburBeam>();
            }
            return base.CanUseItem(player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 右键：手动生成元素圣剑手持弹幕（光棱破碎者的攻击）。
            // 不赌 tML 的 channel 生成机制——本武器左右键共用一件物品，channel 状态不可靠；
            // 已在场时不重复生成，之后的位移/命中/激光全由该弹幕自己结算
            if (player.altFunctionUse == 2)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<ElementalExcaliburBreakerHoldout>()] < 1)
                    Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ElementalExcaliburBreakerHoldout>(), damage, knockback, player.whoAmI);
                return false;
            }
            // 左键：彩虹光束（BeamType 每发递增、0~11 循环，供弹幕选色）
            Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, damage, knockback, player.whoAmI, BeamType, 0f);
            BeamType++;
            if (BeamType > 11)
                BeamType = 0;
            return false;
        }
        public override bool AltFunctionUse(Player player) => true;
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(4))
            {
                Color color = new Color(255, 0, 0, alpha);
                switch (BeamType)
                {
                    case 0: // Red
                        break;
                    case 1: // Orange
                        color = new Color(255, 128, 0, alpha);
                        break;
                    case 2: // Yellow
                        color = new Color(255, 255, 0, alpha);
                        break;
                    case 3: // Lime
                        color = new Color(128, 255, 0, alpha);
                        break;
                    case 4: // Green
                        color = new Color(0, 255, 0, alpha);
                        break;
                    case 5: // Turquoise
                        color = new Color(0, 255, 128, alpha);
                        break;
                    case 6: // Cyan
                        color = new Color(0, 255, 255, alpha);
                        break;
                    case 7: // Light Blue
                        color = new Color(0, 128, 255, alpha);
                        break;
                    case 8: // Blue
                        color = new Color(0, 0, 255, alpha);
                        break;
                    case 9: // Purple
                        color = new Color(128, 0, 255, alpha);
                        break;
                    case 10: // Fuschia
                        color = new Color(255, 0, 255, alpha);
                        break;
                    case 11: // Hot Pink
                        color = new Color(255, 0, 128, alpha);
                        break;
                    default:
                        break;
                }
                Dust dust24 = Main.dust[Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.RainbowMk2, 0f, 0f, alpha, color, 1.2f)];
                dust24.noGravity = true;
            }
        }
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Melee/ElementalExcaliburGlow").Value);
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 真近战命中回血：超上限的部分夹回最大值，否则血条会先冲高再被原版夹回，出现血量跳变
            int healAmount = Main.rand.Next(3) + 10;
            player.statLife += healAmount;
            player.HealEffect(healAmount);
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("VulnerabilityHex", out ModBuff vulnerabilityHex))
                    target.AddBuff(vulnerabilityHex.Type, 600);
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight))
                    target.AddBuff(miracleBlight.Type, 600);
                if (calamity.TryFind<ModBuff>("Dragonfire", out ModBuff dragonfire))
                    target.AddBuff(dragonfire.Type, 600);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("DemonFlames", out ModBuff demonFlames))
                    target.AddBuff(demonFlames.Type, 600);
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 600);
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight))
                    target.AddBuff(holyLight.Type, 600);
            }
        }
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            // 同 OnHitNPC：真近战命中回血，超上限则夹回最大值
            int healAmount = Main.rand.Next(3) + 10;
            player.statLife += healAmount;
            player.HealEffect(healAmount);
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("VulnerabilityHex", out ModBuff vulnerabilityHex))
                    target.AddBuff(vulnerabilityHex.Type, 600);
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight))
                    target.AddBuff(miracleBlight.Type, 600);
                if (calamity.TryFind<ModBuff>("Dragonfire", out ModBuff dragonfire))
                    target.AddBuff(dragonfire.Type, 600);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("DemonFlames", out ModBuff demonFlames))
                    target.AddBuff(demonFlames.Type, 600);
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 600);
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight))
                    target.AddBuff(holyLight.Type, 600);
            }
        }
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.TrueExcalibur);
                recipe.AddIngredient<GreatswordofJudgement>();
                recipe.AddIngredient(calamity.Find<ModItem>("ShadowspecBar").Type, 5);
                recipe.AddTile(calamity.Find<ModTile>("DraedonsForge").Type);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.TrueExcalibur);
                recipe1.AddIngredient<GreatswordofJudgement>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("ShadowspecBar").Type, 5);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
