using System;
using System.Security.Cryptography;
using System.Text;

namespace LiteQuark.Runtime
{
    public static class SecurityUtils
    {
        private static byte[] GetAesKey(string key)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key)); // 固定 32 字节
        }

        public static byte[] AesEncrypt(byte[] plainBytes, string key)
        {
            if (plainBytes == null || plainBytes.Length == 0)
            {
                return plainBytes;
            }

            using var aes = Aes.Create();
            aes.Key = GetAesKey(key); // 32字节 key
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV(); // 随机 IV

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // 拼接 IV + CipherText
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return result;
        }

        public static byte[] AesDecrypt(byte[] cipherBytes, string key)
        {
            if (cipherBytes == null || cipherBytes.Length == 0)
            {
                return cipherBytes;
            }

            using var aes = Aes.Create();
            aes.Key = GetAesKey(key);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 取出 IV（前16字节）
            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[cipherBytes.Length - iv.Length];
            Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return plainBytes;
        }
    }
}