using CalamityDemutation.Content.Items.Accessories.Attack;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Magic;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Summon;
using CalamityDemutation.Content.Items.Accessories.Movement;
using CalamityDemutation.Content.Items.Accessories.StatLife;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.NPCs
{
    /// <summary>
    /// 全局NPC类：向灾厄各 Boss 注入本模组的饰品掉落（兼容现代版与经典预发布版）
    /// </summary>
    internal class CalamityDemutationGlobalNPC : GlobalNPC
    {
        // ── 实例字段 ──
        /// <summary>
        /// 恶魔烈焰标记：由 DemonFlames debuff 在敌怪侧置位，参与命中附加效果
        /// </summary>
        public bool demonFlames = false;
        /// <summary>
        /// 狂怒标记：由 Enraged buff 在敌怪侧置位，仅用于 GlobalNPC.GetAlpha 染色
        /// </summary>
        public bool enraged = false;
        /// <summary>
        /// 女巫眩晕标记：由 SilvaHysteresis debuff 在敌怪侧置位，用于减速等结算
        /// </summary>
        public bool silvaHysteresis = false;
        // ── 属性 ──
        /// <summary>
        /// 按实例启用，避免多个 NPC 共享全局状态
        /// </summary>
        public override bool InstancePerEntity
        {
            get
            {
                return true;
            }
        }
        // ── 生命周期方法 ──
        /// <summary>
        /// tModLoader 的 ResetEffects 钩子：每帧重置 NPC 状态时调用（本类已按实例启用）。
        /// 把本模组在 NPC 上使用的三个标记——恶魔烈焰 demonFlames、狂怒 enraged、
        /// 女巫眩晕 silvaHysteresis——全部复位，随后由对应的 debuff Update 在同帧重新置位，
        /// 从而保证标记不会跨帧残留。
        /// </summary>
        public override void ResetEffects(NPC npc)
        {
            demonFlames = false;
            enraged = false;
            silvaHysteresis = false;
        }
        /// <summary>
        /// 玩家受到 NPC 攻击命中时触发：若玩家拥有“蜂抗”状态且攻击者属于蜂类单位，
        /// 则将最终伤害乘算 0.5f，实现蜂类伤害减半的效果
        /// </summary>
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (target.GetModPlayer<CalamityDemutationPlayer>().beeResist)
            {
                if (CalamityDemutation.beeEnemyList.Contains(npc.type))
                {
                    // 蜂抗生效：蜂类来源的最终伤害减半
                    modifiers.FinalDamage *= 0.5f;
                }
            }
        }
        /// <summary>
        /// 向灾厄各 Boss 的掉落池注入本模组饰品掉落
        /// </summary>
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 我需要与灾厄模组加载有关了
            LeadingConditionRule isExpert = new(new Conditions.IsExpert());
            // 非专家模式下额外掉落的规则
            LeadingConditionRule notExpert = new(new Conditions.NotExpert());
            // ===== 现代版灾厄（CalamityMod）：仅灾厄专属 NPC 的掉落规则（各保留一份） =====
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                // ===== 原版实体（原版 NPC / 原版 Boss）掉落：与灾厄是否安装无关，只注册一次、始终生效 =====
                if (npc.type == NPCID.SandElemental)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<WifeinaBottle>(), 7, 5));
                    npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsExpert(), ModContent.ItemType<WifeinaBottlewithBoobs>(), 20));
                }
                else if (npc.type == NPCID.Demon)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<BladecrestOathsword>(), 25, 20));
                }
                else if (npc.type == NPCID.BoneSerpentHead)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<OldLordOathsword>(), 25, 20));
                }
                else if (npc.type == NPCID.BlueJellyfish)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<ManaJelly>(), 7, 5));
                }
                else if (npc.type == NPCID.PinkJellyfish)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<LifeJelly>(), 7, 5));
                }
                else if (npc.type == NPCID.GreenJellyfish)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<VitalJelly>(), 7, 5));
                }
                else if (npc.type == NPCID.GoblinSummoner)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<TheFirstShadowflame>(), 7, 5));
                }
                else if (npc.type == NPCID.SeaSnail)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<SeaShell>(), 3, 2));
                }
                else if (npc.type == NPCID.Crawdad || npc.type == NPCID.Crawdad2)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<CrawCarapace>(), 7, 5));
                }
                else if (npc.type == NPCID.AnomuraFungus)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<FungalCarapace>(), 7, 5));
                }
                // 巨型陆龟：专家/非专家分别以不同保底系数（200、203）的选项池规则掉落巨龟壳
                else if (npc.type == NPCID.GiantTortoise)
                {
                    isExpert.OnSuccess(new OneFromOptionsDropRule(200, 2,
                    [
                            ModContent.ItemType<GiantTortoiseShell>(),
                ]));
                    notExpert.OnSuccess(new OneFromOptionsDropRule(203, 2,
                    [
                            ModContent.ItemType<GiantTortoiseShell>(),
                ]));
                    npcLoot.Add(isExpert);
                    npcLoot.Add(notExpert);
                }
                else if (npc.type == NPCID.GiantShelly || npc.type == NPCID.GiantShelly2)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<GiantShell>(), 7, 5));
                }
                else if (npc.type == NPCID.MoonLordCore)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<CelestialOnion>(), 1));
                }
                else if (npc.type == NPCID.WallofFlesh)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<CelestialWingsOnion>(), 1));
                }
                if (calamity0.TryFind<ModNPC>("Anahita", out ModNPC anahita) && npc.type == anahita.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<LureofEnthrallment>(), 7, 5));
                }
                else if(calamity0.TryFind<ModNPC>("CloudElemental", out ModNPC cloudElemental) && npc.type == cloudElemental.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<EyeoftheStorm>(), 7, 5));
                }
                else if (calamity0.TryFind<ModNPC>("BrimstoneElemental", out ModNPC brimstoneElemental) && npc.type == brimstoneElemental.Type)
                {
                    notExpert.OnSuccess(new CommonDrop(ModContent.ItemType<RoseStone>(), 10));
                    npcLoot.Add(notExpert);
                }
                else if (calamity0.TryFind<ModNPC>("Cryogen", out ModNPC cryogen) && npc.type == cryogen.Type)
                {
                    notExpert.OnSuccess(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                    npcLoot.Add(notExpert);
                }
                else if (calamity0.TryFind<ModNPC>("CalamitasClone", out ModNPC calamitasClone) && npc.type == calamitasClone.Type)
                {
                    notExpert.OnSuccess(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    npcLoot.Add(notExpert);
                }
                else if(calamity0.TryFind<ModNPC>("PlaguebringerGoliath", out ModNPC plaguebringerGoliath) && npc.type == plaguebringerGoliath.Type)
                {
                    notExpert.OnSuccess(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                    npcLoot.Add(notExpert);
                }
                else if(calamity0.TryFind<ModNPC>("Cnidrion", out ModNPC cnidrion) && npc.type == cnidrion.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<AmidiasSpark>(), 6, 4));
                }
                else if(calamity0.TryFind<ModNPC>("IrradiatedSlime", out ModNPC irradiatedSlime) && npc.type == irradiatedSlime.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<LeadCore>(), 10));
                }
                else if (calamity0.TryFind<ModNPC>("IceClasper", out ModNPC iceClasper) && npc.type == iceClasper.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<FrostBarrier>(), 10));
                }
                else if (npc.type == NPCID.PossessedArmor)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<PsychoticAmulet>(), 200, 150));
                }
            }
            // ===== 经典版灾厄（CalamityModClassicPreTrailer）：Boss 命名不同，掉落规则保持一致（各保留一份） =====
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // ===== 原版实体（原版 NPC / 原版 Boss）掉落：与灾厄是否安装无关，只注册一次、始终生效 =====
                if (npc.type == NPCID.SandElemental)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<WifeinaBottle>(), 7, 5));
                    npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsExpert(), ModContent.ItemType<WifeinaBottlewithBoobs>(), 20));
                }
                else if (npc.type == NPCID.Demon)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<BladecrestOathsword>(), 25, 20));
                }
                else if (npc.type == NPCID.BoneSerpentHead)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<OldLordOathsword>(), 25, 20));
                }
                else if (npc.type == NPCID.BlueJellyfish)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<ManaJelly>(), 7, 5));
                }
                else if (npc.type == NPCID.PinkJellyfish)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<LifeJelly>(), 7, 5));
                }
                else if (npc.type == NPCID.GreenJellyfish)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<VitalJelly>(), 7, 5));
                }
                else if (npc.type == NPCID.GoblinSummoner)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<TheFirstShadowflame>(), 7, 5));
                }
                else if (npc.type == NPCID.SeaSnail)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<SeaShell>(), 3, 2));
                }
                else if (npc.type == NPCID.Crawdad || npc.type == NPCID.Crawdad2)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<CrawCarapace>(), 7, 5));
                }
                else if (npc.type == NPCID.AnomuraFungus)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<FungalCarapace>(), 7, 5));
                }
                // 巨型陆龟：专家/非专家分别以不同保底系数（200、203）的选项池规则掉落巨龟壳
                else if (npc.type == NPCID.GiantTortoise)
                {
                    isExpert.OnSuccess(new OneFromOptionsDropRule(200, 2,
                    [
                            ModContent.ItemType<GiantTortoiseShell>(),
                ]));
                    notExpert.OnSuccess(new OneFromOptionsDropRule(203, 2,
                    [
                            ModContent.ItemType<GiantTortoiseShell>(),
                ]));
                    npcLoot.Add(isExpert);
                    npcLoot.Add(notExpert);
                }
                else if (npc.type == NPCID.GiantShelly || npc.type == NPCID.GiantShelly2)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<GiantShell>(), 7, 5));
                }
                else if (npc.type == NPCID.MoonLordCore)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<CelestialOnion>(), 1));
                }
                else if (npc.type == NPCID.WallofFlesh)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<CelestialWingsOnion>(), 1));
                }
                if (calamity1.TryFind<ModNPC>("Siren", out ModNPC siren) && npc.type == siren.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<LureofEnthrallment>(), 7, 5));
                }
                else if (calamity1.TryFind<ModNPC>("ThiccWaifu", out ModNPC thiccWaifu) && npc.type == thiccWaifu.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<EyeoftheStorm>(), 7, 5));
                }
                else if (calamity1.TryFind<ModNPC>("Cryogen", out ModNPC cryogen) && npc.type == cryogen.Type)
                {
                    notExpert.OnSuccess(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                    npcLoot.Add(notExpert);
                }
                else if (calamity1.TryFind<ModNPC>("Calamitas", out ModNPC calamitas) && npc.type == calamitas.Type)
                {
                    notExpert.OnSuccess(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    npcLoot.Add(notExpert);
                }
                else if (calamity1.TryFind<ModNPC>("PlaguebringerGoliath", out ModNPC plaguebringerGoliath) && npc.type == plaguebringerGoliath.Type)
                {
                    notExpert.OnSuccess(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                    npcLoot.Add(notExpert);
                }
                else if (calamity1.TryFind<ModNPC>("Cnidrion", out ModNPC cnidrion) && npc.type == cnidrion.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<AmidiasSpark>(), 6, 4));
                }
                else if (calamity1.TryFind<ModNPC>("IrradiatedSlime", out ModNPC irradiatedSlime) && npc.type == irradiatedSlime.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<LeadCore>(), 10));
                }
                else if (calamity1.TryFind<ModNPC>("IceClasper", out ModNPC iceClasper) && npc.type == iceClasper.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<FrostBarrier>(), 10));
                }
                else if (npc.type == NPCID.PossessedArmor)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<PsychoticAmulet>(), 200, 150));
                }
            }
        }
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.TravellingMerchant && Main.moonPhase == 0)
            {
                shop.Add(ModContent.ItemType<FrostBarrier>());
            }
        }
        /// <summary>
        /// tModLoader 的 UpdateLifeRegen 钩子：每帧结算 NPC 生命回复时调用，
        /// 通过修改 npc.lifeRegen 施加持续灼烧，并用 ref damage 指定该灼烧显示的每帧伤害数字。
        /// 恶魔烈焰（demonFlames）先清掉正回复，再 -5000，damage 下限抬到 2000；
        /// 女巫眩晕（silvaHysteresis）同样清正回复后 -900，damage 下限 400。
        /// 两个标记分别由 DemonFlames / SilvaHysteresis buff 每帧置位。
        /// </summary>
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (demonFlames)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 5000;
                if (damage < 2000)
                {
                    damage = 2000;
                }
            }
            if(silvaHysteresis)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 900;
                if (damage < 400)
                {
                    damage = 400;
                }
            }
        }
        /// <summary>
        /// tModLoader 的 GetAlpha 钩子：NPC 绘制时决定叠加颜色。返回 null 表示保持默认着色；
        /// 狂怒标记（enraged）生效时返回 (200,50,50) 的红色，alpha 沿用 npc.alpha。
        /// 该标记由 Enraged buff 在敌怪侧置位，仅用于染色，不参与伤害结算。
        /// </summary>
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            if (enraged)
                return new Color(200, 50, 50, npc.alpha);
            return null;
        }
        /// <summary>
        /// tModLoader 的 OnKill 钩子：NPC 死亡时调用，在此额外生成掉落物。
        /// 距离该 NPC 最近的玩家装备塔拉套装（tarraSet）时：非常从雕像生成、伤害 &gt; 5 或为 Boss、
        /// 最大生命 &gt; 100 的敌人有 1/5 概率掉落生命红心（物品 ID 58）。
        /// 该玩家装备血焰套装（bloodflareSet）时，在血月、NPC 位于地表以上且有玩家目标的情况下，
        /// 有 1/2 概率掉落灾厄的血球（BloodOrb）——现代版 CalamityMod 与经典版
        /// CalamityModClassicPreTrailer 各自分支，用对应模组实例查找该物品。
        /// </summary>
        public override void OnKill(NPC npc)
        {
            if (Main.player[(int)Player.FindClosest(npc.position, npc.width, npc.height)].GetModPlayer<CalamityDemutationPlayer>().tarraSet)
            {
                if (!npc.SpawnedFromStatue && (npc.damage > 5 || npc.boss) && npc.lifeMax > 100 && Main.rand.NextBool(5))
                {
                    Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, 58, 1, false, 0, false, false);
                }
            }
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (Main.player[(int)Player.FindClosest(npc.position, npc.width, npc.height)].GetModPlayer<CalamityDemutationPlayer>().bloodflareSet)
                {
                    if (!npc.SpawnedFromStatue && (npc.damage > 5 || npc.boss) && Main.rand.NextBool(2) && Main.bloodMoon && npc.HasPlayerTarget && (double)(npc.position.Y / 16f) < Main.worldSurface)
                    {
                        Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, calamity.Find<ModItem>("BloodOrb").Type, 1, false, 0, false, false);
                    }
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (Main.player[(int)Player.FindClosest(npc.position, npc.width, npc.height)].GetModPlayer<CalamityDemutationPlayer>().bloodflareSet)
                {
                    if (!npc.SpawnedFromStatue && (npc.damage > 5 || npc.boss) && Main.rand.NextBool(2) && Main.bloodMoon && npc.HasPlayerTarget && (double)(npc.position.Y / 16f) < Main.worldSurface)
                    {
                        Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, calamity1.Find<ModItem>("BloodOrb").Type, 1, false, 0, false, false);
                    }
                }
            }
        }
        /// <summary>
        /// tModLoader 的 OnHitByItem 钩子：NPC 被玩家的物品（近战等）命中后调用。
        /// 攻击者装备血焰套装（bloodflareSet）时，对非常从雕像生成且 damage &gt; 0 的敌人：
        /// 生命低于 50% 且 bloodflareHeartTimer 已归零则掉落生命红心（ID 58），
        /// 生命高于 50% 且 bloodflareManaTimer 已归零则掉落魔力星（ID 184），
        /// 掉落的同时把对应计时器置 180 帧，用于限制掉落频率。
        /// </summary>
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (player.GetModPlayer<CalamityDemutationPlayer>().bloodflareSet)
            {
                if (!npc.SpawnedFromStatue && npc.damage > 0 && ((double)npc.life < (double)npc.lifeMax * 0.5) &&
                    player.GetModPlayer<CalamityDemutationPlayer>().bloodflareHeartTimer <= 0)
                {
                    player.GetModPlayer<CalamityDemutationPlayer>().bloodflareHeartTimer = 180;
                    Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, 58, 1, false, 0, false, false);
                }
                else if (!npc.SpawnedFromStatue && npc.damage > 0 && ((double)npc.life > (double)npc.lifeMax * 0.5) &&
                    player.GetModPlayer<CalamityDemutationPlayer>().bloodflareManaTimer <= 0)
                {
                    player.GetModPlayer<CalamityDemutationPlayer>().bloodflareManaTimer = 180;
                    Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, 184, 1, false, 0, false, false);
                }
            }
        }
        /// <summary>
        /// tModLoader 的 OnHitByProjectile 钩子：NPC 被玩家弹幕命中后调用，逻辑与 OnHitByItem 一致，
        /// 区别是以弹幕主人 Main.player[projectile.owner] 作为判定对象。
        /// 血焰套装（bloodflareSet）下按目标生命是否低于 50%，分别掉落生命红心（ID 58）或魔力星（ID 184），
        /// 并重置对应的 180 帧冷却计时器（bloodflareHeartTimer / bloodflareManaTimer）。
        /// </summary>
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().bloodflareSet)
            {
                if (!npc.SpawnedFromStatue && npc.damage > 0 && ((double)npc.life < (double)npc.lifeMax * 0.5) &&
                    Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().bloodflareHeartTimer <= 0)
                {
                    Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().bloodflareHeartTimer = 180;
                    Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, 58, 1, false, 0, false, false);
                }
                else if (!npc.SpawnedFromStatue && npc.damage > 0 && ((double)npc.life > (double)npc.lifeMax * 0.5) &&
                    Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().bloodflareManaTimer <= 0)
                {
                    Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().bloodflareManaTimer = 180;
                    Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, 184, 1, false, 0, false, false);
                }
            }
        }
    }
}
