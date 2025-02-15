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
        public static T Init<T>(Func<string, TextAsset> loader) where T : class
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

                return (T) typeof(T).GetConstructors()[0].Invoke(new object[] {processor});
            }
            catch (Exception e)
            {
                LogKit.E("ClientDB Init Error!");
                LogKit.E(e.Message);
                throw;
            }
            
            
            ByteBuf BinLoader(string sourceName)
            {
                var source = loader?.Invoke(sourceName);
                
                if (source == null)
                {
                    return null;
                }
                
                return new Luban.ByteBuf(source.bytes);
            }

            JArray JsonLoader(string sourceName)
            {
                var source = loader?.Invoke(sourceName);
                
                if (source == null)
                {
                    return null;
                }
                
                return JsonConvert.DeserializeObject<JArray>(source.text);
            }
        }
    }
}