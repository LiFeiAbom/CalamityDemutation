using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 归虚之灵（CI 的 <c>CosmicImmaterializerOld</c>，即「归虚之灵[Legacy]」）——
    /// 「归虚之灵」链（CI 口径）的**链顶**，也是这个批次最后一件。
    /// 口径照 CI：74×72、伤害 **360**、魔力 10、使用/动画 **10 帧**、击退 0、挥动姿态、
    /// **月后 15 档（紫，＝CI 的 `CatalystViolet`）**、价值 **1 铂金 50 金**、音 `SoundID.Item60`、弹速 10，
    /// 在鼠标处召唤一团**宇宙之灵**（<see cref="CosmicEnergySpiralOld"/>）。
    /// </summary>
    /// <remarks>
    /// **ExoLore（传颂之物）分支：本工程恒处 Lore 模式**——照第 9 节 ExoBlade / ExoBeam 的既有口径，
    /// 不搬 CI 的 Lore 判定与专属类，直接把 Lore 那一条分支的行为当默认：仆从**恒发射 12~18 枚小爆裂 +
    /// 2 枚斜射大爆裂 + 1 枚直射大爆裂**（非 Lore 分支是 5~8 小 + 1 大），冷却 60 帧（非 Lore 是 100 帧），
    /// 仆从本体也按 Lore 分支的写法染成**纯白**（非 Lore 分支才是彩虹色）。
    /// <para>
    /// 其他照源细节：`CanUseItem` 要 `maxMinions >= 10` 且全场只能有一只；出手先**清掉自己在场的同类召唤物**；
    /// 掉在地上的发光层走工程既有的 `Item.DrawItemGlowmaskSingleFrame`。
    /// </para>
    /// <para>
    /// **两处按用户 2026-10-09 的口径做的改动**：① tooltip 去掉 CI 的 ExoLore 说明行、回到常规三行；
    /// ② 源里"**没穿月后召唤套时伤害 ×0.66**"这条**不照抄**——本工程的伤害与 `originalDamage` 都按全额走
    /// （灾厄查的是 `CalamityPlayer.WearingPostMLSummonerSet`，本工程没有等价 API，索性不做这条惩罚）。
    /// </para>
    /// <para>
    /// **配方（CI 那条八重 @ 嘉登熔炉）**：天狼星 + 古冰晶 + 元素之斧 + 圣化火花 + 空灵征服者 + 宇宙灯笼 +
    /// 灾厄挽歌 + 奇迹物质 `MiracleMatter`——**前七味全是本工程自持件**，只软引用奇迹物质与嘉登熔炉。
    /// CI 另有一条把奇迹物质换成"古代奇迹物质"的变体，本工程没有那件，故只注册这一条；经典版没有奇迹物质，
    /// 因此不注册经典分支。
    /// </para>
    /// </remarks>
    internal class CosmicImmaterializerOld:ModItem
    {
        /// <summary>研究解锁一份（照 CI）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：照 CI（74×72、伤害 360、使用 10 帧、月后 15 档、1 铂金 50 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 74;
            Item.height = 72;
            Item.damage = 360;
            Item.mana = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = Item.useAnimation = 10;
            Item.noMelee = true;
            Item.knockBack = 0f;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;   // 月后 15：紫（＝CI 的 CatalystViolet）
            Item.UseSound = SoundID.Item60;
            Item.shoot = ModContent.ProjectileType<CosmicEnergySpiralOld>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Summon;
        }
        /// <summary>要 10 格召唤栏，且全场只能同时存在一只宇宙之灵（照源）</summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0 && player.maxMinions >= 10;
        /// <summary>掉落在地上的发光层（照源：单帧 glowmask）</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI) => Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Summon/CosmicImmaterializerOldGlow").Value);
        /// <summary>
        /// 出手：先清掉自己在场的同类召唤物（源调灾厄的 CalamityUtils.KillShootProjectiles，本工程写等价循环），
        /// 再在鼠标处生成；伤害与 `originalDamage` **都按全额**（不照抄源里"没穿月后召唤套就 ×0.66"那条，见类注释）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                {
                    p.Kill();
                }
            }

            int p2 = Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI, 0f, 0f);
            if (Main.projectile.IndexInRange(p2))
                Main.projectile[p2].originalDamage = Item.damage;
            return false;
        }
        /// <summary>
        /// 配方：CI 那条八重 @ 嘉登熔炉（七味自持件 + 软引用的奇迹物质）。
        /// CI 的"古代奇迹物质"变体与本工程的软依赖口径无关，未移植；经典版没有奇迹物质，故不注册经典分支。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("MiracleMatter", out ModItem miracleMatter) &&
                calamity.TryFind<ModTile>("DraedonsForge", out ModTile forge))
            {
                CreateRecipe().
                    AddIngredient<Sirius>().
                    AddIngredient<AncientIceChunk>().
                    AddIngredient<ElementalAxe>().
                    AddIngredient<SanctifiedSpark>().
                    AddIngredient<EtherealSubjugator>().
                    AddIngredient<Cosmilamp>().
                    AddIngredient<CalamarisLament>().
                    AddIngredient(miracleMatter.Type).
                    AddTile(forge.Type).
                    Register();
            }
            else
            {
                Mod.Logger.Warn("归虚之灵：现代版灾厄里找不到 MiracleMatter / DraedonsForge，配方未注册。");
            }
        }
    }
}
