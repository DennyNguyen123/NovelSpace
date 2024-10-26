using DataSharedLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
            // Lấy item tương ứng từ Border
            var border = sender as Border;
            if (border != null)
            {
                var dataContext = border.DataContext as NovelContent;
                OpenBook(dataContext);
            }
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var itemControl = sender as ItemsControl;
            if (itemControl != null)
            {
                var dataContext = itemControl.DataContext as NovelContent;
                OpenBook(dataContext);
            }
        }


        private void ImportBook_Click(object sender, RoutedEventArgs e)
        {
            var filename = WpfUtils.GetFilePath(null, "Novel files (*.novel)|*.novel|EPUB files (*.epub)|*.epub|All Files|*.*");
            string? bookId = null;
            string? msg = null;
            DateTime startDate = DateTime.Now;

            if (!string.IsNullOrEmpty(filename))
            {
                var ext = System.IO.Path.GetExtension(filename);
                var lstExtSupport = new List<string>() { ".epub", ".novel" };

                if (!lstExtSupport.Contains(ext))
                {
                    this.ShowError($"Not support this file extension. Supported files are [{string.Join(", ", lstExtSupport)}]");
                }

                this.RunTaskWithSplash(
                    (splash, cancel) =>
                    {
                        splash.UpdateStatus((processbar, txtStatus) =>
                        {
                            txtStatus.Text = "Importing novel...";
                        });

                        if (ext == ".epub")
                        {
                            (bookId, msg) = _dbContext.ImportEpub(filename, splash.UpdateProgressBar).GetAwaiter().GetResult();
                        }
                        else if (ext == ".novel")
                        {
                            (bookId, msg) = _dbContext.ImportBookByJsonModel(filename, splash.UpdateProgressBar).GetAwaiter().GetResult();
                        }

                    }
                    , doneAction: () =>
                    {

                        if (!string.IsNullOrWhiteSpace(msg) || string.IsNullOrWhiteSpace(bookId))
                        {
                            this.ShowError(msg);
                        }
                        else
                        {
                            string existMsg = string.IsNullOrEmpty(msg) ? "" : "Already exist - ";
                            this.ShowYesNoMessageBox($"{existMsg}Do you want open this book", "Open Book?",

                                yesAction: () =>
                                {
                                    MainWindow.AppConfig.CurrentBookId = bookId;
                                    MainWindow.AppConfig.Save();
                                    MainWindow.LoadNovelData();
                                    MainWindow.UpdateUI();
                                    this.Close();
                                }
                                ,
                                noAction: () =>
                                {
                                    LoadNovels();
                                }

                            );

                        }
                    }

                    , textColor: MainWindow.AppConfig.TextColor
                    , backgroudColor: MainWindow.AppConfig.BackgroundColor
                    , isRunAsync: true
                    , isHideMainWindows: false
                    , isDeactiveMainWindow: true
                    );



            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ItemsControl itemControl && itemControl?.DataContext is NovelContent novel)
            {
                bool isSuccess = false;
                string? msg = "";

                this.RunTaskWithSplash(
                    action: (splash) =>
                    {
                        splash.UpdateStatus((processbar, txtStatus) =>
                        {
                            txtStatus.Text = "Deleting novel...";
                        });

                        (isSuccess, msg) = _dbContext.DeleteNovel(novel.BookId).GetAwaiter().GetResult();
                    }
                    ,
                    doneAction: () =>
                    {
                        if (!isSuccess)
                        {
                            this.ShowError(msg);
                        }
                        else
                        {
                            LoadNovels();
                        }
                    }
                    , textColor: MainWindow.AppConfig.TextColor
                    , backgroudColor: MainWindow.AppConfig.BackgroundColor
                    , isRunAsync: true
                    , isHideMainWindows: false
                    , isDeactiveMainWindow: true
                );


            }
        }

        private void ExportItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ItemsControl itemControl && itemControl?.DataContext is NovelContent novel)
            {
                var filename = WpfUtils.SaveFileFirst($"{novel?.BookName} - {novel?.Author}", "EPUB files (*.epub)|*.epub");

                if (!string.IsNullOrEmpty(filename))
                {

                    this.RunTaskWithSplash(
                    action: (splash, cancel) =>
                    {
                        splash.UpdateStatus((processbar, txtStatus) =>
                        {
                            txtStatus.Text = "Exporting novel...";
                        });

                        _dbContext.ExportToEpub(filename, novel.BookId, splash.UpdateProgressBar, cancel).GetAwaiter().GetResult();
                    }
                    , doneAction: () =>
                    {
                        WpfUtils.OpenFolderAndSelectFile(filename);
                    }
                    , isRunAsync: true
                    , isHideMainWindows: true
                    , textColor: MainWindow.AppConfig.TextColor
                    , backgroudColor: MainWindow.AppConfig.BackgroundColor
                    );

                }

            }


        }



    }



}
