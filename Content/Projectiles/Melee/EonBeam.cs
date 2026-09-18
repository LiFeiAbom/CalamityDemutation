using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 永恒光束 - 方舟系列武器的主弹幕
    /// 高速飞行、可穿透 3 个敌人，渲染颜色由 ai[1] 决定（随武器随机赋予）
    /// </summary>
    internal class EonBeam:ModProjectile
    {
        /// <summary>通过引用把 ai[1] 当作贴图/颜色选择器（1~4 对应不同颜色）</summary>
        public ref float UseTexture => ref Projectile.ai[1];
        /// <summary>
        /// 基础属性：20x20 碰撞箱；友方、近战伤害、可穿透 3 个敌人、存活 500 帧；
        /// 每个敌人独立 10 帧命中冷却、每帧额外更新 1 次提升弹速
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 500;
            Projectile.usesLocalNPCImmunity = true;   // 对每个敌人独立计算命中免疫
            Projectile.localNPCHitCooldown = 10;      // 同一敌人每 10 帧最多受击一次
            Projectile.extraUpdates = 1;              // 额外更新使弹速更快
        }
        /// <summary>
        /// AI：贴图随飞行方向旋转；localAI[1] 累计存在帧数；每帧在弹头前缘喷随机神圣系粉尘，
        /// 超过 7 帧后再补喷彩虹拖尾尘，并持续叠加青蓝色光照
        /// </summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.localAI[1] += 1f;
            // 随机生成三种神圣系粉尘之一
            int dustID = Main.rand.Next(3) switch { 0 => 15, 1 => 57, _ => 58 };
            int num225 = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X * 4f + 2f, Projectile.position.Y + 2f - Projectile.velocity.Y * 4f), 8, 8, dustID, 0f, 0f, 100, default, 1.25f);
            Dust dust59 = Main.dust[num225];
            Dust dust3 = dust59;
            dust3.velocity *= 0.1f;
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.3f / 255f, (255 - Projectile.alpha) * 0.4f / 255f, (255 - Projectile.alpha) * 1f / 255f);
            if (Projectile.localAI[1] > 7f)
            {
                int dType = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, new Color(53, Main.DiscoG, 255), 1.2f);
                Main.dust[dType].velocity *= 0.1f;
                Main.dust[dType].noGravity = true;
            }
        }
        /// <summary>
        /// 命中敌人：ai[0] 为 1（真·远古方舟光束）时跳过附加 debuff；否则施加霜火并补灾厄
        /// 现代版/经典版的硫磺火、神圣火/圣光、瘟疫等元素 debuff 各 120 帧
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ai[0] == 1 时跳过 debuff 施加（真·远古方舟的光束不附加元素 debuff）
            if (Projectile.ai[0] != 1f)
            {
                target.AddBuff(BuffID.Frostburn, 120);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 120); }
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 120); }
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 120); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 120); }
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 120); }
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 120); }
                }
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：效果与命中 NPC 相同（真·远古方舟光束除外）
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.ai[0] != 1f) //excludes True Ark of the Ancients
            {
                target.AddBuff(BuffID.Frostburn, 120);
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
                {
                    if (calamity0.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 120); }
                    if (calamity0.TryFind<ModBuff>("HolyFlames", out ModBuff holyFlames)) { target.AddBuff(holyFlames.Type, 120); }
                    if (calamity0.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 120); }
                }
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
                {
                    if (calamity1.TryFind<ModBuff>("BrimstoneFlames", out ModBuff brimstoneFlames)) { target.AddBuff(brimstoneFlames.Type, 120); }
                    if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight)) { target.AddBuff(holyLight.Type, 120); }
                    if (calamity1.TryFind<ModBuff>("Plague", out ModBuff plague)) { target.AddBuff(plague.Type, 120); }
                }
            }
        }
        /// <summary>
        /// 消亡：迸发 7 粒青色（迪斯科绿）彩虹光尘作为消散特效
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            // 消失时迸发一圈青色光尘
            for (int k = 0; k < 7; k++)
            {
                int dType = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, 0f, 0f, 150, new Color(53, Main.DiscoG, 255), 1.2f);
                Main.dust[dType].noGravity = true;
            }
        }
        /// <summary>
        /// 自定义绘制：生成后前 10 帧不绘制避免贴图跳变；按 ai[1] 选取光束颜色（1紫/2青/3天蓝/4橙）并旋转绘制
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // 生成后前 10 帧不绘制（避免出生瞬间的贴图跳变）
            if (Projectile.timeLeft > 490)
                return false;
            // 按 ai[1] 选择光束颜色：紫 / 青 / 天蓝 / 橙
            UseTexture = MathHelper.Clamp(UseTexture, 1f, 4f);
            Color color;
            if (UseTexture == 1f)
            {
                color = Color.Violet;
            }
            else if (UseTexture == 2f)
            {
                color = Color.Turquoise;
            }
            else if (UseTexture == 3f)
            {
                color = Color.SkyBlue;
            }
            else
            {
                color = Color.Orange;
            }
            Projectile.GetProjDrawInfo_Melee(out Texture2D texture, out Vector2 drawPos, out float drawRot, out Vector2 orig, out SpriteEffects spriteEffects);
            Main.EntitySpriteDraw(texture, drawPos, null, Projectile.GetAlpha(color), drawRot - MathHelper.PiOver4, texture.Size() / 2, Projectile.scale, spriteEffects, 0f);
            return false;
        }
        /// <summary>
        /// 返回 PreDraw 按 ai[1] 选好的颜色（透明度跟随 alpha），使选色链路生效——原先固定青色会让选色整体失效
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return lightColor;
        }
    }
}
