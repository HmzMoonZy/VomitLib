using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        private readonly List<ViewLogic> _registeredViews = new();

        /// <summary>
        /// 待移除的ViewLogic列表（避免在Update过程中修改集合）
        /// </summary>
        private readonly List<ViewLogic> _pendingRemoval = new();

        /// <summary>
        /// 是否正在Update过程中
        /// </summary>
        private bool _isUpdating = false;

        /// <summary>
        /// 注册一个ViewLogic到Update循环
        /// </summary>
        public void RegisterView(ViewLogic viewLogic)
        {
            if (viewLogic == null) return;

            if (!_registeredViews.Contains(viewLogic))
            {
                _registeredViews.Add(viewLogic);
                Log.Debug($"ViewUpdateManager: Registered {viewLogic.ID} for update");
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
                if (!_pendingRemoval.Contains(viewLogic))
                {
                    _pendingRemoval.Add(viewLogic);
                }
            }
            else
            {
                // 直接移除
                if (_registeredViews.Remove(viewLogic))
                {
                    Log.Debug($"ViewUpdateManager: Unregistered {viewLogic.ID} from update");
                }
            }
        }

        public async UniTaskVoid StartUpdateManager()
        {
            while (Application.isPlaying)
            {
                try
                {
                    Update(Time.deltaTime, Time.unscaledDeltaTime);
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
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (_registeredViews.Count == 0) return;

            _isUpdating = true;

            try
            {
                // 遍历所有注册的View，调用它们的Update方法
                for (int i = _registeredViews.Count - 1; i >= 0; i--)
                {
                    var viewLogic = _registeredViews[i];

                    // 检查ViewLogic是否仍然有效
                    if (viewLogic == null || viewLogic.gameObject == null)
                    {
                        _registeredViews.RemoveAt(i);
                        continue;
                    }

                    // 只有激活的View才执行Update
                    if (viewLogic.gameObject.activeInHierarchy)
                    {
                        try
                        {
                            viewLogic.OnUpdate(deltaTime, unscaledDeltaTime);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"ViewLogic {viewLogic.ID} Update failed: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                _isUpdating = false;

                // 处理待移除的View
                if (_pendingRemoval.Count > 0)
                {
                    foreach (var viewLogic in _pendingRemoval)
                    {
                        _registeredViews.Remove(viewLogic);
                        Log.Debug($"ViewUpdateManager: Removed {viewLogic?.ID} from update (pending)");
                    }
                    _pendingRemoval.Clear();
                }
            }
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
        /// 获取当前注册的ViewLogic数量
        /// </summary>
        public int RegisteredCount => _registeredViews.Count;

        /// <summary>
        /// 检查某个ViewLogic是否已注册
        /// </summary>
        public bool IsRegistered(ViewLogic viewLogic)
        {
            return viewLogic != null && _registeredViews.Contains(viewLogic);
        }

        /// <summary>
        /// 获取所有已注册的ViewLogic（只读）
        /// </summary>
        public IReadOnlyList<ViewLogic> GetRegisteredViews()
        {
            return _registeredViews.AsReadOnly();
        }
    }
}