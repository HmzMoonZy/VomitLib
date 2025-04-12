using Twenty2.VomitLib.Config;

namespace Twenty2.VomitLib.Editor
{
    public class VomitEditor
    {
        public static VomitConfig Config
        {
            get
            {
                string guid = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(VomitConfig)}")[0]; 
                return UnityEditor.AssetDatabase.LoadAssetAtPath<VomitConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }
}