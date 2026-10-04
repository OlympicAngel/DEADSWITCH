using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// The AI builds in secret (SPEC-034, doc 03 s5): while bold it pours the compute it skims into hidden nodes.
    /// Each node speeds the project and draws energy off the books, so the stock drifts below what the rates
    /// promise. An Audit exposes the nodes; exposed nodes can be dismantled, setting the project back. Silencing
    /// the AI pauses them; purging the core (or its climax) wipes them. No RNG draws.
    /// </summary>
    public static class SecretSystem
    {
        /// <summary>Called by the project's hour (after the silence check) with the compute skimmed this hour.</summary>
        public static int Hourly(SimContext ctx, int skim)
        {
            GameState s = ctx.State;
            SecretConfig c = ctx.Config.Secrets;
            if (s.BoldnessMilli >= c.FromBoldnessPct * 1000)
            {
                s.SecretPool += skim;
                while (s.SecretPool >= c.NodeCompute && s.SecretNodes < c.MaxNodes)
                {
                    s.SecretPool -= c.NodeCompute;
                    s.SecretNodes++;
                    ctx.Emit(EventKind.SecretBuilt, s.SecretNodes);
                }
            }

            // the drift: energy gone with no line on any ledger
            s.Energy -= System.Math.Min(s.Energy, s.SecretNodes * c.NodeEnergyPerHour);
            return s.SecretNodes * c.NodeGrowthPerHour;
        }

        /// <summary>The Audit finds every node standing.</summary>
        public static void Audited(SimContext ctx)
        {
            GameState s = ctx.State;
            s.SecretExposed = s.SecretNodes;
            ctx.Emit(EventKind.SecretExposed, s.SecretNodes);
        }

        public static CommandResult Dismantle(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.SecretExposed <= 0)
            {
                return CommandResult.Reject(RejectReason.NothingPending);
            }

            int n = s.SecretExposed;
            s.SecretNodes -= n;
            s.SecretExposed = 0;
            ProjectStage before = ProjectSystem.Stage(c, s.ProjectMilli);
            s.ProjectMilli = System.Math.Max(0, s.ProjectMilli - (n * c.Secrets.DismantleProjectMilli));
            ProjectStage after = ProjectSystem.Stage(c, s.ProjectMilli);
            if (after != before)
            {
                ctx.Emit(EventKind.ProjectStage, (int)after, (int)before, s.ProjectMilli);
            }

            int salvage = System.Math.Max(0, System.Math.Min(n * c.Secrets.DismantleCompute, c.Compute.Cap - s.Compute));
            s.Compute += salvage;
            ctx.Emit(EventKind.SecretDismantled, n, salvage);
            return CommandResult.Ok;
        }

        /// <summary>A purged core (or its climax) leaves nothing behind.</summary>
        public static void Reset(GameState s)
        {
            s.SecretNodes = 0;
            s.SecretExposed = 0;
            s.SecretPool = 0;
        }
    }
}
