using System;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个参赛人员，包含人员的基本信息和比赛相关信息
    /// </summary>
    public class Participant
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 关联的项目ID
        /// </summary>
        public int? ProjectId { get; set; }

        /// <summary>
        /// 序号，必须从1开始连续
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 日期（比赛日期）
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 学校
        /// </summary>
        public string? School { get; set; }

        /// <summary>
        /// 年级
        /// </summary>
        public string? Grade { get; set; }

        /// <summary>
        /// 班级
        /// </summary>
        public string? Class { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 性别（"男"或"女"）
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 准考证号，唯一标识（如果提供）
        /// </summary>
        public string? ExamNumber { get; set; }

        /// <summary>
        /// 组别名称
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 号码布编号（可选，后续分配）
        /// </summary>
        public string? BibNumber { get; set; }

        /// <summary>
        /// 芯片编号（可选，后续分配）
        /// </summary>
        public string? ChipNumber { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// UI选择状态（用于批量操作），不持久化到数据库
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 芯片内部号码（非持久化属性，用于UI显示）
        /// </summary>
        public string? ChipInternalNumber { get; set; }

        /// <summary>
        /// 组内序号（非持久化属性，用于UI显示）
        /// </summary>
        public int GroupSequenceNumber { get; set; }

        /// <summary>
        /// 完成的圈数（非持久化属性，用于成绩显示）
        /// </summary>
        public int TotalLaps { get; set; }

        /// <summary>
        /// 累计用时（毫秒）（非持久化属性，用于成绩显示）
        /// </summary>
        public long TotalTimeMs { get; set; }

        /// <summary>
        /// 累计用时格式化显示（精确到毫秒）
        /// </summary>
        public string TotalTimeFormatted
        {
            get
            {
                if (TotalTimeMs <= 0) return "-";
                var ts = TimeSpan.FromMilliseconds(TotalTimeMs);
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
            }
        }
    }
}

