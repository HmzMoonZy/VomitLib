using UnityEditor;
using Twenty2.VomitLib.View;

namespace Twenty2.VomitLib.Editor
{
    [CustomEditor(typeof(ViewConfig))]
    public class ViewConfigInspector : UnityEditor.Editor
    {
        private SerializedProperty _layer;
        private SerializedProperty _enableAutoMask;
        private SerializedProperty _clickMaskTriggerClose;
        private SerializedProperty _enableLocalization;
        private SerializedProperty _isCache;
        private SerializedProperty _autoBindButtons;

        private void OnEnable()
        {
            _layer = serializedObject.FindProperty("_layer");
            _enableAutoMask = serializedObject.FindProperty("_enableAutoMask");
            _clickMaskTriggerClose = serializedObject.FindProperty("_clickMaskTriggerClose");
            _enableLocalization = serializedObject.FindProperty("_enableLocalization");
            _isCache = serializedObject.FindProperty("_isCache");
            _autoBindButtons = serializedObject.FindProperty("_autoBindButtons");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_layer);
            EditorGUILayout.PropertyField(_enableLocalization);
            EditorGUILayout.PropertyField(_isCache);
            EditorGUILayout.PropertyField(_autoBindButtons);
            
            EditorGUILayout.PropertyField(_enableAutoMask);

            if (_enableAutoMask.boolValue)
            {
                EditorGUILayout.PropertyField(_clickMaskTriggerClose);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}