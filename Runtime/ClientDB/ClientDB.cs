using System;
using System.IO;
using QFramework;
using SimpleJSON;
using Twenty2.VomitLib.ClientDB;
using UnityEngine;
using UnityEngine.Networking;


namespace Twenty2.VomitLib.ClientDB
{
    /// <summary>
    /// 基于 Luban 的本地数据库.
    /// </summary>
    /// <typeparam name="TTable">Luban 生成的 Tables 类型.</typeparam>
    public static class ClientDB<TTable>
    {
        /// <summary>
        /// 本地数据库(只读)
        /// </summary>
        public static TTable T;

        /// <summary>
        /// 数据库是否可用
        /// </summary>
        private static bool _isValid;

        public static bool IsValid => _isValid;


        // public static JSONNode Loader(string fileName)
        // {
        //     string sourcePath = Path.Combine(Vomit.RuntimeConfig.ClientDBConfig.JsonOutputPath,
        //         $"{fileName}.{Vomit.RuntimeConfig.ClientDBConfig.Suffix}");
        //     Debug.Log(sourcePath);
        //     return JSON.Parse(File.ReadAllText(sourcePath));
        //
        //     // if (Application.platform != RuntimePlatform.Android)
        //     // {
        //     //     string sourcePath = Path.Combine(Application.streamingAssetsPath, "DB", fileName + ".dat");
        //     //     return JSON.Parse(File.ReadAllText(sourcePath));
        //     // }
        //
        //     // var request = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, "DB", fileName + ".dat"));
        //     // request.SendWebRequest();
        //     // while (true)
        //     // {
        //     //     if (request.downloadHandler.isDone)
        //     //     {
        //     //         return JSON.Parse(request.downloadHandler.text);
        //     //     }
        //     // }
        // }

        public static void Init(object t)
        {
            T = (TTable) t;
            _isValid = true;
        }

        // public static void SwitchLanguage(Language language)
        // {
        //     if (!IsValid)
        //     {
        //         return;
        //     }
        //
        //     switch (language)
        //     {
        //         case Language.ChineseSimplified:
        //             T.TranslateText(TextMapperCN);
        //             break;
        //         case Language.English:
        //             T.TranslateText(TextMapperEN);
        //             break;
        //         default:
        //             T.TranslateText(TextMapperEN);
        //             break;
        //     }
        //
        //     LogKit.I($"ClientDB Language : {language}");
        //
        //     string TextMapperCN(string key, string originText)
        //     {
        //         return T.TbLocalization.GetOrDefault(key)?.TextCn ?? originText;
        //     }
        //
        //     string TextMapperEN(string key, string originText)
        //     {
        //         return T.TbLocalization.GetOrDefault(key)?.TextEn ?? originText;
        //     }
        // }
        //
        // public static string Translate(string key)
        // {
        //     return T.TbLocalization.GetOrDefault(key)?.TextCn ?? key;
        // }
    }
}