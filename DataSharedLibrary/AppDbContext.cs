using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuickEPUB;
using System;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Xml.Linq;
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


        private string? GetContentString(string? input
            , bool isReplaceBookInfo
            , (string? bookName, string? chapterTitle) bookInfo
            )
        {
            var output = Utils.GetHtmlInnerText(input)?.Replace("&nbsp;", "");

            if (isReplaceBookInfo)
            {
                output = output?.Replace(bookInfo.bookName?.ReplaceRegex(@"\s*\(.*?\)", "") ?? "", "");
                output = output?.Replace(bookInfo.chapterTitle ?? "", "");
            }

            return output;
        }

        public async Task GetContentChapter(ChapterContent chapter
            , string? bookName
            , List<ChapterDetailContent>? lstSource = null
            , bool isRemoveBookInfo = true
            , bool isAddLineToModel = false
            , bool isParseHtml = true
            , CancellationToken cancellationToken = default
        )
        {
            //using var db = new AppDbContext(_dbPath, new DbContextOptions<AppDbContext>());


            cancellationToken.ThrowIfCancellationRequested();

            if (lstSource == null)
            {
                lstSource = await this.ChapterDetailContents.AsNoTracking()
                .Where(x =>
                x.BookId == chapter.BookId
                & x.ChapterId == chapter.ChapterId).ToListAsync(cancellationToken);
            }

            var lstIndexRemove = new List<ChapterDetailContent?>();

            foreach (var item in lstSource)
            {
                if (item == null)
                {
                    return;
                }

                item.Content = item.Content?.Trim() ?? "";

                var valuehtmlparse = GetContentString(item?.Content, isRemoveBookInfo, (bookName, chapter.Title));
                if (isParseHtml)
                {
                    if (string.IsNullOrEmpty(valuehtmlparse))
                    {
                        lstIndexRemove.Add(item);
                    }
                    else
                    {
                        item!.Content = valuehtmlparse;
                    }
                }

            }

            lstIndexRemove.ForEach(x => lstSource?.Remove(x!));

            lstSource = lstSource?.Where(x => !string.IsNullOrWhiteSpace(x.Content)).ToList();

            if (isAddLineToModel)
            {
                chapter.ChapterDetailContents = lstSource;
            }
            else
            {
                var content = lstSource?
                    .Select(x => new { x.Index, x.Content })
                    .Distinct()
                    .OrderBy(x => x.Index);

                chapter.Content = content?.Select(x => x.Content).ToList();
            }


            //chapter.Content = content.Distinct().ToList();

        }


        public async Task<NovelContent?> GetFullChapterContent(string? bookId, bool isAddLineToModel, Action<double>? updateProgress, CancellationToken cancellationToken = default)
        {
            var novel = await this.GetNovel(bookId, false);
            double chapExecute = 0;
            double chapterCount = novel?.Chapters?.Count ?? 1;

            var allChapterContent = await this.ChapterDetailContents.AsNoTracking().Where(x => x.BookId == bookId).ToListAsync(cancellationToken);


            //// Sử dụng Parallel.ForEachAsync để chạy nhiều luồng
            await Parallel.ForEachAsync(novel?.Chapters!, cancellationToken, async (chap, token) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chapterContent = allChapterContent.Where(x => x.ChapterId == chap.ChapterId).ToList();

                await this.GetContentChapter(
                    chapter: chap
                    , novel?.BookName
                    , lstSource: chapterContent
                    , isAddLineToModel: isAddLineToModel
                    , cancellationToken: cancellationToken);

                // Tính toán tiến độ
                var state = chapExecute++ / chapterCount * 100;
                updateProgress?.Invoke(state > 100 ? 100 : state);
            });

            return novel;
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
                using var db = new AppDbContext(this._dbPath, new DbContextOptions<AppDbContext>());

                var novelContent = await Utils.JsonFromCompress<NovelContent?>(filename, cancellationToken);

                //var novelContent = Utils.JsonFromCompress<NovelContent?>(filename);

                updateProgress?.Invoke(50);
                if (novelContent != null)
                {
                    var checkExist = await db.CheckExist(novelContent?.BookName);

                    if (checkExist.isExist)
                    {
                        return (checkExist.bookId, checkExist.msg);
                    }

                    await db.NovelContents.AddAsync(novelContent!, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);

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
            //var novel = await GetNovel(bookId, isMergeAuthorName: false);
            var novel = await GetFullChapterContent(bookId, isAddLineToModel: false, updateProgress, cancellationToken);

            var doc = new Epub(novel?.BookName, novel?.Author);

            // Thêm ảnh bìa
            var stream = new MemoryStream(Convert.FromBase64String(novel?.ImageBase64 ?? ""));
            doc.AddResource("cover.jpge", EpubResourceType.JPEG, stream, true);

            var chapterCount = (double)(novel?.Chapters?.Count() ?? 0);
            double chapExcute = 0;




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


        public async Task ExportToModel(string savePath, string? bookId, Action<double>? updateProgress = null, CancellationToken cancellationToken = default)
        {
            //var novel = await GetNovel(bookId, isMergeAuthorName: false);

            var novel = await GetFullChapterContent(bookId, isAddLineToModel: true, updateProgress, cancellationToken);

            await Utils.CompressJsonAndSave(novel, savePath, cancellationToken);
        }



        public async Task<(bool isSuccess, string? msg)> DeleteNovel(string bookId, Action<double>? updateProgress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the novel exists
                //var novel = await this.NovelContents.FindAsync(bookId);
                //if (novel == null)
                //{
                //    return (false, "Novel with the provided BookId not found.");
                //}


                //// Delete ChapterDetailContents
                //var chapterDetails = await this.ChapterDetailContents.Where(x => x.BookId == bookId).ToListAsync();
                //if (chapterDetails.Any())
                //{
                //    this.ChapterDetailContents.RemoveRange(chapterDetails);
                //    updateProgress?.Invoke(20);
                //}

                //// Delete ChapterContents
                //var chapters = await this.ChapterContents.Where(x => x.BookId == bookId).ToListAsync();
                //if (chapters.Any())
                //{
                //    this.ChapterContents.RemoveRange(chapters);
                //    updateProgress?.Invoke(40);
                //}

                //// Delete the Novel
                //this.NovelContents.Remove(novel);
                //updateProgress?.Invoke(70);

                //// Save changes
                //await this.SaveChangesAsync();
                //updateProgress?.Invoke(90);

                await this.ChapterDetailContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync(cancellationToken);
                await this.ChapterContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync(cancellationToken);
                await this.NovelContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync(cancellationToken);


                // Run VACUUM command
                await this.Database.ExecuteSqlRawAsync("VACUUM");
                updateProgress?.Invoke(99);

                return (true, null);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return (false, "A concurrency error occurred: " + ex.Message);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }


        public async Task<(bool isSuccess, string? msg)> SplitNovel(string bookId, string splitHeaderRegex, Action<double>? updateProgress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var novelStock = await GetFullChapterContent(bookId, isAddLineToModel: false, updateProgress, cancellationToken);

                var newNovel = novelStock.Clone();
                if (newNovel == null)
                {
                    return (false, "Internal error.");
                }

                newNovel.BookId = Guid.NewGuid().ToString();
                newNovel.Chapters = new List<ChapterContent>();

                foreach (var chapter in novelStock?.Chapters!)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    //await this.GetContentChapter(chapter, novelStock?.BookName, cancellationToken: cancellationToken, isParseHtml: false);


                    var lstIndexHeader = chapter.Content?
                        .Select((x, index) => new { Content = x?.Trim(), Index = index })
                        .Where(x => x.Content != null &&
                            Regex.IsMatch(x.Content, splitHeaderRegex))
                        .Select(x => x.Index)
                        .ToList();

                    //int lastChapter = newNovel?.Chapters?.Select(x => x.IndexChapter).Max() ?? 0;
                    var preIndexChapter = Int32.Parse($"{chapter.IndexChapter}000");

                    if (lstIndexHeader?.Count() == 0)
                    {
                        chapter.BookId = newNovel?.BookId;
                        chapter.IndexChapter = preIndexChapter + 1;
                        chapter.ChapterId = Guid.NewGuid().ToString();
                        chapter.ChapterDetailContents = new List<ChapterDetailContent>();
                        chapter?.Content?.ToList().ForEach(content =>
                        {
                            if (!string.IsNullOrEmpty(content))
                            {
                                var chapContent = new ChapterDetailContent();
                                chapContent.Id = Guid.NewGuid().ToString();
                                chapContent.BookId = newNovel?.BookId;
                                chapContent.ChapterId = chapter.ChapterId;
                                chapContent.Index = chapter?.Content?.IndexOf(content!);
                                chapContent.Content = content;
                                chapter?.ChapterDetailContents.Add(chapContent);

                            }
                        });

                        newNovel?.Chapters.Add(chapter!);
                        continue;
                    }

                    foreach (var indexHeader in lstIndexHeader!)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var index = lstIndexHeader.IndexOf(indexHeader);

                        if (index == 0 && indexHeader != 0)
                        {
                            var befChap = chapter?.Content?.GetRange(0, indexHeader - 1);
                            var lastChapBef = newNovel?.Chapters?.LastOrDefault();

                            if (lastChapBef != null)
                            {
                                lastChapBef.ChapterDetailContents = new List<ChapterDetailContent>();
                                befChap?.ForEach(content =>
                                {
                                    var newContent = new ChapterDetailContent();
                                    newContent.Id = Guid.NewGuid().ToString();
                                    newContent.BookId = lastChapBef.BookId;
                                    newContent.ChapterId = lastChapBef.ChapterId;
                                    newContent.Content = content;

                                    lastChapBef?.ChapterDetailContents?.Add(newContent);
                                });
                            }



                        }

                        var newChapter = new ChapterContent();
                        newChapter.Title = Utils.GetHtmlInnerText(chapter?.Content?[indexHeader])?.Replace("&nbsp;", "");
                        newChapter.BookId = newNovel?.BookId;
                        newChapter.ChapterId = Guid.NewGuid().ToString();
                        newChapter.IndexChapter = preIndexChapter + index;
                        newChapter.ChapterDetailContents = new List<ChapterDetailContent>();
                        var isLastChap = index == lstIndexHeader.Count() - 1;

                        var lastChapterIndex = (isLastChap ? chapter?.Content?.Count() - 1 : lstIndexHeader[index + 1] - 1) ?? 0;

                        var count = lastChapterIndex - indexHeader;

                        var lstNewContent = chapter?.Content?.GetRange(indexHeader + 1, count);

                        lstNewContent?.ForEach(content =>
                        {
                            var contentParse = Utils.GetHtmlInnerText(content)?.Replace("&nbsp;", "");
                            if (!string.IsNullOrWhiteSpace(contentParse))
                            {
                                var newContent = new ChapterDetailContent();
                                newContent.Id = Guid.NewGuid().ToString();
                                newContent.BookId = newChapter.BookId;
                                newContent.ChapterId = newChapter.ChapterId;
                                newContent.Content = contentParse;
                                newContent.Index = lstNewContent.IndexOf(content);

                                newChapter.ChapterDetailContents.Add(newContent);
                            }
                        });

                        newNovel?.Chapters.Add(newChapter);
                    }

                    var curIndex = (double)(novelStock?.Chapters?.IndexOf(chapter!) ?? 0);
                    var maxCount = (double)(novelStock?.Chapters.Count ?? 1);

                    var state = curIndex / maxCount * 100;

                    updateProgress?.Invoke(state);


                }

                if (newNovel == null)
                {
                    return (false, "");
                }

                cancellationToken.ThrowIfCancellationRequested();

                newNovel.MaxChapterCount = newNovel?.Chapters.Count;

                await this.NovelContents.AddAsync(newNovel!);
                await this.SaveChangesAsync();


                return (true, null);

            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
