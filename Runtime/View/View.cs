using System;
using System.Collections.Generic;
using System.Linq;
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
    public static partial class View
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
        /// 所有被激活的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _visibleViewMap = new Dictionary<string, ViewLogic>();
        
        /// <summary>
        /// 所有隐藏的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _hiddenViewMap = new Dictionary<string, ViewLogic>();
        
        private static IViewLoader _loader;
        private static IViewBinder _binder;
        private static IViewMasker _masker;
        private static IViewLocalizer _localizer;
        private static IViewRecorder _recorder;
        private static IViewLocker _locker;
        
        public static void Init(IViewLoader loader = null, IViewBinder binder = null, IViewMasker masker = null, IViewLocalizer localizer = null, IViewRecorder recorder = null, IViewLocker locker = null)
        {
            _loader = loader ?? new ViewLoaderAddressable(viewName => $"View/{viewName}.prefab");
            _binder = binder ?? new ViewBinder(null);
            _masker = masker ?? new ViewMasker(new Color(0, 0, 0, 0.5f));
            _localizer = localizer ?? new ViewLocalizer();
            _recorder = recorder ?? new ViewRecorder();
            _locker = locker ?? new ViewLocker();
        }

        public static void Init(Func<string, string> viewName2Addr, IViewBinder binder = null,
            IViewMasker masker = null, IViewLocalizer localizer = null, IViewRecorder recorder = null,
            IViewLocker locker = null)
        {
            _loader = new ViewLoaderAddressable(viewName2Addr);
            Init(_loader);
        }

        public static T Open<T>(ViewParameterBase param = null) where T : ViewLogic, new ()
        {
            return (T)Open(typeof(T).Name);
   
        }

        public static ViewLogic Open(string viewName, ViewParameterBase param = null)
        {
                     
            if (_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return logic;
            }
            
            _hiddenViewMap.Remove(viewName, out logic);     // 移除隐藏列表

            // 进入加载流程
            if (logic == null)
            {
                var viewObject = _loader.CreateView(viewName, Root.transform);

                logic = viewObject.GetComponent<ViewLogic>();

                if (logic == null)
                {
                    throw new Exception($"ViewLogic : {viewName} is not found!");
                }

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

                Vomit.Interface.SendEvent(new EView.Create
                {
                    ViewLogic = logic
                });
            }
            
            // 展示逻辑
            _visibleViewMap[logic.ID] = logic;
            
            if (logic.Config.EnableAutoMask)        // 遮罩
            {
                _masker?.Mask(logic);
            }
            
            logic.transform.SetParent(Root.transform, false);
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = Root.ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int) logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            logic.OnOpened(param);
            
            if (logic.Config.RecordOpen)
            {
                _recorder?.RecordOpen(logic.ID);
            }
            
            Vomit.Interface.SendEvent(new EView.Open
            {
                ViewLogic = logic,
            });

            UniTask.Create(async () =>
            {
                await logic.OpenEffect();
                logic.OnOpenEffectDone();
            });
            
            return logic;
        }
        
        public static void Close(string viewName, ViewParameterBase param = null)
        {
            if(!_visibleViewMap.Remove(viewName, out var logic))
            {
                LogKit.I($"Try closing a non-existent View : {viewName}");
                return;
            }
            
            // 取消监听器
            logic.Cancel();
            
            var isCache = logic.Config.IsCache;
            
            logic.OnClose(param);
            
            if (isCache)
            {
                logic.Parent(Root.HiddenCanvas);
                _hiddenViewMap.Add(viewName, logic);
                if (logic.Config.EnableAutoMask)
                {
                    _masker?.Unmask(logic);
                }
                _locker?.UnLock(logic);
            }
            else
            {
                Object.Destroy(logic.gameObject);
                _loader?.ReleaseView(logic.gameObject);
            }
            
            Vomit.Interface.SendEvent(new EView.Close()
            {
                ViewName = viewName,
                IsCache = logic.Config.IsCache,
            });
        }

        public static void Close<T>(ViewParameterBase param = null)
        {
            Close(typeof(T).Name);
        }
        
        /// <summary>
        /// 打开一个View并等待它关闭
        /// </summary>
        public static async UniTask OpenAndWaitClose<T>(ViewParameterBase param = null)  where T : ViewLogic, new ()
        {
            await Open<T>(param).WaitClose();
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
                throw  new Exception("请先初始化 ViewRecorder");
            }
            
            return _recorder.IsFirstOpen(typeof(T).Name);
        }

        public static void Freeze(string viewName)
        {
            var view = GetView(viewName);
            
            if (view != null)
            {
                Debug.Log($"Freeze { viewName}");
                _locker?.Lock(view); 
            }
        }

        public static void UnFreeze(string viewName)
        {
            var view = GetView(viewName);

            if (view != null)
            {
                Debug.Log($"UnFreeze { viewName}");
                _locker?.UnLock(view); 
            }
        }
    }
}