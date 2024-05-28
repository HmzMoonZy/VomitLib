using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine.Networking;

namespace Twenty2.VomitLib.Net
{
    /// <summary>
    /// 网络事件监听（不允许有多个监听，多个后来者顶替前一个）
    /// </summary>
    public abstract class AbstractNetSystem : AbstractSystem
    {
        protected abstract override void OnInit();

        #region Message
        
        private static int UniId = 200;
        
        protected UniTask SendMsg(Message msg)
        {
            msg.UniId = UniId++;
            GameClient.Instance.Send(msg);
            LogKit.I("开始等待消息:" + msg.UniId);
            return MsgWaiter.StartWait(msg.UniId);
        }

        // TODO 自动拼接 url, 和服务端同步
        protected async UniTask<UnityWebRequest> SendMsg(string httpUrl)
        {
            return await UnityWebRequest.Get(httpUrl).SendWebRequest();
        }

        #endregion

        #region Evnets

        // 每个 NetSystem 维护自己的事件表
        private readonly Dictionary<int, Action<Event>> _eventMap = new();
        
        /// <summary>
        /// 往 Net 事件分发器注册服务. 一个 NetSystem 只能注册一个 msgId 任务.
        /// 新注册的事件会顶替掉之前的事件
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="handler"></param>
        protected void RegisterEvent(int msgId, Action<Event> handler)
        {
            if (!_eventMap.TryAdd(msgId, handler))
            {
                LogKit.E($"重复注册网络事件 > {msgId}");
                Net.Dispatcher.RemoveListener(msgId, _eventMap[msgId]);
                _eventMap[msgId] = handler;
            }

            Net.Dispatcher.AddListener(msgId, handler);
        }

        protected void UnRegisterEvent(int msgId, Action<Event> handler)
        {
            if (!_eventMap.ContainsKey(msgId))
            {
                return;
            }
            
            _eventMap[msgId] -= handler;
            Net.Dispatcher.RemoveListener(msgId, handler);
        }

        protected void UnRegisterEvent(int msgId)
        {
            int evtId = msgId;
            if (!_eventMap.ContainsKey(evtId))
            {
                return;
            }
            Net.Dispatcher.RemoveListener(msgId, _eventMap[evtId]);
            _eventMap.Remove(evtId);
        }

        protected void ClearListeners()
        {
            foreach (var kv in _eventMap)
            {
                Net.Dispatcher.RemoveListener(kv.Key, kv.Value);
            }
            _eventMap.Clear();
        }

        #endregion
    }
}