using System;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 破碎剑柄手持弹幕（移植自 CalamityEntropy 的 BrokenHiltHeld）：贴身绘制剑柄并绕玩家挥动。
    /// 挥砍总时长取自 <see cref="Player.itemTimeMax"/>，进度 progress 由 0 走到 1；
    /// ai[0] 为 ±1，决定这一挥的旋转方向（再配合玩家朝向决定顺时针还是逆时针），
    /// 所以物品每次 Shoot 都把方向取反，形成左右交替挥砍。
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds.HiltAttack"/>；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c> 换成 <c>Main.player[owner]</c> 与 TextureAssets；
    /// ③ <c>CEUtils.GetRepeatedCosFromZeroToOne</c>（递归余弦缓动）与 <c>CEUtils.LineThroughRect</c>
    /// 在 CE 侧属于工具库，这里内联；④ 去掉 CE 里只记录、从不读取的 odr 旋转历史与拖尾缓存设置；
    /// ⑤ <c>CEUtils.WeapSound</c>（CE 的近战音量配置项）按 1.0 处理，音量固定 0.6。
    /// </para>
    /// </summary>
    internal class BrokenHiltHeld:ModProjectile
    {
        /// <summary>直接复用武器本体的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/BrokenHilt";
        /// <summary>自身帧数计数（ai[0] 存的是挥砍方向，不是计时器）</summary>
        private float counter = 0f;
        /// <summary>绘制缩放（CE 原版每帧固定为 1，实际尺寸来自 Projectile.scale）</summary>
        private float scale = 1f;
        /// <summary>绘制透明度</summary>
        private float alpha = 0f;
        /// <summary>首帧标记：播挥砍音、套用玩家的近战尺寸加成</summary>
        private bool init = true;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;                        // 命中判定完全由 Colliding 的线段接管，碰撞箱取最小
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = -1;         // 同一次挥砍对同一敌人只结算一次
            Projectile.timeLeft = 100000;                // 占位值，真正的结束判据是 counter
            Projectile.extraUpdates = 3;                 // 配合物品 24 帧使用时间 → 挥砍总帧数 96
        }
        /// <summary>
        /// 挥砍主体：progress 由 counter /（itemTimeMax × MaxUpdates）得出，
        /// 旋转在此基础上按 +4 弧度 × ai[0] 的正弦缓动叠加，得到挥出去的弧线。
        /// </summary>
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            float maxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = counter / maxUpdateTimes;
            counter++;
            if (init)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.HiltAttack with { Pitch = Projectile.ai[0] * 0.12f, Volume = 0.6f }, Projectile.Center);
                float meleeScale = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
                init = false;
            }
            Projectile.timeLeft = 3;                     // 每帧续命，收尾交给下面的 counter 判据
            alpha = 1f;
            scale = 1f;
            const float rotF = 4f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (rotF * -0.5f + rotF * RepeatCos01(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
            Projectile.Center = owner.MountedCenter;
            owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            if (counter > maxUpdateTimes)
            {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
        }
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 自绘：以贴图角为原点（dir 决定取左上角还是右上角），并按 dir 水平翻转，
        /// 让剑柄始终保持握在手里的那头；剑贴图本身朝右上，故额外转 45°。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int dir = (int)Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
            Vector2 origin = dir > 0 ? new Vector2(0, texture.Height) : new Vector2(texture.Width, texture.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;
            Main.EntitySpriteDraw(texture, Projectile.Center + Main.player[Projectile.owner].gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale, effect);
            return false;
        }
        /// <summary>命中判定是从玩家中心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * SwordLength;
            float point = 0f;
            return targetHitbox.Contains((int)start.X, (int)start.Y) || targetHitbox.Contains((int)end.X, (int)end.Y) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 64, ref point);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * SwordLength, 54, DelegateMethods.CutTiles);
        }
        /// <summary>挥砍线段的长度（ai[0]==2 是 CE 预留的加长挥砍，本武器只会传 ±1）</summary>
        private float SwordLength => 70 * (Projectile.ai[0] == 2 ? 1.24f : 1) * Projectile.scale * scale;
        /// <summary>CEUtils.GetRepeatedCosFromZeroToOne 的等价实现：把 [0,1] 的余弦缓动递归套用 repeat 次</summary>
        private static float RepeatCos01(float v, int repeat)
        {
            if (repeat <= 1)
                return (float)Math.Cos(v * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
            return (float)Math.Cos(RepeatCos01(v, repeat - 1) * MathHelper.Pi - MathHelper.Pi) * 0.5f + 0.5f;
        }
    }
}
