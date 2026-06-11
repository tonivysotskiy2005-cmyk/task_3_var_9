using System;

namespace SportModels
{
    public class AthleteEventArgs : EventArgs
    {
        public AthleteEventArgs(Athlete athlete, string message)
        {
            Athlete = athlete;
            Message = message;
            Timestamp = DateTime.Now;
        }

        public Athlete Athlete { get; }

        public string Message { get; }

        public DateTime Timestamp { get; }
    }

    public class WinnerEventArgs : EventArgs
    {
        public WinnerEventArgs(Athlete winner, Award award)
        {
            Winner = winner;
            Award = award;
            Timestamp = DateTime.Now;
        }

        public Athlete Winner { get; }

        public Award Award { get; }

        public DateTime Timestamp { get; }
    }
}
