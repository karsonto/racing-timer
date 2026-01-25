using System;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Timer.Messages;
using Timer.Models;
using Timer.Services;
using Timer.ViewModels;

namespace Timer.Views
{
    /// <summary>
    /// 编辑芯片组对话框
    /// </summary>
    public partial class EditChipGroupDialog : Window
    {
        private readonly EditChipGroupDialogViewModel _vm;
        private readonly IChipRepository _repository;
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化编辑芯片组对话框
        /// </summary>
        /// <param name="chipGroup">要编辑的芯片组</param>
        /// <param name="repository">数据访问仓库</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public EditChipGroupDialog(ChipGroup chipGroup, IChipRepository repository, ILoggingService? loggingService = null)
        {
            InitializeComponent();

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;
            _vm = new EditChipGroupDialogViewModel(chipGroup, _repository, _loggingService);
            DataContext = _vm;
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证数据
                if (!await _vm.ValidateAsync())
                {
                    return; // 显示错误，不关闭对话框
                }

                // 更新数据库
                var updated = _vm.ToChipGroup();
                await _repository.UpdateChipGroupAsync(updated);
                _loggingService?.Info($"成功更新芯片组: {updated.GroupName} (ID: {updated.Id})");

                // 广播更新消息，供其他页面实时刷新（人员分组/计时等）
                WeakReferenceMessenger.Default.Send(new ChipGroupUpdatedMessage(updated));

                // 设置对话框结果为成功
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"保存芯片组信息失败: {ex.Message}", ex);
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

