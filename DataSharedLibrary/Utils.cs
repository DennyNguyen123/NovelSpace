using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

            return default (T);
        }
    }


    public class Utils
    {
        

    }
}
