using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataSharedLibrary
{

    public static class Utils
    {
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


        public static async Task<string> DownloadImageAsBase64(string imageUrl)
        {
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
