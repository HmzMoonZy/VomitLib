using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
                    _root = Object.Instantiate(prefab, Vomit.RuntimeConfig.ViewFrameworkConfig.ViewRootPosition, Quaternion.identity).GetComponent<ViewRoot>();
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
        /// 正在播放动画的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _animatingViewMap = new Dictionary<string, ViewLogic>();

        /// <summary>
        /// ViewLogic - ViewComponent
        /// key : ViewLogic.Name
        /// value : List,ViewComponent
        /// </summary>
        private static Dictionary<string, List<Type>> _viewComponents = new Dictionary<string,  List<Type>>();

        /// <summary>
        /// 加载再内存中的ViewComponent预制体
        /// </summary>
        private static Dictionary<string, GameObject> _viewComponentPrefabs = new Dictionary<string, GameObject>(); 

        /// <summary>
        /// 本地化回调
        /// </summary>
        private static Func<string, string> _localization = null;
        
        private static IViewLoader _loader;
        private static IViewBinder _binder;
        private static IViewMasker _masker;
        
        public static void Init(IViewLoader loader, IViewBinder binder = null, IViewMasker masker = null)
        {
            _loader = loader;
            _binder = binder;
            _masker = masker;
        }

        /// <summary>
        /// 本地化回调.
        /// </summary>
        /// <param name="localization"></param>
        public static void SetLocalization(Func<string, string> localization)
        {
            _localization = localization;
        }
        
        #region OpenView
        
        /// <summary>
        /// 异步打开一个View, 直到预期的动画结束.
        /// 直到加载完成,这个方法是同步的.
        /// 直到表现完成, 无法对UI进行交互(关闭射线检测)
        /// TODO 独立的动画逻辑, 考虑用Animation实现
        /// </summary>
        public static async UniTask<T> OpenAsync<T>(ViewParameterBase param = null) where T : ViewLogic, new ()
        {
            return (T) await OpenAsync(typeof(T), param);
        }
        
        /// <summary>
        /// 打开一个View并等待它关闭
        /// </summary>
        public static async UniTask OpenAndWaitClose<T>(ViewParameterBase param = null)  where T : ViewLogic, new ()
        {
            await (await OpenAsync<T>(param)).WaitClose();
        }
        
        private static async UniTask<ViewLogic> OpenAsync(Type logicType, ViewParameterBase param = null)
        {
            var viewName = logicType.Name;

            ViewLogic logic = null;

            if (_visibleViewMap.TryGetValue(viewName, out logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return logic;
            }

            if (_animatingViewMap.TryGetValue(viewName, out logic))
            {
                LogKit.I($"Try to open animation  View : {viewName}");
                return logic;
            }
            

            if(_hiddenViewMap.TryGetValue(viewName))
            

            logic = LoadOrGenerateViewLogic(logicType);
            
            OnLoadLogic(logic);

            Freeze();
            
            logic.IsAsyncActioning = true;

            var openEvent = new EView.Open
            {
                LogicType = logicType,
                ViewLogic = logic,
                OpenTask =  new UniTaskCompletionSource(),
            };
            
            Vomit.Interface.SendEvent(openEvent);

            await logic.OnOpened(param);

            openEvent.OpenTask.TrySetResult();
            
            logic.IsAsyncActioning = false;

            UnFreeze();

            if (logic.Config.RecordOpen)
            {
                logic.RecordFirstOpen();
            }
            
            return logic;
        }
        
        
        
        private static void OnLoadLogic(ViewLogic logic)
        {
            if (logic.IsAsyncActioning)
            {
                LogKit.I($"Try to open an actioning view : {logic.Name}");
                return;
            }
            
            if (_viewComponents.TryGetValue(logic.Name, out var components))
            {
                foreach (var component in components)
                {
                    LoadViewComponent(component.Name);
                }
            }

            if (logic.Config.EnableAutoMask)
            {
                _masker?.Mask(logic);
            }
            
            if (!logic.gameObject.activeSelf)
            {
                logic.gameObject.SetActive(true);
            }
            
            logic.transform.parent = Root.transform;
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = Root.ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int) logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            _visibleViewMap.Add(logic.Name, logic);
        }
        
        #endregion
        
        #region CloseView
        
        public static UniTask CloseAsync<T>()
        {
            _visibleViewMap.TryGetValue(typeof(T).Name, out var logic);
            return CloseAsync(logic, false);
        }
        
        public static async UniTask CloseAsync(ViewLogic logic, bool immediately)
        {
            if (logic is null) return;
            
            if (logic.IsAsyncActioning)
            {
                LogKit.W($"Try to close an actioning view with name {logic.Name} !");
                return;
            }
            
            // 清除可见字典
            if (!_visibleViewMap.Remove(logic.Name))
            {
                LogKit.W($"Try to close a view with name {logic.Name} that does not exist!");
                return;
            }
            // 清楚缓存字典
            if (!logic.Config.IsCache)
            {
                _viewMap.Remove(logic.Name);
            }

            logic.IsAsyncActioning = true;
            // 关闭射线检测   
            logic.Freeze();
            // 取消监听器
            logic.Cancel();

            // 回调生命周期事件
            if (immediately)
            {
                logic.OnClose().Forget();
            }
            else
            {
                await logic.OnClose();
            }
            
            Vomit.Interface.SendEvent(new EView.Close()
            {
                LogicType = logic.GetType(),
            });
            logic.IsAsyncActioning = false;
            // 不可见
            if (logic.Config.IsCache)
            {
                logic.OnHidden();
                logic.transform.parent = HiddenCanvas;
                
                if (logic.Config.EnableAutoMask)
                {
                    _masker?.Unmask(logic);
                }
                logic.UnFreeze();
            }
            else
            {
                Addressables.ReleaseInstance(logic.gameObject);
            }
        }
        
        #endregion

        
        /// <summary>
        /// 获取 T 类型的 ViewLogic.
        /// 只要它在缓存中,就能被获取到.
        /// </summary>
        public static T GetView<T>() where T : ViewLogic
        {
            if (!_viewMap.TryGetValue(typeof(T).Name, out var info))
            {
                return null;
            }

            return (T) info;
        }
        
        public static ViewLogic GetView(string name)
        {
            if (!_viewMap.TryGetValue(name, out var info))
            {
                return null;
            }

            return info;
        }

        public static ViewLogic GetTop()
        {
            var maxLayer = _visibleViewMap.Values.Max(info => info.Config.Layer);
            var maxSort = _visibleViewMap.Values.Where(info => info.Config.Layer == maxLayer).Max(info => info.SortOrder);
            return _visibleViewMap.Values.First(info => info.SortOrder == maxSort && info.Config.Layer == maxLayer);
        }

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

        public static bool IsViewVisible<T>() where T : ViewLogic
        {
            return _visibleViewMap.ContainsKey(typeof(T).Name);
        }
        
        /// <summary>
        /// 实例化一个在资源面板中标注为 VC 的对象.
        /// 并返回同名的类型.
        /// </summary>
        /// TODO 所有的 VC 继承同一个父类,并实现泛型的Init方法.
        /// <param name="parent"></param>
        /// <param name="position"></param>
        /// <param name="go"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T InstantiateVC<T>(Transform parent, Vector3 position, out GameObject go) where T : ViewComponent
        {
            var prefab = LoadViewComponent<T>();

            go = Object.Instantiate(prefab, parent);
            go.transform.position = position;
            
            return go.GetComponent<T>();
        }

        public static T InstantiateVC<T>(Transform parent, out GameObject go) where T : ViewComponent
        {
            var prefab = LoadViewComponent<T>();
            
            go = Object.Instantiate(prefab, parent);

            return go.GetComponent<T>();
        }
        
        public static T InstantiateVC<T>(Transform parent) where T : UnityEngine.Component
        {
            var prefab = LoadViewComponent<T>();
            
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

        private static GameObject LoadViewComponent<T>() where T : UnityEngine.Component
        {
            string vcName = typeof(T).Name;

            return LoadViewComponent(vcName);
            
            
        }

        private static GameObject LoadViewComponent(string component)
        {
            if (!_viewComponentPrefabs.TryGetValue(component, out GameObject prefab))
            {
                prefab = Addressables.LoadAssetAsync<GameObject>($"{Vomit.RuntimeConfig.ViewFrameworkConfig.ViewComponentAddressablePrefix}/{component}.prefab").WaitForCompletion();
                _viewComponentPrefabs.TryAdd(component, prefab);
            }

            if (prefab.GetComponent(component) == null)
            {
                LogKit.I($"加载的 ViewComponent 不包含预期的组件.{component}");
            }

            return prefab;
        }
        
        /// <summary>
        /// 从缓存或硬盘中加载 T 类型的 ViewLogic.
        /// </summary>
        /// <returns></returns>
        private static ViewLogic LoadOrGenerateViewLogic(Type logicType)
        {
            var viewName = logicType.Name;
            
            if

            var viewObject = Object.Instantiate(_loader.LoadView(viewName), HiddenCanvas); 
            
            var viewLogic = viewObject.GetComponent<ViewLogic>();
            
            viewLogic.Name = viewName;

            if (viewLogic.Config.AutoBindButtons)
            {
                _binder?.Bind(viewLogic);
            }

            if (viewLogic.Config.AutoDefaultFont)
            {
                AutoSetFont(viewLogic, Vomit.RuntimeConfig.ViewFrameworkConfig.DefaultFont);
            }

            if (viewLogic.Config.EnableLocalization && _localization != null)
            {
                Localization(viewLogic);
            }
            
            viewLogic.OnCreated();
            
            Vomit.Interface.SendEvent(new EView.Create
            {
                LogicType = logicType,
                ViewLogic = viewLogic
            });

            _viewMap.Add(viewName, viewLogic);
            
            return viewLogic;
        }

        /// <summary>
        /// 如果 logic 中的Text组件是默认字体,则替换.
        /// </summary>
        private static void AutoSetFont(ViewLogic logic, Font font)
        {
            foreach (var cText in logic.transform.GetComponentsInChildren<Text>())
            {
                if (cText.font.name == "Arial")
                {
                    cText.font = font;
                }
            }
        }

        /// <summary>
        /// 调用本地化方法翻译 logic 中所有的 Text 组件.
        /// </summary>
        private static void Localization(ViewLogic logic)
        {
            foreach (var cText in logic.transform.GetComponentsInChildren<MaskableGraphic>())
            {
                if (cText is Text text)
                {
                
                    string ret = _localization(text.text);
                    text.text = ret ?? text.text;    
                }
                
                if (cText is TextMeshProUGUI textmeshpro)
                {
                
                    string ret = _localization(textmeshpro.text);
                    textmeshpro.text = ret ?? textmeshpro.text;    
                }
            }
        }

    }
}