using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Timer.Data;
using Timer.Services;

namespace Timer
{
    public partial class App : Application
    {
        private static DatabaseContext? _databaseContext;
        private static LoggingService? _loggingService;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 初始化日志服务
            _loggingService = LoggingService.Instance;
            _loggingService.Info("========================================");
            _loggingService.Info("应用程序启动");
            _loggingService.Info($"版本: 1.0.0");
            _loggingService.Info($"启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _loggingService.Info($"运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
            _loggingService.Info($"日志目录: {_loggingService.LogDirectory}");
            _loggingService.Info("========================================");
            
            // 在调试窗口输出日志目录位置
            System.Diagnostics.Debug.WriteLine($"[RacingTimer] 日志目录: {_loggingService.LogDirectory}");

            // 设置全局异常处理
            SetupExceptionHandling();

            // 统一 DatePicker 等控件的短日期显示格式为 yyyy-MM-dd
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("zh-CN").Clone();
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.DateSeparator = "-";
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // 同步设置当前 UI 线程文化（避免已创建线程仍使用旧文化）
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // WPF 绑定/格式化还会受 FrameworkElement.Language 影响，这里统一覆盖为 zh-CN
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            _loggingService.Debug("区域设置初始化完成");

            // 清理过期日志（保留30天）
            _loggingService.CleanupOldLogs(30);
            _loggingService.Debug("过期日志清理完成");

            base.OnStartup(e);
            _loggingService.Info("应用程序启动完成");
        }

        /// <summary>
        /// 设置全局异常处理
        /// </summary>
        private void SetupExceptionHandling()
        {
            // UI线程未处理异常
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // 非UI线程未处理异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // Task未处理异常
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            _loggingService?.Debug("全局异常处理器已设置");
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _loggingService?.Error("UI线程未处理异常", e.Exception);
            
            MessageBox.Show(
                $"发生未处理的错误：{e.Exception.Message}\n\n详细信息已记录到日志文件。",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true; // 阻止应用程序崩溃
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _loggingService?.Error($"非UI线程未处理异常 (IsTerminating: {e.IsTerminating})", exception);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _loggingService?.Error("Task未处理异常", e.Exception);
            e.SetObserved(); // 阻止应用程序崩溃
        }

        /// <summary>
        /// 获取日志服务实例
        /// </summary>
        public static ILoggingService GetLoggingService()
        {
            return _loggingService ?? LoggingService.Instance;
        }

        /// <summary>
        /// 获取数据库上下文实例
        /// </summary>
        public static DatabaseContext GetDatabaseContext()
        {
            if (_databaseContext == null)
            {
                _loggingService?.Debug("初始化数据库上下文...");
                
                try
                {
                    // 使用应用程序所在目录的data文件夹（适用于安装目录）
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var databasePath = Path.Combine(baseDirectory, "data", "timer.db");
                    
                    _loggingService?.Debug($"数据库路径: {databasePath}");
                    
                    _databaseContext = new DatabaseContext(databasePath, _loggingService);
                    // 同步初始化数据库表（确保在UI线程之前完成）
                    _databaseContext.CreateTablesAsync().Wait();
                    
                    _loggingService?.Info("数据库初始化完成");
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"数据库初始化失败: {ex.Message}", ex);
                    throw;
                }
            }
            return _databaseContext;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _loggingService?.Info("========================================");
            _loggingService?.Info("应用程序退出");
            _loggingService?.Info($"退出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _loggingService?.Info("========================================");
            
            _databaseContext?.Dispose();
            _loggingService?.Dispose();
            
            base.OnExit(e);
        }
    }
}

