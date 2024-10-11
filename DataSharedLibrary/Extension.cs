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
