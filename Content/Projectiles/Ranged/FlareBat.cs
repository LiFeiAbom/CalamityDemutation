using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 炎翼蝠（FlareBat） - 炎翼弓把木箭转化出的黑曜石蝙蝠，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/FlareBat.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 24×24、贴图 24×150（**5 帧**动画、每 2 帧换帧）、穿透 <c>3</c>、寿命 300、
    /// 自带微光 0.25、本地命中冷却 10。行为三件套：
    /// ① **回旋**——第 <see cref="HomingStartTime"/>~<see cref="HomingEndTime"/> 帧朝"最近的**玩家**"拉方向
    ///   （源写法 <c>Player.FindClosest</c>，2.0 / 2.2.2 / 经典三处**都这么写**，不是笔误，观感是蝙蝠绕回主人）；
    /// ② **撞墙反弹**——穿透 -1，还有余量就把速度分量各自取反弹开，归零才消失；
    /// ③ **命中即死**——打中敌怪立刻 <c>Kill</c>，所以"穿透 3"实际只服务撞墙反弹。
    /// </para>
    /// <para>
    /// 另外每帧撒一颗 <see cref="DustID.Torch"/> 火尘（无重力、往弹幕中心收），死亡时撒 10 颗。
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做。
    /// </para>
    /// </summary>
    internal class FlareBat : ModProjectile
    {
        /// <summary>动画帧数（照源 <c>Main.projFrames = 5</c>）</summary>
        private const int AnimationFrames = 5;
        /// <summary>回旋起始帧（照源：<c>ai[1] &gt; 30</c>）</summary>
        private const float HomingStartTime = 30f;
        /// <summary>回旋结束帧（照源：<c>ai[1] &lt; 110</c>）</summary>
        private const float HomingEndTime = 110f;
        /// <summary>加速上限：速度低于它时每帧 ×1.02（照源：<c>18f</c>）</summary>
        private const float SpeedCap = 18f;
        /// <summary>回旋的惯性系数（照源：<c>(vel * 24 + dir) / 25</c>）</summary>
        private const float HomingInertia = 24f;
        /// <summary>每帧动画帧数、贴图 5 帧（照源）</summary>
        public override void SetStaticDefaults() => Main.projFrames[Type] = AnimationFrames;
        /// <summary>
        /// 基础属性：24×24 判定、穿透 3、寿命 300 帧、自带微光 0.25、本地命中冷却 10 帧（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 300;
            Projectile.light = 0.25f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        /// <summary>
        /// AI（照源）：回旋 + 低速加速 + 撒火尘 + 5 帧循环动画 + 按速度摆向。
        /// 回旋只改速度、不碰玩家状态，故主人端与其他端都会各自跑出同一条轨迹（速度本身随生成包同步）。
        /// </summary>
        public override void AI()
        {
            // 源写法：朝"最近的玩家"拉方向（三版一致，保留原味）
            int closestPlayer = (int)Player.FindClosest(Projectile.Center, 1, 1);
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] < HomingEndTime && Projectile.ai[1] > HomingStartTime)
            {
                float currentSpeed = Projectile.velocity.Length();
                Vector2 towardsPlayer = Main.player[closestPlayer].Center - Projectile.Center;
                towardsPlayer.Normalize();
                towardsPlayer *= currentSpeed;
                Projectile.velocity = (Projectile.velocity * HomingInertia + towardsPlayer) / (HomingInertia + 1f);
                Projectile.velocity.Normalize();
                Projectile.velocity *= currentSpeed;
            }

            // 低速时每帧小幅加速（照源：仅在 ai[0] < 0 时生效）
            if (Projectile.ai[0] < 0f && Projectile.velocity.Length() < SpeedCap)
                Projectile.velocity *= 1.02f;

            // 火尘尾迹
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, default, 1f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
            Main.dust[dust].position = (Main.dust[dust].position + Projectile.Center) / 2f;

            // 5 帧循环动画（每 2 帧换一帧）
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 2)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= AnimationFrames)
                    Projectile.frame = 0;
            }

            // 朝向
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) * Projectile.direction;
        }
        /// <summary>撞墙：穿透 -1；还有余量就反弹（X / Y 分量各自取反），归零则消失（照源，返回 false 表示不死于撞击本身）</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.penetrate--;
            if (Projectile.penetrate <= 0)
            {
                Projectile.Kill();
            }
            else
            {
                if (Projectile.velocity.X != oldVelocity.X)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Projectile.velocity.Y != oldVelocity.Y)
                    Projectile.velocity.Y = -oldVelocity.Y;
            }
            return false;
        }
        /// <summary>命中敌怪：立刻消失（照源——所以"穿透 3"实际只用于撞墙反弹）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Projectile.Kill();
        /// <summary>消亡：照原速度方向撒 10 颗火尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int k = 0; k < 10; k++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Torch, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }
    }
}
