using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 比赛分组导出服务实现
    /// </summary>
    public class RaceGroupExportService : IRaceGroupExportService
    {
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化导出服务
        /// </summary>
        /// <param name="loggingService">日志服务（可选）</param>
        public RaceGroupExportService(ILoggingService? loggingService = null)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// 导出比赛分组数据到Excel文件
        /// </summary>
        public async Task<bool> ExportToExcelAsync(
            IEnumerable<RaceGroup> raceGroups,
            Dictionary<int, IEnumerable<Participant>> participants,
            string filePath)
        {
            if (raceGroups == null || !raceGroups.Any())
            {
                throw new ArgumentException("分组列表不能为空", nameof(raceGroups));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("分组结果");

                // 设置表头
                var headers = new[] { "组内序号", "日期", "学校", "年级", "班级", "组别", "姓名", "性别", "准考证号", "芯片标签号码", "芯片内部号码" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int currentRow = 2;

                // 遍历每个分组，写入参赛人员数据
                foreach (var raceGroup in raceGroups)
                {
                    if (!participants.TryGetValue(raceGroup.Id, out var groupParticipants))
                    {
                        continue;
                    }

                    foreach (var participant in groupParticipants)
                    {
                        worksheet.Cell(currentRow, 1).Value = participant.GroupSequenceNumber;
                        worksheet.Cell(currentRow, 2).Value = participant.Date.ToString("yyyy-MM-dd");
                        worksheet.Cell(currentRow, 3).Value = participant.School ?? "";
                        worksheet.Cell(currentRow, 4).Value = participant.Grade ?? "";
                        worksheet.Cell(currentRow, 5).Value = participant.Class ?? "";
                        worksheet.Cell(currentRow, 6).Value = participant.GroupName ?? "";
                        worksheet.Cell(currentRow, 7).Value = participant.Name;
                        worksheet.Cell(currentRow, 8).Value = participant.Gender;
                        worksheet.Cell(currentRow, 9).Value = participant.ExamNumber ?? "";
                        worksheet.Cell(currentRow, 10).Value = participant.ChipNumber ?? "";
                        worksheet.Cell(currentRow, 11).Value = participant.ChipInternalNumber ?? "";

                        currentRow++;
                    }
                }

                // 自动调整列宽
                worksheet.Columns().AdjustToContents();

                // 保存文件
                workbook.SaveAs(filePath);

                _loggingService?.Info($"成功导出分组数据到文件: {filePath}，共 {currentRow - 2} 条记录");

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"导出分组数据失败: {ex.Message}", ex);
                throw;
            }
        }
    }
}

