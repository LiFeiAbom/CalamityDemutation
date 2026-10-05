using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 弑神者召唤头（GodSlayerHornedHelm）的机械蠕虫维护。
    /// 源把这段写在物品的 UpdateArmorSet 里（Classic GodSlayerHornedHelm），本工程按第 5 节口径
    /// 挪到玩家侧的 PostUpdateMiscEffects，逻辑逐行对应：补增益 → 没有蠕虫头时重新召唤整条蠕虫。
    /// **与源的一处差异**：源在"没有头"的外层条件下还写了一段"在尾巴前插入两节新身体"的分支，
    /// 但该分支要求"头和尾都存在"，与外层条件自相矛盾、永远不会执行（已逐行核对经典版与现代版同样如此），
    /// 故本工程只实现可达行为（整条重召），不搬运那段死代码。
    /// 生成只在主人端做（本钩子每名玩家 × 每一端都会跑）。
    /// </summary>
    internal partial class CalamityDemutationPlayer : ModPlayer
    {
        /// <summary>蠕虫各段的生成间隔下限：同一帧内只生成一次（由"没有头"这个判据天然保证）</summary>
        public void UpdateGodSlayerMechworm()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<MechwormHead>()] >= 1)
                return;
            // 盾/召唤头上的常驻增益（源在此处补，蠕虫头 AI 里也会续）
            if (Player.FindBuffIndex(ModContent.BuffType<Mechworm>()) == -1)
            {
                Player.AddBuff(ModContent.BuffType<Mechworm>(), 3600, true);
            }
            int headType = ModContent.ProjectileType<MechwormHead>();
            int bodyType = ModContent.ProjectileType<MechwormBody>();
            int bodyType2 = ModContent.ProjectileType<MechwormBody2>();
            int tailType = ModContent.ProjectileType<MechwormTail>();
            int owner = Player.whoAmI;
            // 先把残留下的旧蠕虫段清掉（源原样）
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == owner)
                {
                    if (Main.projectile[i].type == headType || Main.projectile[i].type == bodyType
                        || Main.projectile[i].type == bodyType2 || Main.projectile[i].type == tailType)
                    {
                        Main.projectile[i].Kill();
                    }
                }
            }
            // 伤害：源用 35 × (召唤伤害×5/3 + 召唤伤害×0.46×(仆从数−1))，仆从数封顶 10；
            // 经典版只取 Multiplicative 那一小块，现代/CI 改用 Additive + Multiplicative，这里取后者（已修正）
            int maxMinionScale = Math.Min(Player.maxMinions, 10);
            StatModifier summonStat = Player.GetTotalDamage<SummonDamageClass>();
            float summonDamage = summonStat.Additive + summonStat.Multiplicative;
            int damage = (int)(35f * (summonDamage * 5f / 3f + summonDamage * 0.46f * (maxMinionScale - 1)));
            // 生成点取鼠标处：本方法已在"本机玩家"判据内，可直读 Main.MouseWorld
            Vector2 spawnCenter = Main.MouseWorld;
            float facing = Vector2.Dot(Vector2.UnitX.RotatedBy(Player.fullRotation), spawnCenter - Player.RotatedRelativePoint(Player.MountedCenter, true));
            Player.ChangeDir(facing > 0f ? 1 : -1);
            int head = Projectile.NewProjectile(Player.GetSource_FromThis(), spawnCenter.X, spawnCenter.Y, 0f, 0f, headType, damage, 1f, owner);
            int body = Projectile.NewProjectile(Player.GetSource_FromThis(), spawnCenter.X, spawnCenter.Y, 0f, 0f, bodyType, damage, 1f, owner, head);
            int body2 = Projectile.NewProjectile(Player.GetSource_FromThis(), spawnCenter.X, spawnCenter.Y, 0f, 0f, bodyType2, damage, 1f, owner, body);
            Main.projectile[body].localAI[1] = body2;
            Main.projectile[body].netUpdate = true;
            int tail = Projectile.NewProjectile(Player.GetSource_FromThis(), spawnCenter.X, spawnCenter.Y, 0f, 0f, tailType, damage, 1f, owner, body2);
            Main.projectile[body2].localAI[1] = tail;
            Main.projectile[body2].netUpdate = true;
        }
    }
}
