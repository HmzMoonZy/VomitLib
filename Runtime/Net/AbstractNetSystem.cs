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
public abstract class AbstractNetSystem : QFramework.AbstractSystem
{
    /// <summary>
    /// 消息ID
    /// </summary>
    private static int UniId { set; get; } = 200;

    /// <summary>
    /// 消息注册
    /// </summary>
    private static Dictionary<int, Action<Message>> _msgRegisters = new();

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
    public static async UniTask<bool> Init(MessagePack.IFormatterResolver resolver, string host, int port, int errRespMsgId, [NotNull]Func<Message, bool> onErrCodeResp)
    {
        _onErrCodeResp = onErrCodeResp;

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
            }).Forget();

            // 连接服务器
            return await NetClient.Instance.Connect(host, port);
        }
        catch (System.Exception e)
        {
            Log.Error($"AbstractNetSystem]初始化网络系统失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 收到消息
    /// </summary>
    /// <param name="msg">消息</param>
    public static void RecvMsg(Message msg)
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
    }

    /// <summary>
    /// 发送消息
    protected UniTask<bool> SendMsg(Message msg)
    {
        msg.UniId = UniId++;
        NetClient.Instance.Send(msg);
        Log.Debug("开始等待消息:" + msg.UniId);
        return MsgWaiter.StartWait(msg.UniId);
    }

    /// <summary>
    /// 获取当前消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="msg">消息</param>
    /// <returns></returns>
    protected T GetCurMsg<T>(object msg) where T : Message, new()
    {
        return msg as T;
    }

    /// <summary>
    /// 注册消息
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="onMsg">消息回调</param>
    protected void RegisterMsg(int msgId, Action<Message> onMsg)
    {
        if(_msgRegisters.ContainsKey(msgId))
        {
            Log.Warning($"消息ID {msgId} 已注册");
            _msgRegisters[msgId] = onMsg;
        }
        else
        {
            _msgRegisters.Add(msgId, onMsg);
        }
    }

    protected void UnRegisterMsg(int msgId)
    {
        _msgRegisters.Remove(msgId);
    }
}