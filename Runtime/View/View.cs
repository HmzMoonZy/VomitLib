using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FluentAPI;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/*
 * TODO 自动移除缓存的View.
 */

namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// 基于 QFramework 的UI管理器.
    /// </summary>
    public static class View
    {
        private static ViewRoot _root;
        
        /// <summary>
        /// View 根节点.
        /// </summary>
        public static ViewRoot Root
        {
            get
            {
                _root ??= Object.FindObjectOfType<ViewRoot>();

                if (_root is null)
                {
                    var prefab = Resources.Load<GameObject>("ViewRoot");
                    _root = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<ViewRoot>();
                    if (_root is null)
                    {
                        throw new NotImplementedException("No \"ViewRoot\" component was added in the scene!");
                    }
                }
                
                return _root;
            }
        }

        /// <summary>
        /// 预加载的View
        /// </summary>
        private static Dictionary<string, GameObject> _preLoadMap = new();
        
        /// <summary>
        /// 所有被激活的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _visibleViewMap = new Dictionary<string, ViewLogic>();

        /// <summary>
        /// 所有正在加载的View
        /// </summary>
        private static HashSet<string> _loadingViews = new HashSet<string>();

        /// <summary>
        /// 所有隐藏的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _hiddenViewMap = new Dictionary<string, ViewLogic>();

        /// <summary>
        /// 当 View 加载时
        /// </summary>
        private static Action<bool> _onLoadingView; 
        
        private static IViewLoader _loader;
        private static IViewBinder _binder;
        private static IViewMasker _masker;
        private static IViewLocalizer _localizer;
        private static IViewLocker _locker;

        /// <summary>
        /// Update循环管理
        /// </summary>
        private static ViewUpdateManager _updateManager;

        #region Init
        
        public static async UniTask Init(
            IViewLoader loader = null, 
            IViewBinder binder = null, 
            IViewMasker masker = null, 
            IViewLocalizer localizer = null, 
            IViewLocker locker = null, 
            Action<bool> onLoadingView = null)
        {
            _loader = loader ?? new ViewLoaderAddressable(viewName => $"View/{viewName}.prefab");
            _binder = binder ?? new ViewBinder(null);
            _masker = masker ?? new ViewMasker(new Color(0, 0, 0, 0.5f));
            _localizer = localizer ?? new ViewLocalizer();
            _locker = locker ?? new ViewLocker();
            
            _onLoadingView = onLoadingView;
            
            // 初始化Update管理器
            _updateManager = new ViewUpdateManager();
            _updateManager.StartUpdateManager();

            // 预加载
            await PreloadViews();       // TODO  外抛进度
        }

        private static async UniTask PreloadViews()
        {
            Log.Debug("开始扫描和预加载View");

            var preloadableViews = new List<(string viewName, int priority, bool alwaysDisplay)>();

            // 获取当前域中的所有程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var searchType = typeof(ViewLogic);
                    // 查找继承自ViewLogic的类
                    foreach (var viewType in assembly.GetTypes().Where(type => type.IsSubclassOf(searchType) && !type.IsAbstract))
                    {
                        // 检查是否有ViewPreloadAttribute
                        var preloadAttr = viewType.GetCustomAttribute<ViewPreloadAttribute>();
                        if (preloadAttr != null)
                        {
                            preloadableViews.Add((viewType.Name, preloadAttr.Priority, preloadAttr.AlwaysDisplay));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 跳过无法加载的程序集
                    Log.Warning($"跳过程序集 {assembly.FullName}: {ex.Message}");
                }
            }

            // 按优先级排序（数字越小优先级越高）
            preloadableViews.Sort((a, b) => a.priority.CompareTo(b.priority));
            
            Log.Debug($"找到 {preloadableViews.Count} 个需要预加载的View");

            // 预加载所有View资源
            foreach (var (viewName, priority, alwaysDisplay) in preloadableViews)
            {
                try
                {
                    var view = await _loader.CreateViewAsync(viewName, Root.HiddenCanvas);
                    _preLoadMap.Add(viewName, view);
                    Log.Debug($"预加载成功: {viewName}, 优先级: {priority}");

                    #if UNITY_EDITOR
                    if (view.GetComponent<ViewConfig>().IsCache == false)
                    {
                        Log.Warning($"预加载的View {viewName} 未设置缓存, 可能会引发未知的错误.");
                    }
                    #endif
                    
                    if (alwaysDisplay)
                    {
                        // 显示预加载的View
                        await OpenAsync(viewName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"预加载失败: {viewName}, 错误: {ex.Message}");
                }
            }

            Log.Debug("预加载完成");
        }
        
        #endregion
        
        #region Open View
        
        #region Open

        public static T OpenStandalone<T>(ViewParameterBase param = null) where T : ViewLogic, new()
        {
            if (_preLoadMap.ContainsKey(typeof(T).Name))
            {
                Log.Error("不能独立打开一个预加载的 View");
                return null;
            }
            return __Open($"{typeof(T).Name}_{Guid.NewGuid()}", param) as T;
        }
        
        public static T Open<T>(ViewParameterBase param = null) where T : ViewLogic, new()
        {
            return __Open(typeof(T).Name, param) as T;
        }
        
        public static ViewLogic Open(string viewId, ViewParameterBase param = null)
        {
            return __Open(viewId, param);
        }
        
        private static ViewLogic __Open(string viewId, ViewParameterBase param = null)
        {
            if (_loadingViews.Contains(viewId))
            {
                Log.Warning($"Try to open an already showed the View : {viewId}");
                return null;
            }
            
            if (_visibleViewMap.TryGetValue(viewId, out var logic))
            {
                Log.Warning($"Try to open an already showed the View : {viewId}");
                return logic;
            }

            _hiddenViewMap.Remove(viewId, out logic); // 移除隐藏列表
            
            // 进入创建流程
            if (logic == null)
            {
                _loadingViews.Add(viewId);

                var viewName = viewId.Contains('_') ? viewId.Split('_')[0] : viewId;
                
                if (_preLoadMap.TryGetValue(viewName, out var viewObject))
                {
                    viewObject.transform.SetParent(Root.transform, false);
                }
                else
                {
                    viewObject = _loader.CreateView(viewName, Root.HiddenCanvas);
                }

                logic = viewObject.GetComponent<ViewLogic>();
                logic.Id = viewId;
                __OnCreateLogic(logic);
            }
            
            // 开启流程
            __OnOpenLogic(logic, param);
            
            return logic;
        }

        #endregion

        #region OpenAsync
        
        public static void OpenAsync<T>(ViewParameterBase param, Action<T> callback) where T : ViewLogic, new()
        {
            UniTask.Create(async () =>
            {
                var logic = await __OpenAsync(typeof(T).Name, param);
                callback?.Invoke(logic as T);
            });
        }

        public static void OpenAsync(string viewId, ViewParameterBase param, Action<ViewLogic> callback)
        {
            UniTask.Create(async () =>
            {
                var logic = await __OpenAsync(viewId, param);
                callback?.Invoke(logic);
            });
        }

        public static async UniTask<T> OpenAsync<T>(ViewParameterBase param = null) where T : ViewLogic, new()
        {
            return (T)await __OpenAsync(typeof(T).Name, param);
        }
        
        public static async UniTask<ViewLogic> OpenAsync(string viewId, ViewParameterBase param = null)
        {
            return await __OpenAsync(viewId, param);
        }

        /// <summary>
        /// 内部方法, 异步开启 View
        /// </summary>
        private static async UniTask<ViewLogic> __OpenAsync(string viewId, ViewParameterBase param = null)
        {
            if (_loadingViews.Contains(viewId))
            {
                Log.Warning($"Try to open an already showed the View : {viewId}");
                return null;
            }
            
            if (_visibleViewMap.TryGetValue(viewId, out var logic))
            {
                Log.Warning($"Try to open an already showed the View : {viewId}");
                return logic;
            }

            _hiddenViewMap.Remove(viewId, out logic); // 移除隐藏列表

            // 进入创建流程
            if (logic == null)
            {
                _loadingViews.Add(viewId);

                var viewName = viewId.Contains('_') ? viewId.Split('_')[0] : viewId;
                
                if (_preLoadMap.TryGetValue(viewName, out var viewObject))
                {
                    viewObject.transform.SetParent(Root.transform, false);
                }
                else
                {
                    _onLoadingView?.Invoke(true);
                    viewObject = await _loader.CreateViewAsync(viewName, Root.transform);
                    _onLoadingView?.Invoke(false);
                }

                logic = viewObject.GetComponent<ViewLogic>();
                logic.Id = viewId;
                __OnCreateLogic(logic);
            }

            // 开启流程
            __OnOpenLogic(logic, param);

            return logic;
        }

        #endregion
        
        private static void __OnCreateLogic(ViewLogic logic)
        {
            if (logic == null)
            {
                Log.Error($"ViewLogic : __CreateLogic Error : logic is null");
                return;
            }

            _loadingViews.Remove(logic.Id); // 移除加载中列表

            // 绑定组件
            if (logic.Config.AutoBindButtons)
            {
                _binder?.Bind(logic);
            }

            // 本地化
            if (logic.Config.EnableLocalization)
            {
                _localizer?.Localize(logic);
            }
            
            logic.ViewCanvas.enabled = true;
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = Root.ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int)logic.Config.Layer;

            Log.Debug($"View - {logic.Id} Created");
            logic.OnCreated();
            Vomit.Interface?.SendEvent(new EvtView.Created
            {
                ViewLogic = logic
            });
        }
        
        private static void __OnOpenLogic(ViewLogic logic, ViewParameterBase param = null)
        {
            // 展示逻辑
            _visibleViewMap[logic.Id] = logic;

            if (logic.Config.EnableAutoMask)        // 遮罩
            {
                _masker?.Mask(logic);
            }

            // TODO 刘海屏适配
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            Log.Debug($"View - {logic.Id} Opened");
            logic.OnOpened(param);
            Vomit.Interface?.SendEvent(new EvtView.Open
            {
                ViewLogic = logic,
            });
            
            // 注册到Update管理器
            _updateManager?.RegisterView(logic);
            
            #if UNITY_EDITOR
            logic.gameObject.name = $"{logic.Id}({logic.Config.Layer.ToString()}-{logic.SortOrder.ToString()})";
            #endif
        }

        #endregion

        #region Close View
        
        public static void Close<T>(ViewParameterBase param = null)
        {
            Close(typeof(T).Name);
        }
        
        public static void Close(string viewName, ViewParameterBase param = null)
        {
            if (!_visibleViewMap.Remove(viewName, out var logic))
            {
                Log.Debug($"Try closing a non-existent View : {viewName}");
                return;
            }

            // 取消监听器
            logic.Cancel();

            // 从Update管理器中移除
            _updateManager?.UnregisterView(logic);

            logic.OnClose(param);

            if (logic.Config.IsCache)
            {
                __CacheLogic(logic);
            }
            else
            {
                __DestroyLogic(logic);
            }

            Vomit.Interface?.SendEvent(new EvtView.Close()
            {
                ViewName = viewName,
                IsCache = logic.Config.IsCache,
            });
        }
        
        private static void __CacheLogic(ViewLogic logic)
        {
            logic.Parent(Root.HiddenCanvas);
            _hiddenViewMap.Add(logic.Id, logic);
            if (logic.Config.EnableAutoMask)
            {
                _masker?.Unmask(logic);
            }
            _locker?.UnLock(logic);            
        }

        private static void __DestroyLogic(ViewLogic logic)
        {
            Object.Destroy(logic.gameObject);
            _loader?.ReleaseView(logic.gameObject);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 打开一个View并等待它关闭
        /// </summary>
        public static async UniTask OpenAndWaitClose<T>(ViewParameterBase param = null) where T : ViewLogic, new()
        {
            await (await OpenAsync<T>(param)).WaitClose();
        }
        
        /// <summary>
        /// 获取 T 类型的 ViewLogic.
        /// 只要它在缓存中,就能被获取到.
        /// </summary>
        public static T GetView<T>(bool findHidden = false) where T : ViewLogic
        {
            if (_visibleViewMap.TryGetValue(typeof(T).Name, out var info))
            {
                return (T)info;
            }

            if (findHidden && _hiddenViewMap.TryGetValue(typeof(T).Name, out info))
            {
                return (T)info;
            }

            return null;
        }
        
        public static ViewLogic GetView(string viewName, bool findHidden = false)
        {
            if (_visibleViewMap.TryGetValue(viewName, out var view))
            {
                return view;
            }

            if (findHidden && _hiddenViewMap.TryGetValue(viewName, out view))
            {
                return view;
            }

            return null;
        }
        
        /// <summary>
        /// 获取最顶层的 View
        /// </summary>
        /// <returns></returns>
        public static ViewLogic GetTop()
        {
            var maxLayer = _visibleViewMap.Values.Max(info => info.Config.Layer);
            var maxSort = _visibleViewMap.Values.Where(info => info.Config.Layer == maxLayer).Max(info => info.SortOrder);
            return _visibleViewMap.Values.First(info => info.SortOrder == maxSort && info.Config.Layer == maxLayer);
        }
        
        /// <summary>
        /// 如果给定的T是最顶层, 返回True
        /// </summary>
        /// <param name="ignoreTypes">忽略的类型</param>
        /// <returns></returns>
        public static bool IsTop<T>(params Type[] ignoreTypes) where T : ViewLogic
        {
            var view = GetView<T>();

            if (view == null)
            {
                return false;
            }

            if (!_visibleViewMap.ContainsValue(view))
            {
                return false;
            }

            int sortOrder = view.SortOrder;
            var layer = view.Config.Layer;

            return _visibleViewMap.Values.All(logic =>
            {
                if (logic == view || ignoreTypes.Contains(logic.GetType()))
                {
                    return true;
                }

                if (logic.Config.Layer < layer)
                {
                    return true;
                }

                if (logic.Config.Layer == layer)
                {
                    return logic.SortOrder <= sortOrder;
                }

                return false;
            });

        }
        
        /// <summary>
        /// 判断给定位置是否在某个View上
        /// </summary>
        public static bool IsHitView(Vector3 position)
        {
            PointerEventData ed = new(Root.GetComponent<EventSystem>())
            {
                pressPosition = position,
                position = position
            };

            var list = new List<RaycastResult>();

            foreach (var (_, view) in _visibleViewMap)
            {
                var raycaster = view.transform.GetComponent<GraphicRaycaster>();
                if (raycaster == null) continue;

                raycaster.Raycast(ed, list);

                if (list.Count > 0) return true;
            }

            return false;
        }
        
        #region Freeze

        public static void Freeze(string viewName)
        {
            var view = GetView(viewName);

            if (view != null)
            {
                Debug.Log($"Freeze {viewName}");
                _locker?.Lock(view);
            }
        }

        public static void Freeze<T>() where T : ViewLogic
        {
            Freeze(typeof(T).Name);
        }

        public static void UnFreeze(string viewName)
        {
            var view = GetView(viewName);

            if (view != null)
            {
                Debug.Log($"UnFreeze {viewName}");
                _locker?.UnLock(view);
            }
        }

        public static void UnFreeze<T>() where T : ViewLogic
        {
            UnFreeze(typeof(T).Name);
        }

        #endregion
        
        #endregion
    }
}