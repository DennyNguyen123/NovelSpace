using DataSharedLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace NovelReader
{


    public static class WpfUtils
    {

        public static void RunTaskWithSplash(this Window windows, Action action, Action? doneAction = null, bool isHideManWindows = true, bool isRunAsync = true, string? textColor = null, string? backgroudColor = null)
        {

            SplashScreenWindow splash = new SplashScreenWindow();

            splash.txtStatus.Foreground = ConvertHtmlColorToBrush(textColor);
            splash.Background = ConvertHtmlColorToBrush(backgroudColor);
            SetPositionCenterParent(splash, windows);
            splash.Show();

            if (isHideManWindows)
            {
                windows.Hide();
            }

            var task = new Task(() =>
            {
                action();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    splash.Close(); // Đóng SplashScreen
                    doneAction?.Invoke();
                    if (isHideManWindows)
                    {
                        windows.Show();     // Hiển thị MainWindow
                    }

                });

            });


            if (isRunAsync)
            {
                task.Start();
            }
            else
            {
                task.RunSynchronously();
            }
        }


        public static void ShowError(this Window window, string msg)
        {
            MessageBox.Show(messageBoxText: msg, "Error", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Error);
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

        public static void ClearRAM()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static Brush ConvertHtmlColorToBrush(string? htmlColor)
        {
            BrushConverter converter = new BrushConverter();
            return (Brush)converter.ConvertFromString(htmlColor ?? "#FFFFFF");
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
