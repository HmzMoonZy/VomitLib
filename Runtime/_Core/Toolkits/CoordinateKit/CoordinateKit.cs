using UnityEngine;

/// <summary>
/// 坐标系转换工具
/// </summary>
/// <code>
/// S - Screen
/// W - World
/// U - UI
/// M - TileMap
/// </code>
public static class CoordinateKit
{
    private static Camera s_mainCamera;

    private static Camera s_viewCamera;

    private static Canvas s_benchmarkView;

    private static Grid s_grid;

    /// <summary>
    /// 设置主场景摄像机
    /// </summary>
    public static void SetMainCamera(Camera camera)
    {
        s_mainCamera = camera;
    }

    /// <summary>
    /// 设置UI摄像机
    /// </summary>
    public static void SetViewCamera(Camera viewCamera)
    {
        s_viewCamera = viewCamera;
    }

    /// <summary>
    /// 设置基准画布
    /// </summary>
    public static void SetBenchmarkCanvas(Canvas canvas)
    {
        s_benchmarkView = canvas;
    }

    /// <summary>
    /// 设置基准TilemapGrid
    /// </summary>
    public static void SetGrid(Grid grid)
    {
        s_grid = grid;
    }
    
    public static Vector2 W2S(Vector3 worldPosition)
    {
        return RectTransformUtility.WorldToScreenPoint(s_mainCamera, worldPosition);
    }

    public static Vector2 W2UWorld(Vector3 worldPosition)
    {
        // 屏幕坐标
        var screen = W2S(worldPosition);    
        //将屏幕空间点转换为世界空间中给定RectTransform平面上的位置。
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)s_benchmarkView.transform, 
            screen, 
            s_viewCamera,
            out var uiPosition);
        return uiPosition;
    }

    public static Vector2 W2ULocal(Vector3 worldPosition)
    {
        var screen = W2S(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)s_benchmarkView.transform, 
            screen, 
            s_viewCamera, 
            out var uiPosition);
        return uiPosition;
    }

    public static Vector3Int W2M(Vector3 worldPosition)
    {
        return s_grid.WorldToCell(worldPosition);
    }

    public static Vector3Int S2M(Vector2 screenPosition)
    {
        return W2M(S2WSceneCamera(screenPosition));
    }

    public static Vector3 U2W(Vector3 uiPosition)
    {
        return S2WSceneCamera(U2S(uiPosition));
    }

    public static Vector2 U2S(Vector3 uiPosition)
    {
        return RectTransformUtility.WorldToScreenPoint(s_viewCamera, uiPosition);
    }

    public static Vector3 M2W(Vector3Int mapPosition)
    {
        return s_grid.CellToWorld(mapPosition);
    }

    public static Vector3 MCenter2W(Vector3Int mapPosition)
    {
        return s_grid.GetCellCenterWorld(mapPosition);
    }

    public static Vector3 S2WViewCamera(Vector2 screenPosition)
    {
        return s_viewCamera.ScreenToWorldPoint(screenPosition);
    }

    public static Vector3 S2WSceneCamera(Vector2 screenPosition)
    {
        return s_mainCamera.ScreenToWorldPoint(screenPosition);
    }

    public static Vector3 S2WIgnoreZViewCamera(Vector2 screenPosition)
    {
        var result = S2WViewCamera(screenPosition);
        result.z = 0;
        return result;
    }

    public static Vector3 S2WIgnoreZSceneCamera(Vector2 screenPosition)
    {
        var result = S2WSceneCamera(screenPosition);
        result.z = 0;
        return result;
    }
}