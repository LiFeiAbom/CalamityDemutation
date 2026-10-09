using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 空灵征服者（Ethereal Subjugator）—— 「归虚之灵」链（CI 口径）的自持件之一。
    /// 老规矩取灾厄 **2.0.3.9**：66×70、伤害 **200**、击退 1、使用/动画 **10 帧**、魔力 10、`autoReuse`、
    /// **月后 13 档（荧光绿，即灾厄 `PureGreen`）**、价值 **1 铂金 30 金**（`Rarity13BuyPrice`）、
    /// 音 `SoundID.Item82`、握持偏移（<see cref="UseStyle"/> 里把物品位置往左上挪），
    /// 在鼠标处召唤一个**幻影**（<see cref="PhantomGuy"/>）——幻影自身无接触伤害，靠喷 <see cref="GhostFire"/> 打人。
    /// </summary>
    /// <remarks>
    /// 为什么自持、为什么取 2.0.3.9：1.4.4-release（实装那版）把它砍到伤害 160、使用帧 24；
    /// 2.0 那版更旧也更弱（伤害 45、挥动姿态、幻影只占半格召唤栏，源里的 tooltip 就写着
    /// "Each phantom takes only half of a minion slot"）。本工程按用户 2026-10-09
    /// 「1.4.4-release 大砍、要回调」并从链条统一取 2.0.3.9 的口径落地。
    /// **2.0 的"伤害 45 + 半格栏位"那套没有采用**，需要的话一句话就能换。
    /// <para>
    /// **无配方**——来源照源：**噬魂幽花 `Polterghast` 的"非专家武器池"**（源用
    /// `DropHelper.CalamityStyle(1/4, 7 把武器)`）与**噬魂幽花宝藏袋**。本工程沿用既有简化口径：
    /// 本体 1/4 非专家、宝藏袋 1/3，分别挂在 `NPCs/CalamityDemutationGlobalNPC.cs` 与
    /// `Content/Items/CalamityDemutationGlobalItem.cs` 的**现代/经典两条分支**（与女妖之爪同一套写法）。
    /// </para>
    /// </remarks>
    internal class EtherealSubjugator:ModItem
    {
        /// <summary>研究解锁一份（照源 2.0 的 `SacrificeTotal = 1`）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：照源 2.0.3.9（66×70、伤害 200、击退 1、使用 10 帧、月后 13 档、1 铂金 30 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 66;
            Item.height = 70;
            Item.damage = 200;
            Item.DamageType = DamageClass.Summon;
            Item.shoot = ModContent.ProjectileType<PhantomGuy>();
            Item.knockBack = 1f;
            Item.useTime = Item.useAnimation = 10;
            Item.mana = 10;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 30, 0, 0);   // 源用 Rarity13BuyPrice = buyPrice(1, 30, 0, 0)
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;   // 月后 13：荧光绿（＝灾厄 PureGreen）
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item82;
        }
        /// <summary>握持偏移：把手里的法杖往左上（随朝向翻转）挪一点，照源</summary>
        public override void UseStyle(Player player, Rectangle heldItemFrame) => player.itemLocation += new Vector2(-13f * player.direction, -15f);
        /// <summary>掉落在地上的发光层（照源：单帧 glowmask）</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI) => Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Summon/EtherealSubjugatorGlow").Value);
        /// <summary>在鼠标处召唤幻影、带一点边缘随机初速（照源 2.0.3.9）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectileDirect(source, Main.MouseWorld, Main.rand.NextVector2CircularEdge(5f, 5f), type, damage, knockback, player.whoAmI);
            return false;
        }
    }
}
