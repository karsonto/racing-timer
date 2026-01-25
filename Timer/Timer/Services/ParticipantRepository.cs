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
    /// 参赛人员数据访问实现
    /// </summary>
    public class ParticipantRepository : IParticipantRepository
    {
        private readonly DatabaseContext _dbContext;
        private SqliteTransaction? _transaction;
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化数据访问实现
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public ParticipantRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
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
        /// 获取参赛人员列表，支持搜索和分页
        /// </summary>
        public async Task<IEnumerable<Participant>> GetAllAsync(SearchFilter filter)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            // 构建WHERE子句
            var whereClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                whereClauses.Add("(Name LIKE @searchKeyword OR ExamNumber LIKE @searchKeyword OR BibNumber LIKE @searchKeyword)");
                parameters.Add(new SqliteParameter("@searchKeyword", $"%{filter.SearchKeyword}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.GroupName))
            {
                whereClauses.Add("GroupName = @groupName");
                parameters.Add(new SqliteParameter("@groupName", filter.GroupName));
            }

            if (!string.IsNullOrWhiteSpace(filter.Gender))
            {
                whereClauses.Add("Gender = @gender");
                parameters.Add(new SqliteParameter("@gender", filter.Gender));
            }

            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                whereClauses.Add("School = @school");
                parameters.Add(new SqliteParameter("@school", filter.School));
            }

            if (!string.IsNullOrWhiteSpace(filter.Grade))
            {
                whereClauses.Add("Grade = @grade");
                parameters.Add(new SqliteParameter("@grade", filter.Grade));
            }

            if (!string.IsNullOrWhiteSpace(filter.Class))
            {
                whereClauses.Add("Class = @class");
                parameters.Add(new SqliteParameter("@class", filter.Class));
            }

            // Date 字段在库中是字符串（yyyy-MM-dd HH:mm:ss），这里统一按“日期部分”比较，避免 EndDate 当天筛不出数据
            if (filter.StartDate.HasValue)
            {
                whereClauses.Add("substr(Date, 1, 10) >= @startDate");
                parameters.Add(new SqliteParameter("@startDate", filter.StartDate.Value.ToString("yyyy-MM-dd")));
            }

            if (filter.EndDate.HasValue)
            {
                whereClauses.Add("substr(Date, 1, 10) <= @endDate");
                parameters.Add(new SqliteParameter("@endDate", filter.EndDate.Value.ToString("yyyy-MM-dd")));
            }

            var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            // 构建SQL查询
            command.CommandText = $@"
                SELECT Id, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, BibNumber, ChipNumber, CreatedAt, UpdatedAt
                FROM Participants
                {whereClause}
                ORDER BY SequenceNumber
                LIMIT @limit OFFSET @offset
            ";

            command.Parameters.Add(new SqliteParameter("@limit", filter.PageSize));
            command.Parameters.Add(new SqliteParameter("@offset", filter.Skip));
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            LogSql("GetAllAsync", command.CommandText, $"PageSize={filter.PageSize}, Skip={filter.Skip}");

            var participants = new List<Participant>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                participants.Add(MapToParticipant(reader));
            }

            _loggingService?.Debug($"[SQL] GetAllAsync 返回 {participants.Count} 条记录");
            return participants;
        }

        /// <summary>
        /// 根据ID获取单个参赛人员
        /// </summary>
        public async Task<Participant?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, BibNumber, ChipNumber, CreatedAt, UpdatedAt
                FROM Participants
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToParticipant(reader);
            }

            return null;
        }

        /// <summary>
        /// 根据号码布编号获取参赛人员
        /// </summary>
        public async Task<Participant?> GetByBibNumberAsync(string bibNumber)
        {
            if (string.IsNullOrWhiteSpace(bibNumber))
            {
                throw new ArgumentNullException(nameof(bibNumber));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, BibNumber, ChipNumber, CreatedAt, UpdatedAt
                FROM Participants
                WHERE BibNumber = @bibNumber
            ";

            command.Parameters.Add(new SqliteParameter("@bibNumber", bibNumber));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToParticipant(reader);
            }

            return null;
        }

        /// <summary>
        /// 获取符合条件的总记录数（用于分页计算）
        /// </summary>
        public async Task<int> GetTotalCountAsync(SearchFilter filter)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            // 构建WHERE子句（与GetAllAsync相同）
            var whereClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
            {
                whereClauses.Add("(Name LIKE @searchKeyword OR ExamNumber LIKE @searchKeyword OR BibNumber LIKE @searchKeyword)");
                parameters.Add(new SqliteParameter("@searchKeyword", $"%{filter.SearchKeyword}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.GroupName))
            {
                whereClauses.Add("GroupName = @groupName");
                parameters.Add(new SqliteParameter("@groupName", filter.GroupName));
            }

            if (!string.IsNullOrWhiteSpace(filter.Gender))
            {
                whereClauses.Add("Gender = @gender");
                parameters.Add(new SqliteParameter("@gender", filter.Gender));
            }

            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                whereClauses.Add("School = @school");
                parameters.Add(new SqliteParameter("@school", filter.School));
            }

            if (!string.IsNullOrWhiteSpace(filter.Grade))
            {
                whereClauses.Add("Grade = @grade");
                parameters.Add(new SqliteParameter("@grade", filter.Grade));
            }

            if (!string.IsNullOrWhiteSpace(filter.Class))
            {
                whereClauses.Add("Class = @class");
                parameters.Add(new SqliteParameter("@class", filter.Class));
            }

            // Date 字段在库中是字符串（yyyy-MM-dd HH:mm:ss），这里统一按“日期部分”比较，避免 EndDate 当天筛不出数据
            if (filter.StartDate.HasValue)
            {
                whereClauses.Add("substr(Date, 1, 10) >= @startDate");
                parameters.Add(new SqliteParameter("@startDate", filter.StartDate.Value.ToString("yyyy-MM-dd")));
            }

            if (filter.EndDate.HasValue)
            {
                whereClauses.Add("substr(Date, 1, 10) <= @endDate");
                parameters.Add(new SqliteParameter("@endDate", filter.EndDate.Value.ToString("yyyy-MM-dd")));
            }

            var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            command.CommandText = $"SELECT COUNT(*) FROM Participants {whereClause}";
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 添加新的参赛人员
        /// </summary>
        public async Task<int> AddAsync(Participant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Participants (ProjectId, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, BibNumber, ChipNumber, CreatedAt, UpdatedAt)
                VALUES (@projectId, @sequenceNumber, @date, @school, @grade, @class, @name, @gender, @examNumber, @groupName, @bibNumber, @chipNumber, @createdAt, @updatedAt);
                SELECT last_insert_rowid();
            ";

            AddParticipantParameters(command, participant);
            command.Parameters.Add(new SqliteParameter("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// 更新参赛人员信息
        /// </summary>
        public async Task UpdateAsync(Participant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            if (participant.Id <= 0)
            {
                throw new ArgumentException("Participant Id must be greater than 0", nameof(participant));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Participants
                SET ProjectId = @projectId, SequenceNumber = @sequenceNumber, Date = @date, School = @school, Grade = @grade, Class = @class,
                    Name = @name, Gender = @gender, ExamNumber = @examNumber, GroupName = @groupName,
                    BibNumber = @bibNumber, ChipNumber = @chipNumber, UpdatedAt = @updatedAt
                WHERE Id = @id
            ";

            command.Parameters.Add(new SqliteParameter("@id", participant.Id));
            AddParticipantParameters(command, participant);
            command.Parameters.Add(new SqliteParameter("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 删除参赛人员
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0", nameof(id));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Participants WHERE Id = @id";
            command.Parameters.Add(new SqliteParameter("@id", id));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 批量删除参赛人员
        /// </summary>
        public async Task DeleteBatchAsync(IEnumerable<int> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var idList = ids.ToList();
            if (idList.Count == 0)
            {
                return;
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var placeholders = string.Join(",", idList.Select((_, i) => $"@id{i}"));
            command.CommandText = $"DELETE FROM Participants WHERE Id IN ({placeholders})";

            for (int i = 0; i < idList.Count; i++)
            {
                command.Parameters.Add(new SqliteParameter($"@id{i}", idList[i]));
            }

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 根据项目ID删除所有参赛人员
        /// </summary>
        public async Task DeleteByProjectIdAsync(int projectId)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Participants WHERE ProjectId = @projectId";
            command.Parameters.Add(new SqliteParameter("@projectId", projectId));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// 检查准考证号是否已存在
        /// </summary>
        public async Task<bool> ExistsByExamNumberAsync(string examNumber)
        {
            if (string.IsNullOrWhiteSpace(examNumber))
            {
                throw new ArgumentNullException(nameof(examNumber));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Participants WHERE ExamNumber = @examNumber";
            command.Parameters.Add(new SqliteParameter("@examNumber", examNumber));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// 检查号码布是否已存在
        /// </summary>
        public async Task<bool> ExistsByBibNumberAsync(string bibNumber)
        {
            if (string.IsNullOrWhiteSpace(bibNumber))
            {
                throw new ArgumentNullException(nameof(bibNumber));
            }

            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM Participants WHERE BibNumber = @bibNumber";
            command.Parameters.Add(new SqliteParameter("@bibNumber", bibNumber));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// 获取当前最大序号
        /// </summary>
        public async Task<int> GetMaxSequenceNumberAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "SELECT COALESCE(MAX(SequenceNumber), 0) FROM Participants";

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
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
        /// 添加Participant参数到命令
        /// </summary>
        private void AddParticipantParameters(SqliteCommand command, Participant participant)
        {
            command.Parameters.Add(new SqliteParameter("@projectId", participant.ProjectId.HasValue ? participant.ProjectId.Value : (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@sequenceNumber", participant.SequenceNumber));
            command.Parameters.Add(new SqliteParameter("@date", participant.Date.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(new SqliteParameter("@school", participant.School ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@grade", participant.Grade ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@class", participant.Class ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@name", participant.Name));
            command.Parameters.Add(new SqliteParameter("@gender", participant.Gender));
            command.Parameters.Add(new SqliteParameter("@examNumber", participant.ExamNumber ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@groupName", participant.GroupName ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@bibNumber", participant.BibNumber ?? (object)DBNull.Value));
            command.Parameters.Add(new SqliteParameter("@chipNumber", participant.ChipNumber ?? (object)DBNull.Value));
        }

        /// <summary>
        /// 获取所有唯一的学校列表
        /// </summary>
        public async Task<IEnumerable<string>> GetDistinctSchoolsAsync()
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            command.CommandText = "SELECT DISTINCT School FROM Participants WHERE School IS NOT NULL AND School != '' ORDER BY School";

            var schools = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                schools.Add(reader.GetString(0));
            }

            return schools;
        }

        /// <summary>
        /// 根据学校获取所有唯一的年级列表
        /// </summary>
        public async Task<IEnumerable<string>> GetDistinctGradesAsync(string? school)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            if (string.IsNullOrWhiteSpace(school))
            {
                command.CommandText = "SELECT DISTINCT Grade FROM Participants WHERE Grade IS NOT NULL AND Grade != '' ORDER BY Grade";
            }
            else
            {
                command.CommandText = "SELECT DISTINCT Grade FROM Participants WHERE School = @school AND Grade IS NOT NULL AND Grade != '' ORDER BY Grade";
                command.Parameters.Add(new SqliteParameter("@school", school));
            }

            var grades = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                grades.Add(reader.GetString(0));
            }

            return grades;
        }

        /// <summary>
        /// 根据学校和年级获取所有唯一的班级列表
        /// </summary>
        public async Task<IEnumerable<string>> GetDistinctClassesAsync(string? school, string? grade)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrWhiteSpace(school))
            {
                whereClauses.Add("School = @school");
                parameters.Add(new SqliteParameter("@school", school));
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                whereClauses.Add("Grade = @grade");
                parameters.Add(new SqliteParameter("@grade", grade));
            }

            var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) + " AND" : "WHERE";
            command.CommandText = $"SELECT DISTINCT Class FROM Participants {whereClause} Class IS NOT NULL AND Class != '' ORDER BY Class";

            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            var classes = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                classes.Add(reader.GetString(0));
            }

            return classes;
        }

        /// <summary>
        /// 根据学校、年级和班级获取所有唯一的组别列表
        /// </summary>
        public async Task<IEnumerable<string>> GetDistinctGroupNamesAsync(string? school, string? grade, string? classValue)
        {
            var connection = await _dbContext.GetConnectionAsync();
            var command = connection.CreateCommand();

            var whereClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrWhiteSpace(school))
            {
                whereClauses.Add("School = @school");
                parameters.Add(new SqliteParameter("@school", school));
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                whereClauses.Add("Grade = @grade");
                parameters.Add(new SqliteParameter("@grade", grade));
            }

            if (!string.IsNullOrWhiteSpace(classValue))
            {
                whereClauses.Add("Class = @class");
                parameters.Add(new SqliteParameter("@class", classValue));
            }

            var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) + " AND" : "WHERE";
            command.CommandText = $"SELECT DISTINCT GroupName FROM Participants {whereClause} GroupName IS NOT NULL AND GroupName != '' ORDER BY GroupName";

            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            var groupNames = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                groupNames.Add(reader.GetString(0));
            }

            return groupNames;
        }

        /// <summary>
        /// 从数据读取器映射到Participant对象
        /// </summary>
        private Participant MapToParticipant(SqliteDataReader reader)
        {
            return new Participant
            {
                Id = reader.GetInt32("Id"),
                SequenceNumber = reader.GetInt32("SequenceNumber"),
                Date = DateTime.Parse(reader.GetString("Date")),
                School = reader.IsDBNull("School") ? null : reader.GetString("School"),
                Grade = reader.IsDBNull("Grade") ? null : reader.GetString("Grade"),
                Class = reader.IsDBNull("Class") ? null : reader.GetString("Class"),
                Name = reader.GetString("Name"),
                Gender = reader.GetString("Gender"),
                ExamNumber = reader.IsDBNull("ExamNumber") ? null : reader.GetString("ExamNumber"),
                GroupName = reader.IsDBNull("GroupName") ? null : reader.GetString("GroupName"),
                BibNumber = reader.IsDBNull("BibNumber") ? null : reader.GetString("BibNumber"),
                ChipNumber = reader.IsDBNull("ChipNumber") ? null : reader.GetString("ChipNumber"),
                CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                UpdatedAt = DateTime.Parse(reader.GetString("UpdatedAt"))
            };
        }
    }
}

