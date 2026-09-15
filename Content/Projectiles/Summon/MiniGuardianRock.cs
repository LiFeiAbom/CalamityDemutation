using System;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 迷你神圣守卫的环绕岩石（移植自灾厄 2.2.2 的 MiniGuardianRock，贴图原为 ProfanedRocks 系列）：
    /// 由 MiniGuardianDefense 的护盾在场时召出，ai[0]==0 时绕主人旋转；护盾消失时被标记 ai[0]=1 甩向敌人，
    /// 之后进入 ai[0]==2 的飞行衰减段并在 300 帧后消失。命中一次即穿透耗尽。
    /// 贴图按 ai[2] 取 6 种岩石外观之一（见 GetRockTexture）。
    /// </summary>
    internal class MiniGuardianRock:ModProjectile
    {
        /// <summary>弹幕主人：环绕基准与甩出方向都以主人为参照</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>注册 4 帧动画、登记 4 帧残影缓存，并标记为可右键锁定目标的召唤物</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }
        /// <summary>基础属性：50x50 碰撞箱、友方、穿透 1 次、不碰撞地形、忽略水面；静态 NPC 免疫每 1 帧共用冷却</summary>
        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true; //the sounds get grating otherwise
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.minion = true;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 1;
        }
        /// <summary>
        /// 岩石贴图：对应灾厄的 ProfanedRocks.Textures[rockType - 1]，而 ProfanedRocks 的第 i 张是
        /// "ProfanedRocks" + (i + 1)，故 rockType n 实际对应后缀 n（本工程即同目录的 MiniGuardianRock n）；
        /// rockType 为 0（不应出现）时回退到无后缀的 MiniGuardianRock。
        /// </summary>
        private static Texture2D GetRockTexture(int rockType)
        {
            string suffix = rockType <= 0 ? "" : rockType.ToString();
            return ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/MiniGuardianRock" + suffix).Value;
        }
        /// <summary>
        /// AI：每帧按召唤伤害重算 damage；主人持有神器（profanedSoulArtifact）期间才存在，否则清标志并消散；
        /// ai[0]==0 绕主人旋转（半径 50、每帧转 2 度，水晶形态为半径 80、每帧反向 2 度、按 10 等分）；
        /// ai[0]==1 沿远离主人方向甩出（水晶增益下速度 25、否则 20）；ai[0]==2 头 25 帧衰减减速并喷尘。
        /// </summary>
        public override void AI()
        {
            // 伤害在此实时刷新（originalDamage 由召唤源写入）
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
            // 守护者标志置位期间持续续期，实现"神器在身即常驻"（已被甩出的岩石 ai[0]!=0 不再续期）
            if (modPlayer.profanedSoulGuardians && Projectile.ai[0] == 0f)
                Projectile.timeLeft = 4;
            // 神器消失 / 主人死亡 / 装了水晶却没进入水晶态（四态 < Buffs，例如水晶只戴在时装栏）：
            // 清掉守护者标志并让岩石消散（最后一条对齐 2.2.2 的 profanedCrystal && !profanedCrystalBuffs）
            if (!modPlayer.profanedSoulArtifact || Owner.dead || !Owner.active || (modPlayer.profanedCrystal && !modPlayer.profanedCrystalBuffs))
            {
                modPlayer.profanedSoulGuardians = false;
                Projectile.active = false;
                return;
            }
            if (Projectile.ai[0] == 0f) //regular expected behaviour of floaty rocks
            {
                // 环绕旋转（对齐 2.2.2）：水晶形态（pscState > 0）半径 80、每帧 +2 度；神器形态半径 50、每帧 -2 度。
                // 岩石之间的等分间隔由生成端的 2π/岩石数 决定（水晶 10 颗 / 神器 5 颗）
                // （原代码另算了 rotationVelocityIncrease 与 angle 两个局部量，但从未被读取，此处略去不影响任何数值）
                bool crystal = modPlayer.pscState > 0;
                float distance = 50f + (crystal ? 30f : 0f);
                Projectile.Center = Owner.Center + Projectile.ai[1].ToRotationVector2() * distance;
                Projectile.rotation = Projectile.ai[1] + (float)Math.Atan(90);
                Projectile.ai[1] += MathHelper.ToRadians(crystal ? 2f : -2f);
            }
            else if (Projectile.ai[0] == 1f) //rock yeetage begins
            {
                // 甩出（对齐 2.2.2）：有水晶鞭增益时改为"预测瞄准直射目标"，并把 ai[0] 置 3——3 不进入
                // ai[0]==2 的减速段，等于全速冲过去；否则沿"远离主人"方向甩出，速度按水晶增益取 25 / 神器档 20
                NPC target = MiniGuardianTargeting.MinionHoming(Projectile.Center, 2000f, Owner);
                if (Owner.HasBuff<ProfanedCrystalWhipBuff>() && target != null)
                {
                    Projectile.velocity = MiniGuardianAttack.CalculatePredictiveAimToTarget(Projectile.Center, target, 32f);
                    Projectile.ai[0] = 3f;
                }
                else
                {
                    Projectile.velocity = Projectile.Center - Owner.Center;
                    Projectile.velocity.Normalize();
                    Projectile.velocity *= modPlayer.profanedCrystalBuffs ? 25f : 20f;
                    Projectile.ai[0] = 2f;
                }
                Projectile.timeLeft = 300;
            }
            else if (Projectile.ai[0] == 2f) //rocks have been yeeted, handle the aftermath
            {
                // 甩出后的头 25 帧减速，并在前 15 帧额外多喷一颗尘
                if (Projectile.timeLeft > 275) //slow them down a little
                    Projectile.velocity *= 0.9725f;
                for (int i = 0; i < 2; i++)
                {
                    if (i == 0 || Projectile.timeLeft > 285)
                        Dust.NewDust(Projectile.position, Projectile.width / 2, Projectile.height / 2, MiniGuardianHealer.HolyDustType(false), 0f, -1f, 0, default, 1f);
                }
            }
        }
        /// <summary>环绕期间（ai[0]==0）不造成伤害，甩出后才可命中</summary>
        public override bool? CanDamage() => Projectile.ai[0] >= 1f ? null : false;
        /// <summary>
        /// 绘制：按 ai[2] 选岩石外观，随与主人的距离在 0.35→0.42→1 两段插值放大，缩放的距离档位按水晶增益分档
        /// （原灾厄还用外观染料 Owner.cMinion，本工程未移植该染料，固定 0 即不着色）。
        /// 甩出后（ai[0]>=1）逐帧叠画残影；原灾厄的残影开关 CalamityClientConfig.Afterimages
        /// 用本工程的性能模式配置（ConfigSystem.PerformanceMode）等价替代。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // 原为灾厄玩家字段 Owner.cMinion（仆从外观染料着色器），本工程未移植外观染料，固定 0（不着色）
            int dye = 0;
            // 水晶增益生效时岩石画得更大（72→87 / 87→150，神器档 42→57 / 57→120），见下面的缩放段
            bool crystalBuffed = Owner.GetModPlayer<CalamityDemutationPlayer>().profanedCrystalBuffs;
            int rockType = (int)MathHelper.Clamp(Projectile.ai[2], 1f, 6f);
            Texture2D texture = GetRockTexture(rockType);
            Vector2 drawOrigin = new Vector2(texture.Width / 2, texture.Height / 2);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            drawPos -= new Vector2(texture.Width, texture.Height) * Projectile.scale / 2f;
            drawPos += drawOrigin * Projectile.scale + new Vector2(0f, Projectile.gfxOffY);
            Rectangle frame = new Rectangle(0, 0, texture.Width, texture.Height);
            // 离主人越远画得越大：42→57（水晶 72→87）由 0.35 渐变到 0.42，再 57→120（水晶 87→150）渐变到 1
            float ownerDist = Projectile.Center.Distance(Owner.Center);
            float lerpVal = Utils.GetLerpValue(crystalBuffed ? 72 : 42, crystalBuffed ? 87 : 57, ownerDist, true);
            float mult = MathHelper.Lerp(0.35f, 0.42f, lerpVal);
            if ((crystalBuffed && ownerDist > 87f) || (!crystalBuffed && ownerDist > 57f))
            {
                lerpVal = Utils.GetLerpValue(crystalBuffed ? 87 : 57, crystalBuffed ? 150 : 120, ownerDist, true);
                mult = MathHelper.Lerp(0.42f, 1f, lerpVal);
            }
            if (ConfigSystem.Instance?.PerformanceMode != true && Projectile.ai[0] >= 1f)  //handle afterimages manually since the utility broke it and didn't render correctly
            {
                for (int i = 0; i < Projectile.oldPos.Length; ++i)
                {
                    drawPos = Projectile.oldPos[i] + (Projectile.Size / 2f) - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                    // DO NOT REMOVE THESE "UNNECESSARY" FLOAT CASTS. THIS WILL BREAK THE AFTERIMAGES.
                    Color color = Projectile.GetAlpha(lightColor) * ((float)(Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);
                    color *= mult;
                    var drawData = new DrawData(texture, drawPos, frame, color)
                    {
                        rotation = Projectile.rotation,
                        origin = drawOrigin
                    };
                    GameShaders.Armor.Apply(dye, Projectile, drawData);
                    Main.spriteBatch.Draw(texture, drawPos, frame, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0f);
                }
            }
            else
            {
                var color = Color.White * mult;
                var drawData = new DrawData(texture, drawPos, frame, color)
                {
                    rotation = Projectile.rotation,
                    origin = drawOrigin
                };
                GameShaders.Armor.Apply(dye, Projectile, drawData);
                Main.spriteBatch.Draw(texture, drawPos, frame, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }
        /// <summary>消亡时喷 10 颗神圣色粉尘</summary>
        public override void OnKill(int timeLeft)
        {
            for (int k = 0; k < 10; k++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, MiniGuardianHealer.HolyDustType(false), 0f, -1f, 0, default, 1f);
        }
    }
}
