using QFramework;
using Twenty2.VomitLib.Tools;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public static class ViewExtension
    {
        #region Button - Command 

        public static void BindCommand<T>(this Button selfBtn) where T : ICommand, new()
        {
            selfBtn.onClick.AddListener(() =>
            {
                Vomit.Interface.SendCommand(new T());
            });
        }
        
        public static void BindCommand<T>(this Button selfBtn, T command) where T : ICommand, new()
        {
            selfBtn.onClick.AddListener(() =>
            {
                Vomit.Interface.SendCommand(command);
            });
        }

        #endregion
    }
}

