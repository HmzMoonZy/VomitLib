using System;
using System.Collections.Generic;
using UnityEngine;

namespace Twenty2.VomitLib.Config
{
    /// <summary>
    /// SpriteAtlas配置
    /// </summary>
    [Serializable]
    public class SpriteAtlasInfo
    {
        [SerializeField] public string folderPath;
        [SerializeField] public string atlasPath;
        [SerializeField] public string atlasName;
        
        public SpriteAtlasInfo(string folder, string atlas, string name)
        {
            folderPath = folder;
            atlasPath = atlas;
            atlasName = name;
        }
    }

    [Serializable]
    public class SpriteAtlasConfig
    {
        [SerializeField, Tooltip("SpriteAtlas生成目录")]
        private string _atlasGeneratePath = "Assets/GameMain/Art/Atlas";
        
        [SerializeField, Tooltip("已收集的图集列表")]
        private List<SpriteAtlasInfo> _collectedAtlases = new List<SpriteAtlasInfo>();

        public string AtlasGeneratePath 
        { 
            get => _atlasGeneratePath; 
            set => _atlasGeneratePath = value; 
        }
        
        public List<SpriteAtlasInfo> CollectedAtlases 
        { 
            get => _collectedAtlases; 
        }
        
        public void AddAtlas(string folderPath, string atlasPath, string atlasName)
        {
            // 如果已存在则更新，否则添加
            var existing = _collectedAtlases.Find(x => x.folderPath == folderPath);
            if (existing != null)
            {
                existing.atlasPath = atlasPath;
                existing.atlasName = atlasName;
            }
            else
            {
                _collectedAtlases.Add(new SpriteAtlasInfo(folderPath, atlasPath, atlasName));
            }
        }
        
        public void RemoveAtlas(string folderPath)
        {
            _collectedAtlases.RemoveAll(x => x.folderPath == folderPath);
        }
        
        public bool HasAtlas(string folderPath)
        {
            return _collectedAtlases.Exists(x => x.folderPath == folderPath);
        }
        
        public SpriteAtlasInfo GetAtlasInfo(string folderPath)
        {
            return _collectedAtlases.Find(x => x.folderPath == folderPath);
        }
    }
}