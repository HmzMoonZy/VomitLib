using QFramework;
using Twenty2.VomitLib.Tools;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public static class UguiExtension
    {
        #region Button - Command 

        public static void BindCommand<T>(this Button btn) where T : ICommand, new()
        {
            btn.onClick.AddListener(() =>
            {
                Vomit.Interface.SendCommand(new T());
            });
        }
        
        public static void BindCommand<T>(this Button btn, T command) where T : ICommand, new()
        {
            btn.onClick.AddListener(() =>
            {
                Vomit.Interface.SendCommand(command);
            });
        }

        #endregion

        #region Image

        public static void SetSprite(this Image img, Sprite sprite)
        {
            img.sprite = sprite;
        }

        public static void SetTexture(this Image img, Texture2D texture)
        {
            img.sprite = texture.ToSprite();
        }
        
        // private static Dictionary<string, Texture2D> s_urlTexture = new Dictionary<string, Texture2D>();
        // public static async UniTask SetUrlTextureAsync(this Image img, string url)
        // {
        //     if (img == null)
        //     {
        //         return;
        //     }
        //     
        //     s_urlTexture.TryGetValue(url, out Texture2D texture);
        //     if (texture != null)
        //     {
        //         img.sprite = texture.ToSprite();
        //         return;
        //     }
        //
        //     var uwr = new UnityWebRequest(url);
        //     var handler = new DownloadHandlerTexture(true);
        //     uwr.downloadHandler = handler;
        //     var response = await uwr.SendWebRequest();
        //     if (response.result != UnityWebRequest.Result.Success)
        //     {
        //         Log.Error($"SetUrlTextureAsync Error! {response.error}");
        //         return;
        //     }
        //     
        //     if (handler.texture == null)
        //     {
        //         Log.Error($"SetUrlTextureAsync texture is null! url: {url}");
        //         return;
        //     }
        //     
        //     s_urlTexture[url] = handler.texture;
        //
        //     if (img == null || img.gameObject == null)
        //     {
        //         return;
        //     }
        //     
        //     img.SetSprite(handler.texture.ToSprite());
        // }

        #endregion
    }
}

