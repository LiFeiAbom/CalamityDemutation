using CalamityDemutation.Content.Items;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Wings
{
    /// <summary>
    /// 古圣金源之翼：Drew 之翼的升级版，飞行参数为 400 / 12 / 3，
    /// 提供防御、伤害、暴击、近战攻速、减伤、盔甲穿透、生命/魔力上限与每秒回复。
    /// 飞行尘埃沿用 Drew 的钻石尘形态，颜色改为与古圣金源之翼一致的暖金色。
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    internal class AncientAuricWings : ModItem
    {
        public const int ManaPerSecond = 2;
        private static readonly Color WingDustColor = new Color(236, 214, 147);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(400, 12f, 3f);
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 20;
            Item.defense = 8;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Generic) += 0.16f;
            player.GetCritChance(DamageClass.Generic) += 8f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.16f;
            player.endurance += 0.08f;
            player.GetArmorPenetration(DamageClass.Generic) += 8;
            player.statLifeMax2 += 20;
            player.statManaMax2 += 20;
            player.lifeRegen += 4;
            player.GetModPlayer<CalamityDemutationPlayer>().ancientAuricWings = true;
            player.noFallDmg = true;
            if (player.controlJump && player.wingTime > 0f && !VanillaExtraJump.CloudInABottle.CanStart(player)
                && player.jump == 0 && player.velocity.Y != 0f && !hideVisual)
            {
                int dustXOffset = player.direction == 1 ? -40 : 4;
                int flightDust = Dust.NewDust(
                    new Vector2(player.position.X + player.width / 2f + dustXOffset, player.position.Y + player.height / 2f - 15f),
                    30,
                    30,
                    DustID.GemDiamond,
                    0f,
                    0f,
                    100,
                    WingDustColor,
                    2.4f);
                Main.dust[flightDust].noGravity = true;
                Main.dust[flightDust].velocity *= 0.3f;
                if (Main.rand.NextBool(10))
                    Main.dust[flightDust].fadeIn = 2f;
                Main.dust[flightDust].shader = GameShaders.Armor.GetSecondaryShader(player.cWings, player);
            }
        }
        public override void VerticalWingSpeeds(
            Player player,
            ref float ascentWhenFalling,
            ref float ascentWhenRising,
            ref float maxCanAscendMultiplier,
            ref float maxAscentMultiplier,
            ref float constantAscend)
        {
            ascentWhenFalling = 1f;
            ascentWhenRising = 0.175f;
            maxCanAscendMultiplier = 1.2f;
            maxAscentMultiplier = 3.25f;
            constantAscend = 0.15f;
        }
        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                || !calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar)
                || !calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                return;
            CreateRecipe()
                .AddIngredient<DrewsWings>()
                .AddIngredient(auricBar.Type, 12)
                .AddIngredient(ItemID.SoulofFlight, 20)
                .AddTile(draedonsForge.Type)
                .Register();
        }
    }
}
