using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 芯片数据访问接口，定义芯片组和芯片数据的CRUD操作
    /// </summary>
    public interface IChipRepository
    {
        /// <summary>
        /// 获取所有芯片组，包含每个组的芯片数量
        /// </summary>
        /// <returns>芯片组列表</returns>
        Task<IEnumerable<ChipGroup>> GetAllChipGroupsAsync();

        /// <summary>
        /// 根据ID获取单个芯片组
        /// </summary>
        /// <param name="id">芯片组ID</param>
        /// <returns>芯片组对象，如果不存在则返回null</returns>
        Task<ChipGroup?> GetChipGroupByIdAsync(int id);

        /// <summary>
        /// 获取指定芯片组的所有芯片
        /// </summary>
        /// <param name="chipGroupId">芯片组ID</param>
        /// <returns>芯片列表</returns>
        Task<IEnumerable<Chip>> GetChipsByGroupIdAsync(int chipGroupId);

        /// <summary>
        /// 添加新的芯片组
        /// </summary>
        /// <param name="group">芯片组对象</param>
        /// <returns>新创建的芯片组ID</returns>
        Task<int> AddChipGroupAsync(ChipGroup group);

        /// <summary>
        /// 更新芯片组信息（组名、颜色）
        /// </summary>
        /// <param name="group">芯片组对象（必须包含有效的Id）</param>
        Task UpdateChipGroupAsync(ChipGroup group);

        /// <summary>
        /// 批量添加芯片
        /// </summary>
        /// <param name="chips">要添加的芯片列表</param>
        Task AddChipsAsync(IEnumerable<Chip> chips);

        /// <summary>
        /// 更新芯片信息
        /// </summary>
        /// <param name="chip">芯片对象（必须包含有效的Id）</param>
        Task UpdateChipAsync(Chip chip);

        /// <summary>
        /// 删除芯片组
        /// </summary>
        /// <param name="id">芯片组ID</param>
        Task DeleteChipGroupAsync(int id);

        /// <summary>
        /// 删除单个芯片
        /// </summary>
        /// <param name="id">芯片ID</param>
        Task DeleteChipAsync(int id);

        /// <summary>
        /// 获取指定芯片组的芯片数量
        /// </summary>
        /// <param name="chipGroupId">芯片组ID</param>
        /// <returns>芯片数量</returns>
        Task<int> GetChipCountByGroupIdAsync(int chipGroupId);

        /// <summary>
        /// 检查芯片标签号码是否已存在
        /// </summary>
        /// <param name="labelNumber">芯片标签号码</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        Task<bool> ExistsByLabelNumberAsync(string labelNumber);

        /// <summary>
        /// 清空所有芯片组和芯片数据
        /// </summary>
        Task DeleteAllChipGroupsAndChipsAsync();

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
    }
}

