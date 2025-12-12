using QFramework;
using Twenty2.VomitLib;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public static class UguiExtension
    {
        #region Button - Command 

        public static void BindCommand<T>(this Button btn) where T : ICommand, new()
        {
            btn.onClick.AddListener(() =>
            {
                Vomit.Interface.SendCommand(new T());
            });
        }
        
        public static void BindCommand<T>(this Button btn, T command) where T : ICommand, new()
        {
            btn.onClick.AddListener(() =>
            {
                Vomit.Interface.SendCommand(command);
            });
        }

        #endregion
    }
}

