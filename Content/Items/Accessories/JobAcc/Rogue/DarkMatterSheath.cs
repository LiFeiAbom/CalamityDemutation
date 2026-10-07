using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 暗物质剑鞘（DarkMatterSheath）—— "暗物质剑鞘"链的第三件
    /// （欺诈硬币 → 毁灭徽章 → 暗物质剑鞘 → 日蚀魔镜），远古操纵机档。
    /// 口径按用户 2026-10-07 拍板取 **2.0**（该版里这件叫 <c>DarkGodsSheath</c>，2.0.3.9 起才改名 DarkMatterSheath）：
    /// <para>
    /// ① 48×62、**青档**、**80 金**（2.0.x 的 `Rarity9BuyPrice`；2.0.4/1.4.4 起降级成红档 1 铂金）。
    /// </para>
    /// <para>
    /// ② 效果（2.0 全量）：最大潜行值 **+20 点**（2.0.3.9+ 削到 +10）、潜行打击 **半价**（`stealthStrikeHalfCost`）、
    /// 移动潜行恢复加速旗标 `darkGodSheath`、**盗贼潜行打击 100% 暴击**（2.0.3.9+ 已删）、
    /// 盗贼 **+6% 伤害 / +6 暴击**。
    /// </para>
    /// <para>
    /// ③ 配方走本链的"两分支都能合成"口径：**静默剑鞘（本模组）×1 + 毁灭徽章（本模组）×1 +
    /// 熵构块 `MeldBlob`×14 @ 远古操纵机**。源配方（2.0~2.0.4）用的是已删除的熵构体 `MeldConstruct`×5，
    /// 1.4.4-release 才改成 `MeldBlob`×14 直出；本件取后者（`MeldBlob` 在现代/经典两版灾厄里都在，
    /// 远古操纵机是原版物），故只需注册一条。
    /// </para>
    /// <para>
    /// 效果分三处落地：盗贼伤害/暴击写在 <c>CalamityDemutationPlayer.PostUpdateMiscEffects</c>；
    /// 潜行三项（上限 +20 / 半价 / 加速旗标）写在 <c>PostUpdateEquips</c>（灾厄在 ResetEffects 里每帧复位这些字段、
    /// 之后才读取），走 CDUtil 的反射桥；"必暴击"写在 <c>ModifyHitNPCWithProj</c>，用 <c>HitModifiers.SetCrit()</c>
    /// （2.0 是在灾厄 CalamityGlobalProjectile.ModifyHitNPC 里把 <c>ref bool crit</c> 置真）。
    /// </para>
    /// <para>
    /// 与灾厄本体重名：本体仍有同名件（削弱后的 +10 潜行、无必暴击、红档），本件是"旧版回归"的同名不同物；
    /// 按既有口径**不做互斥**。
    /// </para>
    /// </summary>
    internal class DarkMatterSheath : ModItem
    {
        /// <summary>
        /// 基础属性：48×62、价值 80 金（灾厄 2.0.x 青档 `Rarity9BuyPrice`）、稀有度青、饰品
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 62;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Cyan;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>darkMatterSheath</c> 标记；数值在 CalamityDemutationPlayer 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().darkMatterSheath = true;
        }
        /// <summary>
        /// 配方：静默剑鞘 + 毁灭徽章（均为本模组自写）+ 熵构块 `MeldBlob`×14 @ 远古操纵机。
        /// `MeldBlob` 从现代/经典任一版灾厄取（两版都有），故只注册一条、两分支通用。
        /// </summary>
        public override void AddRecipes()
        {
            int meldBlobType = -1;
            ModItem meldBlob = null;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("MeldBlob", out meldBlob))
            {
                meldBlobType = meldBlob.Type;
            }
            else if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModItem>("MeldBlob", out meldBlob))
            {
                meldBlobType = meldBlob.Type;
            }

            if (meldBlobType < 0)
            {
                Mod.Logger.Warn("暗物质剑鞘：现代/经典版灾厄里都找不到 MeldBlob，本条配方未注册。");
                return;
            }

            Recipe recipe = CreateRecipe();
            recipe.AddIngredient<SilencingSheath>();
            recipe.AddIngredient<RuinMedallion>();
            recipe.AddIngredient(meldBlobType, 14);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }
}
