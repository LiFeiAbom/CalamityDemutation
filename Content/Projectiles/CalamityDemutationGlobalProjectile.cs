using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Projectiles.Healing;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
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
        /// 不受特殊效果影响：BaseSwingCO 挥砍弹幕置位，置位后跳过外部的特殊效果处理
        /// </summary>
        public bool NotSubjectToSpecialEffects;
        /// <summary>
        /// 弹幕"原始 extraUpdates"缓存：-1 表示尚未记录。
        /// ProjUtil 需要临时改动 extraUpdates 时会先缓存原值，用完再写回，避免叠加修改后无法还原。
        /// </summary>
        public int defExtraUpdates = -1;
        /// <summary>
        /// 灾厄穿透计数的软依赖等价物（对应原 CalamityGlobalProjectile.timesPierced）
        /// </summary>
        public int timesPierced;
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
        /// （写入目标玩家索引与治疗量）为其回血。原版 canGhostHeal 限制已被移除，任意敌人均可触发。
        /// </summary>
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
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
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().auricSet)// && target.canGhostHeal
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
        /// 近战弹幕命中玩家（PvP）时，按攻击者实际装备/套装给目标施加效果：
        /// - 亚利姆徽章：随机 120/240/360 帧神圣火（现代版）与圣光（经典版）；
        /// - 元素手套：全套元素 debuff 各 120 帧（原版五毒 + 灾厄现代/经典两版本）；
        /// - omega 蓝套装（omegaBlueSet）：240 帧 HadopelagicPressure / CrushDepth；
        /// - 女巫套装（silvaSet）/ 奥瑞克套装（auricSet）：与 OnHitNPC 同构的吸血逻辑
        ///   （比例 0.03 / 0.05 起，随弹幕命中次数递减，降至 0 即放弃）。
        /// </summary>
        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            // 只处理近战弹幕（含近战连击弹幕），避免远程/魔法弹幕误触发
            if (!projectile.CountsAsClass<MeleeDamageClass>() && !projectile.CountsAsClass<MeleeNoSpeedDamageClass>())
                return;
            // 攻击者 = 弹幕主人 A（直接查其实际装备，不依赖 ModPlayer 标志位）
            Player attacker = Main.player[projectile.owner];
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
            // 攻击者穿着 omega 蓝套装：施加 240 帧水压 / 压碎深度
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().omegaBlueSet)
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HadopelagicPressure", 240);
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 240);
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
            // 奥瑞克套装（auricSet）：弹幕吸血，比例 0.05 起随命中次数递减
            if (Main.player[projectile.owner].GetModPlayer<CalamityDemutationPlayer>().auricSet)
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
