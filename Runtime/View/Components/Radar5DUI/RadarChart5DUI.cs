using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadarChart5DUI : MonoBehaviour
{
    private enum ValueDisplayType { Always, PointerEnter, Hide, }
    
    [SerializeField] private ValueDisplayType _displayType;
    [SerializeField] private RadarChart5DRenderer _renderer;
    [SerializeField] private Text[] _propertyLabels = new Text[5];      // 和Render对应
    
    private void Awake()
    {
        // 起始状态
        SetLabelsAlpha(_displayType == ValueDisplayType.Always ? 1 : 0);
        // 鼠标进入
        _renderer.GetAsyncPointerEnterTrigger().ForEachAsync(ed =>
        {
            if (_displayType == ValueDisplayType.PointerEnter) SetLabelsAlpha(1);
        }, cancellationToken:gameObject.GetCancellationTokenOnDestroy());
        // 鼠标离开
        _renderer.GetAsyncPointerExitTrigger().ForEachAsync(ed =>
        {
            if (_displayType == ValueDisplayType.PointerEnter) SetLabelsAlpha(0);
        }, cancellationToken:gameObject.GetCancellationTokenOnDestroy());
    }

    public void SetData(int index, float value)
    {
        if (_renderer) _renderer.SetValue(index, value);
    }

    public void SetAllData(params float[] values)
    {
        if (values.Length != 5) return;
        for (int i = 0; i < 5; i++) SetData(i, values[i]);
    }

    public void SetLabelText(int index, string text)
    {
        if (index >= 0 && index < _propertyLabels.Length && _propertyLabels[index])
        {
            _propertyLabels[index].text = text;
        }
    }
    
    // 供上层逻辑控制 Label 的显隐/颜色
    public void SetLabelsAlpha(float alpha)
    {
        foreach (var label in _propertyLabels)
        {
            if (label)
            {
                Color c = label.color;
                c.a = alpha;
                label.color = c;
            }
        }
    }

    public float GetValue(int index)
    {
        return _renderer.GetData()[index];
    }
}