using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个芯片组，包含芯片组的基本信息和颜色
    /// </summary>
    public partial class ChipGroup : ObservableObject
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        [ObservableProperty]
        private int _id;

        /// <summary>
        /// 芯片组名称，唯一
        /// </summary>
        [ObservableProperty]
        private string _groupName = string.Empty;

        /// <summary>
        /// 芯片组颜色（存储为ARGB十六进制字符串，如"#FF1890FF"）
        /// </summary>
        [ObservableProperty]
        private string _color = "#FF1890FF"; // Default to blue

        /// <summary>
        /// 创建时间
        /// </summary>
        [ObservableProperty]
        private DateTime _createdAt;

        /// <summary>
        /// 更新时间
        /// </summary>
        [ObservableProperty]
        private DateTime _updatedAt;

        /// <summary>
        /// 芯片数量（非持久化属性，用于UI显示）
        /// </summary>
        [ObservableProperty]
        private int _chipCount;
    }
}

