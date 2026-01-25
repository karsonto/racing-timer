using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Timer.Data;
using Timer.Messages;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 成绩管理页面的ViewModel
    /// </summary>
    public class ScoreViewModel : ObservableObject, IDisposable, IRecipient<DataReloadRequestedMessage>
    {
        private const string AllOption = "全部";
        
        private readonly ILapRecordRepository _lapRecordRepository;
        private readonly ILoggingService? _loggingService;
        private readonly DatabaseContext _dbContext;
        private bool _disposed;

        private ObservableCollection<ScoreResult> _scores = new();
        private ScoreResult? _selectedScore;
        private ScoreSearchFilter _searchFilter = new();
        private bool _isLoading;
        private int _totalCount;
        private int _currentPage = 1;
        private int _totalPages;

        // 日期范围
        private DateTime? _startDate;
        private DateTime? _endDate;

        // 级联下拉框数据源
        private ObservableCollection<string> _schools = new();
        private ObservableCollection<string> _grades = new();
        private ObservableCollection<string> _classes = new();
        private ObservableCollection<string> _groupNames = new();

        /// <summary>
        /// 初始化ScoreViewModel实例
        /// </summary>
        public ScoreViewModel(
            ILapRecordRepository lapRecordRepository,
            DatabaseContext dbContext,
            ILoggingService? loggingService = null)
        {
            _lapRecordRepository = lapRecordRepository ?? throw new ArgumentNullException(nameof(lapRecordRepository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _loggingService = loggingService;

            Title = "成绩管理";
            ExportCommand = new AsyncRelayCommand(ExportToExcelAsync);
            SearchCommand = new RelayCommand(Search);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            RefreshCommand = new AsyncRelayCommand(LoadScoresAsync);
            LoadSchoolsCommand = new AsyncRelayCommand(LoadSchoolsAsync);
            SchoolChangedCommand = new AsyncRelayCommand<string>(OnSchoolChangedAsync);
            GradeChangedCommand = new AsyncRelayCommand<string>(OnGradeChangedAsync);
            ClassChangedCommand = new AsyncRelayCommand<string>(OnClassChangedAsync);

            WeakReferenceMessenger.Default.Register<DataReloadRequestedMessage>(this);

            // 初始化时加载数据
            _ = LoadSchoolsAsync();
            _ = LoadScoresAsync();
        }

        public void Receive(DataReloadRequestedMessage message)
        {
            if (message == null) return;

            if (message.Value == DataDomain.Participants || message.Value == DataDomain.RaceGroups)
            {
                _ = LoadScoresAsync();
                _ = LoadSchoolsAsync();
            }
        }

        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 当前页的成绩列表
        /// </summary>
        public ObservableCollection<ScoreResult> Scores
        {
            get => _scores;
            set => SetProperty(ref _scores, value);
        }

        /// <summary>
        /// 当前选中的成绩
        /// </summary>
        public ScoreResult? SelectedScore
        {
            get => _selectedScore;
            set => SetProperty(ref _selectedScore, value);
        }

        /// <summary>
        /// 搜索筛选条件
        /// </summary>
        public ScoreSearchFilter SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    if (value != null)
                    {
                        _startDate = value.StartDate;
                        _endDate = value.EndDate;
                        OnPropertyChanged(nameof(StartDate));
                        OnPropertyChanged(nameof(EndDate));
                    }
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    UpdateTotalPages();
                }
            }
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    _ = LoadScoresAsync();
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 导出Excel文件命令
        /// </summary>
        public IAsyncRelayCommand ExportCommand { get; }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public IRelayCommand SearchCommand { get; }

        /// <summary>
        /// 清除搜索命令
        /// </summary>
        public IRelayCommand ClearSearchCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public IRelayCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public IRelayCommand NextPageCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public IAsyncRelayCommand RefreshCommand { get; }

        /// <summary>
        /// 加载学校列表命令
        /// </summary>
        public IAsyncRelayCommand LoadSchoolsCommand { get; }

        /// <summary>
        /// 学校选择改变命令
        /// </summary>
        public IAsyncRelayCommand<string> SchoolChangedCommand { get; }

        /// <summary>
        /// 年级选择改变命令
        /// </summary>
        public IAsyncRelayCommand<string> GradeChangedCommand { get; }

        /// <summary>
        /// 班级选择改变命令
        /// </summary>
        public IAsyncRelayCommand<string> ClassChangedCommand { get; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    SearchFilter.StartDate = value;
                }
            }
        }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    SearchFilter.EndDate = value;
                }
            }
        }

        /// <summary>
        /// 选中的学校
        /// </summary>
        public string? SelectedSchool
        {
            get => SearchFilter.School == null ? AllOption : SearchFilter.School;
            set
            {
                var actualValue = value == AllOption ? null : value;
                if (SearchFilter.School != actualValue)
                {
                    _ = OnSchoolChangedAsync(actualValue);
                }
            }
        }

        /// <summary>
        /// 选中的年级
        /// </summary>
        public string? SelectedGrade
        {
            get => SearchFilter.Grade == null ? AllOption : SearchFilter.Grade;
            set
            {
                var actualValue = value == AllOption ? null : value;
                if (SearchFilter.Grade != actualValue)
                {
                    _ = OnGradeChangedAsync(actualValue);
                }
            }
        }

        /// <summary>
        /// 选中的班级
        /// </summary>
        public string? SelectedClass
        {
            get => SearchFilter.Class == null ? AllOption : SearchFilter.Class;
            set
            {
                var actualValue = value == AllOption ? null : value;
                if (SearchFilter.Class != actualValue)
                {
                    _ = OnClassChangedAsync(actualValue);
                }
            }
        }

        /// <summary>
        /// 学校列表
        /// </summary>
        public ObservableCollection<string> Schools
        {
            get => _schools;
            set => SetProperty(ref _schools, value);
        }

        /// <summary>
        /// 年级列表
        /// </summary>
        public ObservableCollection<string> Grades
        {
            get => _grades;
            set => SetProperty(ref _grades, value);
        }

        /// <summary>
        /// 班级列表
        /// </summary>
        public ObservableCollection<string> Classes
        {
            get => _classes;
            set => SetProperty(ref _classes, value);
        }

        /// <summary>
        /// 组别列表
        /// </summary>
        public ObservableCollection<string> GroupNames
        {
            get => _groupNames;
            set => SetProperty(ref _groupNames, value);
        }

        /// <summary>
        /// 选中的组别
        /// </summary>
        public string? SelectedGroup
        {
            get => SearchFilter.GroupName == null ? AllOption : SearchFilter.GroupName;
            set
            {
                var actualValue = value == AllOption ? null : value;
                if (SearchFilter.GroupName != actualValue)
                {
                    SearchFilter.GroupName = actualValue;
                    OnPropertyChanged(nameof(SelectedGroup));
                }
            }
        }

        /// <summary>
        /// 导出查询结果到Excel
        /// </summary>
        private async Task ExportToExcelAsync()
        {
            _loggingService?.Info("[按钮点击] 成绩管理 - 导出查询结果按钮");
            try
            {
                if (TotalCount == 0)
                {
                    _loggingService?.Warn("[导出] 没有可导出的数据");
                    MessageBox.Show("没有可导出的数据，请先查询。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    Title = "导出查询结果",
                    FileName = $"成绩查询结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    _loggingService?.Info($"[导出] 开始导出成绩到: {dialog.FileName}");
                    IsLoading = true;

                    try
                    {
                        // 获取所有符合条件的数据（不分页）
                        var exportFilter = new ScoreSearchFilter
                        {
                            StartDate = SearchFilter.StartDate,
                            EndDate = SearchFilter.EndDate,
                            School = SearchFilter.School,
                            Grade = SearchFilter.Grade,
                            Class = SearchFilter.Class,
                            GroupName = SearchFilter.GroupName,
                            PageSize = int.MaxValue,
                            PageNumber = 1
                        };

                        var dataList = await _lapRecordRepository.SearchScoresAsync(exportFilter);

                        // 使用ClosedXML导出Excel
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("成绩查询结果");

                        // 设置表头
                        var headers = new[] { "比赛日期", "学校", "年级", "班级", "组别", "姓名", "性别", "准考证号", "芯片外部号码", "圈数", "累计用时" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = headers[i];
                            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        }

                        // 填充数据
                        int row = 2;
                        foreach (var item in dataList)
                        {
                            worksheet.Cell(row, 1).Value = item.RaceDateFormatted;
                            worksheet.Cell(row, 2).Value = item.School;
                            worksheet.Cell(row, 3).Value = item.Grade;
                            worksheet.Cell(row, 4).Value = item.Class;
                            worksheet.Cell(row, 5).Value = item.GroupName;
                            worksheet.Cell(row, 6).Value = item.Name;
                            worksheet.Cell(row, 7).Value = item.Gender;
                            worksheet.Cell(row, 8).Value = item.ExamNumber;
                            worksheet.Cell(row, 9).Value = item.BibNumber;
                            worksheet.Cell(row, 10).Value = item.TotalLaps;
                            worksheet.Cell(row, 11).Value = item.TotalTimeFormatted;
                            row++;
                        }

                        // 自动调整列宽
                        worksheet.Columns().AdjustToContents();

                        // 保存文件
                        workbook.SaveAs(dialog.FileName);

                        _loggingService?.Info($"成功导出 {dataList.Count} 条记录到 {dialog.FileName}");
                        MessageBox.Show($"成功导出 {dataList.Count} 条记录", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.Error($"导出Excel失败: {ex.Message}", ex);
                        MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"打开保存对话框失败: {ex.Message}", ex);
                MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private void Search()
        {
            _loggingService?.Info("[按钮点击] 成绩管理 - 查询按钮");
            _loggingService?.Debug($"[查询条件] School={SearchFilter.School}, Grade={SearchFilter.Grade}, Class={SearchFilter.Class}, GroupName={SearchFilter.GroupName}, StartDate={StartDate}, EndDate={EndDate}");
            CurrentPage = 1;
            _ = LoadScoresAsync();
        }

        /// <summary>
        /// 清除搜索
        /// </summary>
        private void ClearSearch()
        {
            _loggingService?.Info("[按钮点击] 成绩管理 - 清除搜索按钮");
            SearchFilter = new ScoreSearchFilter();
            StartDate = null;
            EndDate = null;
            Grades.Clear();
            Classes.Clear();
            GroupNames.Clear();
            CurrentPage = 1;
            _ = LoadSchoolsAsync();
            _ = LoadScoresAsync();
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 更新总页数
        /// </summary>
        private void UpdateTotalPages()
        {
            TotalPages = (int)Math.Ceiling((double)TotalCount / SearchFilter.PageSize);
            if (TotalPages == 0)
            {
                TotalPages = 1;
            }
        }

        /// <summary>
        /// 异步加载成绩列表
        /// </summary>
        public async Task LoadScoresAsync()
        {
            try
            {
                IsLoading = true;
                // 让UI有机会刷新显示遮罩层
                await Task.Delay(50);

                SearchFilter.PageNumber = CurrentPage;
                var scores = await _lapRecordRepository.SearchScoresAsync(SearchFilter);
                var totalCount = await _lapRecordRepository.GetScoresTotalCountAsync(SearchFilter);

                Scores = new ObservableCollection<ScoreResult>(scores);
                TotalCount = totalCount;
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载成绩列表失败: {ex.Message}", ex);
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                UpdateTotalPages();
            }
        }

        /// <summary>
        /// 在列表开头添加"全部"选项
        /// </summary>
        private ObservableCollection<string> AddAllOption(IEnumerable<string> items)
        {
            var collection = new ObservableCollection<string> { AllOption };
            foreach (var item in items)
            {
                collection.Add(item);
            }
            return collection;
        }

        /// <summary>
        /// 加载学校列表
        /// </summary>
        private async Task LoadSchoolsAsync()
        {
            try
            {
                var currentSchool = SearchFilter.School;
                var schools = await _lapRecordRepository.GetScoreSchoolsAsync();
                Schools = AddAllOption(schools);

                if (!string.IsNullOrWhiteSpace(currentSchool) && schools.Contains(currentSchool))
                {
                    await OnSchoolChangedAsync(currentSchool);
                }
                else
                {
                    SearchFilter.School = null;
                    OnPropertyChanged(nameof(SelectedSchool));
                    await OnSchoolChangedAsync(null);
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载学校列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 学校选择改变时的处理
        /// </summary>
        private async Task OnSchoolChangedAsync(string? school)
        {
            SearchFilter.School = school;
            SearchFilter.Grade = null;
            SearchFilter.Class = null;
            SearchFilter.GroupName = null;
            OnPropertyChanged(nameof(SelectedSchool));
            OnPropertyChanged(nameof(SelectedGrade));
            OnPropertyChanged(nameof(SelectedClass));
            OnPropertyChanged(nameof(SelectedGroup));

            Grades.Clear();
            Classes.Clear();
            GroupNames.Clear();

            if (!string.IsNullOrWhiteSpace(school))
            {
                try
                {
                    var grades = await _lapRecordRepository.GetScoreGradesAsync(school);
                    Grades = AddAllOption(grades);
                    SearchFilter.Grade = null;
                    OnPropertyChanged(nameof(SelectedGrade));
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"加载年级列表失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 年级选择改变时的处理
        /// </summary>
        private async Task OnGradeChangedAsync(string? grade)
        {
            SearchFilter.Grade = grade;
            SearchFilter.Class = null;
            SearchFilter.GroupName = null;
            OnPropertyChanged(nameof(SelectedGrade));
            OnPropertyChanged(nameof(SelectedClass));
            OnPropertyChanged(nameof(SelectedGroup));

            Classes.Clear();
            GroupNames.Clear();

            if (!string.IsNullOrWhiteSpace(SearchFilter.School) && !string.IsNullOrWhiteSpace(grade))
            {
                try
                {
                    var classes = await _lapRecordRepository.GetScoreClassesAsync(SearchFilter.School, grade);
                    Classes = AddAllOption(classes);
                    SearchFilter.Class = null;
                    OnPropertyChanged(nameof(SelectedClass));
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"加载班级列表失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 班级选择改变时的处理
        /// </summary>
        private async Task OnClassChangedAsync(string? classValue)
        {
            SearchFilter.Class = classValue;
            SearchFilter.GroupName = null;
            OnPropertyChanged(nameof(SelectedClass));
            OnPropertyChanged(nameof(SelectedGroup));

            GroupNames.Clear();

            if (!string.IsNullOrWhiteSpace(SearchFilter.School) && 
                !string.IsNullOrWhiteSpace(SearchFilter.Grade) && 
                !string.IsNullOrWhiteSpace(classValue))
            {
                try
                {
                    var groupNames = await _lapRecordRepository.GetScoreGroupNamesAsync(
                        SearchFilter.School, SearchFilter.Grade, classValue);
                    GroupNames = AddAllOption(groupNames);
                    SearchFilter.GroupName = null;
                    OnPropertyChanged(nameof(SelectedGroup));
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"加载组别列表失败: {ex.Message}", ex);
                }
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

        /// <summary>
        /// 释放资源的实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                _disposed = true;
            }
        }
    }
}
