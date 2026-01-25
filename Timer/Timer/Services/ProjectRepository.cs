using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Timer.Data;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 项目数据访问实现
    /// </summary>
    public class ProjectRepository : IProjectRepository
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILoggingService? _loggingService;

        public ProjectRepository(DatabaseContext dbContext, ILoggingService? loggingService = null)
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

        public async Task<IEnumerable<Project>> GetAllAsync()
        {
            var projects = new List<Project>();
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, ProjectDate, Name, Status, CreatedAt, UpdatedAt
                FROM Projects
                ORDER BY ProjectDate DESC, Name";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                projects.Add(MapProject(reader));
            }

            return projects;
        }

        public async Task<IEnumerable<Project>> QueryAsync(DateTime? startDate, DateTime? endDate, string? projectName)
        {
            var projects = new List<Project>();
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            var sql = @"
                SELECT Id, ProjectDate, Name, Status, CreatedAt, UpdatedAt
                FROM Projects
                WHERE 1=1";

            if (startDate.HasValue)
            {
                sql += " AND date(ProjectDate) >= date(@StartDate)";
                command.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("yyyy-MM-dd"));
            }

            if (endDate.HasValue)
            {
                sql += " AND date(ProjectDate) <= date(@EndDate)";
                command.Parameters.AddWithValue("@EndDate", endDate.Value.ToString("yyyy-MM-dd"));
            }

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                sql += " AND Name = @ProjectName";
                command.Parameters.AddWithValue("@ProjectName", projectName);
            }

            sql += " ORDER BY ProjectDate DESC, Name";
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                projects.Add(MapProject(reader));
            }

            return projects;
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, ProjectDate, Name, Status, CreatedAt, UpdatedAt
                FROM Projects
                WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapProject(reader);
            }

            return null;
        }

        public async Task<int> AddAsync(Project project)
        {
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Projects (ProjectDate, Name, Status, CreatedAt, UpdatedAt)
                VALUES (@ProjectDate, @Name, @Status, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();";

            var now = DateTime.Now;
            command.Parameters.AddWithValue("@ProjectDate", project.ProjectDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Name", project.Name);
            command.Parameters.AddWithValue("@Status", project.Status.ToString());
            command.Parameters.AddWithValue("@CreatedAt", now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@UpdatedAt", now.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateAsync(Project project)
        {
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Projects
                SET ProjectDate = @ProjectDate,
                    Name = @Name,
                    Status = @Status,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Id", project.Id);
            command.Parameters.AddWithValue("@ProjectDate", project.ProjectDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Name", project.Name);
            command.Parameters.AddWithValue("@Status", project.Status.ToString());
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Projects WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task BatchDeleteAsync(IEnumerable<int> ids)
        {
            var connection = await _dbContext.GetConnectionAsync();

            foreach (var id in ids)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Projects WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<IEnumerable<string>> GetDistinctProjectNamesAsync()
        {
            var names = new List<string>();
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT Name
                FROM Projects
                WHERE Name IS NOT NULL AND Name != ''
                ORDER BY Name";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }

        public async Task<IEnumerable<Project>> GetActiveProjectsAsync()
        {
            var projects = new List<Project>();
            var connection = await _dbContext.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, ProjectDate, Name, Status, CreatedAt, UpdatedAt
                FROM Projects
                WHERE Status = 'Normal' OR Status IS NULL
                ORDER BY ProjectDate DESC, Name";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                projects.Add(MapProject(reader));
            }

            return projects;
        }

        private static Project MapProject(SqliteDataReader reader)
        {
            // 处理 Status 字段可能为 NULL 的情况（旧数据）
            var status = ProjectStatus.Normal;
            if (!reader.IsDBNull(3))
            {
                var statusStr = reader.GetString(3);
                status = statusStr == "Invalid" ? ProjectStatus.Invalid : ProjectStatus.Normal;
            }
            
            return new Project
            {
                Id = reader.GetInt32(0),
                ProjectDate = DateTime.Parse(reader.GetString(1)),
                Name = reader.GetString(2),
                Status = status,
                CreatedAt = DateTime.Parse(reader.GetString(4)),
                UpdatedAt = DateTime.Parse(reader.GetString(5))
            };
        }
    }
}
