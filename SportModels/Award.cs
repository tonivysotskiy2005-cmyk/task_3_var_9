using System;

namespace SportModels
{
    public class Award
    {
        public Award(string title, string description, DateTime issuedAt)
        {
            Title = title;
            Description = description;
            IssuedAt = issuedAt;
        }

        public string Title { get; }

        public string Description { get; }

        public DateTime IssuedAt { get; }

        public override string ToString()
        {
            return $"{Title} — {Description} ({IssuedAt:HH:mm:ss})";
        }
    }
}
