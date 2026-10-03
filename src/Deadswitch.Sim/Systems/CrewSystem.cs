using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Assigns people to facilities in priority order (SPEC-002 rule 6). Short-handed facilities run unmanned
    /// at reduced output; their count is the automation load.
    /// </summary>
    public static class CrewSystem
    {
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            int available = s.People - s.Garrison;
            int load = 0;
            foreach (int id in s.PowerPriority)
            {
                FacilitySlot slot = s.Slots[id];
                if (slot.IsEmpty || !slot.Enabled)
                {
                    slot.Staffed = false;
                    continue;
                }

                int need = Economy.CrewNeeded(ctx.Config, slot);
                if (need <= available)
                {
                    slot.Staffed = true;
                    available -= need;
                }
                else
                {
                    slot.Staffed = false;
                    load++;
                }
            }

            s.AutomationLoad = load;
        }
    }
}
