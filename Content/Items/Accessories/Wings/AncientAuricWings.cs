using CalamityDemutation.Content.Items;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Wings
{
    /// <summary>
    /// 古圣金源之翼：Drew 之翼的升级版，保留 400 帧飞行与暖金色飞行尘。
    /// 不提供独立基础增幅；穿戴全套古圣金源套装时由玩家侧追加四项套装增幅。
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    internal class AncientAuricWings : ModItem
    {
        private static readonly Color WingDustColor = new Color(236, 214, 147);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(400, 12f, 3f);
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 20;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.noFallDmg = true;
            if (player.controlJump && player.wingTime > 0f && !VanillaExtraJump.CloudInABottle.CanStart(player)
                && player.jump == 0 && player.velocity.Y != 0f && !hideVisual)
            {
                int dustXOffset = player.direction == 1 ? -40 : 4;
                int flightDust = Dust.NewDust(
                    new Vector2(player.position.X + player.width / 2f + dustXOffset, player.position.Y + player.height / 2f - 15f),
                    30, 30, DustID.GemDiamond, 0f, 0f, 100, WingDustColor, 2.4f);
                Main.dust[flightDust].noGravity = true;
                Main.dust[flightDust].velocity *= 0.3f;
                if (Main.rand.NextBool(10))
                    Main.dust[flightDust].fadeIn = 2f;
                Main.dust[flightDust].shader = GameShaders.Armor.GetSecondaryShader(player.cWings, player);
            }
        }
        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling,
            ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 1f;
            ascentWhenRising = 0.175f;
            maxCanAscendMultiplier = 1.2f;
            maxAscentMultiplier = 3.25f;
            constantAscend = 0.15f;
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar)
                && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                CreateRecipe()
                    .AddIngredient<DrewsWings>()
                    .AddIngredient(auricBar.Type, 12)
                    .AddIngredient(ItemID.SoulofFlight, 20)
                    .AddTile(cosmicAnvil.Type)
                    .Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                CreateRecipe()
                    .AddIngredient<DrewsWings>()
                    .AddIngredient(auricOre.Type, 60)
                    .AddIngredient(ItemID.SoulofFlight, 20)
                    .AddTile(draedonsForge.Type)
                    .Register();
            }
        }
    }
}
