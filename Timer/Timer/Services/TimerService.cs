using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 计时服务实现，支持多组同时计时
    /// </summary>
    public class TimerService : ITimerService
    {
        private readonly IRaceRecordRepository _raceRecordRepository;
        private readonly ILapRecordRepository _lapRecordRepository;
        private readonly Dictionary<int, RaceRecord> _activeRaces = new();

        public TimerService(
            IRaceRecordRepository raceRecordRepository,
            ILapRecordRepository lapRecordRepository)
        {
            _raceRecordRepository = raceRecordRepository ?? throw new ArgumentNullException(nameof(raceRecordRepository));
            _lapRecordRepository = lapRecordRepository ?? throw new ArgumentNullException(nameof(lapRecordRepository));
        }

        public IReadOnlyDictionary<int, RaceRecord> ActiveRaces => _activeRaces;

        public bool HasActiveRaces => _activeRaces.Count > 0;

        // 兼容旧接口
        public RaceRecord? CurrentRace => _activeRaces.Values.FirstOrDefault();

        public bool IsRunning => _activeRaces.Values.Any(r => r.Status == RaceStatus.Running);

        public async Task<RaceRecord> StartRaceAsync(int raceGroupId, int totalLaps)
        {
            // 检查该分组是否已有正在进行的比赛
            if (_activeRaces.Values.Any(r => r.RaceGroupId == raceGroupId))
            {
                throw new InvalidOperationException("该分组已有正在进行的比赛");
            }

            // 创建新的比赛记录
            var raceRecord = new RaceRecord
            {
                RaceGroupId = raceGroupId,
                StartTime = DateTime.Now,
                Status = RaceStatus.Running,
                TotalLaps = totalLaps,
                CreatedAt = DateTime.Now
            };

            var id = await _raceRecordRepository.CreateAsync(raceRecord);
            raceRecord.Id = id;
            
            // 添加到活跃比赛列表
            _activeRaces[id] = raceRecord;

            return raceRecord;
        }

        public async Task PauseRaceAsync(int raceRecordId)
        {
            if (!_activeRaces.TryGetValue(raceRecordId, out var race))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            if (race.Status != RaceStatus.Running)
            {
                throw new InvalidOperationException("比赛未在运行中");
            }

            race.Status = RaceStatus.Paused;
            await _raceRecordRepository.UpdateAsync(race);
        }

        public async Task ResumeRaceAsync(int raceRecordId)
        {
            if (!_activeRaces.TryGetValue(raceRecordId, out var race))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            if (race.Status != RaceStatus.Paused)
            {
                throw new InvalidOperationException("比赛未处于暂停状态");
            }

            race.Status = RaceStatus.Running;
            await _raceRecordRepository.UpdateAsync(race);
        }

        public async Task StopRaceAsync(int raceRecordId)
        {
            if (!_activeRaces.TryGetValue(raceRecordId, out var race))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            race.Status = RaceStatus.Stopped;
            race.EndTime = DateTime.Now;
            await _raceRecordRepository.UpdateAsync(race);
            
            // 从活跃列表移除
            _activeRaces.Remove(raceRecordId);
        }

        public async Task CompleteRaceAsync(int raceRecordId)
        {
            if (!_activeRaces.TryGetValue(raceRecordId, out var race))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            race.Status = RaceStatus.Completed;
            race.EndTime = DateTime.Now;
            await _raceRecordRepository.UpdateAsync(race);
            
            // 从活跃列表移除
            _activeRaces.Remove(raceRecordId);
        }

        public async Task<LapRecord> RecordLapAsync(int raceRecordId, int participantId, DateTime passTime)
        {
            if (!_activeRaces.TryGetValue(raceRecordId, out var race))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            if (race.Status != RaceStatus.Running)
            {
                throw new InvalidOperationException("比赛未在运行中，无法记录圈次");
            }

            // 获取该参赛者的上一圈记录
            var previousLap = await _lapRecordRepository.GetLatestLapAsync(raceRecordId, participantId);
            
            int lapNumber = (previousLap?.LapNumber ?? 0) + 1;
            long lapTime;
            long totalTime;

            if (previousLap == null)
            {
                // 第一圈：从比赛开始时间计算
                var elapsed = passTime - race.StartTime;
                lapTime = (long)elapsed.TotalMilliseconds;
                totalTime = lapTime;
            }
            else
            {
                // 后续圈：从上一圈通过时间计算
                var elapsed = passTime - previousLap.PassTime;
                lapTime = (long)elapsed.TotalMilliseconds;
                totalTime = previousLap.TotalTime + lapTime;
            }

            // 创建圈次记录
            var lapRecord = new LapRecord
            {
                RaceRecordId = raceRecordId,
                ParticipantId = participantId,
                LapNumber = lapNumber,
                PassTime = passTime,
                LapTime = lapTime,
                TotalTime = totalTime,
                CreatedAt = DateTime.Now
            };

            // 计算排名
            var rank = await CalculateRankInternalAsync(raceRecordId, participantId, totalTime, lapNumber);
            lapRecord.Rank = rank;

            // 保存到数据库
            var id = await _lapRecordRepository.CreateAsync(lapRecord);
            lapRecord.Id = id;

            return lapRecord;
        }

        public async Task<int> GetParticipantCurrentLapAsync(int raceRecordId, int participantId)
        {
            var latestLap = await _lapRecordRepository.GetLatestLapAsync(raceRecordId, participantId);
            return latestLap?.LapNumber ?? 0;
        }

        public async Task<long> GetParticipantTotalTimeAsync(int raceRecordId, int participantId)
        {
            var latestLap = await _lapRecordRepository.GetLatestLapAsync(raceRecordId, participantId);
            return latestLap?.TotalTime ?? 0;
        }

        public async Task<int> CalculateRankAsync(int raceRecordId, int participantId)
        {
            var totalTime = await GetParticipantTotalTimeAsync(raceRecordId, participantId);
            var currentLap = await GetParticipantCurrentLapAsync(raceRecordId, participantId);
            
            return await CalculateRankInternalAsync(raceRecordId, participantId, totalTime, currentLap);
        }

        public async Task<List<RaceRecord>> LoadActiveRacesAsync()
        {
            _activeRaces.Clear();
            
            var activeRaces = await _raceRecordRepository.GetActiveRacesAsync();
            foreach (var race in activeRaces)
            {
                _activeRaces[race.Id] = race;
            }
            
            return activeRaces;
        }

        public async Task<List<LapRecord>> GetLapRecordsAsync(int raceRecordId)
        {
            var records = await _lapRecordRepository.GetByRaceRecordIdAsync(raceRecordId);
            return records.ToList();
        }

        /// <summary>
        /// 计算排名（内部方法，考虑圈数和用时）
        /// </summary>
        private async Task<int> CalculateRankInternalAsync(int raceRecordId, int participantId, long totalTime, int currentLap)
        {
            // 获取所有参赛者的最新圈次记录
            var allLaps = await _lapRecordRepository.GetByRaceRecordIdAsync(raceRecordId);
            
            // 按参赛者分组，取每个参赛者的最新记录
            var latestLaps = allLaps
                .GroupBy(l => l.ParticipantId)
                .Select(g => g.OrderByDescending(l => l.LapNumber).First())
                .ToList();

            // 排名规则：圈数多的排前面，圈数相同时用时少的排前面
            var rank = latestLaps
                .Where(l => l.LapNumber > currentLap || 
                           (l.LapNumber == currentLap && l.TotalTime < totalTime))
                .Count() + 1;

            return rank;
        }

        #region 兼容旧接口实现

        public async Task PauseRaceAsync()
        {
            var race = _activeRaces.Values.FirstOrDefault();
            if (race != null)
            {
                await PauseRaceAsync(race.Id);
            }
        }

        public async Task ResumeRaceAsync()
        {
            var race = _activeRaces.Values.FirstOrDefault(r => r.Status == RaceStatus.Paused);
            if (race != null)
            {
                await ResumeRaceAsync(race.Id);
            }
        }

        public async Task StopRaceAsync()
        {
            var race = _activeRaces.Values.FirstOrDefault();
            if (race != null)
            {
                await StopRaceAsync(race.Id);
            }
        }

        public async Task CompleteRaceAsync()
        {
            var race = _activeRaces.Values.FirstOrDefault();
            if (race != null)
            {
                await CompleteRaceAsync(race.Id);
            }
        }

        public async Task<LapRecord> RecordLapAsync(int participantId, DateTime passTime)
        {
            var race = _activeRaces.Values.FirstOrDefault(r => r.Status == RaceStatus.Running);
            if (race == null)
            {
                throw new InvalidOperationException("没有正在进行的比赛");
            }
            return await RecordLapAsync(race.Id, participantId, passTime);
        }

        public async Task<int> GetParticipantCurrentLapAsync(int participantId)
        {
            var race = _activeRaces.Values.FirstOrDefault();
            if (race == null) return 0;
            return await GetParticipantCurrentLapAsync(race.Id, participantId);
        }

        public async Task<long> GetParticipantTotalTimeAsync(int participantId)
        {
            var race = _activeRaces.Values.FirstOrDefault();
            if (race == null) return 0;
            return await GetParticipantTotalTimeAsync(race.Id, participantId);
        }

        public async Task<int> CalculateRankAsync(int participantId)
        {
            var race = _activeRaces.Values.FirstOrDefault();
            if (race == null) return 0;
            return await CalculateRankAsync(race.Id, participantId);
        }

        public async Task<RaceRecord?> LoadActiveRaceAsync()
        {
            var races = await LoadActiveRacesAsync();
            return races.FirstOrDefault();
        }

        #endregion
    }
}
