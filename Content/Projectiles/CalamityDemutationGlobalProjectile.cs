using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Projectiles.Healing;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
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
        }
        /// <summary>
        /// tModLoader 的 ModifyHitNPC 钩子：弹幕命中 NPC、伤害结算前调用。
        /// 亵渎之魂水晶的鞭痕 tag 加伤：目标身上带着 ProfanedCrystalWhipDebuff（由水晶鞭命中挂上）时，
        /// 该弹幕的主人若处于水晶态（四态 ≥ Buffs），本次伤害追加 20% 乘算伤害（强化档 40%）。
        /// 口径照抄灾厄 2.2.2 的 ProfanedSoulCrystal.ApplyTagModifyHit，并乘上该弹幕自身的 tag 收益倍率
        /// （ProjectileID.Sets.SummonTagDamageMultiplier，本工程各转化弹幕已按灾厄登记，如长矛 0.6、碎片 0.25）。
        /// 原版这套结算跑在灾厄内部的 SummonTag 管线里，本工程用原版 IsATagBuff 标记 + 本钩子等价实现。
        /// <para>
        /// 噬渊鞭挞的鞭痕 tag 也走本钩子：目标带着 WyrmWhipDebuff（由 YstralynProj 命中挂上）时，
        /// 任意**召唤系**弹幕（含幻影妖龙）打它都追加 90 平伤与 15% 乘算，并有 1/8 概率强制暴击
        /// （口径照抄 CE 的 WhipDebuffNPC.ModifyHitByProj 里 WyrmWhipDebuff 那一段，含"鞭自身命中不参与"的排除）。
        /// </para>
        /// </summary>
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            // 无主弹幕（owner = 255 = Main.maxPlayers）、敌怪弹幕与陷阱弹幕不参与（对齐灾厄对 npcProj / trap 的排除）
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || projectile.npcProj || projectile.trap)
                return;
            if (projectile.DamageType.CountsAsClass(DamageClass.Summon) && !ProjectileID.Sets.IsAWhip[projectile.type] && target.HasBuff(ModContent.BuffType<WyrmWhipDebuff>()))
            {
                float wyrmTagDamageMult = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
                modifiers.FlatBonusDamage += WyrmWhipDebuff.TagDamage * wyrmTagDamageMult;
                modifiers.SourceDamage += WyrmWhipDebuff.TagDamageMul * wyrmTagDamageMult;
                // CE 那边是 1/8 的强制暴击（反射塞 _critOverride），本工程用原版的 modifiers.SetCrit
                if (Main.rand.NextBool(8))
                {
                    modifiers.SetCrit();
                }
            }
            if (!target.HasBuff(ModContent.BuffType<ProfanedCrystalWhipDebuff>()))
                return;
            CalamityDemutationPlayer modPlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.pscState < (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Buffs)
                return;
            bool empowered = modPlayer.pscState == (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Empowered;
            float tagDamageMult = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            modifiers.ScalingBonusDamage += (empowered ? 0.4f : 0.2f) * tagDamageMult;
        }
        /// <summary>
        /// tModLoader 的 OnHitNPC 钩子：弹幕命中 NPC 后调用。
        /// 按玩家套装触发弹幕吸血，两条分支逻辑同构、仅数值不同：
        /// - 女巫套装（silvaSet）：吸血比例 = 0.03 - numHits × 0.015；
        /// - 奥瑞克套装（auricSet）：吸血比例 = 0.05 - numHits × 0.025。
        /// 比例降至 0 即放弃；治疗量 = 弹幕伤害 × 该比例，并从本次弹幕的 lifeSteal 额度中扣除 1.5 倍作为消耗。
        /// 随后在 1200 像素内挑出存活且生命缺口最大的队友，于弹幕位置生成 SilvaOrb / AuricOrb 弹幕
        /// （写入目标玩家索引与治疗量）为其回血。原版 canGhostHeal 限制已被移除，任意敌人均可触发。
        /// </summary>
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 无主弹幕（owner = 255 = Main.maxPlayers）会越界，先做范围校验
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().silvaSet)// && target.canGhostHeal
            {// 解除 target.canGhostHeal 限制：任意敌人都可触发吸血
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
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f,ModContent.ProjectileType<SilvaOrb>(), 0, 0f, projectile.owner, (float)num14, num12);
            }
            else if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().auricSet)// AuricTeslaHelm 同时置 silva/auric 两标记，else if 防双倍吸血
            {// 解除 target.canGhostHeal 限制：任意敌人都可触发吸血
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
        /// 弹幕命中玩家（PvP）时，按攻击者的装备 / 套装 / 身上的 buff 给目标施加效果，
        /// 效果集合与 OnHitNPCWithProj 对齐（不按伤害类型过滤的项一律放在近战早退之前）：
        /// - 神圣之怒 buff（HolyWrath）：120 帧神圣火（现代版）与圣光（经典版）；
        /// - 恶魔残影套装（demonshadeSetBonus）：随机 360/240/120 帧恶魔烈焰；
        /// - omega 蓝胸甲（omegaBlueChestplate）：240 帧 HadopelagicPressure / CrushDepth（原误用 omegaBlueSet，已与 NPC 侧统一）；
        /// - 召唤专属：时滞（statisBlessing 60 帧；statisCurse / statisBeltOfCurses 120 帧 + 暗影焰）与原初暗影焰 300 帧；
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
                if (modPlayer.statisCurse || modPlayer.statisBeltOfCurses)
                {
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "TemporalSadness", 120);
                    target.AddBuff(BuffID.ShadowFlame, 120);
                    CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "TemporalSadness", 120);
                }
                // 原初暗影焰（theFirstShadowflame）：300 帧暗影焰
                if (modPlayer.theFirstShadowflame)
                    target.AddBuff(BuffID.ShadowFlame, 300);
                // 亵渎之魂：召唤类（非鞭）或本水晶体系的转化弹幕命中施加圣焰；水晶态 600 帧、神器态 300 帧
                // （与 OnHitNPCWithProj 的召唤分支对齐；经典版灾厄该 buff 名为 HolyLight）
                if ((projectile.CountsAsClass<SummonDamageClass>() && !projectile.CountsAsClass<SummonMeleeSpeedDamageClass>())
                    || ProfanedSoulCrystal.IsPscProjectile(projectile))
                {
                    int profanedHolyFlameFrames = modPlayer.profanedCrystal ? 600 : modPlayer.profanedSoulArtifact ? 300 : 0;
                    if (profanedHolyFlameFrames > 0)
                    {
                        CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", profanedHolyFlameFrames);
                        CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", profanedHolyFlameFrames);
                    }
                }
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
    }
}
