using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataSharedLibrary;
using Flurl;
using Flurl.Http;
using GetTruyen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickEPUB;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GetTruyen
{
    public class TruyenContent
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public string? ImageBase64 { get; set; }
        public string? BookName { get; set; }
        public string? Author { get; set; }
        public string? URL { get; set; }
        public List<string?>? Content { get; set; }

        public TruyenContent()
        {
            this.Content = new List<string?>();
        }
    }

    public class AppConfig
    {
        public string? HostWeb { get; set; }
        public string? HostAPI { get; set; }

        public string? UrlGetListChapter { get; set; }
        public string? outputPath { get; set; }
        public string? epubOutputPath { get; set; }
        public string? pathBrowser { get; set; }
        public string? logPath { get; set; }
        public bool? isHeadless { get; set; }
        public bool? isFull { get; set; }
        public string? browserDevice { get; set; }
        public int delayTime { get; set; } = 200;
        public string? usrGet { get; set; }
        public string? pwdGet { get; set; }
        public int minContentLength { get; set; }
        public int maxThread { get; set; }
        public int maxTrialGet { get; set; }
        public float timeOut { get; set; }
    }

    public class LogWithConsole
    {
        private string _logPath { get; set; }

        public LogWithConsole(string logpath)
        {
            this._logPath = logpath;
        }

        public async Task WriteLog(string? msg)
        {
            await Utils.WriteLogWithConsole(this._logPath, msg);
        }

    }


    public class GetTruyen
    {
        protected IPlaywright? _playwright;
        protected IBrowser _browser;
        protected IBrowserContext _browserContext;
        protected IAPIRequestContext _apiContext;


        protected PageGotoOptions _pageGotoOption;

        protected bool isLogin = false;

        public AppConfig _config;
        protected string _config_path = "appconf.json";

        protected LogWithConsole _log { get; set; }


        public GetTruyen()
        {

            string conf = System.IO.File.ReadAllText(_config_path);
            if (string.IsNullOrEmpty(conf))
            {
                var config = new AppConfig();
                File.WriteAllText(_config_path, System.Text.Json.JsonSerializer.Serialize(config));
                _config = config;
            }
            else
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(conf);
                _config = config;
            }

            Utils.CreateFolderIfNotExist(_config?.outputPath);
            Utils.CreateFolderIfNotExist(_config?.epubOutputPath);
            Utils.CreateFolderIfNotExist(_config?.logPath);


            var logFileName = $"{_config?.logPath ?? "."}/{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.log";
            _log = new LogWithConsole(logFileName);

            _pageGotoOption = new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = _config?.timeOut ?? 3000 };

        }

        public async Task<bool?> InitBrowser()
        {
            Utils.ConsoleUTF8();
            await _log.WriteLog("Loading...");

            // Khởi tạo Playwright và trình duyệt Chromium
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = _config.pathBrowser,
                Headless = _config.isHeadless // Chạy chế độ headless
            });
            _apiContext = await _playwright.APIRequest.NewContextAsync();


            // Define mobile emulation settings (e.g., for an iPhone 11)
            var device = _playwright.Devices[_config.browserDevice ?? "iPhone 11"];


            // Create a new browser context with mobile settings
            _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = device.ViewportSize,
                UserAgent = device.UserAgent,
                HasTouch = true, // Enable touch support
                IsMobile = true // Emulate mobile device
            });

            Console.Clear();

            return true;
        }


        private async Task<IPage> NewPage(string url)
        {
            // Open a new page in mobile emulation
            var page = await _browserContext.NewPageAsync();
            try
            {
                await page.GotoAsync(url, options: _pageGotoOption);
                await Task.Delay(_config.delayTime);
            }
            catch (Exception)
            {
            }
            return page;
        }


        private async Task LoginFirst()
        {
            var page = await NewPage($"{_config.HostWeb}");

            await Login(page, null);

            await page.CloseAsync();
        }


        private async Task Login(IPage? page, string? returnUrl, int trialCount = 0)
        {
            try
            {

                if (!isLogin)
                {
                    await page.GotoAsync("https://docfull.vn/login/", _pageGotoOption);
                    isLogin = true;
                }

                if (page.Url == "https://docfull.vn/login/")
                {
                    var username = page.Locator("xpath=//html/body/div/div/div/div[1]/form/div[1]/input");
                    await username.FillAsync(_config.usrGet ?? "");

                    var pwd = page.Locator("xpath=//html/body/div/div/div/div[1]/form/div[2]/input");
                    await pwd.FillAsync(_config.pwdGet ?? "");

                    var loginbtn = page.Locator("xpath=//html/body/div/div/div/div[1]/form/button");
                    await loginbtn.ClickAsync();

                    //// Extract cookies
                    //var cookies = await _browserContext.CookiesAsync();
                    //string cookiesJson = JsonSerializer.Serialize(cookies);

                    //// Save cookiesJson to a file or use it as needed
                    //System.IO.File.WriteAllText("cookies.json", cookiesJson);

                    await Task.Delay(1000);
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        await page.GotoAsync(returnUrl ?? "", _pageGotoOption);
                    }
                }

            }
            catch (Exception)
            {
                await this.Login(page, returnUrl, trialCount += 1);
            }
        }

        public async Task<List<NovelContent>?> GetNovelsFromJson(string input)
        {
            var json = JObject.Parse(input);

            var results = json?["data"]?["list"]?
            .Where(p => (bool?)p?["isFull"] ?? false == true)
            .Select(p => new NovelContent()
            {
                BookId = (string?)p["id"],
                BookName = (string?)p["name"],
                Slug = (string?)p?["slug"],
                Author = (string?)p?["maker"],
                Tags = p?["categories"]?.Select(r => (string?)r?["name"])?.ToList(),
                Description = (string?)p?["description"],
                ShortDesc = (string?)p?["shortDescription"],
                ImageBase64 = (string?)p?["image"],
                MaxChapterCount = (int?)p?["chapters"]?["chapterNumber"]
            })?.ToList();


            return results;
        }


        public async Task GetChapter(NovelContent novel, int trial = 0)
        {
            try
            {
                //Get chapters
                var urlAllChapter = $"{_config.HostAPI}/chapters/{novel.BookId}?page=1&limit=99999&orderBy=chapterNumber&order=1";
                var req = await _apiContext.GetAsync(urlAllChapter);

                if (!req.Ok)
                {
                    return;
                }

                var jsonAllChapter = JObject.Parse(await req.TextAsync());

                var allChapter = jsonAllChapter?["data"]?["list"]?
                .Select(x =>
                new ChapterContent()
                {
                    ChapterId = (string?)x?["_id"],
                    Title = (string?)x?["name"],
                    IndexChapter = (int?)x?["chapterNumber"],
                    Slug = (string?)x?["chapterString"],
                    BookId = novel.BookId,
                });


                novel.Chapters = allChapter?.OrderBy(x => x.IndexChapter).ToList();
                novel.MaxChapterCount = novel?.MaxChapterCount ?? novel?.Chapters?.Count;
            }
            catch (Exception ex)
            {
                await _log.WriteLog(ex.Message);
                if (trial <= _config.maxTrialGet)
                {
                    await _log.WriteLog("Retry");
                    await GetChapter(novel, trial++);
                }

            }

        }



        public async Task GetFullNovelByCat(string? cateId)
        {

            string host = "https://api.docfull.vn/api/v1";
            string web = $"{host}/novels/categoies/{cateId}?page=1&limit=9999999";


            var response = await _apiContext.GetAsync(web);

            if (!response.Ok)
            {
                return;
            }

            var results = await GetNovelsFromJson(await response.TextAsync());


            var jsonOut = JsonSerializer.Serialize(results);

            string fileName = $"{_config.outputPath}//list-novel.json";

            await File.WriteAllTextAsync(fileName, jsonOut);


        }


        public async Task<List<NovelContent>?> GetFullNovel()
        {
            await _log.WriteLog("Getting novels...");

            string web = $"{_config.HostAPI}{_config.UrlGetListChapter}";


            var response = await _apiContext.GetAsync(web);

            if (!response.Ok)
            {
                return null;
            }

            var results = await GetNovelsFromJson(await response.TextAsync());


            var jsonOut = JsonSerializer.Serialize(results);

            string fileName = $"{_config.outputPath}//list-novel.json";

            await File.WriteAllTextAsync(fileName, jsonOut);
            await _log.WriteLog("Get novels completed");
            return results;
        }


        public async Task GetContentDetail(ChapterContent chap, string? bookSlug, int? maxChap = 0, int trial = 0, string? reTitle = "")
        {
            try
            {
                var link = $"{_config.HostWeb}/{bookSlug}/{chap.Slug}";
                var tab = await NewPage(link);

                await Login(tab, link);

                var content = await tab.InnerHTMLAsync("//html/body/div/div/div[2]/div[1]/div[4]/div[2]/div/div");


                await tab.CloseAsync();


                chap.ChapterDetailContents = await AppDbContext.GenerateChapterContent(content, chap.BookId, chap.ChapterId);

                await _log.WriteLog($"[{reTitle}][{bookSlug}][Trial {trial}] Completed chapter {chap.IndexChapter}/{maxChap} - {chap.Slug} - ({chap?.ChapterDetailContents?.Count} contents)");
            }
            catch (Exception)
            {
                await _log.WriteLog($"[{reTitle}][{bookSlug}] Failed chapter {chap.IndexChapter} - {chap.Slug}");

                if (trial <= _config.maxTrialGet)
                {
                    await GetContentDetail(chap, bookSlug, maxChap, trial++, reTitle);
                }

            }
        }



        public async Task GetContentByList(int trial = 0)
        {
            try
            {

                var filePath = $"{_config.outputPath}//list-novel.json";

                var lstNovel = new List<NovelContent>();

                try
                {
                    await _log.WriteLog($"Loading novels...");
                    lstNovel = JsonSerializer.Deserialize<List<NovelContent>>(await File.ReadAllTextAsync(filePath));
                    await _log.WriteLog($"Loaded novels.");
                }
                catch (Exception)
                {
                    await _log.WriteLog("Not found file list-novel.json. Get again!");
                    lstNovel = await GetFullNovel();
                    await _log.WriteLog($"Loaded novels.");
                }


                if (lstNovel == null)
                {
                    return;
                }


                await LoginFirst();

                foreach (var novel in lstNovel)
                {
                    if (novel == null)
                    {
                        continue;
                    }

                    string reTitle = $"{lstNovel?.IndexOf(novel) + 1}/{lstNovel?.Count}";
                    var fileName = $"{_config?.outputPath}\\{novel?.Slug}.novel";


                    if (File.Exists(fileName))
                    {
                        await _log.WriteLog($"[{reTitle}] Already exist {fileName} - Skipped");
                        continue;
                    }
                    await _log.WriteLog($"[{reTitle}] Get novel: {novel?.BookName} - {novel?.MaxChapterCount} chapters");

                    await GetChapter(novel);


                    novel.ImageBase64 = await Utils.DownloadImageAsBase64(novel?.ImageBase64);


                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _config?.maxThread ?? 5
                    };

                    await Parallel.ForEachAsync(novel?.Chapters, parallelOptions, async (chapter, cancellationToken) =>
                    {
                        await GetContentDetail(chapter, novel?.Slug, maxChap: novel?.MaxChapterCount, reTitle: reTitle);
                    });

                    if (novel?.Chapters?.Any(x => x?.ChapterDetailContents?.Count() == 0) ?? true)
                    {
                        await _log.WriteLog($"[{reTitle}] Some chapter is missing - Skip save");
                        continue;
                    }

                    var json = JsonSerializer.Serialize(novel);

                    var compress = Utils.GZipCompressText(json);

                    await File.WriteAllTextAsync(fileName, compress);

                    await _log.WriteLog(($"[{reTitle}] {novel?.BookName} save completed"));
                }


            }
            catch (Exception ex)
            {
                await _log.WriteLog(ex.Message);
                if (trial <= _config.maxTrialGet)
                {
                    await _log.WriteLog("Restarted");
                    await GetContentByList(trial++);
                }
            }
        }




    }
}