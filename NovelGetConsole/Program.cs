using System;
using System.ComponentModel.Design;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataSharedLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace GetTruyen
{
    public class Program
    {
        // Hàm tạm dừng để chờ người dùng nhấn phím trước khi quay lại menu
        static void Pause()
        {
            Console.WriteLine("Nhấn phím bất kỳ để tiếp tục...");
            Console.ReadKey();
        }

        public static (string url, bool isCheck) GetParam()
        {
            Console.Write("Nhập url: ");
            //string? url = Console.ReadLine()??"";
            string url = "https://docfull.vn/thien-anh-dich-full/";
            Console.Write("Check valid json? (Y,N) (Default N)");
            string? inIsCheck = Console.ReadLine();
            bool isCheck = inIsCheck == "Y" ? true : false;
            return (url, isCheck);
        }

        public static async Task MenuCommand()
        {
            bool isExit = false;
            Console.OutputEncoding = Encoding.UTF8;

            var getTruyen = new GetTruyen();
            Console.WriteLine("Loading...");
            var isLoad = await getTruyen.FirstLoad();

            if (isLoad ?? false == true)
            {
                Console.Clear();
                while (!isExit)
                {
                    // Hiển thị menu
                    Console.Clear();  // Xóa màn hình console để hiển thị menu mới
                    Console.WriteLine("===== MENU =====");
                    Console.WriteLine("1. Get truyện");
                    Console.WriteLine("2. Get list(list.json)");
                    Console.WriteLine("3. Fix missing");
                    Console.WriteLine("4. Convert json to html");
                    Console.WriteLine("5. Validation json file");
                    Console.WriteLine("6. Convert To Epub");
                    Console.WriteLine("7. Convert To Sqlite");
                    Console.WriteLine("e. Exit");
                    Console.WriteLine("0. Test");
                    Console.WriteLine("================");
                    Console.Write("Please choose (1-5): ");


                    // Đọc lựa chọn từ người dùng
                    string choice = Console.ReadLine();

                    // Xử lý lựa chọn
                    switch (choice)
                    {
                        case "0":
                            {
                                //await getTruyen.Test();
                                await getTruyen.FixAndImport();
                                Pause();
                                break;
                            }
                        case "1":
                            Console.Write("Nhập đường dẫn truyện cần tải: ");
                            await getTruyen.Get(Console.ReadLine() ?? "", true);
                            Pause();
                            break;
                        case "2":
                            var lstLinkStr = File.ReadAllText("list.json");
                            var lstLink = JsonSerializer.Deserialize<List<string>>(lstLinkStr);
                            if (lstLink?.Count > 0)
                            {
                                foreach (var item in lstLink)
                                {
                                    await getTruyen.Get(item, false);
                                }
                            }
                            Pause();
                            break;
                        case "3":
                            {
                                Console.Write("Nhập đường dẫn truyện cần fix: ");
                                //string url = Console.ReadLine() ?? "";
                                string url_3 = "https://docfull.vn/che-tao-sieu-huyen-huyen-dich-full/";
                                await getTruyen.CheckAndFix(url_3);
                                Pause();
                                break;
                            }
                        case "4":
                            Console.Write("Nhập đường file: ");
                            await getTruyen.ConvertToHtml(Console.ReadLine() ?? "");
                            Pause();
                            break;
                        case "5":
                            Console.Write("Nhập đường file: ");
                            string url = Console.ReadLine();
                            string filename = $"{url.Split("/").LastOrDefault()}";
                            string jsonfile = $"{getTruyen._config.outputPath}\\{filename}.json";
                            await getTruyen.CheckJson(url, filename);
                            Pause();
                            break;

                        case "6":
                            Console.Write("Nhập url: ");
                            string epubUrl = Console.ReadLine() ?? "";
                            Console.Write("Check valid json? (Y,N) (Default N)");
                            string? inIsCheck = Console.ReadLine();
                            bool isCheck = inIsCheck == "Y" ? true : false;

                            await getTruyen.ConvertToEpub(epubUrl, isCheck);
                            Pause();
                            break;
                        case "7":
                            Console.Write("Nhập url: ");
                            string sqliteUrl = Console.ReadLine() ?? "";
                            Console.Write("Check valid json? (Y,N) (Default N)");
                            string? inIsCheck1 = Console.ReadLine();
                            bool isCheck1 = inIsCheck1 == "Y" ? true : false;

                            await getTruyen.ConvertToSqlite(sqliteUrl, isCheck1);
                            Pause();
                            break;

                        case "8":
                            await getTruyen.ConvertToNovelFile(GetParam());
                            Pause();
                            break;
                        case "e":
                            Console.WriteLine("Thoát chương trình.");
                            isExit = true;  // Thoát vòng lặp và kết thúc chương trình
                            break;
                        default:
                            Console.WriteLine("Lựa chọn không hợp lệ, vui lòng thử lại.");
                            Pause();
                            break;
                    }
                }


            }

            //await getTruyen.CheckJson("thanh-khu-dich-full.json");
        }

        public static async Task Main(string[] args)
        {
            //await MenuCommand();

            var getTruyen = new GetTruyen();
            Console.WriteLine("Loading...");
            var isLoad = await getTruyen.FirstLoad();

            if (isLoad ?? false == true)
            {
                await getTruyen.FixAndImport();
            }

            Console.WriteLine("Done");


            Console.ReadKey();

        }
    }
}