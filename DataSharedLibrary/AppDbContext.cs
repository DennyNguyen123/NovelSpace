using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuickEPUB;
using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Schema;

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


        public async Task<NovelContent?> GetNovel(string? bookId)
        {
            var novel = await this.NovelContents.AsNoTracking().Where(x => x.BookId == bookId).FirstOrDefaultAsync();

            if (novel != null)
            {
                novel.BookName = $"{novel.BookName} - {novel.Author}";
                var lstChapter = await this.ChapterContents.AsNoTracking().Where(x => x.BookId == novel.BookId).OrderBy(x => x.IndexChapter).ToListAsync();
                if (lstChapter?.Count() > 0)
                {
                    lstChapter.ForEach(x =>
                    {
                        x.Content = new List<string?>();
                        x.Title = $"[{x.IndexChapter + 1}/{novel.MaxChapterCount}] {x.Title}";
                    }
                    );
                    novel?.Chapters?.AddRange(lstChapter);
                }
            }

            return novel;
        }

        public async Task<ChapterContent> GetContentChapter(ChapterContent chapter)
        {
            var content = await this.ChapterDetailContents.AsNoTracking()
                .Where(x =>
                !string.IsNullOrWhiteSpace(x.Content)
                & x.BookId == chapter.BookId
                & x.ChapterId == chapter.ChapterId)
                .OrderBy(x => x.Index)
                .Select(r => r.Content)
                .ToListAsync();

            chapter.Content = content?.Where(x => !x.IsHtml()).ToList();
            return chapter;
        }


        public async Task<(string? bookId, string? msg)> ImportBookByJsonModel(string filename)
        {
            IDbContextTransaction? transaction = null;
            try
            {


                transaction = await this.Database.BeginTransactionAsync();
                //using FileStream stream = File.OpenRead(filename);
                //NovelContent? novelContent = null;
                //novelContent = await JsonSerializer.DeserializeAsync<NovelContent?>(stream);

                var novelContent = Utils.JsonFromCompress<NovelContent?>(filename);
                if (novelContent != null)
                {
                    var checkExist = await CheckExist(novelContent?.BookName);

                    if (checkExist.isExist)
                    {
                        return (checkExist.bookId, checkExist.msg);
                    }

                    var newNovel = new NovelContent();
                    newNovel.Title = novelContent?.Title;
                    newNovel.URL = novelContent?.URL;
                    newNovel.Author = novelContent?.Author;
                    newNovel.BookId = novelContent?.BookId ?? Guid.NewGuid().ToString();
                    newNovel.MaxChapterCount = novelContent?.MaxChapterCount;
                    newNovel.BookName = novelContent?.BookName;
                    newNovel.ImageBase64 = novelContent?.ImageBase64;


                    var lstChapter = new List<ChapterContent>();
                    var lstChapterDetail = new List<ChapterDetailContent>();
                    novelContent?.Chapters?.ForEach(chapter =>
                    {
                        var newChap = new ChapterContent();
                        newChap.Title = chapter.Title;
                        newChap.IndexChapter = novelContent.Chapters.IndexOf(chapter);
                        newChap.URL = chapter.URL;
                        newChap.BookId = newNovel.BookId;
                        newChap.ChapterId = chapter.ChapterId ?? Guid.NewGuid().ToString();
                        lstChapter.Add(newChap);

                        chapter?.Content?.ForEach(con =>
                        {
                            var content = new ChapterDetailContent();
                            content.Id = Guid.NewGuid().ToString();
                            content.ChapterId = newChap.ChapterId;
                            content.BookId = newNovel.BookId;
                            content.Content = con;
                            content.Index = chapter.Content.IndexOf(con);
                            lstChapterDetail.Add(content);
                        });

                    });

                    await this.NovelContents.AddRangeAsync(newNovel);
                    await this.ChapterContents.AddRangeAsync(lstChapter);
                    await this.ChapterDetailContents.AddRangeAsync(lstChapterDetail);

                    await transaction.CommitAsync();
                    await this.SaveChangesAsync();
                    return (newNovel.BookId, null);

                }
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                await this.SaveChangesAsync();
                return (null, ex.Message);
            }
            return (null, null);
        }


        public async Task<string?> ImportBookNovelModel(NovelContent novelContent)
        {
            try
            {
                if (novelContent != null)
                {
                    var checkExist = await CheckExist(novelContent.BookName);

                    if (checkExist.isExist)
                    {
                        return (checkExist.bookId);
                    }

                    var newNovel = new NovelContent();
                    newNovel.Title = novelContent.Title;
                    newNovel.URL = novelContent.URL;
                    newNovel.Author = novelContent.Author;
                    newNovel.BookId = novelContent.BookId ?? Guid.NewGuid().ToString();
                    newNovel.MaxChapterCount = novelContent.MaxChapterCount;
                    newNovel.BookName = novelContent.BookName;
                    newNovel.ImageBase64 = novelContent.ImageBase64;


                    var lstChapter = new List<ChapterContent>();
                    var lstChapterDetail = new List<ChapterDetailContent>();
                    novelContent?.Chapters?.ForEach(chapter =>
                    {
                        var newChap = new ChapterContent();
                        newChap.Title = chapter.Title;
                        newChap.IndexChapter = novelContent.Chapters.IndexOf(chapter);
                        newChap.URL = chapter.URL;
                        newChap.BookId = newNovel.BookId;
                        newChap.ChapterId = chapter.ChapterId ?? Guid.NewGuid().ToString();
                        lstChapter.Add(newChap);

                        chapter?.Content?.ForEach(con =>
                        {
                            var content = new ChapterDetailContent();
                            content.Id = Guid.NewGuid().ToString();
                            content.ChapterId = newChap.ChapterId;
                            content.BookId = newNovel.BookId;
                            content.Content = con;
                            content.Index = chapter.Content.IndexOf(con);
                            lstChapterDetail.Add(content);
                        });

                    });

                    await this.NovelContents.AddRangeAsync(newNovel);
                    await this.ChapterContents.AddRangeAsync(lstChapter);
                    await this.ChapterDetailContents.AddRangeAsync(lstChapterDetail);

                    await this.SaveChangesAsync();
                    return newNovel.BookId;

                }
            }
            catch (Exception)
            {
            }
            return null;
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


        public async Task<(string? bookId, string? msg)> ImportEpub(string filePath)
        {
            try
            {
                var epub = VersOne.Epub.EpubReader.ReadBook(filePath);

                var bookName = epub.Title;
                var bookAuthors = epub.Author;


                //Console.WriteLine($"Book Name: {bookName}");
                //Console.WriteLine($"Book Authors: {bookAuthors}");
                var chapters = epub.Navigation;

                var checkExist = await CheckExist(bookName);

                if (checkExist.isExist)
                {
                    return (checkExist.bookId, checkExist.msg);
                }


                var novel = new NovelContent();


                novel.BookName = bookName;
                novel.Author = bookAuthors;
                novel.MaxChapterCount = chapters?.Count;
                novel.BookId = Guid.NewGuid().ToString();
                novel.Chapters = new List<ChapterContent>();
                novel.ImageBase64 = Convert.ToBase64String(epub.CoverImage);

                chapters?.ForEach(chapter =>
                {
                    var novelChapter = new ChapterContent();
                    var index = chapters.IndexOf(chapter);
                    var chapter_title = chapter.Title;

                    novelChapter.IndexChapter = index;
                    novelChapter.ChapterId = Guid.NewGuid().ToString();
                    novelChapter.Title = chapter_title;
                    novelChapter.BookId = novel.BookId;
                    novelChapter.ChapterDetailContents = new List<ChapterDetailContent>();

                    var chapter_content = chapter.HtmlContentFile?.Content;

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(chapter_content);

                    var body = htmlDoc.DocumentNode.SelectSingleNode("//body").InnerText;

                    var lstBody = body.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (lstBody?.Count < 2)
                    {
                        var separatestring = new string[] { ". ", "? ", "! " };
                        lstBody = body.Split(separatestring, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }


                    lstBody?.ForEach(content =>
                    {
                        var contentChapter = new ChapterDetailContent();
                        contentChapter.BookId = novel.BookId;
                        contentChapter.ChapterId = novelChapter.ChapterId;
                        contentChapter.Id = Guid.NewGuid().ToString();
                        contentChapter.Content = content;
                        novelChapter.ChapterDetailContents.Add(contentChapter);

                    });

                    novel.Chapters.Add(novelChapter);

                }
                );

                await this.AddAsync(novel);
                await this.SaveChangesAsync();
                return (novel?.BookId, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }

        }


        public async Task ExportToEpub(string epubFileName, string? bookId)
        {

            var novel = await GetNovel(bookId);

            var doc = new Epub(novel?.BookName, novel?.Author);

            var stream = new MemoryStream(Convert.FromBase64String(novel?.ImageBase64));
            doc.AddResource("cover.jpge", EpubResourceType.JPEG, stream, true);


            novel?.Chapters?.ForEach(async (chap) =>
            {
                chap = await GetContentChapter(chap);

                string html = @$"<h2 style=""color:red"">{chap?.Title}</h2><br><br><br>{string.Join("<br><br>", chap?.Content)}<br><br><br>";
                
                doc.AddSection(chap?.Title, html);
            }
            );

            using (var fs = new FileStream(epubFileName, FileMode.Create))
            {
                doc.Export(fs);
            }

        }


        public async Task<(bool isSuccess, string? msg)> DeleteNovel(string bookId)
        {
            try
            {

                await this.ChapterDetailContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync();
                await this.ChapterContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync();
                await this.NovelContents.Where(x => x.BookId == bookId).ExecuteDeleteAsync();
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
