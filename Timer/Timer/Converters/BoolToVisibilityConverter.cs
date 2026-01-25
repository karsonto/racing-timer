using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Timer.Converters
{
    /// <summary>
    /// 布尔值到可见性转换器，用于将bool值或整数转换为Visibility枚举值
    /// 支持 ConverterParameter="Invert" 参数或 IsInverted 属性来反转逻辑
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 是否反转结果
        /// </summary>
        public bool IsInverted { get; set; }

        /// <summary>
        /// 将bool值或整数转换为Visibility
        /// </summary>
        /// <param name="value">bool值或整数</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，"Invert"表示反转逻辑</param>
        /// <param name="culture">区域信息</param>
        /// <returns>true/非零返回Visible，false/零返回Collapsed（可通过Invert参数反转）</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = false;
            
            if (value is bool boolValue)
            {
                isVisible = boolValue;
            }
            else if (value is int intValue)
            {
                isVisible = intValue > 0;
            }
            else if (value is long longValue)
            {
                isVisible = longValue > 0;
            }
            
            // 如果参数是 "Invert" 或 IsInverted 为 true，则反转可见性
            if (IsInverted || (parameter is string param && param == "Invert"))
            {
                isVisible = !isVisible;
            }
            
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 将Visibility值转换回bool值
        /// </summary>
        /// <param name="value">Visibility值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域信息</param>
        /// <returns>Visible返回true，其他返回false</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                var result = visibility == Visibility.Visible;
                return IsInverted ? !result : result;
            }
            return false;
        }
    }
}

