using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Data;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 圈次记录数据访问实现
    /// </summary>
    public class LapRecordRepository : ILapRecordRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILoggingService? _loggingService;

        public LapRecordRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
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

        public async Task<LapRecord?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE Id = @Id
            ";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapLapRecord(reader);
            }

            return null;
        }

        public async Task<List<LapRecord>> GetByRaceRecordIdAsync(int raceRecordId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId
                ORDER BY PassTime ASC
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);

            var records = new List<LapRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapLapRecord(reader));
            }

            return records;
        }

        public async Task<List<LapRecord>> GetParticipantLapsAsync(int raceRecordId, int participantId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId AND ParticipantId = @ParticipantId
                ORDER BY LapNumber ASC
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", participantId);

            var records = new List<LapRecord>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(MapLapRecord(reader));
            }

            return records;
        }

        public async Task<LapRecord?> GetLatestLapAsync(int raceRecordId, int participantId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                       PassTime, LapTime, TotalTime, Rank, CreatedAt
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId AND ParticipantId = @ParticipantId
                ORDER BY LapNumber DESC
                LIMIT 1
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", participantId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapLapRecord(reader);
            }

            return null;
        }

        public async Task<long?> GetParticipantBestTimeAsync(int raceRecordId, int participantId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT MIN(LapTime)
                FROM LapRecords
                WHERE RaceRecordId = @RaceRecordId AND ParticipantId = @ParticipantId
            ";
            command.Parameters.AddWithValue("@RaceRecordId", raceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", participantId);

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : Convert.ToInt64(result);
        }

        public async Task<int> CreateAsync(LapRecord lapRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO LapRecords (RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                                       PassTime, LapTime, TotalTime, Rank, CreatedAt)
                VALUES (@RaceRecordId, @ParticipantId, @ChipNumber, @LapNumber, 
                        @PassTime, @LapTime, @TotalTime, @Rank, @CreatedAt);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@RaceRecordId", lapRecord.RaceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", lapRecord.ParticipantId);
            command.Parameters.AddWithValue("@ChipNumber", lapRecord.ChipNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LapNumber", lapRecord.LapNumber);
            command.Parameters.AddWithValue("@PassTime", lapRecord.PassTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@LapTime", lapRecord.LapTime);
            command.Parameters.AddWithValue("@TotalTime", lapRecord.TotalTime);
            command.Parameters.AddWithValue("@Rank", lapRecord.Rank ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<int> CreateBatchAsync(List<LapRecord> lapRecords)
        {
            if (lapRecords == null || lapRecords.Count == 0)
                return 0;

            var connection = await _dbContext.GetConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                int count = 0;
                foreach (var lapRecord in lapRecords)
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO LapRecords (RaceRecordId, ParticipantId, ChipNumber, LapNumber, 
                                               PassTime, LapTime, TotalTime, Rank, CreatedAt)
                        VALUES (@RaceRecordId, @ParticipantId, @ChipNumber, @LapNumber, 
                                @PassTime, @LapTime, @TotalTime, @Rank, @CreatedAt)
                    ";

                    command.Parameters.AddWithValue("@RaceRecordId", lapRecord.RaceRecordId);
                    command.Parameters.AddWithValue("@ParticipantId", lapRecord.ParticipantId);
                    command.Parameters.AddWithValue("@ChipNumber", lapRecord.ChipNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@LapNumber", lapRecord.LapNumber);
                    command.Parameters.AddWithValue("@PassTime", lapRecord.PassTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    command.Parameters.AddWithValue("@LapTime", lapRecord.LapTime);
                    command.Parameters.AddWithValue("@TotalTime", lapRecord.TotalTime);
                    command.Parameters.AddWithValue("@Rank", lapRecord.Rank ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    count += await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return count;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(LapRecord lapRecord)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE LapRecords
                SET RaceRecordId = @RaceRecordId,
                    ParticipantId = @ParticipantId,
                    ChipNumber = @ChipNumber,
                    LapNumber = @LapNumber,
                    PassTime = @PassTime,
                    LapTime = @LapTime,
                    TotalTime = @TotalTime,
                    Rank = @Rank
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", lapRecord.Id);
            command.Parameters.AddWithValue("@RaceRecordId", lapRecord.RaceRecordId);
            command.Parameters.AddWithValue("@ParticipantId", lapRecord.ParticipantId);
            command.Parameters.AddWithValue("@ChipNumber", lapRecord.ChipNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LapNumber", lapRecord.LapNumber);
            command.Parameters.AddWithValue("@PassTime", lapRecord.PassTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@LapTime", lapRecord.LapTime);
            command.Parameters.AddWithValue("@TotalTime", lapRecord.TotalTime);
            command.Parameters.AddWithValue("@Rank", lapRecord.Rank ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM LapRecords WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// 获取多个参赛者的最终成绩（最后一圈的圈数和累计用时）
        /// </summary>
        public async Task<Dictionary<int, (int Laps, long TotalTimeMs)>> GetFinalScoresAsync(IEnumerable<int> participantIds)
        {
            var result = new Dictionary<int, (int Laps, long TotalTimeMs)>();
            var idList = participantIds.ToList();
            
            if (idList.Count == 0)
                return result;

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            
            // 获取每个参赛者的最大圈数和对应的累计用时
            var placeholders = string.Join(",", idList.Select((_, i) => $"@p{i}"));
            command.CommandText = $@"
                SELECT lr.ParticipantId, lr.LapNumber, lr.TotalTime
                FROM LapRecords lr
                INNER JOIN (
                    SELECT ParticipantId, MAX(LapNumber) as MaxLap
                    FROM LapRecords
                    WHERE ParticipantId IN ({placeholders})
                    GROUP BY ParticipantId
                ) max_laps ON lr.ParticipantId = max_laps.ParticipantId AND lr.LapNumber = max_laps.MaxLap
            ";

            for (int i = 0; i < idList.Count; i++)
            {
                command.Parameters.AddWithValue($"@p{i}", idList[i]);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var participantId = reader.GetInt32(0);
                var laps = reader.GetInt32(1);
                var totalTime = reader.GetInt64(2);
                result[participantId] = (laps, totalTime);
            }

            return result;
        }

        /// <summary>
        /// 查询成绩结果（关联RaceRecords、RaceGroups、Participants）
        /// </summary>
        public async Task<List<ScoreResult>> SearchScoresAsync(ScoreSearchFilter filter)
        {
            var results = new List<ScoreResult>();
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var sql = @"
                SELECT DISTINCT
                    rr.Id as RaceRecordId,
                    p.Id as ParticipantId,
                    rr.StartTime as RaceDate,
                    rg.School,
                    rg.Grade,
                    rg.Class,
                    rg.GroupName,
                    p.Name,
                    p.Gender,
                    p.ExamNumber,
                    p.BibNumber,
                    COALESCE(scores.MaxLap, 0) as TotalLaps,
                    COALESCE(scores.TotalTime, 0) as TotalTimeMs
                FROM RaceRecords rr
                INNER JOIN RaceGroups rg ON rr.RaceGroupId = rg.Id
                INNER JOIN Participants p ON p.School = rg.School 
                    AND p.Grade = rg.Grade 
                    AND p.Class = rg.Class 
                    AND p.GroupName = rg.GroupName
                LEFT JOIN (
                    SELECT lr.RaceRecordId, lr.ParticipantId, lr.LapNumber as MaxLap, lr.TotalTime
                    FROM LapRecords lr
                    INNER JOIN (
                        SELECT RaceRecordId, ParticipantId, MAX(LapNumber) as MaxLapNumber
                        FROM LapRecords
                        GROUP BY RaceRecordId, ParticipantId
                    ) max_laps ON lr.RaceRecordId = max_laps.RaceRecordId 
                        AND lr.ParticipantId = max_laps.ParticipantId 
                        AND lr.LapNumber = max_laps.MaxLapNumber
                ) scores ON scores.RaceRecordId = rr.Id AND scores.ParticipantId = p.Id
                WHERE rr.Status = 'Completed'
            ";

            var conditions = new List<string>();
            
            if (filter.StartDate.HasValue)
            {
                conditions.Add("date(rr.StartTime) >= date(@StartDate)");
                command.Parameters.AddWithValue("@StartDate", filter.StartDate.Value.ToString("yyyy-MM-dd"));
            }
            
            if (filter.EndDate.HasValue)
            {
                conditions.Add("date(rr.StartTime) <= date(@EndDate)");
                command.Parameters.AddWithValue("@EndDate", filter.EndDate.Value.ToString("yyyy-MM-dd"));
            }
            
            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                conditions.Add("rg.School = @School");
                command.Parameters.AddWithValue("@School", filter.School);
            }
            
            if (!string.IsNullOrWhiteSpace(filter.Grade))
            {
                conditions.Add("rg.Grade = @Grade");
                command.Parameters.AddWithValue("@Grade", filter.Grade);
            }
            
            if (!string.IsNullOrWhiteSpace(filter.Class))
            {
                conditions.Add("rg.Class = @Class");
                command.Parameters.AddWithValue("@Class", filter.Class);
            }
            
            if (!string.IsNullOrWhiteSpace(filter.GroupName))
            {
                conditions.Add("rg.GroupName = @GroupName");
                command.Parameters.AddWithValue("@GroupName", filter.GroupName);
            }

            if (conditions.Count > 0)
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            sql += " ORDER BY rr.StartTime DESC, rg.School, rg.Grade, rg.Class, rg.GroupName, p.Name";
            sql += " LIMIT @PageSize OFFSET @Offset";
            
            command.Parameters.AddWithValue("@PageSize", filter.PageSize);
            command.Parameters.AddWithValue("@Offset", (filter.PageNumber - 1) * filter.PageSize);
            
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ScoreResult
                {
                    RaceRecordId = reader.GetInt32(0),
                    ParticipantId = reader.GetInt32(1),
                    RaceDate = DateTime.Parse(reader.GetString(2)),
                    School = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Grade = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Class = reader.IsDBNull(5) ? null : reader.GetString(5),
                    GroupName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Name = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Gender = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ExamNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                    BibNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                    TotalLaps = reader.GetInt32(11),
                    TotalTimeMs = reader.GetInt64(12)
                });
            }

            return results;
        }

        /// <summary>
        /// 获取成绩查询的总记录数
        /// </summary>
        public async Task<int> GetScoresTotalCountAsync(ScoreSearchFilter filter)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var sql = @"
                SELECT COUNT(DISTINCT p.Id || '-' || rr.Id)
                FROM RaceRecords rr
                INNER JOIN RaceGroups rg ON rr.RaceGroupId = rg.Id
                INNER JOIN Participants p ON p.School = rg.School 
                    AND p.Grade = rg.Grade 
                    AND p.Class = rg.Class 
                    AND p.GroupName = rg.GroupName
                WHERE rr.Status = 'Completed'
            ";

            var conditions = new List<string>();
            
            if (filter.StartDate.HasValue)
            {
                conditions.Add("date(rr.StartTime) >= date(@StartDate)");
                command.Parameters.AddWithValue("@StartDate", filter.StartDate.Value.ToString("yyyy-MM-dd"));
            }
            
            if (filter.EndDate.HasValue)
            {
                conditions.Add("date(rr.StartTime) <= date(@EndDate)");
                command.Parameters.AddWithValue("@EndDate", filter.EndDate.Value.ToString("yyyy-MM-dd"));
            }
            
            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                conditions.Add("rg.School = @School");
                command.Parameters.AddWithValue("@School", filter.School);
            }
            
            if (!string.IsNullOrWhiteSpace(filter.Grade))
            {
                conditions.Add("rg.Grade = @Grade");
                command.Parameters.AddWithValue("@Grade", filter.Grade);
            }
            
            if (!string.IsNullOrWhiteSpace(filter.Class))
            {
                conditions.Add("rg.Class = @Class");
                command.Parameters.AddWithValue("@Class", filter.Class);
            }
            
            if (!string.IsNullOrWhiteSpace(filter.GroupName))
            {
                conditions.Add("rg.GroupName = @GroupName");
                command.Parameters.AddWithValue("@GroupName", filter.GroupName);
            }

            if (conditions.Count > 0)
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            command.CommandText = sql;
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 获取成绩查询中所有学校列表
        /// </summary>
        public async Task<List<string>> GetScoreSchoolsAsync()
        {
            var results = new List<string>();
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT rg.School
                FROM RaceRecords rr
                INNER JOIN RaceGroups rg ON rr.RaceGroupId = rg.Id
                WHERE rr.Status = 'Completed' AND rg.School IS NOT NULL AND rg.School != ''
                ORDER BY rg.School
            ";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }

        /// <summary>
        /// 获取指定学校下的年级列表
        /// </summary>
        public async Task<List<string>> GetScoreGradesAsync(string school)
        {
            var results = new List<string>();
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT rg.Grade
                FROM RaceRecords rr
                INNER JOIN RaceGroups rg ON rr.RaceGroupId = rg.Id
                WHERE rr.Status = 'Completed' 
                    AND rg.School = @School 
                    AND rg.Grade IS NOT NULL AND rg.Grade != ''
                ORDER BY rg.Grade
            ";
            command.Parameters.AddWithValue("@School", school);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }

        /// <summary>
        /// 获取指定学校和年级下的班级列表
        /// </summary>
        public async Task<List<string>> GetScoreClassesAsync(string school, string grade)
        {
            var results = new List<string>();
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT rg.Class
                FROM RaceRecords rr
                INNER JOIN RaceGroups rg ON rr.RaceGroupId = rg.Id
                WHERE rr.Status = 'Completed' 
                    AND rg.School = @School 
                    AND rg.Grade = @Grade
                    AND rg.Class IS NOT NULL AND rg.Class != ''
                ORDER BY rg.Class
            ";
            command.Parameters.AddWithValue("@School", school);
            command.Parameters.AddWithValue("@Grade", grade);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }

        /// <summary>
        /// 获取指定学校、年级、班级下的组别列表
        /// </summary>
        public async Task<List<string>> GetScoreGroupNamesAsync(string school, string grade, string className)
        {
            var results = new List<string>();
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT rg.GroupName
                FROM RaceRecords rr
                INNER JOIN RaceGroups rg ON rr.RaceGroupId = rg.Id
                WHERE rr.Status = 'Completed' 
                    AND rg.School = @School 
                    AND rg.Grade = @Grade
                    AND rg.Class = @Class
                    AND rg.GroupName IS NOT NULL AND rg.GroupName != ''
                ORDER BY rg.GroupName
            ";
            command.Parameters.AddWithValue("@School", school);
            command.Parameters.AddWithValue("@Grade", grade);
            command.Parameters.AddWithValue("@Class", className);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }

        private LapRecord MapLapRecord(SqliteDataReader reader)
        {
            return new LapRecord
            {
                Id = reader.GetInt32(0),
                RaceRecordId = reader.GetInt32(1),
                ParticipantId = reader.GetInt32(2),
                ChipNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                LapNumber = reader.GetInt32(4),
                PassTime = DateTime.Parse(reader.GetString(5)),
                LapTime = reader.GetInt64(6),
                TotalTime = reader.GetInt64(7),
                Rank = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                CreatedAt = DateTime.Parse(reader.GetString(9))
            };
        }
    }
}

