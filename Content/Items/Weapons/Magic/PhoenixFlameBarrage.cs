using System;
using CalamityDemutation.Content.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Magic
{
    /// <summary>
    /// 星火凤凰雨（Phoenix Flame Barrage）—— 移植自 旧版支线的
    /// <c>Content/Items/Weapons/Magic/DragonStaff.cs</c>（旧版支线里显示名为
    /// "Phoenix Flame Barrage Legacy"，本工程按命名口径去掉 Legacy 后缀）。
    /// <para>
    /// 物品贴图使用 旧版支线的 <c>DragonStaff.png</c>（70×70，本工程按类名存为
    /// <c>PhoenixFlameBarrage.png</c>），判定框照旧版支线源取 72×70；其余数值与出手逻辑也全部照旧版支线：
    /// 伤害 290、魔力 20、使用 15 / 动画 30 帧、击退 3.5、弹速 30，
    /// 月后 15 档（紫名，与焚灭天惩同级；旧版支线源为 <c>DeepBlue</c> 14 档）与 2 铂金价值。
    /// </para>
    /// <para>
    /// 出手：从玩家上方召唤 30~32 枚 <see cref="PhoenixFlame"/>，各自从不同高度与横向偏移
    /// 朝鼠标方向坠落，形成"火幕从天而降"；每枚弹幕命中挂 旧版支线的圣火 300 帧。
    /// </para>
    /// <para>
    /// 伤害由 <c>Shoot</c> 的 <c>damage</c> 参数派生，整条自动跟随。
    /// </para>
    /// </summary>
    internal class PhoenixFlameBarrage : ModItem
    {
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 旧版支线源值 290</summary>
        /// <summary>登记为法杖、研究解锁一份（照旧版支线源）</summary>
        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true;
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：除稀有度映射外，全部照旧版支线的 <c>DragonStaff</c></summary>
        public override void SetDefaults()
        {
            Item.width = 72;
            Item.height = 70;
            Item.damage = 290;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 20;
            Item.useTime = 15;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3.5f;
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PhoenixFlame>();
            Item.shootSpeed = 30f;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(2, 0, 0, 0);
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>法杖持握原点（照旧版支线的 15,15；tML 的坐标从贴图左下角起算）</summary>
        public override Vector2? HoldoutOrigin() => new Vector2(15, 15);
        /// <summary>
        /// 出手（逐行照旧版支线的 <c>DragonStaff.Shoot</c>）：先算鼠标方向，再按 30~32 枚的数量
        /// 从玩家上方 600 像素起、逐枚再抬高 100 像素生成火雨；每枚都重新朝鼠标归一化，
        /// 并叠加 ±0.6 的横向/纵向随机扰动。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float shotSpeed = Item.shootSpeed;
            player.itemTime = Item.useTime;
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

            int flameCount = 30;
            if (Main.rand.NextBool(3))
            {
                flameCount++;
            }
            if (Main.rand.NextBool(3))
            {
                flameCount++;
            }
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
    }
}
