using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 激光喷泉弹幕（移植自 CWR 的 LaserFountains）：隐形无伤害的发射源，
    /// 每 12 帧向随机方向喷一束死亡激光（DeathLaser）。
    /// </summary>
    internal class LaserFountains : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：4×4 的极小型隐形发射源、穿透无限、不碰撞物块、存活 60 帧、伤害类型为近战。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.DamageType = DamageClass.Melee;
        }
        public ref float Time => ref Projectile.ai[0];
        /// <summary>
        /// 恒定返回 false：本体是隐形发射源，不造成任何伤害，只负责生成 DeathLaser。
        /// </summary>
        public override bool? CanDamage() => false;
        /// <summary>
        /// 更新逻辑：每 12 帧（且场上已有的 DeathLaser 不超过 13 束时）播放音效，并在距中心 760~920 像素的随机方向处
        /// 生成一束 DeathLaser 射向中心。新激光伤害为本体的一半、长度 localAI[1] 设为 1500、伤害类型校正为近战。
        /// </summary>
        public override void AI()
        {
            int type = ModContent.ProjectileType<DeathLaser>();
            if (Time > 0 && Time % 12 == 0 && Main.player[Projectile.owner].ownedProjectileCounts[type] <= 13)
            {
                SoundEngine.PlaySound(SoundID.Item12, Projectile.position);
                Vector2 vr = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * Main.rand.Next(760, 920);
                int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + vr, vr.UnitVector() * -1, type, Projectile.damage / 2, 0, Projectile.owner, 1);
                Main.projectile[proj].DamageType = DamageClass.Melee;
                Main.projectile[proj].localAI[1] = 1500;
            }
            Time++;
        }
        /// <summary>
        /// 命中敌怪：按灾厄双版本分别施加 180 帧的 GodSlayerInferno（弑神炼狱）debuff。
        /// 注意本体 <see cref="CanDamage"/> 恒为 false、不造成伤害，此回调正常情况下不会被触发，保留以保持与 DeathLaser 一致。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                target.AddBuff(calamity.Find<ModBuff>("GodSlayerInferno").Type, 180);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                target.AddBuff(calamity1.Find<ModBuff>("GodSlayerInferno").Type, 180);
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：同 <see cref="OnHitNPC"/>，按双版本分支施加 180 帧的 GodSlayerInferno；
        /// 同样因本体不造成伤害而正常情况下不会被触发。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                target.AddBuff(calamity.Find<ModBuff>("GodSlayerInferno").Type, 180);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                target.AddBuff(calamity1.Find<ModBuff>("GodSlayerInferno").Type, 180);
            }
        }
    }
}
