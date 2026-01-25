using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 比赛记录数据访问接口
    /// </summary>
    public interface IRaceRecordRepository
    {
        /// <summary>
        /// 根据ID获取比赛记录
        /// </summary>
        Task<RaceRecord?> GetByIdAsync(int id);

        /// <summary>
        /// 获取所有比赛记录
        /// </summary>
        Task<List<RaceRecord>> GetAllAsync();

        /// <summary>
        /// 根据比赛分组ID获取比赛记录列表
        /// </summary>
        Task<List<RaceRecord>> GetByRaceGroupIdAsync(int raceGroupId);

        /// <summary>
        /// 获取当前正在进行的比赛记录（状态为Running或Paused）
        /// </summary>
        Task<RaceRecord?> GetActiveRaceAsync();

        /// <summary>
        /// 获取所有正在进行的比赛记录（状态为Running或Paused）
        /// </summary>
        Task<List<RaceRecord>> GetActiveRacesAsync();

        /// <summary>
        /// 创建比赛记录
        /// </summary>
        Task<int> CreateAsync(RaceRecord raceRecord);

        /// <summary>
        /// 更新比赛记录
        /// </summary>
        Task<bool> UpdateAsync(RaceRecord raceRecord);

        /// <summary>
        /// 删除比赛记录
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}

