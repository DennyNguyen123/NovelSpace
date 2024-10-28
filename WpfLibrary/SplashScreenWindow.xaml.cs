using System;
using System.Collections.Generic;
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

namespace WpfLibrary
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow(bool isAllowsTransparency = true)
        {
            this.AllowsTransparency = isAllowsTransparency;
            
            InitializeComponent();
        }

        public void UpdateProgressBar(double value)
        {
            // Check if the current thread is different from the UI thread
            if (!this.Dispatcher.CheckAccess())
            {
                // If the current thread is not the UI thread, invoke the update on the UI thread
                this.Dispatcher.Invoke(() =>
                {
                    this.progressBar.Value = value;
                });
            }
            else
            {
                // If the current thread is the UI thread, update directly
                this.progressBar.Value = value;
            }
        }



        public void UpdateStatus(Action<ProgressBar, TextBlock> action)
        {
            // Check if the current thread is different from the UI thread
            if (!this.Dispatcher.CheckAccess())
            {
                // If the current thread is not the UI thread, invoke the update on the UI thread
                this.Dispatcher.Invoke(() =>
                {
                    action(this.progressBar, this.txtStatus);
                });
            }
            else
            {
                // If the current thread is the UI thread, update directly
                action(this.progressBar, this.txtStatus);
            }
        }
    }
}
