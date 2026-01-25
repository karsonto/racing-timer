using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timer.Models;

namespace Timer.ViewModels
{
    /// <summary>
    /// 编辑分组成员对话框的ViewModel
    /// </summary>
    public class EditGroupMemberDialogViewModel : ObservableObject
    {
        private readonly Participant _participant;
        private string? _chipLabelNumber;
        private string? _chipInternalNumber;

        public EditGroupMemberDialogViewModel(Participant participant, ObservableCollection<Chip> availableChips)
        {
            _participant = participant ?? throw new ArgumentNullException(nameof(participant));
            AvailableChips = availableChips ?? new ObservableCollection<Chip>();

            // 初始化当前值
            _chipLabelNumber = participant.ChipNumber;
            _chipInternalNumber = participant.ChipInternalNumber;

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>
        /// 可用的芯片列表
        /// </summary>
        public ObservableCollection<Chip> AvailableChips { get; }

        /// <summary>
        /// 芯片标签号码
        /// </summary>
        public string? ChipLabelNumber
        {
            get => _chipLabelNumber;
            set
            {
                if (SetProperty(ref _chipLabelNumber, value))
                {
                    // 当芯片标签号码改变时，自动更新内部号码
                    var chip = AvailableChips.FirstOrDefault(c => c.LabelNumber == value);
                    ChipInternalNumber = chip?.InternalNumber;
                }
            }
        }

        /// <summary>
        /// 芯片内部号码
        /// </summary>
        public string? ChipInternalNumber
        {
            get => _chipInternalNumber;
            set => SetProperty(ref _chipInternalNumber, value);
        }

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public bool DialogResult { get; private set; }

        public event EventHandler? RequestClose;

        private void Save()
        {
            // 更新参赛人员的芯片信息
            _participant.ChipNumber = ChipLabelNumber;
            _participant.ChipInternalNumber = ChipInternalNumber;
            // 业务约定：号码布 = 芯片标签号码（与自动分配芯片逻辑保持一致）
            _participant.BibNumber = ChipLabelNumber;

            DialogResult = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 获取更新后的参赛人员
        /// </summary>
        public Participant GetUpdatedParticipant()
        {
            return _participant;
        }
    }
}



