using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 导入参赛人员对话框的ViewModel
    /// </summary>
    public class ImportParticipantDialogViewModel : ObservableObject
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly IExcelImportService _excelImportService;
        private Project? _selectedProject;
        private string? _selectedFilePath;
        private string? _selectedFileName;
        private bool _isImporting;
        private double _importProgress;
        private ImportResult? _importResult;

        public ImportParticipantDialogViewModel(
            IProjectRepository projectRepository,
            IParticipantRepository participantRepository,
            IExcelImportService excelImportService)
        {
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _excelImportService = excelImportService ?? throw new ArgumentNullException(nameof(excelImportService));

            Projects = new ObservableCollection<Project>();
            
            SelectFileCommand = new RelayCommand(SelectFile);
            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);

            // 加载项目列表
            _ = LoadProjectsAsync();
        }

        /// <summary>
        /// 项目列表（只有正常状态的）
        /// </summary>
        public ObservableCollection<Project> Projects { get; }

        /// <summary>
        /// 选中的项目
        /// </summary>
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 选中的文件路径
        /// </summary>
        public string? SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (SetProperty(ref _selectedFilePath, value))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 选中的文件名（用于显示）
        /// </summary>
        public string? SelectedFileName
        {
            get => _selectedFileName;
            set => SetProperty(ref _selectedFileName, value);
        }

        /// <summary>
        /// 是否正在导入
        /// </summary>
        public bool IsImporting
        {
            get => _isImporting;
            set => SetProperty(ref _isImporting, value);
        }

        /// <summary>
        /// 导入进度
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
        /// 对话框结果
        /// </summary>
        public bool? DialogResult { get; private set; }

        /// <summary>
        /// 选择文件命令
        /// </summary>
        public RelayCommand SelectFileCommand { get; }

        /// <summary>
        /// 确认命令
        /// </summary>
        public AsyncRelayCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public RelayCommand CancelCommand { get; }

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<bool>? RequestClose;

        /// <summary>
        /// 加载项目列表
        /// </summary>
        private async Task LoadProjectsAsync()
        {
            try
            {
                var projects = await _projectRepository.GetActiveProjectsAsync();
                Projects.Clear();
                foreach (var project in projects)
                {
                    Projects.Add(project);
                }
            }
            catch (Exception)
            {
                // 忽略加载错误
            }
        }

        /// <summary>
        /// 选择文件
        /// </summary>
        private void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel文件 (*.xls;*.xlsx)|*.xls;*.xlsx|所有文件 (*.*)|*.*",
                Title = "选择要导入的Excel文件"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                SelectedFileName = System.IO.Path.GetFileName(dialog.FileName);
            }
        }

        /// <summary>
        /// 是否可以确认
        /// </summary>
        private bool CanConfirm()
        {
            return SelectedProject != null && !string.IsNullOrEmpty(SelectedFilePath) && !IsImporting;
        }

        /// <summary>
        /// 确认导入（只关闭弹窗，实际导入由主页面执行）
        /// </summary>
        private Task ConfirmAsync()
        {
            if (SelectedProject == null || string.IsNullOrEmpty(SelectedFilePath))
                return Task.CompletedTask;

            DialogResult = true;
            RequestClose?.Invoke(true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(false);
        }

        /// <summary>
        /// 获取导入结果
        /// </summary>
        public ImportResult? GetImportResult()
        {
            return ImportResult;
        }

        /// <summary>
        /// 获取选择的项目
        /// </summary>
        public Project? GetSelectedProject()
        {
            return SelectedProject;
        }

        /// <summary>
        /// 获取选择的文件路径
        /// </summary>
        public string? GetSelectedFilePath()
        {
            return SelectedFilePath;
        }
    }
}
