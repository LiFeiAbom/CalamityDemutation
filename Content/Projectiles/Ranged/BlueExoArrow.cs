using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 蓝色星流箭（BlueExoArrow） - 天堂之风把木箭转化出的四色星流箭之一，＝旧版支线的
    /// <c>Content/Projectiles/ExoLore/ExoArrowBlueExoLore</c>（本工程常驻 Lore 模式，故不留非 Lore 版）。
    /// <para>
    /// 定位是"命中后追加一串后续箭矢"：判定 5×5、**穿透 5**、本地命中冷却 20 帧、额外更新 3 帧、寿命 300 帧
    /// （非 Lore 分支只穿透 1、无本地冷却，追加串的伤害还是 ×0.7——Lore 分支是全额 ×1.0，本工程照 Lore）。
    /// </para>
    /// <para>
    /// 命中时朝目标方向撒 3 组「弹幕齐射」（<c>CDUtil.ProjectileBarrage</c>，每组一颗
    /// <see cref="BlueExoArrow2"/>，速度 10、偏移 ±500、上下随机），因整组写在
    /// <c>owner == Main.myPlayer</c> 判据内，联机时只在主人端生成、随同步包发出去。
    /// 观感与青色箭同构：一条蓝色光束（共用 <c>Content/Projectiles/LaserProj</c> + <c>CDUtil.DrawBeam</c>）。
    /// </para>
    /// </summary>
    internal class BlueExoArrow : ModProjectile
    {
        /// <summary>光束渐隐总长（像素），照源 <c>DrawBeam(40f, 1.5f, …)</c> 的第一个参数</summary>
        private const float BeamLength = 40f;
        /// <summary>相邻两次盖印的间距（像素），同时是 <c>localAI[0]</c> 每帧的增长量（照源第二个参数）</summary>
        private const float BeamStep = 1.5f;
        /// <summary>命中时追加的后续箭数量（源为 <c>for (x = 0; x &lt; 3; x++)</c>）</summary>
        private const int BarrageCount = 3;
        /// <summary>后续箭的伤害倍率（照 Lore 分支：全额 1.0；非 Lore 分支是 0.7）</summary>
        private const float BarrageDamageMult = 1f;
        /// <summary>后续箭的击退倍率（照源 0.7）</summary>
        private const float BarrageKnockbackMult = 0.7f;
        /// <summary>后续箭的出手速度（照源 <c>projSpeed = 10f</c>）</summary>
        private const float BarrageSpeed = 10f;
        /// <summary>共用的光束贴图（见 <see cref="TealExoArrow"/> 的说明）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>基础属性：5×5、穿透 5、本地命中冷却 20、额外更新 3 帧、寿命 300 帧、射手伤害类型（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 5;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        /// <summary>AI：淡入 + 光束长度增长到 <see cref="BeamLength"/>；<c>ai[1]</c> 非 0 时反向收缩、缩到 0 自毁（照源）</summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += BeamStep;
                if (Projectile.localAI[0] > BeamLength)
                    Projectile.localAI[0] = BeamLength;
            }
            else
            {
                Projectile.localAI[0] -= BeamStep;
                if (Projectile.localAI[0] <= 0f)
                    Projectile.Kill();
            }
        }
        /// <summary>蓝色（照源 <c>new Color(0, 0, 250)</c>）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(0, 0, 250, Projectile.alpha);
        /// <summary>自绘光束（顶掉默认绘制）</summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.DrawBeam(BeamLength, BeamStep, lightColor);
        /// <summary>命中敌人：挂整套星云系减益，并追加一组后续箭矢</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.ExoDebuffs();
            SpawnFollowingBarrage(target.Center);
        }
        /// <summary>PvP 命中：同上（源的 <c>OnHitPvp</c> 在 1.4.4 起并入此钩子）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.ExoDebuffs();
            SpawnFollowingBarrage(target.Center);
        }
        /// <summary>
        /// 从自身位置朝目标点撒出一组后续箭：X 偏移 ±500、Y 偏移 ±500（上下随机），速度 10，
        /// 伤害 / 击退按 Lore 分支的倍率折算（源整组写在主人端判据里，本工程保留该判据）
        /// </summary>
        private void SpawnFollowingBarrage(Vector2 targetPos)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            for (int x = 0; x < BarrageCount; x++)
            {
                CDUtil.ProjectileBarrage(Projectile.GetSource_FromThis(), Projectile.Center, targetPos, Main.rand.NextBool(2),
                    500f, 500f, 0f, 500f, BarrageSpeed, ModContent.ProjectileType<BlueExoArrow2>(),
                    (int)(Projectile.damage * BarrageDamageMult), Projectile.knockBack * BarrageKnockbackMult, Projectile.owner);
            }
        }
    }
}
