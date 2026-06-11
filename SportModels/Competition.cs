using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SportModels
{
    public class Competition : INotifyPropertyChanged
    {
        private readonly ObservableCollection<Athlete> _athletes;
        private readonly IDoctor _doctor;
        private readonly Random _random;
        private readonly int _tickIntervalMs;

        private CancellationTokenSource? _cts;
        private Task? _raceTask;
        private bool _isRunning;
        private string _status;
        private Athlete? _winner;

        public Competition(string title, double trackLength, IDoctor doctor, int tickIntervalMs, int seed)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Название не должно быть пустым.", nameof(title));
            }

            if (trackLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(trackLength), "Дистанция должна быть положительной.");
            }

            if (tickIntervalMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickIntervalMs), "Интервал должен быть положительным.");
            }

            Title = title;
            TrackLength = trackLength;
            _doctor = doctor ?? throw new ArgumentNullException(nameof(doctor));
            _tickIntervalMs = tickIntervalMs;
            _random = new Random(seed);
            _athletes = new ObservableCollection<Athlete>();
            _status = "Готова к старту";
        }

        public string Title { get; }

        public double TrackLength { get; }

        public IDoctor Doctor => _doctor;

        public ObservableCollection<Athlete> Athletes => _athletes;

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public Athlete? Winner
        {
            get => _winner;
            private set
            {
                if (!ReferenceEquals(_winner, value))
                {
                    _winner = value;
                    OnPropertyChanged();
                }
            }
        }

        public event EventHandler<AthleteEventArgs>? AthleteInjured;

        public event EventHandler<AthleteEventArgs>? AthleteHealed;

        public event EventHandler<AthleteEventArgs>? AthleteProgress;

        public event EventHandler<WinnerEventArgs>? CompetitionFinished;

        public event EventHandler? CompetitionStarted;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void AddAthlete(Athlete athlete)
        {
            if (athlete == null)
            {
                throw new ArgumentNullException(nameof(athlete));
            }

            if (IsRunning)
            {
                throw new InvalidOperationException("Нельзя добавлять спортсменов во время соревнования.");
            }

            _athletes.Add(athlete);
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            if (_athletes.Count == 0)
            {
                throw new InvalidOperationException("Нет спортсменов.");
            }

            foreach (Athlete athlete in _athletes)
            {
                athlete.Reset();
                athlete.State = AthleteState.Running;
            }

            Winner = null;
            Status = "Идёт соревнование";
            IsRunning = true;
            CompetitionStarted?.Invoke(this, EventArgs.Empty);

            _cts = new CancellationTokenSource();
            _raceTask = Task.Run(() => RunRace(_cts.Token));
        }

        public void Stop()
        {
            CancellationTokenSource? cts = _cts;
            Task? task = _raceTask;
            cts?.Cancel();

            try
            {
                task?.Wait(500);
            }
            catch
            {
            }

            foreach (Athlete athlete in _athletes)
            {
                athlete.Reset();
            }

            Winner = null;
            Status = "Готова к старту";
            IsRunning = false;
            _cts = null;
            _raceTask = null;
        }

        private void RunRace(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    bool anyoneStillRacing = false;

                    foreach (Athlete athlete in _athletes.ToArray())
                    {
                        if (athlete.State == AthleteState.Finished || athlete.State == AthleteState.Retired)
                        {
                            continue;
                        }

                        if (athlete.State == AthleteState.Running)
                        {
                            anyoneStillRacing = true;

                            double roll = _random.NextDouble();
                            if (roll < athlete.InjuryProbability)
                            {
                                athlete.State = AthleteState.Injured;
                                AthleteInjured?.Invoke(this, new AthleteEventArgs(athlete, $"{athlete.Name} получил травму!"));
                                ScheduleDoctor(athlete);
                                continue;
                            }

                            double delta = athlete.CurrentSpeed * (_tickIntervalMs / 1000.0);
                            double newPosition = athlete.Position + delta;
                            if (newPosition >= TrackLength)
                            {
                                athlete.Position = TrackLength;
                                athlete.State = AthleteState.Finished;
                                AthleteProgress?.Invoke(this, new AthleteEventArgs(athlete, $"{athlete.Name} финишировал."));
                                HandleFinish(athlete);
                                return;
                            }

                            athlete.Position = newPosition;
                            AthleteProgress?.Invoke(this, new AthleteEventArgs(athlete, $"{athlete.Name}: {athlete.Position:F1} м"));
                        }
                        else if (athlete.State == AthleteState.Injured || athlete.State == AthleteState.Healing)
                        {
                            anyoneStillRacing = true;
                        }
                    }

                    if (!anyoneStillRacing)
                    {
                        Status = "Никто не финишировал";
                        IsRunning = false;
                        return;
                    }

                    Thread.Sleep(_tickIntervalMs);
                }
            }
            catch (Exception ex)
            {
                Status = $"Ошибка: {ex.Message}";
                IsRunning = false;
            }
        }

        private void ScheduleDoctor(Athlete athlete)
        {
            CancellationToken token = _cts?.Token ?? CancellationToken.None;
            Task.Run(() =>
            {
                try
                {
                    _doctor.Heal(athlete, token);
                    if (!token.IsCancellationRequested)
                    {
                        AthleteHealed?.Invoke(this, new AthleteEventArgs(athlete, $"{athlete.Name} снова в строю."));
                    }
                }
                catch (Exception ex)
                {
                    AthleteHealed?.Invoke(this, new AthleteEventArgs(athlete, $"Лечение прервано: {ex.Message}"));
                }
            });
        }

        public double[] CalculateWinProbabilities(int simulations = 5000)
        {
            int n = _athletes.Count;
            if (n == 0) return [];

            int[] wins = new int[n];
            var rng = new Random(0);
            for (int s = 0; s < simulations; s++)
            {
                int winner = SimulateRace(rng);
                if (winner >= 0) wins[winner]++;
            }

            double total = wins.Sum();
            return [.. wins.Select(w => total > 0 ? w / total : 1.0 / n)];
        }

        private int SimulateRace(Random rng)
        {
            int n        = _athletes.Count;
            double[] spd = [.. _athletes.Select(a => a.BaseSpeed)];
            double[] pos = new double[n];
            int[]    st  = new int[n];
            int[]    ht  = [.. Enumerable.Repeat(-1, n)];
            int      doc = 0;
            double   ts  = _tickIntervalMs / 1000.0;
            int      htk = (int)Math.Ceiling((double)_doctor.HealDurationMs / _tickIntervalMs);

            for (int tick = 0; tick < 200_000; tick++)
            {
                bool any = false;
                for (int i = 0; i < n; i++)
                {
                    if (st[i] == 3) continue;
                    any = true;
                    if (st[i] == 1) {
                        if (tick >= doc) { st[i] = 2; ht[i] = tick + htk; doc = ht[i]; }
                        continue;
                    }
                    if (st[i] == 2) {
                        if (tick >= ht[i]) { spd[i] *= 0.85; st[i] = 0; }
                        continue;
                    }
                    if (rng.NextDouble() < _athletes[i].InjuryProbability) { st[i] = 1; continue; }
                    pos[i] += spd[i] * ts;
                    if (pos[i] >= TrackLength) { st[i] = 3; return i; }
                }
                if (!any) break;
            }
            return -1;
        }

        private void HandleFinish(Athlete winner)
        {
            Winner = winner;
            Award award = new Award(
                title: $"Золотая медаль: {Title}",
                description: BuildWinnerReport(winner),
                issuedAt: DateTime.Now);

            Status = $"Победил {winner.Name}";
            IsRunning = false;
            CompetitionFinished?.Invoke(this, new WinnerEventArgs(winner, award));
        }

        private static string BuildWinnerReport(Athlete winner)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Характеристики победителя:");

            Type type = winner.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead)
                {
                    continue;
                }

                DisplayNameAttribute? display = property.GetCustomAttribute<DisplayNameAttribute>();
                if (display == null)
                {
                    continue;
                }

                object? value;
                try
                {
                    value = property.GetValue(winner);
                }
                catch
                {
                    continue;
                }

                sb.AppendLine($"  • {display.DisplayName}: {FormatValue(value)}");
            }

            return sb.ToString();
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
            {
                return "—";
            }

            switch (value)
            {
                case double d when d >= 0 && d < 1:
                    return $"{d * 100:F2} %";
                case double d:
                    return d.ToString("F2", CultureInfo.CurrentCulture);
                case AthleteState state:
                    return state switch
                    {
                        AthleteState.Ready => "Готов к старту",
                        AthleteState.Running => "В движении",
                        AthleteState.Injured => "Травмирован",
                        AthleteState.Healing => "На лечении",
                        AthleteState.Finished => "Финишировал",
                        AthleteState.Retired => "Сошёл с дистанции",
                        _ => state.ToString()
                    };
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
