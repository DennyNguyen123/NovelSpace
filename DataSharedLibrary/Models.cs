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
        public string? BookId { get; set; }
        public string? Title { get; set; }
        public string? ImageBase64 { get; set; }
        public string? BookName { get; set; }
        public string? Author { get; set; }
        public string? URL { get; set; }
        public int? MaxChapterCount { get; set; }
        public List<ChapterContent>? Chapters { get; set; }
        public NovelContent()
        {
            Chapters = new List<ChapterContent>();
        }
    }

    public class ChapterContent
    {
        [ForeignKey(nameof(NovelContent))] // Chỉ định khóa phụ
        public string? BookId { get; set; }

        public int? IndexChapter { get; set; }

        [Key]
        public string? ChapterId { get; set; }
        public string? Title { get; set; }
        public string? URL { get; set; }
        public List<string?>? Content { get; set; }
        public ChapterContent()
        {
            this.Content = new List<string?>();
        }

    }

    public class ChapterDetailContent
    {
        [Key]
        public string? Id { get; set; }

        public int? Index { get; set; }

        [ForeignKey(nameof(NovelContent))]
        public string? BookId { get; set; }

        [ForeignKey(nameof(ChapterContent))]
        public string? ChapterId { get; set; }

        public string? Content { get; set; }
    }


}
