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
    /// <summary>
    /// 自定义参数的View逻辑
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
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
    
    /// <summary>
    /// View运行时逻辑
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ViewLogic : MonoController
    {
        /// <summary>
        /// UI 的名称, 唯一标识.
        /// 实例创建时框架确定, 业务层只读.
        /// </summary>
        public string Id { get; set; } 
        

        private Canvas _viewCanvas;
        /// <summary>
        /// View 的 Canvas
        /// </summary>
        public Canvas ViewCanvas => _viewCanvas ??= GetComponent<Canvas>();

        [SerializeField] private ViewConfig _config;
        /// <summary>
        /// View 通用配置
        /// </summary>
        public ViewConfig Config => _config;
        

        private RectTransform _rectView;
        /// <summary>
        /// 面板下命名为'#View'的子节点
        /// </summary>
        protected RectTransform RectView => _rectView ??= transform.Find("#View")?.GetComponent<RectTransform>();
        
        /// <summary>
        /// 动画组件
        /// </summary>
        private Animation _animation;
        protected Animation Animation => _animation ??= GetComponent<Animation>();

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
            View.Close(Id);
        }

        protected void Freeze()
        {
            View.Freeze(Id);
        }

        protected void UnFreeze()
        {
            View.UnFreeze(Id);
        }
    }
    
}