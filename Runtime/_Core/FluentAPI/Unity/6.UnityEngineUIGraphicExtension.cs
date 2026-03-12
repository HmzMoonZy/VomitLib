/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine;
using UnityEngine.UI;

namespace FluentAPI
{

    public static class UnityEngineUIGraphicExtension
    {
        public static T ColorAlpha<T>(this T selfGraphic, float alpha) where T : Graphic
        {
            var color = selfGraphic.color;
            color.a = alpha;
            selfGraphic.color = color;
            return selfGraphic;
        }
        
        /// <summary>
        /// 清空按钮点击监听
        /// </summary>
        public static void ClearClickListener(this Button selfBtn)
        {
            selfBtn.onClick.RemoveAllListeners();
        }
        
        #region Image & Texture

        public static Sprite ToSprite(this Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        
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