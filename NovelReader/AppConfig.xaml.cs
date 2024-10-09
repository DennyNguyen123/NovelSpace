using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Text.Json;
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
    public partial class AppConfigWindows : Window, INotifyPropertyChanged
    {
        public List<string> ListVoiceAvaible { get; set; }
        public MainWindow MainWindow { get; set; }

        public AppConfigWindows()
        {
            var speech = new SpeechSynthesizer();
            ListVoiceAvaible = speech.GetInstalledVoices().Select(x => x.VoiceInfo.Name).ToList();
            InitializeComponent();

            DataContext = this;

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnContentRendered(EventArgs e)
        {
            UpdateUI();
            base.OnContentRendered(e);
        }


        public void UpdateUI()
        {
            MainWindow = (MainWindow)this.Owner;
            cdBackgroundColor.Fill = WpfUtils.ConvertHtmlColorToBrush(MainWindow.AppConfig.BackgroundColor);
            cdTextColor.Fill = WpfUtils.ConvertHtmlColorToBrush(MainWindow.AppConfig.TextColor);
            cdParagrapthColor.Fill = WpfUtils.ConvertHtmlColorToBrush(MainWindow.AppConfig.SelectedParagraphColor);
            cdCurTextColor.Fill = WpfUtils.ConvertHtmlColorToBrush(MainWindow.AppConfig.CurrentTextColor);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
        private void ChoosePathBook_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.AppConfig.FolderTemp = WpfUtils.GetFolderPath(MainWindow.AppConfig.FolderTemp ?? "");
            OnPropertyChanged("");


        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.AppConfig.Save();

                MainWindow.speechSynthesizer.Rate = MainWindow.AppConfig.VoiceRate;
                MainWindow.speechSynthesizer.Volume = MainWindow.AppConfig.VoiceVolumn;
                MainWindow.UpdateUI();
                MessageBox.Show("Save successfully");
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cdTextColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cl = WpfUtils.ColorPicker();
            if (!string.IsNullOrEmpty(cl))
            {
                MainWindow.AppConfig.TextColor = cl;
                cdTextColor.Fill = WpfUtils.ConvertHtmlColorToBrush(cl);
                OnPropertyChanged("");
            }
        }

        private void cdBackgroundColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cl = WpfUtils.ColorPicker();
            if (!string.IsNullOrEmpty(cl))
            {
                MainWindow.AppConfig.BackgroundColor = cl;
                cdBackgroundColor.Fill = WpfUtils.ConvertHtmlColorToBrush(cl);
                OnPropertyChanged("");
            }
        }


        private void cdParagrapthColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cl = WpfUtils.ColorPicker();
            if (!string.IsNullOrEmpty(cl))
            {
                MainWindow.AppConfig.SelectedParagraphColor = cl;
                cdParagrapthColor.Fill = WpfUtils.ConvertHtmlColorToBrush(cl);
                OnPropertyChanged("");
            }
        }

        private void cdCurTextColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cl = WpfUtils.ColorPicker();
            if (!string.IsNullOrEmpty(cl))
            {
                MainWindow.AppConfig.CurrentTextColor = cl;
                cdCurTextColor.Fill = WpfUtils.ConvertHtmlColorToBrush(cl);
                OnPropertyChanged("");
            }
        }
    }
}
