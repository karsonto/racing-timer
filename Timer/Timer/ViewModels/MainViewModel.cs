using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timer.Data;
using Timer.Models;
using Timer.Services;
using Timer.ViewModels;

namespace Timer.ViewModels
{
    /// <summary>
    /// 主窗口的ViewModel，管理导航菜单和页面切换
    /// </summary>
    public class MainViewModel : ObservableObject, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly DatabaseContext _dbContext;
        private readonly ILoggingService _loggingService;
        private NavigationItem? _selectedMenuItem;
        private object? _currentView;
        private bool _disposed;

        /// <summary>
        /// 初始化MainViewModel实例
        /// </summary>
        /// <param name="navigationService">导航服务实例</param>
        /// <param name="dbContext">数据库上下文实例</param>
        /// <param name="loggingService">日志服务实例</param>
        /// <exception cref="ArgumentNullException">当navigationService为null时抛出</exception>
        public MainViewModel(INavigationService navigationService, DatabaseContext dbContext, ILoggingService loggingService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            _loggingService.Info("MainViewModel 初始化开始");
            
            MenuItems = new ObservableCollection<NavigationItem>();
            NavigateCommand = new RelayCommand<NavigationItem>(NavigateTo);
            ToggleExpandCommand = new RelayCommand<NavigationItem>(ToggleExpand);
            InitializeMenuItems();
            
            _loggingService.Info("MainViewModel 初始化完成");
        }

        public ObservableCollection<NavigationItem> MenuItems { get; }

        public NavigationItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set => SetProperty(ref _selectedMenuItem, value);
        }

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public IRelayCommand<NavigationItem> NavigateCommand { get; }
        public IRelayCommand<NavigationItem> ToggleExpandCommand { get; }

        private void InitializeMenuItems()
        {
            // 比赛计时（使用新的多组并行模式）
            var raceTimerItem = new NavigationItem
            {
                Title = "比赛计时",
                Icon = "⏱️",
                IconColor = "#f97316",  // 橙色
                ViewModel = CreateMultiRaceTimerViewModel()
            };
            raceTimerItem.Command = NavigateCommand;

            // 成绩管理
            var scoreItem = new NavigationItem
            {
                Title = "成绩管理",
                Icon = "📊",
                IconColor = "#22c55e",  // 绿色
                ViewModel = CreateScoreViewModel()
            };
            scoreItem.Command = NavigateCommand;

            // 人员管理
            var participantManagementItem = new NavigationItem
            {
                Title = "人员管理",
                Icon = "👥",
                IconColor = "#a855f7",  // 紫色
                ViewModel = null
            };

            var participantItem = new NavigationItem
            {
                Title = "参赛人员",
                Icon = "👤",
                IconColor = "#ec4899",  // 粉色
                ViewModel = CreateParticipantViewModel()
            };
            participantItem.Command = NavigateCommand;

            var groupItem = new NavigationItem
            {
                Title = "人员分组",
                Icon = "📋",
                IconColor = "#8b5cf6",  // 浅紫色
                ViewModel = CreateGroupViewModel()
            };
            groupItem.Command = NavigateCommand;

            var projectItem = new NavigationItem
            {
                Title = "项目管理",
                Icon = "📁",
                IconColor = "#f59e0b",  // 琥珀色
                ViewModel = CreateProjectViewModel()
            };
            projectItem.Command = NavigateCommand;

            participantManagementItem.Children.Add(projectItem);
            participantManagementItem.Children.Add(participantItem);
            participantManagementItem.Children.Add(groupItem);
            participantManagementItem.Command = ToggleExpandCommand;

            // 设备管理
            var deviceManagementItem = new NavigationItem
            {
                Title = "设备管理",
                Icon = "🔧",
                IconColor = "#06b6d4",  // 青色
                ViewModel = null
            };

            var deviceItem = new NavigationItem
            {
                Title = "扫描设备",
                Icon = "📡",
                IconColor = "#14b8a6",  // 蓝绿色
                ViewModel = new DeviceViewModel()
            };
            deviceItem.Command = NavigateCommand;

            var chipItem = new NavigationItem
            {
                Title = "芯片设备",
                Icon = "💳",
                IconColor = "#3b82f6",  // 蓝色
                ViewModel = CreateChipViewModel()
            };
            chipItem.Command = NavigateCommand;

            deviceManagementItem.Children.Add(deviceItem);
            deviceManagementItem.Children.Add(chipItem);
            deviceManagementItem.Command = ToggleExpandCommand;

            MenuItems.Add(raceTimerItem);
            MenuItems.Add(scoreItem);
            MenuItems.Add(participantManagementItem);
            MenuItems.Add(deviceManagementItem);

            // 应用启动默认选中“比赛计时”
            NavigateTo(raceTimerItem);
        }

        /// <summary>
        /// 导航到指定菜单项对应的页面
        /// </summary>
        /// <param name="item">导航菜单项</param>
        private void NavigateTo(NavigationItem? item)
        {
            if (item == null || item.ViewModel == null)
                return;

            try
            {
                _loggingService.Debug($"导航到: {item.Title}");
                
                // Clear previous selection
                if (SelectedMenuItem != null)
                {
                    SelectedMenuItem.IsSelected = false;
                }

                // Set new selection
                item.IsSelected = true;
                SelectedMenuItem = item;

                // Navigate to view
                var view = _navigationService.GetView(item.ViewModel);
                if (view == null)
                {
                    _loggingService.Warn($"导航失败: 无法获取视图 - {item.Title}");
                    // Navigation failed - revert selection
                    item.IsSelected = false;
                    if (SelectedMenuItem != null)
                    {
                        SelectedMenuItem.IsSelected = true;
                    }
                    return;
                }

                CurrentView = view;
                _loggingService.Info($"导航成功: {item.Title}");
            }
            catch (Exception ex)
            {
                _loggingService.Error($"导航异常: {item.Title}", ex);
                
                // Revert selection on error
                if (SelectedMenuItem != null)
                {
                    SelectedMenuItem.IsSelected = true;
                }
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// 切换菜单项的展开/折叠状态
        /// </summary>
        /// <param name="item">导航菜单项</param>

        private void ToggleExpand(NavigationItem? item)
        {
            if (item == null || !item.HasChildren)
                return;

            item.IsExpanded = !item.IsExpanded;
        }

        /// <summary>
        /// 创建ParticipantViewModel实例（带依赖注入）
        /// </summary>
        private ParticipantViewModel CreateParticipantViewModel()
        {
            _loggingService.Debug("创建 ParticipantViewModel");
            var repository = new ParticipantRepository(_dbContext, _loggingService);
            var excelImportService = new ExcelImportService(repository);
            var projectRepository = new ProjectRepository(_dbContext, _loggingService);
            return new ParticipantViewModel(repository, excelImportService, projectRepository, _dbContext, _loggingService);
        }

        /// <summary>
        /// 创建ChipViewModel实例（带依赖注入）
        /// </summary>
        private ChipViewModel CreateChipViewModel()
        {
            _loggingService.Debug("创建 ChipViewModel");
            var repository = new ChipRepository(_dbContext, _loggingService);
            var chipImportService = new ChipImportService(repository, _loggingService);
            return new ChipViewModel(repository, chipImportService, _dbContext, _loggingService);
        }

        /// <summary>
        /// 创建GroupViewModel实例（带依赖注入）
        /// </summary>
        private GroupViewModel CreateGroupViewModel()
        {
            _loggingService.Debug("创建 GroupViewModel");
            var participantRepo = new ParticipantRepository(_dbContext, _loggingService);
            var chipRepo = new ChipRepository(_dbContext, _loggingService);
            var raceGroupRepo = new RaceGroupRepository(_dbContext, _loggingService);
            var exportService = new RaceGroupExportService();
            return new GroupViewModel(participantRepo, chipRepo, raceGroupRepo, exportService, _loggingService);
        }

        /// <summary>
        /// 创建ProjectViewModel实例（带依赖注入）
        /// </summary>
        private ProjectViewModel CreateProjectViewModel()
        {
            _loggingService.Debug("创建 ProjectViewModel");
            var repository = new ProjectRepository(_dbContext, _loggingService);
            return new ProjectViewModel(repository, _loggingService);
        }

        /// <summary>
        /// 创建ScoreViewModel实例（带依赖注入）
        /// </summary>
        private ScoreViewModel CreateScoreViewModel()
        {
            _loggingService.Debug("创建 ScoreViewModel");
            var lapRecordRepository = new LapRecordRepository(_dbContext, _loggingService);
            return new ScoreViewModel(lapRecordRepository, _dbContext, _loggingService);
        }

        /// <summary>
        /// 创建RaceTimerViewModel实例（带依赖注入）- 旧版单组模式
        /// </summary>
        private RaceTimerViewModel CreateRaceTimerViewModel()
        {
            _loggingService.Debug("创建 RaceTimerViewModel");
            var raceGroupRepo = new RaceGroupRepository(_dbContext, _loggingService);
            var participantRepo = new ParticipantRepository(_dbContext, _loggingService);
            var raceRecordRepo = new RaceRecordRepository(_dbContext, _loggingService);
            var lapRecordRepo = new LapRecordRepository(_dbContext, _loggingService);
            var timerService = new TimerService(raceRecordRepo, lapRecordRepo);
            return new RaceTimerViewModel(raceGroupRepo, participantRepo, timerService, _dbContext);
        }

        /// <summary>
        /// 创建MultiRaceTimerViewModel实例（带依赖注入）- 新版多组并行模式
        /// </summary>
        private MultiRaceTimerViewModel CreateMultiRaceTimerViewModel()
        {
            _loggingService.Debug("创建 MultiRaceTimerViewModel");
            var raceGroupRepo = new RaceGroupRepository(_dbContext, _loggingService);
            var participantRepo = new ParticipantRepository(_dbContext, _loggingService);
            var raceRecordRepo = new RaceRecordRepository(_dbContext, _loggingService);
            var lapRecordRepo = new LapRecordRepository(_dbContext, _loggingService);
            var timerService = new TimerService(raceRecordRepo, lapRecordRepo);
            return new MultiRaceTimerViewModel(raceGroupRepo, participantRepo, timerService, _loggingService);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    // RelayCommand会自动处理，这里可以添加其他清理逻辑
                    MenuItems.Clear();
                    _currentView = null;
                    _selectedMenuItem = null;
                }

                _disposed = true;
            }
        }
    }
}

