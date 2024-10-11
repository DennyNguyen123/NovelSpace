using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

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


        public CurrentReader GetCurrentReader(string bookId)
        {
            var cur = this.CurrentReader.Where(x => x.BookId == bookId).FirstOrDefault();
            if (cur == null)
            {
                cur = new CurrentReader();
                cur.BookId = bookId;
                cur.CurrentChapter = 0;
                cur.CurrentLine = 0;
                cur.CurrentPosition = 0;
                this.CurrentReader.Add(cur);
                this.SaveChanges();
            }
            return cur;

        }


        public NovelContent? GetNovel(string? bookId)
        {
            var novel = this.NovelContents.Where(x => x.BookId == bookId).FirstOrDefault().Clone();
            if (novel != null)
            {
                var lstChapter = this.ChapterContents.Where(x => x.BookId == novel.BookId).OrderBy(x => x.IndexChapter).ToList().Clone();
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

        public ChapterContent GetContentChapter(ChapterContent chapter)
        {
            var content = this.ChapterDetailContents.Where(x => !string.IsNullOrWhiteSpace(x.Content) & x.BookId == chapter.BookId & x.ChapterId == chapter.ChapterId).OrderBy(x => x.Index).Select(r => r.Content).ToList();
            chapter.Content = content;
            return chapter;
        }


        public async Task ImportBookByJsonModel(string filename)
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

                }
            }
            catch (Exception)
            {
                transaction?.Rollback();
                await this.SaveChangesAsync();
            }
        }


        public async Task ImportBookByJsonModel(NovelContent novelContent)
        {
            try
            {
                if (novelContent != null)
                {
                    if (this.NovelContents.Any(x => x.URL == novelContent.URL))
                    {
                        Console.WriteLine("Already exist - skipped.");
                        return;
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

                }
            }
            catch (Exception)
            {
            }
        }




    }
}
