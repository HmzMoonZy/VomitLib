using System;
using System.Linq;
using System.Text;

namespace Twenty2.VomitLib.Tools
{
    public static class EncryptKit
    {
        public static string SimpleEncrypt(string str)
        {
            str = Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
            char[] array = str.ToCharArray();
            Array.Reverse(array);
            return new String(array);
        }
        
        public static string SimpleDecrypt(string str)
        {
            char[] array = str.ToCharArray();
            Array.Reverse(array);
            var bytes = Convert.FromBase64String(new string(array));
            return Encoding.UTF8.GetString(bytes);
        }
    }
}