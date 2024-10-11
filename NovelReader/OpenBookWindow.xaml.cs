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

namespace NovelReader
{

    public class FileItem
    {
        public string Name { get; set; }
        public string Type { get; set; } // Ví dụ: File, Folder
        public string Icon { get; set; } // Đường dẫn đến biểu tượng nếu cần
    }

    /// <summary>
    /// Interaction logic for OpenBookWindow.xaml
    /// </summary>
    public partial class OpenBookWindow : Window, INotifyPropertyChanged
    {

        public List<FileItem> Items { get; set; }

        public List<NovelContent> novelContents { get; set; }

        public MainWindow MainWindow { get; set; }

        public OpenBookWindow()
        {
            InitializeComponent();
        }


        protected override void OnContentRendered(EventArgs e)
        {
            MainWindow = (MainWindow)Owner;

            novelContents = MainWindow._AppDbContext.NovelContents.ToList();

            CardItemsControl.ItemsSource = novelContents;
            this.Background = WpfUtils.ConvertHtmlColorToBrush(MainWindow.AppConfig.BackgroundColor);

            base.OnContentRendered(e);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChange()
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(""));
        }


        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Lấy item tương ứng từ Border
            var border = sender as Border;
            if (border != null)
            {
                var dataContext = border.DataContext as NovelContent;
                if (dataContext != null)
                {
                    MainWindow.AppConfig.CurrentBookId = dataContext.BookId;
                    MainWindow.AppConfig.Save();
                    MainWindow.LoadNovelData();
                    this.Close();
                }
            }
        }
    }



}
