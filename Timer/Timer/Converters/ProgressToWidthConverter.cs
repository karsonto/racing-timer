using System;
using System.Globalization;
using System.Windows.Data;

namespace Timer.Converters
{
    /// <summary>
    /// 将进度百分比（0-100）转换为宽度值
    /// </summary>
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0.0;
            
            if (values[0] is double percentage && values[1] is double maxWidth)
            {
                return maxWidth * percentage / 100.0;
            }
            
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将进度百分比（0-100）转换为固定容器的宽度
    /// </summary>
    public class PercentageToWidthConverter : IValueConverter
    {
        /// <summary>
        /// 容器的最大宽度（默认120像素）
        /// </summary>
        public double MaxWidth { get; set; } = 120;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                return MaxWidth * percentage / 100.0;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

