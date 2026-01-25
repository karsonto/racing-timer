using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timer.Models;

namespace Timer.ViewModels
{
    /// <summary>
    /// 项目状态选项
    /// </summary>
    public class ProjectStatusOption
    {
        public ProjectStatus Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 编辑/新增项目对话框的ViewModel
    /// </summary>
    public class EditProjectDialogViewModel : ObservableObject
    {
        private DateTime _projectDate;
        private string _name = string.Empty;
        private ProjectStatusOption _selectedStatus = null!;
        private readonly Project? _originalProject;

        public EditProjectDialogViewModel(Project? project = null)
        {
            _originalProject = project;

            // 初始化状态选项
            StatusOptions = new ObservableCollection<ProjectStatusOption>
            {
                new ProjectStatusOption { Value = ProjectStatus.Normal, DisplayName = "正常" },
                new ProjectStatusOption { Value = ProjectStatus.Invalid, DisplayName = "失效" }
            };

            if (project != null)
            {
                // 编辑模式
                _projectDate = project.ProjectDate;
                _name = project.Name;
                _selectedStatus = project.Status == ProjectStatus.Invalid ? StatusOptions[1] : StatusOptions[0];
                DialogTitle = "编辑项目";
            }
            else
            {
                // 新增模式
                _projectDate = DateTime.Today;
                _name = string.Empty;
                _selectedStatus = StatusOptions[0]; // 默认"正常"
                DialogTitle = "新增项目";
            }

            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string DialogTitle { get; }

        /// <summary>
        /// 是否为编辑模式
        /// </summary>
        public bool IsEditMode => _originalProject != null;

        /// <summary>
        /// 项目日期
        /// </summary>
        public DateTime ProjectDate
        {
            get => _projectDate;
            set
            {
                if (SetProperty(ref _projectDate, value))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 状态选项列表
        /// </summary>
        public ObservableCollection<ProjectStatusOption> StatusOptions { get; }

        /// <summary>
        /// 选中的状态
        /// </summary>
        public ProjectStatusOption SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool? DialogResult { get; private set; }

        /// <summary>
        /// 确认命令
        /// </summary>
        public RelayCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public RelayCommand CancelCommand { get; }

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<bool>? RequestClose;

        /// <summary>
        /// 获取更新后的项目对象
        /// </summary>
        public Project GetProject()
        {
            if (_originalProject != null)
            {
                // 编辑模式：更新原有对象
                _originalProject.ProjectDate = ProjectDate;
                _originalProject.Name = Name.Trim();
                _originalProject.Status = SelectedStatus.Value;
                return _originalProject;
            }
            else
            {
                // 新增模式：创建新对象
                return new Project
                {
                    ProjectDate = ProjectDate,
                    Name = Name.Trim(),
                    Status = SelectedStatus.Value
                };
            }
        }

        private bool CanConfirm()
        {
            return !string.IsNullOrWhiteSpace(Name);
        }

        private void Confirm()
        {
            DialogResult = true;
            RequestClose?.Invoke(true);
        }

        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(false);
        }
    }
}
