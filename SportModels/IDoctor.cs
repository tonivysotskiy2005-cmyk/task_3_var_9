using System.Threading;

namespace SportModels
{
    public interface IDoctor
    {
        string Name { get; }

        int HealDurationMs { get; }

        void Heal(Athlete athlete, CancellationToken cancellationToken);
    }
}
