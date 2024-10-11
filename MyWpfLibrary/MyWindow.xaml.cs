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

namespace MyWpfLibrary
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MyWindow : Window, INotifyPropertyChanged
    {
        public MyWindow()
        {
            InitializeComponent();
        }

        // Phương thức để thông báo thay đổi thuộc tính
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
