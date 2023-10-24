using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QFramework;
using Twenty2.VomitLib;
using UnityEngine;
using UnityEngine.Serialization;


namespace Twenty2.VomitLib.View
{
    public class ViewConfig : MonoBehaviour
    {
        [Header("View视图面板的配置信息")]
        
        [Tooltip("层级")]
        [SerializeField] private ViewSortLayer _layer;

        [Tooltip("自动产生一个遮罩")]
        [SerializeField] private bool _enableAutoMask = true;
        
        [Tooltip("点击遮罩关闭这个面板")]
        [SerializeField] private bool _clickMaskTriggerClose = false;

        [Tooltip("替换默认字体")]
        [SerializeField] private bool _autoDefaultFont = true;

        [Tooltip("本地化")]
        [SerializeField] private bool _enableLocalization = true;
        
        [Tooltip("是否缓存")] 
        [SerializeField] private bool  _isCache = true;
        
        [Tooltip("自动绑定按钮事件")] 
        [SerializeField] private bool  _autoBindButtons = true;


        public ViewSortLayer Layer => _layer;

        public bool EnableAutoMask => _enableAutoMask;

        public bool ClickMaskTriggerClose => _clickMaskTriggerClose;

        public bool AutoDefaultFont => _autoDefaultFont;

        public bool EnableLocalization => _enableLocalization;

        public bool IsCache => _isCache;

        public bool AutoBindButtons => _autoBindButtons;
    }
}