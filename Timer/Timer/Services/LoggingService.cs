using System;
using System.IO;
using System.Text;

namespace Timer.Services
{
    /// <summary>
    /// 日志服务实现，支持按天轮转日志文件
    /// </summary>
    public class LoggingService : ILoggingService, IDisposable
    {
        private static LoggingService? _instance;
        private static readonly object _lock = new object();
        
        private readonly string _logDirectory;
        private readonly object _writeLock = new object();
        private StreamWriter? _currentWriter;
        private string _currentLogDate = string.Empty;
        private bool _disposed;

        /// <summary>
        /// 日志级别
        /// </summary>
        public enum LogLevel
        {
            Trace = 0,
            Debug = 1,
            Info = 2,
            Warn = 3,
            Error = 4
        }

        /// <summary>
        /// 最低日志级别（低于此级别的日志不会记录）
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// 是否同时输出到控制台
        /// </summary>
        public bool WriteToConsole { get; set; } = true;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // 优先使用应用程序目录，如果失败则使用用户本地数据目录
                            string logDirectory;
                            try
                            {
                                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                                logDirectory = Path.Combine(baseDirectory, "logs");
                                
                                // 测试是否可以创建目录
                                if (!Directory.Exists(logDirectory))
                                {
                                    Directory.CreateDirectory(logDirectory);
                                }
                                
                                // 测试是否可以写入文件
                                var testFile = Path.Combine(logDirectory, ".write_test");
                                File.WriteAllText(testFile, "test");
                                File.Delete(testFile);
                            }
                            catch
                            {
                                // 如果应用目录不可写，使用用户本地数据目录
                                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                                logDirectory = Path.Combine(localAppData, "RacingTimer", "logs");
                            }
                            
                            _instance = new LoggingService(logDirectory);
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 获取日志目录路径
        /// </summary>
        public string LogDirectory => _logDirectory;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logDirectory">日志目录路径</param>
        public LoggingService(string logDirectory)
        {
            _logDirectory = logDirectory;
            
            // 确保日志目录存在
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch (Exception ex)
            {
                // 如果创建目录失败，输出到调试窗口
                System.Diagnostics.Debug.WriteLine($"[LoggingService] Failed to create log directory: {_logDirectory}, Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录跟踪级别的日志
        /// </summary>
        public void Trace(string message)
        {
            WriteLog(LogLevel.Trace, message);
        }

        /// <summary>
        /// 记录调试级别的日志
        /// </summary>
        public void Debug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        /// <summary>
        /// 记录信息级别的日志
        /// </summary>
        public void Info(string message)
        {
            WriteLog(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录警告级别的日志
        /// </summary>
        public void Warn(string message)
        {
            WriteLog(LogLevel.Warn, message);
        }

        /// <summary>
        /// 记录错误级别的日志
        /// </summary>
        public void Error(string message, Exception? exception = null)
        {
            var fullMessage = exception != null 
                ? $"{message}\nException: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}"
                : message;
            WriteLog(LogLevel.Error, fullMessage);
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        private void WriteLog(LogLevel level, string message)
        {
            if (level < MinimumLevel)
                return;

            var timestamp = DateTime.Now;
            var logDate = timestamp.ToString("yyyy-MM-dd");
            var logTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelString = level.ToString().ToUpper().PadRight(5);
            var logEntry = $"[{logTime}] [{levelString}] {message}";

            // 输出到 Visual Studio 调试窗口
            System.Diagnostics.Debug.WriteLine(logEntry);

            // 输出到控制台（WPF WinExe 模式下可能不可见）
            if (WriteToConsole)
            {
                try
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = GetConsoleColor(level);
                    Console.WriteLine(logEntry);
                    Console.ForegroundColor = originalColor;
                }
                catch
                {
                    // 忽略控制台输出错误
                }
            }

            // 写入文件
            lock (_writeLock)
            {
                try
                {
                    EnsureLogFile(logDate);
                    _currentWriter?.WriteLine(logEntry);
                    _currentWriter?.Flush();
                }
                catch (Exception ex)
                {
                    // 日志写入失败时输出到控制台
                    Console.WriteLine($"[LOG ERROR] Failed to write log: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 确保日志文件可用（按天轮转）
        /// </summary>
        private void EnsureLogFile(string logDate)
        {
            if (_currentLogDate != logDate || _currentWriter == null)
            {
                // 关闭旧的文件流
                _currentWriter?.Dispose();
                
                // 创建新的日志文件
                _currentLogDate = logDate;
                var logFileName = $"timer_{logDate}.log";
                var logFilePath = Path.Combine(_logDirectory, logFileName);
                
                var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _currentWriter = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true };
            }
        }

        /// <summary>
        /// 根据日志级别获取控制台颜色
        /// </summary>
        private static ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => ConsoleColor.DarkGray,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// 清理过期日志文件（保留指定天数）
        /// </summary>
        /// <param name="retentionDays">保留天数，默认30天</param>
        public void CleanupOldLogs(int retentionDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var logFiles = Directory.GetFiles(_logDirectory, "timer_*.log");
                
                foreach (var filePath in logFiles)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        try
                        {
                            fileInfo.Delete();
                            Info($"已删除过期日志文件: {fileInfo.Name}");
                        }
                        catch (Exception ex)
                        {
                            Warn($"删除过期日志文件失败: {fileInfo.Name}, 错误: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"清理过期日志失败", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_writeLock)
                    {
                        _currentWriter?.Dispose();
                        _currentWriter = null;
                    }
                }
                _disposed = true;
            }
        }

        ~LoggingService()
        {
            Dispose(false);
        }
    }
}
