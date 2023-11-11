using System;
using UnityEngine;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class ViewFrameworkConfig
    {
        [SerializeField, Tooltip("Addressable 寻址前缀\n通常确定一个View资源的方式为\n[addressPrefix][typeof(view).Name]")]
        private string _viewAddressablePrefix;

        [SerializeField, Tooltip("Addressable 寻址前缀\n通常确定一个ViewComponent的方式为\n[viewComponentAddressablePrefix][typeof(ViewComponent).Name")]
        private string _viewComponentAddressablePrefix;

        [SerializeField, Tooltip("自动遮罩面板的颜色值")]
        private Color _autoMaskColor = new Color(0, 0, 0, 0.5f);
        
        [SerializeField, Tooltip("自动替换的字体")]
        private Font _defaultFont;
        
        [SerializeField, Tooltip("UI代码生成目录")]
        private string _scriptGeneratePath;

        [SerializeField, Tooltip("Canvas 开发分辨率")]
        private Vector2 _viewResolution = new Vector2(1440, 2560);
        
        public Font DefaultFont
        {
            get => _defaultFont;
        }

        /// <summary>
        /// AA寻址前缀
        /// </summary>
        public string ViewAddressablePrefix
        {
            get => _viewAddressablePrefix;
        }

        /// <summary>
        /// AA组件寻址前缀
        /// </summary>
        public string ViewComponentAddressablePrefix
        {
            get => _viewComponentAddressablePrefix;
        }

        /// <summary>
        /// 自动遮罩颜色
        /// </summary>
        public Color AutoMaskColor
        {
            get => _autoMaskColor;
        }
        
        public string ScriptGeneratePath
        {
            get => _scriptGeneratePath;
        }

        public Vector2 ViewResolution => _viewResolution;

    }
}