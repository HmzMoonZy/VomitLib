using System;
using System.Reflection;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public class ViewBinder : IViewBinder
    {
        private readonly Action _beforeClickButton;

        public ViewBinder(Action beforeClickButton)
        {
            _beforeClickButton = beforeClickButton;
        }

        public void Bind(ViewLogic view)
        {
            var buttons = view.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var methodName = $"__OnClick_{btn.name}";
                const BindingFlags bindFlag = BindingFlags.NonPublic | BindingFlags.Instance;
                var methodInfo = view.GetType().GetMethod(methodName, bindFlag);
                
                if (methodInfo == null)
                {
                    Log.Warning($"Try to bind {view.ID} button event but method not found! Please check method named [{methodName}]");
                    continue;
                }
                
                btn.onClick.AddListener(() =>
                {
                    _beforeClickButton?.Invoke();
                    methodInfo.Invoke(view, null);
                });
                
                Log.Debug($"[{view.ID}] Binding Button Event to [{methodName}] successful!");
            }
        }
    }
}