using System;

namespace Timer.Models
{
    /// <summary>
    /// 比赛记录状态枚举
    /// </summary>
    public enum RaceStatus
    {
        /// <summary>
        /// 比赛进行中
        /// </summary>
        Running,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已停止（中途终止）
        /// </summary>
        Stopped
    }

    /// <summary>
    /// 表示一场比赛的记录，包含比赛的整体信息
    /// </summary>
    public class RaceRecord
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 关联的比赛分组ID（外键关联RaceGroups）
        /// </summary>
        public int RaceGroupId { get; set; }

        /// <summary>
        /// 比赛开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 比赛结束时间（可为空，比赛进行中时为null）
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 比赛状态
        /// </summary>
        public RaceStatus Status { get; set; }

        /// <summary>
        /// 比赛总圈数
        /// </summary>
        public int TotalLaps { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 比赛分组信息（非持久化属性，用于UI显示）
        /// </summary>
        public RaceGroup? RaceGroup { get; set; }

        /// <summary>
        /// 参赛人数（非持久化属性，用于UI显示）
        /// </summary>
        public int ParticipantCount { get; set; }
    }
}

