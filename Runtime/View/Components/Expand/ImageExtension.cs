using UnityEngine;
using UnityEngine.UI;

namespace Twenty2.VomitLib.View
{
    public static class ImageExtension
    {
        public static void SetUrl(this Image self)
        {
            // TODO
        }
        
        public static void SetTexture(this Image self)
        {
            // TODO
        }

        public static void SetSprite(this Image self, Sprite sprite, bool isNaive = false)
        {
            self.sprite = sprite;
            if (isNaive)
            {
                self.SetNativeSize();
            }
        }
        
    }
}