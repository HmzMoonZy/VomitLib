using System;
using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.HotFix
{
    /// <summary>
    /// 热更新处理器, 注意, 热更新无关构建, 是运行时的概念.
    /// </summary>
    public interface IHotFixHandler
    {
        /// <summary>
        /// 检查强制更新
        /// </summary>
        /// <param name="onSuc">检查成功回调</param>
        /// <param name="onFail">检查失败回调</param>
        /// <param name="onCheck">检查回调</param>
        public void CheckForceUpdate(Action<bool> onSuc, Action onFail, Func<string, bool> onCheck);
    }
}