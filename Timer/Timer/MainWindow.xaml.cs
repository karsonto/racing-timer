using System.Windows;
using Timer.Services;
using Timer.ViewModels;

namespace Timer
{
    /// <summary>
    /// 主窗口
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        /// <summary>
        /// 初始化MainWindow实例
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            var navigationService = new NavigationService();
            var dbContext = App.GetDatabaseContext();
            var loggingService = App.GetLoggingService();
            _viewModel = new MainViewModel(navigationService, dbContext, loggingService);
            DataContext = _viewModel;
        }

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}

