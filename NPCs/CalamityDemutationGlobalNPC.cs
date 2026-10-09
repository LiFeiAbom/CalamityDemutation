using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Items.Accessories.Attack;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Magic;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Summon;
using CalamityDemutation.Content.Items.Accessories.Movement;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Items.Consumables;
using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using CalamityDemutation.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
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
        /// 狂怒标记：由 Enraged buff 在敌怪侧置位。
        /// 两处消费——<see cref="GetAlpha"/> 把它染成红色；<see cref="ModifyHitPlayer"/> 让这只敌怪造成 +25% 伤害
        /// </summary>
        public bool enraged = false;
        public bool hellfireExplosion = false;
        /// <summary>
        /// 女巫眩晕标记：由 SilvaHysteresis debuff 在敌怪侧置位，用于减速等结算
        /// </summary>
        public bool silvaHysteresis = false;
        /// <summary>
        /// 泰拉巨刃的电击命中计数（移植自大修 CWRGlobalNPC 的 TerratomereBoltOnHitNum）：
        /// TerratomereBigSlashs 每次命中 +1、上限 6，累计超过 5 时触发 TerratomereExplosion 爆炸后清零。
        /// </summary>
        public int TerratomereBoltOnHitNum = 0;
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
        /// 把本模组在 NPC 上使用的四个标记——恶魔烈焰 demonFlames、狂怒 enraged、地狱火爆炸 hellfireExplosion、
        /// 女巫眩晕 silvaHysteresis——全部复位，随后由对应的 debuff
        /// Update 在同帧重新置位，从而保证标记不会跨帧残留。
        /// </summary>
        public override void ResetEffects(NPC npc)
        {
            demonFlames = false;
            enraged = false;
            hellfireExplosion = false;
            silvaHysteresis = false;
        }
        /// <summary>
        /// 玩家受到 NPC 攻击命中时触发：蜂抗（蜂类来源伤害减至 75%）与魔影套装「激怒」的增伤都在这里结算。
        /// </summary>
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (target.GetModPlayer<CalamityDemutationPlayer>().beeResist)
            {
                if (CalamityDemutation.beeEnemyList.Contains(npc.type))
                {
                    // 蜂抗生效：蜂类来源的最终伤害减至 75%
                    modifiers.FinalDamage *= 0.75f;
                }
            }
            // 魔影套装 Y 键「激怒」的另一半：被 Enraged 标记的敌怪造成的伤害 +25%，
            // 与套装文案「它们受到的伤害提高 125%」（由玩家自身的 Enraged 增伤实现）配对。
            if (enraged)
            {
                modifiers.FinalDamage *= 1.25f;
            }
        }
        /// <summary>
        /// 向灾厄各 Boss 的掉落池注入本模组饰品掉落
        /// </summary>
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // ===== 原版实体（原版 NPC / 原版 Boss）掉落：与灾厄是否安装无关，只注册一次、始终生效 =====
            // 本条链放在两个灾厄分支之前无条件执行，保证同时安装现代版与经典版灾厄时原版怪也只注册一遍掉落
            LeadingConditionRule isExpert = new(new Conditions.IsExpert());
            // 非专家模式下额外掉落的规则
            LeadingConditionRule notExpert = new(new Conditions.NotExpert());
            if (npc.type == NPCID.SandElemental)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<WifeinaBottle>(), 7, 5));
                npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsExpert(), ModContent.ItemType<WifeinaBottlewithBoobs>(), 20));
            }
            else if (npc.type == NPCID.Demon)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<BladecrestOathsword>(), 25, 20));
            }
            else if (npc.type == NPCID.RedDevil)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<Abaddon>(), 12, 7));
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
                npcLoot.Add(new CommonDrop(ModContent.ItemType<MoonPact>(), 1));
            }
            // 被附身铠甲属于原版怪，其掉落随原版链注册
            else if (npc.type == NPCID.PossessedArmor)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<PsychoticAmulet>(), 200, 150));
            }
            // 王冠宝石：照源挂史莱姆王（普通 10% / 专家宝藏袋 10%，用 NormalvsExpert 一条写清）
            else if (npc.type == NPCID.KingSlime)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<CrownJewel>(), 10, 10));
            }
            // 本工程自有的丛林材料（供「（古）」链）——掉落者与概率照 2.0.7.2 的 CalamityGlobalNPCLoot
            else if (npc.type == NPCID.JungleSlime || npc.type == NPCID.SpikedJungleSlime || npc.type == NPCID.Arapaima)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<MurkyPaste>(), 3, 2));
            }
            else if (npc.type == NPCID.AngryTrapper)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<TrapperBulb>(), 2, 1));
            }
            // 本工程自有的沙漠材料（供「太阳神杖」链）——掉落者与概率照 2.0 的 CalamityGlobalNPCLoot：
            // 秃鹫 100% 掉 1~2 片
            else if (npc.type == NPCID.Vulture)
            {
                npcLoot.Add(new CommonDrop(ModContent.ItemType<DesertFeather>(), 1, 1, 2));
            }
            // ===== 现代版灾厄（CalamityMod）：仅灾厄专属 NPC 的掉落规则（各保留一份） =====
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 本分支专用的非专家条件规则，不与经典版分支共用实例
                LeadingConditionRule notExpert0 = new(new Conditions.NotExpert());
                if (calamity.TryFind<ModNPC>("Anahita", out ModNPC anahita) && npc.type == anahita.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<LureofEnthrallment>(), 7, 5));
                }
                else if(calamity.TryFind<ModNPC>("CloudElemental", out ModNPC cloudElemental) && npc.type == cloudElemental.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<EyeoftheStorm>(), 7, 5));
                }
                else if (calamity.TryFind<ModNPC>("BrimstoneElemental", out ModNPC brimstoneElemental) && npc.type == brimstoneElemental.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<RoseStone>(), 10));
                    npcLoot.Add(notExpert0);
                }
                else if (calamity.TryFind<ModNPC>("Cryogen", out ModNPC cryogen) && npc.type == cryogen.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                    npcLoot.Add(notExpert0);
                }
                else if (calamity.TryFind<ModNPC>("CalamitasClone", out ModNPC calamitasClone) && npc.type == calamitasClone.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    npcLoot.Add(notExpert0);
                }
                else if(calamity.TryFind<ModNPC>("PlaguebringerGoliath", out ModNPC plaguebringerGoliath) && npc.type == plaguebringerGoliath.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                    npcLoot.Add(notExpert0);
                }
                else if(calamity.TryFind<ModNPC>("Cnidrion", out ModNPC cnidrion) && npc.type == cnidrion.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<AmidiasSpark>(), 6, 4));
                }
                else if(calamity.TryFind<ModNPC>("IrradiatedSlime", out ModNPC irradiatedSlime) && npc.type == irradiatedSlime.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<LeadCore>(), 10));
                }
                else if (calamity.TryFind<ModNPC>("IceClasper", out ModNPC iceClasper) && npc.type == iceClasper.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<FrostBarrier>(), 10));
                    // 本工程自持的古冰晶：照源 2.0.3.9 与经典版同一条——冰灵 1/3 概率掉 1 个
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<AncientIceChunk>(), 3));
                }
                else if (calamity.TryFind<ModNPC>("Providence", out ModNPC providence) && npc.type == providence.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<ElysianAegis>(), 1));
                }
                // 女妖之爪（CWR 重制版）：现代版源里它属于波尔提斯的"非专家武器池"——
                // DropHelper.CalamityStyle(1/4, 7 把武器)：每把独立 1/4，若全不中则保底随机给一把；
                // 本工程只能往池子里补自己这一件，沿用既有简化口径记作 1/4 非专家掉落（宝袋那条见 GlobalItem）。
                else if (calamity.TryFind<ModNPC>("Polterghast", out ModNPC polterghast) && npc.type == polterghast.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<BansheeHook>(), 4));
                    npcLoot.Add(notExpert0);
                }
            }
            // ===== 经典版灾厄（CalamityModClassicPreTrailer）：Boss 命名不同，掉落规则保持一致（各保留一份） =====
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 本分支专用的非专家条件规则，不与现代版分支共用实例
                LeadingConditionRule notExpert1 = new(new Conditions.NotExpert());
                if (classic.TryFind<ModNPC>("Siren", out ModNPC siren) && npc.type == siren.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<LureofEnthrallment>(), 7, 5));
                }
                else if (classic.TryFind<ModNPC>("ThiccWaifu", out ModNPC thiccWaifu) && npc.type == thiccWaifu.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<EyeoftheStorm>(), 7, 5));
                }
                else if (classic.TryFind<ModNPC>("Cryogen", out ModNPC cryogen) && npc.type == cryogen.Type)
                {
                    notExpert1.OnSuccess(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                    npcLoot.Add(notExpert1);
                }
                else if (classic.TryFind<ModNPC>("Calamitas", out ModNPC calamitas) && npc.type == calamitas.Type)
                {
                    notExpert1.OnSuccess(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    npcLoot.Add(notExpert1);
                }
                else if (classic.TryFind<ModNPC>("PlaguebringerGoliath", out ModNPC plaguebringerGoliath) && npc.type == plaguebringerGoliath.Type)
                {
                    notExpert1.OnSuccess(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                    npcLoot.Add(notExpert1);
                }
                else if (classic.TryFind<ModNPC>("Cnidrion", out ModNPC cnidrion) && npc.type == cnidrion.Type)
                {
                    npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<AmidiasSpark>(), 6, 4));
                }
                else if (classic.TryFind<ModNPC>("IrradiatedSlime", out ModNPC irradiatedSlime) && npc.type == irradiatedSlime.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<LeadCore>(), 10));
                }
                else if (classic.TryFind<ModNPC>("IceClasper", out ModNPC iceClasper) && npc.type == iceClasper.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<FrostBarrier>(), 10));
                    // 本工程自持的古冰晶：照源 2.0.3.9 与经典版同一条——冰灵 1/3 概率掉 1 个
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<AncientIceChunk>(), 3));
                }
                else if (classic.TryFind<ModNPC>("Providence", out ModNPC providence) && npc.type == providence.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<ElysianAegis>(), 1));
                }
                // 女妖之爪（CWR 重制版）：经典版源里就是波尔提斯的 CommonDrop(..., 4)，即 1/4 非专家掉落（精确复刻）
                else if (classic.TryFind<ModNPC>("Polterghast", out ModNPC polterghast) && npc.type == polterghast.Type)
                {
                    notExpert1.OnSuccess(new CommonDrop(ModContent.ItemType<BansheeHook>(), 4));
                    npcLoot.Add(notExpert1);
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
        /// 各标记均由对应的 debuff（DemonFlames / SilvaHysteresis / HellfireExplosion）每帧置位。
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
            if(hellfireExplosion)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 1000;
                if(damage < 450)
                {
                    damage = 450;
                }
            }
        }
        /// <summary>
        /// tModLoader 的 GetAlpha 钩子：NPC 绘制时决定叠加颜色。返回 null 表示保持默认着色；
        /// 狂怒标记（enraged）生效时返回 (200,50,50) 的红色，alpha 沿用 npc.alpha。
        /// 该标记由 Enraged buff 在敌怪侧置位；染色只是表现，其增伤结算见 <see cref="ModifyHitPlayer"/>。
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
                    if (calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb) && !npc.SpawnedFromStatue && (npc.damage > 5 || npc.boss) && Main.rand.NextBool(2) && Main.bloodMoon && npc.HasPlayerTarget && (double)(npc.position.Y / 16f) < Main.worldSurface)
                    {
                        Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, bloodOrb.Type, 1, false, 0, false, false);
                    }
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (Main.player[(int)Player.FindClosest(npc.position, npc.width, npc.height)].GetModPlayer<CalamityDemutationPlayer>().bloodflareSet)
                {
                    if (classic.TryFind<ModItem>("BloodOrb", out ModItem classicBloodOrb) && !npc.SpawnedFromStatue && (npc.damage > 5 || npc.boss) && Main.rand.NextBool(2) && Main.bloodMoon && npc.HasPlayerTarget && (double)(npc.position.Y / 16f) < Main.worldSurface)
                    {
                        Item.NewItem(npc.GetSource_FromThis(), (int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, classicBloodOrb.Type, 1, false, 0, false, false);
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
            // 无主/敌意弹幕（owner = 255 = Main.maxPlayers）会越界，先做范围校验
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;
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
