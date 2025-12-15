using System;
using UnityEngine;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class CodeGenConfig
    {
        // 代码路径
        [SerializeField] private string _genCodePath;
        public string GenCodePath
        {
            get
            {
                const string defaultGenCodePath = "Assets/UnityCodeGen.Generated";
                
                if (string.IsNullOrEmpty(_genCodePath))
                {
                    return defaultGenCodePath;
                }
                return _genCodePath;
            }
        }
        
        // 代码命名空间
        [SerializeField] private string _genCodeNamespace;
        public string GenCodeNamespace => _genCodeNamespace;

        // 代码类名
        [SerializeField] private string _genCodeClassName;
        public string GenCodeClassName
        {
            get
            {
                const string defaultGenCodeClassName = "VomitBoot";
                
                if (string.IsNullOrEmpty(_genCodeClassName))
                {
                    return defaultGenCodeClassName;
                }
                return _genCodeClassName;
            }
        }
    }
}