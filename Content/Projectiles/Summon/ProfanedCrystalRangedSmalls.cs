using Microsoft.Xna.Framework;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎之魂水晶·远程转化的小型圣光火球（移植自灾厄 2.2.2 的 ProfanedCrystalRangedSmalls）。
    /// 与陨石同一次触发里按 20%/30% 的分支生成：存活 240 帧、4 帧动画、持续缓慢加速（每帧 ×1.01），
    /// 第 210 帧起才开启地形碰撞与命中判定（在那之前 ai[0] == 1 的变体完全不可命中）。
    /// 伤害按通用伤害折算（originalDamage 为未折算的基础值 400，由派发端写入）。
    /// 命中敌人时按 1/50 的概率让主人的进攻守护者喷一圈"追加长矛"（rollBabSpears，见本类 OnHitNPC）。
    /// </summary>
    internal class ProfanedCrystalRangedSmalls:ModProjectile
    {
        /// <summary>注册 4 帧动画</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        /// <summary>基础属性：20x20 碰撞箱、1.5 缩放、友方、无视水、不撞地形、穿透 1 次、存活 240 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.5f;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>AI：每帧按通用伤害重算 damage、4 帧循环动画（每 4 帧切一帧）、持续缓慢加速，第 210 帧开启地形碰撞</summary>
        public override void AI()
        {
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            Projectile.velocity.X *= 1.01f;   // 每帧 +1%，缓慢加速
            Projectile.velocity.Y *= 1.01f;
            if (Projectile.timeLeft == 210)
                Projectile.tileCollide = true;   // 第 210 帧（射出约 30 帧后）才开启地形碰撞
        }
        /// <summary>命中敌人：小概率滚一次追加长矛（对齐 2.2.2 的 rollBabSpears(50, chaseable)）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(50, target.chaseable);
        }
        /// <summary>命中玩家（PvP）：同上，可追击恒真</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(50, true);
        }
        /// <summary>ai[0] == 1 的变体在第 210 帧之前不可命中</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[0] == 1f && Projectile.timeLeft > 210)
                return false;
            return null;
        }
        /// <summary>
        /// 自毁：播放原版 SoundID.Item14（爆炸）；1/3 概率把判定框临时放大到 50 见方，
        /// 再按昼夜喷两轮神圣色粉尘（白天更大更多、夜晚更小更少）。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            if (!Main.rand.NextBool(3)) // 1/3 概率喷粉尘
                return;
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            int dust = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            for (int num621 = 0; num621 < 10; num621++)
            {
                int num622 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[num622].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[num622].scale = 0.5f;
                    Main.dust[num622].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                }
            }
            for (int num623 = 0; num623 < 15; num623++)
            {
                int num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 3f : 0.75f);
                Main.dust[num624].noGravity = true;
                Main.dust[num624].velocity *= 5f;
                num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[num624].velocity *= 2f;
            }
        }
        /// <summary>昼夜配色（白天橙红 / 夜晚青蓝）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha);
        }
        /// <summary>绘制：先画十向背光再按当前帧画本体（本弹幕只有白天一套贴图）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            int frameY = frameHeight * Projectile.frame;
            Rectangle frame = new Rectangle(0, frameY, texture.Width, frameHeight);
            Projectile.DrawBackglow(MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha, true), 4f, texture);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), frame, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
