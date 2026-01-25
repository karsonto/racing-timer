using System;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个圈次记录，记录参赛者的每圈成绩
    /// </summary>
    public class LapRecord
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 关联的比赛记录ID（外键关联RaceRecords）
        /// </summary>
        public int RaceRecordId { get; set; }

        /// <summary>
        /// 关联的参赛者ID（外键关联Participants）
        /// </summary>
        public int ParticipantId { get; set; }

        /// <summary>
        /// 芯片编号（冗余字段，方便查询）
        /// </summary>
        public string? ChipNumber { get; set; }

        /// <summary>
        /// 圈数（第几圈）
        /// </summary>
        public int LapNumber { get; set; }

        /// <summary>
        /// 通过时间（绝对时间）
        /// </summary>
        public DateTime PassTime { get; set; }

        /// <summary>
        /// 本圈用时（毫秒）
        /// </summary>
        public long LapTime { get; set; }

        /// <summary>
        /// 累计用时（毫秒）
        /// </summary>
        public long TotalTime { get; set; }

        /// <summary>
        /// 当时排名（完成该圈时的排名）
        /// </summary>
        public int? Rank { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 参赛者信息（非持久化属性，用于UI显示）
        /// </summary>
        public Participant? Participant { get; set; }
    }
}

