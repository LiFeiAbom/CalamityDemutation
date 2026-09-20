using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 宙宇波能刃（Excelsus）—— 月后神吞档近战（移植自灾厄大修 0.4.0.1.3 的 ExcelsusEcType 重置版）。
    /// 左键散射三把旋转刃（ExcelsusMain/Blue/Pink），右键投出蓝色炸弹（ExcelsusBomb），命中召唤激光喷泉（LaserFountains）。
    /// 获取方式沿用灾厄原版：神吞宝藏袋掉落。
    /// 伤害 990 = 大修 EcType 220 × 4.5（本模组 CE 分形膨胀口径）。
    /// </summary>
    internal class Excelsus : ModItem
    {
        /// <summary>允许右键重复触发：右键炸弹每次点击都重新投掷</summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>物品基础属性：78×94、伤害 990、14 帧挥砍、击退 8、自动挥舞；主弹幕主刃、月后稀有度 14（蓝）</summary>
        public override void SetDefaults()
        {
            Item.width = 78;
            Item.damage = 990;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.height = 94;
            Item.value = Item.buyPrice(2, 0, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<ExcelsusMain>();
            Item.shootSpeed = 12f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>掉落在地上时绘制 Glow 发光层</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Melee/ExcelsusGlow").Value);
        }
        /// <summary>左键散射三刃（主刃/蓝刃/粉刃带随机散布）、右键投炸弹（伤害 ×2）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useTime = 10;
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ExcelsusBomb>(), damage * 2, knockback, player.whoAmI);
            }
            else
            {
                Item.useTime = 14;
                for (int i = 0; i < 3; i++)
                {
                    float speedX = velocity.X + Main.rand.NextFloat(-1.5f, 1.5f);
                    float speedY = velocity.Y + Main.rand.NextFloat(-1.5f, 1.5f);
                    switch (i)
                    {
                        case 0:
                            type = ModContent.ProjectileType<ExcelsusMain>();
                            break;
                        case 1:
                            type = ModContent.ProjectileType<ExcelsusBlue>();
                            break;
                        case 2:
                            type = ModContent.ProjectileType<ExcelsusPink>();
                            break;
                    }
                    Projectile.NewProjectile(source, position.X, position.Y, speedX, speedY, type, damage, knockback, player.whoAmI);
                }
            }
            return false;
        }
        /// <summary>允许右键使用</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>命中召唤激光喷泉（ai0 写入目标索引）</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, ModContent.ProjectileType<LaserFountains>(), Item.damage, 0f, player.whoAmI, target.whoAmI);
        }
        /// <summary>PvP 命中同样召唤激光喷泉</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, ModContent.ProjectileType<LaserFountains>(), Item.damage, 0f, player.whoAmI, target.whoAmI);
        }
    }
}
