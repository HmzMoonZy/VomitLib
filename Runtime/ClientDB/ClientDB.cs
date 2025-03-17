using System;
using Luban;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Twenty2.VomitLib.Config;
using UnityEngine;

namespace Twenty2.VomitLib.ClientDB
{
    /// <summary>
    /// 基于 Luban 的本地数据库.
    /// </summary>
    /// TODO 支持懒加载, 支持卸载
    public static class ClientDB
    {
        /// <summary>
        /// 加载本地数据表, 并返回预期的 Tables 实例
        /// </summary>
        /// <param name="loader">加载数据表资源</param>
        /// <param name="onLoad">Tables 实例构造完毕回调</param>
        /// <typeparam name="T">Luban 生成的 Tables 类型</typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static T Init<T>(Func<string, TextAsset> loader, Action<TextAsset> onLoad = null) where T : class
        {
            try
            {
                Delegate processor = null;
            
                var config = Vomit.Config.ClientDatabaseConfig;
                
                if (config.Format is ClientDBConfig.JsonFormat.Bin)
                {
                    processor = new Func<string, Luban.ByteBuf>(BinLoader);
                }
                else if (config.Format is ClientDBConfig.JsonFormat.NewtonsoftJson)
                {
                    processor = new Func<string, JArray>(JsonLoader);
                }
                else
                {
                    throw new NotImplementedException();
                }

                T table = typeof(T).GetConstructors()[0].Invoke(new object[] {processor}) as T;
                
                return table;
            }
            catch (Exception e)
            {
                Log.Error("ClientDB Init Error!");
                Log.Error(e.Message);
                throw;
            }
            
            
            ByteBuf BinLoader(string sourceName)
            {
                var source = loader?.Invoke(sourceName);
                
                if (source == null)
                {
                    return null;
                }

                var bytebuf = new Luban.ByteBuf(source.bytes);
                
                onLoad?.Invoke(source);

                return bytebuf;
            }

            JArray JsonLoader(string sourceName)
            {
                var source = loader?.Invoke(sourceName);
                
                if (source == null)
                {
                    return null;
                }
                
                var jarray = JsonConvert.DeserializeObject<JArray>(source.text);
                
                onLoad?.Invoke(source);

                return jarray;
            }
        }
    }
}