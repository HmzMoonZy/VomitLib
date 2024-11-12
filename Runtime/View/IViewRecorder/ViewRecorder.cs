using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public class ViewRecorder : IViewRecorder
    {
        public bool IsFirstOpen(string viewName)
        {
            return !PlayerPrefs.HasKey($"__FIRST__{viewName}");
        }

        public void RecordOpen(string viewName)
        {
            PlayerPrefs.SetInt($"__FIRST__{viewName}", 1);
        }
    }
}