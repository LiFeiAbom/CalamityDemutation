using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 腐臭倾泻者（Fetid Emesis）—— 移植自灾厄 Public 2.0.4，伤害按用户要求改为 200。
    /// 六帧连发子弹，40% 几率省弹；每次射击有 1/8 概率改为喷出黏附腐肉。
    /// 灾厄的贴脸射击机制按本工程长期口径不接入。
    /// </summary>
    internal class FetidEmesis : ModItem
    {
        private const int AmmoSaveChance = 40;
        private const int GoreChanceDenominator = 8;
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 76;
            Item.height = 46;
            Item.damage = 200;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = Item.useAnimation = 6;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.UseSound = SoundID.Item11;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 16f;
            Item.useAmmo = AmmoID.Bullet;
            Item.value = Item.buyPrice(1, 75, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
            => Main.rand.Next(100) >= AmmoSaveChance;
        public override Vector2? HoldoutOffset() => new Vector2(-5f, 0f);
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!Main.rand.NextBool(GoreChanceDenominator))
                return true;
            Projectile.NewProjectile(source, position, velocity * 0.8f,
                ModContent.ProjectileType<EmesisGore>(), damage, knockback, player.whoAmI);
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustDirect(position, 10, 10, DustID.Shadowflame);
                dust.velocity = velocity.SafeNormalize(Vector2.UnitX)
                    .RotatedByRandom(MathHelper.ToRadians(15f));
                dust.noGravity = true;
            }
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.fetidEmesisSoundCooldown <= 0)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath13 with
                {
                    Volume = SoundID.NPCDeath13.Volume * 0.5f
                }, position);
                modPlayer.fetidEmesisSoundCooldown = 120;
            }
            return false;
        }
    }
}
