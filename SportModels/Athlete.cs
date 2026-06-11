using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportModels
{
    public class Athlete : INotifyPropertyChanged
    {
        private double _position;
        private AthleteState _state;
        private double _currentSpeed;

        public Athlete(string name, double baseSpeed, double injuryProbability)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Имя спортсмена не должно быть пустым.", nameof(name));
            }

            if (baseSpeed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseSpeed), "Скорость должна быть положительной.");
            }

            if (injuryProbability < 0 || injuryProbability > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(injuryProbability), "Вероятность должна быть в диапазоне [0;1].");
            }

            Name = name;
            BaseSpeed = baseSpeed;
            InjuryProbability = injuryProbability;
            _state = AthleteState.Ready;
            _currentSpeed = baseSpeed;
        }

        [DisplayName("Имя")]
        public string Name { get; }

        [DisplayName("Базовая скорость, м/с")]
        public double BaseSpeed { get; }

        [DisplayName("Вероятность травмы за такт")]
        public double InjuryProbability { get; }

        [DisplayName("Пройденная дистанция, м")]
        public double Position
        {
            get => _position;
            set
            {
                if (Math.Abs(_position - value) > double.Epsilon)
                {
                    _position = value;
                    OnPropertyChanged();
                }
            }
        }

        [DisplayName("Состояние")]
        public AthleteState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

        [DisplayName("Текущая скорость, м/с")]
        public double CurrentSpeed
        {
            get => _currentSpeed;
            set
            {
                if (Math.Abs(_currentSpeed - value) > double.Epsilon)
                {
                    _currentSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Reset()
        {
            Position = 0;
            CurrentSpeed = BaseSpeed;
            State = AthleteState.Ready;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
