using System.ComponentModel;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms; // Thêm thư viện Windows Forms
using System.Drawing;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Brushes = System.Windows.Media.Brushes;
using Hardcodet.Wpf.TaskbarNotification;
using DataSharedLibrary;
using System.Data.Entity; // Thêm thư viện Drawing để dùng Icon

namespace NovelReader
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Property
        public SpeechSynthesizer speechSynthesizer;
        public AppConfig AppConfig { get; set; }


        public NovelContent Novel { get; set; }

        private ChapterContent _selectedChapter;
        public ChapterContent SelectedChapter
        {
            get => _selectedChapter;
            set
            {
                _selectedChapter = value;
                OnPropertyChanged(nameof(SelectedChapter));
            }
        }

        public CurrentReader _current_reader { get; set; }

        // Phương thức để thông báo thay đổi thuộc tính
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isSpeeking;
        public AppDbContext _AppDbContext = null;

        public Style listBoxItemStyle
        {
            get; set;
        }

        #endregion Property

        public MainWindow()
        {
            AppConfig = new AppConfig();
            AppConfig.Get();
            LoadNovelData();

            InitTTS();
            InitKeyHook();

            InitializeComponent();

            UpdateUIFirst();

            InitTaskBarIcon();

            DataContext = this;

        }

        protected override void OnContentRendered(EventArgs e)
        {
            UpdateUI();
            base.OnContentRendered(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _keyboardHook?.Dispose();
            _AppDbContext?.Dispose();

            base.OnClosed(e);
        }


        #region TaskBar Icon


        private void ToggleWindowVisibility()
        {
            if (this.Visibility == Visibility.Visible && this.WindowState != WindowState.Minimized)
            {
                // Nếu cửa sổ đang hiện thì ẩn nó
                this.Hide();
                //this.ShowInTaskbar = AppConfig.isShowInTaskBar;
            }
            else
            {
                // Nếu cửa sổ đang ẩn thì hiện lên
                //this.ShowInTaskbar = AppConfig.isShowInTaskBar;
                this.Show();
                this.WindowState = WindowState.Normal; // Đảm bảo cửa sổ hiển thị bình thường
                this.Activate(); // Đưa cửa sổ lên phía trước
            }
        }

        public void InitTaskBarIcon()
        {
            var icon = System.Drawing.Icon.ExtractAssociatedIcon("novelreader.ico");
            taskBarIcon.Icon = icon;
        }

        private void TaskBarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ToggleWindowVisibility();
        }

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowVisibility();
        }

        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        #endregion TaskBar Icon

        #region Key Hook
        private KeyboardHook _keyboardHook;

        private void InitKeyHook()
        {
            _keyboardHook = new KeyboardHook();
            _keyboardHook.MediaKeyPressed += KeyboardHook_MediaKeyPressed;
        }

        private void KeyboardHook_MediaKeyPressed(Key key)
        {
            if (key == Key.MediaPlayPause)
            {
                ButtonPlay_Click(new object(), new RoutedEventArgs());
            }
            else if (key == Key.MediaNextTrack)
            {
                ButtonNextChap_Click(new object(), new RoutedEventArgs());
            }
            else if (key == Key.MediaPreviousTrack)
            {
                ButtonPreChap_Click(new object(), new RoutedEventArgs());
            }

        }

        #endregion Key Hook

        #region Function Logical

        public void UpdateUIFirst()
        {
            //Hide leftsidebar
            leftColumn.Width = new GridLength(0);

            this.Width = AppConfig.LastWidth;
            this.Height = AppConfig.LastHeigh;
            this.Left = AppConfig.LastLeft;
            this.Top = AppConfig.LastTop;

        }

        public void InitTriggerChangeColor()
        {
            // Tạo một style mới cho ListBoxItem
            listBoxItemStyle = new Style(typeof(ListBoxItem));

            // Đặt event trigger hoặc style trigger nếu cần
            var trigger = new Trigger
            {
                Property = ListBoxItem.IsSelectedProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, WpfUtils.ConvertHtmlColorToBrush(AppConfig.SelectedParagraphColor)));

            // Thêm trigger vào style
            listBoxItemStyle.Triggers.Add(trigger);
        }

        public void UpdateUI()
        {
            InitTriggerChangeColor();
            if (Novel != null)
            {
                this.Title = $"{Novel.BookName} - {Novel.Author}";

                ModifySelectedChapter();
            }

            //Change UI
            OnPropertyChanged("");
        }

        private void UpdateHightlightFirst()
        {
            string? currentLine = SelectedChapter?.Content?[_current_reader.CurrentLine];
            var curPos = string.IsNullOrEmpty(currentLine) ? 0 : _current_reader.CurrentPosition;
            string selectedText = WpfUtils.GetTextUntilSpace(currentLine ?? "", curPos);


            HighlightSpeechingSelected(curPos, selectedText);
        }

        private void MoveNextLine()
        {

            if (_current_reader.CurrentLine < SelectedChapter?.Content?.Count - 1)
            {
                _current_reader.CurrentLine += 1;
            }
            else
            {
                if (_current_reader.CurrentChapter < Novel?.Chapters?.Count - 1)
                {
                    _current_reader.CurrentLine = 0;
                    _current_reader.CurrentChapter += 1;
                    var selectedChapter = Novel.Chapters[_current_reader.CurrentChapter];
                    SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);
                }
                else//Move to first
                {
                    _current_reader.CurrentLine = 0;
                    _current_reader.CurrentChapter = 0;
                    var selectedChapter = Novel.Chapters[_current_reader.CurrentChapter];
                    SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);
                }
            }

            //ContinueSpeech();

            _current_reader.CurrentPosition = 0;

            ModifySelectedChapter();
        }

        private void MoveNextChap()
        {
            var currentIndex = Novel?.Chapters?.IndexOf(SelectedChapter);
            var newIndex = currentIndex + 1;
            if (newIndex.HasValue & newIndex <= Novel?.Chapters?.Count())
            {
                var selectedChapter = Novel?.Chapters?[newIndex.Value] ?? new ChapterContent();
                SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);

                _current_reader.CurrentChapter = newIndex.Value;
                _current_reader.CurrentLine = 0;
                _current_reader.CurrentPosition = 0;
                ModifySelectedChapter();
            }
        }

        private void MovePrevLine()
        {

            if (_current_reader.CurrentLine > 0)
            {
                _current_reader.CurrentLine -= 1;
            }
            else
            {
                if (_current_reader.CurrentChapter > 0)
                {
                    _current_reader.CurrentChapter -= 1;
                    var selectedChapter = Novel?.Chapters?[_current_reader.CurrentChapter] ?? new ChapterContent();
                    SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);

                    var curLine = (SelectedChapter?.Content?.Count ?? 1) - 1;
                    _current_reader.CurrentLine = curLine;
                }
            }



            //ContinueSpeech();

            _current_reader.CurrentPosition = 0;

            ModifySelectedChapter();
        }

        private void MovePrevChap()
        {
            var currentIndex = Novel?.Chapters?.IndexOf(SelectedChapter);
            var newIndex = currentIndex - 1;
            if (newIndex.HasValue & newIndex >= 0)
            {
                var selectedChapter = Novel?.Chapters?[newIndex.Value] ?? new ChapterContent();
                SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);

                _current_reader.CurrentChapter = newIndex.Value;
                _current_reader.CurrentLine = 0;
                _current_reader.CurrentPosition = 0;
                ModifySelectedChapter();
            }
        }

        private void LoadNovelData_bak()
        {
            if (!string.IsNullOrEmpty(AppConfig.FolderTemp))
            {
                Novel = new NovelContent();
                Novel = WpfUtils.GetModelFromJsonFile<NovelContent>(AppConfig.FolderTemp) ?? new NovelContent();

                if (Novel != null)
                {
                    if (Novel.Chapters != null)
                    {
                        foreach (var item in Novel.Chapters)
                        {
                            item?.Content?.RemoveAll(x => string.IsNullOrEmpty(x));
                        }

                        SelectedChapter = Novel.Chapters[_current_reader.CurrentChapter];
                    }
                }
            }

        }

        private void LoadNovelData()
        {
            var dbPath = AppConfig._sqlitepath;
            var bookId = AppConfig.CurrentBookId;
            var dbContextOptions = new Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>();
            _AppDbContext = new AppDbContext(dbPath, dbContextOptions);

            _current_reader = _AppDbContext.GetCurrentReader(bookId);


            Novel = _AppDbContext.GetNovel(bookId);

            if (Novel != null)
            {
                if (Novel.Chapters?.Count > 0)
                {
                    var selectedChapter = Novel.Chapters[_current_reader.CurrentChapter];
                    SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);
                }
            }
        }

        private void ModifySelectedChapter()
        {
            //lstContent.SelectedIndex = _current_reader.CurrentLine;
            lstContent.SelectedIndex = _current_reader.CurrentLine;
            ChapterListView.ScrollIntoView(ChapterListView.SelectedItem);
            lstContent.ScrollIntoView(lstContent.SelectedItem);
            ContinueSpeech();
            UpdateHightlightFirst();
            _AppDbContext.CurrentReader.Update(_current_reader);
            _AppDbContext.SaveChanges();
        }


        // Hàm tìm và highlight từ trong TextBlock
        private void HighlightWord(TextBlock textBlock, string? text, int startIndex, int length)
        {
            if (!string.IsNullOrEmpty(text) & startIndex < text?.Length)
            {
                textBlock.Inlines.Clear();
                var isExist = text.Substring(startIndex, length)?.Count() > 0;

                if (!isExist)
                {
                    textBlock.Inlines.Add(new Run(text));
                }
                else
                {
                    string beforeKeyword = text.Substring(0, startIndex);
                    string highlightedKeyword = text.Substring(startIndex, length);
                    string afterKeyword = text.Substring(startIndex + length);

                    if (!string.IsNullOrEmpty(beforeKeyword))
                    {
                        textBlock.Inlines.Add(new Run(beforeKeyword));
                    }

                    var highlightedRun = new Run(highlightedKeyword)
                    {
                        Background = WpfUtils.ConvertHtmlColorToBrush(AppConfig.CurrentTextColor),
                        FontWeight = FontWeights.Bold
                    };
                    textBlock.Inlines.Add(highlightedRun);

                    if (!string.IsNullOrEmpty(afterKeyword))
                    {
                        textBlock.Inlines.Add(new Run(afterKeyword));
                    }
                }
            }
        }

        // Hàm tìm TextBlock bên trong ListBoxItem
        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T tChild)
                {
                    return tChild;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }


        private void HighlightSpeechingSelected(int startPosition, string? highlightText)
        {
            foreach (var item in lstContent.Items)
            {
                var container = (ListBoxItem)lstContent.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(container);
                    if (textBlock != null)
                    {
                        textBlock.Inlines.Clear();
                        textBlock.Inlines.Add(new Run(item.ToString())); // Hiển thị nội dung mặc định (bỏ highlight)
                    }
                }
            }

            // Highlight item đang được chọn
            if (lstContent.SelectedItem is string selectedText)
            {
                var container = (ListBoxItem)lstContent.ItemContainerGenerator.ContainerFromItem(selectedText);
                if (container != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(container);
                    if (textBlock != null)
                    {
                        HighlightWord(textBlock, selectedText, startPosition, (highlightText ?? "").Length);
                    }
                }
            }
        }

        #endregion Function Logical

        #region Event Handler
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (!AppConfig.isShowInTaskBar & this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            AppConfig.LastTop = Top;
            AppConfig.LastLeft = Left;
            AppConfig.Save();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AppConfig.LastWidth = Width;
            AppConfig.LastHeigh = Height;
            AppConfig.Save();
        }

        private void lstContent_KeyDown(object sender, KeyEventArgs e)
        {
            if (SelectedChapter != null)
            {
                switch (e.Key)
                {
                    case Key.Down:
                        MoveNextLine();
                        break;
                    case Key.Up:
                        MovePrevLine();
                        break;
                    case Key.Right:
                        MoveNextChap();
                        break;
                    case Key.Left:
                        MovePrevChap();
                        break;
                    case Key.Space:
                        ButtonPlay_Click(null, null);
                        break;
                    case Key.Escape:
                        OpenConfig_Click(null, null);
                        break;
                    case Key.F1:
                        ToggleTOC_Click(null, null);
                        break;
                    default:
                        break;
                }
            }

        }


        private void LstContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listView)
            {
                if (listView.IsMouseCaptured)
                {
                    _current_reader.CurrentLine = lstContent.SelectedIndex;
                    _current_reader.CurrentPosition = 0;
                    ModifySelectedChapter();
                }
            }
        }

        private void ChaptersListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Cập nhật SelectedChapter với chương được chọn
            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is ChapterContent chapter)
            {
                if (listView.IsMouseCaptured)
                {
                    SelectedChapter = _AppDbContext.GetContentChapter(chapter);
                    _current_reader.CurrentChapter = Novel?.Chapters?.IndexOf(chapter) ?? 0;
                    _current_reader.CurrentLine = 0;
                    _current_reader.CurrentPosition = 0;
                    ModifySelectedChapter();
                }
            }
        }

        #endregion Event Handler

        #region TTS
        public void InitTTS()
        {
            speechSynthesizer = new SpeechSynthesizer();
            // Đăng ký sự kiện SpeakProgress
            speechSynthesizer.SpeakProgress += SpeechSynthesizer_SpeakProgress;
            speechSynthesizer.SpeakCompleted += SpeechSynthesizer_SpeakCompleted;

            if (!string.IsNullOrEmpty(AppConfig.VoiceName))
            {
                speechSynthesizer.SelectVoice(AppConfig.VoiceName);
            }

            this.speechSynthesizer.Rate = this.AppConfig.VoiceRate;
            this.speechSynthesizer.Volume = this.AppConfig.VoiceVolumn;

        }

        public void ContinueSpeech()
        {
            string? voiceText = null;
            if (_isSpeeking)
            {

                if (_current_reader.CurrentLine >= 0)
                {
                    voiceText = SelectedChapter.Content[_current_reader.CurrentLine];
                }

                SpeakInBackground(voiceText);

            }
            else
            {
                InitTTS();
            }
        }

        public void SpeakInBackground(string? text)
        {
            // Chạy SpeakAsync trong một task và kiểm tra yêu cầu hủy
            Task.Run(() =>
            {
                try
                {
                    speechSynthesizer.SpeakAsyncCancelAll();

                    //if (_current_reader.CurrentPosition > 0)
                    //{
                    //    text = text?.Substring(0);
                    //}

                    speechSynthesizer.SpeakAsync(text);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Speech operation was canceled.");
                }
            });
        }


        private void SpeechSynthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                MoveNextLine();
            }
        }

        // Sự kiện này sẽ được kích hoạt khi bắt đầu đọc từng từ
        private void SpeechSynthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {

            _current_reader.CurrentPosition = e.CharacterPosition;
            AppConfig.Save();

            HighlightSpeechingSelected(e.CharacterPosition, e.Text);
        }
        #endregion TTS

        #region Button_Action

        private void ToggleTOC_Click(object sender, RoutedEventArgs e)
        {
            // Toggle visibility of the left column
            if (leftColumn.Width != new GridLength(0, GridUnitType.Star))
            {
                leftColumn.Width = new GridLength(0, GridUnitType.Star); // Hide the column by setting its width to 0
                lstContent.Focus();
            }
            else
            {
                leftColumn.Width = new GridLength(3, GridUnitType.Star); // Show the column by restoring its width to 30%
            }
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            AppConfigWindows config = new AppConfigWindows();
            config.Owner = this;
            config.ShowDialog();
        }

        private void ButtonPreChap_Click(object sender, RoutedEventArgs e)
        {
            MovePrevChap();
        }

        private void ButtonPreLine_Click(object sender, RoutedEventArgs e)
        {
            MovePrevLine();
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            if (speechSynthesizer.State == SynthesizerState.Ready)
            {
                _isSpeeking = true;
                string? textToSpeak = lstContent?.SelectedValue?.ToString();
                SpeakInBackground(textToSpeak);
            }
            if (speechSynthesizer.State == SynthesizerState.Speaking)
            {
                _isSpeeking = false;
                speechSynthesizer.Pause();
            }
            else
            {
                _isSpeeking = true;
                speechSynthesizer.Resume();
            }


            if (_isSpeeking)
            {
                icoPlay.Icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
            }
            else
            {
                icoPlay.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
            }
        }

        private void ButtonNextLine_Click(object sender, RoutedEventArgs e)
        {
            MoveNextLine();
        }

        private void ButtonNextChap_Click(object sender, RoutedEventArgs e)
        {
            MoveNextChap();
        }





        #endregion Button_Action


        #region Menu Region
        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            OpenBookWindow openBookWindow = new OpenBookWindow();
            openBookWindow.Owner = this;
            openBookWindow.ShowDialog();
        }
        #endregion Menu Region

    }
}