using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SportApp.Commands;
using SportModels;

namespace SportApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Random _seedSource;
        private int _competitionCounter;

        public MainViewModel()
        {
            Competitions = new ObservableCollection<CompetitionViewModel>();
            _seedSource = new Random();
            AddCompetitionCommand = new RelayCommand(_ => AddCompetition());
            StartAllCommand = new RelayCommand(_ => StartAll(), _ => Competitions.Any(c => !c.IsRunning && c.AthleteCount > 0));
            StopAllCommand = new RelayCommand(_ => StopAll(), _ => Competitions.Any(c => c.IsRunning));
            RemoveCompetitionCommand = new RelayCommand(
                parameter => RemoveCompetition(parameter as CompetitionViewModel),
                parameter => parameter is CompetitionViewModel);

            Competitions.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(CompetitionCount));
                OnPropertyChanged(nameof(HasCompetitions));
                OnPropertyChanged(nameof(RunningCount));
            };
        }

        public ObservableCollection<CompetitionViewModel> Competitions { get; }

        public int CompetitionCount => Competitions.Count;

        public bool HasCompetitions => Competitions.Count > 0;

        public int RunningCount => Competitions.Count(c => c.IsRunning);

        public ICommand AddCompetitionCommand { get; }

        public ICommand StartAllCommand { get; }

        public ICommand StopAllCommand { get; }

        public ICommand RemoveCompetitionCommand { get; }

        private void AddCompetition()
        {
            _competitionCounter++;

            string[] namePool = { "Иван", "Пётр", "Алексей", "Дмитрий", "Сергей", "Андрей", "Николай", "Михаил", "Олег", "Роман", "Максим", "Артём" };
            string[] surnamePool = { "Петров", "Сидоров", "Кузнецов", "Смирнов", "Волков", "Орлов", "Белов", "Зайцев", "Соколов", "Попов" };
            string[] doctorNames = { "Морозов", "Лебедев", "Соловьёв", "Новиков", "Федоров" };

            Doctor doctor = new Doctor($"Др. {doctorNames[_seedSource.Next(doctorNames.Length)]}", 1500);
            doctor.HealingStarted += OnDoctorHealingState;

            Competition competition = new Competition(
                title: $"Забег №{_competitionCounter}",
                trackLength: 200,
                doctor: doctor,
                tickIntervalMs: 80,
                seed: _seedSource.Next());

            int athleteCount = 5;
            HashSet<string> used = new HashSet<string>();
            for (int i = 0; i < athleteCount; i++)
            {
                string fullName;
                do
                {
                    string firstName = namePool[_seedSource.Next(namePool.Length)];
                    string lastName = surnamePool[_seedSource.Next(surnamePool.Length)];
                    fullName = $"{firstName} {lastName}";
                }
                while (!used.Add(fullName));

                double baseSpeed = 11 + _seedSource.NextDouble() * 2;
                double injuryProbability = 0.005 + _seedSource.NextDouble() * 0.015;
                competition.AddAthlete(new Athlete(fullName, baseSpeed, injuryProbability));
            }

            CompetitionViewModel vm = new CompetitionViewModel(competition);
            vm.PropertyChanged += OnCompetitionVmChanged;
            Competitions.Add(vm);
        }

        private void RemoveCompetition(CompetitionViewModel? vm)
        {
            if (vm == null)
            {
                return;
            }

            if (vm.IsRunning && vm.StopCommand.CanExecute(null))
            {
                vm.StopCommand.Execute(null);
            }

            vm.PropertyChanged -= OnCompetitionVmChanged;
            Competitions.Remove(vm);
        }

        private void OnCompetitionVmChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompetitionViewModel.IsRunning))
            {
                OnPropertyChanged(nameof(RunningCount));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void OnDoctorHealingState(object? sender, AthleteEventArgs e)
        {
            // Keep doctor hookup alive; the CompetitionViewModel re-subscribes for logging.
        }

        private void StartAll()
        {
            foreach (CompetitionViewModel vm in Competitions)
            {
                if (!vm.IsRunning && vm.AthleteCount > 0 && vm.StartCommand.CanExecute(null))
                {
                    vm.StartCommand.Execute(null);
                }
            }
        }

        private void StopAll()
        {
            foreach (CompetitionViewModel vm in Competitions)
            {
                if (vm.IsRunning && vm.StopCommand.CanExecute(null))
                {
                    vm.StopCommand.Execute(null);
                }
            }
        }
    }
}
