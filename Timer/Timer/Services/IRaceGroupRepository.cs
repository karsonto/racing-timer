using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 比赛分组数据访问接口，定义分组数据的CRUD操作
    /// </summary>
    public interface IRaceGroupRepository
    {
        /// <summary>
        /// 根据条件查询比赛分组（关联查询参赛人数和芯片组信息）
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="school">学校（必填）</param>
        /// <param name="grade">年级（可选）</param>
        /// <param name="classValue">班级（可选）</param>
        /// <param name="groupName">组别（可选）</param>
        /// <returns>符合条件的分组列表</returns>
        Task<IEnumerable<RaceGroup>> QueryRaceGroupsAsync(
            System.DateTime? startDate,
            System.DateTime? endDate,
            string? school,
            string? grade = null,
            string? classValue = null,
            string? groupName = null);

        /// <summary>
        /// 获取所有比赛分组
        /// </summary>
        /// <returns>所有分组列表</returns>
        Task<List<RaceGroup>> GetAllAsync();

        /// <summary>
        /// 根据ID获取单个分组
        /// </summary>
        /// <param name="id">分组ID</param>
        /// <returns>分组对象，如果不存在则返回null</returns>
        Task<RaceGroup?> GetByIdAsync(int id);

        /// <summary>
        /// 获取或创建分组记录（如果不存在则自动创建）
        /// </summary>
        /// <param name="school">学校</param>
        /// <param name="grade">年级</param>
        /// <param name="classValue">班级</param>
        /// <param name="groupName">组别</param>
        /// <returns>分组对象</returns>
        Task<RaceGroup> GetOrCreateRaceGroupAsync(
            string school,
            string? grade,
            string? classValue,
            string groupName);

        /// <summary>
        /// 更新分组的配置（芯片组 + 圈数）
        /// </summary>
        /// <param name="id">分组ID</param>
        /// <param name="chipGroupId">芯片组ID</param>
        /// <param name="raceLaps">圈数</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateRaceGroupSettingsAsync(int id, int chipGroupId, int raceLaps);

        /// <summary>
        /// 兼容旧接口：仅更新芯片组（圈数保持不变）
        /// </summary>
        /// <param name="id">分组ID</param>
        /// <param name="chipGroupId">芯片组ID</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateChipGroupAsync(int id, int chipGroupId);

        /// <summary>
        /// 为分组内的所有参赛人员分配芯片
        /// </summary>
        /// <param name="raceGroupId">分组ID</param>
        /// <param name="chipGroupId">芯片组ID</param>
        /// <returns>成功分配的人员数量</returns>
        Task<int> AssignChipsToParticipantsAsync(int raceGroupId, int chipGroupId);

        /// <summary>
        /// 获取分组内的所有参赛人员
        /// </summary>
        /// <param name="school">学校</param>
        /// <param name="grade">年级</param>
        /// <param name="classValue">班级</param>
        /// <param name="groupName">组别</param>
        /// <returns>参赛人员列表</returns>
        Task<IEnumerable<Participant>> GetParticipantsByGroupAsync(
            string school,
            string? grade,
            string? classValue,
            string groupName);
    }
}



