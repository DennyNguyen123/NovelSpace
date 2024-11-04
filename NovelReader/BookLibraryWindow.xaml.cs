using DataSharedLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfLibrary;

namespace NovelReader
{
    /// <summary>
    /// Interaction logic for OpenBookWindow.xaml
    /// </summary>
    public partial class BookLibraryWindow : Window, INotifyPropertyChanged
    {
        public List<NovelContent> novelContents { get; set; }

        public MainWindow MainWindow { get; set; }


        public AppDbContext? _dbContext { get; set; }

        public bool isHandling = false;


        public BookLibraryWindow()
        {
            InitializeComponent();
        }




        protected override void OnContentRendered(EventArgs e)
        {
            MainWindow = (MainWindow)Owner;

            _dbContext = new AppDbContext(MainWindow.AppConfig._sqlitepath, new Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>());

            LoadNovels();
            base.OnContentRendered(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            MainWindow?.Focus();
            _dbContext?.Dispose();
            base.OnClosed(e);
        }


        private void LoadNovels()
        {
            novelContents = _dbContext?.NovelContents?.ToList();

            if (novelContents != null)
            {
                novelContents.ForEach(novel =>
                {
                    novel.Title = $"{novel.MaxChapterCount} Chapter";
                });

                CardItemsControl.ItemsSource = novelContents;
            }


        }


        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChange()
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(""));
        }


        private void OpenBook(NovelContent dataContext)
        {

            if (dataContext != null)
            {
                this.Close();
                MainWindow.AppConfig.CurrentBookId = dataContext.BookId;
                MainWindow.AppConfig.Save();
                MainWindow.LoadNovelData();
                MainWindow.UpdateUI();
            }
        }


        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isHandling)
            {
                // Lấy item tương ứng từ Border
                var border = sender as Border;
                if (border != null)
                {
                    var dataContext = border.DataContext as NovelContent;
                    OpenBook(dataContext);
                }
                isHandling = false;
            }
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (!isHandling)
            {
                var itemControl = sender as ItemsControl;
                if (itemControl != null)
                {
                    var dataContext = itemControl.DataContext as NovelContent;
                    OpenBook(dataContext);
                }
                isHandling = false;
            }
        }

        private (bool isValid, string ext) CheckValidFileExtension(string? filename)
        {

            var lstExtSupport = new List<string>() { ".epub", ".novel" };
            var ext = System.IO.Path.GetExtension(filename);

            if (!lstExtSupport.Contains(ext))
            {
                this.ShowError($"Not support this file extension. Supported files are [{string.Join(", ", lstExtSupport)}]");
                return (false, ext);
            }

            return (true, ext);
        }


        private void ImportBook_Click(object sender, RoutedEventArgs e)
        {
            if (!isHandling)
            {
                var filename = WpfUtils.GetFilePath(null, "Novel files (*.novel)|*.novel|EPUB files (*.epub)|*.epub|All Files|*.*");
                string? bookId = null;
                string? msg = null;
                DateTime startDate = DateTime.Now;

                if (!string.IsNullOrEmpty(filename))
                {
                    var check = CheckValidFileExtension(filename);

                    if (!check.isValid)
                    {
                        return;
                    }

                    this.RunTaskWithSplash(
                        (splash, cancel) =>
                        {
                            splash.UpdateStatus((processbar, txtStatus) =>
                            {
                                txtStatus.Text = "Importing novel...";
                            });

                            if (check.ext == ".epub")
                            {
                                (bookId, msg) = _dbContext.ImportEpub(filename, splash.UpdateProgressBar).GetAwaiter().GetResult();
                            }
                            else if (check.ext == ".novel")
                            {
                                (bookId, msg) = _dbContext.ImportBookByJsonModel(filename, splash.UpdateProgressBar).GetAwaiter().GetResult();
                            }


                            this.Dispatcher.Invoke(() =>
                            {
                                if (!string.IsNullOrWhiteSpace(msg) || string.IsNullOrWhiteSpace(bookId))
                                {
                                    this.ShowError(msg);
                                }
                                else
                                {
                                    LoadNovels();
                                }
                            });

                        }
                        , textColor: MainWindow.AppConfig.TextColor
                        , backgroudColor: MainWindow.AppConfig.BackgroundColor
                        , isHideMainWindows: false
                        , IsIndeterminate: false
                        );

                }

                isHandling = false;
            }
        }

        private void ExportItem_Click(object sender, RoutedEventArgs e)
        {
            if (!isHandling)
            {
                if (sender is ItemsControl itemControl && itemControl?.DataContext is NovelContent novel)
                {
                    var filename = WpfUtils.SaveFileFirst($"{novel?.BookName} - {novel?.Author}", "EPUB files (*.epub)|*.epub|NOVEL Files (*.novel)|novel");

                    if (!string.IsNullOrEmpty(filename))
                    {
                        var check = CheckValidFileExtension(filename);

                        if (!check.isValid)
                        {
                            return;
                        }

                        this.RunTaskWithSplash(
                        action: (splash, cancel) =>
                        {
                            splash.UpdateStatus((processbar, txtStatus) =>
                            {
                                txtStatus.Text = $"Exporting novel {novel?.BookName}...";
                                splash.SizeToContent = SizeToContent.WidthAndHeight;
                            });

                            if (check.ext == ".epub")
                            {
                                _dbContext.ExportToEpub(filename, novel.BookId, splash.UpdateProgressBar, cancel).GetAwaiter().GetResult();
                            }
                            else if (check.ext == ".novel")
                            {
                                _dbContext.ExportToModel(filename, novel.BookId, splash.UpdateProgressBar, cancel).GetAwaiter().GetResult();
                            }


                            this.Dispatcher.Invoke(() =>
                            {
                                WpfUtils.OpenFolderAndSelectFile(filename);
                            });
                        }
                        , isHideMainWindows: true
                        , textColor: MainWindow.AppConfig.TextColor
                        , backgroudColor: MainWindow.AppConfig.BackgroundColor
                        , IsIndeterminate: false
                        );

                    }

                }
                isHandling = false;
            }


        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!isHandling)
            {
                if (sender is ItemsControl itemControl && itemControl?.DataContext is NovelContent novel)
                {
                    bool isSuccess = false;
                    string? msg = "";

                    this.RunTaskWithSplash(
                        action: (splash, cancel) =>
                        {
                            splash.UpdateStatus((processbar, txtStatus) =>
                            {
                                txtStatus.Text = "Deleting novel...";
                            });

                            (isSuccess, msg) = _dbContext.DeleteNovel(novel.BookId, splash.UpdateProgressBar).GetAwaiter().GetResult();

                            this.Dispatcher.Invoke(() =>
                            {
                                if (!isSuccess)
                                {
                                    this.ShowError(msg);
                                }
                                else
                                {
                                    LoadNovels();
                                }
                            });

                        }
                        , textColor: MainWindow.AppConfig.TextColor
                        , backgroudColor: MainWindow.AppConfig.BackgroundColor
                        , isHideMainWindows: true
                        , IsIndeterminate: true
                    );


                }
                isHandling = false;
            }
        }


        private void SplitItem_Click(object sender, RoutedEventArgs e)
        {
            if (!isHandling)
            {
                if (sender is ItemsControl itemControl && itemControl?.DataContext is NovelContent novel)
                {
                    bool isSuccess = false;
                    string? msg = "";

                    this.RunTaskWithSplash(
                        action: (splash, cancel) =>
                        {
                            splash.UpdateStatus((processbar, txtStatus) =>
                            {
                                txtStatus.Text = "Split novel...";
                            });

                            (isSuccess, msg) = _dbContext.SplitNovel(novel.BookId, MainWindow.AppConfig.SplitHeaderRegex, splash.UpdateProgressBar, cancel).GetAwaiter().GetResult();

                            this.Dispatcher.Invoke(() =>
                            {
                                if (!isSuccess)
                                {
                                    this.ShowError(msg);
                                }
                                else
                                {
                                    LoadNovels();
                                }
                            });

                        }
                        , textColor: MainWindow.AppConfig.TextColor
                        , backgroudColor: MainWindow.AppConfig.BackgroundColor
                        , isHideMainWindows: true
                        , IsIndeterminate: false
                    );
                }
                isHandling = false;
            }



        }



    }
}
