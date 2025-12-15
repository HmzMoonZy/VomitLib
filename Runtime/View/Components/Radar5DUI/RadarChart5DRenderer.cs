using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RadarChart5DRenderer : MaskableGraphic
{
    [Header("--- 数据配置 ---")]
    [Tooltip("最大值(100%)对应的数值")]
    [SerializeField] private float _maxValue = 100f;
    [SerializeField] private float[] _datas = new float[5]; // 固定5维

    [Header("--- 布局配置 ---")]
    [Range(0.1f, 1f)] 
    [SerializeField] private float _radiusScale = 0.9f; // 控制图形在Rect中的占比
    [Range(0f, 360f)]
    [SerializeField] private float _rotationOffset = 0f; // 整体旋转

    [Header("--- 样式: 填充 (Fill) ---")]
    [SerializeField] private Color _centerColor = new Color(0, 1, 1, 0.2f); // 中心颜色
    [SerializeField] private Color _tipColor = new Color(0, 1, 1, 0.8f);    // 顶点颜色

    [Header("--- 样式: 描边 (Outline) ---")]
    [SerializeField] private bool _showOutline = true;
    [SerializeField] private float _outlineThickness = 3.0f;
    [SerializeField] private Color _outlineColor = Color.white;

    [Header("--- 样式: 背景底图 (Background) ---")]
    [SerializeField] private bool _drawBackground = true; // 是否绘制满属性的灰色底图
    [SerializeField] private Color _bgColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    
    // 缓存：5边形的固定角度 (0, 72, 144...)
    private static readonly float[] _angles = new float[] { 0f, 72f, 144f, 216f, 288f };

    protected override void OnValidate()
    {
        base.OnValidate();
        // 强制保持5个数据，防止手滑删减
        if (_datas.Length != 5) System.Array.Resize(ref _datas, 5);
        SetVerticesDirty(); // 触发重绘
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        // 计算实际半径
        float r = Mathf.Min(rect.width, rect.height) * 0.5f * _radiusScale;

        // 1. (可选) 绘制背景底图 - 一个满属性的五边形
        if (_drawBackground)
        {
            DrawPentagon(vh, center, r, 1.0f, _bgColor, _bgColor, false, 0, Color.clear);
        }

        // 2. 绘制实际数据雷达图
        // 计算归一化后的比例数组
        float[] normalizedValues = new float[5];
        for (int i = 0; i < 5; i++)
        {
            normalizedValues[i] = Mathf.Clamp01(_datas[i] / _maxValue);
        }

        DrawPentagon(vh, center, r, normalizedValues, _centerColor, _tipColor, _showOutline, _outlineThickness, _outlineColor);
    }

    /// <summary>
    /// 通用绘制方法：支持统一半径（画背景）或变长半径（画数据）
    /// </summary>
    private void DrawPentagon(VertexHelper vh, Vector2 center, float maxRadius, object radiusData, Color colCenter, Color colTip, bool drawLine, float lineWidth, Color lineCol)
    {
        // 判断是传入了 float[] (数据) 还是 float (背景)
        float[] values = radiusData as float[];
        float fixedVal = (values == null) ? (float)radiusData : 1f;

        // --- 绘制填充面 ---
        int centerIdx = vh.currentVertCount;
        vh.AddVert(center, colCenter, Vector2.zero); // 中心点

        Vector2[] positions = new Vector2[5];

        for (int i = 0; i < 5; i++)
        {
            float val = (values != null) ? values[i] : fixedVal;
            
            // 计算角度：基础角度 + 旋转偏移 + 90度(让第一个点朝上)
            float rad = (_angles[i] + _rotationOffset + 90) * Mathf.Deg2Rad;
            Vector2 pos = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (maxRadius * val);
            
            positions[i] = pos;
            vh.AddVert(pos, colTip, Vector2.zero);
        }

        // 构建三角形扇形
        for (int i = 0; i < 5; i++)
        {
            // 索引关系: Center(0) -> Point(i+1) -> Point(next)
            vh.AddTriangle(centerIdx, centerIdx + i + 1, centerIdx + (i + 1) % 5 + 1);
        }

        // --- 绘制描边 ---
        if (drawLine && lineWidth > 0)
        {
            for (int i = 0; i < 5; i++)
            {
                AddQuadLine(vh, positions[i], positions[(i + 1) % 5], lineWidth, lineCol);
            }
        }
    }

    // 绘制有宽度的线段
    private void AddQuadLine(VertexHelper vh, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 dir = (end - start).normalized;
        // 计算法线方向，用于向两侧扩展宽度
        Vector2 normal = new Vector2(-dir.y, dir.x) * width * 0.5f;

        int idx = vh.currentVertCount;
        
        vh.AddVert(start - normal, color, Vector2.zero);
        vh.AddVert(start + normal, color, Vector2.zero);
        vh.AddVert(end + normal, color, Vector2.zero);
        vh.AddVert(end - normal, color, Vector2.zero);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx);
    }

    // --- 公开 API ---

    public void SetValue(int index, float val)
    {
        if (index >= 0 && index < 5)
        {
            _datas[index] = val;
            SetVerticesDirty();
        }
    }

    public float[] GetData()
    {
        return _datas;
    }
}