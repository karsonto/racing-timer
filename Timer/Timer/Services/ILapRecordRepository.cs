using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 圈次记录数据访问接口
    /// </summary>
    public interface ILapRecordRepository
    {
        /// <summary>
        /// 根据ID获取圈次记录
        /// </summary>
        Task<LapRecord?> GetByIdAsync(int id);

        /// <summary>
        /// 根据比赛记录ID获取所有圈次记录
        /// </summary>
        Task<List<LapRecord>> GetByRaceRecordIdAsync(int raceRecordId);

        /// <summary>
        /// 获取指定参赛者在指定比赛中的所有圈次记录
        /// </summary>
        Task<List<LapRecord>> GetParticipantLapsAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 获取指定参赛者在指定比赛中的最新圈次记录
        /// </summary>
        Task<LapRecord?> GetLatestLapAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 获取指定参赛者在指定比赛中的最佳单圈时间
        /// </summary>
        Task<long?> GetParticipantBestTimeAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 创建圈次记录
        /// </summary>
        Task<int> CreateAsync(LapRecord lapRecord);

        /// <summary>
        /// 批量创建圈次记录
        /// </summary>
        Task<int> CreateBatchAsync(List<LapRecord> lapRecords);

        /// <summary>
        /// 更新圈次记录
        /// </summary>
        Task<bool> UpdateAsync(LapRecord lapRecord);

        /// <summary>
        /// 删除圈次记录
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 获取多个参赛者的最终成绩（最后一圈的圈数和累计用时）
        /// </summary>
        /// <param name="participantIds">参赛者ID列表</param>
        /// <returns>字典：key为参赛者ID，value为(圈数, 累计用时毫秒)</returns>
        Task<Dictionary<int, (int Laps, long TotalTimeMs)>> GetFinalScoresAsync(IEnumerable<int> participantIds);

        /// <summary>
        /// 查询成绩结果（关联RaceRecords、RaceGroups、Participants）
        /// </summary>
        Task<List<ScoreResult>> SearchScoresAsync(ScoreSearchFilter filter);

        /// <summary>
        /// 获取成绩查询的总记录数
        /// </summary>
        Task<int> GetScoresTotalCountAsync(ScoreSearchFilter filter);

        /// <summary>
        /// 获取成绩查询中所有学校列表
        /// </summary>
        Task<List<string>> GetScoreSchoolsAsync();

        /// <summary>
        /// 获取指定学校下的年级列表
        /// </summary>
        Task<List<string>> GetScoreGradesAsync(string school);

        /// <summary>
        /// 获取指定学校和年级下的班级列表
        /// </summary>
        Task<List<string>> GetScoreClassesAsync(string school, string grade);

        /// <summary>
        /// 获取指定学校、年级、班级下的组别列表
        /// </summary>
        Task<List<string>> GetScoreGroupNamesAsync(string school, string grade, string className);
    }
}

