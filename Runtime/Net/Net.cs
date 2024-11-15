using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using FluentAPI;
using MessagePack;
using PolymorphicMessagePack;
using MessagePack.Resolvers;
using QFramework;

namespace Twenty2.VomitLib.Net
{ 
    public static class Net
    {
        public static EventDispatcher Dispatcher { get; private set; }

        private static CancellationTokenSource _netCancellationToken;
        
        private static bool _isInit;

        private static int UniID = 200;

        /// <summary>
        /// 启用框架的网络功能
        /// </summary>
        /// <param name="resolver">框架生成的消息处理器</param>
        /// <param name="errMsgId">判断消息是否合法的消息的ID</param>
        /// <param name="errCodeHandler">判断消息是否合法</param>
        /// <param name="getDisconnectMsg">离线Message</param>
        /// <typeparam name="TPolymorphicRegister">服务端框架生成的类型注册器</typeparam>
        public static void Init<TPolymorphicRegister>(IFormatterResolver resolver, int errMsgId, Func<Event, bool> errCodeHandler, Func<Message> getDisconnectMsg = null) where TPolymorphicRegister : new()
        {
            if (_isInit)
            {
                LogKit.E("重复初始化 Net !!!");
                return;
            }

            _isInit = true;
            _netCancellationToken = new CancellationTokenSource();
            
            // 注册所需的类型
            _ = new TPolymorphicRegister();                 // 触发生成代码的静态函数  
            PolymorphicResolver.AddInnerResolver(resolver);
            PolymorphicTypeMapper.Register<Message>();
            PolymorphicResolver.Instance.Init();
            // 事件分发器
            Dispatcher = new EventDispatcher();
            // 目前这个回调主要用于兼容服务端的规则, 和隐藏掉 NetSystem 对 MsgWaiter.EndWait 的调用 但是大多数游戏一收一发足以应对, 所以暂时不抛出这个事件.
            Dispatcher.AddListener(errMsgId, e =>
            {
                MsgWaiterMgr.EndWait(e.GetMessage<Message>().UniId, errCodeHandler.Invoke(e));
            });
            
            // 初始化链接
            GameClient.Instance.Init(getDisconnectMsg);
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
            MsgWaiterMgr.DisposeAll();
        }

        public static UniTask<bool> Connect(string address, int port)
        {
            return GameClient.Instance.Connect(address, port);
        }

        public static UniTask Send(Message msg)
        {
            msg.UniId = UniID++;
            GameClient.Instance.Send(msg);
#if UNITY_EDITOR
            LogKit.I("开始等待消息:" + msg.UniId);
#endif
            
            return MsgWaiterMgr.StartWait(msg.UniId);
        }
    }
}



