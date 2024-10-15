using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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


        public async Task<CurrentReader> GetCurrentReader(string bookId)
        {
            var cur = await this.CurrentReader.Where(x => x.BookId == bookId).FirstOrDefaultAsync();
            if (cur == null)
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
            //await Task.Delay(5000);
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


        public async Task<string?> ImportBookByJsonModel(string filename)
        {
            IDbContextTransaction? transaction = null;
            try
            {
                transaction = await this.Database.BeginTransactionAsync();
                using FileStream stream = File.OpenRead(filename);
                NovelContent? novelContent = null;
                novelContent = await JsonSerializer.DeserializeAsync<NovelContent?>(stream);
                if (novelContent != null)
                {
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

                    await transaction.CommitAsync();
                    await this.SaveChangesAsync();
                    return newNovel.BookId;

                }
            }
            catch (Exception)
            {
                transaction?.Rollback();
                await this.SaveChangesAsync();
            }
            return null;
        }


        public async Task<string?> ImportBookByJsonModel(NovelContent novelContent)
        {
            try
            {
                if (novelContent != null)
                {
                    if (this.NovelContents.Any(x => x.URL == novelContent.URL))
                    {
                        Console.WriteLine("Already exist - skipped.");
                        return null;
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


                //check exist
                var existBook = await this.NovelContents.Where(x => x.BookName == bookName).FirstOrDefaultAsync();

                if (existBook != null)
                {
                    return (existBook.BookId, "Already exist - Skipped");
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




    }
}
