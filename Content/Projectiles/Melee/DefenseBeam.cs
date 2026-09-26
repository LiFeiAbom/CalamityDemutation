using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 防御光束（DefenseBeam） - 防御之刃（DefenseBlade）的近战弹幕。
    /// 由 DefenseBlade 的 Item.shoot 发射，具备自动追踪能力。
    /// 命中/消亡时以 DefenseBeam.OnKill 爆炸，并散出 DefenseFlame 火舌弹幕形成二次伤害。
    /// </summary>
    internal class DefenseBeam:ModProjectile
    {
        /// <summary>
        /// 拖尾缓存：保留 13 个历史采样点；TrailingMode=2 表示同时记录 oldPos 与 oldRot，
        /// 供 PreDraw 中 PrimitiveRenderer 绘制平滑光带拖尾使用。
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 13;   // 拖尾历史点数
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;        // 2=记录位置与旋转
        }
        /// <summary>
        /// 弹幕基础属性：小型判定框、穿透 1 次、2 倍更新频率的友方近战弹幕。
        /// 初始 alpha=255（完全透明），在 AI 中逐帧递减实现淡入。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;      // 判定框宽（像素）
            Projectile.height = 20;     // 判定框高（像素）
            Projectile.friendly = true; // 友方弹幕，只伤害敌怪
            Projectile.penetrate = 1;   // 命中一次即消失（配合 OnKill 触发爆炸）
            Projectile.timeLeft = 120;  // 存活 120 帧
            Projectile.alpha = 255;     // 初始全透明，AI 中淡入
            Projectile.DamageType = DamageClass.Melee;  // 归属近战伤害，吃近战加成
            Projectile.MaxUpdates = 2;  // 每帧更新 2 次，等效 extraUpdates=2，飞行更快更顺滑
        }
        /// <summary>
        /// 运动逻辑：金色光照、自旋、淡入、一次性音效与金色尘粒，并持续追踪 1200 像素内的敌人。
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.25f, 0.25f, 0f);   // 金色光晕（RGB 中只给红绿）
            Projectile.rotation += 1f;                                 // 每帧固定自旋
            Projectile.alpha -= 25;                                    // 逐帧提高不透明度（淡入）
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;                                  // 上限：完全不透明
            }
            // localAI[0] 当一次性开关用：仅发射首帧播放一次声音
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item73, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            // 金色尘粒：随机初速度，随后被压到 0.5 倍并叠加弹幕方向的 0.1 倍，形成随弹幕拖行的尾迹
            int num458 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, new Color(255, Main.DiscoG, 53), 0.8f);
            Main.dust[num458].noGravity = true;
            Main.dust[num458].velocity *= 0.5f;
            Main.dust[num458].velocity += Projectile.velocity * 0.1f;
            // 追踪：1200 像素内锁定最近敌人，追踪速度 20、惯性 12（数值越大转向越迟钝）
            Projectile.HomeInNPC(1200f, 20f, 12f);//(1600,30,20)->(1200,20f,12f)
            // 以下为旧的备用追踪方案（已被上方 HomeInNPC 取代），仅作保留、不参与执行
            /*
            if (Projectile.timeLeft < 90)
            {
                NPC npc = Projectile.Center.FindClosestNPC(250);
                if (npc != null)
                    Projectile.ChasingBehavior2(npc.Center, 1.001f, 0.045f);
            }
            */
        }
        /// <summary>
        /// 消亡处理：以自身为中心做 32 半径的伤害爆炸，喷出金色尘粒，并生成 3~4 枚 DefenseFlame 火舌。
        /// 只有拥有者客户端（owner == Main.myPlayer）才负责生成子弹幕，避免多人重复生成。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.Explode(32, SoundID.Item20);   // 半径 32 的爆炸判定 + 指定音效
            // 31 颗金色尘粒向外飞散（d 从 0 到 30 闭区间）：先取随机方向，再归一化到 3~9 的随机速度
            for (int d = 0; d <= 30; d++)
            {
                float num463 = Main.rand.Next(-10, 11);
                float num464 = Main.rand.Next(-10, 11);
                float speed = Main.rand.Next(3, 9);
                float num466 = (float)Math.Sqrt((double)((num463 * num463) + (num464 * num464)));
                num466 = speed / num466;   // 归一化系数：让方向向量长度等于 speed
                num463 *= num466;
                num464 *= num466;
                int num467 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, new Color(255, Main.DiscoG, 53), 1.2f);
                Dust dust = Main.dust[num467];
                dust.noGravity = true;
                dust.position.X = Projectile.Center.X;       // 尘粒强制从爆心出发
                dust.position.Y = Projectile.Center.Y;
                dust.position.X += Main.rand.Next(-10, 11);  // 再加一层随机抖动，避免完全重合
                dust.position.Y += Main.rand.Next(-10, 11);
                dust.velocity.X = num463;
                dust.velocity.Y = num464;
            }
            int flameAmt = Main.rand.Next(3, 5);//(2,4)->(3,5)   // 生成 3~4 团火舌
            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < flameAmt; i++)
                {
                    // 随机方向速度（方向系数 100、速度 70~100、倍率 0.1）
                    Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<DefenseFlame>(),Projectile.damage, 0f, Projectile.owner, 0f, 0f);
                }
            }
        }
        /// <summary>
        /// 自定义绘制：屏蔽默认贴图绘制，改用 HeavenlyGaleTrail 着色器（Effects/HeavenlyGaleTrailShader.fx）
        /// 沿 oldPos 历史点渲染金色渐变光带拖尾。identity 偏移让不同弹幕的流光相位错开，观感更自然。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            float localIdentityOffset = Projectile.identity * 0.1372f;   // 用弹幕唯一 ID 打散相位
            // 随时间循环流动的主/副颜色（0.2 相位差形成双色流动）
            Color mainColor = CDUtil.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + localIdentityOffset) % 1f, Color.Gold, Color.White, Color.Goldenrod, Color.DarkGoldenrod, Color.Gold);
            Color secondaryColor = CDUtil.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + localIdentityOffset + 0.2f) % 1f, Color.Gold, Color.White, Color.Goldenrod, Color.DarkGoldenrod, Color.Gold);
            mainColor = Color.Lerp(Color.White, mainColor, 0.85f);        // 向白色混合，提亮整体
            secondaryColor = Color.Lerp(Color.White, secondaryColor, 0.85f);
            Vector2 trailOffset = Projectile.Size * 0.5f;                 // 拖尾相对顶点的偏移（居中）
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/GreyscaleGradients/EternityStreak"));
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseImage2("Images/Extra_189");
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseColor(mainColor);
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseSecondaryColor(secondaryColor);
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].Apply();
            // 沿历史位置渲染光带，采样 53 个点；smoothen 平滑、pixelate 关闭像素化
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, (float _, Vector2 _) => trailOffset, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"]), 53);
            return true;
        }
        /// <summary>
        /// 拖尾颜色函数：统一使用金色，透明度跟随弹幕淡入淡出。
        /// </summary>
        public Color PrimitiveColorFunction(float _, Vector2 vertexPosition) => Color.Gold * Projectile.Opacity;
        /// <summary>
        /// 拖尾宽度函数：整体宽度随弹幕缩放变化，基准 30 像素。
        /// </summary>
        public float PrimitiveWidthFunction(float completionRatio, Vector2 _) => Projectile.scale * 30f;
    }
}
