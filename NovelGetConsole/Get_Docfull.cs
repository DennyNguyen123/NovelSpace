using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;
using DataSharedLibrary;
using Flurl;
using Flurl.Http;
using GetTruyen;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickEPUB;
using RestSharp;
using RestSharp.Authenticators;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Get_DocFull
{
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

    /// <summary>
    /// A helper class to deserialize the JSON response from the token endpoint.
    /// The JsonPropertyName attribute maps the JSON key "access_token" to the C# property "AccessToken".
    /// </summary>
    public class TokenResponse
    {
        public bool status { get; set; }

        public string? statusCode { get; set; }
        public string? message { get; set; }

        public TokenResponseDetail data { get; set; }
    }
    public class TokenResponseDetail
    {
        public string? token { get; set; }
        public string? refreshToken { get; set; }
    }


    public class Get_DocFull
    {
        protected IPlaywright? _playwright = null;
        protected IBrowser? _browser = null;
        protected IBrowserContext? _browserContext = null;
        protected IAPIRequestContext? _apiContext = null;


        protected PageGotoOptions _pageGotoOption;

        protected bool isLogin = false;

        public AppConfig? _config = null;
        protected string _config_path = "appconf.json";

        protected LogWithConsole _log { get; set; }

        protected string? _token = null;


        public Get_DocFull()
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

                if (config == null)
                {
                    config = new AppConfig();
                }

                _config = config;
            }

            Utils.CreateFolderIfNotExist(_config?.outputPath);
            Utils.CreateFolderIfNotExist(_config?.epubOutputPath);
            Utils.CreateFolderIfNotExist(_config?.logPath);


            var logFileName = $"{_config?.logPath ?? "."}/{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.log";
            _log = new LogWithConsole(logFileName);

            _pageGotoOption = new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = _config?.timeOut ?? 3000 };

        }




        public List<NovelContent>? GetNovelsFromJson(string input)
        {
            var json = JObject.Parse(input);

            var results = json?["data"]?["list"]?
            //.Where(p => (bool?)p?["isFull"] ?? false == true)
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

        public async Task GetToken()
        {

            var loginmodel = new
            {
                email = _config?.usrGet,
                password = _config?.pwdGet
            }
            ;

            var rq = await MyHttpRequest(Method.Post, "/auth/customers/login", loginmodel, ContentType.Json, false);

            if (!rq.isSuccessful || string.IsNullOrEmpty(rq.Content))
            {
                await _log.WriteLog("Failed to get token");
                return;
            }
            else
            {
                var jsonContent = JObject.Parse(rq.Content!);

                _token = jsonContent?["data"]?["token"]?.ToString();
            }

        }

        // Assume you have a _config object, a _token variable,
        // and GetToken() method defined at the class level.
        // Also, assume you have a ContentType enum.
        // private YourConfigClass? _config;
        // private string? _token;
        // private static readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);
        // public enum ContentType { Json, Xml, FormUrlEncoded }

        public async Task<(bool isSuccessful, string? Content)> MyHttpRequest(
            Method method,
            string api,
            object? body,
            ContentType contentType,
            bool requiresAuth = true)
        {
            // --- Step 1: Handle Authentication ---
            if (requiresAuth && string.IsNullOrEmpty(_token))
            {
                await GetToken();
            }

            // --- Step 2: Setup RestClient and RestRequest ---
            var options = new RestClientOptions(_config?.HostAPI ?? "")
            {
                // Assign the authenticator if authentication is required.
                // This is the modern and recommended way to add the Bearer token in RestSharp.
                Authenticator = requiresAuth ? new JwtAuthenticator(_token!) : null
            };
            var client = new RestClient(options);
            var request = new RestRequest(api, method);

            // --- Step 3: Add Request Body if it exists ---
            if (body != null)
            {
                request.AddBody(body, contentType);
            }

            // --- Step 4: Execute Request and Handle Response ---
            try
            {
                // Execute the request asynchronously.
                var response = await client.ExecuteAsync(request);

                // Optional: Add logic to handle token expiration (e.g., HTTP 401 Unauthorized)
                // and retry the request once after getting a new token.

                return (response.IsSuccessful, response.Content);
            }
            catch (Exception ex)
            {
                // Catch and log any exceptions during the API call.
                Console.WriteLine($"An error occurred during the HTTP request: {ex.Message}");
                return (false, ex.Message); // Return the exception message for better debugging.
            }
        }





        public async Task GetChapter(NovelContent novel, int trial = 0)
        {
            try
            {
                //Get chapters
                var urlAllChapter = $"{_config?.HostAPI}/chapters/{novel.BookId}?page=1&limit=99999&orderBy=chapterNumber&order=1";

                var req = await MyHttpRequest(
                    method: Method.Get
                    , api: urlAllChapter
                    , body: null
                    , contentType: ContentType.Json
                    , requiresAuth: false
                );

                if (!req.isSuccessful)
                {
                    return;
                }

                var jsonAllChapter = JObject.Parse(req!.Content!);

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
                if (trial <= _config?.maxTrialGet)
                {
                    await _log.WriteLog("Retry");
                    await GetChapter(novel, trial += 1);
                }

            }

        }



        public async Task GetFullNovelByCat(string? cateId)
        {

            string web = $"{_config?.HostAPI}/novels/categoies/{cateId}?page=1&limit=9999999";

            if (_apiContext == null)
            {
                return;
            }

            var response = await _apiContext.GetAsync(web);

            if (!response.Ok)
            {
                return;
            }

            var results = GetNovelsFromJson(await response.TextAsync());


            var jsonOut = JsonSerializer.Serialize(results);

            string fileName = $"{_config?.outputPath}//list-novel.json";

            await File.WriteAllTextAsync(fileName, jsonOut);


        }


        public async Task<List<NovelContent>?> GetFullNovel()
        {
            await _log.WriteLog("Getting novels...");

            string web = $"{_config?.HostAPI}{_config?.UrlGetListChapter}";

            if (_apiContext == null)
            {
                return null;
            }

            var response = await _apiContext.GetAsync(web);

            if (!response.Ok)
            {
                return null;
            }

            var results = GetNovelsFromJson(await response.TextAsync());


            var jsonOut = JsonSerializer.Serialize(results);

            string fileName = $"{_config?.outputPath}//list-novel.json";

            await File.WriteAllTextAsync(fileName, jsonOut);
            await _log.WriteLog("Get novels completed");
            return results;
        }


        public async Task GetContentDetail(ChapterContent chap, string? bookSlug, int? maxChap = 0, int trial = 0, string? reTitle = "")
        {
            try
            {
                var link = $"{_config?.HostAPI}/chapters/get-chapter/{bookSlug}/{chap.IndexChapter}";

                //var content = await tab.InnerHTMLAsync("//html/body/div/div/div[2]/div[1]/div[4]/div[2]/div/div");
                var rs = await MyHttpRequest(
                    method: Method.Get,
                    api: link,
                    body: null,
                    contentType: ContentType.Json,
                    requiresAuth: true
                );

                if (rs.isSuccessful)
                {
                    var jsonContent = JObject.Parse(rs.Content!);
                    if (jsonContent?["data"]?["content"] == null)
                    {
                        await _log.WriteLog($"[{reTitle}][{bookSlug}] No content found for chapter {chap.IndexChapter} - {chap.Slug}");
                        return;
                    }
                    
                    var data_encrypted = jsonContent?["data"]?["content"]?.ToString()??"";

                    var content_html = ChapterDecryptor.DecryptChapter(data_encrypted);


                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(content_html);


                    HtmlNode chapterNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'chapter')]");

                    var content = chapterNode.InnerHtml;

                    chap.ChapterDetailContents = AppDbContext.GenerateChapterContent(content, chap.BookId, chap.ChapterId);

                    await _log.WriteLog($"[{reTitle}][{bookSlug}][Trial {trial}] Completed chapter {chap.IndexChapter}/{maxChap} - {chap.Slug} - ({chap?.ChapterDetailContents?.Count} contents)");
                }
            }
            catch (Exception)
            {
                await _log.WriteLog($"[{reTitle}][{bookSlug}] Failed chapter {chap.IndexChapter} - {chap.Slug}");

                if (trial <= _config?.maxTrialGet)
                {
                    await GetContentDetail(chap, bookSlug, maxChap, trial += 1, reTitle);
                }

            }
        }


        public async Task GetOneNovel(string url)
        {
            string[] parts = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // The desired part is the last element in the array.
            string id = parts.Last();

            var urlGetData = $"{_config?.HostAPI}/novels/{id}";

            var rs = MyHttpRequest(
                method: Method.Get,
                api: urlGetData,
                body: null,
                contentType: ContentType.Json,
                requiresAuth: true
            );

            if (!rs.Result.isSuccessful || string.IsNullOrEmpty(rs.Result.Content))
            {
                await _log.WriteLog("Failed to get novel data");
                return;
            }

            var jsonrs = JObject.Parse(rs.Result.Content);


            var novel = new NovelContent()
            {
                BookId = (string?)jsonrs?["data"]?["id"],
                BookName = (string?)jsonrs?["data"]?["name"],
                Slug = (string?)jsonrs?["data"]?["slug"],
                Author = (string?)jsonrs?["data"]?["maker"],
                Tags = jsonrs?["data"]?["categories"]?.Select(r => (string?)r?["name"])?.ToList(),
                Description = (string?)jsonrs?["data"]?["description"],
                ShortDesc = (string?)jsonrs?["data"]?["shortDescription"],
                ImageBase64 = (string?)jsonrs?["data"]?["image"],
                MaxChapterCount = (int?)jsonrs?["data"]?["chapters"]?["chapterNumber"]

            };

            await GetChapter(novel);


            novel.ImageBase64 = await Utils.DownloadImageAsBase64(novel.ImageBase64);


            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _config?.maxThread ?? 5
            };

            await Parallel.ForEachAsync(novel?.Chapters!, parallelOptions, async (chapter, cancellationToken) =>
            {
                await GetContentDetail(chapter, novel?.Slug, maxChap: novel?.MaxChapterCount);
                await Task.Delay(_config?.delayTime ?? 200);
            });

            if (novel?.Chapters?.Any(x => x?.ChapterDetailContents?.Count() == 0) ?? true)
            {
                await _log.WriteLog($"Some chapter is missing - Skip save");
                return;
            }

            var json = JsonSerializer.Serialize(novel);

            var compress = Utils.GZipCompressText(json);

            var path = $"{_config?.outputPath ?? "./"}/{novel.BookName ?? novel.Slug ?? urlGetData}.novel";
            await File.WriteAllTextAsync(path, compress);

            await _log.WriteLog(($"{novel?.BookName} save completed"));
        }

        public async Task GetContentByList(int trial = 0)
        {
            try
            {

                var filePath = $"{_config?.outputPath}//list-novel.json";

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

                    await GetChapter(novel!);


                    novel!.ImageBase64 = await Utils.DownloadImageAsBase64(novel?.ImageBase64);


                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _config?.maxThread ?? 5
                    };

                    await Parallel.ForEachAsync(novel?.Chapters!, parallelOptions, async (chapter, cancellationToken) =>
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
                if (trial <= _config?.maxTrialGet)
                {
                    await _log.WriteLog("Restarted");
                    await GetContentByList(trial += 1);
                }
            }
        }




    }
}