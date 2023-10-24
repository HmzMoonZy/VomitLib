using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Twenty2.VomitLib.Addr
{
    /// <summary>
    /// 基于 Unity.Addressable 的资源管理器
    /// </summary>
    /// <code>
    /// AA 在原先的 AB 基础上做了增强,本质上提供了 [通过资源的唯一名称(寻址地址)找到这个资源], 而无需关心资源具体位置.
    ///
    /// 于是,这个类则提供,通过[类型]+[资源名]的方式来自动拼接[唯一的资源名].
    /// 因为对于大多数项目,同一种类的资源命名规则理应是统一的.
    /// </code>
    public static class Addr
    {
        private static Dictionary<AddrResType, Func<string, string>> splicingRule = new ();
        
        private static Dictionary<string, AddrLoadAsyncHandle> asyncHandles = new();

        private static Dictionary<string, object> resCaches = new();

        public static bool RegisterRule<T>(object subType, Func<string, string> splicingFunc)
        {
            var t = new AddrResType(typeof(T), subType);
            
            if (splicingRule.ContainsKey(t)) return false;
            
            splicingRule.Add(t, splicingFunc);
            return true;
        }

        public static T Load<T>(object subType, string index) where T : class
        {
            return __Load<T>(new AddrResType(typeof(T), subType), index);
        }
        
        public static T Load<T>(object subType, object index) where T : class
        {
            return __Load<T>(new AddrResType(typeof(T), subType), index.ToString());
        }
        
        public static UniTask<T> LoadAsync<T>(object subType, string index) where T : class
        {
            return __LoadAsync<T>(new AddrResType(typeof(T), subType), index);
        }
        
        public static UniTask<T> LoadAsync<T>(object subType, object index) where T : class
        {
            return __LoadAsync<T>(new AddrResType(typeof(T), subType), index.ToString());
        }
        
        public static void LoadAsync<T>(object subType, string index, Action<T> callback) where T : class
        {
            __LoadAsync<T>(new AddrResType(typeof(T), subType), index, callback);
        }
        
        public static void LoadAsync<T>(object subType, object index, Action<T> callback) where T : class
        {
            __LoadAsync<T>(new AddrResType(typeof(T), subType), index.ToString(), callback);
        }

        private static T __Load<T>(AddrResType resType, string index) where T : class
        {
            // 寻找规则
            if (!splicingRule.TryGetValue(resType, out var func))
            {
                LogKit.E("Addr 没有对应的寻址规则");
                return null;
            }
            
            var address = func(index);
            
            // 缓存
            if (resCaches.TryGetValue(address, out object cache))
            {
                return (T) cache;
            }


            if (asyncHandles.ContainsKey(address))
            {
                LogKit.E($"同步加载的资源{address}正在异步加载中,会造成重复加载");
            }

            var result = Addressables.LoadAssetAsync<T>(address).WaitForCompletion();
            resCaches.Add(address, result);
            return result;
        }
        
        private static async UniTask<T> __LoadAsync<T>(AddrResType resType, string index) where T : class
        {
            // 寻找规则
            if (!splicingRule.TryGetValue(resType, out var func))
            {
                LogKit.E("Addr 没有对应的寻址规则");
                return null;
            }

            var address = func(index);
            
            // 缓存
            if (resCaches.TryGetValue(address, out object cache))
            {
                return (T) cache;
            }
            
            if (asyncHandles.TryGetValue(address, out var addrHandle))
            {
                LogKit.W($"多个重复的异步加载请求 [{address}]");
                await addrHandle.ToUniTask();
                return (T) addrHandle.Result;
            }

            addrHandle = new AddrLoadAsyncHandle(Addressables.LoadAssetAsync<T>(address));
            asyncHandles.Add(address, addrHandle);
            await addrHandle.ToUniTask();
            resCaches.Add(address, addrHandle.Result);
            asyncHandles.Remove(address);
            return (T) addrHandle.Result;
        }
        
        private static async void __LoadAsync<T>(AddrResType resType, string index, Action<T> callback) where T : class
        {
            // 寻找规则
            if (!splicingRule.TryGetValue(resType, out var func))
            {
                LogKit.E("Addr 没有对应的寻址规则");
                return;
            }

            var address = func(index);
            
            // 缓存
            if (resCaches.TryGetValue(address, out object cache))
            {
                callback?.Invoke((T)cache);
                return;
            }
            
            if (asyncHandles.TryGetValue(address, out var addrHandle))
            {
                LogKit.W($"多个重复的异步加载请求 [{address}]");
                addrHandle.OnComplete(callback);    
                return;
            }


            addrHandle = new AddrLoadAsyncHandle(Addressables.LoadAssetAsync<T>(address));
            addrHandle.OnComplete(callback);
            asyncHandles.Add(address, addrHandle);
            await addrHandle.ToUniTask();
            resCaches.Add(address, addrHandle.Result);
            asyncHandles.Remove(address);
        }
    }
    
}