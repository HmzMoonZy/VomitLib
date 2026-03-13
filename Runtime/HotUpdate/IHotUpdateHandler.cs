using System;
using Cysharp.Threading.Tasks;

namespace Twenty2.VomitLib.HotFix
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

        public enum ErrCode
        {
            Success = 0,
            /// <summary>
            /// 检查强更失败
            /// </summary>
            CheckForceUpdateFailed = 101,
            /// <summary>
            /// 版本低, 需要强更
            /// </summary>
            NeedForceUpdate = 102,
            /// <summary>
            /// 超时错误
            /// </summary>
            TimeOut = 201,
            /// <summary>
            /// 网络无连接
            /// </summary>
            NotReachable = 202,
            /// <summary>
            /// 用户不同意使用移动数据
            /// </summary>
            NotAllowUseMobileData = 301,
            
            /// <summary>
            /// 内部错误
            /// </summary>
            InternalError = 10000,
        }
        
        public UniTask HotUpdate(Action<IHotUpdateHandler.Status> onStatus, Action<IHotUpdateHandler.ErrCode> onFail);
        
    }
}