using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NovelReader
{

    public class NovelContent
    {
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
        public string? BookId { get; set; }
        public string? ChapterId { get; set; }
        public string? Title { get; set; }
        public string? URL { get; set; }
        public List<string?>? Content { get; set; }
        public ChapterContent()
        {
            this.Content = new List<string?>();
        }

        public void GetContent(SqliteProvider sqliteProvider)
        {
            string sql = $"select * from Content where id = {this.ChapterId} and bookid = {this.BookId}";
            var rs = sqliteProvider.ExecuteQuery<string?>(sql);

        }
    }

    public class Content
    {
        public int? Id { get; set; }
        public string? BookId { get; set; }
        public string? ChapterId { get; set; }
    }


    public class AppConfig
    {
        public int CurrentChapter { get; set; }
        public int CurrentLine { get; set; }
        public int CurrentPosition { get; set; }
        public string? BookJsonPath { get; set; }
        public int FontSize { get; set; } = 14;
        public string? VoiceName { get; set; }
        public int VoiceRate { get; set; } = 0;
        public int VoiceVolumn { get; set; } = 100;

        public bool isShowInTaskBar { get; set; } = true;
        public bool isRememberLocation { get; set; } = true;

        public string? BackgroundColor { get; set; } = "#FFFFFF";
        public string? TextColor { get; set; } = "#000000";
        public string? CurrentTextColor { get; set; } = "#EBFA03";
        public string? SelectedParagraphColor { get; set; } = "#FA5D03";
        public double LastWidth { get; set; } = 300;
        public double LastHeigh { get; set; } = 300;
        public double LastTop { get; set; } = 300;
        public double LastLeft { get; set; } = 300;

        private string _savepath = "appconfig.json";

        public void Save()
        {
            var json = JsonSerializer.Serialize(this);
            WpfUtils.ClearAndWriteToFile(_savepath, json);
        }

        public void Get()
        {
            if (!File.Exists(_savepath))
            {
                WpfUtils.ClearAndWriteToFile(_savepath, JsonSerializer.Serialize(this));
            }
            else
            {
                var json = File.ReadAllText(_savepath);
                var rs = JsonSerializer.Deserialize<AppConfig>(json);
                WpfUtils.UpdateValueSamePropName(this, rs);

            }

        }

    }

}
