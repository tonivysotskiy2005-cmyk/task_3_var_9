using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using SportModels;

namespace SportApp.ViewModels
{
    public class AthleteViewModel : ViewModelBase
    {
        private readonly Athlete _athlete;
        private readonly double _trackLength;
        private readonly double _canvasWidth;
        private readonly Dispatcher _dispatcher;

        private double _leftOffset;
        private string _stateText = string.Empty;
        private double _progressPercent;
        private double _speedDisplay;
        private double _odds;

        public AthleteViewModel(Athlete athlete, double trackLength, double canvasWidth, int laneIndex, double laneHeight, double topMargin)
        {
            _athlete = athlete;
            _trackLength = trackLength;
            _canvasWidth = canvasWidth;
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            LaneIndex = laneIndex;
            TopOffset = topMargin + laneIndex * laneHeight;
            LaneHeight = laneHeight;
            _speedDisplay = _athlete.CurrentSpeed;
            UpdateStateText();

            _athlete.PropertyChanged += OnAthletePropertyChanged;
        }

        public Athlete Model => _athlete;

        public string Name => _athlete.Name;

        public int LaneIndex { get; }

        public int LaneNumber => LaneIndex + 1;

        public double TopOffset { get; }

        public double LaneHeight { get; }

        public double BaseSpeed => _athlete.BaseSpeed;

        public double InjuryProbabilityPercent => _athlete.InjuryProbability * 100.0;

        public double LeftOffset
        {
            get => _leftOffset;
            private set => SetField(ref _leftOffset, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            private set => SetField(ref _progressPercent, value);
        }

        public double SpeedDisplay
        {
            get => _speedDisplay;
            private set => SetField(ref _speedDisplay, value);
        }

        public string StateText
        {
            get => _stateText;
            private set => SetField(ref _stateText, value);
        }

        private void OnAthletePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (e.PropertyName == nameof(Athlete.Position))
                {
                    double ratio = _trackLength > 0 ? _athlete.Position / _trackLength : 0;
                    LeftOffset = ratio * (_canvasWidth - 20);
                    ProgressPercent = ratio * 100.0;
                }
                else if (e.PropertyName == nameof(Athlete.State))
                {
                    UpdateStateText();
                }
                else if (e.PropertyName == nameof(Athlete.CurrentSpeed))
                {
                    SpeedDisplay = _athlete.CurrentSpeed;
                }
            }));
        }

        private void UpdateStateText()
        {
            StateText = _athlete.State switch
            {
                AthleteState.Ready => "READY",
                AthleteState.Running => "RUNNING",
                AthleteState.Injured => "INJURED",
                AthleteState.Healing => "HEALING",
                AthleteState.Finished => "FINISHED",
                AthleteState.Retired => "RETIRED",
                _ => _athlete.State.ToString().ToUpperInvariant()
            };
            OnPropertyChanged(nameof(StateColor));
            OnPropertyChanged(nameof(ChipColor));
            OnPropertyChanged(nameof(IsInjured));
            OnPropertyChanged(nameof(IsHealing));
            OnPropertyChanged(nameof(IsFinished));
        }

        public bool IsInjured => _athlete.State == AthleteState.Injured;
        public bool IsHealing => _athlete.State == AthleteState.Healing;
        public bool IsFinished => _athlete.State == AthleteState.Finished;

        public string StateColor => _athlete.State switch
        {
            AthleteState.Running => "#D1FF3D",
            AthleteState.Injured => "#FF4D5E",
            AthleteState.Healing => "#FFB547",
            AthleteState.Finished => "#5CC8FF",
            AthleteState.Retired => "#5C6576",
            _ => "#8A93A6"
        };

        public string ChipColor => _athlete.State switch
        {
            AthleteState.Running => "#D1FF3D",
            AthleteState.Injured => "#FF4D5E",
            AthleteState.Healing => "#FFB547",
            AthleteState.Finished => "#5CC8FF",
            AthleteState.Retired => "#3A424F",
            _ => "#8A93A6"
        };

        public double Odds
        {
            get => _odds;
            set
            {
                if (SetField(ref _odds, value))
                    OnPropertyChanged(nameof(OddsDisplay));
            }
        }

        public string OddsDisplay => _odds > 0 ? $"× {_odds:F2}" : "—";
    }
}
