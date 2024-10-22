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

namespace NovelGetter
{
    public class GetterAppConfig
    {
        public string? FolderTemp { get; set; } = "./temp";

        public string? PathBrowser { get; set; } = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Google Chrome.lnk";
        public bool IsHeadless { get; set; } = false;
        public string? BrowserDevice { get; set; } = "iPhone 11";

        public string? ListHostSavePath { get; set; } = "listhost.json";
        public string _sqlitepath { get => $"{this.FolderTemp}//data.db"; }

        private string _savepath = "getterconfig.json";


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
                var rs = JsonSerializer.Deserialize<GetterAppConfig>(json);
                WpfUtils.UpdateValueSamePropName(this, rs);

            }
        }

    }


    public enum eGetChapterType
    {
        GetEndChapter,
        MoveToLast,
        MoveToFirst,
        MoveToPageWithoutChapter
    }

    public class HostGetter
    {
        public string? Url { get; set; }
        public eGetChapterType GetChapterType { get; set; }
        public string? XPathListChapter { get; set; }
        public string? XPathHeader { get; set; }
        public string? XPathContent { get; set; }

        public bool IsLogin { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

}
