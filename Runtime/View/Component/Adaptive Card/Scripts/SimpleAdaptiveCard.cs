using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SocialPlatforms;

/// <summary>
/// 依靠Horizontal Layout Group组件, 简单的自适应离散卡牌展示;
/// 自适应屏幕宽度, 展示3个元素;
/// </summary>
[RequireComponent(typeof(HorizontalLayoutGroup))]
public class SimpleAdaptiveCard : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private float childWidth;
    private float offset;

    //public bool isForceAdaptive = false;

    public GameObject objPrefab;

    private RectTransform m_rect;


    [Header("坐标设置")]
    public float height = 600;         //尺寸
    public float bottom = 1200;     //posY

    [Header("元素")]
    public int count = 0;
    public int index = 0;
    public GameObject leftObj;
    public GameObject curObj;
    public GameObject rightObj;
    public List<RectTransform> trans = new List<RectTransform>();

    [Header("缩放值")]
    public float scaleValue = 0.8f;
    public float duration = 1f;
    private Vector2 selecScal = Vector2.one;
    private Vector2 unSelecScal = new Vector2();

    [Header("布局组件")]
    public int spacing = 10;
    public HorizontalLayoutGroup m_group;

    private void Awake()
    {
        this.childWidth = (Screen.width - 2 * this.spacing) / (2 * this.scaleValue + 1);
        this.offset = (Screen.width / 2) - (this.childWidth / 2);
        Debug.Log(childWidth);
        Debug.Log(offset);
        //Init Self
        m_rect = gameObject.GetComponent<RectTransform>();
        m_rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, offset, Screen.width);
        m_rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, bottom, height);

        //Init Group
        m_group = gameObject.GetComponent<HorizontalLayoutGroup>();
        SetHorizontalLayoutGroup(spacing);
        this.unSelecScal.x = scaleValue;
        this.unSelecScal.y = scaleValue;

        //Init Child
        trans = new List<RectTransform>();
        SetTrans();

        SelectCard(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("Press A");
            if (index > 0)
            {
                index--;
                SelectCard(index);
            }
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("Press D");
            if (index < count - 1)
            {
                index++;
                SelectCard(index);
            }
        }
    }

    private void SelectCard(int index)
    {
        if (index < 0 || index > count - 1 || count < 2)
        {
            return;
        }

        m_rect.DOLocalMoveX(offset - (index * offset), duration);
        SetObjState();
    }

    private void SetTrans()
    {
        foreach (RectTransform child in m_rect)
        {
            trans.Add(child);
            child.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, this.childWidth);
        }
        count = trans.Count;

    }

    private void SetObjState()
    {
        curObj = trans[index].gameObject;
        if (index == 0)
        {
            leftObj = null;
            rightObj = trans[index + 1].gameObject;
        }
        else if (index == count - 1)
        {
            leftObj = trans[index - 1].gameObject;
            rightObj = null;
        }
        else
        {
            leftObj = trans[index - 1].gameObject;
            rightObj = trans[index + 1].gameObject;
        }

        for (int i = 0; i < trans.Count; i++)
        {
            GameObject obj = trans[i].gameObject;
            if (obj == leftObj || obj == rightObj)
            {
                //obj.SetActive(true);
                obj.transform.localScale = unSelecScal;
            }
            else if (obj == curObj)
            {
                //obj.SetActive(true);
                obj.transform.localScale = selecScal;
            }
            else
            {
                //obj.SetActive(false);
                obj.transform.localScale = unSelecScal;
            }
        }
        DoRefreshGroup();
    }

    private void DoRefreshGroup()
    {
        m_group.spacing--;
        m_group.spacing++;
    }

    private void SetHorizontalLayoutGroup(int spacing)
    {
        m_group.childAlignment = TextAnchor.MiddleLeft;

        m_group.spacing = spacing;

        m_group.childControlHeight = true;
        m_group.childControlWidth = false;

        m_group.childForceExpandHeight = true;
        m_group.childForceExpandWidth = false;

        m_group.childScaleHeight = true;
        m_group.childScaleWidth = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("OnDrag");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("ObEndDrag");
    }


    #region 外部接口

    public int TransCount()
    {
        if (trans != null)
        {
            return trans.Count;
        }
        else return 0;
    }

    public GameObject GetObjectByIndex(int index)
    {
        if (index < 0 || index >= trans.Count)
        {
            return null;
        }
        return trans[index].gameObject;
    }

    public GameObject GetCurObject()
    {
        return curObj;
    }
    #endregion


}
