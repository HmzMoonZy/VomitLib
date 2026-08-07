namespace Twenty2.VomitLib.Editor.SharpWindow
{
    /// <summary>
    /// SharpWindow 的可插拔调试模块。
    /// 新增调试功能 = 实现接口 + 在 SharpWindow._modules 中加一行。
    /// </summary>
    internal interface ISharpModule
    {
        /// <summary>
        /// Section 标题（由窗口统一渲染，模块不自行绘制）。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 窗口 OnEnable 时调用：订阅事件、读初值。
        /// </summary>
        void OnEnable();

        /// <summary>
        /// 窗口 OnDisable 时调用：退订事件、归还资源。
        /// </summary>
        void OnDisable();

        /// <summary>
        /// Section 内容（不含标题）。
        /// </summary>
        void OnGUI();
    }
}
