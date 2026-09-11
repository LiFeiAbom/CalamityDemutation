using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 星云核心 - 专家饰品
    /// 提供 +20% 通用伤害与 +20% 暴击，周期性生成星云星攻击敌人，
    /// 并在濒死时有概率触发回血而免于死亡。
    /// </summary>
    internal class NebulousCore : ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、饰品与专家物品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 16;                          // 贴图宽（像素）
            Item.height = 14;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 售价 60 金
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            Item.expert = true;                       // 标记为专家物品（专家模式专属外观框）
        }
        /// <summary>
        /// ModItem.Update 钩子：物品掉落在世界中时每帧调用（非背包内），复刻原版星云物品的紫色发光效果
        /// </summary>
        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            // 物品掉落在地时发出紫色光芒，复刻原版星云物品的发光效果
            float num = (float)Main.rand.Next(90, 111) * 0.01f;
            num *= Main.essScale;
            Lighting.AddLight((int)((Item.position.X + (float)(Item.width / 2)) / 16f), (int)((Item.position.Y + (float)(Item.height / 2)) / 16f), 0.35f * num, 0.05f * num, 0.35f * num);
        }
        /// <summary>
        /// 装备时：置位星云核心标记，并复刻原版星云套奖励——周期性在玩家周围随机生成星云星自动攻击敌人
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.nebulousCore = true;
            // 复刻原版星云套装的套装奖励：周期性在玩家周围生成星云星
            int damage = 1500;
            float knockBack = 3f;
            if (Main.rand.NextBool(15))
            {
                // 统计玩家当前已存在的星云星数量
                int num = 0;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI && Main.projectile[i].type == ModContent.ProjectileType<NebulaStar>())
                    {
                        num++;
                    }
                }
                // 星云星达到 25 个上限后不再生成
                if (Main.rand.Next(15) >= num && num < 25)
                {
                    int num2 = 50;
                    int num3 = 24;
                    int num4 = 90;
                    for (int j = 0; j < num2; j++)
                    {
                        int num5 = Main.rand.Next(200 - j * 2, 400 + j * 2);
                        Vector2 center = player.Center;
                        center.X += (float)Main.rand.Next(-num5, num5 + 1);
                        center.Y += (float)Main.rand.Next(-num5, num5 + 1);
                        if (!Collision.SolidCollision(center, num3, num3) && !Collision.WetCollision(center, num3, num3))
                        {
                            center.X += (float)(num3 / 2);
                            center.Y += (float)(num3 / 2);
                            if (Collision.CanHit(new Vector2(player.Center.X, player.position.Y), 1, 1, center, 1, 1) || Collision.CanHit(new Vector2(player.Center.X, player.position.Y - 50f), 1, 1, center, 1, 1))
                            {
                                int num6 = (int)center.X / 16;
                                int num7 = (int)center.Y / 16;
                                bool flag = false;
                                // 优先在墙体附近生成，其次寻找无实心碰撞的位置
                                if (Main.rand.Next(3) == 0 && Main.tile[num6, num7] != null && Main.tile[num6, num7].WallType > WallID.None)
                                {
                                    flag = true;
                                }
                                else
                                {
                                    center.X -= (float)(num4 / 2);
                                    center.Y -= (float)(num4 / 2);
                                    if (Collision.SolidCollision(center, num4, num4))
                                    {
                                        center.X += (float)(num4 / 2);
                                        center.Y += (float)(num4 / 2);
                                        flag = true;
                                    }
                                }
                                if (flag)
                                {
                                    // 避免与已有星云星过于接近
                                    for (int k = 0; k < 1000; k++)
                                    {
                                        if (Main.projectile[k].active && Main.projectile[k].owner == player.whoAmI && Main.projectile[k].type == ModContent.ProjectileType<NebulaStar>() && (center - Main.projectile[k].Center).Length() < 48f)
                                        {
                                            flag = false;
                                            break;
                                        }
                                    }
                                    if (flag && Main.myPlayer == player.whoAmI)
                                    {
                                        Projectile.NewProjectile(player.GetSource_Accessory(Item), center.X, center.Y, 0f, 0f, ModContent.ProjectileType<NebulaStar>() , damage, knockBack, player.whoAmI, 0f, 0f);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
