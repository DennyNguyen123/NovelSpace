using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace DataSharedLibrary
{

    public class CurrentReader
    {
        [Key]
        public string? BookId { get; set; }
        public int CurrentChapter { get; set; }
        public int CurrentLine { get; set; }
        public int CurrentPosition { get; set; }

    }

    public class NovelContent
    {
        [Key]
        public string? BookId { get; set; }  // Khóa chính

        public string? Title { get; set; }
        public string? ImageBase64 { get; set; }
        public string? BookName { get; set; }
        public string? Description { get; set; }
        public string? ShortDesc { get; set; }
        public List<string?>? Tags { get; set; } = new List<string?>();
        public string? Author { get; set; }
        public string? URL { get; set; }
        public string? Slug { get; set; }
        public int? MaxChapterCount { get; set; }

        // Mối quan hệ một-nhiều với ChapterContent
        public List<ChapterContent>? Chapters { get; set; }

        public NovelContent()
        {
            Chapters = new List<ChapterContent>();
        }
    }

    public class ChapterContent
    {
        [Key] // Khóa chính
        public string? ChapterId { get; set; }

        public string? Slug { get; set; }

        [ForeignKey(nameof(NovelContent))] // Khóa phụ tham chiếu đến NovelContent
        public string? BookId { get; set; }

        public int? IndexChapter { get; set; }
        public string? Title { get; set; }
        public string? URL { get; set; }
        public List<string?>? Content { get; set; }

        // Mối quan hệ nhiều-một với NovelContent
        public NovelContent? NovelContent { get; set; }

        // Điều hướng tới ChapterDetailContent
        public ICollection<ChapterDetailContent>? ChapterDetailContents { get; set; }

        public ChapterContent()
        {
            this.Content = new List<string?>();
        }
    }

    public class ChapterDetailContent
    {
        [Key]
        public string? Id { get; set; }  // Khóa chính

        public int? Index { get; set; }

        [ForeignKey(nameof(NovelContent))] // Khóa phụ tham chiếu đến NovelContent
        public string? BookId { get; set; }

        [ForeignKey(nameof(ChapterContent))] // Khóa phụ tham chiếu đến ChapterContent
        public string? ChapterId { get; set; }

        public string? Content { get; set; }

        // Điều hướng tới ChapterContent
        public ChapterContent? ChapterContent { get; set; }
    }


}
