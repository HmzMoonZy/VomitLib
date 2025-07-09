using System;
using System.Security.Cryptography;
using System.Text;

namespace VomitLib.Net.Http
{
    /// <summary>
    /// HTTP签名生成工具类
    /// 实现与GeekServer完全一致的签名算法
    /// </summary>
    public static class HttpSignHelper
    {
        /// <summary>
        /// 生成HTTP签名
        /// </summary>
        /// <param name="httpCode">服务器配置的HttpCode</param>
        /// <param name="timestamp">时间戳</param>
        /// <returns>签名字符串</returns>
        public static string GenerateSign(string httpCode, string timestamp)
        {
            if (string.IsNullOrEmpty(httpCode))
                throw new ArgumentException("HttpCode不能为空", nameof(httpCode));
            
            if (string.IsNullOrEmpty(timestamp))
                throw new ArgumentException("Timestamp不能为空", nameof(timestamp));

            string originalString = httpCode + timestamp;
            return GetStringSign(originalString);
        }

        /// <summary>
        /// 实现与服务器一致的签名算法
        /// </summary>
        /// <param name="str">原始字符串</param>
        /// <returns>签名结果</returns>
        private static string GetStringSign(string str)
        {
            // 1. MD5加密
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] md5Bytes = MD5.Create().ComputeHash(data);
            string md5 = BitConverter.ToString(md5Bytes).Replace("-", "").ToLower();

            // 2. 计算校验码
            int checkCode1 = 0; // 字母ASCII码总和
            int checkCode2 = 0; // 数字ASCII码总和
            
            for (int i = 0; i < md5.Length; ++i)
            {
                if (md5[i] >= 'a')
                    checkCode1 += md5[i];
                else
                    checkCode2 += md5[i];
            }

            // 3. 组合最终签名
            return checkCode1 + md5 + checkCode2;
        }
    }
}