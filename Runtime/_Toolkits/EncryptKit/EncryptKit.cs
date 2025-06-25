using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Twenty2.VomitLib.Tools
{
    public static class EncryptKit
    {
        /// <summary>
        /// 用于流程验证, 生产环境禁止使用
        /// </summary>
        public static string SimpleEncrypt(string str)
        {
            str = Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
            char[] array = str.ToCharArray();
            Array.Reverse(array);
            return new String(array);
        }

        /// <summary>
        /// 用于流程验证, 生产环境禁止使用
        /// </summary>
        public static string SimpleDecrypt(string str)
        {
            char[] array = str.ToCharArray();
            Array.Reverse(array);
            var bytes = Convert.FromBase64String(new string(array));
            return Encoding.UTF8.GetString(bytes);
        }

        #region AES加密

        /// <summary>
        /// AES加密
        /// </summary>
        public static string AESEncrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(key))
                throw new ArgumentException("明文和密钥不能为空");

            using (Aes aes = Aes.Create())
            {
                // 使用SHA256确保密钥长度正确
                using (var sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                }
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // 先写入IV
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// AES解密
        /// </summary>
        public static string AESDecrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(key))
                throw new ArgumentException("密文和密钥不能为空");

            try
            {
                byte[] fullData = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    using (var sha256 = SHA256.Create())
                    {
                        aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                    }

                    // 提取IV (前16字节)
                    byte[] iv = new byte[16];
                    Array.Copy(fullData, 0, iv, 0, 16);
                    aes.IV = iv;

                    // 提取加密数据
                    byte[] encryptedData = new byte[fullData.Length - 16];
                    Array.Copy(fullData, 16, encryptedData, 0, encryptedData.Length);

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"解密失败，可能是密钥错误或数据损坏: {ex.Message}");
            }
        }

        /// <summary>
        /// AES加密 - byte数组版本，性能更好，适合大数据加密
        /// </summary>
        public static byte[] AESEncrypt(byte[] plainBytes, string key)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                throw new ArgumentException("明文数据不能为空或长度为0");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("密钥不能为空");

            using (Aes aes = Aes.Create())
            {
                // 使用SHA256确保密钥长度正确
                using (var sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                }
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // 先写入IV
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// AES解密 - byte数组版本，对应上面的加密方法
        /// </summary>
        public static byte[] AESDecrypt(byte[] cipherBytes, string key)
        {
            if (cipherBytes == null || cipherBytes.Length <= 16) // 至少要有IV的16字节
                throw new ArgumentException("密文数据无效或长度不足");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("密钥不能为空");

            try
            {
                using (Aes aes = Aes.Create())
                {
                    using (var sha256 = SHA256.Create())
                    {
                        aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                    }

                    // 提取IV (前16字节)
                    byte[] iv = new byte[16];
                    Array.Copy(cipherBytes, 0, iv, 0, 16);
                    aes.IV = iv;

                    // 提取加密数据
                    byte[] encryptedData = new byte[cipherBytes.Length - 16];
                    Array.Copy(cipherBytes, 16, encryptedData, 0, encryptedData.Length);

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var result = new MemoryStream())
                    {
                        cs.CopyTo(result);
                        return result.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"解密失败，可能是密钥错误或数据损坏: {ex.Message}");
            }
        }

        /// <summary>
        /// AES加密 - byte数组密钥版本，最高性能
        /// </summary>
        public static byte[] AESEncrypt(byte[] plainBytes, byte[] keyBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                throw new ArgumentException("明文数据不能为空或长度为0");

            if (keyBytes == null || keyBytes.Length == 0)
                throw new ArgumentException("密钥数据不能为空或长度为0");

            using (Aes aes = Aes.Create())
            {
                // 使用SHA256确保密钥长度正确
                using (var sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(keyBytes);
                }
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // 先写入IV
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// AES解密 - byte数组密钥版本，最高性能
        /// </summary>
        public static byte[] AESDecrypt(byte[] cipherBytes, byte[] keyBytes)
        {
            if (cipherBytes == null || cipherBytes.Length <= 16)
                throw new ArgumentException("密文数据无效或长度不足");

            if (keyBytes == null || keyBytes.Length == 0)
                throw new ArgumentException("密钥数据不能为空或长度为0");

            try
            {
                using (Aes aes = Aes.Create())
                {
                    using (var sha256 = SHA256.Create())
                    {
                        aes.Key = sha256.ComputeHash(keyBytes);
                    }

                    // 提取IV (前16字节)
                    byte[] iv = new byte[16];
                    Array.Copy(cipherBytes, 0, iv, 0, 16);
                    aes.IV = iv;

                    // 提取加密数据
                    byte[] encryptedData = new byte[cipherBytes.Length - 16];
                    Array.Copy(cipherBytes, 16, encryptedData, 0, encryptedData.Length);

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var result = new MemoryStream())
                    {
                        cs.CopyTo(result);
                        return result.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"解密失败，可能是密钥错误或数据损坏: {ex.Message}");
            }
        }

        #endregion


#if UNITY_EDITOR
        #region 测试用例
        /// <summary>
        /// EncryptKit测试类 - 验证加密解密功能是否正确
        /// </summary>
        public static class EncryptKitTest
        {
            public static void TestAllEncryption()
            {
                Debug.Log("=== EncryptKit 完整测试开始 ===");

                TestAESStringEncryption();
                TestAESBytesEncryption();

                Debug.Log("=== EncryptKit 完整测试结束 ===");
            }

            /// <summary>
            /// 测试AES字符串加密解密
            /// </summary>
            public static void TestAESStringEncryption()
            {
                try
                {
                    Debug.Log("=== AES字符串加密测试开始 ===");

                    string[] testTexts = {
                    "Hello World",
                    "这是中文测试",
                    "Special chars: !@#$%^&*()_+-={}[]|\\:;\"'<>?,./",
                    "🎮🎯🚀💻" // Emoji测试
                };

                    string testKey = "test_key_123";
                    int successCount = 0;

                    foreach (string testText in testTexts)
                    {
                        try
                        {
                            string encrypted = EncryptKit.AESEncrypt(testText, testKey);
                            string decrypted = EncryptKit.AESDecrypt(encrypted, testKey);

                            if (testText == decrypted)
                            {
                                successCount++;
                                Debug.Log($"✅ 字符串加密测试通过: {testText.Substring(0, Math.Min(20, testText.Length))}...");
                            }
                            else
                            {
                                Debug.LogError($"❌ 字符串加密测试失败: {testText}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"❌ 字符串加密异常: {ex.Message}");
                        }
                    }

                    Debug.Log($"字符串加密测试结果: {successCount}/{testTexts.Length}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"字符串加密测试过程异常: {ex}");
                }
            }

            /// <summary>
            /// 测试AES byte数组加密解密
            /// </summary>
            public static void TestAESBytesEncryption()
            {
                try
                {
                    Debug.Log("=== AES byte数组加密测试开始 ===");

                    // 测试数据
                    byte[][] testDataArray = {
                    Encoding.UTF8.GetBytes("Hello World"),
                    Encoding.UTF8.GetBytes("这是中文测试"),
                    new byte[] { 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD }, // 二进制数据
                    new byte[1024], // 大数据块
                    new byte[] { 0x00 }, // 单字节
                    GenerateRandomBytes(4096) // 随机大数据
                };

                    // 填充大数据块
                    for (int i = 0; i < testDataArray[3].Length; i++)
                    {
                        testDataArray[3][i] = (byte)(i % 256);
                    }

                    string stringKey = "test_key_bytes";
                    byte[] byteKey = Encoding.UTF8.GetBytes("byte_key_test_123");

                    int successCount = 0;
                    int totalTests = 0;

                    foreach (byte[] testData in testDataArray)
                    {
                        // 测试string密钥版本
                        totalTests++;
                        try
                        {
                            byte[] encrypted = EncryptKit.AESEncrypt(testData, stringKey);
                            byte[] decrypted = EncryptKit.AESDecrypt(encrypted, stringKey);

                            if (ByteArraysEqual(testData, decrypted))
                            {
                                successCount++;
                                Debug.Log($"✅ byte数组(string密钥)加密测试通过 - 数据长度: {testData.Length}");
                            }
                            else
                            {
                                Debug.LogError($"❌ byte数组(string密钥)加密测试失败 - 数据长度: {testData.Length}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"❌ byte数组(string密钥)加密异常: {ex.Message}");
                        }

                        // 测试byte密钥版本
                        totalTests++;
                        try
                        {
                            byte[] encrypted = EncryptKit.AESEncrypt(testData, byteKey);
                            byte[] decrypted = EncryptKit.AESDecrypt(encrypted, byteKey);

                            if (ByteArraysEqual(testData, decrypted))
                            {
                                successCount++;
                                Debug.Log($"✅ byte数组(byte密钥)加密测试通过 - 数据长度: {testData.Length}");
                            }
                            else
                            {
                                Debug.LogError($"❌ byte数组(byte密钥)加密测试失败 - 数据长度: {testData.Length}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"❌ byte数组(byte密钥)加密异常: {ex.Message}");
                        }
                    }

                    Debug.Log($"byte数组加密测试结果: {successCount}/{totalTests}");

                    // 测试性能对比
                    TestPerformanceComparison();

                    // 测试错误情况
                    TestBytesErrorCases();
                    
                    // 测试最小数据加密
                    TestMinimalDataEncryption();

                }
                catch (Exception ex)
                {
                    Debug.LogError($"byte数组加密测试过程异常: {ex}");
                }
            }

            /// <summary>
            /// 性能对比测试
            /// </summary>
            private static void TestPerformanceComparison()
            {
                Debug.Log("=== 性能对比测试 ===");

                try
                {
                    byte[] testData = GenerateRandomBytes(10240); // 10KB数据
                    string stringKey = "performance_test_key";
                    byte[] byteKey = Encoding.UTF8.GetBytes(stringKey);

                    // 测试string版本性能
                    var startTime = System.DateTime.Now;
                    for (int i = 0; i < 100; i++)
                    {
                        byte[] encrypted = EncryptKit.AESEncrypt(testData, stringKey);
                        byte[] decrypted = EncryptKit.AESDecrypt(encrypted, stringKey);
                    }
                    var stringTime = System.DateTime.Now - startTime;

                    // 测试byte版本性能
                    startTime = System.DateTime.Now;
                    for (int i = 0; i < 100; i++)
                    {
                        byte[] encrypted = EncryptKit.AESEncrypt(testData, byteKey);
                        byte[] decrypted = EncryptKit.AESDecrypt(encrypted, byteKey);
                    }
                    var byteTime = System.DateTime.Now - startTime;

                    Debug.Log($"性能对比 (100次10KB加解密):");
                    Debug.Log($"String密钥版本: {stringTime.TotalMilliseconds:F2}ms");
                    Debug.Log($"Byte密钥版本: {byteTime.TotalMilliseconds:F2}ms");
                    Debug.Log($"性能提升: {(stringTime.TotalMilliseconds / byteTime.TotalMilliseconds):F2}x");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"性能测试异常: {ex.Message}");
                }
            }

            /// <summary>
            /// 测试错误情况
            /// </summary>
            private static void TestBytesErrorCases()
            {
                Debug.Log("=== byte数组错误情况测试 ===");

                try
                {
                    // 测试空数据
                    try
                    {
                        EncryptKit.AESEncrypt("", "key");
                        Debug.LogError("❌ 空数据应该抛出异常");
                    }
                    catch (ArgumentException)
                    {
                        Debug.Log("✅ 空数据正确抛出异常");
                    }

                    // 测试空密钥
                    try
                    {
                        EncryptKit.AESEncrypt(new byte[] { 1, 2, 3 }, "");
                        Debug.LogError("❌ 空密钥应该抛出异常");
                    }
                    catch (ArgumentException)
                    {
                        Debug.Log("✅ 空密钥正确抛出异常");
                    }

                    // 测试无效密文
                    try
                    {
                        EncryptKit.AESDecrypt(new byte[] { 1, 2, 3 }, "key"); // 长度不足16
                        Debug.LogError("❌ 无效密文应该抛出异常");
                    }
                    catch (ArgumentException)
                    {
                        Debug.Log("✅ 无效密文正确抛出异常");
                    }

                    // 测试错误密钥
                    try
                    {
                        byte[] testData = Encoding.UTF8.GetBytes("test");
                        byte[] encrypted = EncryptKit.AESEncrypt(testData, "correct_key");
                        EncryptKit.AESDecrypt(encrypted, "wrong_key");
                        Debug.LogError("❌ 错误密钥应该抛出异常");
                    }
                    catch (ArgumentException)
                    {
                        Debug.Log("✅ 错误密钥正确抛出异常");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"错误情况测试异常: {ex.Message}");
                }
            }

            /// <summary>
            /// 比较两个byte数组是否相等
            /// </summary>
            private static bool ByteArraysEqual(byte[] a, byte[] b)
            {
                if (a == null && b == null) return true;
                if (a == null || b == null) return false;
                if (a.Length != b.Length) return false;

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i]) return false;
                }
                return true;
            }

            /// <summary>
            /// 生成随机byte数组
            /// </summary>
            private static byte[] GenerateRandomBytes(int length)
            {
                byte[] bytes = new byte[length];
                var random = new System.Random();
                random.NextBytes(bytes);
                return bytes;
            }

            /// <summary>
            /// 测试最小数据加密 - 验证为什么小数据加密后会变大
            /// </summary>
            private static void TestMinimalDataEncryption()
            {
                Debug.Log("=== 最小数据加密测试 ===");
                
                try
                {
                    // 测试各种小数据
                    byte[][] smallDataArray = {
                        new byte[] { 1 },                    // 1字节
                        new byte[] { 1, 2 },                 // 2字节
                        new byte[] { 1, 2, 3, 4, 5 },        // 5字节
                        new byte[15],                         // 15字节 (刚好小于块大小)
                        new byte[16],                         // 16字节 (正好一个块)
                        new byte[17]                          // 17字节 (超过一个块)
                    };
                    
                    string key = "test_key";
                    
                    foreach (byte[] data in smallDataArray)
                    {
                        try
                        {
                            byte[] encrypted = EncryptKit.AESEncrypt(data, key);
                            byte[] decrypted = EncryptKit.AESDecrypt(encrypted, key);
                            
                            Debug.Log($"原始数据: {data.Length}字节 -> 加密后: {encrypted.Length}字节");
                            Debug.Log($"  IV部分: 16字节, 加密数据部分: {encrypted.Length - 16}字节");
                            
                            // 验证解密正确性
                            if (ByteArraysEqual(data, decrypted))
                            {
                                Debug.Log($"  ✅ 解密成功");
                            }
                            else
                            {
                                Debug.LogError($"  ❌ 解密失败");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"处理{data.Length}字节数据时出错: {ex.Message}");
                        }
                        
                        Debug.Log("---");
                    }
                    
                    // 解释为什么长度不足16字节无法解密
                    Debug.Log("🔍 为什么长度不足16字节无法解密？");
                    Debug.Log("因为前16字节是IV，如果总长度≤16，就没有实际的加密数据了！");
                    Debug.Log("任何有效的AES密文至少需要：16字节IV + 16字节加密数据 = 32字节");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"最小数据加密测试异常: {ex.Message}");
                }
            }
        }
        #endregion
#endif
    }
}