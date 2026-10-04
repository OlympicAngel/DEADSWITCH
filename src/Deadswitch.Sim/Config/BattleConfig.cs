namespace Deadswitch.Sim.Config
{
    /// <summary>Short live battles (SPEC-020, doc 04 s7). Placeholders: tune at F-099.</summary>
    public sealed class BattleConfig : IConfigSection
    {
        public int BattleMinutes = 1;
        public int FocusDefensePct = 25;
        public int FocusCompute = 20;
        public int BarrageStrengthPct = 20;
        public int BarrageEnergy = 120;
        public int TakeoverDefensePct = 40;
        public int SeizeStrengthPct = 35;
        public int SeizeCorruption = 3_000;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("battle", "Short live battles: take command at contact and spend a few abilities (doc 04 s7). Corruption is milli. All (tune).");
            v.Int("battle_minutes", ref BattleMinutes, 1, 60, "How long the handler commands at the wall before the fight resolves (1 game minute = one real minute at 1x).");
            v.Int("focus_defense_pct", ref FocusDefensePct, 0, 500, "FOCUS FIRE: defense + this % for the fight.");
            v.Int("focus_compute", ref FocusCompute, 0, 10_000, "FOCUS FIRE: compute it costs.");
            v.Int("barrage_strength_pct", ref BarrageStrengthPct, 0, 100, "MORTAR BARRAGE: attack strength - this %.");
            v.Int("barrage_energy", ref BarrageEnergy, 0, 100_000, "MORTAR BARRAGE: energy it costs.");
            v.Int("takeover_defense_pct", ref TakeoverDefensePct, 0, 500, "MANUAL TAKEOVER (OVERRIDE): defense + this %.");
            v.Int("seize_strength_pct", ref SeizeStrengthPct, 0, 100, "MACHINE SEIZURE (OVERRIDE): attack strength - this % (their drones turn).");
            v.Int("seize_corruption", ref SeizeCorruption, 0, 100_000, "MACHINE SEIZURE: extra corruption on top of the OVERRIDE cost.");
            v.EndSection();
        }
    }
}
