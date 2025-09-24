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
        /// 游戏层,通常是小地图或HUD.
        /// </summary>
        GamePlay = 10,

        /// <summary>
        /// 游戏操作层,比如聊天窗口.
        /// </summary>
        LowOperation = 20,

        /// <summary>
        /// 用于动静分离的层级,例如伤害显示.
        /// </summary>
        Public = 30,
        
        /// <summary>
        /// 高于 Public 的游戏操作层
        /// </summary>
        Operation = 40,
        
        /// <summary>
        /// 低于 Loading 但高于 Public
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