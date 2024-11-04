using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace WpfLibrary
{

    public static class WpfUtils
    {
        public static void RunTaskWithSplash(this Window windows, Action<SplashScreenWindow, CancellationToken> action
        , bool isHideMainWindows = true
        , bool isTopMost = false
        , string? textColor = null, string? backgroudColor = null
        , bool IsIndeterminate = false
        )
        {
            SplashScreenWindow splash = new SplashScreenWindow(isAllowsTransparency: IsIndeterminate);
            splash.Topmost = isTopMost;
            splash.progressBar.IsIndeterminate = IsIndeterminate;
            splash.txtProgress.Visibility = IsIndeterminate ? Visibility.Hidden : Visibility.Visible;
            splash.WindowStyle = IsIndeterminate ? WindowStyle.None : WindowStyle.ToolWindow;
            splash.txtStatus.Foreground = ConvertHtmlColorToBrush(textColor);
            splash.txtProgress.Foreground = ConvertHtmlColorToBrush(textColor);
            splash.Background = ConvertHtmlColorToBrush(backgroudColor);
            if (windows.IsLoaded)
            {
                splash.Owner = windows;
            }

            splash.Show();

            if (isHideMainWindows)
            {
                windows.Hide();
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            splash.Closed += (s, e) =>
            {

                windows.Dispatcher.Invoke(() =>
                {
                    windows.Focus();
                });

                cancellationTokenSource.Cancel(); // Hủy bỏ action nếu splash bị đóng
            };

            Task.Run(() =>
            {
                try
                {
                    action(splash, token);
                }
                catch (OperationCanceledException)
                {
                    // Chuyển về UI thread để hiển thị lại cửa sổ
                    windows.Dispatcher.Invoke(() =>
                    {
                        if (isHideMainWindows)
                        {
                            windows.Show();  // Thao tác trên UI phải thực hiện trong UI thread
                        }
                    });
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {

                    splash.Close(); // Đóng SplashScreen

                    if (isHideMainWindows)
                    {
                        windows.Show();     // Hiển thị MainWindow
                    }
                });
            }, token);
        }



        public static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
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
                    T? childOfChild = FindVisualChild<T>(child!);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }


        public static void HighlightWord(TextBlock textBlock, string colorHighlight, string? text, int startIndex, int length)
        {
            if (!string.IsNullOrEmpty(text) & startIndex < text?.Length)
            {
                textBlock.Inlines.Clear();
                var isExist = text?.Substring(startIndex, length)?.Count() > 0;

                if (!isExist)
                {
                    textBlock.Inlines.Add(new Run(text));
                }
                else
                {
                    string beforeKeyword = text?.Substring(0, startIndex) ?? "";
                    string highlightedKeyword = text?.Substring(startIndex, length) ?? "";
                    string afterKeyword = text?.Substring(startIndex + length) ?? "";

                    if (!string.IsNullOrEmpty(beforeKeyword))
                    {
                        textBlock.Inlines.Add(new Run(beforeKeyword));
                    }

                    var highlightedRun = new Run(highlightedKeyword)
                    {
                        Background = WpfUtils.ConvertHtmlColorToBrush(colorHighlight),
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


        public static List<string> GetAvailableFonts()
        {
            var lstrs = new List<string>();
            // Lấy danh sách tất cả các font trên hệ thống
            var fonts = Fonts.SystemFontFamilies;

            // Ký tự tiếng Việt cần kiểm tra (có thể dùng bất kỳ ký tự nào tiếng Việt có dấu)
            string sampleText = "Ă"; // Ví dụ với ký tự tiếng Việt

            //Console.WriteLine("Danh sách các font hỗ trợ tiếng Việt:");

            foreach (var fontFamily in fonts)
            {
                // Kiểm tra xem font có thể hiển thị ký tự tiếng Việt hay không
                var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                if (typeface.TryGetGlyphTypeface(out var glyphTypeface))
                {
                    // Kiểm tra nếu ký tự có glyph tương ứng trong font
                    if (glyphTypeface.CharacterToGlyphMap.ContainsKey(sampleText[0]))
                    {
                        lstrs.Add(fontFamily.Source);
                    }
                }
            }

            return lstrs;
        }

        public static void ShowError(this Window window, string msg)
        {
            MessageBox.Show(messageBoxText: msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowYesNoMessageBox(
            this Window window,
            string msg,
            string title,
            Action? yesAction = null,
            Action? noAction = null
            )
        {
            // Hiển thị MessageBox với Yes và No
            MessageBoxResult result = MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            // Xử lý kết quả người dùng chọn Yes hoặc No
            if (result == MessageBoxResult.Yes)
            {
                yesAction?.Invoke();
            }
            else if (result == MessageBoxResult.No)
            {
                noAction?.Invoke();
            }
        }


        public static void OpenFolderAndSelectFile(string filePath)
        {
            // Kiểm tra xem file có tồn tại không
            if (System.IO.File.Exists(filePath))
            {
                // Mở thư mục chứa file và highlight file đó
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else
            {
                MessageBox.Show("File không tồn tại!");
            }
        }


        public static string SaveFileFirst(string filter = "Text file (*.txt)|*.txt")
        {
            // Tạo SaveFileDialog
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();

            // Đặt bộ lọc cho loại file (Ví dụ: Chỉ cho phép lưu file .txt hoặc .csv)
            saveFileDialog.Filter = filter;

            // Hiển thị hộp thoại và kiểm tra xem người dùng có chọn đường dẫn hay không
            if (saveFileDialog.ShowDialog() == true)
            {
                // Lấy full path của file mà người dùng chọn
                string filePath = saveFileDialog.FileName;

                if (!File.Exists(filePath))
                {
                    System.IO.File.Create(filePath).Close();
                }


                return filePath;
            }
            return "";
        }


        public static string SaveFileFirst(string? filenameDefault = null, string filter = "All file (*.*)|*.*")
        {
            // Tạo SaveFileDialog
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = filter,
                FileName = filenameDefault
            };


            // Hiển thị hộp thoại và kiểm tra xem người dùng có chọn đường dẫn hay không
            if (saveFileDialog.ShowDialog() == true)
            {
                // Lấy full path của file mà người dùng chọn
                string filePath = saveFileDialog?.FileName ?? "";

                if (!File.Exists(filePath))
                {
                    System.IO.File.Create(filePath).Close();
                }


                return filePath;
            }
            return "";
        }



        public static Brush? ConvertHtmlColorToBrush(string? htmlColor)
        {
            BrushConverter converter = new BrushConverter();
            return (Brush?)converter.ConvertFromString(htmlColor ?? "#FFFFFF");
        }

        public static string? ColorPicker()
        {
            // Sử dụng hộp thoại ColorDialog để chọn màu
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Lấy màu đã chọn từ ColorDialog
                var selectedColor = colorDialog.Color;

                // Chuyển đổi từ System.Drawing.Color sang System.Windows.Media.Color
                var mediaColor = Color.FromArgb(selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B);

                return mediaColor.ToString();
            }
            return null;
        }


        public static void SetPositionCenterParent(this Window childWindows, Window? parentWindows = null)
        {
            if (parentWindows == null)
            {
                parentWindows = childWindows.Owner;
            }
            // Lấy vị trí và kích thước của MainWindow
            double mainWindowLeft = parentWindows.Left;
            double mainWindowTop = parentWindows.Top;
            double mainWindowWidth = parentWindows.Width;
            double mainWindowHeight = parentWindows.Height;

            // Tính toán để đặt BookWindow ở giữa MainWindow
            childWindows.Left = mainWindowLeft + (mainWindowWidth - childWindows.Width) / 2;
            childWindows.Top = mainWindowTop + (mainWindowHeight - childWindows.Height) / 2;

        }


        public static string? GetFilePath(string? currentPath = null, string filter = "All Files (*.*)|*.*")
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a File",
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                return selectedFilePath;
            }
            return currentPath;
        }

        public static string? GetFolderPath(string? currentPath = null, string filter = "All Files (*.*)|*.*")
        {
            Microsoft.Win32.OpenFolderDialog openFileDialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select a Folder",
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FolderName;
                return selectedFilePath;
            }
            return currentPath;
        }

        public static void ClearAndWriteToFile(string filePath, string newContent)
        {
            // Ghi đè nội dung mới lên file
            using (StreamWriter writer = new StreamWriter(filePath, false)) // false để ghi đè
            {
                writer.Write(newContent);
            }
        }


        public static T Clone<T>(T obj) where T : class, new()
        {
            var json = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }

        public static bool IsNotNumber(string? input)
        {
            // Biểu thức chính quy để kiểm tra chuỗi chỉ chứa số (cả số nguyên và số thực)
            string pattern = @"^-?\d+(\.\d+)?$";

            // Kiểm tra nếu chuỗi không khớp với biểu thức chính quy
            return !Regex.IsMatch(input ?? "", pattern);
        }

        public static void UpdateValueSamePropName(object objNeedUpdate, object objContainValUpdate)
        {
            var propNeedUpdate = objNeedUpdate.GetType().GetProperties();
            var propContainValUpdate = objContainValUpdate.GetType().GetProperties();

            foreach (var prop in propContainValUpdate)
            {
                // Check if the property exists in objNeedUpdate
                var matchingProp = propNeedUpdate.FirstOrDefault(x => x.Name == prop.Name);
                if (matchingProp != null && matchingProp.CanWrite) // Ensure the property is writable
                {
                    // Get value from objContainValUpdate and set it to objNeedUpdate
                    var valueToSet = prop.GetValue(objContainValUpdate);
                    matchingProp.SetValue(objNeedUpdate, valueToSet);
                }
            }

        }


        public static string GetTextUntilSpace(string input, int startIndex)
        {
            // Kiểm tra nếu index hợp lệ
            if (startIndex >= 0 && startIndex < input.Length)
            {
                // Tìm vị trí ký tự space tiếp theo sau startIndex
                int spaceIndex = input.IndexOf(' ', startIndex);

                // Nếu không tìm thấy ký tự space, lấy toàn bộ phần còn lại của chuỗi
                if (spaceIndex == -1)
                {
                    return input.Substring(startIndex);
                }

                // Trích xuất chuỗi từ startIndex cho đến ký tự space
                return input.Substring(startIndex, spaceIndex - startIndex);
            }
            return string.Empty; // Trả về chuỗi rỗng nếu index không hợp lệ
        }


        public static async Task<T?> GetModelFromJsonFileAsync<T>(string jsonpath, T? input = null, string? action = null) where T : class?
        {
            if (input != null)
            {
                return input;
            }

            if (File.Exists(jsonpath))
            {
                var fileName = Path.GetFileName(jsonpath);
                action = string.IsNullOrEmpty(action) ? null : $"[{action}]";

                var json = await File.ReadAllTextAsync(jsonpath);
                Console.WriteLine($"{action}[{fileName}] Read file done.");
                var rs = JsonSerializer.Deserialize<T>(json);

                Console.WriteLine($"{action}[{fileName}] Json to model done.");
                return rs;

            }
            else
            {
                Console.WriteLine($"{action} Not found file");
            }


            return default;
        }

        public static T? GetModelFromJsonFile<T>(string jsonpath, T? input = null, string? action = null) where T : class?
        {
            if (input != null)
            {
                return input;
            }

            if (File.Exists(jsonpath))
            {
                var fileName = Path.GetFileName(jsonpath);
                action = string.IsNullOrEmpty(action) ? null : $"[{action}]";

                var json = File.ReadAllText(jsonpath);
                Console.WriteLine($"{action}[{fileName}] Read file done.");
                var rs = JsonSerializer.Deserialize<T>(json);

                Console.WriteLine($"{action}[{fileName}] Json to model done.");
                return rs;

            }
            else
            {
                Console.WriteLine($"{action} Not found file");
            }


            return default;
        }



        public static async Task SaveFile(string input, string filename)
        {
            await System.IO.File.WriteAllTextAsync(filename, input);
        }

        public static async Task ConvertJsonAndSaveFile(object? input, string filename)
        {
            if (input != null)
            {
                var json = JsonSerializer.Serialize(input);

                await System.IO.File.WriteAllTextAsync(filename, json);

            }
        }


        public static async Task<string?> DownloadImageAsBase64(string imageUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Tải ảnh từ URL
                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                    // Chuyển đổi sang Base64
                    string base64String = Convert.ToBase64String(imageBytes);

                    return base64String;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tải ảnh: {ex.Message}");
                    return null;
                }
            }
        }

        public static bool CreateFolderIfNotExist(string? path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                // Kiểm tra thư mục có tồn tại hay không
                if (!Directory.Exists(path))
                {
                    // Nếu thư mục không tồn tại, tạo mới
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }


        public static FileStream? CreateFileIfNotExist(string? path)
        {
            try
            {
                FileStream? file = null;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
                // Kiểm tra thư mục có tồn tại hay không
                if (!File.Exists(path))
                {
                    // Nếu thư mục không tồn tại, tạo mới
                    file = File.Create(path);
                }

                file?.Close();
                return file;
            }
            catch (Exception)
            {
                return null;
            }

        }



    }
}
