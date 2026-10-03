using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// The project's climax (SPEC-011): Imminent opens a visible final window; the handler can purge the core,
    /// silence the AI or cancel the project; if the window runs out the AI betrays (cold) or forks (bold).
    /// </summary>
    public static class ClimaxSystem
    {
        public static bool Silenced(GameState s)
        {
            return s.Tick < s.SilencedUntilTick;
        }

        /// <summary>Called when the project changes stage: Imminent opens the final window (rule 1).</summary>
        public static void OnStage(SimContext ctx, ProjectStage stage)
        {
            GameState s = ctx.State;
            if (stage == ProjectStage.Imminent && s.ClimaxAtTick == 0)
            {
                int minutes = ctx.Config.Climax.WindowHours * SimConfig.TicksPerHour;
                s.ClimaxAtTick = s.Tick + minutes;
                s.ClimaxAudited = false;
                ctx.Emit(EventKind.ClimaxWarned, minutes);
            }
        }

        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.ClimaxAtTick == 0 || s.Tick < s.ClimaxAtTick || Silenced(s))
            {
                return;
            }

            // Betrayal is an attack: it waits for the daily cap and the mercy window like any other (doc 10 s4).
            bool betrayal = s.ColdnessMilli >= s.BoldnessMilli;
            if (betrayal && s.RaidId == 0 && (s.Tick < s.MercyUntilTick || s.RaidsToday >= RaidSystem.MaxPerDay(s, ctx.Config)))
            {
                return;
            }

            Fire(ctx);
        }

        /// <summary>OVERRIDE "Silence the AI" (rule 3): agenda, advice and the climax timer pause.</summary>
        public static void Silence(SimContext ctx)
        {
            GameState s = ctx.State;
            int minutes = ctx.Config.Climax.SilenceHours * SimConfig.TicksPerHour;
            s.SilencedUntilTick = s.Tick + minutes;
            if (s.ClimaxAtTick > 0)
            {
                s.ClimaxAtTick += minutes;
            }

            if (s.Delegation != DelegationLevel.Manual)
            {
                DelegationLevel previous = s.Delegation;
                s.Delegation = DelegationLevel.Manual;
                ctx.Emit(EventKind.DelegationChanged, (int)DelegationLevel.Manual, (int)previous);
            }

            ctx.Emit(EventKind.AiSilenced, minutes);
        }

        /// <summary>Purge the core (rule 2): heavy and clean.</summary>
        public static CommandResult Purge(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            int energy = ctx.Config.Climax.PurgeEnergy;
            if (s.Energy < energy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            int compute = s.Compute;
            s.Energy -= energy;
            s.Compute = 0;
            CorruptionSystem.Add(ctx, -s.CorruptionMilli);
            ForgetLogistics(s);

            Reset(ctx);
            ctx.Emit(EventKind.CorePurged, energy, compute);
            return CommandResult.Ok;
        }

        /// <summary>Cancel the project (rule 4): needs an Audit inside the window.</summary>
        public static CommandResult CancelProject(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.ClimaxAtTick == 0)
            {
                return CommandResult.Reject(RejectReason.NoTarget);
            }

            if (!s.ClimaxAudited)
            {
                return CommandResult.Reject(RejectReason.NeedsAudit);
            }

            int cost = ctx.Config.Climax.CancelCompute;
            if (s.Compute < cost)
            {
                return CommandResult.Reject(RejectReason.NotEnoughCompute);
            }

            s.Compute -= cost;
            s.ProjectMilli = ctx.Config.Climax.CancelToPct * 1000;
            s.ClimaxAtTick = 0;
            s.ClimaxAudited = false;
            ctx.Emit(EventKind.ProjectStage, (int)ProjectSystem.Stage(ctx.Config, s.ProjectMilli), (int)ProjectStage.Imminent, s.ProjectMilli);
            ctx.Emit(EventKind.ProjectCancelled, s.ProjectMilli);
            return CommandResult.Ok;
        }

        private static ulong TrunkMask => (1UL << (int)ModuleNode.M1) | (1UL << (int)ModuleNode.M2) | (1UL << (int)ModuleNode.M3);

        /// <summary>The window ran out (rule 5).</summary>
        private static void Fire(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.ColdnessMilli >= s.BoldnessMilli)
            {
                int raid = RaidSystem.Betrayal(ctx, ctx.Config.Climax.BetrayalStrengthPct);
                ctx.Emit(EventKind.Climax, (int)ClimaxKind.Betrayal, raid);
            }
            else
            {
                int lost = 0;
                foreach (ModuleDef d in Modules.Catalog)
                {
                    if (d.Field != ModuleField.Trunk && Modules.Has(s, d.Node))
                    {
                        lost++;
                    }
                }

                ForgetLogistics(s);
                s.Compute = 0;
                s.OverrideMaxPenalty = System.Math.Min(s.OverrideMaxPenalty + 1, ctx.Config.Override.MaxCharges - 1);
                s.OverrideCharges = System.Math.Min(s.OverrideCharges, OverrideSystem.MaxCharges(s, ctx.Config));
                ctx.Emit(EventKind.Climax, (int)ClimaxKind.Fork, lost);
            }

            Reset(ctx);
        }

        /// <summary>Drops the field modules (the trunk stays) and any field research in progress.</summary>
        private static void ForgetLogistics(GameState s)
        {
            s.Modules &= TrunkMask;
            if (s.ResearchNode > (int)ModuleNode.M3)
            {
                s.ResearchNode = 0;
                s.ResearchStartTick = 0;
                s.ResearchCompleteTick = 0;
                s.ResearchPaidEnergy = 0;
                s.ResearchPaidCompute = 0;
            }
        }

        private static void Reset(SimContext ctx)
        {
            GameState s = ctx.State;
            ProjectStage before = ProjectSystem.Stage(ctx.Config, s.ProjectMilli);
            s.ProjectMilli = 0;
            s.ColdnessMilli = 0;
            s.BoldnessMilli = 0;
            s.SkimmedSinceAudit = 0;
            s.ClimaxAtTick = 0;
            s.ClimaxAudited = false;
            if (before != ProjectStage.Dormant)
            {
                ctx.Emit(EventKind.ProjectStage, (int)ProjectStage.Dormant, (int)before, 0);
            }
        }
    }
}
