using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 项目数据访问接口，定义项目数据的CRUD操作
    /// </summary>
    public interface IProjectRepository
    {
        /// <summary>
        /// 获取所有项目
        /// </summary>
        /// <returns>项目列表</returns>
        Task<IEnumerable<Project>> GetAllAsync();

        /// <summary>
        /// 根据条件查询项目
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="projectName">项目名称（可选）</param>
        /// <returns>项目列表</returns>
        Task<IEnumerable<Project>> QueryAsync(DateTime? startDate, DateTime? endDate, string? projectName);

        /// <summary>
        /// 根据ID获取单个项目
        /// </summary>
        /// <param name="id">项目ID</param>
        /// <returns>项目对象，如果不存在则返回null</returns>
        Task<Project?> GetByIdAsync(int id);

        /// <summary>
        /// 添加新项目
        /// </summary>
        /// <param name="project">项目对象</param>
        /// <returns>新创建的项目ID</returns>
        Task<int> AddAsync(Project project);

        /// <summary>
        /// 更新项目信息
        /// </summary>
        /// <param name="project">项目对象（必须包含有效的Id）</param>
        Task UpdateAsync(Project project);

        /// <summary>
        /// 删除项目
        /// </summary>
        /// <param name="id">项目ID</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// 批量删除项目
        /// </summary>
        /// <param name="ids">项目ID列表</param>
        Task BatchDeleteAsync(IEnumerable<int> ids);

        /// <summary>
        /// 获取所有不重复的项目名称
        /// </summary>
        /// <returns>项目名称列表</returns>
        Task<IEnumerable<string>> GetDistinctProjectNamesAsync();

        /// <summary>
        /// 获取所有状态为正常的项目
        /// </summary>
        /// <returns>正常状态的项目列表</returns>
        Task<IEnumerable<Project>> GetActiveProjectsAsync();
    }
}
