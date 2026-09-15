using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Players;
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
    /// 亵渎之魂水晶·魔法转化的巨型圣光爆弹（移植自灾厄 2.2.2 的 ProfanedCrystalMageFireball，贴图源自 HolyBlast）。
    /// 使用魔法武器时额外发射：每发消耗 100 倍魔力消耗的魔力。
    /// 冷却按原版：派发端发完把计数器置 20/25 帧并逐帧递减，场上没有爆弹与裂片时计数器提前清零
    /// （所以"场上已有爆弹/裂片"并不是硬闸门，魔力与那 20/25 帧计数器才是）。
    /// 飞行 75 帧后接触地形，命中敌人或耗尽时间都会炸成外圈 10~16 + 内圈 8~12 枚裂片（Enraged 及以上档更多更快）。
    /// 伤害按通用伤害折算（originalDamage 为未折算的基础值 1800，由派发端写入）。
    /// </summary>
    internal class ProfanedCrystalMageFireball:ModProjectile
    {
        /// <summary>夜晚贴图路径（白天直接用类名对应的贴图）</summary>
        private const string NightTexture = "CalamityDemutation/Content/Projectiles/Summon/ProfanedCrystalMageFireballNight";
        /// <summary>注册 4 帧动画、可被右键锁定目标、5 帧残影缓存</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        /// <summary>
        /// 炸裂成裂片：外圈 outerSplits 枚（Enraged 及以上 16、否则 10）、内圈 innerSplits 枚（12 / 8），
        /// 内外圈错开半个间距形成双层环。命中时倍率 0.6（Enraged 0.3），未命中时 0.1（Enraged 0.2）——
        /// 空放惩罚很重。裂片的基础伤害为主弹基础值的 0.2 倍再乘该倍率，折算后回写 originalDamage。
        /// </summary>
        private void Split(bool hit, bool chaseable)
        {
            Player player = Main.player[Projectile.owner];
            bool enrage = player.GetModPlayer<CalamityDemutationPlayer>().pscState >= (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Empowered;
            // 只有真正命中时才滚一次追加长矛（空放传 0 = 不滚，对齐 2.2.2 的 rollBabSpears(hit ? 1 : 0, chaseable)）
            player.GetModPlayer<CalamityDemutationPlayer>().rollBabSpears(hit ? 1 : 0, chaseable);
            int outerSplits = enrage ? 16 : 10;
            int innerSplits = enrage ? 12 : 8;
            float mult = enrage ? 0.3f : 0.6f;
            if (!hit)
                mult = enrage ? 0.2f : 0.1f; // 空放惩罚很重
            int origDmg = (int)((Projectile.originalDamage * 0.2f) * mult);
            int damage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(origDmg);
            float outerAngleVariance = MathHelper.TwoPi / (float)outerSplits;
            float outerOffsetAngle = MathHelper.Pi / (2f * outerSplits);
            float innerAngleVariance = MathHelper.TwoPi / (float)innerSplits;
            float innerOffsetAngle = MathHelper.Pi / (2f * innerSplits);
            Vector2 outerPosVec = new Vector2(8f, 0f).RotatedByRandom(MathHelper.TwoPi);
            Vector2 innerPosVec = new Vector2(5f, 0f).RotatedByRandom(MathHelper.TwoPi); // 两个向量在循环里会被反复重赋值
            for (int i = 0; i < outerSplits; i++) // 按外圈次数循环（它一定不少于内圈）
            {
                outerPosVec = outerPosVec.RotatedBy(outerAngleVariance);
                Vector2 velocity = new Vector2(outerPosVec.X, outerPosVec.Y).RotatedBy(outerOffsetAngle);
                velocity.Normalize();
                velocity *= 8f;
                int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + outerPosVec, velocity, ModContent.ProjectileType<ProfanedCrystalMageFireballSplit>(), damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
                if (Main.projectile.IndexInRange(proj))
                {
                    Main.projectile[proj].DamageType = DamageClass.Generic;
                    Main.projectile[proj].originalDamage = origDmg;
                }
                if (innerSplits > 0) // 内圈还有剩余时才生成
                {
                    innerPosVec = innerPosVec.RotatedBy(innerAngleVariance);
                    velocity = new Vector2(innerPosVec.X, innerPosVec.Y).RotatedBy(innerOffsetAngle);
                    velocity.Normalize();
                    velocity *= 5f;
                    proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + innerPosVec, velocity, ModContent.ProjectileType<ProfanedCrystalMageFireballSplit>(), damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
                    if (Main.projectile.IndexInRange(proj))
                    {
                        Main.projectile[proj].DamageType = DamageClass.Generic;
                        Main.projectile[proj].originalDamage = origDmg;
                    }
                }
                innerSplits--;
            }
        }
        /// <summary>基础属性：180x180 碰撞箱、友方、穿透 1 次、不撞地形（前 25 帧）、存活 75 帧、0.6 缩放</summary>
        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 75;
            Projectile.minion = true;
            Projectile.scale = 0.6f;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>4 帧循环动画（每 6 帧切一帧）；存活 50 帧时开启地形碰撞、落水即灭、朝向跟随速度</summary>
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
                Projectile.tileCollide = true;
            if (Projectile.wet && !Projectile.lavaWet && Projectile.timeLeft < 70) // timeLeft 判定是因为首帧时 lavawet 还没刷新
                Projectile.Kill();
            Projectile.rotation = Projectile.velocity.ToRotation();
            return false;
        }
        /// <summary>AI：每帧按通用伤害重算 damage，并原地留一颗静止的神圣色粉尘（形成拖尾光晕）</summary>
        public override void AI()
        {
            int dustID = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            int num469 = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, dustID, 0f, 0f, 100, default, 1f);
            Main.dust[num469].noGravity = true;
            Main.dust[num469].velocity *= 0f;
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
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
        /// <summary>命中敌人：本地玩家端炸出裂片，然后本弹幕直接失活（不再走 OnKill 的空放分支）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == Projectile.owner)
                Split(true, target.chaseable);
            Projectile.active = false;
        }
        /// <summary>命中玩家（PvP）：逻辑同命中敌人</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (info.Damage <= 0)
                return;
            if (Main.myPlayer == Projectile.owner)
                Split(true, true);
            Projectile.active = false;
        }
        /// <summary>
        /// 耗尽时间自毁：播放原版 SoundID.Item20（魔法射击）并按昼夜喷两轮粉尘；
        /// ai[1] == 0（未被命中而自然消亡）时补一次"空放炸裂"，把伤害惩罚传给裂片。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner && Projectile.ai[1] == 0f)
                Split(false, false);
            SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
            int dustID = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            for (int num193 = 0; num193 < 6; num193++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 50, default, 1.5f);
            }
            for (int num194 = 0; num194 < 60; num194++)
            {
                int num195 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 0, default, 2.5f);
                Main.dust[num195].noGravity = true;
                Main.dust[num195].velocity *= 3f;
                num195 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, 0f, 0f, 50, default, 1.5f);
                Main.dust[num195].velocity *= 2f;
                Main.dust[num195].noGravity = true;
            }
        }
    }
}
