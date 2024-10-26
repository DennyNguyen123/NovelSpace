using Microsoft.Playwright;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfLibrary;

namespace NovelGetter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class GetterMainWindow : Window
    {

        public IPlaywright? _playwright;
        public IBrowser? _browser;
        public IBrowserContext? _browserContext;
        public PageGotoOptions? _pageGotoOption;

        public GetterAppConfig? _getterAppConfig;

        public List<HostGetter>? _lstHostGetter;

        public GetterMainWindow()
        {
            InitializeComponent();

            this.RunTaskWithSplash((splash) =>
            {
                _getterAppConfig = GetterAppConfig.Get();
                _lstHostGetter = HostGetter.GetList(_getterAppConfig?.ListHostSavePath);
                FirstLoad().GetAwaiter().GetResult();

            });
        }



        public async Task<bool?> FirstLoad()
        {
            // Khởi tạo Playwright và trình duyệt Chromium
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = _getterAppConfig?.PathBrowser,
                Headless = _getterAppConfig?.IsHeadless // Chạy chế độ headless
            });



            // Define mobile emulation settings (e.g., for an iPhone 11)
            var device = _playwright.Devices[_getterAppConfig?.BrowserDevice ?? "iPhone 11"];


            // Create a new browser context with mobile settings
            _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = device.ViewportSize,
                UserAgent = device.UserAgent,
                HasTouch = true, // Enable touch support
                IsMobile = true // Emulate mobile device
            });

            return true;
        }



    }
}