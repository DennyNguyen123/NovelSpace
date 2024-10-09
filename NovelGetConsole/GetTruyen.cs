using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataSharedLibrary;
using EpubSharp;
using Flurl;
using GetTruyen;
using Microsoft.Playwright;

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
        public string? outputPath { get; set; }
        public string? epubOutputPath { get; set; }
        public string? pathBrowser { get; set; }
        public string? logPath { get; set; }
        public bool? isHeadless { get; set; }
        public string? browserDevice { get; set; }
        public int delayTime { get; set; } = 200;
        public string? usrGet { get; set; }
        public string? pwdGet { get; set; }
        public int minContentLength { get; set; }
        public int maxThread { get; set; }
        public int maxTrialGet { get; set; }
        public float timeOut { get; set; }
    }

    public class GetTruyen
    {
        protected IPlaywright? _playwright;
        protected IBrowser _browser;
        protected IBrowserContext _browserContext;

        protected bool isLogin = false;

        public AppConfig _config;
        protected string _config_path = "appconf.json";
        protected PageGotoOptions _pageGotoOption;


        public GetTruyen()
        {
            string conf = System.IO.File.ReadAllText(_config_path);
            if (string.IsNullOrEmpty(conf))
            {
                var config = new AppConfig();
                File.WriteAllText(_config_path, JsonSerializer.Serialize(config));
                _config = config;
            }
            else
            {
                var config = JsonSerializer.Deserialize<AppConfig>(conf);
                _config = config;
            }

            Utils.CreateFolderIfNotExist(_config?.outputPath);
            Utils.CreateFolderIfNotExist(_config?.epubOutputPath);
            Utils.CreateFolderIfNotExist(_config?.logPath);

            _pageGotoOption = new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = _config?.timeOut ?? 3000 };

        }

        public async Task<bool?> FirstLoad()
        {
            // Khởi tạo Playwright và trình duyệt Chromium
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = _config.pathBrowser,
                Headless = _config.isHeadless // Chạy chế độ headless
            });



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

            //// Read cookies from file
            //string cookiesJson = System.IO.File.ReadAllText("cookies.json");
            //var cookies = JsonSerializer.Deserialize<List<Microsoft.Playwright.Cookie>>(cookiesJson);

            //if (_browserContext != null)
            //{
            //    await _browserContext.AddCookiesAsync(cookies);
            //}

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


        private async Task Login(IPage page, string? returnUrl, int trialCount = 0)
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

        public async Task<TruyenContent> GetContent(IPage page, int trynum = 0)
        {
            var rs = new TruyenContent();
            try
            {

                var title_div = page.Locator("xpath=//html/body/div/div/div[2]/div[1]/div[4]/h2");
                //var content_div = page.Locator("xpath=//html/body/div/div/div[2]/div[1]/div[4]/div[2]/div/div");


                var content = await page.EvaluateAsync<string>(@"() => {
                let element = document.evaluate(
                    '//html/body/div/div/div[2]/div[1]/div[4]/div[2]/div/div',  // Thay 'your_xpath' bằng XPath của bạn
                    document,
                    null,
                    XPathResult.FIRST_ORDERED_NODE_TYPE,
                    null
                ).singleNodeValue;
    
                if (element) {
                    let htmlContent = element.innerHTML;
        
                    // Thay thế tất cả <br>, </br>, <p> bằng \n
                    htmlContent = htmlContent
                                    .replace(/<br\s*\/?>/gi, '\n')
                                    .replace(/<\/br\s*>/gi, '\n')
                                    .replace(/<\/?p\s*>/gi, '\n')
                                    .replace(/<\/p\s*>/gi, '\n');
        
                    return htmlContent;
                }
                return null;
            }");


                var lstContent = content?.Split("\n")?.ToList();

                lstContent?.RemoveRange(0, 10);
                lstContent = lstContent?.Where(x => !string.IsNullOrEmpty(x))?.ToList();
                lstContent?.ForEach(x => x = x.Replace("<p>", "").Replace("</p>", ""));

                if (lstContent != null)
                {
                    rs.Content?.AddRange(lstContent);
                }

                rs.URL = page.Url;
                rs.Title = await title_div.TextContentAsync();

            }
            catch (Exception)
            {

            }

            //goto retry;


            //retry:
            //    {
            //        try
            //        {
            //            if (rs.Content?.Count <= _config.minContentLength & trynum < _config.maxTrialGet)
            //            {
            //                await page.ReloadAsync(new PageReloadOptions() { WaitUntil = _pageGotoOption.WaitUntil, Timeout = _pageGotoOption.Timeout });
            //                await Task.Delay(1000);
            //                await GetContent(page, trynum += 1);
            //            }
            //        }
            //        catch (Exception)
            //        {
            //            //Dung 30s khi bi chan
            //            await Task.Delay(30000);
            //            Console.WriteLine("Delay 30s");
            //            goto retry;
            //        }

            //    }

            return rs;
        }


        public async Task<TruyenContent> GetChapterContent(string url)
        {
            var page = await NewPage(url);

            await Login(page, url);
            var chap = await GetContent(page);
            await page.CloseAsync();

            return chap;
        }



        public async Task<(int lastChapter, string? bookName, string? Author, string? ImageBase64)>
            GetLastChapter(string url, int trialcount = 0)
        {
            int lastChap;
            string? imageBase64 = null;

            var page = await NewPage(url);

            await Login(page, url);


            var bookNameDiv = page.Locator("xpath=//html/body/div/div/div/div/div[1]/div[3]/div[2]");
            var bookName = await bookNameDiv.TextContentAsync();

            var authorNameDiv = page.Locator("xpath=//html/body/div/div/div/div/div[1]/div[3]/div[3]");
            var authorName = await authorNameDiv.TextContentAsync();

            var imageUrlDiv = page.Locator("xpath=/html/body/div/div/div/div/div[1]/div[2]/img");
            var imageUrl = await imageUrlDiv.GetAttributeAsync("src");

            if (!string.IsNullOrEmpty(imageUrl))
            {
                imageBase64 = await Utils.DownloadImageAsBase64(imageUrl);
            }


            var lastChapterDiv = page.Locator("xpath=//html/body/div/div/div/div/div[2]/div[1]/div/div/div/div/div[1]/div[2]/div[1]");
            await lastChapterDiv.ClickAsync();
            await Task.Delay(1000);


            var lastChapStr = page.Url.Split("-")?.ToList().LastOrDefault();


            Int32.TryParse(lastChapStr?.Replace("/", ""), out lastChap);

            await page.CloseAsync();

            if (
                lastChap == 0
                || string.IsNullOrEmpty(bookName)
                || string.IsNullOrEmpty(authorName)
                || string.IsNullOrEmpty(imageBase64)

                )
            {
                if (trialcount < _config.maxTrialGet)
                {
                    await GetLastChapter(url, trialcount += 1);
                }
            }


            return (lastChap, bookName, authorName, imageBase64);
        }


        public async Task<string?> SaveJsonToFile(object? rs, string url)
        {
            if (rs != null)
            {
                var jsonfilename = $"{_config.outputPath}\\{url.Split("/").LastOrDefault()}.json";
                var json = JsonSerializer.Serialize(rs);
                await System.IO.File.WriteAllTextAsync(jsonfilename, json);
                return jsonfilename;
            }
            return null;

        }

        public async Task AddNew(string url, List<TruyenContent>? rs, string fileLog, string filename, (int lastChapter, string? bookName, string? Author, string? ImageBase64) info, int fromchap = 1, bool isCustomize = false)
        {
            await Utils.WriteLogWithConsole(fileLog, $"[{filename}] : {info.lastChapter} chapters");

            var tasks = new List<Task>();
            int maxDegreeOfParallelism = _config.maxThread;  // Giới hạn chỉ chạy tối đa 10 tác vụ đồng thời
            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);


            int tochap = info.lastChapter;
            if (isCustomize)
            {
                try
                {
                    Console.Write("From chap (Default 1): ");
                    string? input_fromchap = Console.ReadLine();
                    fromchap = Int32.Parse((string.IsNullOrEmpty(input_fromchap) || Utils.IsNotNumber(input_fromchap)) ? fromchap.ToString() : input_fromchap);

                    Console.Write($"To chap (Default {tochap}): ");
                    string? input_tochap = Console.ReadLine();
                    tochap = Int32.Parse((string.IsNullOrEmpty(input_tochap) || Utils.IsNotNumber(input_tochap)) ? input_tochap?.ToString() : input_tochap);

                }
                catch (Exception)
                {
                }
            }

            for (int i = fromchap; i <= tochap; i++)
            {
                int localI = i;
                if (i > 0)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var chapterUrl = $"{url}/chuong-{localI}";
                            //Console.WriteLine(chapterUrl);
                            var chap = await this.GetChapterContent(chapterUrl);
                            chap.Id = localI;
                            chap.Author = info.Author;
                            chap.ImageBase64 = info.ImageBase64;
                            chap.BookName = info.bookName;
                            rs?.Add(chap);

                            if (chap?.Content?.Count <= _config.minContentLength)
                            {
                                await Utils.WriteLogWithConsole(fileLog, $"[{filename}] : Missing content chap {localI} ({chapterUrl})");
                            }
                            else
                            {
                                await Utils.WriteLogWithConsole(fileLog, $"[{filename}] : Done chap {localI}");
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                    }));



                }
            }

            await Task.WhenAll(tasks);

            if (rs?.Count > 0)
            {
                string jsonfilename = await this.SaveJsonToFile(rs?.OrderBy(x => x.Id), url);


                if (rs.Any(x => x.Content?.Count <= _config.minContentLength || string.IsNullOrWhiteSpace(x.Title) || string.IsNullOrEmpty(x.URL)))
                {
                    await FixMissChap(url, rs: rs);

                }
                else
                {
                    await ConvertToHtml($"{filename}.json");
                    await Utils.WriteLogWithConsole(fileLog, $"[{filename}] :Done");
                }
            }
        }


        public async Task Get(string url, bool isCustomize)
        {
            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            string filename = $"{url.Split("/").LastOrDefault()}";
            string jsonfile = $"{_config.outputPath}\\{filename}.json";
            string fileLog = $"{_config.logPath}\\{filename}.log";


            var isExistJson = File.Exists(jsonfile);
            var rs = new List<TruyenContent>();
            var info = await GetLastChapter(url);

            if (isExistJson)
            {
                (bool isValid, rs, var listmis) = await CheckJson(jsonfile, filename);
                if (!isValid)
                {
                    int trialcount = -1;
                    await this.FixMissChap(url, trialcount += 1, rs);
                }
                else
                {
                    var maxId = rs?.Max(x => x.Id);
                    //Get new chapter
                    if (maxId < info.lastChapter)
                    {
                        await AddNew(url, rs, fileLog, filename, info, maxId ?? 1, isCustomize);
                    }
                }
            }
            else
            {
                await AddNew(url, rs, fileLog, filename, info, 1, isCustomize);
            }

        }


        public async Task ConvertToHtml(string jsonfile)
        {
            string content = "";
            var json = await System.IO.File.ReadAllTextAsync($"{_config.outputPath}\\{jsonfile}");
            Console.WriteLine("Read file done.");
            var listChapter = JsonSerializer.Deserialize<List<TruyenContent>>(json);

            Console.WriteLine("Json to model done.");


            if (listChapter != null)
            {
                foreach (var item in listChapter)
                {
                    item?.Content?.ForEach(x => x?.Replace("/n", "<br>"));
                    content += @$"<h2 style=""color:red"">{item?.Title}</h2><br><br><br>{string.Join("<br><br>", item.Content)}<br><br><br>";
                }
            }

            content = $@"<html><head></head><body>{content}</body>";

            string htmlfilename = $"{_config.outputPath}\\{jsonfile.Split(".").FirstOrDefault()}.html";
            await System.IO.File.WriteAllTextAsync(htmlfilename, content);

            Console.WriteLine("Convert done.");

        }


        public async Task<(bool isValidChapter, List<TruyenContent>? listChapter, List<int?>? listMissingId)> CheckJson(string jsonfile, string fileName, List<TruyenContent>? rs = null)
        {
            string action = "Check missing chapter";
            //if (rs?.Count == null || rs?.Count == 0)
            //{
            //    var json = await System.IO.File.ReadAllTextAsync(jsonfile);
            //    Console.WriteLine($"[{action}][{fileName}] Read file done.");
            //    rs = JsonSerializer.Deserialize<List<TruyenContent>>(json);

            //    Console.WriteLine($"[{action}][{fileName}] Json to model done.");
            //}

            rs = await Utils.GetModelFromJsonFile(jsonfile, rs, action);


            if (rs != null)
            {
                var lstchapfail = rs.Where(x => x.Content?.Count <= _config.minContentLength || string.IsNullOrWhiteSpace(x.Title) || string.IsNullOrEmpty(x.URL))?.Select(x => x.Id)?.ToList();
                if (lstchapfail?.Count > 0)
                {

                    Console.Write($"[{action}][{fileName}] Failed : ");
                    if (lstchapfail != null)
                    {
                        Console.Write($"[{string.Join(", ", lstchapfail)}]");
                    }

                    Console.WriteLine();
                    return (false, rs, lstchapfail);

                }
                else
                {
                    Console.WriteLine($"[{action}][{fileName}] Passed");
                    return (true, rs, lstchapfail);
                }

            }
            else
            {
                Console.WriteLine($"[{action}][{fileName}] Not found file");
                return (false, rs, null);
            }

        }



        public async Task<(bool isValid, List<TruyenContent>? listChapter, List<int?>? listMissingId)> CheckMissingInfo(string jsonfile, string fileName, List<TruyenContent>? rs = null)
        {
            string action = "Check missing info";
            //if (rs?.Count == null || rs?.Count == 0)
            //{
            //    var json = await System.IO.File.ReadAllTextAsync(jsonfile);
            //    Console.WriteLine($"[{action}][{fileName}] Read file done.");
            //    rs = JsonSerializer.Deserialize<List<TruyenContent>>(json);

            //    Console.WriteLine($"[{action}][{fileName}] Json to model done.");
            //}

            rs = await Utils.GetModelFromJsonFile(jsonfile, rs, action);

            if (rs != null)
            {
                var lstchapfail = rs.Where(x => string.IsNullOrEmpty(x.BookName) || string.IsNullOrEmpty(x.Author) || string.IsNullOrEmpty(x.ImageBase64))?.Select(x => x.Id)?.ToList();
                if (lstchapfail?.Count > 0)
                {

                    Console.Write($"[{action}][{fileName}] Missing Author/Images chapter: [{string.Join(", ", lstchapfail)}]");

                    Console.WriteLine();

                    return (false, rs, lstchapfail);

                }
                else
                {
                    Console.WriteLine($"[{action}][{fileName}] Passed");
                    return (true, rs, null);
                }
            }
            else
            {
                Console.WriteLine($"[{action}][{fileName}] Not found file");
                return (false, rs, null);
            }

        }


        public async Task<(string url, string filename, string jsonfile, string fileLog)> GetFilePath(string url, string exten = "")
        {

            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }


            string filename = $"{url.Split("/").LastOrDefault()}";
            string jsonfile = $"{_config.outputPath}\\{filename}.json";
            string fileLog = $"{_config.logPath}\\{filename}{exten}.log";

            return (url, filename, jsonfile, fileLog);

        }

        public async Task<List<TruyenContent>?> FixMissChap(string url, int trialcount = 0, List<TruyenContent>? rs = null)
        {
            (url, string filename, string jsonfile, string fileLog) = await GetFilePath(url);

            (bool isValid, rs, List<int?>? lstErrorId) = await this.CheckJson(jsonfile, filename, rs);

            if (!isValid)
            {

                await Utils.WriteLogWithConsole(fileLog, $"Fix missing chap [{filename}]" + (trialcount > 0 ? $"(thử lại lần {trialcount})" : ""));

                var tasks = new List<Task>();
                int maxDegreeOfParallelism = _config.maxThread;  // Giới hạn chỉ chạy tối đa 10 tác vụ đồng thời
                using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

                //Fix error
                if (lstErrorId != null)
                {
                    await Login(await this.NewPage(url), null);

                    foreach (var i in lstErrorId)
                    {
                        int localI = i ?? 0;
                        if (i > 0)
                        {
                            await semaphore.WaitAsync();
                            tasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    var chapterUrl = $"{url}/chuong-{localI}";
                                    //Console.WriteLine(chapterUrl);
                                    var chap = await this.GetChapterContent(chapterUrl);
                                    chap.Id = localI;

                                    if (chap?.Content?.Count <= _config.minContentLength)
                                    {
                                        await Utils.WriteLogWithConsole(fileLog, $"[{filename}] : Missing content chap {localI} ({chapterUrl})");
                                    }
                                    else
                                    {
                                        var wrongChap = rs?.Where(x => x.Id == localI).FirstOrDefault();
                                        if (wrongChap != null & chap?.Content.Count > 0)
                                        {
                                            wrongChap.Content = new List<string?>();
                                            wrongChap.Content?.AddRange(chap.Content);
                                        }

                                        //Console.WriteLine($"Done chap {localI}");
                                        await Utils.WriteLogWithConsole(fileLog, $"[{filename}] : Done chap {localI}");
                                    }
                                }
                                finally
                                {
                                    semaphore.Release();
                                }

                            }));



                        }
                    }

                    await Task.WhenAll(tasks);

                    if (rs != null & rs?.Count > 0)
                    {
                        await this.SaveJsonToFile(rs?.OrderBy(x => x.Id), url);


                        if (rs.Any(x => x.Content?.Count <= 1))
                        {
                            var lstchapfail = rs.Where(x => x.Content?.Count <= 1);
                            await Utils.WriteLogWithConsole(fileLog, $"[{filename}] :Missing Chapter [{string.Join(", ", lstchapfail.Select(x => x.Id))}]");
                            //Recall when error
                            if (trialcount < _config.maxTrialGet)
                            {
                                await FixMissChap(url, trialcount += 1, rs);
                            }

                        }
                        else
                        {
                            await Utils.WriteLogWithConsole(fileLog, $"[{filename}] :Done");
                            await ConvertToHtml($"{filename}.json");
                        }
                    }
                }

            }
            return rs;

        }

        public async Task<List<TruyenContent>?> FixMissingInfo(string url, List<TruyenContent>? rs = null)
        {
            (url, string filename, string jsonfile, string fileLog) = await GetFilePath(url, ".fix");

            (bool isValid, rs, List<int?>? lstErrorId) = await this.CheckMissingInfo(jsonfile, filename, rs);

            if (!isValid)
            {
                var info = await GetLastChapter(url);

                await Utils.WriteLogWithConsole(filename, "Fix missing info...");

                foreach (var item in rs)
                {
                    item.BookName = info.bookName;
                    item.Author = info.Author;
                    item.ImageBase64 = info.ImageBase64;
                }

                await SaveJsonToFile(rs, filename);

                await Utils.WriteLogWithConsole(filename, "Fix successfully");

            }

            return rs;

        }


        public async Task<List<TruyenContent>?> CheckAndFix(string url)
        {
            var rs = await this.FixMissingInfo(url);
            rs = await this.FixMissChap(url, rs: rs);
            return rs;
        }

        public async Task ConvertToEpub(string url, bool isCheck = true)
        {

            (url, string filename, string jsonfile, string fileLog) = await GetFilePath(url, ".cepub");

            List<TruyenContent>? rs = null;

            if (isCheck)
            {
                rs = await CheckAndFix(url);
            }
            else
            {
                rs = await Utils.GetModelFromJsonFile<List<TruyenContent>>(jsonfile);
            }

            if (rs?.Count > 0)
            {
                await Utils.WriteLogWithConsole(fileLog, "Convert to epub...");
                var firstChap = rs.FirstOrDefault();
                string? epubName = $"{firstChap?.BookName} - {firstChap?.Author}";
                string epubFileName = $"{_config.epubOutputPath}\\{filename}.epub";

                EpubWriter writer = new EpubWriter();
                writer.SetCover(Convert.FromBase64String(firstChap?.ImageBase64), ImageFormat.Jpeg);
                writer.SetTitle(epubName);

                foreach (var item in rs)
                {
                    item?.Content?.ForEach(x => x?.Replace("/n", "<br>"));
                    string html = @$"<h2 style=""color:red"">{item?.Title}</h2><br><br><br>{string.Join("<br><br>", item.Content)}<br><br><br>";
                    writer.AddChapter(item?.Title, html);
                }


                writer.Write(epubFileName);
                await Utils.WriteLogWithConsole(fileLog, "Convert to epub done.");
            }
        }


        public async Task ConvertToSqlite(string url, bool isCheck = true)
        {
            AppDbContext dbContext = null;
            try
            {
                (url, string filename, string jsonfile, string fileLog) = await GetFilePath(url, ".cepub");
                dbContext = new AppDbContext($"D:\\Truyen\\SQLite\\{filename}.db", new Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>());
                await dbContext.Database.EnsureCreatedAsync();

                List<TruyenContent>? rs = null;

                if (isCheck)
                {
                    rs = await CheckAndFix(url);
                }
                else
                {
                    rs = await Utils.GetModelFromJsonFile<List<TruyenContent>>(jsonfile);
                }

                var novelContent = new NovelContent();
                var bookId = Guid.NewGuid().ToString();
                var fRs = rs?.FirstOrDefault();
                novelContent.Author = fRs?.Author;
                novelContent.BookName = fRs?.BookName;
                novelContent.BookId = bookId;
                novelContent.MaxChapterCount = rs?.Count;
                novelContent.URL = fRs?.URL;
                novelContent.ImageBase64 = fRs?.ImageBase64;


                var lstChapterDetailContent = new List<ChapterDetailContent>();
                var lstChapterContent = new List<ChapterContent>();
                rs?.ForEach(x =>
                {
                    var chapter = new ChapterContent();
                    var chapterId = Guid.NewGuid().ToString();
                    chapter.Title = x.Title;
                    chapter.ChapterId = chapterId;
                    chapter.BookId = bookId;
                    chapter.IndexChapter = rs.IndexOf(x);

                    lstChapterContent?.Add(chapter);

                    x?.Content?.ForEach(r =>
                    {
                        var index = x?.Content.IndexOf(r);
                        var content = new ChapterDetailContent();
                        content.ChapterId = chapterId;
                        content.BookId = bookId;
                        content.Index = index;
                        content.Content = r;
                        content.Id = Guid.NewGuid().ToString();

                        lstChapterDetailContent.Add(content);
                    });
                });

                await dbContext.NovelContents.AddAsync(novelContent);
                await dbContext.ChapterContents.AddRangeAsync(lstChapterContent);
                await dbContext.ChapterDetailContents.AddRangeAsync(lstChapterDetailContent);
                await dbContext.SaveChangesAsync();

                Console.WriteLine("Done");
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                dbContext?.Dispose();
            }
        }

    }
}