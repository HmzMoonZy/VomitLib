namespace Twenty2.VomitLib.Editor.SharpWindow
{
    /// <summary>
    /// SharpWindow 的可插拔调试模块。
    /// 任意 Editor 程序集实现此接口后，会被 SharpWindow 自动发现。
    /// </summary>
    public interface ISharpModule
    {
        /// <summary>
        /// 模块标题。未实现 <see cref="ISharpTabModule"/> 时，同时作为 Tab 标题。
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
        /// 模块内容（不含标题）。
        /// </summary>
        void OnGUI();
    }

    /// <summary>
    /// 可选的 Tab 分组接口。多个模块返回相同 TabName 时，会聚合到同一个 Tab 中。
    /// 未实现该接口的旧模块仍独占以 Name 命名的 Tab，保持向后兼容。
    /// </summary>
    public interface ISharpTabModule : ISharpModule
    {
        string TabName { get; }
    }
}
