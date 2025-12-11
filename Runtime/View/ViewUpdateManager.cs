using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FluentAPI;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// ViewLogic的Update管理器
    /// 统一管理所有可见ViewLogic的Update调用
    /// </summary>
    public class ViewUpdateManager
    {
        /// <summary>
        /// 已注册的ViewLogic列表
        /// </summary>
        private readonly Dictionary<string, ViewLogic> _registeredViews = new();

        /// <summary>
        /// 待移除的ViewLogic列表（避免在Update过程中修改集合）
        /// </summary>
        private readonly HashSet<string> _pendingRemoval = new();

        /// <summary>
        /// 获取当前注册的ViewLogic数量
        /// </summary>
        public int RegisteredCount => _registeredViews.Count;
        
        /// <summary>
        /// 是否正在Update过程中
        /// </summary>
        private bool _isUpdating = false;

        /// <summary>
        /// 注册一个ViewLogic到Update循环
        /// </summary>
        public void RegisterView(ViewLogic viewLogic)
        {
            if (viewLogic == null || viewLogic.Id.IsNullOrEmpty()) return;

            if (!_registeredViews.ContainsKey(viewLogic.Id))
            {
                _registeredViews.Add(viewLogic.Id, viewLogic);
                Log.Debug($"ViewUpdateManager: Registered {viewLogic.Id} for update");
            }
        }

        /// <summary>
        /// 从Update循环中移除ViewLogic
        /// </summary>
        public void UnregisterView(ViewLogic viewLogic)
        {
            if (viewLogic == null) return;

            if (_isUpdating)
            {
                // 如果正在Update过程中，添加到待移除列表
                _pendingRemoval.Add(viewLogic.Id);
            }
            else
            {
                // 直接移除
                if (_registeredViews.Remove(viewLogic.Id))
                {
                    Log.Debug($"ViewUpdateManager: Unregistered {viewLogic.Id} from update");
                }
            }
        }

        public async UniTaskVoid StartUpdateManager()
        {
            _isUpdating = true;
            
            while (Application.isPlaying && _isUpdating)
            {
                var deltaTime = Time.deltaTime;
                var unscaledDeltaTime = Time.unscaledDeltaTime;
                
                try
                {
                    foreach (var (viewId, viewLogic) in _registeredViews)
                    {
                        // 检查ViewLogic是否仍然有效
                        if (viewLogic == null || viewLogic.gameObject == null)
                        {
                            continue;
                        }

                        try
                        {
                            viewLogic.OnUpdate(deltaTime, unscaledDeltaTime);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"ViewLogic {viewId} Update failed: {ex.Message}");
                        }
                    }
                    
                    // 处理待移除的View
                    if (_pendingRemoval.Count > 0)
                    {
                        foreach (var viewId in _pendingRemoval)
                        {
                            _registeredViews.Remove(viewId);
                            Log.Debug($"ViewUpdateManager: Removed {viewId} from update (pending)");
                        }
                        _pendingRemoval.Clear();
                    }
                    
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                catch (Exception ex)
                {
                    Log.Error($"ViewUpdateManager update failed: {ex.Message}");
                    await UniTask.Yield();
                }
            }
        }

        /// <summary>
        /// 执行所有已注册ViewLogic的Update
        /// </summary>
        private void Update(float deltaTime, float unscaledDeltaTime)
        {
   
            

        }

        /// <summary>
        /// 清空所有注册的ViewLogic
        /// </summary>
        public void Clear()
        {
            _registeredViews.Clear();
            _pendingRemoval.Clear();
            _isUpdating = false;
            Log.Debug("ViewUpdateManager: Cleared all registered views");
        }
        
        /// <summary>
        /// 检查某个ViewLogic是否已注册
        /// </summary>
        public bool IsRegistered(ViewLogic viewLogic)
        {
            return viewLogic != null && _registeredViews.ContainsKey(viewLogic.Id);
        }
        
    }
}