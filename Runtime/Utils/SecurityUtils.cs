using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LiteQuark.Runtime
{
    public static class SecurityUtils
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;

        /// <summary>
        /// 加密字符串
        /// </summary>
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var keyBytes = DeriveKey(key);
                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();

                // Write IV first
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch (Exception ex)
            {
                LLog.Error($"Encryption failed: {ex.Message}");
                return plainText;
            }
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                var keyBytes = DeriveKey(key);
                var fullCipher = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Read IV
                var iv = new byte[aes.BlockSize / 8];
                var cipherBytes = new byte[fullCipher.Length - iv.Length];

                Array.Copy(fullCipher, iv, iv.Length);
                Array.Copy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipherBytes);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                LLog.Error($"Decryption failed: {ex.Message}");
                return cipherText;
            }
        }

        /// <summary>
        /// 加密字节数组
        /// </summary>
        public static byte[] EncryptBytes(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
                return data;

            try
            {
                var keyBytes = DeriveKey(key);
                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();

                // Write IV first
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(data, 0, data.Length);
                }

                return msEncrypt.ToArray();
            }
            catch (Exception ex)
            {
                LLog.Error($"Encryption failed: {ex.Message}");
                return data;
            }
        }

        /// <summary>
        /// 解密字节数组
        /// </summary>
        public static byte[] DecryptBytes(byte[] encryptedData, string key)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return encryptedData;

            try
            {
                var keyBytes = DeriveKey(key);
                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Read IV
                var iv = new byte[aes.BlockSize / 8];
                var cipherBytes = new byte[encryptedData.Length - iv.Length];

                Array.Copy(encryptedData, iv, iv.Length);
                Array.Copy(encryptedData, iv.Length, cipherBytes, 0, cipherBytes.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipherBytes);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var msOutput = new MemoryStream();

                csDecrypt.CopyTo(msOutput);
                return msOutput.ToArray();
            }
            catch (Exception ex)
            {
                LLog.Error($"Decryption failed: {ex.Message}");
                return encryptedData;
            }
        }

        /// <summary>
        /// 从密钥字符串派生固定长度的密钥字节
        /// </summary>
        private static byte[] DeriveKey(string key)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
}