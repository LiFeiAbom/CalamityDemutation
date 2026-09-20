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
using CalamityDemutation.Content.Items.Accessories.StatLife;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Items.Weapons.Melee;
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
        /// 狂怒标记：由 Enraged buff 在敌怪侧置位，仅用于 GlobalNPC.GetAlpha 染色
        /// </summary>
        public bool enraged = false;
        public bool hellfireExplosion = false;
        /// <summary>
        /// 女巫眩晕标记：由 SilvaHysteresis debuff 在敌怪侧置位，用于减速等结算
        /// </summary>
        public bool silvaHysteresis = false;
        public bool voidErosion = false;
        // ── 虚空侵蚀（VoidTouch）实例字段：移植自 CE 的 EGlobalNPC（这三项在 CE 里不随帧重置，是持续计数）──
        /// <summary>
        /// 虚空侵蚀剩余时间（帧）：每次被虚空系攻击命中累加，归零时层数清零（CE 的 VoidTouchTime，上限由施加方给）
        /// </summary>
        public int VoidTouchTime = 0;
        /// <summary>
        /// 虚空侵蚀层数：每层让敌怪受击增伤 +1%（弹幕命中）/ +5%（真近战命中），并每 20 帧额外扣 26×层数 血
        /// （CE 的 VoidTouchLevel，上限由施加方给，通常是 10）
        /// </summary>
        public float VoidTouchLevel = 0;
        /// <summary>
        /// 虚空侵蚀抗性（0~1）：为 1 时完全免疫（AddVoidTouch 直接返回失败），否则全部效果按 (1 - 此值) 缩放
        /// （CE 的 VoidTouchDR，本模组暂无内容设置它，作为系统的一部分保留）
        /// </summary>
        public float VoidTouchDR = 0;
        /// <summary>灵魂紊乱染色用的配色图（CE 的 Assets/Extra/SoulDiscorderColorMap）</summary>
        private const string SoulDiscorderColorMap = "CalamityDemutation/Assets/ExtraTextures/SoulDiscorderColorMap";
        /// <summary>本帧是否已为这只敌怪换到着色器批次（换过就要在 PostDraw 里还原）</summary>
        private bool soulDisorderShaderActive = false;
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
        /// 把本模组在 NPC 上使用的五个标记——恶魔烈焰 demonFlames、狂怒 enraged、地狱火爆炸 hellfireExplosion、
        /// 女巫眩晕 silvaHysteresis、虚空侵蚀 voidErosion——全部复位，随后由对应的 debuff Update 在同帧重新置位，
        /// 从而保证标记不会跨帧残留。
        /// </summary>
        public override void ResetEffects(NPC npc)
        {
            demonFlames = false;
            enraged = false;
            hellfireExplosion = false;
            silvaHysteresis = false;
            voidErosion = false;
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
                npcLoot.Add(new CommonDrop(ModContent.ItemType<CelestialWingsOnion>(), 1));
            }
            // 被附身铠甲属于原版怪，其掉落随原版链注册
            else if (npc.type == NPCID.PossessedArmor)
            {
                npcLoot.Add(ItemDropRule.NormalvsExpert(ModContent.ItemType<PsychoticAmulet>(), 200, 150));
            }
            // ===== 现代版灾厄（CalamityMod）：仅灾厄专属 NPC 的掉落规则（各保留一份） =====
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                // 本分支专用的非专家条件规则，不与经典版分支共用实例
                LeadingConditionRule notExpert0 = new(new Conditions.NotExpert());
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
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<RoseStone>(), 10));
                    npcLoot.Add(notExpert0);
                }
                else if (calamity0.TryFind<ModNPC>("Cryogen", out ModNPC cryogen) && npc.type == cryogen.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                    npcLoot.Add(notExpert0);
                }
                else if (calamity0.TryFind<ModNPC>("CalamitasClone", out ModNPC calamitasClone) && npc.type == calamitasClone.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    npcLoot.Add(notExpert0);
                }
                else if(calamity0.TryFind<ModNPC>("PlaguebringerGoliath", out ModNPC plaguebringerGoliath) && npc.type == plaguebringerGoliath.Type)
                {
                    notExpert0.OnSuccess(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                    npcLoot.Add(notExpert0);
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
                else if (calamity0.TryFind<ModNPC>("Providence", out ModNPC providence) && npc.type == providence.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<ElysianAegis>(), 1));
                }
            }
            // ===== 经典版灾厄（CalamityModClassicPreTrailer）：Boss 命名不同，掉落规则保持一致（各保留一份） =====
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 本分支专用的非专家条件规则，不与现代版分支共用实例
                LeadingConditionRule notExpert1 = new(new Conditions.NotExpert());
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
                    notExpert1.OnSuccess(new CommonDrop(ModContent.ItemType<CryoStone>(), 10));
                    npcLoot.Add(notExpert1);
                }
                else if (calamity1.TryFind<ModNPC>("Calamitas", out ModNPC calamitas) && npc.type == calamitas.Type)
                {
                    notExpert1.OnSuccess(new CommonDrop(ModContent.ItemType<ChaosStone>(), 10));
                    npcLoot.Add(notExpert1);
                }
                else if (calamity1.TryFind<ModNPC>("PlaguebringerGoliath", out ModNPC plaguebringerGoliath) && npc.type == plaguebringerGoliath.Type)
                {
                    notExpert1.OnSuccess(new CommonDrop(ModContent.ItemType<BloomStone>(), 10));
                    npcLoot.Add(notExpert1);
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
                else if (calamity1.TryFind<ModNPC>("Providence", out ModNPC providence) && npc.type == providence.Type)
                {
                    npcLoot.Add(new CommonDrop(ModContent.ItemType<ElysianAegis>(), 1));
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
            if(voidErosion)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 10000;
                if (damage < 5000)
                {
                    damage = 5000;
                }
            }
        }
        /// <summary>
        /// tModLoader 的 DrawEffects 钩子：NPC 绘制前调用，可修改 drawColor 并生成粒子。
        /// 虚空侵蚀（voidErosion）生效时把红色通道压到 100（整体偏青），并在敌怪四周喷射星屑。
        /// 该标记由 VoidErosion debuff 在敌怪侧置位。
        /// </summary>
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (voidErosion)
            {
                drawColor.R = 100;
                // 仅在本地玩家附近喷射星屑：远处敌人本就不可见，避免多只被侵蚀敌怪挤占全局粒子上限
                if (Vector2.DistanceSquared(npc.Center, Main.LocalPlayer.Center) < 1600f * 1600f)
                    VoidErosion.SpanStar(npc, CDUtil.randVr(npc.width / 2));
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
        /// tModLoader 的 PreDraw 钩子：带「灵魂紊乱」减益的敌怪换到带 <c>SoulDiscorder</c> 着色器的批次里，
        /// 之后原版就会用这个批次画出这只敌怪（返回 true 继续默认绘制）。着色器参数照 CE 原样：
        /// <c>f1/f2</c> 是当前动画帧在贴图里的上下边界（只染这一帧，不会把整张图都算进去），
        /// <c>offset</c> 走全局时间让灵魂色流动，<c>colorMap</c> 是配色图；批次还原在 <see cref="PostDraw"/>。
        /// </summary>
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            soulDisorderShaderActive = false;
            if (npc.HasBuff(ModContent.BuffType<SoulDisorder>()))
            {
                Effect shader = CDShaders.SoulDiscorderShader.Value;
                shader.Parameters["strength"].SetValue(1);
                int npcTextureHeight = TextureAssets.Npc[npc.type].Value.Height;
                shader.Parameters["f1"].SetValue((float)npc.frame.Y / npcTextureHeight);
                shader.Parameters["f2"].SetValue((float)(npc.frame.Y + npc.frame.Height) / npcTextureHeight);
                shader.Parameters["offset"].SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["colorMap"].SetValue(ModContent.Request<Texture2D>(SoulDiscorderColorMap).Value);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
                shader.CurrentTechnique.Passes[0].Apply();
                soulDisorderShaderActive = true;
            }
            return true;
        }
        /// <summary>把 PreDraw 里换成着色器的批次还原回默认批次，避免污染它之后同帧的绘制</summary>
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (soulDisorderShaderActive)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
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
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (Main.player[(int)Player.FindClosest(npc.position, npc.width, npc.height)].GetModPlayer<CalamityDemutationPlayer>().bloodflareSet)
                {
                    if (calamity1.TryFind<ModItem>("BloodOrb", out ModItem classicBloodOrb) && !npc.SpawnedFromStatue && (npc.damage > 5 || npc.boss) && Main.rand.NextBool(2) && Main.bloodMoon && npc.HasPlayerTarget && (double)(npc.position.Y / 16f) < Main.worldSurface)
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
        /// <summary>
        /// 给敌怪叠加虚空侵蚀（移植自 CE 的 <c>EGlobalNPC.AddVoidTouch</c>）：
        /// 时间按传入值的 1.4 倍累加（封顶 <paramref name="maxTime"/>），层数按 <c>level / 10</c> 累加
        /// （封顶 <paramref name="maxLevel"/>）。已完全免疫（抗性为 1）时直接返回 false。
        /// </summary>
        public static bool AddVoidTouch(NPC npc, int time, float level, int maxTime = 600, int maxLevel = 10)
        {
            CalamityDemutationGlobalNPC globalNPC = npc.GetGlobalNPC<CalamityDemutationGlobalNPC>();
            if (globalNPC.VoidTouchDR == 1f)
            {
                return false;
            }
            if (globalNPC.VoidTouchTime < maxTime)
            {
                globalNPC.VoidTouchTime += (int)(time * 1.4f);
                if (globalNPC.VoidTouchTime > maxTime)
                {
                    globalNPC.VoidTouchTime = maxTime;
                }
            }
            if (globalNPC.VoidTouchLevel < maxLevel)
            {
                globalNPC.VoidTouchLevel += level / 10f;
                if (globalNPC.VoidTouchLevel > maxLevel)
                {
                    globalNPC.VoidTouchLevel = maxLevel;
                }
            }
            // 减益图标挂 maxTime 帧（CE 原样）；实际结算计时仍以 VoidTouchTime 为准
            npc.AddBuff(ModContent.BuffType<VoidTouch>(), maxTime);
            return true;
        }
        /// <summary>
        /// 玩家侧的虚空侵蚀（只有 PvP 命中玩家才会走到）：与 CE 一致挂本模组自己的
        /// <see cref="VoidTouch"/> 减益 —— 那件 buff 的玩家侧 Update 已按 CE 移植
        /// （每 4 帧扣 3 血 + 速度 ×0.99 + 腐蚀喷尘 + 专属死亡文本）。
        /// </summary>
        public static bool AddVoidTouch(Player player, int time, int level, int maxTime = 600, int maxLevel = 10)
        {
            player.AddBuff(ModContent.BuffType<VoidTouch>(), maxTime);
            return true;
        }
        /// <summary>
        /// 虚空侵蚀的每帧结算（移植自 CE 的 <c>EGlobalNPC.PreAI</c>）：每 20 个游戏更新周期扣一次
        /// <c>26 × 层数 × (1 - 抗性)</c> 血（隐藏原版战斗文本，改在血条处画蓝紫数字；多人客户端再补一次
        /// <c>SendStrikeNPC</c>），非 Boss 每帧减速 4%、Boss 在抗性低于 0.2 时减速 2%，同时撒腐蚀喷尘；
        /// 时间每帧 -1，归零则层数清零。
        /// <para>
        /// 与 CE 的差异：① CE 那段 <c>PRT_Void</c> 粒子被它自己用 <c>&amp;&amp; false</c>
        /// 关掉了（死代码），没有移植；② CE 的 <c>SendExtraAI</c> 里同步这两项计数的代码整段被注释掉，
        /// 也就是说 CE 本身不同步——本模组保持同样行为（各端各自计数）。
        /// （CE 在这里挂的 <c>VoidTouch</c> 减益图标已按同一位置移植，见本方法末尾。）
        /// </para>
        /// </summary>
        public override bool PreAI(NPC npc)
        {
            if (VoidTouchTime > 0)
            {
                if (Main.GameUpdateCount % 20 == 0 && !npc.dontTakeDamage)
                {
                    NPC.HitInfo hit = npc.CalculateHitInfo((int)(26 * VoidTouchLevel * (1 - VoidTouchDR)), 0, false, 0, DamageClass.Generic, false, 0);
                    hit.HideCombatText = true;
                    int damageDone = npc.StrikeNPC(hit, false, false);
                    CombatText.NewText(npc.getRect(), new Color(148, 148, 255), damageDone);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendStrikeNPC(npc, hit);
                    }
                }
                if (npc.boss)
                {
                    if (VoidTouchDR < 0.2f)
                    {
                        npc.velocity *= 0.98f;
                    }
                }
                else
                {
                    npc.velocity *= 0.96f;
                }
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.CorruptSpray, Main.rand.NextFloat() * 2f - 1f, Main.rand.NextFloat() * 2f - 1f);
                VoidTouchTime -= 1;
            }
            if (VoidTouchTime > 0)
            {
                // 只负责显示减益图标；效果全在上面那一段按 VoidTouchTime/Level 结算
                npc.AddBuff(ModContent.BuffType<VoidTouch>(), VoidTouchTime);
            }
            else
            {
                VoidTouchLevel = 0;
            }
            return base.PreAI(npc);
        }
        /// <summary>虚空侵蚀对弹幕命中的增伤：每层 +1%（满层 +10%），按抗性缩放</summary>
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage += VoidTouchLevel * 0.01f * (1 - VoidTouchDR);
        }
        /// <summary>虚空侵蚀对真近战（物品直接命中）的增伤：每层 +5%（满层 +50%），按抗性缩放</summary>
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage += VoidTouchLevel * 0.05f * (1 - VoidTouchDR);
        }
        /// <summary>
        /// 灵魂紊乱的数值效果（移植自 CE 的 <c>SoulDisorderDebuffNPC.ModifyIncomingHit</c>）：
        /// 带该减益的敌怪受击时**额外 +15 护甲穿透**、**最终伤害 ×1.05**。
        /// 用 <c>ModifyIncomingHit</c> 而不是 <c>ModifyHitByProjectile/Item</c>，是为了让弹幕与真近战一视同仁
        /// （CE 也是写在这个钩子里）。
        /// </summary>
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff(ModContent.BuffType<SoulDisorder>()))
            {
                modifiers.ArmorPenetration += 15;
                modifiers.FinalDamage *= 1.05f;
            }
        }
    }
}
