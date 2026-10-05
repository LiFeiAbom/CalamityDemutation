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
        /// tModLoader 的 OnHitNPC 钩子：弹幕命中 NPC 后调用。
        /// 按玩家套装触发弹幕吸血，两条分支逻辑同构、仅数值不同：
        /// - 女巫套装（silvaSet）：吸血比例 = 0.03 - numHits × 0.015；
        /// - 奥瑞克套装（auricSet）：吸血比例 = 0.05 - numHits × 0.025。
        /// 比例降至 0 即放弃；治疗量 = 弹幕伤害 × 该比例，并从本次弹幕的 lifeSteal 额度中扣除 1.5 倍作为消耗。
        /// 随后在 1200 像素内挑出存活且生命缺口最大的队友，于弹幕位置生成 SilvaOrb / AuricOrb 弹幕
        /// （写入目标玩家索引与治疗量）为其回血。仅当 target.canGhostHeal 为真（该敌人允许吸血）时触发，
        /// 口径对齐经典版灾厄 CalamityGlobalProjectile.cs:346/380。
        /// </summary>
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 无主弹幕（owner = 255 = Main.maxPlayers）会越界，先做范围校验
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().silvaSet && target.canGhostHeal)
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
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f,ModContent.ProjectileType<SilvaOrb>(), 0, 0f, projectile.owner, (float)num14, num12);
            }
            else if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().auricSet && target.canGhostHeal)// AuricTeslaHelm 同时置 silva/auric 两标记，else if 防双倍吸血
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
