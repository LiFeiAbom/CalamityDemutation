using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Melee
{
    /// <summary>
    /// 烈火手套EX（移植自 CalamityExpansion，按用户口径复合烈火手套与天界壳）：
    /// Purple 稀有度、售价 20 金、防御 8；+24% 近战伤害/暴击率/攻击速度、+400 仇恨。
    /// 近战命中同时施加燃烧III与破晓II。
    /// </summary>
    internal class FireGauntletEX : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 30;
            Item.defense = 8;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 20);
            Item.rare = ItemRarityID.Purple;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Melee) += 0.24f;
            player.GetCritChance(DamageClass.Melee) += 24f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.24f;
            player.aggro += 400;
            // 烈火手套功能：点火、近战击退×2、近战/鞭子自动挥舞、近战武器大小×1.1。
            player.magmaStone = true;
            player.kbGlove = true;
            player.autoReuseGlove = true;
            player.meleeScaleGlove = true;
            // 天界壳功能：狼人/人鱼形态、生命再生、采矿速度与召唤物击退。
            player.wolfAcc = true;
            player.accMerman = true;
            if (hideVisual)
            {
                player.hideWolf = true;
                player.hideMerman = true;
            }
            player.lifeRegen += 2;
            player.pickSpeed -= 0.15f;
            player.GetKnockback(DamageClass.Summon) += 1f;
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                CreateRecipe()
                    .AddIngredient<WarriorEmblemEX>()
                    .AddIngredient(ItemID.FireGauntlet)
                    .AddIngredient(ItemID.BerserkerGlove)
                    .AddIngredient(ItemID.CelestialShell)
                    .AddIngredient(ItemID.FragmentSolar, 99)
                    .AddIngredient(cosmiliteBar.Type, 5)
                    .AddTile(cosmicAnvil.Type)
                    .Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                CreateRecipe()
                    .AddIngredient<WarriorEmblemEX>()
                    .AddIngredient(ItemID.FireGauntlet)
                    .AddIngredient(ItemID.BerserkerGlove)
                    .AddIngredient(ItemID.CelestialShell)
                    .AddIngredient(ItemID.FragmentSolar, 99)
                    .AddIngredient(classicCosmiliteBar.Type, 5)
                    .AddTile(draedonsForge.Type)
                    .Register();
            }
        }
    }
}
