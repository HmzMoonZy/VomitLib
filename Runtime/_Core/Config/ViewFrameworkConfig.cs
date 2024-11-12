using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class ViewFrameworkConfig
    {
        [SerializeField, Tooltip("UI代码生成目录")]
        private string _scriptGeneratePath;

        [SerializeField, Tooltip("Canvas 开发分辨率")]
        private Vector2 _viewResolution = new Vector2(1440, 2560);
        
        public string ScriptGeneratePath
        {
            get => _scriptGeneratePath;
        }

        public Vector2 ViewResolution => _viewResolution;
    }
}