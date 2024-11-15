using Geek.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using QFramework;
using Twenty2.VomitLib.Tools;
using Debug = UnityEngine.Debug;

namespace Twenty2.VomitLib.Net
{
    public class GameClient
    {
        private const float DISPATCH_TICK_TIME = 0.06f;  //每一帧最大的派发事件时间，超过这个时间则停止派发，等到下一帧再派发 
        
        private static GameClient _instance;
        public static GameClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameClient();
                }

                return _instance;
            }
        }

        private NetChannel _channel { get; set; }
        
        private ConcurrentQueue<Message> _msgQueue = new ConcurrentQueue<Message>();

        private Func<Message> _onDisconnected;
        
        /// <summary>
        /// 连接的端口
        /// </summary>
        public int Port { private set; get; }
        
        /// <summary>
        /// 连接的地址
        /// </summary>
        public string Host { private set; get; }
        
        private GameClient()
        {
            
        }
        
        public GameClient Init(Func<Message> onDisconnected)
        {
            this._onDisconnected = onDisconnected;
            return this;
        }
        
        /// <summary>
        /// 连接到地址
        /// </summary>
        public async UniTask<bool> Connect(string host, int port, int timeOut = 5000)
        {
            Host = host;
            Port = port;
            try
            {
                ClearAllMsg();

                (var ipType, string ip) = NetKit.GetIPv6Address(host, port);

                var socket = new TcpClient(ipType);
                try
                {
                    await socket.ConnectAsync(host, port);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }

                if (!socket.Connected)
                {
                    return false;
                }

                LogKit.I($"connected success....");
                
                OnConnected();
                
                _channel = new NetChannel(socket, OnReceived, OnDisConnected);
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }
        }
        
        public void Send(Message msg)
        {
            _channel?.Write(msg);
        }

        private void OnConnected()
        {
            
        }

        private void OnDisConnected()
        {
            if (_onDisconnected != null)
            {
                _msgQueue.Enqueue(_onDisconnected.Invoke());     
            }
            
        }

        private void OnReceived(Message msg)
        {
            _msgQueue.Enqueue(msg);
        }

        public void Close()
        {
            _channel?.Close();
            _channel = null;
            ClearAllMsg();
        }

        public void ClearAllMsg()
        {
            _msgQueue = new ConcurrentQueue<Message>();
        }

        public void Update(EventDispatcher evt, float maxTime = DISPATCH_TICK_TIME)
        {
            float curTime = UnityEngine.Time.realtimeSinceStartup;
            float endTime = curTime + maxTime;
            while (curTime < endTime)
            {
                if (_msgQueue.IsEmpty)
                    return;

                if (!_msgQueue.TryDequeue(out var msg))
                    break;

                if (msg == null)
                    return;

#if UNITY_EDITOR 
                var msgName = msg != null ? msg.GetType().FullName : "";
                Debug.Log($"开始处理网络事件 {msg.MsgId}  {msgName}");
#endif

                try
                {
                    evt.DispatchEvent(msg.MsgId, msg);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                curTime = UnityEngine.Time.realtimeSinceStartup;
                if (!ignoreCodeList.Contains(msg.MsgId))
                    ResCode++;
            }
        }

        /// <summary>上次接收消失时间</summary>
        public float HandMsgTime { get; private set; }
        /// <summary>收到的消息计数 和服务器对不上则应该断线重连</summary>
        public int ResCode { get; private set; }
        List<int> ignoreCodeList = new List<int>();
        public void ResetResCode(int code = 0)
        {
            ResCode = code;
        }

        /// <summary>
        /// 心跳等无关逻辑的消息可忽略
        /// </summary>
        public void AddIgnoreCode(int msgId)
        {
            if (!ignoreCodeList.Contains(msgId))
                ignoreCodeList.Add(msgId);
        }
    }
}