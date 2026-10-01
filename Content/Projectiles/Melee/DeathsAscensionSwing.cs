using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 死神擢升·引导挥砍（移植自灾厄 2.0.4 的 <c>DeathsAscensionSwing</c>）——
    /// 死神擢升左键按住时挥出的巨镰：2×6 帧的超大剑身，3 帧一帧地推完一整趟挥砍，
    /// 只有「下劈到收回」的那几帧（frameX=0 且 frameY≥3、或 frameX=1）才有伤害判定，
    /// 期间玩家一直举着剑、朝向跟随鼠标，松手（或死亡/受控）即收招。
    /// <para>
    /// 与源的差异：① 伤害类型 TrueMeleeNoSpeedDamageClass → 本工程的 <c>DamageClass.MeleeNoSpeed</c>；
    /// ② <c>Owner.CantUseHoldout()</c> 换成同判据的内联写法；③ 鼠标坐标走跨端可见的
    /// <see cref="CalamityDemutationPlayer.GetMouseWorld"/>（源直接读 <c>Main.MouseWorld</c>）。
    /// </para>
    /// </summary>
    internal class DeathsAscensionSwing : ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>当前帧的列（0~1），对应贴图的左右两列动作</summary>
        public int frameX = 0;
        /// <summary>当前帧的行（0~5），对应一趟挥砍的六个姿势</summary>
        public int frameY = 0;
        // ── 属性 ──
        /// <summary>帧序列下标（列 × 6 + 行）；赋值时自动拆回 frameX / frameY</summary>
        public int CurrentFrame
        {
            get => frameX * 6 + frameY;
            set
            {
                frameX = value / 6;
                frameY = value % 6;
            }
        }
        /// <summary>弹幕主人（手持者）</summary>
        private Player Owner => Main.player[Projectile.owner];
        // ── 生命周期方法 ──
        /// <summary>
        /// 基础属性：159×230 的巨大判定框、放大 1.15 倍、近战但不吃攻速（<see cref="DamageClass.MeleeNoSpeed"/>）、
        /// 无限穿透、不撞物块、同一敌人按当前帧段享受 8~12 帧的静态无敌。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 159;
            Projectile.height = 230;
            Projectile.scale = 1.15f;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ownerHitCheck = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
            Projectile.frameCounter = 0;
        }
        /// <summary>
        /// AI：推帧（3 帧一帧，推完 12 帧回到起点）→ 挥砍音效与分段无敌帧 →
        /// 把弹幕钉在玩家手上并让玩家保持举剑姿势 → 松手即收招。
        /// 只有主人端读取鼠标与引导状态，其余端按同步来的速度与位置跟随。
        /// </summary>
        public override void AI()
        {
            // 帧循环：3 帧推进一格，走完两列六行（12 格）后回到起点
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 3 == 0)
            {
                CurrentFrame++;
                if (frameX >= 2)
                    CurrentFrame = 0;
            }
            // 下劈起始那一帧补一次挥砍音效
            if (frameX == 0 && frameY == 3 && Projectile.frameCounter % 3 == 0)
                SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
            // 分段无敌帧：下劈到收回的 8 帧更密，上撩段放到 12 帧
            if ((frameX == 0 && frameY >= 3) || (frameX == 1 && frameY <= 1))
                Projectile.idStaticNPCHitCooldown = 8;
            else if (frameX == 1 && frameY > 1)
                Projectile.idStaticNPCHitCooldown = 12;
            Vector2 playerRotatedPoint = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner)
            {
                if (!CantUseHoldout(Owner))
                    HandleChannelMovement(playerRotatedPoint);
                else
                    Projectile.Kill();
            }
            // 朝向与贴图翻转
            Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.spriteDirection = Projectile.direction;
            if (Projectile.direction == 1)
                Projectile.Left = Owner.MountedCenter;
            else
                Projectile.Right = Owner.MountedCenter;
            Projectile.position.X += Projectile.spriteDirection == -1 ? 26f : -26f;
            Projectile.position.Y -= Projectile.scale * 2f;
            Owner.ChangeDir(Projectile.direction);
            // 防止弹幕自然消亡：每帧把寿命顶回 2
            Projectile.timeLeft = 2;
            // 让玩家保持「正在使用」的姿势与手臂角度
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
        }
        // ── 公开方法 ──
        /// <summary>引导期间的瞄准：只按鼠标在玩家的左/右来取水平朝向（与源一致，不跟随纵向）。</summary>
        public void HandleChannelMovement(Vector2 playerRotatedPoint)
        {
            Vector2 mouseWorld = Owner.GetModPlayer<CalamityDemutationPlayer>().GetMouseWorld();
            Vector2 newVelocity = Vector2.UnitX * (mouseWorld.X > playerRotatedPoint.X).ToDirectionInt();
            // 朝向发生变化时同步给其他端
            if (Projectile.velocity.X != newVelocity.X || Projectile.velocity.Y != newVelocity.Y)
                Projectile.netUpdate = true;
            Projectile.velocity = newVelocity;
        }
        /// <summary>绘制 2×6 帧的巨镰贴图（前两帧不画，避免起手时闪一下原姿势）。</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.frameCounter <= 1)
                return false;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 position = Projectile.Center - Main.screenPosition + (Projectile.spriteDirection == -1 ? new Vector2(60, 0) : new Vector2(-60, 0));
            Vector2 origin = texture.Size() / new Vector2(2f, 6f) * 0.5f;
            Rectangle frame = texture.Frame(2, 6, frameX, frameY);
            SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(texture, position, frame, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            return false;
        }
        /// <summary>半透明的武器着色（源原值）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, 170);
        /// <summary>只有下劈到收回的那几帧才有伤害判定（源原值）</summary>
        public override bool? CanDamage() => ((frameX == 0 && frameY >= 3) || frameX == 1) && Projectile.frameCounter > 6;
        // ── 私有工具 ──
        /// <summary>玩家是否已无法继续引导（照搬灾厄 <c>Player.CantUseHoldout</c> 的判据）。</summary>
        private static bool CantUseHoldout(Player player) => player == null || !player.active || player.dead || !player.channel || player.CCed || player.noItems;
    }
}
