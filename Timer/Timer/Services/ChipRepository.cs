using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Data;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 芯片数据访问实现
    /// </summary>
    public class ChipRepository : IChipRepository
    {
        private readonly DatabaseContext _dbContext;
        private SqliteTransaction? _transaction;
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化数据访问实现
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public ChipRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _loggingService = loggingService;
        }

        /// <summary>
        /// 记录SQL执行日志
        /// </summary>
        private void LogSql(string operation, string sql, object? parameters = null)
        {
            var paramStr = parameters != null ? $", Params: {parameters}" : "";
            _loggingService?.Debug($"[SQL] {operation}: {sql.Trim().Replace("\n", " ").Replace("  ", " ")}{paramStr}");
        }

        /// <summary>
        /// 记录数据库异常
        /// </summary>
        private void LogDbError(string operation, Exception ex, string? sql = null)
        {
            var sqlInfo = sql != null ? $"\nSQL: {sql.Trim().Replace("\n", " ")}" : "";
            _loggingService?.Error($"[数据库异常] {operation} 失败: {ex.Message}{sqlInfo}", ex);
        }

        /// <summary>
        /// 获取所有芯片组，包含每个组的芯片数量
        /// </summary>
        public async Task<IEnumerable<ChipGroup>> GetAllChipGroupsAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    cg.Id,
                    cg.GroupName,
                    cg.Color,
                    cg.CreatedAt,
                    cg.UpdatedAt,
                    COALESCE(COUNT(c.Id), 0) as ChipCount
                FROM ChipGroups cg
                LEFT JOIN Chips c ON cg.Id = c.ChipGroupId
                GROUP BY cg.Id, cg.GroupName, cg.Color, cg.CreatedAt, cg.UpdatedAt
                ORDER BY cg.GroupName
            ";

            LogSql("GetAllChipGroupsAsync", command.CommandText);

            var chipGroups = new List<ChipGroup>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chipGroups.Add(MapToChipGroup(reader));
            }

            _loggingService?.Debug($"[SQL] GetAllChipGroupsAsync 返回 {chipGroups.Count} 条记录");
            return chipGroups;
        }

        /// <summary>
        /// 根据ID获取单个芯片组
        /// </summary>
        public async Task<ChipGroup?> GetChipGroupByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    Id,
                    GroupName,
                    Color,
                    CreatedAt,
                    UpdatedAt
                FROM ChipGroups
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var group = MapToChipGroupBasic(reader);
                group.ChipCount = await GetChipCountByGroupIdAsync(id);
                return group;
            }

            return null;
        }

        /// <summary>
        /// 获取指定芯片组的所有芯片
        /// </summary>
        public async Task<IEnumerable<Chip>> GetChipsByGroupIdAsync(int chipGroupId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT 
                    Id,
                    ChipGroupId,
                    LabelNumber,
                    InternalNumber,
                    CreatedAt,
                    UpdatedAt
                FROM Chips
                WHERE ChipGroupId = @chipGroupId
                ORDER BY LabelNumber
            ";

            command.Parameters.Add(new SqliteParameter("@chipGroupId", chipGroupId));

            var chips = new List<Chip>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chips.Add(MapToChip(reader));
            }

            return chips;
        }

        /// <summary>
        /// 添加新的芯片组
        /// </summary>
        public async Task<int> AddChipGroupAsync(ChipGroup group)
        {
            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO ChipGroups (GroupName, Color, CreatedAt, UpdatedAt)
                VALUES (@groupName, @color, @createdAt, @updatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.Add(new SqliteParameter("@groupName", group.GroupName));
            command.Parameters.Add(new SqliteParameter("@color", group.Color));
            command.Parameters.Add(new SqliteParameter("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 更新芯片组信息（组名、颜色）
        /// </summary>
        public async Task UpdateChipGroupAsync(ChipGroup group)
        {
            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (group.Id <= 0)
            {
                throw new ArgumentException("ChipGroup Id must be greater than 0", nameof(group));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE ChipGroups
                SET GroupName = @groupName, Color = @color, UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", group.Id));
            command.Parameters.Add(new SqliteParameter("@groupName", group.GroupName));
            command.Parameters.Add(new SqliteParameter("@color", group.Color));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 批量添加芯片
        /// </summary>
        public async Task AddChipsAsync(IEnumerable<Chip> chips)
        {
            if (chips == null)
            {
                throw new ArgumentNullException(nameof(chips));
            }

            var chipList = chips.ToList();
            if (chipList.Count == 0)
            {
                return;
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            foreach (var chip in chipList)
            {
                command.Parameters.Clear();
                command.CommandText = @"
                    INSERT INTO Chips (ChipGroupId, LabelNumber, InternalNumber, CreatedAt, UpdatedAt)
                    VALUES (@chipGroupId, @labelNumber, @internalNumber, @createdAt, @updatedAt)
                ";

                command.Parameters.Add(new SqliteParameter("@chipGroupId", chip.ChipGroupId));
                command.Parameters.Add(new SqliteParameter("@labelNumber", chip.LabelNumber));
                command.Parameters.Add(new SqliteParameter("@internalNumber", chip.InternalNumber));
                command.Parameters.Add(new SqliteParameter("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// 更新芯片信息
        /// </summary>
        public async Task UpdateChipAsync(Chip chip)
        {
            if (chip == null)
            {
                throw new ArgumentNullException(nameof(chip));
            }

            if (chip.Id <= 0)
            {
                throw new ArgumentException("Chip Id must be greater than 0", nameof(chip));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Chips
                SET ChipGroupId = @chipGroupId, LabelNumber = @labelNumber, InternalNumber = @internalNumber, UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", chip.Id));
            command.Parameters.Add(new SqliteParameter("@chipGroupId", chip.ChipGroupId));
            command.Parameters.Add(new SqliteParameter("@labelNumber", chip.LabelNumber));
            command.Parameters.Add(new SqliteParameter("@internalNumber", chip.InternalNumber));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 删除芯片组
        /// </summary>
        public async Task DeleteChipGroupAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(id));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM ChipGroups WHERE Id = @id";
            command.Parameters.Add(new SqliteParameter("@id", id));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 删除单个芯片
        /// </summary>
        public async Task DeleteChipAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(id));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Chips WHERE Id = @id";
            command.Parameters.Add(new SqliteParameter("@id", id));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 获取指定芯片组的芯片数量
        /// </summary>
        public async Task<int> GetChipCountByGroupIdAsync(int chipGroupId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Chips WHERE ChipGroupId = @chipGroupId";
            command.Parameters.Add(new SqliteParameter("@chipGroupId", chipGroupId));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 检查芯片标签号码是否已存在
        /// </summary>
        public async Task<bool> ExistsByLabelNumberAsync(string labelNumber)
        {
            if (string.IsNullOrWhiteSpace(labelNumber))
            {
                throw new ArgumentNullException(nameof(labelNumber));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Chips WHERE LabelNumber = @labelNumber";
            command.Parameters.Add(new SqliteParameter("@labelNumber", labelNumber));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// 清空所有芯片组和芯片数据
        /// </summary>
        public async Task DeleteAllChipGroupsAndChipsAsync()
        {
            _loggingService?.Info("[数据清空] 开始清空芯片相关数据...");
            
            try
            {
                var connection = await _dbContext.GetConnectionAsync();
                
                // 按外键依赖顺序删除数据：
                // LapRecords -> RaceRecords -> RaceGroups -> Chips -> ChipGroups
                
                // 1. 删除所有圈次记录
                var deleteLapRecordsCommand = connection.CreateCommand();
                deleteLapRecordsCommand.CommandText = "DELETE FROM LapRecords";
                LogSql("DeleteAllChipGroupsAndChipsAsync", deleteLapRecordsCommand.CommandText);
                var lapRecordsDeleted = await deleteLapRecordsCommand.ExecuteNonQueryAsync();
                _loggingService?.Debug($"[SQL] DELETE FROM LapRecords, 删除 {lapRecordsDeleted} 条记录");
                
                // 2. 删除所有比赛记录
                var deleteRaceRecordsCommand = connection.CreateCommand();
                deleteRaceRecordsCommand.CommandText = "DELETE FROM RaceRecords";
                LogSql("DeleteAllChipGroupsAndChipsAsync", deleteRaceRecordsCommand.CommandText);
                var raceRecordsDeleted = await deleteRaceRecordsCommand.ExecuteNonQueryAsync();
                _loggingService?.Debug($"[SQL] DELETE FROM RaceRecords, 删除 {raceRecordsDeleted} 条记录");
                
                // 3. 删除所有比赛分组
                var deleteRaceGroupsCommand = connection.CreateCommand();
                deleteRaceGroupsCommand.CommandText = "DELETE FROM RaceGroups";
                LogSql("DeleteAllChipGroupsAndChipsAsync", deleteRaceGroupsCommand.CommandText);
                var raceGroupsDeleted = await deleteRaceGroupsCommand.ExecuteNonQueryAsync();
                _loggingService?.Debug($"[SQL] DELETE FROM RaceGroups, 删除 {raceGroupsDeleted} 条记录");
                
                // 4. 删除所有芯片
                var deleteChipsCommand = connection.CreateCommand();
                deleteChipsCommand.CommandText = "DELETE FROM Chips";
                LogSql("DeleteAllChipGroupsAndChipsAsync", deleteChipsCommand.CommandText);
                var chipsDeleted = await deleteChipsCommand.ExecuteNonQueryAsync();
                _loggingService?.Debug($"[SQL] DELETE FROM Chips, 删除 {chipsDeleted} 条记录");
                
                // 5. 删除所有芯片组
                var deleteGroupsCommand = connection.CreateCommand();
                deleteGroupsCommand.CommandText = "DELETE FROM ChipGroups";
                LogSql("DeleteAllChipGroupsAndChipsAsync", deleteGroupsCommand.CommandText);
                var groupsDeleted = await deleteGroupsCommand.ExecuteNonQueryAsync();
                _loggingService?.Debug($"[SQL] DELETE FROM ChipGroups, 删除 {groupsDeleted} 条记录");
                
                _loggingService?.Info($"[数据清空] 完成，共删除: LapRecords={lapRecordsDeleted}, RaceRecords={raceRecordsDeleted}, RaceGroups={raceGroupsDeleted}, Chips={chipsDeleted}, ChipGroups={groupsDeleted}");
            }
            catch (Exception ex)
            {
                LogDbError("DeleteAllChipGroupsAndChipsAsync", ex);
                throw; // 重新抛出异常，让调用者处理
            }
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            _transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// 从数据读取器映射到ChipGroup对象（包含芯片数量）
        /// </summary>
        private ChipGroup MapToChipGroup(SqliteDataReader reader)
        {
            return new ChipGroup
            {
                Id = reader.GetInt32("Id"),
                GroupName = reader.GetString("GroupName"),
                Color = reader.GetString("Color"),
                CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                UpdatedAt = DateTime.Parse(reader.GetString("UpdatedAt")),
                ChipCount = reader.GetInt32("ChipCount")
            };
        }

        /// <summary>
        /// 从数据读取器映射到ChipGroup对象（不包含芯片数量）
        /// </summary>
        private ChipGroup MapToChipGroupBasic(SqliteDataReader reader)
        {
            return new ChipGroup
            {
                Id = reader.GetInt32("Id"),
                GroupName = reader.GetString("GroupName"),
                Color = reader.GetString("Color"),
                CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                UpdatedAt = DateTime.Parse(reader.GetString("UpdatedAt"))
            };
        }

        /// <summary>
        /// 从数据读取器映射到Chip对象
        /// </summary>
        private Chip MapToChip(SqliteDataReader reader)
        {
            return new Chip
            {
                Id = reader.GetInt32("Id"),
                ChipGroupId = reader.GetInt32("ChipGroupId"),
                LabelNumber = reader.GetString("LabelNumber"),
                InternalNumber = reader.GetString("InternalNumber"),
                CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                UpdatedAt = DateTime.Parse(reader.GetString("UpdatedAt"))
            };
        }
    }
}

