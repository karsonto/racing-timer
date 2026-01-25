using System;

namespace Timer.Models
{
    /// <summary>
    /// 成绩查询结果模型
    /// </summary>
    public class ScoreResult
    {
        /// <summary>
        /// 比赛记录ID
        /// </summary>
        public int RaceRecordId { get; set; }

        /// <summary>
        /// 参赛者ID
        /// </summary>
        public int ParticipantId { get; set; }

        /// <summary>
        /// 比赛日期（比赛开始时间）
        /// </summary>
        public DateTime RaceDate { get; set; }

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
        /// 组别
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// 准考证号
        /// </summary>
        public string? ExamNumber { get; set; }

        /// <summary>
        /// 芯片外部号码（号码布）
        /// </summary>
        public string? BibNumber { get; set; }

        /// <summary>
        /// 完成圈数
        /// </summary>
        public int TotalLaps { get; set; }

        /// <summary>
        /// 累计用时（毫秒）
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

        /// <summary>
        /// 比赛日期格式化显示
        /// </summary>
        public string RaceDateFormatted => RaceDate.ToString("yyyy-M-d");
    }
}
