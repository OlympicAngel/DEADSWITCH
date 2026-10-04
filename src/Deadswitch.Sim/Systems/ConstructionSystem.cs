using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Completes construction jobs whose timer ran out, in queue order (SPEC-002 rule 4).</summary>
    public static class ConstructionSystem
    {
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            for (int i = 0; i < s.Jobs.Count;)
            {
                BuildJob job = s.Jobs[i];
                if (job.CompleteTick > s.Tick)
                {
                    i++;
                    continue;
                }

                FacilitySlot slot = s.Slots[job.Slot];
                bool isNew = slot.IsEmpty;
                slot.Kind = job.Kind;
                slot.Level = job.TargetLevel;
                if (isNew)
                {
                    slot.Enabled = true;
                    slot.Powered = true;
                    slot.Staffed = false;
                }

                s.Jobs.RemoveAt(i);
                ctx.Emit(EventKind.BuildCompleted, job.Slot, (int)job.Kind, job.TargetLevel);
            }
        }
    }
}
