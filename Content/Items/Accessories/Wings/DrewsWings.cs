using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Wings
{
    /// <summary>
    /// 德鲁之翼（Drew's Wings） - 翅膀
    /// 专家饰品翅膀；飞行数据为 361 帧飞行时间、水平加速 12、水平速度上限 4，
    /// 垂直飞行速度亦经强化（见 VerticalWingSpeeds）。
    /// 飞行时在身后洒落钻石色尘埃，仅视觉表现，无数值结算。
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]  // 装备时自动分配 Item.wingSlot 并按翅膀外观渲染
    internal class DrewsWings:ModItem
    {
        /// <summary>
        /// 注册翅膀统计数据：飞行时间 361 帧、水平加速 12、水平速度上限 4
        /// （Item.wingSlot 由 AutoloadEquip(EquipType.Wings) 自动分配）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(361, 12f, 4f);
        }
        /// <summary>
        /// 物品基础属性：尺寸、售价、专家品质与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 22;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 0, 0, 0);   // 售价 1 铂金
            Item.expert = true;                       // 专家限定品质
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时仅做视觉处理：满足"按住跳跃、仍有飞行时间、非云瓶首跳、Y 方向有速度"时，
        /// 在玩家身后生成一颗钻石色尘埃；飞行数值由 SetStaticDefaults 的 WingStats 提供
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.controlJump && player.wingTime > 0f && !VanillaExtraJump.CloudInABottle.CanStart(player) && player.jump == 0 && player.velocity.Y != 0f && !hideVisual)
            {
                int num59 = 4;       // 面朝左时的尘埃水平偏移
                if (player.direction == 1)
                {
                    num59 = -40;     // 面朝右时翻到身后
                }
                int num60 = Dust.NewDust(new Vector2(player.position.X + (float)(player.width / 2) + (float)num59, player.position.Y + (float)(player.height / 2) - 15f), 30, 30, DustID.GemDiamond, 0f, 0f, 100, default(Color), 2.4f);  // 钻石色尘埃
                Main.dust[num60].noGravity = true;   // 不受重力
                Main.dust[num60].velocity *= 0.3f;   // 减缓漂移
                if (Main.rand.NextBool(10))
                {
                    Main.dust[num60].fadeIn = 2f;    // 1/10 概率淡入，增加闪光感
                }
                Main.dust[num60].shader = GameShaders.Armor.GetSecondaryShader(player.cWings, player);  // 随玩家翅膀染色
            }
        }
        /// <summary>
        /// 设置垂直飞行参数（行尾注释为原版默认值）：
        /// 下落/上升时的上升加速度、可上升速度倍率、最大上升倍率与持续上升力
        /// </summary>
        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 1f; //0.85
            ascentWhenRising = 0.175f; //0.15
            maxCanAscendMultiplier = 1.2f; //1
            maxAscentMultiplier = 3.25f; //3
            constantAscend = 0.15f; //0.135
        }
    }
}
