using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 幻影妖龙（PhantomWyrm，噬渊鞭挞 Ystralyn 的召唤物）的玩家侧逻辑，对应 CE 的 <c>EModPlayer</c> 里两处：
    /// ① <c>ResetEffects</c> —— 每帧把 <c>wyrmPhantom</c> 标记复位（真正的复位语句放在主类 ResetEffects 里）；
    /// ② <c>PostUpdate</c> —— 标记为真且场上没有幻影妖龙时补生成一只。
    /// <para>
    /// 与 CE 的差异：生成时机从 <c>PostUpdate</c> 挪到主类的 <c>PostUpdateMiscEffects</c> 末尾调用——
    /// 本模组主类的 PostUpdate 被亵渎水晶的变身动画占着、开头就有两处 early return，挂在那里的话
    /// 不带水晶时反而永远跑不到；PostUpdateMiscEffects 才是本模组既有的"每帧结算中心"
    /// （虚空斩突进同样挂在那儿）。两处都在 Player.Update 内、都早于弹幕主循环，生成时点等价。
    /// </para>
    /// </summary>
    internal partial class CalamityDemutationPlayer:ModPlayer
    {
        /// <summary>虚无幻象标记：由 WyrmPhantom buff 每帧置位，主类 ResetEffects 每帧复位</summary>
        public bool wyrmPhantom = false;
        /// <summary>场上没有幻影妖龙且标记为真时补生成一只；由主类 <c>PostUpdateMiscEffects</c> 末尾调用</summary>
        public void WyrmPhantomSpawn()
        {
            if (!wyrmPhantom || Player.ownedProjectileCounts[ModContent.ProjectileType<PhantomWyrm>()] > 0)
            {
                return;
            }
            // 只在本地端生成：多人下各客户端都会跑这段，必须由主人自己发，否则同一条龙会被生成好几遍
            if (Main.myPlayer != Player.whoAmI)
            {
                return;
            }
            Vector2 randomVelocity = new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-14f, 14f));
            int damage = (int)Player.GetTotalDamage(DamageClass.Summon).ApplyTo(Ystralyn.PhantomDamage);
            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, randomVelocity, ModContent.ProjectileType<PhantomWyrm>(), damage, 1f, Player.whoAmI);
        }
    }
}
