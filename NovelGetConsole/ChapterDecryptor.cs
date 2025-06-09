// add reference System.Security.Cryptography to your project if it's not there
// System.Security.Cryptography should be available in .NET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public static class ChapterDecryptor
{
    // The hardcoded password from the JavaScript code
    private const string Password = "j4DSfugdAASCKEwAAAD8xGX0qEeHh-WJzRc11TBp&%#%$2";

    /// <summary>
    /// Decrypts a string that was encrypted using CryptoJS.AES.encrypt() with a password.
    /// This method replicates the OpenSSL salted format.
    /// </summary>
    /// <param name="encryptedText">The Base64 encoded encrypted string.</param>
    /// <returns>The decrypted string.</returns>
    public static string DecryptChapter(string encryptedText, string pwd = Password)
    {
        // 1. Decode from Base64 to get the bytes
        byte[] encryptedBytesWithSalt = Convert.FromBase64String(encryptedText);

        // 2. Extract the salt and the actual ciphertext
        // OpenSSL salted format: 8 bytes for "Salted__" header + 8 bytes for the salt
        byte[] salt = new byte[8];
        Array.Copy(encryptedBytesWithSalt, 8, salt, 0, 8);

        byte[] ciphertext = new byte[encryptedBytesWithSalt.Length - 16];
        Array.Copy(encryptedBytesWithSalt, 16, ciphertext, 0, ciphertext.Length);

        // 3. Derive the key and IV from the password and salt
        // This mimics OpenSSL's EVP_BytesToKey function.
        // For AES-256, we need a 32-byte key and a 16-byte IV.
        byte[] key = new byte[32];
        byte[] iv = new byte[16];
        DeriveKeyAndIV(Encoding.UTF8.GetBytes(pwd), salt, key, iv);

        // 4. Decrypt using AES
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;       // Standard for CryptoJS
            aes.Padding = PaddingMode.PKCS7; // Standard for CryptoJS

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

                // 5. Convert the result to a UTF-8 string
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }

    /// <summary>
    /// Implements OpenSSL's EVP_BytesToKey key derivation function.
    /// It uses MD5 hashing to derive a key and an IV from a password and salt.
    /// </summary>
    private static void DeriveKeyAndIV(byte[] password, byte[] salt, byte[] key, byte[] iv)
    {
        var concatenatedHashes = new List<byte>();
        byte[] currentHash = Array.Empty<byte>();

        using (var md5 = MD5.Create())
        {
            bool enoughBytes = false;
            while (!enoughBytes)
            {
                // Concatenate previous hash (if any), password, and salt
                var preHash = new byte[(currentHash.Length) + password.Length + (salt?.Length ?? 0)];

                Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
                if (salt != null)
                {
                    Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);
                }

                // Compute the new hash
                currentHash = md5.ComputeHash(preHash);
                concatenatedHashes.AddRange(currentHash);

                if (concatenatedHashes.Count >= key.Length + iv.Length)
                {
                    enoughBytes = true;
                }
            }
        }

        // Copy the derived bytes to the key and IV arrays
        Buffer.BlockCopy(concatenatedHashes.ToArray(), 0, key, 0, key.Length);
        Buffer.BlockCopy(concatenatedHashes.ToArray(), key.Length, iv, 0, iv.Length);
    }
}