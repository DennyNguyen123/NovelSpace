using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace NovelReader
{
    public class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Kiểm tra và chuyển đổi giá trị sang Thickness
            if (value is double margin)
            {
                return new Thickness(0, 0, 0, margin); // Margin đều cho 4 cạnh
            }
            return new Thickness(0); // Mặc định là 0 nếu không có giá trị phù hợp
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
