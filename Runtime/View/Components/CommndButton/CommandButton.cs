using QFramework;
using Twenty2.VomitLib;
using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public class CommandButton : Button, IAbstractController
    {
        private ICommand _command;

        protected override void Awake()
        {
            base.Awake();
            onClick.AddListener(OnClick);
        }

        protected override void OnDestroy()
        {
            onClick.RemoveListener(OnClick);
            base.OnDestroy();
        }

        public void SetCommand(ICommand command)
        {
            _command = command;
        }
    
        public void SetCommand<T>() where T : ICommand, new()
        {
            _command = new T();
        }
    

        private void OnClick()
        {
            if (_command != null)
            {
                ((IAbstractController) this).GetArchitecture().SendCommand(_command);    
            }
        }
    
    }
}

