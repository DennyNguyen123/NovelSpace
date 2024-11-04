using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WpfLibrary;

namespace NovelReader
{
    public class AppConfig
    {
        public string? FolderTemp { get; set; } = "./temp";
        public string? CurrentBookId { get; set; }
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
        public double? LastWidth { get; set; } = 300;
        public double? LastHeigh { get; set; } = 300;
        public double? LastTop { get; set; } = 300;
        public double? LastLeft { get; set; } = 300;
        public double TextMargin { get; set; } = 30;
        public double LineHeight { get; set; } = 100;
        public string FontFamily { get; set; } = "Arial";
        public string SplitHeaderRegex { get; set; } = "^Chương\\s\\d+:\\s*";
        public string _sqlitepath { get => $"{this.FolderTemp}//data.db"; }

        private string _savepath = "appconfig.json";


        private JsonSerializerOptions jsonOption = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            WriteIndented = true // Tùy chọn cho đẹp mã JSON
        };

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this);
                WpfUtils.ClearAndWriteToFile(_savepath, json);
            }
            catch (Exception)
            {

            }
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
