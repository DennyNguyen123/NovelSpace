using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZstdNet;
using static System.Net.Mime.MediaTypeNames;

namespace DataSharedLibrary
{
    public enum CompressType
    {
        Zstd,
        GZip,
    }

    public static class Utils
    {

        // Hàm nén dữ liệu
        public static string CompressZstd(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text); // Chuyển đổi chuỗi thành byte array
            using (var compressor = new Compressor())
            {
                byte[] compressedData = compressor.Wrap(data); // Nén dữ liệu
                return Convert.ToBase64String(compressedData); // Chuyển đổi byte array thành chuỗi Base64
            }
        }

        public static string DecompressZstd(string compressedText)
        {
            byte[] compressedData = Convert.FromBase64String(compressedText); // Chuyển đổi chuỗi Base64 thành byte array
            using (var decompressor = new Decompressor())
            {
                byte[] decompressedData = decompressor.Unwrap(compressedData); // Giải nén dữ liệu
                return Encoding.UTF8.GetString(decompressedData); // Chuyển đổi byte array thành chuỗi
            }
        }

        public static string DecompressZstd(byte[] compressedData)
        {
            // Chuyển đổi chuỗi Base64 thành byte array
            using (var decompressor = new Decompressor())
            {
                byte[] decompressedData = decompressor.Unwrap(compressedData); // Giải nén dữ liệu
                return Encoding.UTF8.GetString(decompressedData); // Chuyển đổi byte array thành chuỗi
            }
        }


        public static T JsonFromCompress<T>(string filePath)
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


        public async static Task WriteLogWithConsole(FileStream? fileStream, string? msg)
        {
            string? patternMsg = $"{DateTime.Now} : {msg}";
            Console.WriteLine(patternMsg);
            WriteAtLast(fileStream, patternMsg);

        }


        public async static Task WriteLogWithConsole(string filename, string? msg)
        {
            string? patternMsg = $"{DateTime.Now} : {msg}";
            Console.WriteLine(patternMsg);
            WriteAtLast(filename, patternMsg);

        }
    }
}
