using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 银河爆炸：追踪敌人，接近后原地扩散成区域性爆炸
    /// </summary>
    internal class GalaxyBlast:ModProjectile
    {
        public float DustType => Projectile.ai[2];   // ai[2]：0=地牢水、1=星云紫、其余=暗影光束，决定粉尘/爆炸颜色
        // 使用隐形贴图，视觉效果完全由粉尘与动画帧承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        public int UseDustID => DustType == 0 ? DustID.DungeonWater : DustType == 1 ? 235 : DustID.ShadowbeamStaff;
        /// <summary>
        /// 静态属性：注册 4 帧爆炸动画（爆炸阶段按帧推进）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        /// <summary>
        /// 基础属性：10x10 碰撞箱；友方、借用箭矢 AI 模板、无限穿透、每帧额外更新 5 次、
        /// 无视地形、独立 10 帧命中冷却、存活 600 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 5;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 600;
        }
        /// <summary>
        /// 飞行阶段追踪敌人，接近后转为原地扩散爆炸并播放动画
        /// </summary>
        public override void AI()
        {
            // 未进入爆炸阶段且已下落到 ai[1] 记录的高度以下时：恢复地形碰撞，让星弹能落地爆炸
            if (Projectile.ai[1] != -1f && Projectile.position.Y > Projectile.ai[1])
            {
                Projectile.tileCollide = true;
            }
            // 坐标出现 NaN 时直接销毁，防止弹幕卡死
            if (Projectile.position.HasNaNs())
            {
                Projectile.Kill();
                return;
            }
            bool isInTile = WorldGen.SolidTile(Framing.GetTileSafely((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16));
            Dust blastDust = Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, UseDustID, 0f, 0f, 0, default, 1f)];
            blastDust.position = Projectile.Center;
            blastDust.velocity = Vector2.Zero;
            blastDust.noGravity = true;
            if (isInTile)
            {
                blastDust.noLight = true;
            }
            // ai[1]==-1 表示已贴近敌人进入爆炸阶段：速度清零、碰撞箱扩大到 140x140、播放 4 帧爆炸动画后自毁
            if (Projectile.ai[1] == -1f)
            {
                Projectile.ai[0] += 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.penetrate = -1;
                Projectile.position = Projectile.Center;
                Projectile.width = Projectile.height = 140;
                Projectile.Center = Projectile.position;
                Projectile.alpha -= 10;
                if (Projectile.alpha < 0)
                {
                    Projectile.alpha = 0;
                }
                if (++Projectile.frameCounter >= Projectile.MaxUpdates * 3)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
                if (Projectile.ai[0] >= Main.projFrames[Projectile.type] * Projectile.MaxUpdates * 3)
                {
                    Projectile.Kill();
                }
                return;
            }
            Projectile.alpha = 255;
            // 仅在本体更新帧执行索敌（extraUpdates 的中间帧跳过），找 60 像素内最近敌人并切换到爆炸阶段
            if (Projectile.numUpdates == 0)
            {
                int npcTracker = -1;
                float homingRange = 60f;
                foreach (NPC nPC in Main.ActiveNPCs)
                {
                    if (nPC.CanBeChasedBy(Projectile, false))
                    {
                        float npcDistance = Projectile.Distance(nPC.Center);
                        if (npcDistance < homingRange && Collision.CanHitLine(Projectile.Center, 0, 0, nPC.Center, 0, 0))
                        {
                            homingRange = npcDistance;
                            npcTracker = nPC.whoAmI;
                        }
                    }
                }
                if (npcTracker != -1)
                {
                    Projectile.ai[0] = 0f;
                    Projectile.ai[1] = -1f;
                    Projectile.netUpdate = true;
                }
            }
        }
        /// <summary>
        /// 命中敌人时施加灼烧并立即消失
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
            Projectile.Kill();
        }
        /// <summary>
        /// 命中玩家（PvP）时施加灼烧并立即消失
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 180);
            Projectile.Kill();
        }
        /// <summary>
        /// 消失时播放音效并迸发一圈对应颜色的粉尘
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            bool insideTile = WorldGen.SolidTile(Framing.GetTileSafely((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16));
            for (int m = 0; m < 4; m++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, UseDustID, 0f, 0f, 100, default, 1.5f);
            }
            for (int n = 0; n < 4; n++)
            {
                int killingPeople = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, UseDustID, 0f, 0f, 0, default, 2.5f);
                Main.dust[killingPeople].noGravity = true;
                Main.dust[killingPeople].velocity *= 3f;
                if (insideTile)
                {
                    Main.dust[killingPeople].noLight = true;
                }
                killingPeople = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, UseDustID, 0f, 0f, 100, default, 1.5f);
                Main.dust[killingPeople].velocity *= 2f;
                Main.dust[killingPeople].noGravity = true;
                if (insideTile)
                {
                    Main.dust[killingPeople].noLight = true;
                }
            }
        }
    }
}
