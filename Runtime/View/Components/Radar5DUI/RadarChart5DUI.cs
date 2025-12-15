using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadarChart5DUI : MonoBehaviour
{
    private enum ValueDisplayType { Always, PointerEnter, Hide, }
    
    [SerializeField] private ValueDisplayType _displayType;
    [SerializeField] private RadarChart5DRenderer _renderer;
    [SerializeField] private Text[] _propertyLabels = new Text[5];      // 和Render对应
}