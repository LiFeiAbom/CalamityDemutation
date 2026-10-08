using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 封存奇点召出的黑洞（照灾厄 2.0 的 <c>SealedSingularityBlackhole</c>）：
    /// 40×40 判定、穿透无限、不撞物块、与同类共享一套静态无敌帧（每 10 帧可再结算一次）；
    /// 7 帧动画（贴图 88×630，每 5 帧换一帧）。
    /// </summary>
    /// <remarks>
    /// 吸力（每帧给范围内的普通敌怪加速度）：常规 **500 像素 / 0.1**，潜行打击召出的翻倍成
    /// **1000 像素 / 0.25**；<c>ai[0] &gt; 300</c> 之后每帧缩小 5%（体积与不透明度同步），
    /// 缩到 <c>scale ≤ 0.05</c> 自行消失。潜行那枚以 <c>ai[0] = -180</c> 生成，等于多吸 180 帧。
    /// <para>
    /// 目标筛选照源内联 <c>CalamityGlobalNPC.ShouldAffectNPC</c>（写法见 `AnarchyBlade`）；
    /// 源还额外放行了灾厄自己的"超级木桩" <c>SuperDummyNPC</c>，那类软依赖下引不到，本件**只跳过这一项**
    /// （普通敌怪的行为完全一致）。
    /// </para>
    /// </remarks>
    internal class SealedSingularityBlackhole : ModProjectile
    {
        /// <summary>常规吸收半径（照源）</summary>
        private const float SuckRange = 500f;
        /// <summary>常规吸力（照源）</summary>
        private const float SuckPower = 0.1f;
        /// <summary>潜行黑洞的吸收半径（照源）</summary>
        private const float StealthSuckRange = 1000f;
        /// <summary>潜行黑洞的吸力（照源）</summary>
        private const float StealthSuckPower = 0.25f;
        /// <summary>开始收缩的帧数（照源）</summary>
        private const float ShrinkStart = 300f;
        /// <summary>自行消失的缩放阈值（照源）</summary>
        private const float ScaleFloor = 0.05f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
        }
        /// <summary>40×40、穿透无限、不撞物块、共享静态无敌帧（每 10 帧一次）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>7 帧动画 + 吸怪 + 300 帧后收缩消散（照源）</summary>
        public override void AI()
        {
            if (Projectile.timeLeft % 5 == 0)
                Projectile.frame++;
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.frame = 0;

            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            float maxDistance = stealthStrike ? StealthSuckRange : SuckRange;
            float suckPower = stealthStrike ? StealthSuckPower : SuckPower;
            Vector2 center = Projectile.Center;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile, false) || !Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;
                if (!ShouldAffectNPC(npc))
                    continue;
                float extraDistance = npc.width / 2 + npc.height / 2;
                if (Vector2.Distance(npc.Center, Projectile.Center) >= maxDistance + extraDistance)
                    continue;
                npc.velocity.X += npc.position.X < center.X ? suckPower : -suckPower;
                npc.velocity.Y += npc.position.Y < center.Y ? suckPower : -suckPower;
            }

            Projectile.ai[0]++;
            if (Projectile.ai[0] > ShrinkStart)
            {
                Projectile.scale *= 0.95f;
                Projectile.Opacity *= 0.95f;
                Projectile.height = (int)(Projectile.height * Projectile.scale);
                Projectile.width = (int)(Projectile.width * Projectile.scale);
            }
            if (Projectile.scale <= ScaleFloor)
                Projectile.Kill();
        }
        /// <summary>拖影（照源调灾厄的 DrawAfterimagesCentered，本工程用同名 CDUtil 版本）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
        /// <summary>PvP 命中挂 5 秒「失明」（源只写了这一条，没有 PvE 命中回调）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Blackout, 300);
        }
        /// <summary>
        /// 对应灾厄的 <c>CalamityGlobalNPC.ShouldAffectNPC</c>：只对普通敌怪生效
        /// （灾厄还排除了自家的一批小 Boss 部件，那些类型软依赖下引不到，这里只保留原版那部分排除项，
        /// 与工程里 AnarchyBlade 的内联版完全一致）。
        /// </summary>
        private static bool ShouldAffectNPC(NPC target) => target.damage > 0 && !target.boss && !target.friendly && !target.dontTakeDamage
            && target.type != NPCID.Creeper && target.type != NPCID.MourningWood && target.type != NPCID.Everscream
            && target.type != NPCID.SantaNK1 && target.type != NPCID.GolemFistLeft && target.type != NPCID.GolemFistRight
            && target.type != NPCID.DD2Betsy;
    }
}
