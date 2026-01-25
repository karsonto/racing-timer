using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Timer.Models;
using Timer.Messages;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 多组比赛计时页面的ViewModel，支持多个比赛组同时进行
    /// </summary>
    public partial class MultiRaceTimerViewModel : ObservableObject, IDisposable, IRecipient<ChipGroupUpdatedMessage>, IRecipient<DataReloadRequestedMessage>
    {
        private readonly IRaceGroupRepository _raceGroupRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly ITimerService _timerService;
        private readonly ILoggingService? _loggingService;
        private bool _disposed;

        /// <summary>
        /// 可用的比赛分组列表（用于选择添加）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<RaceGroup> _availableRaceGroups = new();

        /// <summary>
        /// 当前选中的可用分组
        /// </summary>
        [ObservableProperty]
        private RaceGroup? _selectedAvailableGroup;

        /// <summary>
        /// 已添加到比赛的分组列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<RaceGroupTimingInfo> _raceGroups = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSelectAllChecked;

        [ObservableProperty]
        private string _quickLapInput = string.Empty;

        /// <summary>
        /// 选中的比赛组数量
        /// </summary>
        public int SelectedCount => RaceGroups.Count(g => g.IsSelected);

        /// <summary>
        /// 是否有选中的比赛组
        /// </summary>
        public bool HasSelectedGroups => SelectedCount > 0;

        /// <summary>
        /// 是否有正在进行的比赛
        /// </summary>
        public bool HasActiveRaces => RaceGroups.Any(g => g.IsRaceActive);

        /// <summary>
        /// 是否有已添加的比赛组
        /// </summary>
        public bool HasRaceGroups => RaceGroups.Count > 0;

        public string Title => "比赛计时";

        public MultiRaceTimerViewModel(
            IRaceGroupRepository raceGroupRepository,
            IParticipantRepository participantRepository,
            ITimerService timerService,
            ILoggingService? loggingService = null)
        {
            _raceGroupRepository = raceGroupRepository ?? throw new ArgumentNullException(nameof(raceGroupRepository));
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
            _loggingService = loggingService;

            _loggingService?.Debug("MultiRaceTimerViewModel 初始化");

            // 加载可用分组 + 注册跨页面实时刷新（显式注册，避免多 IRecipient<> 时 Register(this) 歧义）
            WeakReferenceMessenger.Default.Register<ChipGroupUpdatedMessage>(this);
            WeakReferenceMessenger.Default.Register<DataReloadRequestedMessage>(this);
            _ = InitializeAsync();
        }

        public void Receive(ChipGroupUpdatedMessage message)
        {
            if (message?.Value == null) return;
            var updated = message.Value;

            // 可用分组列表（RaceGroup）
            foreach (var g in AvailableRaceGroups.Where(r => r.ChipGroupId == updated.Id))
            {
                g.ChipGroupName = updated.GroupName;
                g.ChipGroupColor = updated.Color;
            }

            // 已添加到比赛的分组（RaceGroupTimingInfo）
            foreach (var g in RaceGroups.Where(r => r.RaceGroupId > 0 && r.ChipGroupName != null))
            {
                // RaceGroupTimingInfo 没有 ChipGroupId，按名称/颜色不可靠；改为通过 AvailableRaceGroups 的映射更稳
                // 如果未来需要更强一致性，建议在 TimingInfo 增加 ChipGroupId。
            }

            // 通过 RaceGroupId 反查当前 TimingInfo 对应的 RaceGroup，再更新 TimingInfo 的颜色/名称
            foreach (var timing in RaceGroups)
            {
                var rg = AvailableRaceGroups.FirstOrDefault(x => x.Id == timing.RaceGroupId);
                if (rg != null && rg.ChipGroupId == updated.Id)
                {
                    timing.ChipGroupName = updated.GroupName;
                    timing.ChipGroupColor = updated.Color;
                }
            }
        }

        public void Receive(DataReloadRequestedMessage message)
        {
            if (message == null) return;

            if (message.Value == DataDomain.RaceGroups)
            {
                _ = LoadAvailableGroupsAsync();
            }
            else if (message.Value == DataDomain.Participants)
            {
                _ = RefreshParticipantsForActiveGroupsAsync();
            }
        }

        private async Task RefreshParticipantsForActiveGroupsAsync()
        {
            // 逐个分组刷新参赛者“身份信息”（号码布/姓名/芯片号）
            foreach (var group in RaceGroups.ToList())
            {
                // 比赛未开始：可直接全量重载
                if (!group.IsRaceActive && group.Status == RaceStatus.Stopped)
                {
                    await LoadParticipantsForGroupAsync(group);
                    continue;
                }

                // 比赛进行中/已暂停/已完成：仅更新显示字段，避免重置圈次与计时
                await RefreshParticipantIdentityOnlyAsync(group);
            }
        }

        private async Task RefreshParticipantIdentityOnlyAsync(RaceGroupTimingInfo group)
        {
            try
            {
                var searchFilter = new SearchFilter
                {
                    School = group.School,
                    Grade = group.Grade,
                    Class = group.Class,
                    GroupName = group.GroupName,
                    PageNumber = 1,
                    PageSize = 1000
                };

                var participants = (await _participantRepository.GetAllAsync(searchFilter)).ToList();
                var map = participants.ToDictionary(p => p.Id, p => p);

                foreach (var p in group.Participants)
                {
                    if (map.TryGetValue(p.ParticipantId, out var latest))
                    {
                        p.BibNumber = latest.BibNumber ?? "-";
                        p.Name = latest.Name;
                        p.ChipNumber = latest.ChipNumber;
                    }
                }

                // 参赛人数变化：比赛进行中不强行增删，避免影响计时；仅更新显示统计
                group.ParticipantCount = group.Participants.Count;
            }
            catch
            {
                // 静默：实时刷新失败不应打扰用户
            }
        }

        /// <summary>
        /// 加载可用的比赛分组（不添加到比赛列表）
        /// </summary>
        [RelayCommand]
        private async Task LoadAvailableGroupsAsync()
        {
            try
            {
                IsLoading = true;
                var groups = await _raceGroupRepository.GetAllAsync();
                
                AvailableRaceGroups.Clear();
                foreach (var group in groups)
                {
                    // 过滤掉已经添加的分组
                    if (!RaceGroups.Any(r => r.RaceGroupId == group.Id))
                    {
                        AvailableRaceGroups.Add(group);
                    }
                }

                // 同时恢复活跃的比赛
                await RestoreActiveRacesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载比赛分组失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 恢复活跃的比赛
        /// </summary>
        private async Task RestoreActiveRacesAsync()
        {
            try
            {
                var activeRaces = await _timerService.LoadActiveRacesAsync();
                
                foreach (var activeRace in activeRaces)
                {
                    // 如果已经在列表中，跳过
                    if (RaceGroups.Any(r => r.RaceGroupId == activeRace.RaceGroupId))
                        continue;

                    // 获取分组信息
                    var group = AvailableRaceGroups.FirstOrDefault(g => g.Id == activeRace.RaceGroupId);
                    if (group == null)
                    {
                        // 从数据库加载
                        group = await _raceGroupRepository.GetByIdAsync(activeRace.RaceGroupId);
                    }
                    
                    if (group == null) continue;

                    var timingInfo = CreateTimingInfoFromGroup(group, activeRace.TotalLaps);
                    timingInfo.RaceRecordId = activeRace.Id;

                    // 加载参赛者和圈次记录
                    await LoadParticipantsForGroupAsync(timingInfo);
                    await RestoreLapRecordsAsync(timingInfo, activeRace.Id);

                    // 恢复计时状态
                    if (activeRace.Status == RaceStatus.Running || activeRace.Status == RaceStatus.Paused)
                    {
                        // 标记所有未完成的参赛者为比赛中
                        foreach (var p in timingInfo.Participants.Where(p => !p.IsCompleted))
                        {
                            p.IsRacing = true;
                        }
                        
                        if (activeRace.Status == RaceStatus.Running)
                        {
                            timingInfo.StartTimer(activeRace.StartTime);
                        }
                        else
                        {
                            timingInfo.StartTimer(activeRace.StartTime);
                            timingInfo.PauseTimer();
                        }
                    }

                    RaceGroups.Add(timingInfo);
                    
                    // 从可用列表中移除
                    var availableGroup = AvailableRaceGroups.FirstOrDefault(g => g.Id == group.Id);
                    if (availableGroup != null)
                    {
                        AvailableRaceGroups.Remove(availableGroup);
                    }
                }

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复活跃比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加选中的分组到比赛列表
        /// </summary>
        [RelayCommand]
        private async Task AddGroupToRaceAsync()
        {
            if (SelectedAvailableGroup == null)
            {
                MessageBox.Show("请选择一个比赛分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                var group = SelectedAvailableGroup;
                var laps = group.RaceLaps > 0 ? group.RaceLaps : 1;
                var timingInfo = CreateTimingInfoFromGroup(group, laps);

                // 加载参赛者
                await LoadParticipantsForGroupAsync(timingInfo);

                if (timingInfo.ParticipantCount == 0)
                {
                    MessageBox.Show($"分组 [{group.DisplayName}] 没有参赛者，无法添加", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 添加到比赛列表
                RaceGroups.Add(timingInfo);

                // 从可用列表中移除
                AvailableRaceGroups.Remove(group);
                SelectedAvailableGroup = null;

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加分组失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 从比赛列表移除分组
        /// </summary>
        [RelayCommand]
        private async Task RemoveGroupFromRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null) return;

            if (group.IsRaceActive)
            {
                MessageBox.Show("比赛进行中，无法移除。请先停止比赛。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"确定要从比赛列表中移除 [{group.DisplayName}] 吗？",
                "确认移除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // 移除分组
            RaceGroups.Remove(group);
            group.Dispose();

            // 重新添加到可用列表
            var raceGroup = await _raceGroupRepository.GetByIdAsync(group.RaceGroupId);
            if (raceGroup != null)
            {
                AvailableRaceGroups.Add(raceGroup);
            }

            UpdateSelectionState();
        }

        /// <summary>
        /// 从 RaceGroup 创建 TimingInfo
        /// </summary>
        private RaceGroupTimingInfo CreateTimingInfoFromGroup(RaceGroup group, int totalLaps)
        {
            return new RaceGroupTimingInfo
            {
                RaceGroupId = group.Id,
                DisplayName = group.DisplayName,
                School = group.School,
                Grade = group.Grade,
                Class = group.Class,
                GroupName = group.GroupName,
                ChipGroupColor = group.ChipGroupColor,
                ChipGroupName = group.ChipGroupName,
                ParticipantCount = group.ParticipantCount,
                TotalLaps = totalLaps
            };
        }

        /// <summary>
        /// 切换分组展开/折叠状态
        /// </summary>
        [RelayCommand]
        private void ToggleExpand(RaceGroupTimingInfo? group)
        {
            if (group == null) return;
            group.IsExpanded = !group.IsExpanded;
        }

        /// <summary>
        /// 单独启动一个比赛组
        /// </summary>
        [RelayCommand]
        private async Task StartSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanStart) return;

            try
            {
                if (group.ParticipantCount == 0)
                {
                    MessageBox.Show($"分组 [{group.DisplayName}] 没有参赛者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 启动比赛
                var race = await _timerService.StartRaceAsync(group.RaceGroupId, group.TotalLaps);
                group.RaceRecordId = race.Id;
                group.StartTimer(race.StartTime);

                // 重置参赛者状态
                foreach (var p in group.Participants)
                {
                    p.CurrentLap = 0;
                    p.TotalTime = TimeSpan.Zero;
                    p.LastLapTime = null;
                    p.LapTimes.Clear();
                    p.IsCompleted = false;
                    p.IsLeading = false;
                    p.Rank = 0;
                    p.IsRacing = true;  // 标记比赛开始
                    p.LiveElapsedTime = TimeSpan.Zero;  // 重置实时用时
                }

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 暂停单个比赛组
        /// </summary>
        [RelayCommand]
        private async Task PauseSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanPause) return;

            try
            {
                await _timerService.PauseRaceAsync(group.RaceRecordId);
                group.PauseTimer();
                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"暂停比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 继续单个比赛组
        /// </summary>
        [RelayCommand]
        private async Task ResumeSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanResume) return;

            try
            {
                await _timerService.ResumeRaceAsync(group.RaceRecordId);
                group.ResumeTimer();
                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"继续比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重跑（停止并重置比赛，可重新开始）
        /// </summary>
        [RelayCommand]
        private async Task StopSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanStop) return;

            var result = MessageBox.Show(
                $"确定要对 [{group.DisplayName}] 执行重跑吗？\n\n计时器和所有参赛者的成绩将被清零，可重新开始比赛。",
                "确认重跑",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _timerService.StopRaceAsync(group.RaceRecordId);
                
                // 重置计时器（清零）
                group.ResetTimer();
                group.RaceRecordId = 0;  // 清除比赛记录ID，以便重新开始
                
                // 重置所有参赛者的状态和成绩
                foreach (var p in group.Participants)
                {
                    p.IsRacing = false;
                    p.CurrentLap = 0;
                    p.TotalTime = TimeSpan.Zero;
                    p.LastLapTime = null;
                    p.LapTimes.Clear();
                    p.IsCompleted = false;
                    p.IsLeading = false;
                    p.Rank = 0;
                    p.LiveElapsedTime = TimeSpan.Zero;
                    p.NotifyAllLapsChanged();
                }
                
                // 重置完成人数
                group.CompletedCount = 0;
                
                UpdateSelectionState();
                
                MessageBox.Show($"[{group.DisplayName}] 已重置，可重新开始比赛。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重跑失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 批量启动选中的比赛组
        /// </summary>
        [RelayCommand]
        private async Task StartSelectedRacesAsync()
        {
            var selectedGroups = RaceGroups.Where(g => g.IsSelected && g.CanStart).ToList();
            if (selectedGroups.Count == 0)
            {
                MessageBox.Show("请选择可以启动的比赛组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要启动选中的 {selectedGroups.Count} 个比赛组吗？",
                "批量启动",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            foreach (var group in selectedGroups)
            {
                await StartSingleRaceAsync(group);
            }
        }

        /// <summary>
        /// 批量停止选中的比赛组
        /// </summary>
        [RelayCommand]
        private async Task StopSelectedRacesAsync()
        {
            var selectedGroups = RaceGroups.Where(g => g.IsSelected && g.CanStop).ToList();
            if (selectedGroups.Count == 0)
            {
                MessageBox.Show("请选择正在进行的比赛组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要停止选中的 {selectedGroups.Count} 个比赛组吗？",
                "批量停止",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            foreach (var group in selectedGroups)
            {
                try
                {
                    await _timerService.StopRaceAsync(group.RaceRecordId);
                    group.StopTimer(RaceStatus.Stopped);
                    
                    // 重置参赛者的比赛状态
                    foreach (var p in group.Participants)
                    {
                        p.IsRacing = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"停止 [{group.DisplayName}] 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            UpdateSelectionState();
        }

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        [RelayCommand]
        private void ToggleSelectAll()
        {
            var newState = !IsSelectAllChecked;
            foreach (var group in RaceGroups)
            {
                group.IsSelected = newState;
            }
            IsSelectAllChecked = newState;
            UpdateSelectionState();
        }

        /// <summary>
        /// 记录圈次（单个参赛者）
        /// </summary>
        [RelayCommand]
        private async Task RecordLapAsync(ParticipantTimingInfo participant)
        {
            if (participant == null) return;

            // 找到该参赛者所属的比赛组
            var group = RaceGroups.FirstOrDefault(g => 
                g.Participants.Contains(participant) && g.Status == RaceStatus.Running);

            if (group == null)
            {
                MessageBox.Show("该参赛者所属的比赛未在进行中", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (participant.CurrentLap >= group.TotalLaps)
            {
                MessageBox.Show("该选手已完成所有圈数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var lapRecord = await _timerService.RecordLapAsync(group.RaceRecordId, participant.ParticipantId, DateTime.Now);
                
                // 更新UI显示
                participant.CurrentLap = lapRecord.LapNumber;
                participant.TotalTime = TimeSpan.FromMilliseconds(lapRecord.TotalTime);
                participant.LastLapTime = TimeSpan.FromMilliseconds(lapRecord.LapTime);
                participant.Rank = lapRecord.Rank ?? 0;
                participant.LapTimes.Add(TimeSpan.FromMilliseconds(lapRecord.LapTime));
                participant.NotifyAllLapsChanged();

                // 检查是否完成比赛
                if (participant.CurrentLap >= group.TotalLaps)
                {
                    participant.IsCompleted = true;
                }

                // 更新排名
                group.UpdateRankings();

                // 检查是否所有选手都已完成
                if (group.Participants.All(p => p.IsCompleted))
                {
                    await CompleteRaceAsync(group);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"记录圈次失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 快速记圈（通过号码布/芯片号）
        /// </summary>
        [RelayCommand]
        private async Task QuickRecordLapAsync()
        {
            if (string.IsNullOrWhiteSpace(QuickLapInput)) return;

            var input = QuickLapInput.Trim();

            // 在所有正在进行的比赛中查找参赛者
            ParticipantTimingInfo? foundParticipant = null;

            foreach (var group in RaceGroups.Where(g => g.Status == RaceStatus.Running))
            {
                var participant = group.Participants.FirstOrDefault(p =>
                    p.BibNumber == input || p.ChipNumber == input);

                if (participant != null)
                {
                    foundParticipant = participant;
                    break;
                }
            }

            if (foundParticipant == null)
            {
                MessageBox.Show($"在进行中的比赛中未找到号码布为 '{input}' 的参赛者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await RecordLapAsync(foundParticipant);
            QuickLapInput = string.Empty;
        }

        /// <summary>
        /// 完成比赛
        /// </summary>
        private async Task CompleteRaceAsync(RaceGroupTimingInfo group)
        {
            try
            {
                await _timerService.CompleteRaceAsync(group.RaceRecordId);
                group.StopTimer(RaceStatus.Completed);
                
                // 重置参赛者的比赛状态（虽然已完成，但语义上比赛已结束）
                foreach (var p in group.Participants)
                {
                    p.IsRacing = false;
                }
                
                UpdateSelectionState();

                MessageBox.Show($"[{group.DisplayName}] 比赛已完成！所有选手都已完成比赛。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"完成比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载分组的参赛者
        /// </summary>
        private async Task LoadParticipantsForGroupAsync(RaceGroupTimingInfo group)
        {
            try
            {
                var searchFilter = new SearchFilter
                {
                    School = group.School,
                    Grade = group.Grade,
                    Class = group.Class,
                    GroupName = group.GroupName,
                    PageNumber = 1,
                    PageSize = 1000
                };

                var participants = await _participantRepository.GetAllAsync(searchFilter);
                
                group.Participants.Clear();
                foreach (var participant in participants)
                {
                    group.Participants.Add(new ParticipantTimingInfo
                    {
                        ParticipantId = participant.Id,
                        Rank = 0,
                        BibNumber = participant.BibNumber ?? "-",
                        Name = participant.Name,
                        ChipNumber = participant.ChipNumber,
                        CurrentLap = 0,
                        TotalTime = TimeSpan.Zero,
                        LastLapTime = null,
                        Status = "未开始",
                        IsLeading = false,
                        IsCompleted = false
                    });
                }

                group.ParticipantCount = group.Participants.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载参赛者失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 恢复圈次记录
        /// </summary>
        private async Task RestoreLapRecordsAsync(RaceGroupTimingInfo group, int raceRecordId)
        {
            try
            {
                var lapRecords = await _timerService.GetLapRecordsAsync(raceRecordId);
                
                // 按参赛者分组
                var recordsByParticipant = lapRecords.GroupBy(l => l.ParticipantId);
                
                foreach (var participantRecords in recordsByParticipant)
                {
                    var participant = group.Participants.FirstOrDefault(p => p.ParticipantId == participantRecords.Key);
                    if (participant == null) continue;

                    var orderedRecords = participantRecords.OrderBy(r => r.LapNumber).ToList();
                    var latestRecord = orderedRecords.LastOrDefault();
                    
                    if (latestRecord != null)
                    {
                        participant.CurrentLap = latestRecord.LapNumber;
                        participant.TotalTime = TimeSpan.FromMilliseconds(latestRecord.TotalTime);
                        participant.LastLapTime = TimeSpan.FromMilliseconds(latestRecord.LapTime);
                        participant.Rank = latestRecord.Rank ?? 0;
                        
                        foreach (var record in orderedRecords)
                        {
                            participant.LapTimes.Add(TimeSpan.FromMilliseconds(record.LapTime));
                        }
                        participant.NotifyAllLapsChanged();

                        if (participant.CurrentLap >= group.TotalLaps)
                        {
                            participant.IsCompleted = true;
                        }
                    }
                }

                group.UpdateRankings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复圈次记录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新选择状态
        /// </summary>
        private void UpdateSelectionState()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedGroups));
            OnPropertyChanged(nameof(HasActiveRaces));
            OnPropertyChanged(nameof(HasRaceGroups));
        }

        private async Task InitializeAsync()
        {
            await LoadAvailableGroupsAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                foreach (var group in RaceGroups)
                {
                    group.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
