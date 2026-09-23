using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 熵之舞（Entropic Claymore）—— 月球工作台档近战（物品与挥砍行为移植自灾厄大修 0.4.0.1.3 的
    /// <c>EntropicClaymoreEcType</c> / <c>REntropicClaymore</c>，三档飞刃弹幕取自灾厄 2.0.3.9）。
    /// 每次挥砍把玩家锁进 78 帧的挥砍体（<see cref="EntropicClaymoreHoldout"/>），收招时朝准心扇形撒出 9 枚熵之飞刃；
    /// 飞刃按 中→大→小… 循环轮换档位，伤害倍率随档位变化（小 0.3 / 中 0.5 / 大 1.0），碰撞箱尺寸同步放大。
    /// 配方：熵构体×15 @ 月球工作台（熵构体 MeldConstruct 是灾厄材料，走软依赖）。
    /// </summary>
    internal class EntropicClaymore : ModItem
    {
        /// <summary>主题色 1（大修 EntropicColor1，挥砍体剑气用）</summary>
        public static readonly Color EntropicColor1 = new Color(25, 5, 9);
        /// <summary>主题色 2（大修 EntropicColor2，挥砍体剑气用）</summary>
        public static readonly Color EntropicColor2 = new Color(25, 5, 9);
        /// <summary>挥砍周期（帧）——同时是挥砍体存活帧数</summary>
        public const int SwingTime = 78;
        /// <summary>扇形撒出的飞刀总数</summary>
        public const int FlechetteCount = 9;
        /// <summary>扇形总张角（度）</summary>
        public const float FlechetteSpread = 140f;
        /// <summary>
        /// 三档轮换计数（0 小 / 1 中 / 2 大）：大修源码里是物品实例上的 <c>Item.CWR().ai[0]</c>，
        /// 这里照抄成 ModItem 实例字段保持行为一致（与 AnarchyBlade 的 ShootCount 同理，多人下会串）。
        /// </summary>
        private int TierIndex;
        /// <summary>
        /// 物品基础属性：130×106、伤害 152、78 帧挥砍、击退 5.25、青名（80金）、
        /// 无贴图且 noMelee，靠挥砍体 <see cref="EntropicClaymoreHoldout"/> 出伤
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 130;
            Item.height = 106;
            Item.damage = 152;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = SwingTime;
            Item.useTime = SwingTime;
            Item.knockBack = 5.25f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Cyan;
            Item.shoot = ModContent.ProjectileType<EntropicClaymoreHoldout>();
            Item.shootSpeed = 12f;
            Item.shootsEveryUse = true;
        }
        /// <summary>
        /// 每次挥砍先把档位往后推一档（0→1→2→0，大修原码首个档位就是中号），
        /// 再把档位写进挥砍体的 <c>localAI[0]</c>，并把挥砍体存活帧数对齐到物品的 useTime。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            TierIndex++;
            if (TierIndex > 2)
                TierIndex = 0;
            Projectile proj = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI, ai2: Item.useTime);
            proj.timeLeft = Item.useTime;
            proj.localAI[0] = TierIndex;
            return false;
        }
        /// <summary>挥舞表现：BetterSwing 修正挥舞位置；约 1/3 概率洒落暗影焰尘</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Shadowflame);
        }
        /// <summary>配方：熵构体×15 @ 月球工作台（灾厄材料，走软依赖，未加载灾厄时不注册）</summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("MeldConstruct", out ModItem meldConstruct))
            {
                CreateRecipe().
                    AddIngredient(meldConstruct.Type, 15).
                    AddTile(TileID.LunarCraftingStation).
                    Register();
            }
        }
    }
}
