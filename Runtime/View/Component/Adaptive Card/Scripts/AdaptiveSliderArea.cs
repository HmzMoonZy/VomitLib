using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Twenty2.VomitLib.View.Component
{
    //[ExecuteInEditMode]
[RequireComponent(typeof(RectMask2D))]
public abstract class AdaptiveSliderArea : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    /// <summary>
    /// 响应方向
    /// </summary>
    protected enum SlideDir { Prev, Next }
    // UI 遮罩
    private RectMask2D _mask;
    // 自适应节点
    protected RectTransform Node;
    // 预制体
    protected GameObject ElementPrefab;
    // 拖动时的点击坐标
    protected Vector2 Touch = Vector2.zero;
    // 当前元素索引
    protected int Index = 0;
    // 所有子元素
    protected List<RectTransform> Elements;
    // 当前选中的对象
    public GameObject CurrElement { get; private set; }
    
    protected Vector2 ElementSize = Vector2.zero;
    
    // 起始索引
    [SerializeField] private int _initialIndex;
    // 同时展示的数量
    [SerializeField] protected float ShowCount = 3;
    // 拖拽响应
    [SerializeField] protected float _slidingDistance = 80f;
    //切换元素回调
    public Action<GameObject> OnChangeElement;  
    //展示界面回调
    public Action OnShowed;        
    

    private void Awake()
    {
        Node = transform.Find("node").GetComponent<RectTransform>();
        if (Node == null)
        {
            Debug.LogWarning("未设置Node(RectTransform), 所有元素应该放置在Node节点下");
            return;
        }
    }

    private void Start()
    {
        // Init(_initialIndex);
    }
    
    /// <summary>
    /// 在这个方法里确定每个元素的尺寸
    /// </summary>
    protected abstract void InitLayout(ref Vector2 size);
    private void Init(int index = 0)
    {
        Node.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
        Node.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
        Node.anchorMin = Vector2.zero;
        Node.anchorMax = Vector2.one;
        
        InitLayout(ref ElementSize);

        if (Node.childCount != 0)
        {
            Refresh(index);
        }
        else
        {
            Debug.Log("node下没有元素!");
        }
    }
    
    protected void Slide(SlideDir s)
    {
        switch (s)
        {
            case AdaptiveSliderArea.SlideDir.Prev:
                if (Index < Elements.Count - 1)
                {
                    Index++;
                    SelectCard(Index);
                }
                break;
            case AdaptiveSliderArea.SlideDir.Next:
                if (Index > 0)
                {
                    Index--;
                    SelectCard(Index);
                }
                break;

        }
    }

    protected abstract void OnSelectCard(int i);
    private void SelectCard(int i)
    {
        if (i < 0 || i > Elements.Count - 1 || Elements.Count < 2)
        {
            return;
        }
        CurrElement = Elements[Index].gameObject;
        OnChangeElement?.Invoke(CurrElement);
        OnSelectCard(i);
    }

    protected abstract void OnSetTrans(RectTransform child);
    protected abstract void SetChildPosition();
    
    private void SetTrans()
    {
        Elements = new List<RectTransform>();
        foreach (RectTransform child in Node)
        {
            Elements.Add(child);
            OnSetTrans(child);
        }

        SetChildPosition();

        if (ShowCount > Elements.Count)
        {
            ShowCount = Elements.Count;
        }
    }

#region 拖拽接口
    public void OnBeginDrag(PointerEventData eventData)
    {
        Touch = Input.mousePosition;
    }
    public abstract void OnDrag(PointerEventData eventData);
    public abstract void OnEndDrag(PointerEventData eventData);

#endregion



#region 外部接口

    /// <summary>
    /// Editor中调用
    /// </summary>
    /// <param name="node"></param>
    public void SetNode(RectTransform node)
    {
        this.Node = node;
    }

    /// <summary>
    /// 返回元素数量
    /// </summary>
    /// <returns></returns>
    public int GetElementsCount()
    {
        if (Elements != null)
        {
            return Elements.Count;
        }
        else return 0;
    }

    /// <summary>
    /// 根据索引找到元素
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject GetObjectByIndex(int index)
    {
        if (index < 0 || index >= Elements.Count)
        {
            return null;
        }
        return Elements[index].gameObject;
    }

    /// <summary>
    /// 获得当前的元素
    /// </summary>
    /// <returns></returns>
    public GameObject GetCurObject()
    {
        return CurrElement;
    }

    /// <summary>
    /// 获得当前元素的下标
    /// </summary>
    /// <returns></returns>
    public int GetCurObjectIndex()
    {
        return Index;
    }

    /// <summary>
    /// 刷新元素数组, 设置焦点, 通常使用这个.
    /// </summary>
    /// <param name="index"></param>
    public void Refresh(int index = 0)
    {
        SetTrans();
        this.Index = index;
        SelectCard(index);
        OnShowed?.Invoke();
    }

    /// <summary>
    /// 强制刷新
    /// </summary>
    public void ForceRefresh(int index)
    {
        Init(index);
    }

    public void ForceRefresh()
    {
        Init(_initialIndex);
    }

    /// <summary>
    /// 添加指定数量的预制体
    /// </summary>
    /// <param name="count">添加地数量</param>
    /// <param name="index">刷新焦点</param>
    public void AddPrefab(int count, int index = 0)
    {
        if (ElementPrefab == null)
        {
            Debug.Log("未设置prefab!");
            return;
        }
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(ElementPrefab, Node);
            if (!obj.activeSelf)
            {
                obj.SetActive(true);
            }
            obj.name = "prefab" + i;
        }

        Refresh(index);
        
    }

    /// <summary>
    /// 添加指定数量的GameObject, 需要刷新
    /// </summary>
    public void AddGameObject(GameObject go)
    {
        if (go == null)
        {
            return;
        }
        if (!go.activeSelf)
        {
            go.SetActive(true);
        }
        go.transform.SetParent(Node);
        go.transform.localPosition = Vector3.zero;
    }
    
    /// <summary>
    /// 设置同时展示的元素数量
    /// 需要ForceRefresh
    /// </summary>
    /// <param name="showCount"></param>
    public void SetShowCount(float showCount)
    {
        this.ShowCount = showCount;
    }

    /// <summary>
    /// 清空node下的元素
    /// </summary>
    public void ClearElement()
    {
        if (Node.childCount <= 0)
        {
            return;
        }
        foreach (Transform element in Node)
        {
            Destroy(element.gameObject);
        }
    }
#endregion
}
}


