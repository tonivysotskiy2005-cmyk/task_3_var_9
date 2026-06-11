using System;
using System.Threading;

namespace SportModels
{
    public class Doctor : IDoctor
    {
        private readonly object _lock = new object();

        public Doctor(string name, int healDurationMs)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Имя врача не должно быть пустым.", nameof(name));
            }

            if (healDurationMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(healDurationMs), "Время лечения должно быть положительным.");
            }

            Name = name;
            HealDurationMs = healDurationMs;
        }

        public string Name { get; }

        public int HealDurationMs { get; }

        public event EventHandler<AthleteEventArgs>? HealingStarted;

        public event EventHandler<AthleteEventArgs>? HealingFinished;

        public void Heal(Athlete athlete, CancellationToken cancellationToken)
        {
            if (athlete == null)
            {
                throw new ArgumentNullException(nameof(athlete));
            }

            lock (_lock)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                athlete.State = AthleteState.Healing;
                HealingStarted?.Invoke(this, new AthleteEventArgs(athlete, $"Врач {Name} начал лечение {athlete.Name}."));

                bool canceled = cancellationToken.WaitHandle.WaitOne(HealDurationMs);
                if (canceled)
                {
                    return;
                }

                double priorSpeed = athlete.CurrentSpeed > 0 ? athlete.CurrentSpeed : athlete.BaseSpeed;
                athlete.CurrentSpeed = priorSpeed * 0.85;
                athlete.State = AthleteState.Running;
                HealingFinished?.Invoke(this, new AthleteEventArgs(athlete, $"Врач {Name} вылечил {athlete.Name}."));
            }
        }
    }
}
