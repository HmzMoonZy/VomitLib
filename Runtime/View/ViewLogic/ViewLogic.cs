using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FluentAPI;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public abstract class ViewLogic<TParam> : ViewLogic where TParam : ViewParameterBase
    {
        protected TParam Param;
        
        protected abstract void OnOpened(TParam param);
        
        public override void OnOpened(ViewParameterBase param)
        {
            Param = param as TParam;
            OnOpened((TParam)param);
        }
    }
    
    [DisallowMultipleComponent]
    public abstract class ViewLogic : MonoController
    {
        /// <summary>
        /// UI 的名称, 必须是唯一标识.
        /// 可以用作查找 Prefab 和 管理的 ID.
        /// </summary>
        public string ID { get; set; }
        

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
        /// <summary>
        /// UI 面板通用属性
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public ViewConfig Config
        {
            get
            {
                if (_config != null) return _config;

                _config = GetComponent<ViewConfig>();

                if (_config == null)
                {
                    throw new NullReferenceException($"{ID}没有对应的ViewConfig!");
                }

                return _config;
            }
        }
        

        private RectTransform _rectView;
        /// <summary>
        /// 面板下命名为'View'的子节点
        /// </summary>
        protected RectTransform RectView => _rectView ??= transform.Find("View").GetComponent<RectTransform>();


        
        /// <summary>
        /// View 在该 Layer 下的层级.
        /// </summary>
        public int SortOrder
        {
            get => ViewCanvas.sortingOrder;
            set => ViewCanvas.sortingOrder = value;
        }
        
        #region 生命周期

        /// <summary>
        /// 当 ViewInfo 被加载时调用.
        /// 如果这个面板不缓存, 每个UI实例都只会被调用一次
        /// </summary>
        public virtual void OnCreated()
        {
           
        }

        /// <summary>
        /// 当 ViewInfo 被展示后调用.
        /// </summary>
        public abstract void OnOpened(ViewParameterBase param);

        /// <summary>
        /// 关闭时调用.
        /// </summary>
        /// <param name="isCache">是否缓存</param>
        /// <param name="param">关闭参数</param>
        /// <returns></returns>
        public virtual void OnClose(ViewParameterBase param = null)
        {
            return;
        }

        /// <summary>
        /// Update 回调，由ViewUpdateManager统一调用
        /// 子类可以重写此方法来实现自定义的Update逻辑
        /// </summary>
        /// <param name="deltaTime">帧间时间</param>
        /// <param name="unscaledDeltaTime">未缩放的帧间时间</param>
        public virtual void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            // 默认实现为空，子类可以重写
        }

        #endregion
        
        private List<IUnRegister> _viewEvents = new();
        
        /// <summary>
        /// 注册事件, 当 View 被关闭或隐藏后,事件会被移除.
        /// </summary>
        /// <param name="onEvent"></param>
        /// <typeparam name="T"></typeparam>
        protected new void RegisterEvent<T>(Action<T> onEvent) where T : struct
        {
            _viewEvents.Add(this.RegisterEventWithoutUnRegister(onEvent));
        }
        
        private CancellationTokenSource _closeCts;

        public CancellationToken CancellationToken
        {
            get
            {
                _closeCts ??= new CancellationTokenSource();
                return _closeCts.Token;
            }
        }

        public UniTask WaitClose()
        {
            return UniTask.WaitUntilCanceled(CancellationToken);
        }
        
        public void Cancel()
        {
            _closeCts?.CancelAndDispose();
            _closeCts = null;
                
            // 移除事件
            foreach (var unRegister in _viewEvents)
            {
                unRegister.UnRegister();
            }
            _viewEvents.Clear();
        }
        
        protected void CloseSelf()
        {
            View.Close(ID);
        }

        protected void Freeze()
        {
            View.Freeze(ID);
        }

        protected void UnFreeze()
        {
            View.UnFreeze(ID);
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
                trans.DestroyChildren();
                return;
            }

            trans.DestroyChildrenWithCondition(child => !child.TryGetComponent<LayoutElement>(out var element) || !element.ignoreLayout);
        }
    }
    
}