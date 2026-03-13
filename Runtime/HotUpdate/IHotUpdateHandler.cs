using System;
using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.HotUpdate
{
    /// <summary>
    /// 热更新处理器, 注意, 热更新无关构建, 是运行时的概念.
    /// </summary>
    public interface IHotUpdateHandler
    {
        public enum Status
        {
            None,
            
            CheckingForceUpdate,        // 检查包版本
            CheckingResourceUpdate,     // 检查资源更新
            CheckingResourceSize,       // 检查资源大小
            ConfirmMobileData,          // 确认流量下载
            Downloading,                // 下载资源
            
            Done,                       // 流程结束
        }

        public UniTask HotUpdate(Action<Status> onStatus, Action<int> onFail);
        
    }
}
