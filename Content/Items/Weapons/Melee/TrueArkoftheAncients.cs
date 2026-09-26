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
    /// （对照灾厄经典版 1.4.2.101 的同名武器：伤害 60→113、使用时间 25→22、命中减益换成深海重压）
    /// 发射强化版远古光束（EonBeam），并召唤圣星与大地弹（TerraBall）混合攻击
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
        /// 主弹幕为远古光束（EonBeam），每次挥砍发射一次。
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
        }
        /// <summary>
        /// 射击逻辑：主弹幕为 75% 伤害/速度的远古光束（局部命中冷却 14、穿透 2），
        /// 再从玩家上方召唤 2~3 组圣星 + 大地弹混合坠落
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile beam = Projectile.NewProjectileDirect(source, position, velocity * 0.75f, type, (int)(damage * 0.75), knockback, Main.myPlayer);
            if (beam.active)
            {
                beam.localNPCHitCooldown = 14;   // 同一敌人 14 帧内不再被本光束命中
                beam.penetrate = 2;              // 穿透 2 个敌人
                beam.ai[0] = 1f;   // 标记为真·远古方舟光束：命中时不附加元素 debuff（EonBeam.OnHitNPC/OnHitPlayer 读此位豁免）
                beam.ai[1] = Main.rand.Next(1, 3);   // 随机光束颜色编号
            }
            int i = Main.myPlayer;               // 弹幕归属者（本地玩家）
            float num72 = Main.rand.Next(18, 27);// 圣星/大地弹速度基准：18~26
            float adjustedKnockback = player.GetWeaponKnockback(Item, knockback);
            player.itemTime = Item.useTime;      // 保持本体使用计时，与弹幕演出同步
            Vector2 vector2 = player.RotatedRelativePoint(player.MountedCenter, true);
            Vector2 value = Vector2.UnitX.RotatedBy(player.fullRotation);   // （未使用）
            Vector2 vector3 = Main.MouseWorld - vector2;                    // （未使用）
            float num78 = Main.mouseX + Main.screenPosition.X - vector2.X;  // 鼠标相对玩家的 X 分量
            float num79 = Main.mouseY + Main.screenPosition.Y - vector2.Y;  // 鼠标相对玩家的 Y 分量
            if (player.gravDir == -1f)
            {
                num79 = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - vector2.Y;  // 反重力下翻转鼠标 Y
            }
            float num80 = (float)Math.Sqrt(num78 * num78 + num79 * num79);   // 鼠标方向长度
            if (float.IsNaN(num78) && float.IsNaN(num79) || num78 == 0f && num79 == 0f)
            {
                // 退化情形（鼠标与玩家重合/数值异常）：退化为朝玩家朝向正前方
                num78 = player.direction;
                num79 = 0f;
                num80 = num72;
            }
            else
            {
                num80 = num72 / num80;   // 换算成速度缩放系数
            }
            for (int num108 = 0; num108 < Main.rand.Next(2, 4); num108++)
            {
                // 生成点：玩家上方 600 像素、X 在鼠标方向附近随机散布（营造"从天而降"的落点）
                vector2 = new Vector2(player.position.X + player.width * 0.5f + Main.rand.Next(201) * -player.direction + (Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
                vector2.X = (vector2.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
                vector2.Y -= 100 * num108;   // 逐组抬高 100 像素，形成层次
                num78 = Main.mouseX + Main.screenPosition.X - vector2.X;
                num79 = Main.mouseY + Main.screenPosition.Y - vector2.Y;
                if (num79 < 0f)
                {
                    num79 *= -1f;   // 强制向下的分量（只朝下砸）
                }
                if (num79 < 20f)
                {
                    num79 = 20f;    // 保证至少有一点垂直速度，避免水平飞
                }
                num80 = (float)Math.Sqrt(num78 * num78 + num79 * num79);
                num80 = num72 / num80;
                num78 *= num80;
                num79 *= num80;
                float speedX2 = num78 + Main.rand.Next(-160, 161) * 0.02f;   // ±3.2 的随机散布
                float speedY2 = num79 + Main.rand.Next(-160, 161) * 0.02f;
                int proj = Projectile.NewProjectile(source, vector2, new Vector2(speedX2, speedY2), ProjectileID.HallowStar, damage / 2, adjustedKnockback, i, 0f, Main.rand.Next(10));
                Main.projectile[proj].DamageType = DamageClass.Melee;   // 圣星本体是魔法弹幕，强制改成近战以免吃错加成
                speedX2 = num78 + Main.rand.Next(-80, 81) * 0.02f;   // 大地弹散布更小（±1.6）
                speedY2 = num79 + Main.rand.Next(-80, 81) * 0.02f;
                Projectile.NewProjectile(source, vector2, new Vector2(speedX2, speedY2), ModContent.ProjectileType<TerraBall>(), damage, adjustedKnockback, i, 0f, Main.rand.Next(5));
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置，随机生成三种粉尘之一（沿用源版的数字写法 15/57/58）
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
        /// 命中敌人：50% 概率施加灾厄的"深海重压"（CrushDepth）debuff，持续 300 帧
        /// （源版此处施加的是 HolyLight、持续 500 帧）
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
            {
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth))
                    {
                        target.AddBuff(crushDepth.Type, 300);
                    }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff classicCrushDepth))
                    {
                        target.AddBuff(classicCrushDepth.Type, 300);
                    }
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
                    if (calamity0.TryFind<ModBuff>("CrushDepth", out ModBuff crushDepth))
                    {
                        target.AddBuff(crushDepth.Type, 300);
                    }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("CrushDepth", out ModBuff classicCrushDepth))
                    {
                        target.AddBuff(classicCrushDepth.Type, 300);
                    }
                }
            }
        }
        /// <summary>
        /// 配方（分版本）：远古方舟 + 灾厄核心 + 断裂英雄剑（现代版）；经典版额外需要生命碎片
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：远古方舟 + 灾厄核心 + 断裂英雄剑，秘银砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<ArkoftheAncients>();       // 远古方舟（本模组下位）
                    recipe.AddIngredient(coreofCalamity.Type);      // 灾厄材料：灾厄核心
                    recipe.AddIngredient(ItemID.BrokenHeroSword);   // 断裂英雄剑
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            // ── 经典版灾厄：额外需要生命碎片 ×3 ──
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("CoreofCalamity", out ModItem classicCoreofCalamity)
                    && calamity1.TryFind<ModItem>("LivingShard", out ModItem livingShard))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<ArkoftheAncients>();
                    recipe1.AddIngredient(classicCoreofCalamity.Type);
                    recipe1.AddIngredient(livingShard.Type, 3);     // 经典版材料：生命碎片 ×3
                    recipe1.AddIngredient(ItemID.BrokenHeroSword);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
