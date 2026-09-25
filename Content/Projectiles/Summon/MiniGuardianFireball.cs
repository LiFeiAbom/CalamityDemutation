using System;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 迷你神圣守卫的圣光爆弹（移植自灾厄 2.2.2 的 MiniGuardianFireball，贴图源自 HolyBlast）：
    /// 由进攻守护者的爆弹阶段发射（强化档 3 连发、平时单发，均带预测瞄准）。
    /// 飞行 75 帧、前 25 帧不撞地形，命中敌人/玩家或耗尽时间都会炸成裂片
    /// （ai[0] == 1 时 4 枚、否则 8 枚，伤害为母弹的 3/4）。伤害按通用伤害折算。
    /// </summary>
    internal class MiniGuardianFireball:ModProjectile
    {
        /// <summary>夜晚贴图路径（白天直接用类名对应的贴图）</summary>
        private const string NightTexture = "CalamityDemutation/Content/Projectiles/Summon/MiniGuardianFireballNight";
        /// <summary>注册 4 帧动画、可被右键锁定目标、5 帧残影缓存</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        /// <summary>
        /// 炸成裂片：ai[0] == 0 时 8 枚、否则 4 枚，绕一圈均匀铺开，速度 5 并按母弹速度的 1/4 叠加惯性；
        /// 裂片基础伤害为母弹基础值的 0.75 倍，折算后回写 originalDamage（漏写会导致裂片伤害被二次折算）。
        /// </summary>
        private void Split()
        {
            int totalProjectiles = Projectile.ai[0] == 0f ? 8 : 4;   // ai[0] 由派发端写入：0 = 普通 8 枚、非 0 = 强化 4 枚
            float radians = MathHelper.TwoPi / totalProjectiles;     // 相邻裂片的角度间隔（整圆均分）
            int type = ModContent.ProjectileType<MiniGuardianFireballSplit>();
            float velocity = 5f;   // 裂片的基准速度
            Vector2 spinningPoint = new Vector2(0f, -velocity);   // 以正上方为起点铺开
            int splitBaseDamage = (int)Math.Round(Projectile.originalDamage * 0.75);
            int splitDamage = (int)Main.player[Projectile.owner].GetTotalDamage<GenericDamageClass>().ApplyTo(splitBaseDamage);
            for (int k = 0; k < totalProjectiles; k++)
            {
                Vector2 velocity2 = spinningPoint.RotatedBy(radians * k);
                int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity2 + Projectile.velocity * 0.25f, type, splitDamage, 0f, Projectile.owner, 1f);   // 叠加母弹 1/4 的惯性
                if (Main.projectile.IndexInRange(proj))
                {
                    Main.projectile[proj].DamageType = DamageClass.Generic;
                    Main.projectile[proj].originalDamage = splitBaseDamage;
                }
            }
        }
        /// <summary>基础属性：180x180 碰撞箱、友方、穿透 1 次、不撞地形（前 25 帧）、存活 75 帧、0.4 缩放</summary>
        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 75;
            Projectile.minion = true;
            Projectile.scale = 0.4f;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>4 帧循环动画（每 6 帧切一帧）；存活 50 帧时开启地形碰撞；朝向跟随速度</summary>
        public override bool PreAI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            if (Projectile.timeLeft == 50)
                Projectile.tileCollide = true;   // timeLeft 自 75 递减，剩 50 时（约飞了 25 帧）才撞地形
            Projectile.rotation = Projectile.velocity.ToRotation();
            return false;
        }
        /// <summary>AI：每帧按通用伤害重算 damage，并原地留一颗静止的神圣色粉尘（形成拖尾光晕）</summary>
        public override void AI()
        {
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            int num469 = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, MiniGuardianHealer.HolyDustType(!Main.dayTime), 0f, 0f, 100, default, 1f);
            Main.dust[num469].noGravity = true;
            Main.dust[num469].velocity *= 0f;
        }
        /// <summary>昼夜配色（白天橙红 / 夜晚青蓝）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha);
        }
        /// <summary>绘制：夜晚换用 Night 贴图，先画十向背光再按当前帧画本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Main.dayTime ? TextureAssets.Projectile[Type].Value : ModContent.Request<Texture2D>(NightTexture, AssetRequestMode.ImmediateLoad).Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            int frameY = frameHeight * Projectile.frame;
            Rectangle frame = new Rectangle(0, frameY, texture.Width, frameHeight);
            Projectile.DrawBackglow(MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha, true), 4f, texture);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), frame, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>命中敌人：本地玩家端炸出裂片，播放原版 SoundID.DD2_BetsyFireballImpact（替代灾厄的圣光爆弹命中音）并失活</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == Projectile.owner)
                Split();
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.Center);
            Projectile.active = false;
        }
        /// <summary>命中玩家（PvP）：逻辑同命中敌人</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.Damage <= 0)
                return;
            if (Main.myPlayer == Projectile.owner)
                Split();
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.Center);
            Projectile.active = false;
        }
        /// <summary>耗尽时间自毁：本地玩家端炸出裂片，播放命中音并按昼夜喷两轮粉尘（夜晚更小更少）</summary>
        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
                Split();
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.Center);
            int dustID = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            for (int num193 = 0; num193 < 6; num193++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 50, default, Main.dayTime ? 1.5f : 0.5f);
            }
            for (int num194 = 0; num194 < 60; num194++)
            {
                int num195 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 0, default, Main.dayTime ? 2.5f : 0.5f);
                Main.dust[num195].noGravity = true;
                Main.dust[num195].velocity *= 3f;
                num195 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 50, default, Main.dayTime ? 1.5f : 0.5f);
                Main.dust[num195].velocity *= 2f;
                Main.dust[num195].noGravity = true;
            }
        }
    }
}
