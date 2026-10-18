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
    /// 星火凤凰雨（Phoenix Flame Barrage）—— 移植自 CI Beta1.12 的
    /// <c>Content/Items/Weapons/Magic/DragonStaff.cs</c>（CI 里显示名为
    /// "Phoenix Flame Barrage Legacy"，本工程按命名口径去掉 Legacy 后缀）。
    /// <para>
    /// 物品贴图使用用户提供的 <c>Phoenix_Flame_Barrage.png</c>（106×140，现代版同名物品贴图），
    /// 因此判定框按该图取 106×140；其余数值与出手逻辑全部照 CI 源：
    /// 伤害 290、魔力 20、使用 15 / 动画 30 帧、击退 3.5、弹速 30，
    /// 月后 14 档（CI 的 <c>DeepBlue</c>，蓝名）与 2 铂金价值（CI 的 <c>RarityPriceDeepBlue</c>）。
    /// </para>
    /// <para>
    /// 出手：从玩家上方召唤 30~32 枚 <see cref="PhoenixFlame"/>，各自从不同高度与横向偏移
    /// 朝鼠标方向坠落，形成"火幕从天而降"；每枚弹幕命中挂 CI 的圣火 300 帧。
    /// </para>
    /// <para>
    /// **数值膨胀**：用户尚未点名本件的开态数值，暂按工程既有的"犽戎后 ×5"档取
    /// <b>290 → 1450（暂定）</b>；伤害由 <c>Shoot</c> 的 <c>damage</c> 参数派生，整条自动跟随。
    /// </para>
    /// </summary>
    internal class PhoenixFlameBarrage : ModItem
    {
        /// <summary>物品贴图显式指向用户提供的带下划线文件名（类名仍按工程口径用 PascalCase）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Magic/Phoenix_Flame_Barrage";
        /// <summary>数值膨胀后的面板伤害（暂定：CI 源值 290 × 犽戎后 5 倍 = 1450）</summary>
        private const float InflatedDamage = 1450f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 CI 源值 290</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>登记为法杖、研究解锁一份（照 CI 源）</summary>
        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true;
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：除贴图判定框与稀有度映射外，全部照 CI 的 <c>DragonStaff</c></summary>
        public override void SetDefaults()
        {
            Item.width = 106;
            Item.height = 140;
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
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>法杖持握原点（照 CI 的 15,15；tML 的坐标从贴图左下角起算）</summary>
        public override Vector2? HoldoutOrigin() => new Vector2(15, 15);
        /// <summary>数值膨胀：面板基础伤害改读 <see cref="BaseDamage"/>，游戏内切换即时生效</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>
        /// 出手（逐行照 CI 的 <c>DragonStaff.Shoot</c>）：先算鼠标方向，再按 30~32 枚的数量
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
