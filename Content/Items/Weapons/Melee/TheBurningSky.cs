using CalamityDemutation.Content.Items;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 焚灭天惩（TheBurningSky）—— 犽戎档近战（基底照搬灾厄 2.0.3.9 的 <c>Items/Weapons/Melee/TheBurningSky.cs</c>）。
    /// <para>
    /// 源版外观是把剑却按法杖持握、没有真近战（<c>noMelee = true</c> + <c>Item.staff</c>）；
    /// 本工程按用户口径改成**挥舞**：去掉 noMelee 与法杖标记、改用 <c>ItemUseStyleID.Swing</c> 并加 useTurn，
    /// 于是既有真近战挥砍、又照常洒流星；挥砍表现按本工程惯例在 <c>MeleeEffects</c> 里调 <c>CDUtil.BetterSwing</c>。
    /// </para>
    /// <para>
    /// 每次使用朝鼠标上方洒下 <b>10</b> 颗火流星（源为 6 颗，本工程按用户口径改成 10），命中挂 300 帧龙焰。
    /// 与源的其它差异：① 不写 <c>RangedPrefix</c>/<c>MeleePrefix</c>（源把前缀伪装成远程，本工程不许）；
    /// ② 价值与稀有度改参照**经典版**（1 铂 80 金 / 红名 10 / postMoonLordRarity 14），源是 Violet + RarityVioletBuyPrice；
    /// ③ 龙焰走软依赖施加；④ 补一份 PvP 命中（本工程约定）。
    /// </para>
    /// <para>贴图由用户后续自行提供，当前为占位图。</para>
    /// </summary>
    internal class TheBurningSky:ModItem
    {
        /// <summary>每次使用洒下的流星数量（源为 6，本工程按用户口径改成 10）</summary>
        private const int ProjectilesPerBarrage = 10;
        /// <summary>
        /// 物品基础属性：102×146、伤害 147、14 帧使用与挥舞、击退 2.5、红名、1 铂 80 金、月后稀有度 14。
        /// 挥舞式（Swing + useTurn、不设 noMelee）——与源的法杖式持握不同，见类注释
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 102;
            Item.height = 146;
            Item.damage = 147;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 2.5f;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item105;
            Item.shoot = ModContent.ProjectileType<BurningMeteor>();
            Item.shootSpeed = 14f;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 挥砍表现：先用 BetterSwing 修正挥舞位置（本工程近战挥舞武器的必备写法），
        /// 再约 1/3 概率沿命中框洒铜币色尘（照抄经典版 <c>MeleeEffects</c> 里的 244 号尘）
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.CopperCoin);
            }
        }
        /// <summary>
        /// 使用：补一发低音爆（SoundID.Item70），然后洒下 10 颗火流星——
        /// 每颗用 <c>CDUtil.ProjectileRain</c> 从鼠标上方随机高度（850~1100 像素）砸落、落点横向 ±290 像素抖动，
        /// 速度取本次发射速度的 0.7~1.4 倍随机。返回 false 表示不走默认的单发发射。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            SoundEngine.PlaySound(SoundID.Item70, player.Center);
            float speed = velocity.Length();
            for (int i = 0; i < ProjectilesPerBarrage; ++i)
            {
                float randomSpeed = speed * Main.rand.NextFloat(0.7f, 1.4f);
                CDUtil.ProjectileRain(source, Main.MouseWorld, 290f, 130f, 850f, 1100f, randomSpeed, type, damage, knockback, player.whoAmI);
            }
            return false;
        }
        /// <summary>命中敌人：挂 300 帧龙焰（灾厄本家 debuff，缺该 buff 时静默跳过）</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) => CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 300);
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构，挂同样的龙焰</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo) => CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 300);
    }
}
