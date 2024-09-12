using System;
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

        public static void Init(IFormatterResolver generatedResolver, Func<Message> onDisconnected, int errCodeMsgId, Func<Event, ValueTuple<int, bool>> errCodeHandler)
        {
            if (_netCancellationToken != null)
            {
                LogKit.E("重复初始化 Net !!!");
                return;
            }
            
            _netCancellationToken = new();
            // 消息处理器
            PolymorphicResolver.AddInnerResolver(generatedResolver);
            PolymorphicTypeMapper.Register<Message>();
            PolymorphicResolver.Instance.Init();
            // 初始化链接
            GameClient.Instance.Init(onDisconnected);
            // 事件分发器
            Dispatcher = new EventDispatcher();
            // 目前这个回调主要用于兼容服务端的规则, 和隐藏掉 NetSystem 对 MsgWaiter.EndWait 的调用 但是大多数游戏一收一发足以应对, 所以暂时不抛出这个事件.
            Dispatcher.AddListener(errCodeMsgId, e =>
            {
                (int uniId, bool suc) result = errCodeHandler.Invoke(e);
                MsgWaiter.EndWait(result.uniId, result.suc);
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
            GameClient.Instance.Close();
            MsgWaiter.DisposeAll();
            _netCancellationToken?.CancelAndDispose();
        }
    }
}



