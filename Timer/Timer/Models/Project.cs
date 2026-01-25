using System;

namespace Timer.Models
{
    /// <summary>
    /// 项目状态枚举
    /// </summary>
    public enum ProjectStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal,
        /// <summary>
        /// 失效
        /// </summary>
        Invalid
    }

    /// <summary>
    /// 表示一个项目，包含项目的基本信息
    /// </summary>
    public class Project
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 项目日期
        /// </summary>
        public DateTime ProjectDate { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 项目状态
        /// </summary>
        public ProjectStatus Status { get; set; } = ProjectStatus.Normal;

        /// <summary>
        /// 项目状态显示文本
        /// </summary>
        public string StatusText => Status == ProjectStatus.Normal ? "正常" : "失效";

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
    }
}
