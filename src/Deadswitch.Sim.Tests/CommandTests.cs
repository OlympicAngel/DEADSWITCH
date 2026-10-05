using System.Collections.Generic;
using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class CommandTests
    {
        [Fact]
        public void AdGrants_AreConvenienceOnly_OncePerDay_AndTierOneOnly()
        {
            // doc 10 s1.1, ADR-0006: an extra salvage roll per day, a short idle-cap extension; never at Tier 2+
            var sim = new Simulation(4UL);
            int cap = Systems.Economy.EnergyCap(sim.State, sim.Config);
            Assert.True(sim.Execute(Command.ClaimAdGrant(Systems.AdGrant.SalvageRoll)).Accepted);
            Assert.Equal(RejectReason.OnCooldown, sim.Execute(Command.ClaimAdGrant(Systems.AdGrant.SalvageRoll)).Reason);
            Assert.True(sim.Execute(Command.ClaimAdGrant(Systems.AdGrant.IdleCap)).Accepted);
            Assert.True(Systems.Economy.EnergyCap(sim.State, sim.Config) > cap);
            sim.State.Tier = 2;
            sim.Run(SimConfig.TicksPerDay);
            Assert.Equal(RejectReason.Locked, sim.Execute(Command.ClaimAdGrant(Systems.AdGrant.SalvageRoll)).Reason);
        }

        [Fact]
        public void SetDelegation_AppliesRecordsAndEmits()
        {
            var sim = new Simulation(1UL);
            sim.Run(90);

            CommandResult result = sim.Execute(Command.SetDelegation(DelegationLevel.Autopilot));

            Assert.True(result.Accepted);
            Assert.Equal(DelegationLevel.Autopilot, sim.State.Delegation);
            RecordedCommand recorded = Assert.Single(sim.Commands.Commands);
            Assert.Equal(90, recorded.Tick);
            SimEvent e = sim.Log.Events.Last();
            Assert.Equal(EventKind.DelegationChanged, e.Kind);
            Assert.Equal((int)DelegationLevel.Autopilot, e.A);
            Assert.Equal((int)DelegationLevel.Manual, e.B);
            Assert.Equal(90, e.Tick);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(3, 0)]
        [InlineData(1, 7)]
        public void SetDelegation_InvalidArgs_AreRejectedAndChangeNothing(int level, int extra)
        {
            var sim = new Simulation(2UL);
            sim.Run(30);
            ulong before = StateHasher.Hash(sim.State);
            int events = sim.Log.Count;

            CommandResult result = sim.Execute(new Command(CommandKind.SetDelegation, level, extra));

            Assert.Equal(RejectReason.InvalidArgument, result.Reason);
            Assert.Equal(before, StateHasher.Hash(sim.State));
            Assert.Equal(events, sim.Log.Count);
            Assert.Equal(0, sim.Commands.Count);
        }

        [Fact]
        public void SetDelegation_ToCurrentLevel_IsNoChange()
        {
            var sim = new Simulation(3UL);

            CommandResult result = sim.Execute(Command.SetDelegation(DelegationLevel.Manual));

            Assert.Equal(RejectReason.NoChange, result.Reason);
            Assert.Equal(0, sim.Commands.Count);
        }

        [Fact]
        public void UnknownCommand_IsRejected()
        {
            var sim = new Simulation(4UL);

            CommandResult result = sim.Execute(new Command((CommandKind)9999));

            Assert.Equal(RejectReason.UnknownCommand, result.Reason);
        }

        [Fact]
        public void RejectedCommands_DoNotShiftTheRngStream()
        {
            var clean = new Simulation(5UL);
            var noisy = new Simulation(5UL);
            for (int i = 0; i < 2000; i++)
            {
                noisy.Execute(Command.SetDelegation(DelegationLevel.Manual));
                noisy.Execute(new Command(CommandKind.SetDelegation, 42));
                noisy.Step();
                clean.Step();
            }

            Assert.Equal(StateHasher.Hash(clean.State), StateHasher.Hash(noisy.State));
        }

        [Fact]
        public void Replay_ReproducesLiveRunExactly()
        {
            Simulation live = LiveRunWithCommands(77UL);

            Simulation replay = Replay.Run(77UL, SimConfig.Tier1(), live.Commands.Commands, live.State.Tick);

            Assert.Equal(StateHasher.Hash(live.State), StateHasher.Hash(replay.State));
            Assert.Equal(live.Log.Events, replay.Log.Events);
            Assert.Equal(live.Commands.Commands, replay.Commands.Commands);
        }

        [Fact]
        public void Replay_CanStopEarly()
        {
            Simulation live = LiveRunWithCommands(78UL);

            Simulation replay = Replay.Run(78UL, SimConfig.Tier1(), live.Commands.Commands, 500);

            Assert.Equal(500, replay.State.Tick);
            Assert.Equal(DelegationLevel.Delegated, replay.State.Delegation);
        }

        [Fact]
        public void Replay_OutOfOrderLog_Throws()
        {
            var commands = new List<RecordedCommand>
            {
                new RecordedCommand(100, Command.SetDelegation(DelegationLevel.Delegated)),
                new RecordedCommand(50, Command.SetDelegation(DelegationLevel.Autopilot)),
            };

            Assert.Throws<ReplayDivergenceException>(() => Replay.Run(1UL, SimConfig.Tier1(), commands, 200));
        }

        [Fact]
        public void Replay_CommandThatNoLongerValidates_Throws()
        {
            var commands = new List<RecordedCommand>
            {
                new RecordedCommand(10, Command.SetDelegation(DelegationLevel.Manual)),
            };

            Assert.Throws<ReplayDivergenceException>(() => Replay.Run(1UL, SimConfig.Tier1(), commands, 20));
        }

        [Fact]
        public void ChunkedRunWithCommands_EqualsSingleRun()
        {
            var whole = new Simulation(9UL);
            var chunked = new Simulation(9UL);

            // an alliance and a sabotage team in flight (SPEC-025, SPEC-026) cross the chunk boundaries too
            foreach (Simulation sim in new[] { whole, chunked })
            {
                sim.State.Energy = 700;
                sim.State.Fuel = 120;
                sim.Execute(Command.ProposeAlliance(Faction.Vanguard));
                sim.Execute(Command.LaunchOp(3, OpKind.Sabotage, 2));
            }

            whole.Run(700);
            whole.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            whole.Execute(Command.SetPresence(true));
            whole.Run(3000);

            chunked.Run(300);
            chunked.Run(400);
            chunked.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            chunked.Execute(Command.SetPresence(true));
            chunked.Run(1);
            chunked.Run(2999);

            Assert.Contains(whole.Log.Events, e => e.Kind == EventKind.AllianceFormed);
            Assert.Contains(whole.Log.Events, e => e.Kind == EventKind.OpLaunched && e.C == (int)OpKind.Sabotage);
            Assert.Equal(StateHasher.Hash(whole.State), StateHasher.Hash(chunked.State));
        }

        [Fact]
        public void EventSequenceNumbers_AreUniqueAndIncreasing()
        {
            Simulation live = LiveRunWithCommands(80UL);

            long[] seqs = live.Log.Events.Select(e => e.Seq).ToArray();

            Assert.Equal(Enumerable.Range(0, seqs.Length).Select(i => (long)i), seqs);
            Assert.Equal(seqs.Length, live.Log.NextSeq);
        }

        private static Simulation LiveRunWithCommands(ulong seed)
        {
            var sim = new Simulation(seed);
            sim.Execute(Command.SetDelegation(DelegationLevel.Delegated));
            sim.Run(1440);
            sim.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            sim.Execute(Command.SetDelegation(DelegationLevel.Manual));
            sim.Run(1440 * 2);
            sim.Execute(Command.SetDelegation(DelegationLevel.Delegated));
            sim.Run(17);
            return sim;
        }
    }
}
