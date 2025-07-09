using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib;
using Twenty2.VomitLib.Tools;
using UnityEngine;

namespace Twenty2.VomitLib.Net
{
    public class NetClient
    {
        private const float DISPATCH_MAX_TIME = 0.06f;  //每一帧最大的派发事件时间，超过这个时间则停止派发，等到下一帧再派发

        private static NetClient _instance;
        public static NetClient Instance
        {
            get 
            { 
                if (_instance == null)
                {
                    _instance = new NetClient();
                }

                return _instance;
            }
        }
        
        private NetChannel _channel { get; set; }
        
        private ConcurrentQueue<Message> _msgQueue = new();
        
        HashSet<int> _ignoreCodeSet = new();
        
        /// <summary>
        /// 上次接收消失时间
        /// </summary>
        public float HandMsgTime { get; private set; }
        
        /// <summary>
        /// 收到的消息计数 和服务器对不上则应该断线重连
        /// </summary>
        public int ResCode { get; private set; }
        
        public void ResetResCode(int code = 0)
        {
            ResCode = code;
        }

        /// <summary>
        /// 心跳等无关逻辑的消息可忽略
        /// </summary>
        public void IgnoreCode(int msgId)
        {
            _ignoreCodeSet.Add(msgId);
        }
        
        public int Port { private set; get; }
        public string Host { private set; get; }
        
        public async Task<bool> Connect(string host, int port, Action onDisConnected, int timeOut = 5000)
        {
            Host = host;
            Port = port;
            try
            {
                Clear();
                var ipType = AddressFamily.InterNetwork;
                (ipType, host) = NetKit.GetIPv6Address(host, port);

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
                
                Log.Debug($"connected success....");

                _channel = new NetChannel(socket, OnReceive, onDisConnected);
                
                _ = _channel.StartAsync();
                
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

        private void OnReceive(Message msg)
        {
            _msgQueue.Enqueue(msg);
        }

        public void Close()
        {
            _channel?.Close();
            _channel = null;
            Clear();
        }

        public void Clear()
        {
            _msgQueue = new ConcurrentQueue<Message>();
        }

        public void Update()
        {
            float curTime = UnityEngine.Time.realtimeSinceStartup;
            float endTime = curTime + DISPATCH_MAX_TIME;
            while (curTime < endTime)
            {
                if (_msgQueue.IsEmpty)
                {
                    return;
                }

                if (!_msgQueue.TryDequeue(out var msg))
                {
                    return;
                }

                if (msg == null)
                {
                    return;
                }
                
                Log.Debug($"开始处理网络事件 {msg.MsgId} [{msg.GetType().FullName}]");
                try
                {
                    NetSystem.RecvMsg(msg);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                curTime = UnityEngine.Time.realtimeSinceStartup;
                if (!_ignoreCodeSet.Contains(msg.MsgId))
                {
                    ResCode++;
                }
            }
        }
    }
}