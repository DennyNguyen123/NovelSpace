using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuickEPUB;
using System;
using System.Data;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataSharedLibrary
{
    public class AppDbContext : DbContext
    {
        private readonly string _dbPath;

        public AppDbContext(string dbPath, DbContextOptions<AppDbContext> options) : base(options)
        {
            _dbPath = dbPath;

            this.Database.Migrate();
        }



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_dbPath}");
            }
        }

        public DbSet<NovelContent> NovelContents { get; set; }
        public DbSet<ChapterContent> ChapterContents { get; set; }
        public DbSet<ChapterDetailContent> ChapterDetailContents { get; set; }
        public DbSet<CurrentReader> CurrentReader { get; set; }


        public async Task<CurrentReader?> GetCurrentReader(string? bookId)
        {
            var cur = await this.CurrentReader.Where(x => x.BookId == bookId).FirstOrDefaultAsync();
            if (cur == null && !string.IsNullOrEmpty(bookId))
            {
                cur = new CurrentReader();
                cur.BookId = bookId;
                cur.CurrentChapter = 0;
                cur.CurrentLine = 0;
                cur.CurrentPosition = 0;
                this.CurrentReader.Add(cur);
                await this.SaveChangesAsync();
            }
            return cur;

        }


        public async Task<NovelContent?> GetNovel(string? bookId, bool isMergeAuthorName = true)
        {
            var novel = await this.NovelContents.AsNoTracking().Where(x => x.BookId == bookId).FirstOrDefaultAsync();

            if (novel != null)
            {
                novel.BookName = !isMergeAuthorName ? novel.BookName : $"{novel.BookName} - {novel.Author}";
                var lstChapter = await this.ChapterContents.AsNoTracking().Where(x => x.BookId == novel.BookId).OrderBy(x => x.IndexChapter).ToListAsync();
                if (lstChapter?.Count() > 0)
                {
                    lstChapter.ForEach(x =>
                    {
                        x.Content = new List<string?>();
                        if (isMergeAuthorName)
                        {
                            x.Title = $"[{x.IndexChapter + 1}/{novel.MaxChapterCount}] {x.Title}";
                        }
                    }
                    );
                    novel?.Chapters?.AddRange(lstChapter);
                }
            }

            return novel;
        }

        public async Task<ChapterContent?> GetContentChapter(ChapterContent chapter, string? bookName, CancellationToken cancellationToken = default)
        {
            //using var db = new AppDbContext(_dbPath, new DbContextOptions<AppDbContext>());


            cancellationToken.ThrowIfCancellationRequested();

            var content = await this.ChapterDetailContents.AsNoTracking()
                .Where(x =>
                !string.IsNullOrWhiteSpace(x.Content)
                & x.BookId == chapter.BookId
                & x.ChapterId == chapter.ChapterId)
                .OrderBy(x => x.Index)
                .Select(r => r.Content)
                .ToListAsync(cancellationToken);


            cancellationToken.ThrowIfCancellationRequested();

            var cleanedItems = content
            .Select(item => Utils.GetHtmlInnerText(item?.Replace("&nbsp;", "").Trim()?
            //Remove ()
            .Replace(bookName?.ReplaceRegex(@"\s*\(.*?\)", "") ?? "", "")
            .Replace(chapter.Title ?? "", ""))) // Remove &nbsp and trim spaces/tabs
            .Where(item => !string.IsNullOrWhiteSpace(item)) // Optional: remove empty or whitespace items
            .ToList();


            cancellationToken.ThrowIfCancellationRequested();

            chapter.Content = cleanedItems.Distinct().ToList();
            return chapter;

        }


        public static List<ChapterDetailContent> GenerateChapterContent(string? content, string? bookId, string? chapterId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rs = new List<ChapterDetailContent>();

            content = content?.Replace("<br>", "\r\n").Replace("<br/>", "\r\n").Replace("</p>", "\r\n").Replace("<p>", "");


            var lstBody = content?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (lstBody?.Count < 2)
            {
                var separatestring = new string[] { ". ", "? ", "! " };
                lstBody = content?.Split(separatestring, StringSplitOptions.RemoveEmptyEntries).ToList();
            }


            if (lstBody?.Count == 0)
            {
                return rs;
            }

            Parallel.ForEach(lstBody!, (content, token) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    token.Stop();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                var contentChapter = new ChapterDetailContent();
                contentChapter.BookId = bookId;
                contentChapter.ChapterId = chapterId;
                contentChapter.Id = Guid.NewGuid().ToString();
                contentChapter.Content = content;
                contentChapter.Index = lstBody?.IndexOf(content);
                rs.Add(contentChapter);
            });


            return rs.OrderBy(x => x.Index).ToList();
        }

        public async Task<(string? bookId, string? msg)> ImportBookByJsonModel(string filename, Action<double>? updateProgress = null, CancellationToken cancellationToken = default)
        {
            try
            {

                var novelContent = Utils.JsonFromCompress<NovelContent?>(filename);

                updateProgress?.Invoke(50);
                if (novelContent != null)
                {
                    var checkExist = await CheckExist(novelContent?.BookName);

                    if (checkExist.isExist)
                    {
                        return (checkExist.bookId, checkExist.msg);
                    }

                    await this.NovelContents.AddAsync(novelContent!, cancellationToken);
                    await this.SaveChangesAsync(cancellationToken);

                    updateProgress?.Invoke(99);
                    return (novelContent?.BookId, null);

                }
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
            return (null, null);
        }


        public async Task<(bool isExist, string? bookId, string? msg)> CheckExist(string? bookName)
        {
            //check exist
            var existBook = await this.NovelContents.Where(x => x.BookName == bookName).FirstOrDefaultAsync();

            if (existBook != null)
            {
                return (true, existBook.BookId, "Already exist - Skipped");
            }

            return (false, null, null);
        }


        public async Task<(string? bookId, string? msg)> ImportEpub(string filePath, Action<double>? updateProgress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var epub = VersOne.Epub.EpubReader.ReadBook(filePath);

                var bookName = epub.Title;
                var bookAuthors = epub.Author;


                var chapters = epub.Navigation;

                var checkExist = await CheckExist(bookName);

                if (checkExist.isExist)
                {
                    return (checkExist.bookId, checkExist.msg);
                }


                var novel = new NovelContent();


                novel.BookName = bookName;
                novel.Author = bookAuthors;
                novel.BookId = Guid.NewGuid().ToString();
                novel.Chapters = new List<ChapterContent>();

                if (epub?.CoverImage != null)
                {
                    novel.ImageBase64 = Convert.ToBase64String(epub.CoverImage);
                }

                if (chapters?.Count == 1 && chapters?.FirstOrDefault()?.NestedItems?.Count > 0)
                {
                    chapters = chapters.FirstOrDefault()?.NestedItems;
                }

                novel.MaxChapterCount = chapters?.Count;

                double maxChap = (double)(chapters?.Count() ?? 0);
                double chapExcute = 0;

                Parallel.ForEach(chapters!, (chapter, token) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        token.Stop();
                    }
                    var index = (double)chapters!.IndexOf(chapter);
                    var novelChapter = new ChapterContent();
                    var chapter_title = chapter.Title;

                    novelChapter.IndexChapter = (int)index;
                    novelChapter.ChapterId = Guid.NewGuid().ToString();
                    novelChapter.Title = chapter_title;
                    novelChapter.BookId = novel.BookId;
                    novelChapter.ChapterDetailContents = new List<ChapterDetailContent>();

                    var chapter_content = chapter.HtmlContentFile?.Content;

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(chapter_content);

                    var body = htmlDoc.DocumentNode.SelectSingleNode("//body").InnerHtml;

                    novelChapter.ChapterDetailContents = GenerateChapterContent(body, novelChapter.BookId, novelChapter.ChapterId);

                    novel.Chapters.Add(novelChapter);

                    var state = chapExcute += 1 / maxChap * 100;

                    updateProgress?.Invoke(state > 100 ? 0 : state);
                }
                );

                await this.AddAsync(novel, cancellationToken);
                await this.SaveChangesAsync(cancellationToken);
                return (novel?.BookId, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }

        }


        public async Task ExportToEpub(string epubFileName, string? bookId, Action<double>? updateProgress = null, CancellationToken cancellationToken = default)
        {
            var novel = await GetNovel(bookId, isMergeAuthorName: false);

            var doc = new Epub(novel?.BookName, novel?.Author);

            // Thêm ảnh bìa
            var stream = new MemoryStream(Convert.FromBase64String(novel?.ImageBase64 ?? ""));
            doc.AddResource("cover.jpge", EpubResourceType.JPEG, stream, true);


            if (novel?.Chapters?.Count == 0)
            {
                return;
            }
            var chapterCount = (double)(novel?.Chapters?.Count() ?? 0);
            double chapExcute = 0;

            await Parallel.ForEachAsync(novel?.Chapters!, cancellationToken, async (chap, token) =>
            {
                token.ThrowIfCancellationRequested();
                using var db = new AppDbContext(_dbPath, new DbContextOptions<AppDbContext>());
                var updatedChapter = await db.GetContentChapter(chap, novel?.BookName, token);
                var state = chapExcute += 1 / chapterCount * 100;
                updateProgress?.Invoke(state > 100 ? 0 : state);
            });

            chapExcute = 0;


            // Sử dụng vòng lặp foreach thay vì ForEach để hỗ trợ await
            foreach (var chap in novel?.Chapters ?? new List<ChapterContent>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                string content = "";
                if (chap?.Content?.Count > 0)
                {
                    content = string.Join("<br><br>", chap.Content);
                }

                string html = @$"<h2 style=""color:red"">{chap?.Title}</h2><br><br><br>{content}<br><br><br>";
                doc.AddSection(chap?.Title, html);

                var state = chapExcute += 1 / chapterCount * 100;
                updateProgress?.Invoke(state > 100 ? 0 : state);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Xuất file EPUB
            using var fs = new FileStream(epubFileName, FileMode.Create);
            doc.Export(fs);
        }



        public async Task<(bool isSuccess, string? msg)> DeleteNovel(string bookId, Action<double>? updateProgress = null)
        {
            try
            {
                await this.ChapterDetailContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync();
                updateProgress?.Invoke(20);
                await this.ChapterContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync();
                updateProgress?.Invoke(40);
                await this.NovelContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync();
                updateProgress?.Invoke(70);
                await this.SaveChangesAsync();
                updateProgress?.Invoke(90);
                // Run the VACUUM command
                this.Database.ExecuteSqlRaw("VACUUM");
                updateProgress?.Invoke(99);
                return (true, null);

            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

    }
}
