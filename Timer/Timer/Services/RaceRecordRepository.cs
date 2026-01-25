using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Data;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 比赛记录数据访问实现
    /// </summary>
    public class RaceRecordRepository : IRaceRecordRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILoggingService? _loggingService;

        public RaceRecordRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
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

        public async Task<RaceRecord?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, StartTime, EndTime, Status, TotalLaps, CreatedAt
                FROM RaceRecords
                WHERE Id = @Id
            ";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapRaceRecord(reader);
            }

            return null;
        }

        public async Task<List<RaceRecord>> GetAllAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, StartTime, EndTime, Status, TotalLaps, CreatedAt
                FROM RaceRecords
                ORDER BY StartTime DESC
            ";

            var records = new List<RaceRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapRaceRecord(reader));
            }

            return records;
        }

        public async Task<List<RaceRecord>> GetByRaceGroupIdAsync(int raceGroupId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, StartTime, EndTime, Status, TotalLaps, CreatedAt
                FROM RaceRecords
                WHERE RaceGroupId = @RaceGroupId
                ORDER BY StartTime DESC
            ";
            command.Parameters.AddWithValue("@RaceGroupId", raceGroupId);

            var records = new List<RaceRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapRaceRecord(reader));
            }

            return records;
        }

        public async Task<RaceRecord?> GetActiveRaceAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, StartTime, EndTime, Status, TotalLaps, CreatedAt
                FROM RaceRecords
                WHERE Status IN ('Running', 'Paused')
                ORDER BY StartTime DESC
                LIMIT 1
            ";

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapRaceRecord(reader);
            }

            return null;
        }

        public async Task<List<RaceRecord>> GetActiveRacesAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceGroupId, StartTime, EndTime, Status, TotalLaps, CreatedAt
                FROM RaceRecords
                WHERE Status IN ('Running', 'Paused')
                ORDER BY StartTime DESC
            ";

            var records = new List<RaceRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapRaceRecord(reader));
            }

            return records;
        }

        public async Task<int> CreateAsync(RaceRecord raceRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO RaceRecords (RaceGroupId, StartTime, EndTime, Status, TotalLaps, CreatedAt)
                VALUES (@RaceGroupId, @StartTime, @EndTime, @Status, @TotalLaps, @CreatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@RaceGroupId", raceRecord.RaceGroupId);
            command.Parameters.AddWithValue("@StartTime", raceRecord.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@EndTime", raceRecord.EndTime?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", raceRecord.Status.ToString());
            command.Parameters.AddWithValue("@TotalLaps", raceRecord.TotalLaps);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(RaceRecord raceRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE RaceRecords
                SET RaceGroupId = @RaceGroupId,
                    StartTime = @StartTime,
                    EndTime = @EndTime,
                    Status = @Status,
                    TotalLaps = @TotalLaps
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", raceRecord.Id);
            command.Parameters.AddWithValue("@RaceGroupId", raceRecord.RaceGroupId);
            command.Parameters.AddWithValue("@StartTime", raceRecord.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@EndTime", raceRecord.EndTime?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", raceRecord.Status.ToString());
            command.Parameters.AddWithValue("@TotalLaps", raceRecord.TotalLaps);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RaceRecords WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        private RaceRecord MapRaceRecord(SqliteDataReader reader)
        {
            return new RaceRecord
            {
                Id = reader.GetInt32(0),
                RaceGroupId = reader.GetInt32(1),
                StartTime = DateTime.Parse(reader.GetString(2)),
                EndTime = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
                Status = Enum.Parse<RaceStatus>(reader.GetString(4)),
                TotalLaps = reader.GetInt32(5),
                CreatedAt = DateTime.Parse(reader.GetString(6))
            };
        }
    }
}

