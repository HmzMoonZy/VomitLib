using Twenty2.VomitLib.Config;

namespace Twenty2.VomitLib.Editor
{
    public class VomitEditor
    {
        private static VomitConfig _config;
        public static VomitConfig Config
        {
            get
            {
                string guid = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(VomitConfig)}")[0]; 
                _config = UnityEditor.AssetDatabase.LoadAssetAtPath<VomitConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                return _config;
            }
        }
    }
}