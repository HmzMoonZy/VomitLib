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

        private static Func<Event, ValueTuple<int, bool>> _errCodeHandler;

        public static void Init(IFormatterResolver generatedResolver, Func<Message> onDisconnected, int errCodeMsgId, Func<Event, ValueTuple<int, bool>> errCodeHandler)
        {
            _netCancellationToken = new();
            _errCodeHandler = errCodeHandler;
            
            Dispatcher = new EventDispatcher();
            Dispatcher.AddListener(errCodeMsgId, e =>
            {
                (int uniId, bool suc) result = _errCodeHandler.Invoke(e);
                MsgWaiter.EndWait(result.uniId, result.suc);
            });
            
            PolymorphicResolver.AddInnerResolver(generatedResolver);
            PolymorphicTypeMapper.Register<Message>();
            PolymorphicResolver.Instance.Init();
            
            GameClient.Instance.Init(onDisconnected);
            
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



