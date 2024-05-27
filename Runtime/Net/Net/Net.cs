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
        public static EventDispatcher ED;
        
        private static CancellationTokenSource _netCancellationToken;

        public static void Init(IFormatterResolver generatedResolver, Func<Message> onDisconnected)
        {
            _netCancellationToken = new();
            PolymorphicResolver.AddInnerResolver(generatedResolver);
            PolymorphicTypeMapper.Register<Message>();
            PolymorphicResolver.Instance.Init();
            
            ED = new EventDispatcher();
            
            GameClient.Instance.Init(onDisconnected);
            
            UniTask.WaitWhile(() =>
            {
                GameClient.Instance.Update(ED);
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



