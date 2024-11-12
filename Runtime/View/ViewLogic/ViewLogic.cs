using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public abstract class ViewLogic<TParam> : ViewLogic where TParam : ViewParameterBase
    {
        protected TParam Param;
        
        protected abstract UniTask OnOpened(TParam param);
        
        public override UniTask OnOpened(ViewParameterBase param)
        {
            Param = param as TParam;
            return OnOpened((TParam)param);
        }
    }
    
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
                    throw new NullReferenceException($"{Name}没有对应的ViewConfig!");
                }

                return _config;
            }
        }

        private RectTransform _rectView;
        /// <summary>
        /// 面板下命名为'View'的子节点
        /// </summary>
        protected RectTransform RectView => _rectView ??= transform.Find("View").GetComponent<RectTransform>();
        
        
        private Animation _animation;
        
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
        /// 当 ViewInfo 被加载时,初始化前被调用.
        /// </summary>
        public virtual void OnCreated()
        {
           
        }

        public virtual UniTask PlayOpenAnimation()
        {
            _animation ??= GetComponent<Animation>();
            
            if (_animation == null)
            {
                return UniTask.CompletedTask;
            }

            var state = _animation["Open"];

            if (state == null)
            {
                state = _animation[$"{Name}#Open"];
            }

            if (state == null)
            {
                return UniTask.CompletedTask;
            }
            
            _animation.clip = state.clip;
            _animation.Play();
            try
            {
                return UniTask.WaitWhile(() => _animation.isPlaying, cancellationToken: CancellationToken);
            }
            catch (Exception e)
            {
                return UniTask.CompletedTask;
            }
        }

        public virtual UniTask PlayCloseAnimation()
        {
            if (_animation == null)
            {
                return UniTask.CompletedTask;
            }

            var clip = _animation.GetClip("Close");

            if (clip == null)
            {
                clip = _animation.GetClip($"{Name}#Close");
            }

            if (clip == null)
            {
                return UniTask.CompletedTask;
            }

            _animation.clip = clip;
            _animation.Play();
            try
            {
                return UniTask.WaitWhile(() => _animation.isPlaying, cancellationToken: CancellationToken);
            }
            catch (Exception e)
            {
                return UniTask.CompletedTask;
            }
        }
        
        /// <summary>
        /// 当 ViewInfo 被展示后调用.
        /// </summary>
        public abstract UniTask OnOpened(ViewParameterBase param);

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
        
        protected UniTask CloseSelf()
        {
            return View.CloseAsync(Name);
        }

        public void Freeze()
        {
            var rayCaster = transform.GetComponent<GraphicRaycaster>();
            if (rayCaster != null)
            {
                rayCaster.enabled = false;
            }
        }

        public void UnFreeze()
        {
            var rayCaster = transform.GetComponent<GraphicRaycaster>();
            if (rayCaster != null)
            {
                rayCaster.enabled = true;
            }
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