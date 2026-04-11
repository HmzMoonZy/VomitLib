using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画事件接收器 - 挂载在有 Animator 组件的子节点上
/// 用于将 Animation Event 转发到父节点的逻辑脚本
/// </summary>
public class AnimEventReceiver : MonoBehaviour
{
    // 使用 Action 而不是 Dictionary，大多数情况下只需要一个回调
    private Action<string> _onEvent;
    
    /// <summary>
    /// 注册动画事件回调
    /// </summary>
    public void RegisterCallback(Action<string> callback)
    {
        _onEvent += callback;
    }
    
    /// <summary>
    /// 取消注册
    /// </summary>
    public void UnregisterCallback(Action<string> callback)
    {
        _onEvent -= callback;
    }
    
    /// <summary>
    /// 清空所有回调
    /// </summary>
    public void ClearCallbacks()
    {
        _onEvent = null;
    }
    
    // 被 Animator Event 调用
    public void OnAnimEvent(string eventName)
    {
        _onEvent?.Invoke(eventName);
    }
    
    private void OnDestroy()
    {
        ClearCallbacks();
    }
}