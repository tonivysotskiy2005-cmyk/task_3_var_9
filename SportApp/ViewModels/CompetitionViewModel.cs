using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SportApp.Commands;
using SportModels;

namespace SportApp.ViewModels
{
    public class CompetitionViewModel : ViewModelBase
    {
        private readonly Competition _competition;
        private readonly Dispatcher _dispatcher;
        private string? _lastAwardTitle;
        private string? _lastAwardDescription;
        private bool _showAward;

        public const double CanvasWidth = 720;

        public const double LaneHeight = 46;

        public const double LaneTopMargin = 10;

        public double CanvasHeight { get; }

        public CompetitionViewModel(Competition competition)
        {
            _competition = competition ?? throw new ArgumentNullException(nameof(competition));
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            Athletes = new ObservableCollection<AthleteViewModel>();
            Log = new ObservableCollection<string>();

            StartCommand = new RelayCommand(_ => Start(), _ => !_competition.IsRunning && _competition.Athletes.Count > 0);
            StopCommand = new RelayCommand(_ => _competition.Stop(), _ => _competition.IsRunning);
            DismissAwardCommand = new RelayCommand(_ => ShowAward = false);

            int lane = 0;
            foreach (Athlete athlete in _competition.Athletes)
            {
                Athletes.Add(new AthleteViewModel(athlete, _competition.TrackLength, CanvasWidth, lane++, LaneHeight, LaneTopMargin));
            }

            CanvasHeight = LaneTopMargin * 2 + Math.Max(1, _competition.Athletes.Count) * LaneHeight;

            CalculateOdds();

            _competition.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Competition.Status))
                {
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusBadge));
                    OnPropertyChanged(nameof(StatusColor));
                }
                else if (e.PropertyName == nameof(Competition.IsRunning))
                {
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(StatusBadge));
                    OnPropertyChanged(nameof(StatusColor));
                    CommandManager.InvalidateRequerySuggested();
                }
                else if (e.PropertyName == nameof(Competition.Winner))
                {
                    OnPropertyChanged(nameof(WinnerName));
                    OnPropertyChanged(nameof(HasWinner));
                }
            };

            _competition.AthleteInjured += OnAthleteInjured;
            _competition.AthleteHealed += OnAthleteHealed;
            _competition.CompetitionStarted += OnCompetitionStarted;
            _competition.CompetitionFinished += OnCompetitionFinished;

            if (_competition.Doctor is Doctor d)
            {
                d.HealingStarted += (_, args) => AppendLog(args.Message);
            }
        }

        public string Title => _competition.Title;

        public string DoctorName => _competition.Doctor.Name;

        public double TrackLength => _competition.TrackLength;

        public int AthleteCount => _competition.Athletes.Count;

        public string Status => _competition.Status;

        public string StatusBadge => _competition.IsRunning
            ? "LIVE"
            : (_competition.Winner != null ? "FINISHED" : "READY");

        public string StatusColor => _competition.IsRunning
            ? "#FF4D5E"
            : (_competition.Winner != null ? "#5CC8FF" : "#8A93A6");

        public bool IsRunning => _competition.IsRunning;

        public bool HasWinner => _competition.Winner != null;

        public string WinnerName => _competition.Winner?.Name ?? "—";

        public ObservableCollection<AthleteViewModel> Athletes { get; }

        public ObservableCollection<string> Log { get; }

        public ICommand StartCommand { get; }

        public ICommand StopCommand { get; }

        public ICommand DismissAwardCommand { get; }

        public string? LastAwardTitle
        {
            get => _lastAwardTitle;
            private set => SetField(ref _lastAwardTitle, value);
        }

        public string? LastAwardDescription
        {
            get => _lastAwardDescription;
            private set => SetField(ref _lastAwardDescription, value);
        }

        public bool ShowAward
        {
            get => _showAward;
            set => SetField(ref _showAward, value);
        }

        private void Start()
        {
            try
            {
                ShowAward = false;
                _competition.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnAthleteInjured(object? sender, AthleteEventArgs e)
        {
            AppendLog("🤕 " + e.Message);
        }

        private void OnAthleteHealed(object? sender, AthleteEventArgs e)
        {
            AppendLog("💊 " + e.Message);
        }

        private void OnCompetitionStarted(object? sender, EventArgs e)
        {
            AppendLog($"🏁 [{_competition.Title}] Старт!");
        }

        private void OnCompetitionFinished(object? sender, WinnerEventArgs e)
        {
            AppendLog($"🏆 [{_competition.Title}] Победитель — {e.Winner.Name}");
            AppendLog(e.Award.ToString());

            _dispatcher.BeginInvoke(new Action(() =>
            {
                LastAwardTitle = e.Award.Title;
                LastAwardDescription = e.Award.Description;
                ShowAward = true;
            }));
        }

        private void CalculateOdds()
        {
            int n = Athletes.Count;
            if (n == 0) return;

            // Monte Carlo даёт честные вероятности; blending с равномерным
            // сглаживает экстремальные кэфы типа ×0.95 или ×85.
            const double alpha = 0.10;
            const double margin = 0.10;
            double uniform = 1.0 / n;

            double[] mc = _competition.CalculateWinProbabilities(5000);

            for (int i = 0; i < n; i++)
            {
                double blended = (1 - alpha) * mc[i] + alpha * uniform;
                Athletes[i].Odds = 1.0 / (blended * (1.0 + margin));
            }
        }

        private void AppendLog(string line)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                Log.Insert(0, $"{DateTime.Now:HH:mm:ss}  {line}");
                while (Log.Count > 80)
                {
                    Log.RemoveAt(Log.Count - 1);
                }
            }));
        }
    }
}
