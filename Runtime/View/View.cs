using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
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
    public static class View
    {
        /// <summary>
        /// 字体缓存
        /// </summary>
        private static Dictionary<string, Font> _fontCache;
        
        /// <summary>
        /// ViewRoot下的Camera.
        /// </summary>
        public static Camera ViewCamera { get; private set; }

        /// <summary>
        /// 隐藏节点
        /// </summary>
        public static Transform HiddenCanvas { get; private set; }

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
        /// 所有面板缓存
        /// </summary>
        private static Dictionary<string, ViewLogic> _viewMap = new Dictionary<string, ViewLogic>();

        /// <summary>
        /// 所有被激活的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _visibleViewMap = new Dictionary<string, ViewLogic>();

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

        /// <summary>
        /// 用于测试绑定按钮的方法.
        /// </summary>
        private static Action _beforeClickButton;

        public static void Init()
        {
            ViewCamera = Root.ViewCamera;
            HiddenCanvas = Root.HiddenCanvas;
            
            List<Type> _preloads = new();
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.HasAttribute<PreloadAttribute>())
                    {
                        _preloads.Add(type);
                    }

                    if (type.IsSubclassOf(typeof(ViewComponent)))
                    {
                        if (type.HasAttribute<ViewComponentAttribute>())
                        {
                            ProcessViewComponent(type);
                        }
                        else
                        {
                            LogKit.W($"{type.Name} 没有 ViewComponentAttribute, 运行时将永远不会被自动释放.");
                        }
                    }
                }
            }
            
            foreach (var preload in _preloads)
            {
                ProcessPreload(preload);
            }

            
            static void ProcessPreload(Type logic)
            {
                var attr = logic.GetAttribute<PreloadAttribute>();
                var viewLogic = OpenView(logic);    // TODO 写一个加载方法而不是调用开启面板
                if (attr.IsHide)
                {
                    if(!viewLogic.Config.IsCache) LogKit.E($"预加载了ui : {logic.Name} 但是不是缓存的ui,请检查!");
                    CloseView(viewLogic);
                }
            }

            static void ProcessViewComponent(Type vc)
            {
                var att = vc.GetAttribute<ViewComponentAttribute>();
                foreach (var parentType in att.ParentTypes)
                {
                    if (_viewComponents.ContainsKey(parentType.Name))
                    {
                        _viewComponents[parentType.Name].Add(vc);
                    }
                    else
                    {
                        _viewComponents.Add(parentType.Name, new List<Type> { vc });
                    }

                    LogKit.I($"Record ViewComponents {parentType.Name} - {vc.Name}");
                }
            }
        }

        /// <summary>
        /// 本地化回调.
        /// </summary>
        /// <param name="localization"></param>
        public static void SetLocalization(Func<string, string> localization)
        {
            _localization = localization;
        }
        
        /// <summary>
        /// 设置在点击通过框架自动绑定的按钮之前调用的方法(方法拦截)
        /// </summary>
        /// <param name="action"></param>
        public static void SetBeforeClickBtnAction(Action action) => _beforeClickButton = action;
        
        #region OpenView

        [Obsolete("考虑删除中, 请使用 OpenViewAsync()")]
        private static ViewLogic OpenView(Type type, ViewParameterBase param = null)
        {
            var viewName = type.Name;

            if (_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return logic;
            }

            logic = LoadOrGenerateViewLogic(type);
            
            if (logic.isAsyncActioning)
            {
                LogKit.I($"Try to open an actioning view : {viewName}");
                return logic;
            }

            if (_viewComponents.TryGetValue(viewName, out var components))
            {
                foreach (var component in components)
                {
                    LoadViewComponent(component.Name);
                }
            }

            if (logic.Config.EnableAutoMask)
            {
                AutoGenerateViewMask(logic);
            }
            
            if (!logic.gameObject.activeSelf)
            {
                logic.gameObject.SetActive(true);
            }

            logic.transform.parent = Root.transform;
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int) logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            _visibleViewMap.Add(viewName, logic);
            
            logic.OnOpened(param).Forget();
            
            Vomit.Interface.SendEvent(new EView.Open()
            {
                LogicType = type,
                ViewLogic = logic,
            });
            
            return logic;
        }
        
        /// <summary>
        /// 打开一个和 T 同名的 View.
        /// </summary>
        public static T OpenView<T>(ViewParameterBase param = null) where T : ViewLogic, new()
        {
            return (T)OpenView(typeof(T), param);
        }

        #endregion
        
        #region OpenViewAsync

        /// <summary>
        /// 异步打开一个View.
        /// 直到加载完成,这个方法是同步的.
        /// 直到表现完成, 无法对UI进行交互(关闭射线检测)
        /// </summary>
        public static async UniTask<T> OpenViewAsync<T>(ViewParameterBase param = null) where T : ViewLogic, new ()
        {
            return (T) await OpenViewAsync(typeof(T), param);
        }
        
        private static async UniTask<ViewLogic> OpenViewAsync(Type logicType, ViewParameterBase param = null)
        {
            var viewName = logicType.Name;

            if (_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return logic;
            }

            logic = LoadOrGenerateViewLogic(logicType);
            
            if (logic.isAsyncActioning)
            {
                LogKit.I($"Try to open an actioning view : {viewName}");
                return logic;
            }
            
            logic.Freeze();
            logic.isAsyncActioning = true;
            
            if (_viewComponents.TryGetValue(viewName, out var components))
            {
                foreach (var component in components)
                {
                    LoadViewComponent(component.Name);
                }
            }

            if (logic.Config.EnableAutoMask)
            {
                AutoGenerateViewMask(logic);
            }
            
            if (!logic.gameObject.activeSelf)
            {
                logic.gameObject.SetActive(true);
            }

            logic.transform.parent = Root.transform;
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int) logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            _visibleViewMap.Add(viewName, logic);

            await logic.OnOpened(param);
            logic.isAsyncActioning = false;
            logic.UnFreeze();
            return logic;
        }

        #endregion

        #region CloseView
        
        public static UniTask CloseView(ViewLogic logic)
        {
            return CloseViewAsync(logic, false);
        }
        
        public static async UniTask CloseView<T>() where T : ViewLogic
        {
            _visibleViewMap.TryGetValue(typeof(T).Name, out var logic);
            await CloseViewAsync(logic, false);
        }

        public static void CloseViewImmediately(ViewLogic logic)
        {
            CloseViewAsync(logic, true).Forget();
        }
        
        /// <summary>
        /// 关闭一个UI,会立刻关闭这个UI的射线检测
        /// </summary>
        /// <param name="logic"></param>
        private static async UniTask CloseViewAsync(ViewLogic logic, bool immediately)
        {
            if (logic.isAsyncActioning)
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

            logic.isAsyncActioning = true;
            // 关闭射线检测   
            logic.Freeze();
            // 取消监听器
            logic.Cancel();
            // 移除所有的 View 事件
            logic.UnRegisterAllViewEvents();

            // 回调生命周期事件
            if (immediately)
            {
                logic.OnClose().Forget();
            }
            else
            {
                await logic.OnClose();    
            }
            logic.isAsyncActioning = false;
            // 不可见
            if (logic.Config.IsCache)
            {
                logic.OnHidden();
                logic.transform.parent = HiddenCanvas;
                
                if (logic.Config.EnableAutoMask && logic.transform.GetChild(0).name == "__AutoMask")
                {
                    Object.Destroy(logic.transform.GetChild(0).gameObject);
                }
                logic.UnFreeze();
            }
            else
            {
                Addressables.ReleaseInstance(logic.gameObject);
            }
        }
        
        #endregion

        public static void CloseAll()
        {
            foreach (var l in _visibleViewMap.Values.ToList())
            {
                CloseViewImmediately(l);
            }
        }
        
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
        
        public static T InstantiateVC<T>(Transform parent) where T : Component
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
        
        private static GameObject LoadViewComponent<T>() where T : Component
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

            // 如果在缓存中则直接返回
            if (_viewMap.TryGetValue(viewName, out var viewLogic))
            {
                return viewLogic;
            }

            var address = $"{Vomit.RuntimeConfig.ViewFrameworkConfig.ViewAddressablePrefix}/{viewName}.prefab";
            var viewObject = Addressables.InstantiateAsync(address, HiddenCanvas).WaitForCompletion();
            
            viewLogic = viewObject.GetComponent<ViewLogic>();
            viewLogic.Name = viewName;

            if (viewLogic.Config.AutoBindButtons)
            {
                AutoBindViewLogicButtons(viewLogic);
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
        /// 遍历 logic 所有的 Button 组件.
        /// 并反射查找名为 "__OnClick_[button.name]" 的方法.
        /// </summary>
        private static void AutoBindViewLogicButtons(ViewLogic logic)
        {
            var buttons = logic.transform.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var methodName = $"__OnClick_{btn.name}";
                const BindingFlags bindFlag = BindingFlags.NonPublic | BindingFlags.Instance;
                var methodInfo = logic.GetType().GetMethod(methodName, bindFlag);
                
                if (methodInfo == null)
                {
                    LogKit.W($"Try to bind {logic.Name} button event but method not found! Please check method named [{methodName}]");
                    continue;
                }
                
                btn.onClick.AddListener(() =>
                {
                    _beforeClickButton?.Invoke();
                    methodInfo.Invoke(logic, null);
                });
                
                LogKit.I($"[{logic.Name}] Binding Button Event to [{methodName}] successful!");
            }
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

        private static void AutoGenerateViewMask(ViewLogic logic)
        {
            var mask = new GameObject("__AutoMask", typeof(Image));
            mask.GetComponent<Image>().color = Vomit.RuntimeConfig.ViewFrameworkConfig.AutoMaskColor;

            var rectTrans = mask.GetComponent<RectTransform>();
            rectTrans.sizeDelta = new Vector2(50000, 50000);
            rectTrans.SetParent(logic.transform);
            rectTrans.localPosition = Vector3.zero;
            rectTrans.localScale = Vector3.one;
            rectTrans.SetAsFirstSibling();
            

            if (logic.Config.ClickMaskTriggerClose)
            {
                mask.GetAsyncPointerClickTrigger().ForEachAwaitAsync(async _ =>
                {
                    await UniTask.NextFrame();
                    CloseView(logic);
                }, mask.gameObject.GetCancellationTokenOnDestroy());
            }
        }

    }
}