using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FluentAPI;
using QFramework;
using TMPro;
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

        /// <summary>
        /// ViewLogic - ViewComponent
        /// key : ViewLogic.Name
        /// value : List,ViewComponent
        /// </summary>
        private static Dictionary<string, List<Type>> _compMap = new Dictionary<string,  List<Type>>();
        
        private static IViewLoader _loader;
        private static IViewBinder _binder;
        private static IViewMasker _masker;
        private static IViewLocalizer _localizer;
        private static IViewRecorder _recorder;
        
        public static void Init(IViewLoader loader, IViewBinder binder = null, IViewMasker masker = null, IViewLocalizer localizer = null, IViewRecorder recorder = null)
        {
            _loader = loader;
            _binder = binder;
            _masker = masker;
            _localizer = localizer;
            _recorder = recorder;
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    // 记录 ViewComponent 和 View 的引用关系 TODO 还没有实现释放
                    if (type.HasAttribute<ViewCompAttribute>())
                    {   
                        var att = type.GetAttribute<ViewCompAttribute>();
                        foreach (var parentType in att.ParentTypes)
                        {
                            _compMap.TryAdd(parentType.Name, new List<Type>());
                            _compMap[parentType.Name].Add(type);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 异步打开一个View
        /// </summary>
        public static async UniTask<T> OpenAsync<T>(ViewParameterBase param = null) where T : ViewLogic, new ()
        {
            return (T) await OpenAsync(typeof(T).Name, param);
        }

        public static async UniTask<ViewLogic> OpenAsync(string viewName, ViewParameterBase param = null)
        {
            ViewLogic logic = null;

            if (_visibleViewMap.TryGetValue(viewName, out logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return logic;
            }
            
            _hiddenViewMap.Remove(viewName, out logic);
            
            _visibleViewMap.Add(viewName, null);          // 添加标记
            
            if(logic == null)
            {
                var viewObject = Object.Instantiate(await _loader.LoadView(viewName), Root.transform);

                logic = viewObject.GetComponent<ViewLogic>();

                if (logic == null)
                {
                    throw new Exception($"ViewLogic : {viewName} is not found!");
                }

                logic.Name = viewName;

                if (logic.Config.AutoBindButtons)
                {
                    _binder?.Bind(logic);
                }
                
                if (_compMap.TryGetValue(logic.Name, out var components))
                {
                    foreach (var component in components)
                    {
                        _loader.LoadComp(component.Name);   // 预加载
                    }
                }
                
                logic.OnCreated();

                Vomit.Interface.SendEvent(new EView.Create
                {
                    ViewLogic = logic
                });
            }
            
            _visibleViewMap[logic.Name] = logic;
            
            if (logic.Config.EnableAutoMask)
            {
                _masker?.Mask(logic);
            }

            if (logic.Config.EnableLocalization)
            {
                _localizer?.Localize(logic);
            }
            
            logic.transform.parent = Root.transform;
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = Root.ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int) logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            Freeze();
            
            Vomit.Interface.SendEvent(new EView.Open
            {
                ViewLogic = logic,
            });
            
            await logic.OnOpened(param);
            await logic.PlayOpenAnimation();
            
            if (logic.Config.RecordOpen)
            {
                _recorder?.RecordOpen(logic.Name);
            }
            
            Vomit.Interface.SendEvent(new EView.OpenDone()
            {
                ViewLogic = logic,
            });

            UnFreeze();
            
            return logic;
        }
        
        /// <summary>
        /// 打开一个View并等待它关闭
        /// </summary>
        public static async UniTask OpenAndWaitClose<T>(ViewParameterBase param = null)  where T : ViewLogic, new ()
        {
            await (await OpenAsync<T>(param)).WaitClose();
        }
        
        public static UniTask CloseAsync<T>()
        {
            return CloseAsync(typeof(T).Name);
        }
        
        public static async UniTask CloseAsync(string viewName)
        {
            if(!_visibleViewMap.Remove(viewName, out var logic))
            {
                LogKit.I($"Try closing a non-existent View : {viewName}");
                return;
            }

            Freeze();
            
            // 取消监听器
            logic.Cancel();
            
            await logic.PlayCloseAnimation();
            await logic.OnClose();
            
            if (logic.Config.IsCache)
            {
                logic.OnHidden();
                logic.transform.parent = Root.HiddenCanvas;
                _hiddenViewMap.Add(viewName, logic);
                
                if (logic.Config.EnableAutoMask)
                {
                    _masker?.Unmask(logic);
                }
            }
            else
            {
                Object.Destroy(logic.gameObject);
                _loader.ReleaseView(logic.gameObject);
            }
            
            Vomit.Interface.SendEvent(new EView.Close()
            {
                LogicType = logic.GetType(),
            });
            
            UnFreeze();
        }
        
        /// <summary>
        /// 获取 T 类型的 ViewLogic.
        /// 只要它在缓存中,就能被获取到.
        /// </summary>
        public static T GetView<T>() where T : ViewLogic
        {
            if (!_visibleViewMap.TryGetValue(typeof(T).Name, out var info))
            {
                return null;
            }

            return (T) info;
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
        
        /// <summary>
        /// 实例化一个在资源面板中标注为 VC 的对象.
        /// 并返回同名的类型.
        /// </summary>
        public static async UniTask<T> CreateComp<T>(Transform parent, Vector3 position) where T : ViewComponent
        {
            var go = Object.Instantiate(await _loader.LoadComp(typeof(T).Name), parent);
            go.transform.position = position;
            
            return go.GetComponent<T>();
        }
        
        /// <summary>
        /// 实例化一个在资源面板中标注为 VC 的对象.
        /// 并返回同名的类型.
        /// </summary>
        public static async UniTask<T> CreateComp<T>(Transform parent) where T : UnityEngine.Component
        {
            var prefab = await _loader.LoadComp(typeof(T).Name);
            
            return Object.Instantiate(prefab, parent).GetComponent<T>();
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

        public static void Freeze()
        {
            _visibleViewMap.Values.ForEach(logic =>
            {
                logic.Freeze();
            });
        }

        public static void UnFreeze()
        {
            _visibleViewMap.Values.ForEach(logic =>
            {
                logic.UnFreeze();
            });
        }
    }
}