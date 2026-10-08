using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 半影掷出的暗影炸弹（照灾厄 2.0 的 <c>PenumbraBomb</c>）：
    /// 32×32 判定、存活 150 帧、<c>extraUpdates = 2</c>、不撞物块不撞水、贴图借用物品贴图；
    /// 逐帧淡入到 alpha 10，边飞边按速度朝向（含绘制偏移修正），沿途每 2 帧（潜行弹每 4 帧、多加一颗）撒暗影火尘。
    /// </summary>
    /// <remarks>
    /// 炸开（<c>OnKill</c>）：先沿圆周迸出**会追踪的暗影魂**——常规每 60° 一枚（共 6 枚、每枚伤害 ×0.15），
    /// 潜行弹每 40° 一枚（共 9 枚、每枚 ×0.08），整体带一个 ±30° 的随机相位；
    /// 然后撒 70（潜行 100）颗暗影火尘、播爆炸音，最后把判定框撑到 110 并**以半伤补结算一次**。
    /// 弹幕生成只在主人端做（第 5 节口径）。
    /// </remarks>
    internal class PenumbraBomb : ModProjectile
    {
        /// <summary>常规炸弹的撒魂角间隔（度）</summary>
        private const int SoulStepNormal = 60;
        /// <summary>潜行炸弹的撒魂角间隔（度）</summary>
        private const int SoulStepStealth = 40;
        /// <summary>常规撒魂的伤害倍率</summary>
        private const float SoulDamageNormal = 0.15f;
        /// <summary>潜行撒魂的伤害倍率</summary>
        private const float SoulDamageStealth = 0.08f;
        /// <summary>爆炸补结算时的判定框边长（照源）</summary>
        private const int ExplosionSize = 110;
        /// <summary>暗影火尘色（源的固定色）</summary>
        private static readonly Color ShadowColor = new Color(38, 30, 43);

        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/Penumbra";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>32×32、存活 150 帧、两倍额外更新、不撞物块不撞水、全透明起步</summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 2;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>淡入 + 绘制偏移修正 + 按速度朝向 + 定时撒尘（潜行弹每 4 帧多撒一颗）</summary>
        public override void AI()
        {
            Projectile.ai[0]++;
            if (Projectile.direction == 1)
            {
                DrawOffsetX = -4;
                DrawOriginOffsetX = -5;
            }
            else
            {
                DrawOffsetX = -11;
                DrawOriginOffsetX = 5;
            }
            if (Projectile.alpha > 10)
                Projectile.alpha -= 7;
            else
                Projectile.alpha = 10;
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) + MathHelper.ToRadians(180) * Projectile.direction;

            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            float dustFrequency = stealthStrike ? 4f : 2f;
            if (Projectile.ai[0] == dustFrequency)
            {
                Vector2 dustSpeed = Projectile.velocity * Main.rand.NextFloat(0.5f, 0.8f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, dustSpeed.X, dustSpeed.Y, 0, ShadowColor, 1.4f);
                Main.dust[dust].velocity = dustSpeed;
                if (stealthStrike)
                {
                    Vector2 extraSpeed = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f));
                    int extraDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, extraSpeed.X, extraSpeed.Y, 0, ShadowColor, 1.3f);
                    Main.dust[extraDust].velocity = extraSpeed;
                }
                Projectile.ai[0] = 0f;
            }
        }
        /// <summary>炸开：撒魂（主人端）→ 暗影火尘 → 爆炸音 → 撑判定框并按半伤补结算一次</summary>
        public override void OnKill(int timeLeft)
        {
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            if (Projectile.owner == Main.myPlayer)
            {
                int soulStep = stealthStrike ? SoulStepStealth : SoulStepNormal;
                float soulDamageMultiplier = stealthStrike ? SoulDamageStealth : SoulDamageNormal;
                int randomRotation = Main.rand.Next(-30, 31);
                for (int angle = 0; angle < 360; angle += soulStep)
                {
                    Vector2 soulSpeed = new Vector2(13f, 13f).RotatedBy(MathHelper.ToRadians(angle + randomRotation));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, soulSpeed, ModContent.ProjectileType<PenumbraSoul>(), (int)(Projectile.damage * soulDamageMultiplier), 3f, Projectile.owner, 0f, 0f);
                }
            }
            int maxDust = stealthStrike ? 100 : 70;
            for (int i = 0; i < maxDust; i++)
            {
                Vector2 dustSpeed = new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f));
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, dustSpeed.X, dustSpeed.Y, 0, ShadowColor, 1.6f);
            }
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = ExplosionSize;
            Projectile.position -= Projectile.Size * 0.5f;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.damage /= 2;
            Projectile.Damage();
        }
        /// <summary>拖影（照源调灾厄的 DrawAfterimagesCentered，本工程用同名 CDUtil 版本）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>PvP 命中挂 5 秒「失明」（照源）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Blackout, 300);
        }
    }
}
