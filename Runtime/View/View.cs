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
        private static List<string> _loadingViews = new List<string>();

        /// <summary>
        /// 所有隐藏的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _hiddenViewMap = new Dictionary<string, ViewLogic>();

        private static Action<bool> _onLoadingView; 
        
        private static IViewLoader _loader;
        private static IViewBinder _binder;
        private static IViewMasker _masker;
        private static IViewLocalizer _localizer;
        private static IViewRecorder _recorder;
        private static IViewLocker _locker;

        /// <summary>
        /// Update循环管理
        /// </summary>
        private static ViewUpdateManager _updateManager;

        public static async UniTask Init(
            IViewLoader loader = null, 
            IViewBinder binder = null, 
            IViewMasker masker = null, 
            IViewLocalizer localizer = null, 
            IViewRecorder recorder = null, 
            IViewLocker locker = null, 
            Action<bool> onLoadingView = null)
        {
            _loader = loader ?? new ViewLoaderAddressable(viewName => $"View/{viewName}.prefab");
            _binder = binder ?? new ViewBinder(null);
            _masker = masker ?? new ViewMasker(new Color(0, 0, 0, 0.5f));
            _localizer = localizer ?? new ViewLocalizer();
            _recorder = recorder ?? new ViewRecorder();
            _locker = locker ?? new ViewLocker();
            
            _onLoadingView = onLoadingView;
            
            // 初始化Update管理器
            _updateManager = new ViewUpdateManager();
            _updateManager.StartUpdateManager().Forget();

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
                    var prefab = await _loader.CreateView(viewName, Root.HiddenCanvas);
                    _preLoadMap.Add(viewName, prefab);
                    Log.Debug($"预加载成功: {viewName}, 优先级: {priority}");

                    if (alwaysDisplay)
                    {
                        // 显示预加载的View
                        Open(viewName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"预加载失败: {viewName}, 错误: {ex.Message}");
                }
            }

            Log.Debug("预加载完成");
        }

        public static void Open<T>(ViewParameterBase param = null) where T : ViewLogic, new()
        {
            OpenAsync<T>(param).Forget();
        }

        public static void Open(string viewName, ViewParameterBase param = null)
        {
            OpenAsync(viewName, param).Forget();
        }

        public static async UniTask<T> OpenAsync<T>(ViewParameterBase param = null) where T : ViewLogic, new()
        {
            return (T)await OpenAsync(typeof(T).Name, param);
        }
        
        public async static UniTask<ViewLogic> OpenAsync(string viewName, ViewParameterBase param = null)
        {
            if (_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                Log.Warning($"Try to open an already showed the View : {viewName}");
                return logic;
            }

            if (_loadingViews.Contains(viewName))
            {
                Log.Warning($"Try to open an already showed the View : {viewName}");
                return null;
            }

            _hiddenViewMap.Remove(viewName, out logic);     // 移除隐藏列表
            
            // 进入加载流程
            if (logic == null)
            {
                _loadingViews.Add(viewName);
                if (_preLoadMap.TryGetValue(viewName, out var viewObject))
                {
                    viewObject.transform.SetParent(Root.transform, false);
                }
                else
                {
                    _onLoadingView?.Invoke(true);
                    viewObject = await _loader.CreateView(viewName, Root.transform);
                    _onLoadingView?.Invoke(false);
                }

                logic = viewObject.GetComponent<ViewLogic>();

                if (logic == null)
                {
                    Log.Error($"ViewLogic : {viewName} is not found!");
                    return null;
                }
                _loadingViews.Remove(viewName);
                CreateLogic(viewName, logic);
            }

            OpenLogic(logic, param);

            // 注册到Update管理器
            _updateManager?.RegisterView(logic);

            return logic;
        }

        private static void CreateLogic(string viewName, ViewLogic logic)
        {
            logic.ID = viewName;

            if (logic.Config.AutoBindButtons)       // 绑定组件
            {
                _binder?.Bind(logic);
            }

            if (logic.Config.EnableLocalization)    // 本地化
            {
                _localizer?.Localize(logic);
            }

            logic.OnCreated();

            Vomit.Interface?.SendEvent(new EvtView.Create
            {
                ViewLogic = logic
            });
        }

        private static void OpenLogic(ViewLogic logic, ViewParameterBase param = null)
        {
            // 展示逻辑
            _visibleViewMap[logic.ID] = logic;

            if (logic.Config.EnableAutoMask)        // 遮罩
            {
                _masker?.Mask(logic);
            }

            logic.transform.SetParent(Root.transform, false);
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = Root.ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int)logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;

            logic.OnOpened(param);

            Vomit.Interface?.SendEvent(new EvtView.Open
            {
                ViewLogic = logic,
            });

            if (logic.Config.RecordOpen)
            {
                _recorder?.RecordOpen(logic.ID);
            }
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
                CacheLogic(logic);
            }
            else
            {
                DestroyLogic(logic);
            }

            Vomit.Interface?.SendEvent(new EvtView.Close()
            {
                ViewName = viewName,
                IsCache = logic.Config.IsCache,
            });
        }

        public static void Close<T>(ViewParameterBase param = null)
        {
            Close(typeof(T).Name);
        }

        private static void CacheLogic(ViewLogic logic)
        {
            logic.Parent(Root.HiddenCanvas);
            _hiddenViewMap.Add(logic.ID, logic);
            if (logic.Config.EnableAutoMask)
            {
                _masker?.Unmask(logic);
            }
            _locker?.UnLock(logic);            
        }

        private static void DestroyLogic(ViewLogic logic)
        {
            Object.Destroy(logic.gameObject);
            _loader?.ReleaseView(logic.gameObject);
        }

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
        /// 判断一个View是否可见
        /// </summary>
        public static bool IsViewVisible<T>() where T : ViewLogic
        {
            return _visibleViewMap.ContainsKey(typeof(T).Name);
        }

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
                var rr = view.transform.GetComponent<GraphicRaycaster>();
                if (rr == null) continue;

                rr.Raycast(ed, list);

                if (list.Count > 0) return true;
            }

            return false;
        }

        public static bool IsFirstOpen<T>() where T : ViewLogic
        {
            if (_recorder == null)
            {
                throw new Exception("请先初始化 ViewRecorder");
            }

            return _recorder.IsFirstOpen(typeof(T).Name);
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
        

    }
}