using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Melee
{
    /// <summary>
    /// 战士徽章EX（移植自 CalamityExpansion）：Pink 稀有度的近战饰品，
    /// +18% 近战伤害、+12% 近战攻击速度、+8% 近战暴击率；近战命中附加燃烧III。
    /// </summary>
    internal class WarriorEmblemEX : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 3);
            Item.rare = ItemRarityID.Pink;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage<MeleeDamageClass>() += 0.18f;
            player.GetAttackSpeed<MeleeDamageClass>() += 0.12f;
            player.GetCritChance<MeleeDamageClass>() += 8f;
        }
        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out _)
                && !ModLoader.TryGetMod("CalamityModClassicPreTrailer", out _))
                return;
            CreateRecipe()
                .AddIngredient(ItemID.WarriorEmblem)
                .AddIngredient(ItemID.HallowedBar, 15)
                .AddIngredient(ItemID.SoulofLight, 25)
                .AddIngredient(ItemID.SoulofNight, 25)
                .AddIngredient(ItemID.SoulofFright, 25)
                .AddIngredient(ItemID.SoulofMight, 25)
                .AddIngredient(ItemID.SoulofSight, 25)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
