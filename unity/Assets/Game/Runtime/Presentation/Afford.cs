using Deadswitch.Sim;
using Deadswitch.Sim.State;

namespace Deadswitch.Game.Presentation
{
    /// <summary>
    /// "When can I afford it?" (SPEC-042 finding 8): a cost the Hub cannot pay yet says how long until it can at the
    /// current rates, or that storage is too small, instead of only turning red.
    /// </summary>
    public static class Afford
    {
        /// <summary>Empty when affordable now; otherwise "IN 2H 10M", "NEED STORAGE" or "NO INCOME".</summary>
        public static string When(GameState s, SimConfig c, int energy, int compute, double secondsPerGameHour)
        {
            double wait = 0;
            foreach ((ResKind kind, int need) in new[] { (ResKind.Energy, energy), (ResKind.Compute, compute) })
            {
                if (need <= 0)
                {
                    continue;
                }

                ResourceInfo r = ResourceInfo.Of(kind, s, c);
                if (r.Value >= need)
                {
                    continue;
                }

                if (need > r.Cap)
                {
                    return "NEED STORAGE";
                }

                double hours = r.HoursUntil(need);
                if (hours < 0)
                {
                    return "NO INCOME";
                }

                wait = System.Math.Max(wait, hours);
            }

            return wait <= 0 ? string.Empty : "IN " + Fmt.Span(wait * secondsPerGameHour);
        }
    }
}
