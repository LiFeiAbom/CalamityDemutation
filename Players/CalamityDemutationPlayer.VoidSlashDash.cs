using CalamityDemutation.Content.Projectiles.Melee;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 虚空斩突进（VoidSlash，虚空分形右键发动）的玩家侧效果，对应 CE 的 <c>EModPlayer</c> 里两段以
    /// 「场上存在 VoidSlash 且 <c>d &lt; 16</c>」为条件的逻辑：
    /// ① <c>PostUpdateMiscEffects</c> —— 突进期间重力归零、隐形，并把无敌帧抬到 8；
    /// ② <c>SetControls</c> —— 突进期间屏蔽上下左右四个方向键（突进方向完全由弹幕决定，不接受玩家输入）。
    /// <para>
    /// 与 CE 的差异：① CE 在 SetControls 里先把四个 control 存进自己的 <c>cDown/cLeft/cRight/cUp</c> 字段
    /// （供它其它功能在后续帧复用），本模组没有那些功能，故只做"屏蔽"、不做"暂存"；
    /// ② CE 的 SetControls 没判归属，任一玩家在突进时所有人的方向键都会被屏蔽（多人下的实际瑕疵），
    /// 这里按本工程既有的改法补上 <c>owner</c> 判定（单机行为与 CE 完全一致）；
    /// ③ CE 那套无敌帧走它自研的 <c>immune</c> 整数（单调取大、每帧 -1），本模组按
    /// <see cref="CalamityDemutationPlayer.ShieldSlamDash"/> 与 GodSlayerDash 的既有写法直接用
    /// <c>Player.immuneTime</c> 抬下限。
    /// </para>
    /// </summary>
    internal partial class CalamityDemutationPlayer:ModPlayer
    {
        /// <summary>虚空斩突进期间给玩家的碰撞免疫帧数（CE 的 immune 上限值 8）</summary>
        private const int VoidSlashDashImmunityFrames = 8;
        /// <summary>突进期间的玩家侧结算，由主类的 <c>PostUpdateMiscEffects</c> 末尾调用</summary>
        public void VoidSlashDashPlayerEffects()
        {
            if (!VoidSlashDashIsActive())
            {
                return;
            }
            Player.gravity = 0f;
            Player.invis = true;
            Player.immune = true;
            if (Player.immuneTime < VoidSlashDashImmunityFrames)
            {
                Player.immuneTime = VoidSlashDashImmunityFrames;
            }
            for (int i = 0; i < Player.hurtCooldowns.Length; i++)
            {
                if (Player.hurtCooldowns[i] < VoidSlashDashImmunityFrames)
                {
                    Player.hurtCooldowns[i] = VoidSlashDashImmunityFrames;
                }
            }
        }
        /// <summary>突进期间屏蔽四个方向键（跳/下落/左右），位移交给 VoidSlash 每帧钉住玩家中心</summary>
        public override void SetControls()
        {
            if (!VoidSlashDashIsActive())
            {
                return;
            }
            Player.controlDown = false;
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
        }
        /// <summary>本玩家名下是否有仍在突进前段的 VoidSlash（<c>d &lt; 16</c>，超过就进入收招、交还操控）</summary>
        private bool VoidSlashDashIsActive()
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<VoidSlash>()] <= 0)
            {
                return false;
            }
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == Player.whoAmI && projectile.ModProjectile is VoidSlash slash && slash.d < 16)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
