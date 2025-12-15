using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadarChart5DUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private enum ValueDisplayType { Always, PointerEnter, Hide, }

    private const float VERTICES_SIZE_MAX = 247f;
    private const float VERTICES_ANGLE_INCREMENT = 72f; // 360f / 5

    [SerializeField] private Material _radarMaterial;
    [SerializeField] private Texture2D _radarTexture2D;
    [SerializeField] private CanvasRenderer _meshRenderer;
    
    [SerializeField]
    private ValueDisplayType _propertyDisplayType;

    [SerializeField]
    private string _propertyName1;

    [SerializeField]
    private string _propertyName2;

    [SerializeField]
    private string _propertyName3;

    [SerializeField]
    private string _propertyName4;

    [SerializeField]
    private string _propertyName5;

    [SerializeField]
    private int _fontSize = 30;

    [Header("数值显示方式")] 
    [SerializeField] private Color _color = Color.white;

    [Header("数值取值")] 
    [SerializeField] private int _limit;

    Mesh _mesh;
    private int[] _value = new int[5];
    private float[] _normalizeValue = new float[5];

    private Text[] _propertyTexts;

    private void Awake()
    {
        if (_propertyDisplayType != ValueDisplayType.Hide)
        {
            _propertyTexts = new[]
            {
                transform.Find("_propertyName1").GetComponent<Text>(),
                transform.Find("_propertyName2").GetComponent<Text>(),
                transform.Find("_propertyName3").GetComponent<Text>(),
                transform.Find("_propertyName4").GetComponent<Text>(),
                transform.Find("_propertyName5").GetComponent<Text>(),
            };
            
            _propertyTexts[0].fontSize = _fontSize;
            _propertyTexts[1].fontSize = _fontSize;
            _propertyTexts[2].fontSize = _fontSize;
            _propertyTexts[3].fontSize = _fontSize;
            _propertyTexts[4].fontSize = _fontSize;

            _propertyTexts[0].text = _propertyName1;
            _propertyTexts[1].text = _propertyName2;
            _propertyTexts[2].text = _propertyName3;
            _propertyTexts[3].text = _propertyName4;
            _propertyTexts[4].text = _propertyName5;

            _propertyTexts[0].gameObject.SetActive(true);
            _propertyTexts[1].gameObject.SetActive(true);
            _propertyTexts[2].gameObject.SetActive(true);
            _propertyTexts[3].gameObject.SetActive(true);
            _propertyTexts[4].gameObject.SetActive(true);
            
            if (_propertyDisplayType == ValueDisplayType.Always)
            {
                _propertyTexts[0].color = Color.white;
                _propertyTexts[1].color = Color.white;
                _propertyTexts[2].color = Color.white;
                _propertyTexts[3].color = Color.white;
                _propertyTexts[4].color = Color.white;
            }

            if (_propertyDisplayType == ValueDisplayType.PointerEnter)
            {
                transform.Find("Bg").GetComponent<Image>().raycastTarget = true;
            }
        }

        // 不使用默认的颜色
        if (_color != Color.white)
        {
            _radarMaterial = new Material(_radarMaterial);

            _radarMaterial.SetColor("_Color", _color);
        }


        _mesh = new Mesh();
    }

    public void GetValue(out int p1, out int p2, out int p3, out int p4, out int p5)
    {
        p1 = _value[0];
        p2 = _value[1];
        p3 = _value[2];
        p4 = _value[3];
        p5 = _value[4];
    }

    public void GetNormalizeValue(out float p1, out float p2, out float p3, out float p4, out float p5)
    {
        p1 = _normalizeValue[0];
        p2 = _normalizeValue[1];
        p3 = _normalizeValue[2];
        p4 = _normalizeValue[3];
        p5 = _normalizeValue[4];
    }

    public void SetValue(int p1, int p2, int p3, int p4, int p5)
    {
        _value[0] = p1;
        _value[1] = p2;
        _value[2] = p3;
        _value[3] = p4;
        _value[4] = p5;

        _normalizeValue[0] = Mathf.Clamp(p1, 0, _limit) / (float) _limit;
        _normalizeValue[1] = Mathf.Clamp(p2, 0, _limit) / (float) _limit;
        _normalizeValue[2] = Mathf.Clamp(p3, 0, _limit) / (float) _limit;
        _normalizeValue[3] = Mathf.Clamp(p4, 0, _limit) / (float) _limit;
        _normalizeValue[4] = Mathf.Clamp(p5, 0, _limit) / (float) _limit;

        UpdateStatesVisual();
    }
    
    public void SetLimit(int limit)
    {
        _limit = limit;
    }

    private void UpdateStatesVisual()
    {
        Vector3[] vertices = new Vector3[6];
        Vector2[] uv = new Vector2[6];
        int[] triangles = new int[15]; // 3 * 5

        vertices[0] = Vector3.zero;
        for (int i = 1; i < 6; i++)
        {
            // 旋转角 * 归一化值 * 雷达图坐标极限值 * (0, 1, 0)
            var euler = Quaternion.Euler(0, 0, -VERTICES_ANGLE_INCREMENT * (i - 1));
            float length = _normalizeValue[i - 1] * VERTICES_SIZE_MAX;
            vertices[i] = euler * Vector3.up * length;
        }

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        triangles[6] = 0;
        triangles[7] = 3;
        triangles[8] = 4;

        triangles[9] = 0;
        triangles[10] = 4;
        triangles[11] = 5;

        triangles[12] = 0;
        triangles[13] = 5;
        triangles[14] = 1;


        uv[0] = Vector2.zero;
        uv[1] = Vector2.one;
        uv[2] = Vector2.one;
        uv[3] = Vector2.one;
        uv[4] = Vector2.one;
        uv[5] = Vector2.one;


        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;

        _meshRenderer.SetMesh(_mesh);
        _meshRenderer.SetMaterial(_radarMaterial, _radarTexture2D);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _propertyTexts[0].color = Color.white;
        _propertyTexts[1].color = Color.white;
        _propertyTexts[2].color = Color.white;
        _propertyTexts[3].color = Color.white;
        _propertyTexts[4].color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _propertyTexts[0].color = Color.clear;
        _propertyTexts[1].color = Color.clear;
        _propertyTexts[2].color = Color.clear;
        _propertyTexts[3].color = Color.clear;
        _propertyTexts[4].color = Color.clear;
    }
}