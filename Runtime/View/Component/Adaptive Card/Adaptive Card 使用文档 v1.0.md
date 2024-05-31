# Adaptive Card 使用文档 v1.0

## 描述

    该组件能根据设定的参数自动填充在编辑器下设定好的矩形区域.  
    目前能够实现横向自动布局.  

### 使用方法

> 在Hierarchy面板中右键点击任意Canvas组件, 依次选择/JODO/AddAdaptiveCard  
> 拖动组件至合适的位置  
> 设定参数或使用代码控制 

## 结构  

    当挂载组件时, 默认添加\Mask\Image组件
    当使用右键生成时,会额外生成node子物体并自动与组件绑定.

## 变量

| 变量名 |   描述
| :----- | :---- |
| node|所有需要自适应元素的父物体|
|prefab|可通过组件生成的预制体,只能通过编辑器添加|
|slidingDistance|滑动判定距离,默认为80.0|
|showCount|同时展示的元素数量, 子元素根据它设定宽度, 建议设定为奇数个|
|initialIndex|初始选定的元素, 默认为0.例如: 当showCount=3, initialIndex = 1 时, 初始化时显示三个元素|
|curObj|当前选定的物体|
|trans|所有元素的列表|
|scaleValue|非选定元素的缩放值, 通常小于1|
|duration|一次滑动动画的持续时间, 默认为0.2
|spacing|每个元素缩放值为1时的间隔|
|OnChangeElement|改变选择元素时的回调|
|OnShowed|展示元素时的回调, 使用刷新API也会触发

## 公共函数

| 方法名 |   描述
| :----- | :---- |
| SetNode(rectT)|设置Node的值|
|GetElementsCount()|返回所有元素的数量
|GetObjectByIndex(index)|根据索引返回GameObject元素
|GetCurObject()|返回当前选择的元素
|Refresh(index)|重新计算元素列表并设置当前元素
|ForceRefresh(index)|强制重新初始化
|AddPrefab(count, index)|添加指定数量的prefab,并Refresh
|AddGameObject(obj, count, index)|添加指定数量的obj,并Refresh
|SetSlideDuration(duration)|设置滑动持续时间
|SetSpacing(spacing)|设置元素间隔, 需要ForceRefresh
|SetScaleValue(value)|设置未选择元素的缩放值, 通常小于1,需要ForceRefresh
|SetShowCount(count)|设置同时展示的元素数量, 建议为奇数个,需要ForceRefresh
|Create(Transform)|静态函数, 在Transform下创建一个标准的Adaptive Card

## 示例代码

art\Assets\JODO Dev\Adaptive Card\Example目录下查看演示效果   
```
        //在canvas下创建一个自适应组件
        ad = AdaptiveCard.Create(canvas);
        //设定尺寸和坐标
        ad.transform.localPosition = Vector3.zero;


        for (int i = 0; i < 5; i++)
        {
            GameObject go = new GameObject("go" + i);
            go.AddComponent<Image>().color = new Color(Random.Range(100, 255) / 255f, Random.Range(100, 255) / 255f, Random.Range(100, 255) / 255f);
            ad.AddGameObject(go, 0);
        }
        //如果设定prefab, 以上代码等价于
        //ad.AddPrefab(5, 0);


        ad.SetSpacing(15);
        ad.SetSlideDuration(0.1f);
        ad.SetShowCount(3);

        ad.OnShowed = () => { Debug.Log("刷新完毕!"); };
        ad.OnChangeElement = () => { Debug.Log("当前选择的对象为" + ad.GetCurObject().name); };

        //强制刷新(重新初始化)
        ad.ForceRefresh(0);
```
    
    