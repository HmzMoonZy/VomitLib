namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// 修改它后请重新初始化。
    /// </summary>
    public enum ViewSortLayer
    {
        /// <summary>
        /// 大厅层,通常只存在一层.
        /// </summary>
        Lobby = 0,

        /// <summary>
        /// 游戏层,通常是小地图
        /// </summary>
        GamePlay = 10,

        /// <summary>
        /// 低于 Hud 的游戏操作层
        /// </summary>
        LowOperation = 20,

        /// <summary>
        /// 用于动静分离的层级,例如伤害显示.
        /// </summary>
        Hud = 30,
        
        /// <summary>
        /// 标准游戏操作层
        /// </summary>
        Operation = 40,
        
        /// <summary>
        /// 低于 Loading
        /// </summary>
        LowTip = 50,

        /// <summary>
        /// 加载遮罩层.
        /// </summary>
        Loading = 60,

        /// <summary>
        /// 提示层.
        /// </summary>
        Tip = 70,

        /// <summary>
        /// 顶级展示层.
        /// </summary>
        Debug = 100,
    }

}