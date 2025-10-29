using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.Config
{
    [Serializable]
    public class ViewConfig
    {
        [SerializeField, Tooltip("UI代码生成目录")]
        private string _scriptGeneratePath;
        
        [SerializeField, Tooltip("生成同名文件夹")]
        private bool _isGenerateFolder;

        [SerializeField, Tooltip("Canvas 开发分辨率")]
        private Vector2 _viewResolution = new Vector2(1440, 2560);
        
        public string ScriptGeneratePath => _scriptGeneratePath;

        public Vector2 ViewResolution => _viewResolution;
        
        public bool IsGenerateFolder => _isGenerateFolder;
    }
}