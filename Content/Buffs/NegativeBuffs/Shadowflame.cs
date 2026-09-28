using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 暗影焰（Shadowflame）—— 灾厄的暗影焰减益（照搬灾厄 2.0.4 的 Buffs/DamageOverTime/Shadowflame）。
    /// 与原版 <c>BuffID.ShadowFlame</c> 的分工：原版那条挂**敌怪**，本条的 <c>Update</c> 重载收的是
    /// <see cref="Player"/>，只在 PvP 命中时挂到**玩家**身上，故两条并存不冲突。
    /// 掉血在 <c>CalamityDemutationPlayer.UpdateBadLifeRegen</c> 里按 <c>lifeRegen -= 30</c> 结算（= 15 HP/s）。
    /// 名称与说明直接复用原版暗影焰的两条本地化键（灾厄原码如此），故本工程 hjson 里无需新增键。
    /// </summary>
    internal class Shadowflame : ModBuff
    {
        /// <summary>名称复用原版暗影焰（灾厄原码即如此）</summary>
        public override LocalizedText DisplayName => Language.GetText("BuffName.ShadowFlame");
        /// <summary>说明复用原版暗影焰（灾厄原码即如此）</summary>
        public override LocalizedText Description => Language.GetText("BuffDescription.ShadowFlame");
        /// <summary>注册为减益：可写入 PvP、不随存档保存，并在专家/大师模式下延长持续时间</summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        /// <summary>每帧置位玩家身上的 shadowflame 标记；掉血在 UpdateBadLifeRegen 中结算</summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().shadowflame = true;
        }
        /// <summary>挂在玩家身上时撒暗影焰尘（照搬灾厄原码，由 ModPlayer.DrawEffects 调用）</summary>
        internal static void DrawEffects(PlayerDrawSet drawInfo)
        {
            Player Player = drawInfo.drawPlayer;
            if (Main.rand.Next(5) < 4)
            {
                int dust = Dust.NewDust(drawInfo.Position - new Vector2(2f), Player.width + 4, Player.height + 4, DustID.Shadowflame, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.75f;
                Main.dust[dust].velocity.X = Main.dust[dust].velocity.X * 0.75f;
                Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 3f;
                if (Main.rand.NextBool(4))
                {
                    Main.dust[dust].noGravity = false;
                    Main.dust[dust].scale *= 0.3f;
                }
            }
        }
    }
}
