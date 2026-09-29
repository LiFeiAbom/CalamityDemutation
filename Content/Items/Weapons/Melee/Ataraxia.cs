using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 禅心剑（Ataraxia）—— 月后终盘近战巨剑（行为照搬灾厄 2.0.4 的 <c>Ataraxia</c>）。
    /// 挥砍同时射出 1 枚主弹（<see cref="AtaraxiaMain"/>）与 2 枚侧弹（<see cref="AtaraxiaSide"/>）：
    /// 主弹命中即亡并炸出半径 130 的小爆发；侧弹命中即亡并放射状分裂出 6 枚针叶弹（<see cref="AtaraxiaSplit"/>）。
    /// 剑本体（真近战）命中敌人生成 ai[0]=0 的大爆发（<see cref="AtaraxiaBoom"/>，半径 200）——只有这一种会画 6 瓣玫瑰线花瓣尘。
    /// 命中弹幕挂原版暗影焰 180 帧；PvP 命中挂本工程移植的增强版暗影焰（<see cref="Shadowflame"/>，挂玩家身上另算 DoT）。
    /// <para>
    /// 与灾厄源的刻意偏离（均为用户点名）：
    /// ① 伤害 675 → **710**；② 伤害分配改回 1.4.4 的写法（主弹 100%、侧弹 50%），源 2.0.4 是主弹 50%、侧弹 18.75%。
    /// ③ Boom 的多段衰减用 2.0.4 版（×0.88），见 <see cref="AtaraxiaBoom.ModifyHitNPC"/>。
    /// ④ 命中音效改走本工程移植的 CursedDaggerThrow（源直接用灾厄路径）；
    /// ⑤ 使用帧 10 → **14**（用户点名，与焚灭天惩同档）。
    /// 注：⑤ 只是把出招频率降到约 4.3 挥/秒，**不改变任何单次命中表现**——爆发半径、分裂枚数、花瓣圈、各条音效
    /// 都挂在「每一次事件」上而不是时间轴上，故弹幕打击的视觉与听觉逐次与改前一致，只是每秒出现次数等比减少。
    /// </para>
    /// </summary>
    internal class Ataraxia : ModItem
    {
        /// <summary>研究所解锁数量 1（唯一武器）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>物品基础属性：94×92、伤害 710、14 帧挥砍、击退 2.5、月后稀有度 15（紫）、弹速 10、可转向挥舞</summary>
        public override void SetDefaults()
        {
            Item.width = 94;
            Item.height = 92;
            Item.damage = 710;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.knockBack = 2.5f;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<AtaraxiaMain>();
            Item.shootSpeed = 10f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>
        /// 挥砍射击：一枚主弹拿满伤害，两枚侧弹各拿 50%（沿用灾厄 1.4.4 的分配写法，是本工程按用户口径做的偏离之一）。
        /// 侧弹生成在两肩斜 45° 的位置，但用的是与主弹**相同的初速**（那个 22 倍速的向量只用来算生成点偏移，不参与出膛速度）；
        /// ai 参数 (0, 1) / (0, 2) 的第二个值区分左右，只影响绘制朝向。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            SoundEngine.PlaySound(SoundID.Item60, position);
            int centerID = ModContent.ProjectileType<AtaraxiaMain>();
            int centerDamage = damage;
            Projectile.NewProjectile(source, position, velocity, centerID, centerDamage, knockback, player.whoAmI, 0f, 0f);
            int sideID = ModContent.ProjectileType<AtaraxiaSide>();
            int sideDamage = (int)(0.5f * centerDamage);
            Vector2 originalVelocity = velocity;
            velocity.Normalize();
            velocity *= 22f;
            Vector2 rrp = player.RotatedRelativePoint(player.MountedCenter, true);
            Vector2 leftOffset = velocity.RotatedBy(MathHelper.PiOver4, default);
            Vector2 rightOffset = velocity.RotatedBy(-MathHelper.PiOver4, default);
            leftOffset -= 1.4f * velocity;
            rightOffset -= 1.4f * velocity;
            Projectile.NewProjectile(source, new Vector2(rrp.X + leftOffset.X, rrp.Y + leftOffset.Y), originalVelocity, sideID, sideDamage, knockback, player.whoAmI, 0f, 1f);
            Projectile.NewProjectile(source, new Vector2(rrp.X + rightOffset.X, rrp.Y + rightOffset.Y), originalVelocity, sideID, sideDamage, knockback, player.whoAmI, 0f, 2f);
            hitsound = true;
            return false;
        }
        /// <summary>挥舞表现：在判定框中心区撒 3~5 颗尘（70 / 71 / 86 三选一，灾厄原码用魔法数字，照抄）</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dustCount = Main.rand.Next(3, 6);
            Vector2 corner = new Vector2(hitbox.X + hitbox.Width / 4, hitbox.Y + hitbox.Height / 4);
            for (int i = 0; i < dustCount; ++i)
            {
                int dustID;
                switch (Main.rand.Next(5))
                {
                    case 0:
                    case 1:
                        dustID = 70;
                        break;
                    case 2:
                        dustID = 71;
                        break;
                    default:
                        dustID = 86;
                        break;
                }
                int idx = Dust.NewDust(corner, hitbox.Width / 2, hitbox.Height / 2, dustID);
                Main.dust[idx].noGravity = true;
            }
        }
        /// <summary>命中敌人（真近战）：挂原版暗影焰 480 帧，并在目标处生成大型爆发</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 480);
            OnHitEffects(player, target.Center);
        }
        /// <summary>命中玩家（PvP）：挂本工程移植的增强版暗影焰（<see cref="Shadowflame"/>，源用的是灾厄同名 buff），其余与 OnHitNPC 同构</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 480);
            OnHitEffects(player, target.Center);
        }
        /// <summary>
        /// 命中特效（真近战与 PvP 共用）：本挥砍第一次命中时补一声投掷音，然后在目标处生成 <see cref="AtaraxiaBoom"/>
        /// （ai[0] 留 0 → 半径 200 的大型爆发 + 花瓣尘），伤害取物品伤害的 70%。
        /// </summary>
        private void OnHitEffects(Player player, Vector2 targetPos)
        {
            if (hitsound)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.CursedDaggerThrow, player.Center);
                hitsound = false;
            }
            int trueMeleeID = ModContent.ProjectileType<AtaraxiaBoom>();
            int trueMeleeDamage = (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(0.7f * Item.damage);
            var source = player.GetSource_ItemUse(Item);
            Projectile.NewProjectile(source, targetPos, Vector2.Zero, trueMeleeID, trueMeleeDamage, Item.knockBack, player.whoAmI, 0.0f, 0.0f);
        }
        /// <summary>
        /// 「本次挥砍是否还没响过命中音」——Shoot 时置真、命中时置假。灾厄源为 ModItem 实例字段，照抄；
        /// 与 <see cref="ExoBlade"/> 的 hitCount 同理，多人下会串。
        /// </summary>
        public bool hitsound = true;
        /// <summary>
        /// 两条配方分别对应现代版与经典版（**两版配方不同，各自成条、不能混用**），都软依赖、缺料即不注册：
        /// · 现代版（灾厄 2.0.4 的配方）：断剑 + 金 Auric 锭×5 + 宇宙锭×8 + 飞升魂精×2 + 噩梦燃料×20 @ 宇宙铁砧。
        /// · 经典版（灾厄 1.4.2.101 的配方）：断剑 + 宇宙锭×25 + 幻魂质×35 + 噩梦燃料×90 + 吸热能量×90
        ///   + 暗日碎片×65 + 生命锭×15 + 灾厄核心×5 + 冥府碎片×10 @ 嘉登熔炉（其中幻魂质与冥府碎片只有经典版才有）。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem modernAuricBar)
                    && calamity.TryFind<ModItem>("CosmiliteBar", out ModItem modernCosmiliteBar)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModItem>("NightmareFuel", out ModItem modernNightmareFuel)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BrokenHeroSword);
                    recipe.AddIngredient(modernAuricBar.Type, 5);
                    recipe.AddIngredient(modernCosmiliteBar.Type, 8);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 2);
                    recipe.AddIngredient(modernNightmareFuel.Type, 20);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && classic.TryFind<ModItem>("NightmareFuel", out ModItem classicNightmareFuel)
                    && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && classic.TryFind<ModItem>("BarofLife", out ModItem barofLife)
                    && classic.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity)
                    && classic.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ItemID.BrokenHeroSword);
                    recipeClassic.AddIngredient(classicCosmiliteBar.Type, 25);
                    recipeClassic.AddIngredient(phantoplasm.Type, 35);
                    recipeClassic.AddIngredient(classicNightmareFuel.Type, 90);
                    recipeClassic.AddIngredient(endothermicEnergy.Type, 90);
                    recipeClassic.AddIngredient(darksunFragment.Type, 65);
                    recipeClassic.AddIngredient(barofLife.Type, 15);
                    recipeClassic.AddIngredient(coreofCalamity.Type, 5);
                    recipeClassic.AddIngredient(hellcasterFragment.Type, 10);
                    recipeClassic.AddTile(classicForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
