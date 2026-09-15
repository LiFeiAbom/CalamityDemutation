using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 远古方舟 - 神圣系近战剑
    /// 挥砍附带永恒光束（可切换为附魔光束），并召唤圣星从天而降
    /// </summary>
    internal class ArkoftheAncients:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 92、使用时间 22 帧、击退 6.25、淡紫稀有度；
        /// 主弹幕为永恒光束（EonBeam），每次挥砍发射一次（shootsEveryUse）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 50;
            Item.damage = 92;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 22;
            Item.useTime = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6.25f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.height = 50;
            Item.value = Item.buyPrice(0, 48, 0, 0);
            Item.rare = ItemRarityID.LightPurple;
            Item.shoot = ModContent.ProjectileType<EonBeam>();
            Item.shootSpeed = 10f;
            Item.shootsEveryUse = true;
        }
        /// <summary>
        /// 射击逻辑：随机发射永恒光束或附魔光束，并按类型微调穿透/更新次数；
        /// 再从玩家上方 600 像素处召唤 2 颗圣星砸向鼠标位置。
        /// 返回 false 表示由本方法自行管理弹幕生成。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 随机选择主弹幕：本模组的永恒光束 或 原版附魔光束
            type = Utils.SelectRandom(Main.rand, new int[]
            {
                ModContent.ProjectileType < EonBeam >(),
                ProjectileID.EnchantedBeam
            });
            int beam = Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, damage, knockback, Main.myPlayer);
            if (Main.projectile[beam].type == ModContent.ProjectileType<EonBeam>())
                Main.projectile[beam].penetrate = 2;    // 永恒光束穿透 2 个敌人
            if (Main.projectile[beam].type == ProjectileID.EnchantedBeam)
                Main.projectile[beam].extraUpdates = 1; // 附魔光束加速飞行
            float num72 = Main.rand.Next(18, 25);
            Vector2 vector2 = player.RotatedRelativePoint(player.MountedCenter, true);
            float num78 = Main.mouseX + Main.screenPosition.X - vector2.X;
            float num79 = Main.mouseY + Main.screenPosition.Y - vector2.Y;
            if (player.gravDir == -1f)
            {
                num79 = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - vector2.Y;
            }
            float num80 = (float)Math.Sqrt(num78 * num78 + num79 * num79);
            if (float.IsNaN(num78) && float.IsNaN(num79) || num78 == 0f && num79 == 0f)
            {
                num78 = player.direction;
                num79 = 0f;
                num80 = num72;
            }
            else
            {
                num80 = num72 / num80;
            }
            int num107 = 2;
            for (int num108 = 0; num108 < num107; num108++)
            {
                vector2 = new Vector2(player.position.X + player.width * 0.5f + Main.rand.Next(201) * -player.direction + (Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
                vector2.X = (vector2.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
                vector2.Y -= 100 * num108;
                num78 = Main.mouseX + Main.screenPosition.X - vector2.X;
                num79 = Main.mouseY + Main.screenPosition.Y - vector2.Y;
                if (num79 < 0f)
                {
                    num79 *= -1f;
                }
                if (num79 < 20f)
                {
                    num79 = 20f;
                }
                num80 = (float)Math.Sqrt(num78 * num78 + num79 * num79);
                num80 = num72 / num80;
                num78 *= num80;
                num79 *= num80;
                float speedX4 = num78 + Main.rand.Next(-120, 121) * 0.02f;
                float speedY5 = num79 + Main.rand.Next(-120, 121) * 0.02f;
                int proj = Projectile.NewProjectile(source, vector2.X, vector2.Y, speedX4, speedY5, ProjectileID.HallowStar, damage / 3, knockback, player.whoAmI, 0f, Main.rand.Next(5));
                Main.projectile[proj].DamageType = DamageClass.Melee;
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正武器挥舞位置，并随机生成金色/荧光/蓝色粉尘粒子
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(5))
            {
                // 随机选择三种神圣系粉尘之一
                int dustType = 15;
                switch (Main.rand.Next(3))
                {
                    case 0:
                        dustType = 15;
                        break;
                    case 1:
                        dustType = 57;
                        break;
                    case 2:
                        dustType = 58;
                        break;
                    default:
                        break;
                }
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, dustType, player.direction * 2, 0f, 150, default, 1.3f);
                Main.dust[dust].velocity *= 0.2f;
            }
        }
        /// <summary>
        /// 命中敌人：50% 概率施加霜冻 debuff（5 秒）
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Frostburn, 300);
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：同样 50% 概率施加霜冻
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Frostburn, 300);
            }
        }
        /// <summary>
        /// 配方（分版本）：现代版用日光/极寒精华；经典版改用余烬精华，并提供 圣剑/Arkhalis 两种替代路线
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight)
                    && calamity.TryFind<ModItem>("EssenceofEleum", out ModItem essenceofEleum))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(essenceofSunlight.Type, 3);
                    recipe.AddIngredient(essenceofEleum.Type, 3);
                    recipe.AddIngredient(ItemID.Starfury);
                    recipe.AddIngredient(ItemID.EnchantedSword);
                    recipe.AddTile(TileID.Anvils);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder)
                    && calamity1.TryFind<ModItem>("EssenceofEleum", out ModItem classicEssenceofEleum))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(essenceofCinder.Type, 3);
                    recipe1.AddIngredient(classicEssenceofEleum.Type, 3);
                    recipe1.AddIngredient(ItemID.Starfury);
                    recipe1.AddIngredient(ItemID.EnchantedSword);
                    recipe1.AddIngredient(ItemID.Excalibur);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                    recipe1 = CreateRecipe();
                    recipe1.AddIngredient(essenceofCinder.Type, 3);
                    recipe1.AddIngredient(classicEssenceofEleum.Type, 3);
                    recipe1.AddIngredient(ItemID.Starfury);
                    recipe1.AddIngredient(ItemID.Arkhalis);
                    recipe1.AddIngredient(ItemID.Excalibur);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
