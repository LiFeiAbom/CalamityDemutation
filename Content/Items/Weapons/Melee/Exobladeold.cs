using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 星流之刃（Exoblade）—— 月后终盘近战巨剑（行为照搬 CI 的 <c>Exobladeold</c>，即改版前旧 Exoblade 的复刻）。
    /// 挥砍射出一道会掉头追踪玩家的星流射线（<see cref="Exobeamold"/>），命中敌人时：
    /// 累计命中 5 次或目标血量 ≤15% 时原地炸出 <see cref="Exoboomold"/>（伤害为本次命中的 1/4）；
    /// 累计命中 2 次或目标血量 ≤15% 时从屏幕外甩来 2 颗星云彗星（<see cref="ExoComet"/>）；
    /// 挂整套星云系减益，并在目标可吸血且自身未挂月噬时回血 5~8。
    /// 配方按**现代版 / 经典版各一条**（两版不同，见 <c>AddRecipes</c>）：
    /// 现代版 = 六把自有下位剑 + 奇迹物质 @ 嘉登熔炉；经典版 = 同样六把剑 + 七种经典版材料 @ 嘉登熔炉。
    /// 与 CI 源的三处差异：①伤害取经典版原值 6700（CI 写 900）；
    /// ②去掉「损失生命值 1:1 转平伤」与灾厄真近战伤害类加成（本工程没有 TrueMeleeDamageClass）；
    /// ③CI 的传颂之物（LoreExo）分支不存在，直接常驻它那条「多次命中触发」的行为分支，不搬 Lore 专属弹幕与 tooltip 行。
    /// </summary>
    internal class Exobladeold : ModItem
    {
        /// <summary>研究所解锁数量 1（唯一武器）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>物品基础属性：80×114、伤害 6700、14 帧挥砍、击退 9、月后稀有度 15（紫）、光束初速 19</summary>
        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 114;
            Item.damage = 6700; // 经典版原值（CI 写作 900）
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 9f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<Exobeamold>();
            Item.shootSpeed = 19f;
            Item.shootsEveryUse = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>每 14 帧挥砍都射出一道星流射线（不搬 CI 的传颂之物专属光束）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, ModContent.ProjectileType<Exobeamold>(), damage, knockback, player.whoAmI, 0f);
            return false;
        }
        /// <summary>挥舞表现：BetterSwing 修正挥舞位置；约 1/4 概率洒落青色泰拉尘</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(4))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255));
        }
        /// <summary>
        /// 命中结算（常驻 CI 的「多次命中触发」分支）：先从随机一侧屏幕外算一颗冲向目标的彗星初始速度，
        /// 再按命中计数决定是否炸爆炸 / 甩彗星，最后挂减益并回血。
        /// 两个命中计数是 ModItem 实例字段（CI 源如此），多人下会串，与 AnarchyBlade 的 ShootCount 同理。
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item88, player.Center);
            // 起爆点：玩家左右随机一侧 800 像素外、纵向 ±800 像素内的随机点
            float xPos = player.position.X + 800 * (Main.rand.NextBool(2) ? 1 : -1);
            float yPos = player.position.Y + Main.rand.Next(-800, 801);
            Vector2 startPos = new Vector2(xPos, yPos);
            Vector2 velocity = target.position - startPos;
            float dir = 10f / startPos.X;
            velocity.X *= dir * 150;
            velocity.Y *= dir * 150;
            velocity.X = MathHelper.Clamp(velocity.X, -15f, 15f);
            velocity.Y = MathHelper.Clamp(velocity.Y, -15f, 15f);
            hitCount++;
            hitCount2++;
            if (hitCount >= 5 || target.life <= target.lifeMax * 0.15f)
            {
                Projectile.NewProjectile(player.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<Exoboomold>(), damageDone / 4, (int)Item.knockBack, Main.myPlayer);
                hitCount = 0;
            }
            if (hitCount2 >= 2 || target.life <= target.lifeMax * 0.15f)
            {
                for (int comet = 0; comet < 2; comet++)
                {
                    float ai1 = Main.rand.NextFloat() + 0.5f;
                    Projectile.NewProjectile(player.GetSource_OnHit(target), startPos, velocity, ModContent.ProjectileType<ExoComet>(), damageDone, (int)Item.knockBack, player.whoAmI, 0f, ai1);
                }
                hitCount2 = 0;
            }
            target.ExoDebuffs();
            if (!target.canGhostHeal || player.moonLeech)
                return;
            int healAmount = Main.rand.Next(4) + 5;
            player.statLife += healAmount;
            player.HealEffect(healAmount);
        }
        /// <summary>掉在地上时画发光层</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Melee/ExobladeoldGlow").Value);
        }
        /// <summary>累计命中次数（爆炸分支用，CI 源为 ModItem 实例字段，照抄）</summary>
        private int hitCount;
        /// <summary>累计命中次数（彗星分支用，CI 源为 ModItem 实例字段，照抄）</summary>
        private int hitCount2;
        /// <summary>
        /// 两条配方分别对应现代版与经典版（**两版配方不同，各自成条，不能混用**），都软依赖、缺料即不注册：
        /// · 现代版（沿用 CI <c>Exobladeold</c> 的配方）：六把自有下位剑 + 奇迹物质 @ 嘉登熔炉，挂 CalamityMod。
        /// · 经典版（<c>DraedonsExoblade</c> 的配方，去掉其中唯一的非我方武器项 BalefulHarvester）：
        ///   同样六把剑 + 噩梦燃料×5 + 吸热能量×5 + 宇宙锭×5 + 暗日碎片×5 + 冥府碎片×3 + 幽魂质×5 + 金 Auric 矿×25 @ 嘉登熔炉，
        ///   挂 CalamityModClassicPreTrailer（其中冥府碎片与幽魂质只有经典版才有）。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod modern)
                && modern.TryFind<ModItem>("MiracleMatter", out ModItem miracleMatter)
                && modern.TryFind<ModTile>("DraedonsForge", out ModTile modernForge))
            {
                CreateRecipe().
                    AddIngredient<Terratomere>().
                    AddIngredient<AnarchyBlade>().
                    AddIngredient<FlarefrostBlade>().
                    AddIngredient<EntropicClaymore>().
                    AddIngredient<PhoenixBlade>().
                    AddIngredient<StellarStriker>().
                    AddIngredient(miracleMatter.Type).
                    AddTile(modernForge.Type).
                    Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                && classic.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                && classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                && classic.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment)
                && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                && classic.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicForge))
            {
                CreateRecipe().
                    AddIngredient<Terratomere>().
                    AddIngredient<AnarchyBlade>().
                    AddIngredient<FlarefrostBlade>().
                    AddIngredient<EntropicClaymore>().
                    AddIngredient<PhoenixBlade>().
                    AddIngredient<StellarStriker>().
                    AddIngredient(nightmareFuel.Type, 5).
                    AddIngredient(endothermicEnergy.Type, 5).
                    AddIngredient(cosmiliteBar.Type, 5).
                    AddIngredient(darksunFragment.Type, 5).
                    AddIngredient(hellcasterFragment.Type, 3).
                    AddIngredient(phantoplasm.Type, 5).
                    AddIngredient(auricOre.Type, 25).
                    AddTile(classicForge.Type).
                    Register();
            }
        }
    }
}
