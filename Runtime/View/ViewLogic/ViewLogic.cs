﻿using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public abstract class ViewLogic : MonoController
    {
        /// <summary>
        /// UI 的名称, 必须是唯一标识.
        /// 可以用作查找 Prefab 和 管理的 ID.
        /// </summary>
        public string Name { get; set; }

        private Canvas _viewCanvas;

        /// <summary>
        /// 根据规范, UI 界面本身必须具有一个 Canvas 组件.
        /// </summary>
        public Canvas ViewCanvas
        {
            get
            {
                if (_viewCanvas == null)
                {
                    _viewCanvas = GetComponent<Canvas>();
                }

                return _viewCanvas;
            }
        }

        private ViewConfig _config;

        private CancellationTokenSource _closeCts;

        public ViewConfig Config
        {
            get
            {
                if (_config != null) return _config;

                _config = GetComponent<ViewConfig>();

                if (_config == null)
                {
                    throw new NullReferenceException($"{Name}没有对应的ViewConfig!");
                }

                return _config;
            }
        }

        private List<IUnRegister> _viewEvents = new();

        /// <summary>
        /// View 在该 Layer 下的层级.
        /// </summary>
        public int SortOrder
        {
            get => ViewCanvas.sortingOrder;
            set => ViewCanvas.sortingOrder = value;
        }

        /// <summary>
        /// UI 是否是可见的.
        /// </summary>
        public bool IsVisible => transform.parent.name == "ViewRoot";

        #region 生命周期

        /// <summary>
        /// 当 ViewInfo 被加载时,初始化前被调用.
        /// </summary>
        public virtual void OnCreated()
        {
           
        }

        /// <summary>
        /// 当 ViewInfo 被展示后调用.
        /// </summary>
        public abstract UniTask OnOpened();

        /// <summary>
        /// 当 ViewInfo 被关闭时调用,不论它是 Hidden 还是 Destroy.
        /// </summary>
        public virtual UniTask OnClose()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 如果 ViewInfo 被关闭后被缓存,则调用
        /// </summary>
        public virtual void OnHidden()
        {
            return;
        }

        #endregion

        #region Events
        
        protected void RegisterViewEvent<T>(Action<T> onEvent) where T : struct
        {
            _viewEvents.Add(Vomit.Interface.RegisterEvent(onEvent));
        }

        public void UnRegisterAllViewEvents()
        {
            foreach (var unRegister in _viewEvents)
            {
                unRegister.UnRegister();
            }
            _viewEvents.Clear();
        }
        
        

        #endregion

        #region API

        public void Dispose()
        {
            _closeCts?.Cancel();
            _closeCts?.Dispose();
            _closeCts = null;
        }
        
        protected CancellationToken GetViewCloseCancellationToken()
        {
            _closeCts ??= new CancellationTokenSource();
            
            return _closeCts.Token;
        }

        protected void CloseSelf()
        {
            View.CloseView(this);
        }

        protected async void CloseNextFrame()
        {
            await UniTask.NextFrame();
            CloseSelf();
        }

        protected async void CloseWhen(UniTask task)
        {
            await task;
            CloseSelf();
        }

        protected async void CloseWhen(Func<UniTask> task)
        {
            await task.Invoke();
            CloseSelf();
        }

        /// <summary>
        /// 销毁tran的所有子节点.
        /// 当你不想用对象池管理一些生成的对象时非常有用.
        /// </summary>
        /// <param name="trans">遍历的根节点</param>
        /// <param name="ignoreLayoutElement">是否忽略带有ignoreLayout的对象</param>
        protected void DestroyAllChildren(Transform trans, bool ignoreLayoutElement = true)
        {
            if (!ignoreLayoutElement)
            {
                foreach (Transform child in trans)
                {
                    Destroy(child.gameObject);
                }

                return;
            }

            foreach (Transform child in trans)
            {
                if (child.TryGetComponent<LayoutElement>(out var component) && component.ignoreLayout)
                    continue;
                Destroy(child.gameObject);
            }
        }

        #endregion
    }
}