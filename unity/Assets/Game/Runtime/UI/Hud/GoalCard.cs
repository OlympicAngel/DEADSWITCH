using Deadswitch.Game.Presentation;
using Deadswitch.Sim;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The next goal (SPEC-043 s5): the next tier's gates on BASE with progress, so the long-term aim is always one
    /// glance away. The same gates as MODULES (doc 06 s2): facility levels, net power, a restored memory and the
    /// people who leave to expand. Shown once the opening objectives are done; tap opens MODULES.
    /// </summary>
    public sealed class GoalCard
    {
        private readonly VisualElement _root;
        private readonly VisualElement _fill;
        private bool _wasReady;

        public GoalCard(VisualElement root, System.Action open)
        {
            _root = root;
            _fill = root.Q("goal-bar").Q(className: "ds-progress__fill");
            _root.RegisterCallback<ClickEvent>(_ => open());
        }

        public void Refresh(GameState s, SimConfig c, bool allowed)
        {
            bool final = s.Tier >= c.Tier.MaxTier || s.Tier - 1 >= c.Tier.LevelsToAdvance.Length;
            _root.EnableInClassList("is-hidden", !allowed || final);
            if (!allowed || final)
            {
                return;
            }

            TierGates g = Modules.Gates(s, c);
            bool levels = g.Levels >= g.LevelsNeeded;
            bool power = g.NetEnergy >= g.NetEnergyNeeded;
            Gate("goal-levels", levels, "LEVELS " + g.Levels + "/" + g.LevelsNeeded);
            Gate("goal-power", power, "POWER " + Fmt.Signed(g.NetEnergy) + "/" + Fmt.Signed(g.NetEnergyNeeded) + " H");
            Gate("goal-memory", g.ModuleRestored, g.Module == ModuleNode.None ? "MEMORY" : ModuleTexts.Name(g.Module));
            Gate("goal-people", g.PeopleAvailable, g.PeopleCost + " PEOPLE");

            // progress: the level gate counts by level, the other three as whole steps
            float levelPart = g.LevelsNeeded > 0 ? System.Math.Min(1f, g.Levels / (float)g.LevelsNeeded) : 1f;
            float done = (levelPart + (power ? 1f : 0f) + (g.ModuleRestored ? 1f : 0f) + (g.PeopleAvailable ? 1f : 0f)) / 4f;
            _fill.style.width = Length.Percent(done * 100f);
            bool ready = g.All;
            _root.EnableInClassList("is-ready", ready);
            _root.Q<Label>("goal-title").text = ready
                ? "READY // ADVANCE TO TIER " + (s.Tier + 1) + " " + ModuleTexts.TierName(s.Tier + 1)
                : "NEXT GOAL // TIER " + (s.Tier + 1) + " " + ModuleTexts.TierName(s.Tier + 1);
            if (ready && !_wasReady)
            {
                Choreo.Punch(_root, 0.06f);
            }

            _wasReady = ready;
        }

        private void Gate(string name, bool met, string text)
        {
            VisualElement gate = _root.Q(name);
            gate.EnableInClassList("is-met", met);
            gate.Q<Label>(className: "hud-goal__gate-label").text = text;
        }
    }
}
