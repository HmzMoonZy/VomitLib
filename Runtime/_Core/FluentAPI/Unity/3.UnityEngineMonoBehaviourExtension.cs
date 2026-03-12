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

    public static class UnityEngineMonoBehaviourExtension
    {
        public static void Example()
        {
            var gameObject = new GameObject();
            var component = gameObject.GetComponent<MonoBehaviour>();

            component.Enable(); // component.enabled = true
            component.Disable(); // component.enabled = false
        }


        public static T Enable<T>(this T selfBehaviour, bool enable = true) where T : Behaviour
        {
            selfBehaviour.enabled = enable;
            return selfBehaviour;
        }


        public static T Disable<T>(this T selfBehaviour) where T : Behaviour
        {
            selfBehaviour.enabled = false;
            return selfBehaviour;
        }
    }
}