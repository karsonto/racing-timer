using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 比赛分组导出服务接口
    /// </summary>
    public interface IRaceGroupExportService
    {
        /// <summary>
        /// 导出比赛分组数据到Excel文件
        /// </summary>
        /// <param name="raceGroups">要导出的分组列表</param>
        /// <param name="participants">分组对应的参赛人员列表（按分组分组）</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>是否导出成功</returns>
        Task<bool> ExportToExcelAsync(
            IEnumerable<RaceGroup> raceGroups,
            Dictionary<int, IEnumerable<Participant>> participants,
            string filePath);
    }
}




