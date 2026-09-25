using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 友方海妖之歌：由海妖诱饵（SirenLure）发射的声波弹，持续减速，尺寸做呼吸脉冲并附带蓝光。
    /// </summary>
    internal class SirenSongFriendly:ModProjectile
    {
        /// <summary>
        /// 基础属性：召唤物伤害、单次穿透、持续较长。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
        }
        /// <summary>
        /// 弹幕 AI：逐步减速、尺寸呼吸脉冲、初次发射播放音效并发出蓝光。
        /// </summary>
        public override void AI()
        {
            // ai[1]：0 = 尚未播放入场音效（首帧置 1 后再不触发）
            // localAI[0]：呼吸相位，0 = 正在膨胀（0.75→1.25），1 = 正在收缩（1.25→0.75）
            Projectile.velocity.X *= 0.985f;
            Projectile.velocity.Y *= 0.985f;
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale += 0.02f;
                if (Projectile.scale >= 1.25f)
                {
                    Projectile.localAI[0] = 1f;
                }
            }
            else if (Projectile.localAI[0] == 1f)
            {
                Projectile.scale -= 0.02f;
                if (Projectile.scale <= 0.75f)
                {
                    Projectile.localAI[0] = 0f;
                }
            }
            if (Projectile.ai[1] == 0f)
            {
                Projectile.ai[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item26, Projectile.position);
            }
            Lighting.AddLight(Projectile.Center, 0f, 0f, 1.2f);
        }
        /// <summary>
        /// 纯白着色且 A 通道恒为 0（仅保留 RGB），用于实现"半透明音波"叠加质感
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 0);
        }
    }
}
