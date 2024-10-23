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
using System.Data.Entity;
using System.IO;
using System.Text.Json;
using MessageBox = System.Windows.MessageBox; // Thêm thư viện Drawing để dùng Icon
using WpfLibrary;

namespace NovelReader
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Property
        public SpeechSynthesizer speechSynthesizer;

        private AppConfig _appConfig;
        public AppConfig AppConfig
        {
            get { return _appConfig; }
            set
            {
                _appConfig = value;
                OnPropertyChanged(nameof(AppConfig));
            }
        }

        private NovelContent _novel;
        public NovelContent Novel
        {
            get { return _novel; }
            set
            {
                _novel = value;
                OnPropertyChanged(nameof(Novel));
            }
        }

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

        public CurrentReader? _current_reader { get; set; }

        public Thickness contentThinkness { get; set; }

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

        public List<(string? novelId, int? chapId, int? lineId, int? posId)> LstPrevChap { get; set; } = new List<(string? novelId, int? chapId, int? lineId, int? posId)>();

        #endregion Property

        public MainWindow()
        {

            AppConfig = new AppConfig();
            AppConfig.Get();
            LoadNovelData();


            InitializeComponent();


            if ((AppConfig.LastWidth ?? 0) >= 0) this.Width = AppConfig.LastWidth.Value;
            if ((AppConfig.LastHeigh ?? 0) >= 0) this.Height = AppConfig.LastHeigh.Value;
            if ((AppConfig.LastLeft ?? 0) >= 0) this.Left = AppConfig.LastLeft.Value;
            if ((AppConfig.LastTop ?? 0) >= 0) this.Top = AppConfig.LastTop.Value;

            InitTTS();
            InitKeyHook();
            InitTaskBarIcon();

            //Hide leftsidebar
            ToggleTOC_Click(null, null);

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
                this.Focus();
                this.lstContent.Focus();
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
            if (_keyboardHook != null)
            {
                _keyboardHook?.Dispose();
            }
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


        public void UpdateUI()
        {
            NumChapterGoto.Maximum = Novel?.Chapters?.Count;
            NumChapterGoto.Value = _current_reader.CurrentChapter;

            InitTriggerChangeColor();

            OnPropertyChanged("");
        }

        private void LoadChapterContent(bool selectedLastItem = false, bool isFirstLoad = false)
        {

            this.RunTaskWithSplash(() =>
            {
                if (Novel != null)
                {
                    if (Novel.Chapters?.Count > 0)
                    {
                        //remove pre
                        //if (_current_reader.CurrentChapter > 0) this.Novel.Chapters[_current_reader.CurrentChapter - 1].Content = null;

                        this.Novel.Chapters.Where(x => x.Content?.Count > 0).ToList().ForEach(r => { r.Content = null; });

                        var selectedChapter = this.Novel.Chapters[_current_reader.CurrentChapter];
                        this.SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter, Novel.BookName).GetAwaiter().GetResult();

                        if (!isFirstLoad)
                        {
                            if (selectedLastItem)
                            {
                                _current_reader.CurrentLine = selectedChapter.Content.Count - 1;
                            }
                            else
                            {
                                _current_reader.CurrentLine = 0;
                            }
                        }


                        ModifySelectedChapter();
                    }
                }
            }
            , doneAction: () =>
            {
            }
            , isHideMainWindows: false
            , isRunAsync: false
            , textColor: AppConfig.TextColor
            , backgroudColor: AppConfig.BackgroundColor
            );

        }

        public void LoadNovelData()
        {
            this.RunTaskWithSplash(
            action: () =>
            {

                var dbPath = AppConfig._sqlitepath;
                var bookId = AppConfig.CurrentBookId;

                if (_AppDbContext == null)
                {
                    var dbContextOptions = new Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>();
                    _AppDbContext = new AppDbContext(dbPath, dbContextOptions);
                }

                this.Novel = _AppDbContext.GetNovel(bookId).GetAwaiter().GetResult();

                _current_reader = _AppDbContext.GetCurrentReader(bookId).GetAwaiter().GetResult();
            }
            , doneAction: () =>
            {
                LoadChapterContent(isFirstLoad: true);
            }
            , textColor: AppConfig.TextColor
            , backgroudColor: AppConfig.BackgroundColor
            )
            ;

        }



        private void ModifySelectedChapter()
        {
            try
            {

                if (lstContent.Items.Count <= 0)
                {
                    this.SelectedChapter = this.Novel.Chapters[_current_reader.CurrentChapter];
                }
                NumChapterGoto.DefaultValue = _current_reader.CurrentChapter;
                lstContent.SelectedIndex = _current_reader.CurrentLine;
                ChapterListView.ScrollIntoView(ChapterListView.SelectedItem);
                lstContent.ScrollIntoView(lstContent.SelectedItem);
                ContinueSpeech();
                //UpdateHightlightFirst();
                _AppDbContext.CurrentReader.Update(_current_reader);
                _AppDbContext.SaveChanges();
                //Utils.ClearRAM(false);
            }
            catch (Exception)
            {

            }
        }


        private void WritePrevChap()
        {
            var bookId = _current_reader.BookId;
            var chapterId = _current_reader.CurrentChapter;
            var lineId = _current_reader.CurrentLine;
            var posId = _current_reader.CurrentPosition;


            var curLine = LstPrevChap.Where(x => x.novelId == bookId && x.chapId == chapterId).FirstOrDefault();

            if (curLine != default)
            {
                curLine.lineId = lineId;
                curLine.posId = posId;
            }
            else
            {
                LstPrevChap.Add((bookId, chapterId, lineId, posId));
            }


        }

        public void EndOfBook()
        {
            this.ShowYesNoMessageBox("You have finished reading the book, do you want to read it again?"
                   , ""
                   , yesAction: () =>
                   {
                       _current_reader.CurrentChapter = 0;
                       _current_reader.CurrentLine = 0;
                       _current_reader.CurrentPosition = 0;
                       LoadChapterContent();
                   }
                   , noAction: () =>
                   {
                       BookLibraryWindow book = new BookLibraryWindow();
                       book.Owner = this;
                       book.ShowDialog();
                   }
                   );
        }

        public void MoveNextLine()
        {

            _current_reader.CurrentPosition = 0;
            if (_current_reader.CurrentLine < SelectedChapter?.Content?.Count - 1)
            {
                _current_reader.CurrentLine += 1;
                ModifySelectedChapter();
            }
            else
            {
                if (_current_reader.CurrentChapter < Novel?.Chapters?.Count - 1)
                {
                    _current_reader.CurrentLine = 0;
                    _current_reader.CurrentChapter += 1;
                    LoadChapterContent();

                }
                else//Move to first
                {
                    EndOfBook();
                }

            }


        }

        private void MoveNextChap()
        {
            var currentIndex = Novel?.Chapters?.IndexOf(SelectedChapter);
            var newIndex = currentIndex + 1;
            if (newIndex.HasValue & newIndex <= Novel?.Chapters?.Count() - 1)
            {
                _current_reader.CurrentChapter = newIndex.Value;
                _current_reader.CurrentLine = 0;
                _current_reader.CurrentPosition = 0;
                LoadChapterContent();
            }
            else
            {
                EndOfBook();

            }
        }

        private void MovePrevLine()
        {

            _current_reader.CurrentPosition = 0;
            if (_current_reader.CurrentLine > 0)
            {
                _current_reader.CurrentLine -= 1;
                ModifySelectedChapter();
            }
            else
            {
                if (_current_reader.CurrentChapter > 0)
                {
                    _current_reader.CurrentChapter -= 1;

                    //var selectedChapter = Novel?.Chapters?[_current_reader.CurrentChapter] ?? new ChapterContent();
                    //SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);
                    LoadChapterContent(true);
                }
            }


        }

        private void MovePrevChap()
        {
            var currentIndex = Novel?.Chapters?.IndexOf(SelectedChapter);
            var newIndex = currentIndex - 1;
            if (newIndex.HasValue & newIndex >= 0)
            {

                _current_reader.CurrentChapter = newIndex.Value;
                _current_reader.CurrentLine = 0;
                _current_reader.CurrentPosition = 0;

                //var selectedChapter = Novel?.Chapters?[newIndex.Value] ?? new ChapterContent();
                //SelectedChapter = _AppDbContext.GetContentChapter(selectedChapter);
                //ModifySelectedChapter();
                LoadChapterContent();
            }
        }
        #endregion Function Logical

        #region UI Change funtion

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

        private void UpdateHightlightFirst()
        {
            string? currentLine = SelectedChapter?.Content?[_current_reader.CurrentLine];
            var curPos = string.IsNullOrEmpty(currentLine) ? 0 : _current_reader.CurrentPosition;
            string selectedText = WpfUtils.GetTextUntilSpace(currentLine ?? "", curPos);


            HighlightSpeechingSelected(curPos, selectedText);
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
        #endregion UI Change funtion

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
            if (!e.Handled & SelectedChapter != null)
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
                        ButtonPlay_Click(null, new RoutedEventArgs());
                        break;
                    case Key.Escape:
                        OpenConfig_Click(null, new RoutedEventArgs());
                        break;
                    case Key.F12:
                        ToggleTOC_Click(null, new RoutedEventArgs());
                        break;
                    case Key.F3:
                        OpenBook_Click(null, new RoutedEventArgs());
                        break;
                    default:
                        break;
                }

                e.Handled = true;
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
                    _current_reader.CurrentLine = 0;
                    _current_reader.CurrentPosition = 0;
                    _current_reader.CurrentChapter = Novel.Chapters.IndexOf(chapter);
                    //SelectedChapter = _AppDbContext.GetContentChapter(chapter);
                    //_current_reader.CurrentChapter = Novel?.Chapters?.IndexOf(chapter) ?? 0;
                    //ModifySelectedChapter();
                    LoadChapterContent();
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


        private void ButtonGoto_Click(object sender, RoutedEventArgs e)
        {
            var chap = NumChapterGoto.Value ?? 0;

            _current_reader.CurrentChapter = chap > 0 ? chap - 1 : chap;

            LoadChapterContent();


        }





        #endregion Button_Action

        #region Menu Region

        private void ToggleTOC_Click(object sender, RoutedEventArgs e)
        {
            // Toggle visibility of the left column
            if (leftColumn.Width != new GridLength(0))
            {
                leftColumn.Width = new GridLength(0); // Hide the column by setting its width to 0
                gridSplitter.Width = new GridLength(0);
                lstContent.Focus();
            }
            else
            {
                leftColumn.Width = new GridLength(3, GridUnitType.Star); // Show the column by restoring its width to 30%
                gridSplitter.Width = new GridLength(5);
                righColumn.Width = new GridLength(7, GridUnitType.Star);
            }
        }

        private void ToggleTopMost(object sender, RoutedEventArgs e)
        {
            this.Topmost = !this.Topmost;

            if (this.Topmost == true)
            {
                toggleTopmostIco.Icon = FontAwesome.WPF.FontAwesomeIcon.CheckSquareOutline;
            }
            else
            {
                toggleTopmostIco.Icon = FontAwesome.WPF.FontAwesomeIcon.SquareOutline;
            }
            e.Handled = true;
        }


        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            BookLibraryWindow openBookWindow = new BookLibraryWindow();
            openBookWindow.Owner = this;
            openBookWindow.ShowDialog();
        }


        public void ImportBook_Click(object sender, RoutedEventArgs e)
        {


        }

        #endregion Menu Region

    }
}