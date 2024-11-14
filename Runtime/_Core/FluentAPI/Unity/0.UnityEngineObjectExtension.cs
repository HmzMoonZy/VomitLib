/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine;

namespace FluentAPI
{
    public static class UnityEngineObjectExtension
    {

        public static T Instantiate<T>(this T selfObj) where T : UnityEngine.Object
        {
            return UnityEngine.Object.Instantiate(selfObj);
        }

        public static T Instantiate<T>(this T selfObj, Vector3 position, Quaternion rotation)
            where T : UnityEngine.Object
        {
            return UnityEngine.Object.Instantiate(selfObj, position, rotation);
        }


        public static T Instantiate<T>(
            this T selfObj,
            Vector3 position,
            Quaternion rotation,
            Transform parent)
            where T : UnityEngine.Object
        {
            return UnityEngine.Object.Instantiate(selfObj, position, rotation, parent);
        }


        public static T InstantiateWithParent<T>(this T selfObj, Transform parent, bool worldPositionStays)
            where T : UnityEngine.Object
        {
            return (T)UnityEngine.Object.Instantiate((UnityEngine.Object)selfObj, parent, worldPositionStays);
        }
        
        public static T InstantiateWithParent<T>(this T selfObj, Component parent, bool worldPositionStays)
            where T : UnityEngine.Object
        {
            return (T)UnityEngine.Object.Instantiate((UnityEngine.Object)selfObj, parent.transform, worldPositionStays);
        }

        public static T InstantiateWithParent<T>(this T selfObj, Transform parent) where T : UnityEngine.Object
        {
            return UnityEngine.Object.Instantiate(selfObj, parent, false);
        }
        
        public static T InstantiateWithParent<T>(this T selfObj, Component parent) where T : UnityEngine.Object
        {
            return UnityEngine.Object.Instantiate(selfObj, parent.transform, false);
        }



        public static T Name<T>(this T selfObj, string name) where T : UnityEngine.Object
        {
            selfObj.name = name;
            return selfObj;
        }



        public static void DestroySelf<T>(this T selfObj) where T : UnityEngine.Object
        {
            UnityEngine.Object.Destroy(selfObj);
        }


        public static T DestroySelfGracefully<T>(this T selfObj) where T : UnityEngine.Object
        {
            if (selfObj)
            {
                UnityEngine.Object.Destroy(selfObj);
            }

            return selfObj;
        }



        public static T DestroySelfAfterDelay<T>(this T selfObj, float afterDelay) where T : UnityEngine.Object
        {
            UnityEngine.Object.Destroy(selfObj, afterDelay);
            return selfObj;
        }


        public static T DestroySelfAfterDelayGracefully<T>(this T selfObj, float delay) where T : UnityEngine.Object
        {
            if (selfObj)
            {
                UnityEngine.Object.Destroy(selfObj, delay);
            }

            return selfObj;
        }


        public static T DontDestroyOnLoad<T>(this T selfObj) where T : UnityEngine.Object
        {
            UnityEngine.Object.DontDestroyOnLoad(selfObj);
            return selfObj;
        }
    }
}