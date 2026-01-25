using System;

namespace Timer.Models
{
    /// <summary>
    /// 成绩查询筛选条件
    /// </summary>
    public class ScoreSearchFilter
    {
        /// <summary>
        /// 比赛开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 比赛结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 学校名称筛选
        /// </summary>
        public string? School { get; set; }

        /// <summary>
        /// 年级筛选
        /// </summary>
        public string? Grade { get; set; }

        /// <summary>
        /// 班级筛选
        /// </summary>
        public string? Class { get; set; }

        /// <summary>
        /// 组别筛选
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 页码，从1开始
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页记录数
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
