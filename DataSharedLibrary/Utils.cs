using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ZstdNet;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataSharedLibrary
{

    public static class Utils
    {
        #region GZip

        public static string GZipCompressText(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            using (var compressedStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
                gzipStream.Close();
                return Convert.ToBase64String(compressedStream.ToArray());
            }
        }

        public static string GZipDecompressText(string compressedText)
        {
            byte[] compressedData = Convert.FromBase64String(compressedText);
            using (var compressedStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                return Encoding.UTF8.GetString(resultStream.ToArray());
            }
        }

        #endregion GZip

        public static T? JsonFromCompress<T>(string filePath)
        {
            try
            {
                var fileByte = File.ReadAllText(filePath);

                var text = GZipDecompressText(fileByte);

                return JsonSerializer.Deserialize<T?>(text);
            }
            catch (Exception)
            {
            }

            return default(T);
        }


        public static async Task<T?> JsonFromCompress<T>(string filePath, CancellationToken cancellationToken = default)
        {
            // Mở file và đọc nội dung Base64
            using FileStream fs = File.OpenRead(filePath);
            using StreamReader reader = new StreamReader(fs);
            string base64Data = await reader.ReadToEndAsync();

            // Giải mã Base64 trực tiếp vào mảng byte
            byte[] compressedData = Convert.FromBase64String(base64Data);

            // Dùng MemoryStream để chứa dữ liệu đã giải mã
            using MemoryStream compressedStream = new MemoryStream(compressedData);

            // Mở GZipStream để giải nén dữ liệu
            using GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);

            // Deserialize JSON từ stream giải nén
            return await JsonSerializer.DeserializeAsync<T>(gzipStream, cancellationToken: cancellationToken);
        }


        public async static Task CompressJsonAndSave(object? data, string filePath, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                return;
            }

            // Serialize đối tượng thành JSON thành một chuỗi
            var jsonString = JsonSerializer.Serialize(data);

            // Chuyển đổi chuỗi JSON thành mảng byte
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

            // Nén và mã hóa Base64 trực tiếp vào file
            using FileStream fs = File.Create(filePath);
            using (MemoryStream compressedStream = new MemoryStream())
            {
                // Nén dữ liệu vào MemoryStream
                using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, true))
                {
                    await gzipStream.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken);
                }

                // Đặt vị trí của MemoryStream về đầu để đọc lại dữ liệu đã nén
                compressedStream.Seek(0, SeekOrigin.Begin);

                // Chuyển đổi trực tiếp dữ liệu nén sang Base64 và ghi vào FileStream
                using StreamWriter writer = new StreamWriter(fs);
                string base64Data = Convert.ToBase64String(compressedStream.GetBuffer(), 0, (int)compressedStream.Length);
                await writer.WriteAsync(base64Data.AsMemory(), cancellationToken);
            }
        }



        //public async Task SaveJsonWithCompress(object? input, string filePath)
        //{
        //    using var fs = File.OpenWrite(filePath);
        //    // Serialize và ghi dữ liệu vào Stream
        //    return await JsonSerializer.Serialize(fs, input);

        //}





        public static void ClearRAM(bool forceFullCollection = false)
        {
            // Collect only Generation 0 (short-lived objects)
            GC.Collect(0);
            GC.WaitForPendingFinalizers();

            if (forceFullCollection)
            {
                // Optionally, collect Generation 2 (long-lived objects)
                GC.Collect(2);
                GC.WaitForPendingFinalizers();
                GC.Collect(0);
                GC.WaitForPendingFinalizers();
            }
        }

        public static bool IsNotNumber(string? input)
        {
            // Biểu thức chính quy để kiểm tra chuỗi chỉ chứa số (cả số nguyên và số thực)
            string pattern = @"^-?\d+(\.\d+)?$";

            // Kiểm tra nếu chuỗi không khớp với biểu thức chính quy
            return !Regex.IsMatch(input ?? "", pattern);
        }


        public static string? GetHtmlInnerText(string? html)
        {
            if (!html?.IsHtml() ?? false || string.IsNullOrWhiteSpace(html))
            {
                return html;
            }

            // Load HTML document
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);


            string innerText = doc.DocumentNode.InnerText;
            return innerText;
        }




        public static async Task<T?> GetModelFromJsonFile<T>(string jsonpath, T? input = null, string? action = null) where T : class?
        {
            try
            {


                if (input != null)
                {
                    return input;
                }

                if (File.Exists(jsonpath))
                {
                    var fileName = Path.GetFileName(jsonpath);
                    action = string.IsNullOrEmpty(action) ? null : $"[{action}]";

                    var json = await File.ReadAllTextAsync(jsonpath);
                    Console.WriteLine($"{action}[{fileName}] Read file done.");
                    var rs = JsonSerializer.Deserialize<T>(json);

                    Console.WriteLine($"{action}[{fileName}] Json to model done.");
                    return rs;

                }
                else
                {
                    Console.WriteLine($"{action} Not found file");
                }

            }
            catch (Exception)
            {

            }
            return default;
        }


        public static async Task<string?> DownloadImageAsBase64(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return default;
            }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Tải ảnh từ URL
                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                    // Chuyển đổi sang Base64
                    string base64String = Convert.ToBase64String(imageBytes);

                    return base64String;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tải ảnh: {ex.Message}");
                    return null;
                }
            }
        }

        public static bool CreateFolderIfNotExist(string? path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                // Kiểm tra thư mục có tồn tại hay không
                if (!Directory.Exists(path))
                {
                    // Nếu thư mục không tồn tại, tạo mới
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }


        public static FileStream? CreateFileIfNotExist(string? path)
        {
            try
            {
                FileStream? file = null;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
                // Kiểm tra thư mục có tồn tại hay không
                if (!File.Exists(path))
                {
                    // Nếu thư mục không tồn tại, tạo mới
                    file = File.Create(path);
                }

                file?.Close();
                return file;
            }
            catch (Exception)
            {
                return null;
            }

        }


        public static void ConsoleUTF8()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }

        public static void WriteAtLast(FileStream? fileStream, string msg)
        {
            byte[] info = Encoding.UTF8.GetBytes($"{msg}\n");
            fileStream?.Write(info, 0, info.Length);
        }

        public static void WriteAtLast(string filename, string msg)
        {
            CreateFileIfNotExist(filename);
            byte[] info = Encoding.UTF8.GetBytes($"{msg}\n");
            using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Write(info, 0, info.Length);
            }
        }


        public async static Task WriteLogWithConsole(string filename, string? msg)
        {
            string? patternMsg = $"{DateTime.Now} : {msg}";
            Console.WriteLine(patternMsg);
            WriteAtLast(filename, patternMsg);

        }
    }
}
