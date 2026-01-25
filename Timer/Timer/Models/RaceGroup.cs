using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个比赛分组，包含学校、年级、班级、组别和芯片分配信息
    /// </summary>
    public partial class RaceGroup : ObservableObject
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        [ObservableProperty]
        private int _id;

        /// <summary>
        /// 学校名称
        /// </summary>
        [ObservableProperty]
        private string _school = string.Empty;

        /// <summary>
        /// 年级
        /// </summary>
        [ObservableProperty]
        private string? _grade;

        /// <summary>
        /// 班级
        /// </summary>
        [ObservableProperty]
        private string? _class;

        /// <summary>
        /// 组别名称
        /// </summary>
        [ObservableProperty]
        private string _groupName = string.Empty;

        /// <summary>
        /// 关联的芯片组ID（外键关联ChipGroups）
        /// </summary>
        [ObservableProperty]
        private int? _chipGroupId;

        /// <summary>
        /// 比赛圈数（1-20）
        /// </summary>
        [ObservableProperty]
        private int _raceLaps = 1;

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
        /// 芯片组名称（非持久化属性，用于UI显示）
        /// </summary>
        [ObservableProperty]
        private string? _chipGroupName;

        /// <summary>
        /// 芯片组颜色（非持久化属性，用于UI显示）
        /// </summary>
        [ObservableProperty]
        private string? _chipGroupColor;

        /// <summary>
        /// 组内参赛人员数量（非持久化属性，用于UI显示）
        /// </summary>
        [ObservableProperty]
        private int _participantCount;

        /// <summary>
        /// 显示名称：学校-年级-班级-组名（用于UI显示）
        /// </summary>
        public string DisplayName => $"{School}-{Grade}-{Class}-{GroupName}";
    }
}



