using Microsoft.EntityFrameworkCore;
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
                    lstChapter.ForEach(x => x.Content = new List<string?>());
                    novel?.Chapters?.AddRange(lstChapter);
                }
            }

            return novel;
        }

        public ChapterContent GetContentChapter(ChapterContent chapter)
        {
            var content = this.ChapterDetailContents.Where(x => !string.IsNullOrEmpty(x.Content) & x.BookId == chapter.BookId & x.ChapterId == chapter.ChapterId).Select(r => r.Content).ToList();
            chapter.Content = content;
            return chapter;
        }

    }
}
