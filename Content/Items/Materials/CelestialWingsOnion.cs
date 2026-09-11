using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 天界翅膀洋葱：天界洋葱（CelestialOnion）的变体。专家/大师模式消耗品，
    /// 使用后永久开启一个【只放翅膀】的专用饰品栏。
    /// 使用姿势/判定/网络同步逻辑与 CelestialOnion 完全对称。
    /// </summary>
    internal class CelestialWingsOnion : ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、专家标记、堆叠上限与使用姿势/音效（与天界洋葱一致）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                        // 贴图宽（像素）
            Item.height = 28;                       // 贴图高（像素）
            Item.expert = true;                     // 专家物品标记：仅专家/大师世界掉落，普通世界不生成
            Item.maxStack = 99;                     // 最大堆叠 99
            Item.useAnimation = 30;                 // 使用动画时长（帧）
            Item.useTime = 30;                      // 使用冷却时间（帧）
            Item.useStyle = ItemUseStyleID.HoldUp;  // 使用姿势：举起物品
            Item.UseSound = SoundID.Item4;          // 使用音效（原版 Item4）
            Item.consumable = true;                 // 使用后消耗
        }
        /// <summary>
        /// 使用前置判定：仅在专家/大师模式且尚未解锁专用翅膀栏时可使用。
        /// 返回 false 时物品不会被消耗，也不会进入 UseItem。
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 仅在专家/大师模式，且尚未开启翅膀栏时可用
            if (!Main.expertMode || modPlayer.extraWingSlot)   // Main.expertMode 同时覆盖专家与大师；extraWingSlot 为“已使用翅膀洋葱”标志
            {
                return false;                                   // 非专家世界，或已解锁 → 禁止使用
            }
            return true;                                        // 其余情况允许使用
        }
        /// <summary>
        /// 执行解锁：处于使用动画且尚未解锁时，置位 extraWingSlot 永久标志并同步该玩家状态给所有端。
        /// 标志随存档持久化（见 CalamityDemutationPlayer），由 CelestialWingsOnionSlot 读取以显示专用翅膀栏。
        /// </summary>
        public override bool? UseItem(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (player.itemAnimation > 0 && !modPlayer.extraWingSlot && player.itemTime == 0)   // itemAnimation>0 处于使用动画中；itemTime==0 保证本帧只结算一次
            {
                player.itemTime = Item.useTime;   // 设置使用冷却，避免重复触发
                modPlayer.extraWingSlot = true;   // 永久解锁专用翅膀栏（存档持久化）
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, 0, player.whoAmI, 0f, 0f, 0, 0, 0);   // 广播玩家状态，使其他端看到新栏位
            }
            return true;   // 返回 true 交回原版继续消耗物品
        }
    }
    /// <summary>
    /// 天界翅膀洋葱提供的专用翅膀栏。只收翅膀类物品；
    /// 翅膀的飞行效果由 ModAccessorySlot 默认 ApplyEquipEffects 自动应用，无需额外逻辑。
    /// </summary>
    internal class CelestialWingsOnionSlot : ModAccessorySlot
    {
        /// <summary>
        /// 该专用翅膀栏是否启用：玩家对象有效且已使用天界翅膀洋葱（extraWingSlot）时启用。
        /// 与灾厄模组是否加载、世界进度/Boss 击杀均无关。
        /// </summary>
        public override bool IsEnabled()
        {
            // 加载过程早期 Player 可能未初始化，直接取 ModPlayer 会抛 index error
            if (!Player.active)   // 玩家未激活（初始化早期）→ 直接禁用，避免索引越界
                return false;
            return Player.GetModPlayer<CalamityDemutationPlayer>().extraWingSlot;   // 已解锁专用翅膀栏才启用
        }
        public override bool IsHidden() => IsEmpty && !IsEnabled();
        // 未开启时整格隐藏（不影响取回已存物品的行为，纯显示层面）
        public override bool IsVisibleWhenNotEnabled() => false;
        // 空栏时显示一对翅膀作为图标（与 TML 官方 ExampleModWingSlot 同款写法）
        public override string FunctionalTexture => "Terraria/Images/Item_" + ItemID.CreativeWings;
        // 功能槽/时装槽：只接受翅膀类物品（wingSlot > 0 即翅膀）。默认不放行染料。
        public override bool CanAcceptItem(Item checkItem, AccessorySlotType context)
        {
            if (checkItem.wingSlot > 0)
                return true;
            return false;
        }
        // 悬停翅膀右键快捷装备时，优先把翅膀丢进本栏
        public override bool ModifyDefaultSwapSlot(Item item, int accSlotToSwapTo)
        {
            if (item.wingSlot > 0)
                return true;
            return false;
        }
    }
}
