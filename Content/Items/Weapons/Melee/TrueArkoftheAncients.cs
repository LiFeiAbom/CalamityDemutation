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
    /// 真·远古方舟 - 远古方舟的进阶形态
    /// 发射强化版永恒光束，并召唤圣星与大地弹（TerraBall）混合攻击
    /// </summary>
    internal class TrueArkoftheAncients:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 113、使用时间 22 帧、击退 6.5、黄色稀有度；
        /// 主弹幕为永恒光束（EonBeam），每次挥砍发射一次。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.damage = 113;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 22;
            Item.useTime = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.height = 60;
            Item.value = Item.buyPrice(0, 80, 0, 0); // 80 金
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<EonBeam>();
            Item.shootSpeed = 10f;
            Item.shootsEveryUse = true;
        }
        /// <summary>
        /// 射击逻辑：主弹幕为 75% 伤害/速度的永恒光束（局部命中冷却 14、穿透 2），
        /// 再从玩家上方召唤 2~3 组圣星 + 大地弹混合坠落
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile beam = Projectile.NewProjectileDirect(source, position, velocity * 0.75f, type, (int)(damage * 0.75), knockback, Main.myPlayer);
            if (beam.active)
            {
                beam.localNPCHitCooldown = 14;
                beam.penetrate = 2;
                beam.ai[1] = Main.rand.Next(1, 3);
            }
            int i = Main.myPlayer;
            float num72 = Main.rand.Next(18, 27);
            float adjustedKnockback = player.GetWeaponKnockback(Item, knockback);
            player.itemTime = Item.useTime;
            Vector2 vector2 = player.RotatedRelativePoint(player.MountedCenter, true);
            Vector2 value = Vector2.UnitX.RotatedBy(player.fullRotation);
            Vector2 vector3 = Main.MouseWorld - vector2;
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
            for (int num108 = 0; num108 < Main.rand.Next(2, 4); num108++)
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
                float speedX2 = num78 + Main.rand.Next(-160, 161) * 0.02f;
                float speedY2 = num79 + Main.rand.Next(-160, 161) * 0.02f;
                int proj = Projectile.NewProjectile(source, vector2, new Vector2(speedX2, speedY2), ProjectileID.HallowStar, damage / 2, adjustedKnockback, i, 0f, Main.rand.Next(10));
                Main.projectile[proj].DamageType = DamageClass.Melee;
                speedX2 = num78 + Main.rand.Next(-80, 81) * 0.02f;
                speedY2 = num79 + Main.rand.Next(-80, 81) * 0.02f;
                Projectile.NewProjectile(source, vector2, new Vector2(speedX2, speedY2), ModContent.ProjectileType<TerraBall>(), damage, adjustedKnockback, i, 0f, Main.rand.Next(5));
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置，随机生成三种神圣系粉尘之一
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(5))
            {
                int num249 = Main.rand.Next(3);
                if (num249 == 0)
                {
                    num249 = 15;
                }
                else if (num249 == 1)
                {
                    num249 = 57;
                }
                else
                {
                    num249 = 58;
                }
                int num250 = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, num249, player.direction * 2, 0f, 150, default, 1.3f);
                Main.dust[num250].velocity *= 0.2f;
            }
        }
        /// <summary>
        /// 命中敌人：50% 概率施加灾厄的"深海重压"（CrushDepth）debuff
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    target.AddBuff(calamity0.Find<ModBuff>("CrushDepth").Type, 300);
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    target.AddBuff(calamity1.Find<ModBuff>("CrushDepth").Type, 300);
                }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：同样 50% 概率施加深海重压
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    target.AddBuff(calamity0.Find<ModBuff>("CrushDepth").Type, 300);
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    target.AddBuff(calamity1.Find<ModBuff>("CrushDepth").Type, 300);
                }
            }
        }
        /// <summary>
        /// 配方（分版本）：远古方舟 + 灾厄核心 + 断裂英雄剑（现代版）；经典版额外需要生命碎片
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<ArkoftheAncients>();
                recipe.AddIngredient(calamity.Find<ModItem>("CoreofCalamity").Type);
                recipe.AddIngredient(ItemID.BrokenHeroSword);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<ArkoftheAncients>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofCalamity").Type);
                recipe1.AddIngredient(calamity1.Find<ModItem>("LivingShard").Type, 3);
                recipe1.AddIngredient(ItemID.BrokenHeroSword);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
