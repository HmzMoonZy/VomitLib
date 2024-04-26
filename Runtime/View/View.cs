using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
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

        /// <summary>
        /// View 根节点.
        /// </summary>
        public static ViewRoot Root { get; private set; }

        /// <summary>
        /// 所有面板缓存
        /// </summary>
        private static Dictionary<string, ViewLogic> _viewMap = new Dictionary<string, ViewLogic>();

        /// <summary>
        /// 所有被激活的面板
        /// </summary>
        private static Dictionary<string, ViewLogic> _visibleViewMap = new Dictionary<string, ViewLogic>();

        /// <summary>
        /// 所有View组件.
        /// </summary>
        private static Dictionary<string, GameObject> _viewComponents = new Dictionary<string, GameObject>();

        /// <summary>
        /// 本地化回调
        /// </summary>
        private static Func<string, string> _localization = null;

        /// <summary>
        /// 用于测试绑定按钮的方法.
        /// </summary>
        private static Action _beforeClickButton;
        
        static View()
        {
            Root = Object.FindObjectOfType<ViewRoot>();
            
            if (Root == null) throw new NotImplementedException("No \"ViewRoot\" component was added in the scene!");

            ViewCamera = Root.ViewCamera;
            HiddenCanvas = Root.HiddenCanvas;
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.HasAttribute<PreloadAttribute>()) continue;

                    var attr = type.GetAttribute<PreloadAttribute>();
                    var viewLogic = OpenView(type);
                    if (attr.IsHide)
                    {
                        if(!viewLogic.Config.IsCache) LogKit.E($"预加载了ui : {type.Name} 但是不是缓存的ui,请检查!");
                        CloseView(viewLogic);
                    }
                }
            }
        }

        /// <summary>
        /// 本地化会回调.
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

        private static ViewLogic OpenView(Type type)
        {
            var viewName = type.Name;

            if (_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return logic;
            }

            logic = LoadOrGenerateViewLogic(type);

            if (logic.Config.EnableAutoMask)
            {
                AutoGenerateViewMask(logic);
            }
            
            if (!logic.gameObject.activeSelf)
            {
                logic.gameObject.SetActive(true);
            }

            logic.Parent(Root);
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int) logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            _visibleViewMap.Add(viewName, logic);
            
            logic.OnOpened().Forget();
            
            return logic;
        }
        
        /// <summary>
        /// 打开一个和 T 同名的 View.
        /// </summary>
        public static T OpenView<T>() where T : ViewLogic, new()
        {
            return (T)OpenView(typeof(T));
        }

        private static async UniTask<ViewLogic> OpenViewAsync(Type logicType)
        {
            var viewName = logicType.Name;

            if (_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return logic;
            }

            logic = LoadOrGenerateViewLogic(logicType);

            if (logic.Config.EnableAutoMask)
            {
                AutoGenerateViewMask(logic);
            }
            
            if (!logic.gameObject.activeSelf)
            {
                logic.gameObject.SetActive(true);
            }

            logic.Parent(Root);
            logic.ViewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            logic.ViewCanvas.worldCamera = ViewCamera;
            logic.ViewCanvas.sortingLayerID = (int) logic.Config.Layer;
            logic.SortOrder = _visibleViewMap.Count <= 0 ? 0 : _visibleViewMap.Values.Max(i => i.SortOrder) + 1;
            
            _visibleViewMap.Add(viewName, logic);

            await logic.OnOpened();
            
            return logic;
        }
        
        /// <summary>
        /// 异步打开一个View.
        /// </summary>
        /// <code>
        /// 直到加载完成,这个方法是同步的.
        /// 但是自己实现的OnOpen,如一些动画效果是异步的.
        /// 可以等待.
        /// </code>
        public static async UniTask<T> OpenViewAsync<T>() where T : ViewLogic, new ()
        {
            return (T) await OpenViewAsync(typeof(T));
        }
        
        public static void CloseView<T>() where T : ViewLogic
        {
            var viewName = typeof(T).Name;
            if (!_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.W($"Try to close a view with name {viewName} that does not exist!");
                return;
            }

            CloseView(logic);
        }

        public static void CloseView(ViewLogic logic)
        {
            // 清除可见字典
            if (!_visibleViewMap.Remove(logic.Name))
            {
                LogKit.W($"Try to close a view with name {logic.Name} that does not exist!");
                return;
            }
            
            // 回调生命周期事件
            logic.UnRegisterAllViewEvents();
            logic.OnClose().Forget();
            logic.Dispose();

            // 不可见
            if (logic.Config.IsCache)
            {
                logic.OnHidden();
                logic.Parent(HiddenCanvas);
                
                if (logic.Config.EnableAutoMask && logic.transform.GetChild(0).name == "__AutoMask")
                {
                    Object.Destroy(logic.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                _viewMap.Remove(logic.Name);
                Addressables.ReleaseInstance(logic.gameObject);
            }
            
        }

        public static async void CloseViewAsync<T>() where T : ViewLogic
        {
            var viewName = typeof(T).Name;
            if (!_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.W($"Try to close a view with name {viewName} that does not exist!");
                return;
            }

            await CloseViewAsync(logic);
        }
        
        
        private static async UniTask CloseViewAsync(ViewLogic logic)
        {
            // 清除可见字典
            if (!_visibleViewMap.Remove(logic.Name))
            {
                LogKit.W($"Try to close a view with name {logic.Name} that does not exist!");
                return;
            }
            
            // 回调生命周期事件
            logic.UnRegisterAllViewEvents();
            await logic.OnClose();
            logic.Dispose();

            // 不可见
            if (logic.Config.IsCache)
            {
                logic.OnHidden();
                logic.Parent(HiddenCanvas);
                
                if (logic.Config.EnableAutoMask && logic.transform.GetChild(0).name == "__AutoMask")
                {
                    Object.Destroy(logic.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                _viewMap.Remove(logic.Name);
                Addressables.ReleaseInstance(logic.gameObject);
            }
        }

        public static void CloseAll()
        {
            foreach (var l in _visibleViewMap.Values.ToList())
            {
                CloseView(l);
            }
        }
        
        public static string[] CloseAll(string[] ignores)
        {
            string[] result = new string[_visibleViewMap.Count - ignores.Length];
            int index = 0;
            foreach (var l in _visibleViewMap.Values.ToList())
            {
                string key = l.Name;
                if (ignores.Contains(key)) continue;
                result[index] = key;
                index++;
                CloseView(l);
            }

            return result;
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
            if (!_viewComponents.TryGetValue(vcName, out GameObject prefab))
            {
                prefab = Addressables.LoadAssetAsync<GameObject>($"{Vomit.RuntimeConfig.ViewFrameworkConfig.ViewComponentAddressablePrefix}/{vcName}.prefab").WaitForCompletion();
                _viewComponents.TryAdd(vcName, prefab);
            }

            if (prefab.GetComponent<T>() == null)
            {
                LogKit.I($"加载的 ViewComponent 不包含预期的组件.{vcName}");
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
                    // PlayButtonClickSe?.Invoke();
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
            foreach (var cText in logic.transform.GetComponentsInChildren<Text>())
            {
                string ret = _localization(cText.text);
                cText.text = ret ?? cText.text;
            }
        }

        private static void AutoGenerateViewMask(ViewLogic logic)
        {
            var mask = new GameObject("__AutoMask", typeof(Image));
            mask.GetComponent<Image>().color = Vomit.RuntimeConfig.ViewFrameworkConfig.AutoMaskColor;

            var rectTrans = mask.GetComponent<RectTransform>();
            rectTrans.sizeDelta = new Vector2(50000, 50000);
            rectTrans
                .Parent(logic.transform)
                .LocalPosition(Vector3.zero)
                .LocalScale(Vector3.one)
                .SetAsFirstSibling();
            

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