using DataSharedLibrary;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NovelGetConsole
{
    public class Test
    {


        public async Task TestForeachMultiThread()
        {
            List<string> lst = new List<string>()
            {
                "A",
                "B",
                "C",
                "D",
                "E"
            };

            var cts = new CancellationTokenSource(); // Tạo nguồn token để hủy

            await Parallel.ForEachAsync(lst, cts.Token, async (item, cancellationToken) =>
            {
                if (cts.IsCancellationRequested)
                {
                    return;
                }
                // Giả lập công việc không đồng bộ
                await Task.Delay(1000); // Giả sử mỗi công việc mất 1000ms

                if (item == "C")
                {
                    await Task.Delay(1000); // Giả sử mỗi công việc mất 1000ms
                    // Hủy tất cả các tác vụ
                    cts.Cancel();
                    return;
                }


                Console.WriteLine(item);

            });

        }

        public async Task TestCompress()
        {
            DateTime startDate = DateTime.Now;
            using var db = new AppDbContext("D:\\Truyen\\SQLite\\data.db", new DbContextOptions<AppDbContext>());

            //var model = await db.NovelContents.Include(x=>x.Chapters.OrderBy(q=>q.IndexChapter)).ThenInclude(r=>r.ChapterDetailContents.OrderBy(q=>q.Index)).FirstOrDefaultAsync();


            var chap = await db.ChapterContents.FirstOrDefaultAsync();

            var model = await db.ChapterDetailContents.Where(x => x.BookId == chap.BookId & x.ChapterId == chap.ChapterId).ToListAsync();

            Console.WriteLine($"Excute sql time {DateTime.Now - startDate}");

            startDate = DateTime.Now;

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true // Để hiển thị JSON đẹp hơn
            };

            var json = JsonSerializer.Serialize(model, options);

            Console.WriteLine($"Excute to json: {DateTime.Now - startDate}");
            startDate = DateTime.Now;

            Console.WriteLine($"Json length: {json?.Length}");


            var compress = Utils.GZipCompressText(json ?? "");
            Console.WriteLine($"Excute compress: {DateTime.Now - startDate}");
            startDate = DateTime.Now;
            Console.WriteLine($"Compress length {compress?.Length}");



            var decompress = Utils.GZipDecompressText(compress ?? "");
            Console.WriteLine($"Excute de-compress: {DateTime.Now - startDate}");
            startDate = DateTime.Now;
            Console.WriteLine($"De-Compress length {decompress?.Length}");
        }


        public async Task TestReadEpub()
        {
            Console.OutputEncoding = Encoding.UTF8;
            string epubFileName = @"E:\Downloads\Thông Minh Cảm Xúc Để Hạnh Phúc Và Thành Công - Travis Bradberry & Jean Greaves.epub";

            var epub = VersOne.Epub.EpubReader.ReadBook(epubFileName);


            var bookName = epub.Title;
            var bookAuthors = epub.Author;


            Console.WriteLine($"Book Name: {bookName}");
            Console.WriteLine($"Book Authors: {bookAuthors}");
            var chapters = epub.Navigation;


            using var db = new AppDbContext(@"D:\Truyen\SQLite\data.db", new DbContextOptions<AppDbContext>());
            //check exist
            var existBook = db.NovelContents.Any(x => x.BookName == bookName);

            if (existBook)
            {
                Console.WriteLine("Already exist - Skipped");
                return;
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

            await db.AddAsync(novel);
            await db.SaveChangesAsync();


        }

    }
}
