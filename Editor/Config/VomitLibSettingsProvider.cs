using UnityEditor;
using UnityEngine;
using Twenty2.VomitLib.Config;

namespace Twenty2.VomitLib.Editor
{
    public class VomitLibSettingsProvider : SettingsProvider
    {
        private SerializedObject _so;

        public VomitLibSettingsProvider()
            : base("Project/VomitLib", SettingsScope.Project) { }

        [SettingsProvider]
        public static SettingsProvider Create() => new VomitLibSettingsProvider
        {
            keywords = new[] { "vomit", "luban", "view", "net", "codegen", "toolbar" }
        };

        public override void OnActivate(string searchContext,
            UnityEngine.UIElements.VisualElement rootElement)
        {
            VomitConfig.instance.hideFlags &= ~HideFlags.NotEditable;
            _so = new SerializedObject(VomitConfig.instance);
        }

        public override void OnGUI(string searchContext)
        {
            _so.Update();

            var iter = _so.GetIterator();
            iter.NextVisible(true); // 跳过 m_Script
            while (iter.NextVisible(false))
            {
                EditorGUILayout.PropertyField(iter, true);
            }

            if (_so.ApplyModifiedProperties())
            {
                VomitConfig.instance.Save();
            }
        }
    }
}
