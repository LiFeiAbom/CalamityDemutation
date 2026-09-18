using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 死亡激光弹幕（移植自 CWR 的 DeathLaser，裁剪掉锚定手持弓的 Status==0 分支）：
    /// 自由飞行的激光束，线判定碰撞，加色混合绘制 Body/Head/Don 三截光束。
    /// </summary>
    internal class DeathLaser : ModProjectile
    {
        // ── 属性 ──
        /// <summary>
        /// 光束长度（存于 localAI[1]）；为 0 时由 AI 初始化为 5000，可由生成方改写
        /// </summary>
        public float Leng
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }
        /// <summary>
        /// 贴图路径：本体为 RayBeam，绘制时再拼 Body/Head/Don 后缀取三截光束贴图
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Ranged/RayBeam";
        /// <summary>
        /// 中段光束的滚动计时（存于 ai[2]），自增后驱动 Body 贴图的纵向采样偏移
        /// </summary>
        public int Time { get => (int)Projectile.ai[2]; set => Projectile.ai[2] = value; }
        /// <summary>
        /// 横向拉伸系数（存于 localAI[0]），由剩余时间推出，供 PreDraw 做光束收缩动画
        /// </summary>
        private ref float wit => ref Projectile.localAI[0];
        // ── 生命周期方法 ──
        /// <summary>
        /// 静态设置：把 DrawScreenCheckFluff 提高到 5000，使这条超长光束即便远端在屏幕外很远也仍参与绘制判定，
        /// 避免被原版的屏幕裁剪提前剔除。
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;
        }
        /// <summary>
        /// 基础属性：32×32 碰撞盒、穿透无限、timeLeft 仅 10（短命光束）、本地无敌帧 10 帧。
        /// 实际光束长度与命中范围由 <see cref="Colliding"/> 的线判定覆盖，默认碰撞盒基本不参与。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.scale = 1;
            Projectile.alpha = 80;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;   // = timeLeft，收紧冗余冷却值（原 12 > 10）
        }
        /// <summary>
        /// 固定位置：本弹幕不随速度移动，仅原地按朝向绘制与判定。
        /// </summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 更新逻辑：首次进入把长度 Leng 初始化为 5000（由 LaserFountains 生成时可能被改写）；
        /// alpha 每帧 +15 逐渐淡出，rotation 对齐速度方向，wit 由剩余时间推出（供 PreDraw 做横向拉伸动画），Time 自增驱动中段光束滚动。
        /// </summary>
        public override void AI()
        {
            if (Leng == 0)
                Leng = 5000;
            Projectile.alpha += 15;
            Projectile.rotation = Projectile.velocity.ToRotation();
            wit = (10 - Projectile.timeLeft) / 15f;
            Time++;
        }
        /// <summary>
        /// 线判定碰撞：从弹幕中心沿 rotation 方向延伸 Leng 长度得到线段，线宽固定 8，与目标 AABB 做检测。
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.rotation.ToRotationVector2() * Leng + Projectile.Center, 8, ref point);
        }
        /// <summary>
        /// 自绘光束：依次绘制 Don（炮口段）、Body（中段，按 Time 滚动采样贴图）与 Head（末端）三截贴图；
        /// 颜色在蓝（119,210,255）与紫（247,119,255）之间按全局时间做正弦插值，纵向拉伸由 wit 控制。
        /// 绘制前切入加色混合、结束还原默认混合，并返回 false 阻止默认贴图绘制。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D body = ModContent.Request<Texture2D>(Texture + "Body").Value;
            Texture2D head = ModContent.Request<Texture2D>(Texture + "Head").Value;
            Texture2D dons = ModContent.Request<Texture2D>(Texture + "Don").Value;
            float colorMePurple = (float)((Math.Sin(MathHelper.TwoPi / 0.9f * Main.GlobalTimeWrappedHourly) + 1) * 0.5f);
            Color color = Color.Lerp(new Color(119, 210, 255), new Color(247, 119, 255), colorMePurple);
            float rots = Projectile.rotation - MathHelper.PiOver2;
            Vector2 slp = new Vector2(wit, 1);
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            Main.EntitySpriteDraw(dons, Projectile.Center - Main.screenPosition, null, color, rots, new Vector2(dons.Width * 0.5f, 0), slp, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(body, Projectile.Center - Main.screenPosition + dir * dons.Height, new Rectangle(0, Time * -5, body.Width, (int)(Leng + 1)), color, rots, new Vector2(body.Width * 0.5f, 0), slp, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(head, Projectile.Center + dir * Leng - Main.screenPosition + dir * dons.Height, null, color, rots, new Vector2(body.Width * 0.5f, 0), slp, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>
        /// 命中敌怪：按灾厄双版本分别查找并施加 180 帧的 GodSlayerInferno（弑神炼狱）debuff
        /// （现代版 CalamityMod / 经典版 CalamityModClassicPreTrailer 分支各自注册）。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 180);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff classicGodSlayerInferno))
                    target.AddBuff(classicGodSlayerInferno.Type, 180);
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：同 <see cref="OnHitNPC"/>，按双版本分支施加 180 帧的 GodSlayerInferno debuff。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 180);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff classicGodSlayerInferno))
                    target.AddBuff(classicGodSlayerInferno.Type, 180);
            }
        }
    }
}
