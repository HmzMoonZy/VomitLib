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


        private static Dictionary<string, Action> _registeredViewActions = new Dictionary<string, Action>();
        
        private static Dictionary<string, Action> _registeredViewCloseOnceActions = new Dictionary<string, Action>();


        static View()
        {
            Root = Object.FindObjectOfType<ViewRoot>();
            
            if (Root == null) throw new NotImplementedException("No \"ViewRoot\" component was added in the scene!");

            ViewCamera = Root.ViewCamera;
            HiddenCanvas = Root.HiddenCanvas;
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

        // public static void SetButtonClickSEAction(Action onClickSEAction)
        // {
        //     _helper.PlayButtonClickSe = onClickSEAction;
        // }



        /// <summary>
        /// 打开一个和 T 同名的 View.
        /// </summary>
        public static T OpenView<T>() where T : ViewLogic, new()
        {
            var viewName = typeof(T).Name;

            if (_visibleViewMap.TryGetValue(viewName, out var logic))
            {
                LogKit.I($"Try to open an already showed the View : {viewName}");
                return (T)logic;
            }

            logic = LoadOrGenerateViewLogic<T>();

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
            
            return (T) logic;
        }
        
        public static T OpenViewAsync<T>() where T : ViewLogic, new ()
        {
            throw new NotImplementedException();
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
            _visibleViewMap.Remove(logic.Name);
            // 回调生命周期事件
            logic.OnClose().Forget();

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

            if (_registeredViewCloseOnceActions.TryGetValue(logic.Name, out var onceAction))
            {
                onceAction?.Invoke();
                _registeredViewCloseOnceActions[logic.Name] = null;    
            }
            
        }

        public static void CloseAll()
        {
            foreach (var l in _visibleViewMap.Values.ToList())
            {
                CloseView(l);
            }
        }
        
        public static void CloseAll(string[] ignores)
        {
            foreach (var l in _visibleViewMap.Values.ToList())
            {
                if (ignores.Contains(l.Name)) continue;
                CloseView(l);
            }
        }

        public static void RegisterViewClosedOnce<T>(Action callback) where T : ViewLogic
        {
            _registeredViewCloseOnceActions.TryAdd(typeof(T).Name, null);
            _registeredViewCloseOnceActions[typeof(T).Name] += callback;
        }

        /// <summary>
        /// 获取 T 类型的 ViewLogic.
        /// 只要它在缓存中,就能被获取到.
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
            var view = GetView<T>();
            return view != null && view.IsVisible;
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
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T LoadOrGenerateViewLogic<T>() where T : ViewLogic
        {
            var viewName = typeof(T).Name;

            // 如果在缓存中则直接返回
            if (_viewMap.TryGetValue(viewName, out var viewLogic))
            {
                return (T) viewLogic;
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
            
            return (T) viewLogic;
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