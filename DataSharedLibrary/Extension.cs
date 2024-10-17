using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataSharedLibrary
{
    public static class Extension
    {

        public static async Task ForeachMultiThread<T>(this IEnumerable<T> list, Action<T> action, int maxThread = 5)
        {
            using var semaphore = new SemaphoreSlim(maxThread);
            var tasks = new List<Task>();
            var lockObject = new object(); // Để đảm bảo thứ tự in ra

            foreach (var item in list)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Sử dụng lock để đảm bảo thứ tự khi in ra kết quả
                        lock (lockObject)
                        {
                            action(item);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        public static T? Clone<T>(this T input)
        {
            if (input == null)
            {
                return default(T);
            }
            try
            {
                var json = JsonSerializer.Serialize(input);
                return JsonSerializer.Deserialize<T>(json);

            }
            catch (Exception)
            {

            }

            return default(T);
        }


        public static bool Like(this string input, string pattern)
        {
            // Thay thế ký tự '%' bằng ký tự đại diện
            string regexPattern = "^" + pattern.Replace("%", ".*").Replace("_", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern);
        }


        public static bool IsHtml(this string? input)
        {
            Regex htmlRegex = new Regex("<.*?>");
            return htmlRegex.IsMatch(input);
        }

    }

}
