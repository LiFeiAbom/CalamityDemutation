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
            // 口径照经典版 CalamityGlobalProjectile.cs:659-726。节流预算 godSlayerDmg 与召唤侧的弑神幻影共用——
            // 每触发一次按"本次命中伤害的一半"累加，随即在它衰减到 0 之前不再触发（每帧 -2.5）。
            // ① 在 800 像素内挑一个敌人（优先有视线且距离 > 50 的），朝它射一枚 GodSlayerOrb
            //    （伤害 = 半伤 ×1.5，穿金源 ×2.0，ai[0] = 目标索引）；
            // ② 若该敌人允许吸血（canGhostHeal），再补一枚 GodSlayerHealOrb 飞向 1200 像素内血亏最多的队友
            //    （治疗比例 = 0.06，穿金源 0.03，随 numHits 每层再 -0.015）。
            CalamityDemutationPlayer godMage = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (projectile.CountsAsClass<MagicDamageClass>() && godMage.godSlayerMage && godMage.godSlayerDmg <= 0f)
            {
                int orbBase = projectile.damage / 2;
                godMage.godSlayerDmg += orbBase;
                int[] candidates = new int[Main.maxNPCs];
                int sightCount = 0;      // 有视线且距离 > 50 的优先目标
                int fallbackCount = 0;   // 无视线 / 距离 ≤ 50 的兜底目标
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].CanBeChasedBy(projectile, false))
                        continue;
                    float manhattan = Math.Abs(Main.npc[i].position.X + Main.npc[i].width / 2 - projectile.position.X + projectile.width / 2)
                        + Math.Abs(Main.npc[i].position.Y + Main.npc[i].height / 2 - projectile.position.Y + projectile.height / 2);
                    if (manhattan < 800f)
                    {
                        if (Collision.CanHit(projectile.position, 1, 1, Main.npc[i].position, Main.npc[i].width, Main.npc[i].height) && manhattan > 50f)
                        {
                            candidates[sightCount] = i;
                            sightCount++;
                        }
                        else if (sightCount == 0)
                        {
                            candidates[fallbackCount] = i;
                            fallbackCount++;
                        }
                    }
                }
                // 源在"一个目标都挑不到"时直接 return（放弃余下逻辑）；本工程只跳过这两枚弹幕的生成
                if (sightCount > 0 || fallbackCount > 0)
                {
                    int orbTarget = sightCount > 0 ? candidates[Main.rand.Next(sightCount)] : candidates[Main.rand.Next(fallbackCount)];
                    // 源固定 20 像素/帧的随机方向初速（GodSlayerOrb 随后自行追踪）
                    float orbVelX = Main.rand.Next(-100, 101);
                    float orbVelY = Main.rand.Next(-100, 101);
                    float orbVelDist = (float)Math.Sqrt(orbVelX * orbVelX + orbVelY * orbVelY);
                    orbVelDist = 20f / orbVelDist;
                    orbVelX *= orbVelDist;
                    orbVelY *= orbVelDist;
                    Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, orbVelX, orbVelY,
                        ModContent.ProjectileType<GodSlayerOrb>(), (int)(orbBase * (godMage.auricSet ? 2.0f : 1.5f)), 0f, projectile.owner, orbTarget, 0f);
                    if (target.canGhostHeal)
                    {
                        float healMult = godMage.auricSet ? 0.03f : 0.06f;
                        healMult -= (float)projectile.numHits * 0.015f;
                        float healValue = (float)projectile.damage * healMult;
                        if (healMult > 0f && (int)healValue > 0 && Main.LocalPlayer.lifeSteal > 0f)
                        {
                            Main.LocalPlayer.lifeSteal -= healValue * 1.5f;
                            float worstMissing = 0f;
                            int healTarget = projectile.owner;
                            for (int i = 0; i < Main.maxPlayers; i++)
                            {
                                if (Main.player[i].active && !Main.player[i].dead && ((!Main.player[projectile.owner].hostile && !Main.player[i].hostile) || Main.player[projectile.owner].team == Main.player[i].team))
                                {
                                    float manhattan = Math.Abs(Main.player[i].position.X + Main.player[i].width / 2 - projectile.position.X + projectile.width / 2)
                                        + Math.Abs(Main.player[i].position.Y + Main.player[i].height / 2 - projectile.position.Y + projectile.height / 2);
                                    if (manhattan < 1200f && Main.player[i].statLifeMax2 - Main.player[i].statLife > worstMissing)
                                    {
                                        worstMissing = Main.player[i].statLifeMax2 - Main.player[i].statLife;
                                        healTarget = i;
                                    }
                                }
                            }
                            Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center.X, projectile.Center.Y, 0f, 0f,
                                ModContent.ProjectileType<GodSlayerHealOrb>(), 0, 0f, projectile.owner, healTarget, healValue);
                        }
                    }
                }
            }
            // 始源林海法师套装（silvaMage）的「魔法弹幕命中敌人时有几率引发巨型爆炸」：
            // 口径照经典版 CalamityGlobalProjectile.cs:416-438。**注意**：源 tooltip 写「10% 几率」，
            // 但实现是 `Main.rand.Next(0, 100) >= 97` = **3%**，且只对「穿透为 1 的魔法弹幕」生效——
            // 本工程照源实现、保留原文案。
            // 效果：播 SoundID.Zombie103，把本次判定框临时撑到 96×96，喷一圈 ChlorophyteWeapon 尘
            //（源裸数字 157 已 Cecil 反查），把本次伤害乘 4（穿金源 ×7）后再结算一次 Damage()。
            CalamityDemutationPlayer silvaMagePlayer = Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>();
            if (projectile.CountsAsClass<MagicDamageClass>() && silvaMagePlayer.silvaMage && projectile.penetrate == 1 && Main.rand.Next(0, 100) >= 97)
            {
                SoundEngine.PlaySound(SoundID.Zombie103, projectile.position);
                projectile.position = projectile.Center;
                projectile.width = projectile.height = 96;
                projectile.position.X -= projectile.width / 2;
                projectile.position.Y -= projectile.height / 2;
                for (int i = 0; i < 3; i++)
                {
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 1.5f);
                }
                for (int i = 0; i < 30; i++)
                {
                    int blastDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 0, new Color(Main.DiscoR, 203, 103), 2.5f);
                    Main.dust[blastDust].noGravity = true;
                    Main.dust[blastDust].velocity *= 3f;
                    blastDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 1.5f);
                    Main.dust[blastDust].velocity *= 2f;
                    Main.dust[blastDust].noGravity = true;
                }
                projectile.damage *= silvaMagePlayer.auricSet ? 7 : 4;
                projectile.Damage();
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
