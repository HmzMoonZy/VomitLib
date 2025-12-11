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

        [Tooltip("本地化")]
        [SerializeField] private bool _enableLocalization = true;
        
        [Tooltip("是否缓存")] 
        [SerializeField] private bool  _isCache = true;
        
        [Tooltip("是否存在多个")] // TODO
        [SerializeField] private bool _allowMultiple = false;
        
        [Tooltip("自动绑定按钮事件")] 
        [SerializeField] private bool  _autoBindButtons = true;

        /// <summary>
        /// 层级
        /// </summary>
        public ViewSortLayer Layer => _layer;

        /// <summary>
        /// 自动产生一个遮罩
        /// </summary>
        public bool EnableAutoMask => _enableAutoMask;

        /// <summary>
        /// 点击遮罩关闭这个面板
        /// </summary>
        public bool ClickMaskTriggerClose => _clickMaskTriggerClose;

        /// <summary>
        /// 本地化
        /// </summary>
        public bool EnableLocalization => _enableLocalization;

        /// <summary>
        /// 是否缓存
        /// </summary>
        public bool IsCache => _isCache;
        
        /// <summary>
        /// 自动绑定按钮事件
        /// </summary>
        public bool AutoBindButtons => _autoBindButtons;
    }
}