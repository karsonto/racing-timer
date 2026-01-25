using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Timer.ViewModels
{
    /// <summary>
    /// 参赛者计时信息，用于UI显示
    /// </summary>
    public partial class ParticipantTimingInfo : ObservableObject
    {
        [ObservableProperty]
        private int _participantId;

        [ObservableProperty]
        private int _rank;

        [ObservableProperty]
        private string _bibNumber = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _chipNumber;

        [ObservableProperty]
        private int _currentLap;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalTimeFormatted))]
        private TimeSpan _totalTime;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LastLapTimeFormatted))]
        private TimeSpan? _lastLapTime;

        /// <summary>
        /// 实时用时（比赛进行中同步更新）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LiveElapsedTimeFormatted))]
        [NotifyPropertyChangedFor(nameof(TotalTimeFormatted))]
        private TimeSpan _liveElapsedTime;

        /// <summary>
        /// 所有圈次的用时列表（索引0表示第1圈）
        /// </summary>
        public List<TimeSpan> LapTimes { get; } = new();

        [ObservableProperty]
        private string _status = "未开始";

        [ObservableProperty]
        private bool _isLeading;

        [ObservableProperty]
        private bool _isCompleted;

        /// <summary>
        /// 是否正在比赛中（比赛开始后为true）
        /// </summary>
        [ObservableProperty]
        private bool _isRacing;

        /// <summary>
        /// 格式化显示的累计用时（已完成显示完成时间，进行中显示实时时间）
        /// </summary>
        public string TotalTimeFormatted => _isCompleted ? FormatTimeSpan(_totalTime) : LiveElapsedTimeFormatted;

        /// <summary>
        /// 格式化显示的实时用时
        /// </summary>
        public string LiveElapsedTimeFormatted => FormatTimeSpan(_liveElapsedTime);

        /// <summary>
        /// 格式化显示的最后一圈用时
        /// </summary>
        public string LastLapTimeFormatted => _lastLapTime.HasValue ? FormatTimeSpan(_lastLapTime.Value) : "-";

        /// <summary>
        /// 所有圈次成绩的格式化显示（用于工具提示或详细视图）
        /// </summary>
        public string AllLapsFormatted
        {
            get
            {
                if (LapTimes.Count == 0)
                    return "未开始";

                return string.Join(" | ", LapTimes.Select((time, index) => 
                    $"第{index + 1}圈: {FormatTimeSpan(time)}"));
            }
        }

        /// <summary>
        /// 格式化时间显示（HH:MM:SS.fff）
        /// </summary>
        private string FormatTimeSpan(TimeSpan time)
        {
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }

        partial void OnCurrentLapChanged(int value)
        {
            UpdateStatus();
        }

        partial void OnIsCompletedChanged(bool value)
        {
            UpdateStatus();
        }

        partial void OnIsRacingChanged(bool value)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_isCompleted)
            {
                Status = "已完成";
            }
            else if (_isRacing)
            {
                Status = "进行中";
            }
            else
            {
                Status = "未开始";
            }
        }

        /// <summary>
        /// 通知全部圈次显示已更新
        /// </summary>
        public void NotifyAllLapsChanged()
        {
            OnPropertyChanged(nameof(AllLapsFormatted));
        }
    }
}

