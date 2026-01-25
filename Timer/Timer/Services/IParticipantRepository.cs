using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 数据访问接口，定义参赛人员数据的CRUD操作
    /// </summary>
    public interface IParticipantRepository
    {
        /// <summary>
        /// 获取参赛人员列表，支持搜索和分页
        /// </summary>
        /// <param name="filter">搜索筛选条件，包含搜索关键词、筛选条件、分页参数</param>
        /// <returns>人员列表</returns>
        Task<IEnumerable<Participant>> GetAllAsync(SearchFilter filter);

        /// <summary>
        /// 根据ID获取单个参赛人员
        /// </summary>
        /// <param name="id">人员ID</param>
        /// <returns>人员对象，如果不存在则返回null</returns>
        Task<Participant?> GetByIdAsync(int id);

        /// <summary>
        /// 根据号码布编号获取参赛人员
        /// </summary>
        /// <param name="bibNumber">号码布编号</param>
        /// <returns>人员对象，如果不存在则返回null</returns>
        Task<Participant?> GetByBibNumberAsync(string bibNumber);

        /// <summary>
        /// 获取符合条件的总记录数（用于分页计算）
        /// </summary>
        /// <param name="filter">搜索筛选条件（分页参数忽略）</param>
        /// <returns>总记录数</returns>
        Task<int> GetTotalCountAsync(SearchFilter filter);

        /// <summary>
        /// 添加新的参赛人员
        /// </summary>
        /// <param name="participant">人员对象</param>
        /// <returns>新创建的人员ID</returns>
        Task<int> AddAsync(Participant participant);

        /// <summary>
        /// 更新参赛人员信息
        /// </summary>
        /// <param name="participant">人员对象（必须包含有效的Id）</param>
        Task UpdateAsync(Participant participant);

        /// <summary>
        /// 删除参赛人员
        /// </summary>
        /// <param name="id">人员ID</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// 批量删除参赛人员
        /// </summary>
        /// <param name="ids">人员ID列表</param>
        Task DeleteBatchAsync(IEnumerable<int> ids);

        /// <summary>
        /// 根据项目ID删除所有参赛人员
        /// </summary>
        /// <param name="projectId">项目ID</param>
        Task DeleteByProjectIdAsync(int projectId);

        /// <summary>
        /// 检查准考证号是否已存在
        /// </summary>
        /// <param name="examNumber">准考证号</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        Task<bool> ExistsByExamNumberAsync(string examNumber);

        /// <summary>
        /// 检查号码布是否已存在
        /// </summary>
        /// <param name="bibNumber">号码布编号</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        Task<bool> ExistsByBibNumberAsync(string bibNumber);

        /// <summary>
        /// 获取当前最大序号
        /// </summary>
        /// <returns>最大序号，如果数据库为空则返回0</returns>
        Task<int> GetMaxSequenceNumberAsync();

        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// 获取所有唯一的学校列表
        /// </summary>
        Task<IEnumerable<string>> GetDistinctSchoolsAsync();

        /// <summary>
        /// 根据学校获取所有唯一的年级列表
        /// </summary>
        /// <param name="school">学校名称</param>
        Task<IEnumerable<string>> GetDistinctGradesAsync(string? school);

        /// <summary>
        /// 根据学校和年级获取所有唯一的班级列表
        /// </summary>
        /// <param name="school">学校名称</param>
        /// <param name="grade">年级</param>
        Task<IEnumerable<string>> GetDistinctClassesAsync(string? school, string? grade);

        /// <summary>
        /// 根据学校、年级和班级获取所有唯一的组别列表
        /// </summary>
        /// <param name="school">学校名称</param>
        /// <param name="grade">年级</param>
        /// <param name="classValue">班级</param>
        Task<IEnumerable<string>> GetDistinctGroupNamesAsync(string? school, string? grade, string? classValue);
    }
}

