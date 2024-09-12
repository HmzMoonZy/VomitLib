using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;
using PolymorphicMessagePack;
using MessagePack.Resolvers;
using QFramework;

namespace Twenty2.VomitLib.Net
{ 
    public static class Net
    {
        public static EventDispatcher Dispatcher;
        
        private static CancellationTokenSource _netCancellationToken;

        /// <summary>
        /// 启用框架的网络功能
        /// </summary>
        public static void Init<TRegister, TDisconnectMsg>(
            IFormatterResolver generatedResolver, 
            int errMsgId, 
            Func<Event, ValueTuple<int, bool>> errCodeHandler) 
            where TDisconnectMsg : Message, new() 
            where TRegister : new()
        {
            if (_netCancellationToken != null)
            {
                LogKit.E("重复初始化 Net !!!");
                return;
            }
            
            _netCancellationToken = new();
            
            // 为了调用生成的静态构造函数
            _ = new TRegister();
            // 注册所需的类型
            PolymorphicResolver.AddInnerResolver(generatedResolver);
            PolymorphicTypeMapper.Register<Message>();
            PolymorphicResolver.Instance.Init();
            // 初始化链接
            GameClient.Instance.Init(()=> new TDisconnectMsg());
            // 事件分发器
            Dispatcher = new EventDispatcher();
            // 目前这个回调主要用于兼容服务端的规则, 和隐藏掉 NetSystem 对 MsgWaiter.EndWait 的调用 但是大多数游戏一收一发足以应对, 所以暂时不抛出这个事件.
            Dispatcher.AddListener(errMsgId, e =>
            {
                (int uniId, bool suc) result = errCodeHandler.Invoke(e);
                MsgWaiterMgr.EndWait(result.uniId, result.suc);
            });
            // 轮询
            UniTask.WaitWhile(() =>
            {
                GameClient.Instance.Update(Dispatcher);
                return true;
            }, PlayerLoopTiming.Update, cancellationToken: _netCancellationToken.Token).Forget();
        }

        public static void Shutdown()
        {
            _netCancellationToken?.CancelAndDispose();
            _netCancellationToken = null;
            
            GameClient.Instance.Close();
            MsgWaiterMgr.DisposeAll();
        }
    }
}



