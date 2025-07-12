using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using PolymorphicMessagePack;
using Twenty2.VomitLib;
using Twenty2.VomitLib.Net;
using UnityEngine;

/// <summary>
/// 网络系统基类
/// </summary>
public static class NetSystem
{
    private static int UniId { get; set; } = 200;
    
    private static Dictionary<int, Action<Message>> _msgRegisters = new();

    private static Dictionary<int, Action<Message>> _msgOnceRegisters = new();

    private static Func<Message, bool> _onErrCodeResp;

    private static int _errRespMsgId;

    /// <summary>
    /// 初始化网络系统, 并连接服务器.
    /// 需要注意, 服务器采用双消息机制.
    /// 实现层必须注册错误码处理回调, 否则所有消息都不会返回.
    /// </summary>
    /// <param name="resolver">消息解析器</param>
    /// <param name="host">服务器地址</param>
    /// <param name="port">服务器端口</param>
    /// <param name="onErrCodeResp">错误码消息回调</param>
    /// <returns>是否链接成功</returns>
    public static async UniTask<bool> Init(MessagePack.IFormatterResolver resolver, string host, int port, int errRespMsgId, [NotNull]Func<Message, bool> onErrCodeResp, [NotNull]Action onDisConnected)
    {
        _onErrCodeResp = onErrCodeResp;
        _errRespMsgId = errRespMsgId;
        try
        {
            // GeekServer 的 MessagePack 解析器
            PolymorphicResolver.AddInnerResolver(resolver);
            PolymorphicTypeMapper.Register<Message>();
            PolymorphicResolver.Instance.Init();

            // 循环消息
            UniTask.Create(async () =>
            {
                while(Application.isPlaying)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    NetClient.Instance.Update();
                }

                NetClient.Instance.Close();
            }).Forget();

            // 连接服务器
            return await NetClient.Instance.Connect(host, port, onDisConnected);
        }
        catch (System.Exception e)
        {
            Log.Error($"AbstractNetSystem]初始化网络系统失败: {e.Message}");
            return false;
        }
    }

    public static UniTask Send(Message msg)
    {
        msg.UniId = UniId++;
        NetClient.Instance.Send(msg);
        Log.Debug("开始等待消息:" + msg.UniId);
        return MsgWaiter.StartWait(msg.UniId);
    }

    public static UniTask Send<T>() where T : Message, new()
    {
        var msg = new T();
        return Send(msg);
    }

    /// <summary>
    /// 收到消息
    /// </summary>
    /// <param name="msg">消息</param>
    public static void ProcessMsg(Message msg)
    {
        // 双消息机制
        if(msg.MsgId == _errRespMsgId)
        {
            var isSuccess = _onErrCodeResp.Invoke(msg);
            MsgWaiter.EndWait(msg.UniId, isSuccess);
            return;
        }

        // 业务消息路由
        Log.Debug($"AbstractNetSystem]收到消息: {msg.GetType().Name}");
        if (_msgRegisters.TryGetValue(msg.MsgId, out var onMsg))
        {
            onMsg?.Invoke(msg);
        }
        if (_msgOnceRegisters.TryGetValue(msg.MsgId, out var onOnceMsg))
        {
            onOnceMsg?.Invoke(msg);
            _msgOnceRegisters.Remove(msg.MsgId);
        }
    }

    /// <summary>
    /// 注册消息
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="onMsg">消息回调</param>
    public static void RegisterMsg<T>(Action<Message> onMsg) where T : Message, new()
    {
        var msgId = new T().MsgId;              // TODO MsgFactory 有办法映射,未来添加支持.
        RegisterMsg(msgId, onMsg);
    }

    public static void RegisterMsg(int msgId, Action<Message> onMsg)
    {
        if(_msgRegisters.ContainsKey(msgId))
        {
            Log.Warning($"消息ID {msgId} 已注册, 覆盖旧回调");
            _msgRegisters[msgId] = onMsg;
        }
        else
        {
            _msgRegisters.Add(msgId, onMsg);
        }
    }

    /// <summary>
    /// 注册一次性消息
    /// </summary>
    /// <param name="onMsg">消息回调</param>
    public static void RegisterOnceMsg<T>(Action<Message> onMsg) where T : Message, new()
    {
        var msgId = new T().MsgId;
        RegisterOnceMsg(msgId, onMsg);
    }

    /// <summary>
    /// 注册一次性消息
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="onMsg">消息回调</param>
    public static void RegisterOnceMsg(int msgId, Action<Message> onMsg)
    {
        if(_msgOnceRegisters.ContainsKey(msgId))
        {
            Log.Warning($"消息ID {msgId} 已注册, 覆盖旧回调");
            _msgOnceRegisters[msgId] = onMsg;
        }
        else
        {
            _msgOnceRegisters.Add(msgId, onMsg);
        }
    }

    /// <summary>
    /// 取消注册消息
    /// </summary>
    /// <param name="msgId">消息ID</param>
    public static void UnRegisterMsg(int msgId)
    {
        _msgRegisters.Remove(msgId);
    }

    /// <summary>
    /// 取消注册消息
    /// </summary>
    public static void UnRegisterMsg<T>() where T : Message, new()
    {
        var msgId = new T().MsgId;
        UnRegisterMsg(msgId);
    }

    /// <summary>
    /// 取消注册一次性消息
    /// </summary>
    /// <param name="msgId">消息ID</param>
    public static void UnRegisterOnceMsg(int msgId)
    {
        _msgOnceRegisters.Remove(msgId);
    }

    /// <summary>
    /// 取消注册一次性消息
    /// </summary>
    public static void UnRegisterOnceMsg<T>() where T : Message, new()
    {
        var msgId = new T().MsgId;
        UnRegisterOnceMsg(msgId);
    }
}