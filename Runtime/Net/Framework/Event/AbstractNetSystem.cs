using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QFramework;

namespace Twenty2.VomitLib.Net
{
    /// <summary>
    /// 网络事件监听（不允许有多个监听，多个后来者顶替前一个）
    /// </summary>
    /// TODO 兼容UniTask
    public abstract class AbstractNetSystem : AbstractSystem
    {
        protected abstract override void OnInit();

        #region Message
        
        protected int UniId = 200;
        
        protected virtual Task SendMsg(Message msg)
        {
            msg.UniId = UniId++;
            GameClient.Instance.Send(msg);
            LogKit.I("开始等待消息:" + msg.UniId);
            return MsgWaiter.StartWait(msg.UniId);
        }

        protected T GetCurMsg<T>(object msg) where T : Message, new()
        {
            return msg as T;
        }
        

        #endregion

        #region Evnets

        private readonly Dictionary<int, Action<Event>> _evtMap = new();

        protected void AddNetListener(int id, Action<Event> handler)
        {
            int evtId = id;
            if (!_evtMap.TryAdd(evtId, handler))
            {
                //去重，一个网络消息只要一个监听
                LogKit.E("重复注册网络事件>" + evtId);
                Net.Ned.RemoveListener(evtId, _evtMap[evtId]);
                _evtMap[evtId] = handler;
            }

            Net.Ned.AddListener(id, handler);
        }

        protected void RemoveListener(int id, Action<Event> handler)
        {
            int evtId = id;
            if (!_evtMap.ContainsKey(evtId))
                return;
            _evtMap[evtId] -= handler;
            Net.Ned.RemoveListener(id, handler);
        }

        protected void RemoveListeners(int id)
        {
            int evtId = id;
            if (!_evtMap.ContainsKey(evtId))
                return;
            Net.Ned.RemoveListener(id, _evtMap[evtId]);
            _evtMap.Remove(evtId);
        }

        protected void ClearListeners()
        {
            foreach (var kv in _evtMap)
                Net.Ned.RemoveListener(kv.Key, kv.Value);
            _evtMap.Clear();
        }

        #endregion
    }
}