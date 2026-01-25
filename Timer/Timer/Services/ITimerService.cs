using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 计时服务接口，提供核心计时业务逻辑，支持多组同时计时
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// 当前活跃的比赛记录（支持多个）
        /// </summary>
        IReadOnlyDictionary<int, RaceRecord> ActiveRaces { get; }

        /// <summary>
        /// 是否有正在进行的比赛
        /// </summary>
        bool HasActiveRaces { get; }

        /// <summary>
        /// 开始新的比赛（支持多组同时进行）
        /// </summary>
        /// <param name="raceGroupId">比赛分组ID</param>
        /// <param name="totalLaps">总圈数</param>
        Task<RaceRecord> StartRaceAsync(int raceGroupId, int totalLaps);

        /// <summary>
        /// 暂停指定比赛
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        Task PauseRaceAsync(int raceRecordId);

        /// <summary>
        /// 继续指定比赛
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        Task ResumeRaceAsync(int raceRecordId);

        /// <summary>
        /// 停止指定比赛
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        Task StopRaceAsync(int raceRecordId);

        /// <summary>
        /// 完成指定比赛（所有选手完成）
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        Task CompleteRaceAsync(int raceRecordId);

        /// <summary>
        /// 记录参赛者完成一圈
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        /// <param name="participantId">参赛者ID</param>
        /// <param name="passTime">通过时间</param>
        Task<LapRecord> RecordLapAsync(int raceRecordId, int participantId, DateTime passTime);

        /// <summary>
        /// 获取参赛者当前已完成圈数
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        /// <param name="participantId">参赛者ID</param>
        Task<int> GetParticipantCurrentLapAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 获取参赛者累计用时（毫秒）
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        /// <param name="participantId">参赛者ID</param>
        Task<long> GetParticipantTotalTimeAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 计算当前排名（基于累计用时）
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        /// <param name="participantId">参赛者ID</param>
        Task<int> CalculateRankAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 加载所有活跃的比赛（恢复现场）
        /// </summary>
        Task<List<RaceRecord>> LoadActiveRacesAsync();

        /// <summary>
        /// 获取指定比赛的圈次记录
        /// </summary>
        /// <param name="raceRecordId">比赛记录ID</param>
        Task<List<LapRecord>> GetLapRecordsAsync(int raceRecordId);

        #region 兼容旧接口（单比赛模式）

        /// <summary>
        /// 当前比赛记录（兼容旧接口，返回第一个活跃比赛）
        /// </summary>
        [Obsolete("请使用 ActiveRaces 属性")]
        RaceRecord? CurrentRace { get; }

        /// <summary>
        /// 比赛是否正在进行（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 HasActiveRaces 属性")]
        bool IsRunning { get; }

        /// <summary>
        /// 暂停比赛（兼容旧接口，暂停第一个活跃比赛）
        /// </summary>
        [Obsolete("请使用 PauseRaceAsync(int raceRecordId)")]
        Task PauseRaceAsync();

        /// <summary>
        /// 继续比赛（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 ResumeRaceAsync(int raceRecordId)")]
        Task ResumeRaceAsync();

        /// <summary>
        /// 停止比赛（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 StopRaceAsync(int raceRecordId)")]
        Task StopRaceAsync();

        /// <summary>
        /// 完成比赛（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 CompleteRaceAsync(int raceRecordId)")]
        Task CompleteRaceAsync();

        /// <summary>
        /// 记录圈次（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 RecordLapAsync(int raceRecordId, int participantId, DateTime passTime)")]
        Task<LapRecord> RecordLapAsync(int participantId, DateTime passTime);

        /// <summary>
        /// 获取参赛者当前圈数（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 GetParticipantCurrentLapAsync(int raceRecordId, int participantId)")]
        Task<int> GetParticipantCurrentLapAsync(int participantId);

        /// <summary>
        /// 获取参赛者总用时（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 GetParticipantTotalTimeAsync(int raceRecordId, int participantId)")]
        Task<long> GetParticipantTotalTimeAsync(int participantId);

        /// <summary>
        /// 计算排名（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 CalculateRankAsync(int raceRecordId, int participantId)")]
        Task<int> CalculateRankAsync(int participantId);

        /// <summary>
        /// 加载活跃比赛（兼容旧接口）
        /// </summary>
        [Obsolete("请使用 LoadActiveRacesAsync()")]
        Task<RaceRecord?> LoadActiveRaceAsync();

        #endregion
    }
}

