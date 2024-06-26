using UnityEngine;

namespace Twenty2.VomitLib.Tools
{
    public static class CameraKit
    {
        /// <summary>
        /// 获取指定距离下透视相机视口四个角的坐标
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="distance">相对于相机的距离</param>
        /// <returns></returns>
        public static Vector3[] GetFovPositionByDistanceWithPerspective(this Camera cam, float distance)
        {
            Vector3[] corners = new Vector3[4];

            float halfFOV = (cam.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            float aspect = cam.aspect;

            float height = distance * Mathf.Tan(halfFOV);
            float width = height * aspect;

            Transform tx = cam.transform;

            // 左上角
            corners[0] = tx.position - (tx.right * width);
            corners[0] += tx.up * height;
            corners[0] += tx.forward * distance;

            // 右上角
            corners[1] = tx.position + (tx.right * width);
            corners[1] += tx.up * height;
            corners[1] += tx.forward * distance;

            // 左下角
            corners[2] = tx.position - (tx.right * width);
            corners[2] -= tx.up * height;
            corners[2] += tx.forward * distance;

            // 右下角
            corners[3] = tx.position + (tx.right * width);
            corners[3] -= tx.up * height;
            corners[3] += tx.forward * distance;

            return corners;
        }
        
        /// <summary>
        /// 获取指定距离下正交相机视口四个角的坐标
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="distance">相对于相机的距离</param>
        /// <returns></returns>
        public static Vector3[] GetFovPositionByDistanceWithOrthogonal(this Camera cam, float distance)
        {
            Vector3[] corners = new Vector3[4];

            float halfHeight = cam.orthographicSize; // 正交相机的垂直半视口大小
            float halfWidth = halfHeight * cam.aspect; // 根据相机宽高比计算水平半视口大小

            Transform tx = cam.transform;

            // 左上角
            corners[0] = tx.position - (tx.right * halfWidth) - (tx.up * halfHeight);
            corners[0] += tx.forward * distance;

            // 右上角
            corners[1] = tx.position + (tx.right * halfWidth) - (tx.up * halfHeight);
            corners[1] += tx.forward * distance;

            // 左下角
            corners[2] = tx.position - (tx.right * halfWidth) + (tx.up * halfHeight);
            corners[2] += tx.forward * distance;

            // 右下角
            corners[3] = tx.position + (tx.right * halfWidth) + (tx.up * halfHeight);
            corners[3] += tx.forward * distance;

            return corners;
        }
    }
}