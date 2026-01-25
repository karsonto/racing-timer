using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Timer.Models;

namespace Timer.ViewModels
{
    /// <summary>
    /// 比赛分组计时信息，用于多组同时计时的UI显示
    /// </summary>
    public partial class RaceGroupTimingInfo : ObservableObject, IDisposable
    {
        private readonly DispatcherTimer _timer;
        private DateTime _raceStartTime;
        private TimeSpan _pausedElapsed;
        private bool _disposed;

        public RaceGroupTimingInfo()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;
        }

        /// <summary>
        /// 比赛分组ID
        /// </summary>
        [ObservableProperty]
        private int _raceGroupId;

        /// <summary>
        /// 比赛记录ID（开始比赛后获得）
        /// </summary>
        [ObservableProperty]
        private int _raceRecordId;

        /// <summary>
        /// 显示名称
        /// </summary>
        [ObservableProperty]
        private string _displayName = string.Empty;

        /// <summary>
        /// 学校
        /// </summary>
        [ObservableProperty]
        private string _school = string.Empty;

        /// <summary>
        /// 年级
        /// </summary>
        [ObservableProperty]
        private string? _grade;

        /// <summary>
        /// 班级
        /// </summary>
        [ObservableProperty]
        private string? _class;

        /// <summary>
        /// 组名
        /// </summary>
        [ObservableProperty]
        private string _groupName = string.Empty;

        /// <summary>
        /// 芯片组颜色
        /// </summary>
        [ObservableProperty]
        private string? _chipGroupColor;

        /// <summary>
        /// 芯片组名称
        /// </summary>
        [ObservableProperty]
        private string? _chipGroupName;

        /// <summary>
        /// 总圈数
        /// </summary>
        [ObservableProperty]
        private int _totalLaps = 1;

        /// <summary>
        /// 参赛人数
        /// </summary>
        [ObservableProperty]
        private int _participantCount;

        /// <summary>
        /// 已完成人数
        /// </summary>
        [ObservableProperty]
        private int _completedCount;

        /// <summary>
        /// 比赛状态
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(CanPause))]
        [NotifyPropertyChangedFor(nameof(CanResume))]
        [NotifyPropertyChangedFor(nameof(CanStop))]
        [NotifyPropertyChangedFor(nameof(IsRaceActive))]
        [NotifyPropertyChangedFor(nameof(StatusColorHex))]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private RaceStatus _status = RaceStatus.Stopped;

        /// <summary>
        /// 已用时显示
        /// </summary>
        [ObservableProperty]
        private string _elapsedTimeDisplay = "00:00:00.000";

        /// <summary>
        /// 是否展开显示参赛者
        /// </summary>
        [ObservableProperty]
        private bool _isExpanded;

        /// <summary>
        /// 是否选中（用于批量操作）
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// 参赛者计时信息列表
        /// </summary>
        public ObservableCollection<ParticipantTimingInfo> Participants { get; } = new();

        /// <summary>
        /// 是否可以开始
        /// </summary>
        public bool CanStart => Status == RaceStatus.Stopped && ParticipantCount > 0;

        /// <summary>
        /// 是否可以暂停
        /// </summary>
        public bool CanPause => Status == RaceStatus.Running;

        /// <summary>
        /// 是否可以继续
        /// </summary>
        public bool CanResume => Status == RaceStatus.Paused;

        /// <summary>
        /// 是否可以停止
        /// </summary>
        public bool CanStop => Status == RaceStatus.Running || Status == RaceStatus.Paused;

        /// <summary>
        /// 比赛是否正在进行（运行或暂停）
        /// </summary>
        public bool IsRaceActive => Status == RaceStatus.Running || Status == RaceStatus.Paused;

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText => Status switch
        {
            RaceStatus.Running => "进行中",
            RaceStatus.Paused => "已暂停",
            RaceStatus.Completed => "已完成",
            RaceStatus.Stopped => "待开始",
            _ => "待开始"
        };

        /// <summary>
        /// 状态颜色（十六进制）
        /// </summary>
        public string StatusColorHex => Status switch
        {
            RaceStatus.Running => "#22c55e",
            RaceStatus.Paused => "#f59e0b",
            RaceStatus.Completed => "#3b82f6",
            RaceStatus.Stopped => "#94a3b8",
            _ => "#94a3b8"
        };

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                if (ParticipantCount == 0) return 0;
                return (double)CompletedCount / ParticipantCount * 100;
            }
        }

        /// <summary>
        /// 比赛开始时间
        /// </summary>
        public DateTime RaceStartTime => _raceStartTime;

        /// <summary>
        /// 启动计时器
        /// </summary>
        public void StartTimer(DateTime startTime)
        {
            _raceStartTime = startTime;
            _pausedElapsed = TimeSpan.Zero;
            Status = RaceStatus.Running;
            _timer.Start();
            UpdateElapsedDisplay();
        }

        /// <summary>
        /// 恢复比赛（从暂停状态继续）
        /// </summary>
        public void ResumeTimer()
        {
            // 调整开始时间以保持暂停前的计时
            _raceStartTime = DateTime.Now - _pausedElapsed;
            Status = RaceStatus.Running;
            _timer.Start();
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        public void PauseTimer()
        {
            _pausedElapsed = DateTime.Now - _raceStartTime;
            Status = RaceStatus.Paused;
            _timer.Stop();
        }

        /// <summary>
        /// 停止计时器
        /// </summary>
        public void StopTimer(RaceStatus finalStatus = RaceStatus.Stopped)
        {
            _timer.Stop();
            Status = finalStatus;
        }

        /// <summary>
        /// 重置计时器（违规重跑时使用）
        /// </summary>
        public void ResetTimer()
        {
            _timer.Stop();
            _raceStartTime = DateTime.MinValue;
            _pausedElapsed = TimeSpan.Zero;
            Status = RaceStatus.Stopped;
            ElapsedTimeDisplay = "00:00:00.000";
        }

        /// <summary>
        /// 获取已用时间
        /// </summary>
        public TimeSpan GetElapsedTime()
        {
            if (Status == RaceStatus.Running)
            {
                return DateTime.Now - _raceStartTime;
            }
            return _pausedElapsed;
        }

        /// <summary>
        /// 更新参赛者的排名
        /// </summary>
        public void UpdateRankings()
        {
            var sorted = Participants
                .OrderByDescending(p => p.CurrentLap)
                .ThenBy(p => p.TotalTime)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Rank = i + 1;
                sorted[i].IsLeading = (i == 0 && sorted[i].CurrentLap > 0);
            }

            CompletedCount = Participants.Count(p => p.IsCompleted);
            OnPropertyChanged(nameof(ProgressPercentage));
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateElapsedDisplay();
        }

        private void UpdateElapsedDisplay()
        {
            var elapsed = GetElapsedTime();
            ElapsedTimeDisplay = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}";
            
            // 同步更新所有未完成参赛者的实时用时
            foreach (var participant in Participants)
            {
                if (!participant.IsCompleted)
                {
                    participant.LiveElapsedTime = elapsed;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Stop();
                _disposed = true;
            }
        }
    }
}

