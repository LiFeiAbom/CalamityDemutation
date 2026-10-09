using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Projectiles.Healing;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles
{
    /// <summary>
    /// 全局弹幕类：处理元素箭袋的箭矢分裂，以及套装在弹幕命中时的吸血与 debuff 施加
    /// （女巫 / 奥瑞克套装的弹幕吸血，亚利姆徽章 / 元素手套 / omega 蓝套装的 PvP debuff）。
    /// </summary>
    internal class CalamityDemutationGlobalProjectile:GlobalProjectile
    {
        // ── 实例字段 ──
        /// <summary>
        /// 弹幕"原始 extraUpdates"缓存：-1 表示尚未记录。
        /// ProjUtil 需要临时改动 extraUpdates 时会先缓存原值，用完再写回，避免叠加修改后无法还原。
        /// </summary>
        public int defExtraUpdates = -1;
        /// <summary>
        /// 该箭矢是否已被虚无箭袋的虚空场强化过（对应灾厄 CalamityGlobalProjectile 的同名字段）。
        /// 虚空场（<c>VoidFieldGenerator</c>）每帧扫描周围 65 像素内的己方箭矢，
        /// 只强化一次（伤害 ×1.75、<c>extraUpdates + 1</c>），用本标记去重。
        /// </summary>
        public bool nihilicArrow = false;
        /// <summary>
        /// 纳米技术（Nanotech）的潜行打击「+20 护甲穿透」是否已经给这枚弹幕加过。
        /// 照 CI 的写法只在弹幕首次更新时加一次（本类 per-entity，字段随实例走）。
        /// </summary>
        public bool nanotechArmorPenApplied = false;
        // ── 属性 ──
        /// <summary>
        /// 弹幕按实例保存状态，故开启 per-entity
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
        /// 元素箭袋的箭矢分裂效果：
        /// 装备元素箭袋时，友好的箭类弹幕有约 0.5% 概率（每帧 Next(200) 仅返回 199 时触发）
        /// 分裂为两个伤害减半、飞行 60 帧、不掉落物品的镜像箭矢。
        /// 注意：此处的 for 循环条件 i &lt; 1 使循环体仅执行一次（每次触发分裂发射一对镜像弹幕），
        /// 属从原版散射代码复制后未调整循环次数的冗余写法，功能上等价于直接执行一次。
        /// </summary>
        public override void AI(Projectile projectile)
        {
            // 无主弹幕（owner = 255 = Main.maxPlayers）会越界，先做范围校验
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().elementalQuiver && projectile.CountsAsClass<RangedDamageClass>() &&
                projectile.friendly && projectile.arrow)
            {
                if (Main.rand.Next(200) > 198)
                {
                    float spread = 180f * 0.0174f;
                    double startAngle = Math.Atan2(projectile.velocity.X, projectile.velocity.Y) - spread / 2;
                    double deltaAngle = spread / 8f;
                    double offsetAngle;
                    int i;
                    for (i = 0; i < 1; i++)
                    {
                        offsetAngle = (startAngle + deltaAngle * (i + i * i) / 2f) + 32f * i;
                        if (projectile.owner == Main.myPlayer)
                        {
                            int projectile1 = Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, (float)(Math.Sin(offsetAngle) * 8f), (float)(Math.Cos(offsetAngle) * 8f), projectile.type, (int)((double)projectile.damage * 0.5), projectile.knockBack, projectile.owner, 0f, 0f);
                            int projectile2 = Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, (float)(-Math.Sin(offsetAngle) * 8f), (float)(-Math.Cos(offsetAngle) * 8f), projectile.type, (int)((double)projectile.damage * 0.5), projectile.knockBack, projectile.owner, 0f, 0f);
                            Main.projectile[projectile1].timeLeft = 60;
                            Main.projectile[projectile2].timeLeft = 60;
                            Main.projectile[projectile1].noDropItem = true;
                            Main.projectile[projectile2].noDropItem = true;
                        }
                    }
                }
            }
            // 纳米技术（Nanotech）：盗贼弹幕飞行途中每 30 帧在原地留下一枚纳米刀刃。
            // 机制照 CI 的 NanotechOld（用户 2026-10-07 指定）：判据为「友好 + 有伤害 + 非 NPC 弹幕/陷阱 +
            // 算作盗贼弹幕」，节奏用 玩家 miscCounter % 30（numUpdates == 0 保证带 extraUpdates 的弹幕
            // 一帧只判一次——本机 tML 没有 CI 依赖的 FinalExtraUpdate 扩展），生成只在主人端做。
            CalamityDemutationPlayer nanotechPlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (nanotechPlayer.nanotech && projectile.friendly && projectile.damage > 0
                && !projectile.npcProj && !projectile.trap && CDUtil.IsRogueProjectile(projectile))
            {
                // 潜行打击的 +20 护甲穿透：每枚弹幕只加一次（CI 同样只在首次更新时加）
                if (!nanotechArmorPenApplied && CDUtil.IsStealthStrike(projectile, out _))
                {
                    nanotechArmorPenApplied = true;
                    projectile.ArmorPenetration += 20;
                }
                if (projectile.numUpdates == 0 && Main.player[projectile.owner].miscCounter % 30 == 0
                    && projectile.owner == Main.myPlayer)
                {
                    int bladeIndex = Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<Rogue.Nanotech>(), (int)(projectile.damage * 0.15), 0f, projectile.owner);
                    // 伤害类型 = 盗贼（用户指定；刀刃 SetDefaults 里也设了，这里显式再写一次）
                    Main.projectile[bladeIndex].DamageType = CDUtil.GetRogueDamageClass();
                }
            }
        }
        /// <summary>
        /// tModLoader 的 OnHitNPC 钩子：弹幕命中 NPC 后调用。
        /// 按玩家套装触发弹幕吸血，两条分支逻辑同构、仅数值不同：
        /// - 女巫套装（silvaSet）：吸血比例 = 0.03 - numHits × 0.015；
        /// - 奥瑞克套装（auricSet）：吸血比例 = 0.05 - numHits × 0.025。
        /// 比例降至 0 即放弃；治疗量 = 弹幕伤害 × 该比例，并从本次弹幕的 lifeSteal 额度中扣除 1.5 倍作为消耗。
        /// 随后在 1200 像素内挑出存活且生命缺口最大的队友，于弹幕位置生成 SilvaOrb / AuricOrb 弹幕
        /// （写入目标玩家索引与治疗量）为其回血。仅当 target.canGhostHeal 为真（该敌人允许吸血）时触发，
        /// 口径对齐经典版灾厄 CalamityGlobalProjectile.cs:346/380。
        /// 在这两条**经典版**分支之外，另有一条 CI 独有的分支（2026-10-09 用户点名补入）：
        /// 凡置位 silvaSet（＝ CI 的 AuricSilvaSet）者，任意弹幕命中都会再生成一枚固定 rand(5,11) 点的
        /// SilvaOrb（搜队友半径 3000、lifeSteal 消耗倍率 2），口径见 CI 的
        /// CalamityInheritancePlayerOnHit.ProjLifesteal。
        /// 另有一条独立分支：装备魔能谐振仪（manaOverloader）且手持魔法武器时，魔法弹幕命中会按
        /// "伤害 × (0.2 − 已命中次数×0.05) × 当前魔力比例"（单次封顶 10）生成 ManaPolarizerHealOrb。
        /// </summary>
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 无主弹幕（owner = 255 = Main.maxPlayers）会越界，先做范围校验
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;
            // 纳米技术（Nanotech）：潜行打击命中时（这枚弹幕的潜行打击命中数还不到 3 次）从画面上方
            // 砸下 6 枚灾厄本体的「纳米闪光」NanoFlare——机制照 CI 的 NanotechOld，用户 2026-10-07 指定。
            // NanoFlare 走软依赖按名取（经典版灾厄没有这件，取不到就整段跳过）；潜行打击状态由 CDUtil
            // 反射读灾厄 GlobalProjectile 的 stealthStrike，经典版恒为 false。本块不写提前 return。
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().nanotech
                && projectile.owner == Main.myPlayer
                && CDUtil.IsStealthStrike(projectile, out int nanotechStealthHits) && nanotechStealthHits < 3
                && ModContent.TryFind<ModProjectile>("CalamityMod", "NanoFlare", out ModProjectile nanoFlare))
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 flareSpawn = new Vector2(target.Center.X + Main.rand.Next(-201, 201), Main.screenPosition.Y - 600f - Main.rand.Next(50));
                    Vector2 flareVelocity = (target.Center - flareSpawn) / 40f;
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), flareSpawn, flareVelocity, nanoFlare.Type,
                        (int)(projectile.damage * 0.05), 3f, projectile.owner);
                }
            }
            // 魔能谐振仪（manaOverloader）：手持魔法武器时，魔法弹幕命中敌人按其伤害与当前魔力比例吸血
            // （源 CalamityPlayerOnHit 的 manaOverloader 分支）。它与下面的套装吸血相互独立（源里两者不在同一条
            // else 链上），故本块刻意不写提前 return——否则一旦这里的治疗量算成 0，会把套装吸血一起跳过。
            CalamityDemutationPlayer manaPolarizerPlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (manaPolarizerPlayer.manaOverloader && projectile.CountsAsClass<MagicDamageClass>() && Main.player[projectile.owner].HeldItem.CountsAsClass<MagicDamageClass>())
            {
                float manaRatio = Main.player[projectile.owner].statMana / (float)Main.player[projectile.owner].statManaMax2;
                float manaHealMult = 0.2f - projectile.numHits * 0.05f;
                float manaHeal = projectile.damage * manaHealMult * manaRatio;
                if (manaHeal > 10f)
                    manaHeal = 10f;   // 源用 CalamityMod.lifeStealCap = 10
                // lifeSteal 预算的判断与本文件里 silvaSet / auricSet 两条分支口径一致
                // （现代版灾厄 2.0 之后已把这道判据挪进了 SpawnLifeStealProjectile）
                if (manaHealMult > 0f && (int)manaHeal > 0 && Main.LocalPlayer.lifeSteal > 0f)
                {
                    Main.LocalPlayer.lifeSteal -= manaHeal * 3f;   // 源 SpawnLifeStealProjectile 的 cooldownMultiplier = 3
                    float num13 = 0f;
                    int num14 = projectile.owner;
                    for (int i = 0; i < 255; i++)
                    {
                        if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                        {
                            float num15 = Math.Abs(Main.player[i].position.X + (float)(Main.player[i].width / 2) - projectile.position.X + (float)(projectile.width / 2)) + Math.Abs(Main.player[i].position.Y + (float)(Main.player[i].height / 2) - projectile.position.Y + (float)(projectile.height / 2));
                            if (num15 < 1200f && (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife) > num13)
                            {
                                num13 = (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife);
                                num14 = i;
                            }
                        }
                    }
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<ManaPolarizerHealOrb>(), 0, 0f, projectile.owner, (float)num14, manaHeal);
                }
            }
            // ⚠️ 这两条吸血分支**绝不能用 return 提前退出**：本方法后面还有一票套装效果
            // （龙蒿盗贼计数、血炎盗贼、林海盗贼的 1.25 倍、龙蒿法师暴击计数与回血、血炎法师火焰爆炸、
            //  弑神者法师烈焰/治疗球、林海法师巨型爆炸、弑神者召唤幻影——即"金源头复合四套"的全部内容），
            //  提前 return 会把它们一并跳过。金源套同时置 silvaSet 与 auricSet，必进这两条分支，
            //  一旦 lifeSteal 额度耗尽就会整个方法返回 —— 这正是"穿龙蒿有叶风暴、穿金源没有"的原因。
            //  源（经典版）把后面这些效果写在玩家侧 CalamityPlayerPreTrailer.ModifyHitNPCWithProj、
            //  CalamityGlobalItem.Shoot 等钩子里，天然不受这两处 return 影响；本工程把它们集中到了本方法，
            //  故必须把"放弃"改写成嵌套 if（判据与顺序与原三条 return 完全等价）。
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().silvaSet && target.canGhostHeal)
            {
                float num11 = 0.03f;
                num11 -= (float)projectile.numHits * 0.015f;
                float num12 = (float)projectile.damage * num11;
                if (num11 > 0f && (int)num12 > 0 && Main.LocalPlayer.lifeSteal > 0f)
                {
                    Main.LocalPlayer.lifeSteal -= num12 * 1.5f;
                    float num13 = 0f;
                    int num14 = projectile.owner;
                    for (int i = 0; i < 255; i++)
                    {
                        if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                        {
                            float num15 = Math.Abs(Main.player[i].position.X + (float)(Main.player[i].width / 2) - projectile.position.X + (float)(projectile.width / 2)) + Math.Abs(Main.player[i].position.Y + (float)(Main.player[i].height / 2) - projectile.position.Y + (float)(projectile.height / 2));
                            if (num15 < 1200f && (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife) > num13)
                            {
                                num13 = (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife);
                                num14 = i;
                            }
                        }
                    }
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f,ModContent.ProjectileType<SilvaOrb>(), 0, 0f, projectile.owner, (float)num14, num12);
                }
            }
            else if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().auricSet && target.canGhostHeal)// AuricTeslaHelm 同时置 silva/auric 两标记，else if 防双倍吸血
            {
                float num11 = 0.05f;
                num11 -= (float)projectile.numHits * 0.025f;
                float num12 = (float)projectile.damage * num11;
                if (num11 > 0f && (int)num12 > 0 && Main.LocalPlayer.lifeSteal > 0f)
                {
                    Main.LocalPlayer.lifeSteal -= num12 * 1.5f;
                    float num13 = 0f;
                    int num14 = projectile.owner;
                    for (int i = 0; i < 255; i++)
                    {
                        if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                        {
                            float num15 = Math.Abs(Main.player[i].position.X + (float)(Main.player[i].width / 2) - projectile.position.X + (float)(projectile.width / 2)) + Math.Abs(Main.player[i].position.Y + (float)(Main.player[i].height / 2) - projectile.position.Y + (float)(projectile.height / 2));
                            if (num15 < 1200f && (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife) > num13)
                            {
                                num13 = (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife);
                                num14 = i;
                            }
                        }
                    }
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<AuricOrb>(), 0, 0f, projectile.owner, (float)num14, num12);
                }
            }
            // CI 的 AuricSilvaSet 那条「任意弹幕命中敌人即生成治疗叶球」：
            // 本工程的 silvaSet 就是 CI 的 AuricSilvaSet（林海五颗头 + 金源五颗头的套装方法都会置位它），
            // 口径照 CI 的 CalamityInheritancePlayerOnHit.ProjLifesteal——回**固定** rand(5, 11) 点、
            // 走 SilvaOrb、搜队友半径 3000、生命值消耗倍率 2（SpawnLifeStealProjectile 的第 6 参）。
            // 用户 2026-10-09 指定把这条 CI 独有的机制补进来：它与上面那条经典版「按伤害比例递减」的吸血
            // 是**并列**的两条（CI 也把它写成独立一条），两者共用 same 一个 lifeSteal 额度，故总量仍受闸门约束。
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().silvaSet && target.canGhostHeal)
            {
                int silvaHealAmount = Main.rand.Next(5, 11);
                if (Main.LocalPlayer.lifeSteal > 0f)
                {
                    Main.LocalPlayer.lifeSteal -= silvaHealAmount * 2f;
                    float silvaWorstMissing = 0f;
                    int silvaHealTarget = projectile.owner;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                        {
                            float manhattan = Math.Abs(Main.player[i].position.X + Main.player[i].width / 2 - projectile.position.X + projectile.width / 2)
                                + Math.Abs(Main.player[i].position.Y + Main.player[i].height / 2 - projectile.position.Y + projectile.height / 2);
                            if (manhattan < 3000f && Main.player[i].statLifeMax2 - Main.player[i].statLife > silvaWorstMissing)
                            {
                                silvaWorstMissing = Main.player[i].statLifeMax2 - Main.player[i].statLife;
                                silvaHealTarget = i;
                            }
                        }
                    }
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f,
                        ModContent.ProjectileType<SilvaOrb>(), 0, 0f, projectile.owner, (float)silvaHealTarget, (float)silvaHealAmount);
                }
            }
            // 龙蒿盗贼套装（tarraThrowing）的「每 25 次盗贼暴击」计数
            //（照经典版 CalamityPlayerPreTrailer.cs:6158：要求暴击 + 弹幕算盗贼弹幕，且冷却归零、未满 25；
            //  判定方式见 CDUtil.IsRogueProjectile——现代版按 RogueDamageClass，经典版读它自己的
            //  CalamityGlobalProjectile.rogue，因为经典盗贼弹幕不带 DamageType）。
            CalamityDemutationPlayer roguePlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (roguePlayer.tarraThrowing && roguePlayer.tarraThrowingCritTimer <= 0 && roguePlayer.tarraThrowingCrits < 25
                && hit.Crit && CDUtil.IsRogueProjectile(projectile))
            {
                roguePlayer.tarraThrowingCrits++;
            }
            // 血炎盗贼套装（bloodflareThrowing）的「盗贼暴击 50% 几率治疗你」
            //（照经典版 CalamityPlayerPreTrailer.cs:6226-6246）：
            // 要求暴击 + 弹幕算盗贼弹幕 + 骰中 50%，且目标允许吸血（canGhostHeal）。
            // **注意源只回 1 点生命**——算出来的数值只是拿去扣 lifeSteal 额度（×2），并不是治疗量，本工程照源。
            CalamityDemutationPlayer bloodThrowing = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (bloodThrowing.bloodflareThrowing && hit.Crit && Main.rand.Next(2) == 0
                && CDUtil.IsRogueProjectile(projectile) && target.canGhostHeal)
            {
                float healCostMult = 0.03f - (float)projectile.numHits * 0.015f;
                if (healCostMult < 0f)
                    healCostMult = 0f;
                float lifeStealCost = (float)projectile.damage * healCostMult;
                if (lifeStealCost < 0f)
                    lifeStealCost = 0f;
                Player bloodOwner = Main.player[projectile.owner];
                if (bloodOwner.lifeSteal > 0f)
                {
                    bloodOwner.statLife += 1;
                    bloodOwner.HealEffect(1);
                    bloodOwner.lifeSteal -= lifeStealCost * 2f;
                }
            }
            // 始源林海盗贼头（SilvaMask）在经典版里还带一条**隐藏**加成（不在 tooltip 内，被 auricSet 门控）：
            // `if (auricSet) { if (silvaThrowing && 盗贼弹幕 && hit.Crit && 生命 >50%) hit.Damage *= 1.25; }`
            //（CalamityPlayerPreTrailer.cs:6136-6144）—— 源如此：单穿始源林海套不生效，得配上金源套才吃，本工程照源。
            // 实现差异：源是在命中**结算前**把 hit.Damage ×1.25；tML 的 GlobalProjectile.OnHitNPC 拿到的 HitInfo
            // 是按值传的、改不动已结算伤害，故改为**事后补打 25% 的实伤**（对线性缩放的最终伤害等效）。
            CalamityDemutationPlayer silvaThrowingPlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (silvaThrowingPlayer.auricSet && silvaThrowingPlayer.silvaThrowing && hit.Crit
                && Main.player[projectile.owner].statLife > (int)(Main.player[projectile.owner].statLifeMax2 * 0.5)
                && CDUtil.IsRogueProjectile(projectile))
            {
                int bonusDamage = (int)(damageDone * 0.25f);
                if (bonusDamage > 0)
                {
                    Main.player[projectile.owner].ApplyDamageToNPC(target, bonusDamage, 0f, 0, false, CDUtil.GetRogueDamageClass());
                }
            }
            // 龙蒿法师套装（tarraMage）的两件事（口径照经典版 CalamityGlobalProjectile.cs:440-462）：
            // ① 统计魔法暴击次数，满 5 由 CalamityDemutationGlobalItem.Shoot 喷出叶暴风；
            // ② 命中时按弹幕伤害回血：比例 = 0.03 - numHits × 0.015，剂量 = 弹幕伤害 ÷50（穿金源时 ÷100），
            //    90 帧冷却，且要求本机玩家的 lifeSteal 额度 > 0（与上面两条吸血同源的闸门）。
            //    注：源 tooltip 写「50% 几率」，但经典版实现里只有冷却、没有随机骰——本工程照源实现、保留原文案。
            CalamityDemutationPlayer magePlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (magePlayer.tarraMage)
            {
                if (hit.Crit && projectile.CountsAsClass<MagicDamageClass>())
                {
                    magePlayer.tarraCrits++;
                }
                if (target.canGhostHeal && magePlayer.tarraMageHealCooldown <= 0)
                {
                    magePlayer.tarraMageHealCooldown = 90;
                    float healMult = 0.03f - (float)projectile.numHits * 0.015f;
                    if (healMult > 0f)
                    {
                        float lifeStealCost = (float)projectile.damage * healMult;
                        if ((int)lifeStealCost > 0 && Main.LocalPlayer.lifeSteal > 0f)
                        {
                            Main.LocalPlayer.lifeSteal -= lifeStealCost * 1.5f;
                            int healAmount = magePlayer.auricSet ? projectile.damage / 100 : projectile.damage / 50;
                            Player healTarget = Main.player[projectile.owner];
                            healTarget.statLife += healAmount;
                            healTarget.HealEffect(healAmount);
                            if (healTarget.statLife > healTarget.statLifeMax2)
                            {
                                healTarget.statLife = healTarget.statLifeMax2;
                            }
                        }
                    }
                }
            }
            // 血炎法师套装（bloodflareMage）的「魔法暴击每 2 秒引发一次火焰爆炸」：
            // 源写在玩家侧命中钩子里（CalamityPlayerPreTrailer.cs:6249），本工程挪到弹幕侧——
            // hit.Crit 同源、同样只在主人端跑。在目标中心朝随机方向喷 3 枚原版火球
            //（ProjectileID.BallofFire，源里裸数字 15 已 Cecil 反查），伤害 = 本次弹幕伤害 ×0.5，
            // 冷却 120 帧（2 秒）。
            CalamityDemutationPlayer bloodMage = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (bloodMage.bloodflareMage && bloodMage.bloodflareMageCooldown <= 0 && hit.Crit && projectile.CountsAsClass<MagicDamageClass>())
            {
                bloodMage.bloodflareMageCooldown = 120;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 fireVel = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                    while (fireVel.X == 0f && fireVel.Y == 0f)
                    {
                        fireVel = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                    }
                    fireVel.Normalize();
                    fireVel *= Main.rand.Next(70, 101) * 0.1f;
                    int fire = Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center, fireVel, ProjectileID.BallofFire, (int)(projectile.damage * 0.5f), 0f, projectile.owner);
                    Main.projectile[fire].netUpdate = true;
                }
            }
            // 弑神者法师套装（godSlayerMage）的「魔法攻击命中敌人时释放弑神者烈焰与治疗烈焰」：
            // 用户 2026-10-09 指定换成 CI 口径（CI 的 ClPlayerClassSpecific.MagicOnHit ＋
            // CalamityInheritancePlayerOnHit.ProjLifesteal）：
            // ① 弑神火：闸门换成 CI 的 fireCD（命中即置 2 → 最快每 2 帧一枚），往**随机方向**以
            //    rand(12, 16) 的初速射一枚 GodSlayerOrb，伤害 =（400 + 手持武器伤害 ÷ 2）× 5；
            //   CI 不挑目标——那枚光球自己会追 3000 像素内的敌人，本工程的 GodSlayerOrb 同样自寻敌
            //   （不读 ai[0]），故这里不写目标索引。
            // ② 治疗烈焰：改为**固定** rand(5, 11) 点（不再按弹幕伤害的比例），走 GodSlayerHealOrb、
            //    搜队友半径 3000（原 1200）、生命值消耗倍率 2（源 SpawnLifeStealProjectile 的第 6 参）。
            // 注：经典版那套「按本次命中伤害折半累加、每帧衰减 2.5」的 godSlayerDmg 预算现在只归召唤侧使用；
            //  CI 写的是裸 <c>HeldItem.damage</c>，本工程照抄（不走 GetWeaponDamage，故不随数值膨胀开关变化）。
            CalamityDemutationPlayer godMage = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (projectile.CountsAsClass<MagicDamageClass>() && godMage.godSlayerMage && godMage.godSlayerMageFireCD <= 0)
            {
                godMage.godSlayerMageFireCD = 2;
                int fireDamage = 400 + Main.player[projectile.owner].HeldItem.damage / 2;
                float fireAngle = Main.rand.NextFloat() * MathHelper.TwoPi;
                Vector2 fireVelocity = new Vector2((float)Math.Cos(fireAngle), (float)Math.Sin(fireAngle))
                    * Main.rand.NextFloat(12f, 16f);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, fireVelocity,
                    ModContent.ProjectileType<GodSlayerOrb>(), fireDamage * 5, projectile.knockBack, projectile.owner);
                if (target.canGhostHeal)
                {
                    int healAmount = Main.rand.Next(5, 11);
                    if (Main.LocalPlayer.lifeSteal > 0f)
                    {
                        Main.LocalPlayer.lifeSteal -= healAmount * 2f;
                        float worstMissing = 0f;
                        int healTarget = projectile.owner;
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                            {
                                float manhattan = Math.Abs(Main.player[i].position.X + Main.player[i].width / 2 - projectile.position.X + projectile.width / 2)
                                    + Math.Abs(Main.player[i].position.Y + Main.player[i].height / 2 - projectile.position.Y + projectile.height / 2);
                                if (manhattan < 3000f && Main.player[i].statLifeMax2 - Main.player[i].statLife > worstMissing)
                                {
                                    worstMissing = Main.player[i].statLifeMax2 - Main.player[i].statLife;
                                    healTarget = i;
                                }
                            }
                        }
                        Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f,
                            ModContent.ProjectileType<GodSlayerHealOrb>(), 0, 0f, projectile.owner, healTarget, (float)healAmount);
                    }
                }
            }
            // 始源林海法师套装（silvaMage）的「魔法弹幕引发巨型爆炸」：
            // 用户 2026-10-09 指定换成 CI 的 SilvaMagicSetLegacy 口径（CI 的 ClPlayerClassSpecific.MagicOnHit）：
            // 触发条件是「弹幕只穿透一名敌人（penetrate == 1）**或**即将消散（timeLeft <= 5）」时 **100%** 触发，
            // 触发后置 300 帧冷却；效果 = 播 SoundID.Zombie103，并在弹幕位置生成一枚**独立**的 SilvaBurst
            // （96×96 隐形判定，伤害 = 800 + 0.6 × 本弹幕伤害）。
            // 与经典版（也是工程原先的写法）的三点不同：① 3% 概率 → 100% + 300 帧冷却；
            // ② 撑大本弹幕判定框并把本次伤害 ×4（金源 ×7）→ 改为生成独立弹幕；
            // ③ 判据从"只认 penetrate == 1"放宽到"或即将消散"。
            // 注：CI 的 ApplyArmorAccDamageBonusesTo 在本工程恒等（没有 Old Fashioned）故省略，与盾冲那处同一口径。
            CalamityDemutationPlayer silvaMagePlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (projectile.CountsAsClass<MagicDamageClass>() && silvaMagePlayer.silvaMage
                && silvaMagePlayer.silvaMageBurstCooldown <= 0
                && (projectile.penetrate == 1 || projectile.timeLeft <= 5))
            {
                silvaMagePlayer.silvaMageBurstCooldown = 300;
                SoundEngine.PlaySound(SoundID.Zombie103, projectile.Center);
                int silvaBurstDamage = (int)(800.0 + 0.6 * projectile.damage);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<Magic.SilvaBurst>(), silvaBurstDamage, 8f, projectile.owner);
            }
            // 弑神者召唤套装（godSlayerSummon）：召唤物 / 哨兵命中敌人时，若节流预算归零就召出一枚弑神幻影
            //（经典版 CalamityGlobalProjectile.cs:848 起；条件同样只认 minion / sentry，不含鞭类弹幕）。
            // 源里那段"挑一个 800 像素内的敌怪"选出的索引其实从未被使用——幻影固定生成在弹幕自身位置、方向纯随机，
            // 所以这里只保留其真实作用（附近存在可追击的敌人就放行）。
            CalamityDemutationPlayer summonPlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if ((projectile.minion || projectile.sentry) && summonPlayer.godSlayerSummon && summonPlayer.godSlayerDmg <= 0f)
            {
                int phantomDamage = projectile.damage / 2;
                float phantomAi = Main.rand.NextFloat() + 0.5f;
                summonPlayer.godSlayerDmg += phantomDamage;
                bool hasNearbyTarget = false;
                for (int i = 0; i < 200; i++)
                {
                    if (!Main.npc[i].CanBeChasedBy(projectile, false))
                        continue;
                    float manhattan = Math.Abs(Main.npc[i].position.X + Main.npc[i].width / 2 - projectile.position.X + projectile.width / 2)
                        + Math.Abs(Main.npc[i].position.Y + Main.npc[i].height / 2 - projectile.position.Y + projectile.height / 2);
                    if (manhattan < 800f)
                    {
                        hasNearbyTarget = true;
                        break;
                    }
                }
                // 源在此处会 return（放弃整个 OnHitNPC 余下逻辑）；本工程只跳过生成，避免吞掉后面的效果
                if (hasNearbyTarget)
                {
                    const float phantomSpeed = 15f;
                    float velX = Main.rand.Next(-100, 101);
                    float velY = Main.rand.Next(-100, 101);
                    float length = (float)Math.Sqrt(velX * velX + velY * velY);
                    length = phantomSpeed / length;
                    velX *= length;
                    velY *= length;
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, velX, velY,
                        ModContent.ProjectileType<GodSlayerPhantom>(), phantomDamage * 2, 0f, projectile.owner, 0f, phantomAi);
                }
            }
        }
        /// <summary>
        /// PvP：弹幕命中玩家时的**伤害修正**，与 PvE 侧 <c>CalamityDemutationPlayer.ModifyHitNPCWithProj</c> 对齐——
        /// 狂怒 buff ×2.25；金源套 + 女巫近战的近战弹幕按当前生命比例追加至多 +0.2 倍；
        /// 女巫射手套装免死窗口内远程伤害 +0.4。
        /// <para>
        /// **未镜像**弑神者射手套装的「再次暴击」：它靠 <c>HitModifiers.CritDamage</c> 实现，而
        /// <c>Player.HurtModifiers</c> 根本没有 CritDamage 字段（玩家受伤不吃暴击伤害倍率），机制上无法等价表达。
        /// </para>
        /// </summary>
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;
            Player attacker = Main.player[projectile.owner];
            CalamityDemutationPlayer modPlayer = attacker.GetModPlayer<CalamityDemutationPlayer>();
            float damageMult = 1f;
            if (modPlayer.enraged)
                damageMult += 1.25f;
            if (modPlayer.auricSet && modPlayer.silvaMelee && projectile.CountsAsClass<MeleeDamageClass>())
                damageMult += (float)((double)attacker.statLife / attacker.statLifeMax2) * 0.2f;
            if (modPlayer.silvaRanged && modPlayer.silvaCountdown > 0 && modPlayer.hasSilvaEffect && projectile.CountsAsClass<RangedDamageClass>())
                damageMult += 0.4f;
            modifiers.FinalDamage *= damageMult;
        }
        /// <summary>
        /// 弹幕命中玩家（PvP）时，按攻击者的装备 / 套装 / 身上的 buff 给目标施加效果，
        /// 效果集合与 OnHitNPCWithProj 对齐（不按伤害类型过滤的项一律放在近战早退之前）：
        /// - 神圣之怒 buff（HolyWrath）：120 帧神圣火（现代版）与圣光（经典版）；
        /// - 恶魔残影套装（demonshadeSetBonus）：随机 360/240/120 帧恶魔烈焰；
        /// - omega 蓝胸甲（omegaBlueChestplate）：240 帧 HadopelagicPressure / CrushDepth（原误用 omegaBlueSet，已与 NPC 侧统一）；
        /// - 召唤专属：时滞（statisBlessing 与 statisCurse 各 60 帧、statisBeltOfCurses 120 帧，均附暗影焰）与原初暗影焰 300 帧；
        /// - 亚利姆徽章：随机 120/240/360 帧神圣火（现代版）与圣光（经典版）；
        /// - 元素手套：全套元素 debuff 各 120 帧（原版五毒 + 灾厄现代/经典两版本）；
        /// - 女巫套装近战弹幕（silvaMelee）：1/4 概率 20 帧女巫眩晕；
        /// - 血焰套装（bloodflareMelee）/ 弑神近战（godSlayerMelee）：作用在攻击者自身（累计命中回血 / 生成弑神飞镖）；
        /// - 女巫套装（silvaSet）/ 奥瑞克套装（auricSet）：与 OnHitNPC 同构的吸血逻辑
        ///   （比例 0.03 / 0.05 起，随弹幕命中次数递减，降至 0 即放弃）。
        /// </summary>
        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            // 无主弹幕（owner = 255 = Main.maxPlayers）会越界，先做范围校验
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;
            // 攻击者 = 弹幕主人 A（直接查其实际装备，不依赖 ModPlayer 标志位）
            Player attacker = Main.player[projectile.owner];
            CalamityDemutationPlayer modPlayer = attacker.GetModPlayer<CalamityDemutationPlayer>();
            // ── 不按伤害类型过滤的效果（与 OnHitNPCWithProj 一致），必须在下面近战早退之前判定 ──
            // 神圣之怒（HolyWrath）：120 帧神圣火（现代版）与圣光（经典版）。
            // holyWrath 由 HolyWrath buff 置位（非饰品），故查攻击者的 buff 列表（跨端同步），不用 IsAccessoryEquipped
            if (attacker.HasBuff(ModContent.BuffType<HolyWrath>()))
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
            }
            // 恶魔残影套装（demonshadeSetBonus）：随机 360/240/120 帧恶魔烈焰
            if (modPlayer.demonshadeSetBonus)
            {
                if (Main.rand.NextBool(4))
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 360, false);
                else if (Main.rand.NextBool(2))
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 240, false);
                else
                    target.AddBuff(ModContent.BuffType<DemonFlames>(), 120, false);
            }
            // omega 蓝胸甲（omegaBlueChestplate）：240 帧 HadopelagicPressure / CrushDepth。
            // 判定口径与 NPC 侧统一（原先误用套装标记 omegaBlueSet）
            if (modPlayer.omegaBlueChestplate)
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HadopelagicPressure", 240);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 240);
            }
            // ── 召唤专属（鞭类弹幕）：与 OnHitNPCWithProj 的召唤分支对齐 ──
            if (projectile.CountsAsClass<SummonDamageClass>() || projectile.CountsAsClass<SummonMeleeSpeedDamageClass>())
            {
                // 时滞（TemporalSadness）：现代 / 经典两灾厄同名，缺名时静默跳过
                if (modPlayer.statisBlessing)
                {
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 60);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 60);
                }
                if (modPlayer.statisCurse)
                {
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 60);
                    target.AddBuff(BuffID.ShadowFlame, 300);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 60);
                }
                if (modPlayer.statisBeltOfCurses)
                {
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 120);
                    target.AddBuff(BuffID.ShadowFlame, 300);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 120);
                }
                // 原初暗影焰（theFirstShadowflame）：300 帧暗影焰
                if (modPlayer.theFirstShadowflame)
                    target.AddBuff(BuffID.ShadowFlame, 300);
            }
            // ── 以下为近战专属：用 if 包裹而非提前 return，避免误挡下面的套装吸血（吸血不按伤害类型过滤）──
            bool isMelee = projectile.CountsAsClass<MeleeDamageClass>() || projectile.CountsAsClass<MeleeNoSpeedDamageClass>();
            if (isMelee)
            {
                // 攻击者装备亚利姆徽章：随机 120/240/360 帧施加神圣火（现代版）与圣光（经典版）
                if (CalamityDemutationPlayer.IsAccessoryEquipped(attacker, ModContent.ItemType<YharimsInsignia>()))
                {
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", Main.rand.NextBool(4) ? 360 : Main.rand.NextBool(2) ? 240 : 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", Main.rand.NextBool(4) ? 360 : Main.rand.NextBool(2) ? 240 : 120);
                }
                // 攻击者装备元素手套：施加全套元素 debuff（原版五毒 + 灾厄现代/经典两版本）各 120 帧
                if (CalamityDemutationPlayer.IsAccessoryEquipped(attacker, ModContent.ItemType<ElementalGauntlet>()))
                {
                    target.AddBuff(BuffID.Poisoned, 120, false);
                    target.AddBuff(BuffID.OnFire, 120, false);
                    target.AddBuff(BuffID.CursedInferno, 120, false);
                    target.AddBuff(BuffID.Frostburn, 120, false);
                    target.AddBuff(BuffID.Ichor, 120, false);
                    target.AddBuff(BuffID.Venom, 120, false);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Voidfrost", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Nightwither", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Plague", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "SulphuricPoisoning", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "AbyssalFlames", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", 120);
                    if (Main.rand.NextBool(5))
                        CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GlacialState", 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 120);
                }
                // 女巫套装近战弹幕（silvaMelee）：1/4 概率施加 20 帧女巫眩晕
                if (projectile.CountsAsClass<MeleeDamageClass>() && modPlayer.silvaMelee && Main.rand.NextBool(4))
                    target.AddBuff(ModContent.BuffType<SilvaHysteresis>(), 20);
                // 血焰套装（bloodflareMelee）：累计命中数并在本端小额回血（与 OnHitNPCWithProj 同构，仅近战连击弹幕）
                if (modPlayer.bloodflareMelee && projectile.CountsAsClass<MeleeNoSpeedDamageClass>())
                {
                    if (modPlayer.bloodflareMeleeHits < 15 && modPlayer.bloodflareFrenzyTimer <= 0 && modPlayer.bloodflareFrenzyCooldown <= 0)
                        modPlayer.bloodflareMeleeHits++;
                    if (attacker.whoAmI == Main.myPlayer)
                    {
                        int healAmount = Main.rand.Next(3) + 1;
                        attacker.statLife += healAmount;
                        attacker.HealEffect(healAmount);
                        if (attacker.statLife > attacker.statLifeMax2)
                            attacker.statLife = attacker.statLifeMax2;
                    }
                }
                // 弑神近战（godSlayerMelee）：生成弑神飞镖（与 OnHitNPCWithProj 同构）
                if (modPlayer.godSlayerMelee && modPlayer.godSlayerMeleefireCD <= 0
                    && (projectile.CountsAsClass<MeleeDamageClass>() || projectile.CountsAsClass<MeleeNoSpeedDamageClass>()))
                {
                    int finalDamage = 500 + attacker.HeldItem.damage / 2;
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), attacker.Center, CDUtil.GiveVelocity(200f) * 4f
                        , ModContent.ProjectileType<GodSlayerDart>(), finalDamage, 0f, attacker.whoAmI);
                    modPlayer.godSlayerMeleefireCD = 60;
                }
                if (modPlayer.armorShattering || modPlayer.armorCrumbling)
                {
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ArmorCrunch", 240);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "ArmorCrunch", 240);
                }
            }
            // 女巫套装（silvaSet）：弹幕吸血，比例 0.03 起随命中次数递减
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().silvaSet)
            {
                float num11 = 0.03f;
                num11 -= (float)projectile.numHits * 0.015f;
                if (num11 <= 0f)
                {
                    return;
                }
                float num12 = (float)projectile.damage * num11;
                if ((int)num12 <= 0)
                {
                    return;
                }
                if (Main.LocalPlayer.lifeSteal <= 0f)
                {
                    return;
                }
                Main.LocalPlayer.lifeSteal -= num12 * 1.5f;
                float num13 = 0f;
                int num14 = projectile.owner;
                for (int i = 0; i < 255; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                    {
                        float num15 = Math.Abs(Main.player[i].position.X + (float)(Main.player[i].width / 2) - projectile.position.X + (float)(projectile.width / 2)) + Math.Abs(Main.player[i].position.Y + (float)(Main.player[i].height / 2) - projectile.position.Y + (float)(projectile.height / 2));
                        if (num15 < 1200f && (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife) > num13)
                        {
                            num13 = (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife);
                            num14 = i;
                        }
                    }
                }
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<SilvaOrb>(), 0, 0f, projectile.owner, (float)num14, num12);
            }
            // 奥瑞克套装（auricSet）：弹幕吸血，比例 0.05 起随命中次数递减（与 silvaSet 互斥：AuricTeslaHelm 同时置两标记，else if 防双倍吸血）
            else if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().auricSet)
            {
                float num11 = 0.05f;
                num11 -= (float)projectile.numHits * 0.025f;
                if (num11 <= 0f)
                {
                    return;
                }
                float num12 = (float)projectile.damage * num11;
                if ((int)num12 <= 0)
                {
                    return;
                }
                if (Main.LocalPlayer.lifeSteal <= 0f)
                {
                    return;
                }
                Main.LocalPlayer.lifeSteal -= num12 * 1.5f;
                float num13 = 0f;
                int num14 = projectile.owner;
                for (int i = 0; i < 255; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                    {
                        float num15 = Math.Abs(Main.player[i].position.X + (float)(Main.player[i].width / 2) - projectile.position.X + (float)(projectile.width / 2)) + Math.Abs(Main.player[i].position.Y + (float)(Main.player[i].height / 2) - projectile.position.Y + (float)(projectile.height / 2));
                        if (num15 < 1200f && (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife) > num13)
                        {
                            num13 = (float)(Main.player[i].statLifeMax2 - Main.player[i].statLife);
                            num14 = i;
                        }
                    }
                }
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<AuricOrb>(), 0, 0f, projectile.owner, (float)num14, num12);
            }
        }
        /// <summary>
        /// tModLoader 的 OnKill 钩子：弹幕消失时调用。
        /// 龙蒿射手套装（tarraRanged）的经典版口径：远程弹幕消失时 12% 概率在残骸处分裂出 2~3 枚
        /// 生命能量 TarraEnergy，每枚伤害 = min(本弹幕伤害 × 0.33, 65)（源在 CalamityGlobalProjectile.OnKill 里）。
        /// 与命中回调不同，OnKill 在**每个端**都会跑，故必须限定只在弹幕主人的本机生成，
        /// 否则多人下每位玩家都会替别人各分裂一份。
        /// </summary>
        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || projectile.owner != Main.myPlayer)
                return;
            if (!projectile.friendly || !projectile.CountsAsClass<RangedDamageClass>())
                return;
            if (!Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().tarraRanged)
                return;
            if (Main.rand.Next(0, 100) >= 12)
                return;
            int energyCount = Main.rand.Next(2, 4);
            for (int i = 0; i < energyCount; i++)
            {
                Vector2 energyVelocity = Main.rand.NextVector2Unit() * (Main.rand.Next(70, 101) * 0.1f);
                int energyDamage = (int)(projectile.damage * 0.33);
                if (energyDamage > 65)
                    energyDamage = 65;
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.oldPosition + new Vector2(projectile.width / 2f, projectile.height / 2f), energyVelocity, ModContent.ProjectileType<TarraEnergy>(), energyDamage, 0f, projectile.owner);
            }
        }
    }
}
