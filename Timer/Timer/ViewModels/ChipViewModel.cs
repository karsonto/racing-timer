using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Win32;
using Timer.Data;
using Timer.Messages;
using Timer.Models;
using Timer.Services;
using Timer.Views;

namespace Timer.ViewModels
{
    /// <summary>
    /// 芯片设备页面的ViewModel
    /// </summary>
    public class ChipViewModel : ObservableObject, IDisposable, IRecipient<DataReloadRequestedMessage>
    {
        private readonly IChipRepository _repository;
        private readonly IChipImportService _chipImportService;
        private readonly ILoggingService? _loggingService;
        private readonly DatabaseContext _dbContext;
        private bool _disposed;

        private ObservableCollection<ChipGroup> _chipGroups = new();
        private ChipGroup? _selectedChipGroup;
        private ObservableCollection<Chip> _chips = new();
        private bool _isLoading;
        private double _importProgress;
        private ImportResult? _importResult;

        /// <summary>
        /// 初始化ChipViewModel实例
        /// </summary>
        public ChipViewModel(
            IChipRepository repository,
            IChipImportService chipImportService,
            DatabaseContext dbContext,
            ILoggingService? loggingService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _chipImportService = chipImportService ?? throw new ArgumentNullException(nameof(chipImportService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _loggingService = loggingService;

            Title = "芯片设备管理";
            ImportCommand = new AsyncRelayCommand(ImportExcelAsync);
            SelectChipGroupCommand = new RelayCommand<ChipGroup>(SelectChipGroup);
            EditChipGroupCommand = new AsyncRelayCommand<ChipGroup>(EditChipGroupAsync);
            DeleteChipGroupCommand = new AsyncRelayCommand<ChipGroup>(DeleteChipGroupAsync);
            EditChipCommand = new AsyncRelayCommand<Chip>(EditChipAsync);
            DeleteChipCommand = new AsyncRelayCommand<Chip>(DeleteChipAsync);

            WeakReferenceMessenger.Default.Register<DataReloadRequestedMessage>(this);

            // 初始化时加载数据
            _ = LoadChipGroupsAsync();
        }

        public void Receive(DataReloadRequestedMessage message)
        {
            if (message == null) return;

            switch (message.Value)
            {
                case DataDomain.ChipGroups:
                    _ = LoadChipGroupsAsync();
                    break;
                case DataDomain.Chips:
                    _ = LoadChipsAsync();
                    break;
            }
        }

        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 芯片组列表
        /// </summary>
        public ObservableCollection<ChipGroup> ChipGroups
        {
            get => _chipGroups;
            set => SetProperty(ref _chipGroups, value);
        }

        /// <summary>
        /// 选中的芯片组
        /// </summary>
        public ChipGroup? SelectedChipGroup
        {
            get => _selectedChipGroup;
            set
            {
                if (SetProperty(ref _selectedChipGroup, value))
                {
                    _ = LoadChipsAsync();
                }
            }
        }

        /// <summary>
        /// 当前选中组的芯片列表
        /// </summary>
        public ObservableCollection<Chip> Chips
        {
            get => _chips;
            set => SetProperty(ref _chips, value);
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
        /// 导入进度（0-100）
        /// </summary>
        public double ImportProgress
        {
            get => _importProgress;
            set => SetProperty(ref _importProgress, value);
        }

        /// <summary>
        /// 导入结果
        /// </summary>
        public ImportResult? ImportResult
        {
            get => _importResult;
            set => SetProperty(ref _importResult, value);
        }

        /// <summary>
        /// 导入Excel文件命令
        /// </summary>
        public IAsyncRelayCommand ImportCommand { get; }

        /// <summary>
        /// 选择芯片组命令
        /// </summary>
        public IRelayCommand<ChipGroup> SelectChipGroupCommand { get; }

        /// <summary>
        /// 编辑芯片组命令
        /// </summary>
        public IAsyncRelayCommand<ChipGroup> EditChipGroupCommand { get; }

        /// <summary>
        /// 删除芯片组命令
        /// </summary>
        public IAsyncRelayCommand<ChipGroup> DeleteChipGroupCommand { get; }

        /// <summary>
        /// 编辑芯片命令
        /// </summary>
        public IAsyncRelayCommand<Chip> EditChipCommand { get; }

        /// <summary>
        /// 删除芯片命令
        /// </summary>
        public IAsyncRelayCommand<Chip> DeleteChipCommand { get; }

        /// <summary>
        /// 加载芯片组列表
        /// </summary>
        private async Task LoadChipGroupsAsync()
        {
            try
            {
                IsLoading = true;
                await LoadChipGroupsInternalAsync();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载芯片组列表失败: {ex.Message}", ex);
                MessageBox.Show($"加载芯片组列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载芯片组列表（内部方法，不设置IsLoading）
        /// </summary>
        private async Task LoadChipGroupsInternalAsync()
        {
            var groups = await _repository.GetAllChipGroupsAsync();
            ChipGroups.Clear();
            foreach (var group in groups)
            {
                ChipGroups.Add(group);
            }
        }

        /// <summary>
        /// 加载选中芯片组的芯片列表
        /// </summary>
        private async Task LoadChipsAsync()
        {
            if (SelectedChipGroup == null)
            {
                Chips.Clear();
                return;
            }

            try
            {
                IsLoading = true;
                var chips = await _repository.GetChipsByGroupIdAsync(SelectedChipGroup.Id);
                Chips.Clear();
                foreach (var chip in chips)
                {
                    Chips.Add(chip);
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载芯片列表失败: {ex.Message}", ex);
                MessageBox.Show($"加载芯片列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 选择芯片组
        /// </summary>
        private void SelectChipGroup(ChipGroup? group)
        {
            SelectedChipGroup = group;
        }

        /// <summary>
        /// 编辑芯片组
        /// </summary>
        private async Task EditChipGroupAsync(ChipGroup? group)
        {
            if (group == null) return;

            try
            {
                // 获取最新数据
                var latestGroup = await _repository.GetChipGroupByIdAsync(group.Id);
                if (latestGroup == null)
                {
                    MessageBox.Show("芯片组不存在或已被删除", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await LoadChipGroupsAsync();
                    return;
                }

                // 打开编辑对话框
                var dialog = new EditChipGroupDialog(latestGroup, _repository, _loggingService)
                {
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    _loggingService?.Info($"芯片组已更新: {group.GroupName}");
                    // 刷新列表
                    await LoadChipGroupsAsync();
                    // 通知其它页面：芯片组列表/分组列表可能需要刷新（批量/引用场景）
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.ChipGroups));
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.RaceGroups));
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"编辑芯片组失败: {ex.Message}", ex);
                MessageBox.Show($"编辑芯片组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除芯片组
        /// </summary>
        private async Task DeleteChipGroupAsync(ChipGroup? group)
        {
            if (group == null) return;

            var result = MessageBox.Show(
                $"确定要删除芯片组 \"{group.GroupName}\" 吗？\n\n此操作将同时删除该组下的所有芯片（共 {group.ChipCount} 个），且无法恢复。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await _repository.DeleteChipGroupAsync(group.Id);
                    _loggingService?.Info($"删除芯片组成功: {group.GroupName}");
                    
                    // 如果删除的是当前选中的组，清空选中
                    if (SelectedChipGroup?.Id == group.Id)
                    {
                        SelectedChipGroup = null;
                    }
                    
                    // 刷新列表
                    await LoadChipGroupsAsync();
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.ChipGroups));
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.RaceGroups));
                    
                    MessageBox.Show($"芯片组 \"{group.GroupName}\" 已删除", "删除成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"删除芯片组失败: {ex.Message}", ex);
                    MessageBox.Show($"删除芯片组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 编辑芯片
        /// </summary>
        private async Task EditChipAsync(Chip? chip)
        {
            if (chip == null) return;

            try
            {
                // 打开编辑对话框
                var dialog = new EditChipDialog(chip, _repository, _loggingService)
                {
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    _loggingService?.Info($"芯片已更新: {chip.LabelNumber}");
                    // 刷新芯片列表
                    await LoadChipsAsync();
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.Chips));
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"编辑芯片失败: {ex.Message}", ex);
                MessageBox.Show($"编辑芯片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除芯片
        /// </summary>
        private async Task DeleteChipAsync(Chip? chip)
        {
            if (chip == null) return;

            var result = MessageBox.Show(
                $"确定要删除芯片 \"{chip.LabelNumber}\" 吗？\n\n此操作无法恢复。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await _repository.DeleteChipAsync(chip.Id);
                    _loggingService?.Info($"删除芯片成功: {chip.LabelNumber}");
                    
                    // 刷新芯片列表
                    await LoadChipsAsync();
                    
                    // 更新芯片组的数量
                    await LoadChipGroupsAsync();
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.Chips));
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.ChipGroups));
                    
                    MessageBox.Show($"芯片 \"{chip.LabelNumber}\" 已删除", "删除成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"删除芯片失败: {ex.Message}", ex);
                    MessageBox.Show($"删除芯片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 导入Excel文件
        /// </summary>
        private async Task ImportExcelAsync()
        {
            _loggingService?.Info("[按钮点击] 芯片管理 - 导入芯片信息按钮");
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Excel文件 (*.xls;*.xlsx)|*.xls;*.xlsx|所有文件 (*.*)|*.*",
                    Title = "选择要导入的芯片信息Excel文件"
                };

                if (dialog.ShowDialog() == true)
                {
                    _loggingService?.Info($"[导入] 选择文件: {dialog.FileName}");
                    
                    // 确认是否清空现有数据
                    var confirmResult = MessageBox.Show(
                        "导入将清空以下所有数据，然后重新导入：\n\n" +
                        "• 芯片组和芯片数据\n" +
                        "• 比赛分组数据\n" +
                        "• 比赛记录数据\n" +
                        "• 圈次成绩数据\n\n" +
                        "此操作不可恢复，确定要继续吗？",
                        "确认导入",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirmResult != MessageBoxResult.Yes)
                    {
                        _loggingService?.Info("[导入] 用户取消导入");
                        return;
                    }

                    _loggingService?.Info("[导入] 用户确认导入，开始清空数据并导入");
                    IsLoading = true;
                    ImportProgress = 0;
                    ImportResult = null;

                    // 让UI有机会刷新显示遮罩层
                    await Task.Delay(50);

                    string? resultMessage = null;
                    string? resultTitle = null;
                    MessageBoxImage resultIcon = MessageBoxImage.Information;

                    try
                    {
                        // 先清空所有芯片组和芯片数据
                        await _repository.DeleteAllChipGroupsAndChipsAsync();
                        _loggingService?.Info("已清空现有芯片数据，准备重新导入");
                        
                        // 清空当前选中
                        SelectedChipGroup = null;
                        ChipGroups.Clear();
                        Chips.Clear();

                        // 读取Excel文件
                        var chipData = await _chipImportService.ReadFromFileAsync(dialog.FileName);

                        // 导入到数据库
                        var progress = new Progress<double>(value => ImportProgress = value);
                        ImportResult = await _chipImportService.ImportAsync(chipData, progress);

                        if (ImportResult.IsSuccess())
                        {
                            resultMessage = $"成功导入{ImportResult.SuccessCount}条记录";
                            resultTitle = "导入成功";
                            resultIcon = MessageBoxImage.Information;
                        }
                        else
                        {
                            resultMessage = $"导入完成：成功{ImportResult.SuccessCount}条，失败{ImportResult.FailureCount}条\n\n";
                            resultMessage += string.Join("\n", ImportResult.Errors.Take(10).Select(e => e.ToString()));
                            if (ImportResult.Errors.Count > 10)
                            {
                                resultMessage += $"\n... 还有{ImportResult.Errors.Count - 10}个错误";
                            }
                            resultTitle = "导入完成（有错误）";
                            resultIcon = MessageBoxImage.Warning;
                        }

                        // 刷新列表（使用内部方法，不重置IsLoading）
                        await LoadChipGroupsInternalAsync();
                        WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.ChipGroups));
                        WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.Chips));
                        WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.RaceGroups));
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.Error($"导入Excel文件失败: {ex.Message}", ex);
                        resultMessage = $"导入Excel文件失败: {ex.Message}";
                        resultTitle = "错误";
                        resultIcon = MessageBoxImage.Error;
                    }
                    finally
                    {
                        // 关闭遮罩层
                        IsLoading = false;
                        // 让UI有机会刷新关闭遮罩层
                        await Task.Delay(50);
                    }

                    // 在遮罩层关闭后显示结果
                    if (resultMessage != null && resultTitle != null)
                    {
                        MessageBox.Show(resultMessage, resultTitle, MessageBoxButton.OK, resultIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"打开文件对话框失败: {ex.Message}", ex);
                MessageBox.Show($"打开文件对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                // 清理托管资源
                _disposed = true;
            }
        }
    }
}
