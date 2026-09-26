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
    /// 混乱之刃（Anarchy Blade）—— 秘银砧档近战（行为移植自灾厄大修 0.4.0.1.3 的 <c>AnarchyBladeEcType</c>）。
    /// 左键：每 3 次挥砍射出一道硫磺光束（<see cref="AnarchyBeam"/>）；受伤越重伤害越高（生命差 ×0.1 加算到基础伤害）。
    /// 右键：挥砍体积放大到 1.2 倍，命中时原地炸出硫磺爆炸并附加硫磺火；
    /// 自身血量低于一半时，每次右键命中还有 1/5 概率直接抹杀非 Boss 敌人（场上有任何 Boss 时不触发）。
    /// 配方：毁灭刃 + 不洁核心×5 + 灾祸核心×3 @ 秘银砧（后两样是灾厄材料，走软依赖）。
    /// 硫磺爆炸弹幕用的是灾厄本体 <c>BrimstoneBoom</c>，同样走软依赖。
    /// </summary>
    internal class AnarchyBlade : ModItem
    {
        /// <summary>允许同一件物品连续右键</summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>右键可用</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>每 3 次左键挥砍才发射一道光束</summary>
        private const int ShootPeriod = 3;
        /// <summary>左键挥砍计数（大修源码里是 ModItem 实例字段，这里照抄保持行为一致）</summary>
        private int ShootCount;
        /// <summary>
        /// 物品基础属性：114×122、伤害 203、19 帧挥砍、击退 7.5、黄名（60金）、光束初速 15
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 114;
            Item.height = 122;
            Item.damage = 203; // 源值 150，×1.35 膨胀（202.5 上取整）
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 19;
            Item.useTime = 19;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 60, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<AnarchyBeam>();
            Item.shootSpeed = 15;
        }
        /// <summary>左键 17 帧 / 1 倍体积，右键 19 帧 / 1.2 倍体积</summary>
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            Item.scale = 1;
            Item.useTime = Item.useAnimation = 17;
            if (player.altFunctionUse == 2)
            {
                Item.scale = 1.2f;
                Item.useTime = Item.useAnimation = 19;
            }
        }
        /// <summary>左键每满 3 次挥砍才真正发射一次光束（并补一声 Item20 音效）；右键不发射</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return false;
            ShootCount++;
            if (ShootCount < ShootPeriod)
                return false;
            ShootCount = 0;
            SoundEngine.PlaySound(SoundID.Item20, position);
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }
        /// <summary>挥舞表现：BetterSwing 修正挥舞位置；约 1/3 概率洒落灾厄硫磺尘</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, AnarchyBeam.BrimstoneDustType);
        }
        /// <summary>损失的生命值 ×0.1 直接加算到基础伤害（大修原设定）</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            int lifeAmount = player.statLifeMax2 - player.statLife;
            damage.Base += lifeAmount * 0.1f;
        }
        /// <summary>暴击伤害减半（大修原设定）</summary>
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) => modifiers.CritDamage *= 0.5f;
        /// <summary>
        /// 右键命中：原地炸出硫磺爆炸并附加硫磺火；自身血量 ≤ 半血时 1/5 概率直接抹杀非 Boss 敌人
        /// （场上存在任何 Boss 时抹杀不触发）。
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (player.altFunctionUse != 2)
                return;
            int boom = AnarchyBeam.BrimstoneBoomType();
            if (boom > 0)
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, boom, Item.damage, Item.knockBack, Main.myPlayer);
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "BrimstoneFlames", 300, BuffID.OnFire);
            if (player.statLife > player.statLifeMax2 * 0.5f || !Main.rand.NextBool(5))
                return;
            if (AnyBossAlive() || !ShouldAffectNPC(target))
                return;
            target.life = 0;
            target.HitEffect(0, 10.0);
            target.active = false;
            target.NPCLoot();
        }
        /// <summary>PvP 右键命中：炸出硫磺爆炸并附加硫磺火（不涉及抹杀）</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            if (player.altFunctionUse != 2)
                return;
            int boom = AnarchyBeam.BrimstoneBoomType();
            if (boom > 0)
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero, boom, Item.damage, Item.knockBack, Main.myPlayer);
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "BrimstoneFlames", 300, BuffID.OnFire);
        }
        /// <summary>
        /// 场景中是否有 Boss 存活。对应灾厄的 <c>CalamityPlayer.areThereAnyDamnBosses</c>
        /// （那个字段由灾厄自己的系统用 <c>CalamityUtils.AnyBossNPCS()</c> 刷新），这里直接扫 <c>NPC.boss</c>。
        /// </summary>
        private static bool AnyBossAlive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 对应灾厄的 <c>CalamityGlobalNPC.ShouldAffectNPC</c>：只对普通敌怪生效。
        /// 灾厄原版还排除了自家的一批小 Boss 部件（爬虫/暴君爪/死神鲨/巨型乌贼等），
        /// 那些类型没法在软依赖下引用，这里只保留原版那部分排除项。
        /// </summary>
        private static bool ShouldAffectNPC(NPC target) => target.damage > 0 && !target.boss && !target.friendly && !target.dontTakeDamage
            && target.type != NPCID.Creeper && target.type != NPCID.MourningWood && target.type != NPCID.Everscream
            && target.type != NPCID.SantaNK1 && target.type != NPCID.GolemFistLeft && target.type != NPCID.GolemFistRight
            && target.type != NPCID.DD2Betsy;
        /// <summary>配方：毁灭刃 + 不洁核心×5 + 灾祸核心×3 @ 秘银砧（两样核心走灾厄软依赖）</summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("UnholyCore", out ModItem unholyCore)
                && calamity.TryFind<ModItem>("CoreofHavoc", out ModItem coreofHavoc))
            {
                CreateRecipe().
                    AddIngredient(ItemID.BreakerBlade).
                    AddIngredient(unholyCore.Type, 5).
                    AddIngredient(coreofHavoc.Type, 3).
                    AddTile(TileID.MythrilAnvil).
                    Register();
            }
        }
    }
}
