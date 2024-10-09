using Microsoft.EntityFrameworkCore;

namespace DataSharedLibrary
{
    public class AppDbContext : DbContext
    {
        private readonly string _dbPath;

        public AppDbContext(string dbPath, DbContextOptions<AppDbContext> options) : base(options)
        {
            _dbPath = dbPath;
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

        public NovelContent? GetNovel(string? bookId)
        {
            var novel = this.NovelContents.Where(x => x.BookId == bookId).FirstOrDefault();
            if (novel != null)
            {
                var lstChapter = this.ChapterContents.Where(x => x.BookId == novel.BookId).OrderBy(x => x.IndexChapter);
                if (lstChapter != null)
                {
                    novel?.Chapters?.AddRange(lstChapter);
                }
            }

            return novel;
        }

        public ChapterContent GetContentChapter(ChapterContent chapter)
        {
            var content = this.ChapterDetailContents.Where(x => x.BookId == chapter.BookId & x.ChapterId == chapter.ChapterId).Select(r => r.Content).ToList();
            chapter.Content = content;
            return chapter;
        }

    }
}
