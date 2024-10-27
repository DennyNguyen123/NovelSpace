using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WpfLibrary
{
    public class Base64ToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string base64String)
            {
                try
                {
                    // Tách bỏ prefix "data:image/png;base64," nếu có
                    if (base64String.StartsWith("data:image"))
                    {
                        int commaIndex = base64String.IndexOf(',');
                        if (commaIndex >= 0)
                        {
                            base64String = base64String.Substring(commaIndex + 1);
                        }
                    }

                    byte[] imageBytes = System.Convert.FromBase64String(base64String);
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze(); // Để tránh lỗi Binding
                        return image;
                    }
                }
                catch (Exception)
                {
                    // Xử lý lỗi nếu cần
                }
            }
            return null; // Trả về null nếu không có giá trị hợp lệ
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
