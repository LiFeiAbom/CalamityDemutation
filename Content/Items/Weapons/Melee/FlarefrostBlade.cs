using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 霜火之刃（Flarefrost Blade）—— 秘银砧档近战（整把照搬灾厄 2.0.3.9 的 <c>FlarefrostBlade</c>）。
    /// 挥舞时朝准心甩出一道霜火弹幕（弹幕伤害为面板的 60%），命中附加狱炎 + 霜咬各 180 帧。
    /// 配方：冰晶锭×8 + 狱石锭×8 + 光明之魂×3 @ 秘银砧（冰晶锭 CalamityMod 专有，走软依赖）。
    /// 弹幕用的是灾厄本体 <c>Flarefrost</c>，同样走软依赖（灾厄未加载时本武器只是不会发射弹幕）。
    /// </summary>
    internal class FlarefrostBlade : ModItem
    {
        /// <summary>
        /// 物品基础属性：64×66、伤害 169、29 帧挥砍、击退 6.25、粉名（24金）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 66;
            Item.damage = 169; // 源值 125，×1.35 膨胀（168.75 上取整）
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 29;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6.25f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 24, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.shootSpeed = 11f;
            // 弱引用下 SetDefaults 阶段可能查不到灾厄弹幕，查不到就挂原版占位保证 Shoot 被调用，
            // 真正弹幕在 ModifyShootStats / Shoot 里运行时解析。
            Item.shoot = ModContent.TryFind("CalamityMod", "Flarefrost", out ModProjectile flarefrost) ? flarefrost.Type : ProjectileID.WoodenArrowFriendly;
        }
        /// <summary>运行时解析灾厄本体弹幕 <c>Flarefrost</c> 的类型；灾厄未加载时返回 0</summary>
        private static int FlarefrostType() => ModContent.TryFind("CalamityMod", "Flarefrost", out ModProjectile flarefrost) ? flarefrost.Type : 0;
        /// <summary>
        /// 挥手时甩出霜火弹幕。原版源码用 <c>ModifyShootStats</c> 把伤害压到面板的 60%，
        /// 这里改成在 Shoot 内手动生成，顺便处理"灾厄未加载"的情况（不生成而不是生成 0 号弹幕）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int flarefrost = FlarefrostType();
            if (flarefrost <= 0)
                return false;
            Projectile.NewProjectile(source, position, velocity, flarefrost, (int)(damage * 0.6f), knockback, player.whoAmI);
            return false;
        }
        /// <summary>挥舞表现：BetterSwing 修正挥舞位置；约 1/3 概率洒落霜（<c>DustID.IceRod</c>，源码写 67）或火（<c>DustID.Torch</c>，源码写 6）尘</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            int dustChoice = Main.rand.Next(2) == 0 ? DustID.IceRod : DustID.Torch;
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, dustChoice);
        }
        /// <summary>近战命中：附加狱炎 + 霜咬各 180 帧</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
            target.AddBuff(BuffID.Frostburn2, 180);
        }
        /// <summary>PvP 命中：同上</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire3, 180);
            target.AddBuff(BuffID.Frostburn2, 180);
        }
        /// <summary>
        /// 配方（分版本）：现代版冰晶锭×8 + 狱石锭×8 + 光明之魂×3 @ 秘银砧；
        /// 经典版灾厄没有 CryonicBar（叫 CryonicBar 的是现代版），沿用同名查找，查不到则本条不注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && calamity.TryFind<ModItem>("CryonicBar", out ModItem cryonicBar))
            {
                CreateRecipe().
                    AddIngredient(cryonicBar.Type, 8).
                    AddIngredient(ItemID.HellstoneBar, 8).
                    AddIngredient(ItemID.SoulofLight, 3).
                    AddTile(TileID.MythrilAnvil).
                    Register();
            }
        }
    }
}
