using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 宇宙暗流的长矛弹幕（移植自灾厄大修 Beta1.12 的 <c>StreamGougeProjOld</c>）——
    /// 源注释里写明"灾厄的矛基类有动画问题，所以自己写了一套"，本弹幕即那套自写长矛逻辑：
    /// 矛身挂在玩家手上，沿鼠标方向在 56~196 像素之间"刺出—收回"，寿命与玩家的挥击动画等长；
    /// 刺出的第一帧朝矛尖甩出一束宇宙精粹（<see cref="EssenceBeam"/>，伤害为长矛的 4 倍），
    /// 刺到最远点时在矛尖炸开一圈传送门尘。
    /// <para>
    /// 与 CI 源的差异（数值全部照搬）：
    /// ① 矛的转角：源写 <c>rotation += 45°/135°</c>，而源自己又把原版矛 AI 钩子干掉了，于是转角会逐帧累加——
    ///    18 帧的刺击里整根矛会自转 6 圈多。这里按源内同一套方向常量、另一处正确写法（<c>MarniteSpearProj</c> 投掷模式）
    ///    改成直接赋值，并补上源里没人赋值的 <c>spriteDirection</c>，矛尖才会始终朝着刺击方向；
    /// ② 顶点尘：源的条件 <c>Projectile.velocity == Projectile.velocity * RangeMax / 2</c> 是恒假的浮点比较，
    ///    源注释里的"顶点处生成传送门粒子"实际从不触发；这里改成刺出进度到 1 的那一帧触发；
    /// ③ 瞄准：源的 <c>Main.MouseWorld</c> 换成跨端可见的 <see cref="CalamityDemutationPlayer.GetMouseWorld"/>
    ///    （长矛 AI 在所有端都会跑，直读 Main.MouseWorld 在别人的客户端上会朝那个端自己的鼠标刺）；
    /// ④ 命中减益走本工程的双版本容错封装（现代版 / 经典版 GodSlayerInferno，两版都取不到时退回原版诅咒地狱）。
    /// </para>
    /// </summary>
    internal class StreamGougeProj : ModProjectile
    {
        /// <summary>矛刺出的最近距离（照源）</summary>
        protected virtual float RangeMin => 56f;
        /// <summary>矛刺出的最远距离（照源）</summary>
        protected virtual float RangeMax => 196f;
        /// <summary>基础属性：40×40、原版矛 AI（实际由 PreAI 接管）、不撞物块、穿透无限、逐帧可再命中、存活 90 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.aiStyle = ProjAIStyleID.Spear;
            Projectile.timeLeft = 90;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }
        /// <summary>
        /// 长矛主逻辑（返回 false 干掉原版矛 AI）：贴到玩家手上 → 按挥击动画进度算出刺出的位置 →
        /// 修正矛的朝向 → 首帧发射宇宙精粹 → 朝鼠标方向平滑转向
        /// </summary>
        public override bool PreAI()
        {
            Player owner = Main.player[Projectile.owner];
            int dura = owner.itemAnimationMax;   // 这次挥击的总帧数，源直接拿它当刺击时长
            owner.heldProj = Projectile.whoAmI;  // 把武器本体挂到这根矛上（配合物品的 noUseGraphic 画法）

            // 必要时刻重置生命：矛的寿命跟挥击动画同长
            if (Projectile.timeLeft > dura)
                Projectile.timeLeft = dura;

            // 源写作 Normalize(velocity * 5)，与直接归一化等价；下面算刺出距离要用单位向量
            Projectile.velocity = Vector2.Normalize(Projectile.velocity);

            // 分段进度：前半段刺出（0→1），后半段收回（1→0）
            float halfDura = dura * 0.5f;
            float progression;
            if (Projectile.timeLeft < halfDura)
                progression = Projectile.timeLeft / halfDura;
            else
                progression = (dura - Projectile.timeLeft) / halfDura;

            // 让矛开始移动：从 56 像素平滑刺到 196 像素
            Projectile.Center = owner.MountedCenter + Vector2.SmoothStep(Projectile.velocity * RangeMin, Projectile.velocity * RangeMax, progression);

            // 刺到最远点时在矛尖炸开传送门尘（源里那处恒假条件见类注释 ②）
            if (progression >= 1f)
                PortalDust();

            // 给矛一个正确的转角：贴图是 45° 斜放的，朝左转 45°、朝右转 135°（方向常量照源）
            Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == -1 ? MathHelper.PiOver4 : MathHelper.PiOver4 * 3f);

            // 让矛刺出的第一帧就发射弹幕，而非等到顶点（照源）
            if (Projectile.ai[0] == 0f)
            {
                ShootProj();
                Projectile.ai[0] = 1f;
            }

            // 朝鼠标方向平滑转向：每帧把当前速度往鼠标方向插值一半，速度取武器 shootSpeed
            Vector2 rrp = owner.RotatedRelativePoint(owner.MountedCenter, true);
            UpdateAim(rrp, owner.HeldItem.shootSpeed);

            // 干掉原版矛 AI 钩子
            return false;
        }
        /// <summary>
        /// 朝鼠标方向平滑转向（照源 UpdateAim）：每帧把当前速度往鼠标方向插值一半再乘上武器初速；
        /// 鼠标坐标取跨端可见的 <see cref="CalamityDemutationPlayer.GetMouseWorld"/>。
        /// </summary>
        public virtual void UpdateAim(Vector2 source, float speed)
        {
            // 玩家当前的瞄准方向（单位向量）
            Vector2 aim = Vector2.Normalize(Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().GetMouseWorld() - source);
            if (aim.HasNaNs())
            {
                aim = -Vector2.UnitY;
            }

            // 只把速度往瞄准方向转一部分，让矛在刺出过程中平滑转向
            aim = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(Projectile.velocity), aim, 0.5f));
            aim *= speed;

            if (aim != Projectile.velocity)
            {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = aim;
        }
        /// <summary>
        /// 发射宇宙精粹（照源 ShootProj）：伤害 = 长矛伤害 × 0.5 × 4（源的写法），初速为矛速度的 15 倍，
        /// 同时播放音效并在矛身上撒粒子。
        /// </summary>
        public void ShootProj()
        {
            int damage = (int)(Projectile.damage * 0.5f);
            float kb = Projectile.knockBack * 0.5f;
            Vector2 projPos = Projectile.Center + Projectile.velocity;
            Vector2 projVel = Projectile.velocity * 15f;
            if (Projectile.owner == Main.myPlayer)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), projPos, projVel, ModContent.ProjectileType<EssenceBeam>(), damage * 4, kb, Projectile.owner, 0f, 0f);

            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            // 矛身上的粒子
            ExtraBehavior();
        }
        /// <summary>矛尖的传送门尘环（照源 PortalDust）：18 颗暗影束尘沿圆周以 3.8 的初速散开、放大到 2.4 倍</summary>
        public void PortalDust()
        {
            int circleDust = 18;
            Vector2 baseDustVel = new Vector2(3.8f, 0f);
            for (int i = 0; i < circleDust; ++i)
            {
                int dustID = DustID.ShadowbeamStaff;   // 源里写死的 173
                float angle = i * (MathHelper.TwoPi / circleDust);
                Vector2 dustVel = baseDustVel.RotatedBy(angle);

                int idx = Dust.NewDust(Projectile.Center, 1, 1, dustID);
                Main.dust[idx].noGravity = true;
                Main.dust[idx].position = Projectile.Center;
                Main.dust[idx].velocity = dustVel;
                Main.dust[idx].scale = 2.4f;
            }
        }
        /// <summary>矛身上的粒子（照源 ExtraBehavior）：在矛的半宽半高范围内撒 3 颗静止的暗影束尘</summary>
        public void ExtraBehavior()
        {
            int movingDust = 3;
            for (int i = 0; i < movingDust; ++i)
            {
                int dustID = DustID.ShadowbeamStaff;
                Vector2 corner = 0.5f * Projectile.position + 0.5f * Projectile.Center;
                int idx = Dust.NewDust(corner, Projectile.width / 2, Projectile.height / 2, dustID);

                Main.dust[idx].noGravity = true;
                Main.dust[idx].velocity = Vector2.Zero;
            }
        }
        /// <summary>命中：挂 300 帧神裁狱火（双版本容错，两版灾厄都取不到时退回原版诅咒地狱）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "GodSlayerInferno", 300, BuffID.CursedInferno);
        }
    }
}
