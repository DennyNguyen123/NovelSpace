using System;
using System.ComponentModel.Design;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataSharedLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using NovelGetConsole;

namespace GetTruyen
{
    public class Program
    {

        static void ProcessArguments(string[] args)
        {
            // Kiểm tra nếu không có tham số nào được truyền vào
            if (args.Length == 0)
            {
                Console.WriteLine("Không có tham số nào được truyền vào.");
                return;
            }

            // Duyệt qua tất cả các tham số
            foreach (var arg in args)
            {
                Console.WriteLine($"Tham số: {arg}");
            }

            // Ví dụ: Xử lý tham số theo kiểu key-value
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    string key = args[i].TrimStart('-');
                    string value = (i + 1 < args?.Length && !args[i + 1].StartsWith("--")) ? args[i + 1] : null;

                    if (value != null)
                    {

                        if (key == "help")
                        {
                            Console.WriteLine("help");
                        }

                        //Console.WriteLine($"Key: {key}, Value: {value}");
                        //i++; // Tăng chỉ số để bỏ qua giá trị đã xử lý
                    }
                    else
                    {
                        Console.WriteLine($"Key: {key} không có giá trị.");
                    }
                }
            }
        }


        public static async Task Main(string[] args)
        {

            var getTruyen = new GetTruyen();
            var isLoad = await getTruyen.InitBrowser();

            if (isLoad ?? false == true)
            {
                await getTruyen.GetContentByList();
            }

            //ProcessArguments(args);

            Console.WriteLine("Done");


            Console.ReadKey();

        }
    }
}