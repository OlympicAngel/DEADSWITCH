namespace Deadswitch.Sim.Config
{
    /// <summary>Ruthless choices and loyalty (SPEC-012, doc 02 s5, doc 10 s1.3).</summary>
    public sealed class PeopleChoiceConfig : IConfigSection
    {
        public int StrainedBelow = 60;
        public int MutinousBelow = 30;
        public int RecoverPerHour = 400;
        public int StrainedOutputPts = 10;
        public int MutinousOutputPts = 25;
        public int MinPeople = 4;
        public int SurgeDeaths = 2;
        public int SurgeHours = 6;
        public int SurgeOutputPts = 40;
        public int SurgeLoyalty = 20_000;
        public int SurgeColdness = 15_000;
        public int SurgeCooldownHours = 24;
        public int CleanseMax = 5;
        public int CleansePerPerson = 8_000;
        public int CleanseLoyalty = 5_000;
        public int CleanseColdness = 6_000;
        public int CrackdownPeople = 2;
        public int CrackdownLoyalty = 25_000;
        public int CrackdownColdness = 20_000;
        public int RogueChancePct = 35;
        public int RogueEnergy = 80;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("people_choices", "Ruthless choices and loyalty (SPEC-012). Loyalty and Coldness are milli-units: 100000 = 100%. All (tune).");
            v.Int("strained_below", ref StrainedBelow, 1, 100, "Loyalty under this % is Strained.");
            v.Int("mutinous_below", ref MutinousBelow, 0, 100, "Loyalty under this % is Mutinous.");
            v.Int("recover_per_hour", ref RecoverPerHour, 0, 100_000, "Loyalty regained per game hour while no surge runs.");
            v.Int("strained_output_pts", ref StrainedOutputPts, 0, 100, "Generator and Server Rack output lost while Strained (points).");
            v.Int("mutinous_output_pts", ref MutinousOutputPts, 0, 100, "Generator and Server Rack output lost while Mutinous (points).");
            v.Int("min_people", ref MinPeople, 1, 1_000, "No choice or event takes the population below this.");
            v.Int("surge_deaths", ref SurgeDeaths, 0, 100, "Forced labor surge: people who die at once.");
            v.Int("surge_hours", ref SurgeHours, 1, 1_000, "Forced labor surge: game hours of boosted output.");
            v.Int("surge_output_pts", ref SurgeOutputPts, 0, 1_000, "Forced labor surge: Generator and Server Rack output bonus (points).");
            v.Int("surge_loyalty", ref SurgeLoyalty, 0, 100_000, "Forced labor surge: loyalty lost.");
            v.Int("surge_coldness", ref SurgeColdness, 0, 100_000, "Forced labor surge: Coldness gained.");
            v.Int("surge_cooldown_hours", ref SurgeCooldownHours, 0, 1_000, "Forced labor surge: game hours before the next one.");
            v.Int("cleanse_max", ref CleanseMax, 1, 100, "Neural cleansing: most people used at once.");
            v.Int("cleanse_per_person", ref CleansePerPerson, 0, 100_000, "Neural cleansing: corruption removed per person (milli).");
            v.Int("cleanse_loyalty", ref CleanseLoyalty, 0, 100_000, "Neural cleansing: loyalty lost per person.");
            v.Int("cleanse_coldness", ref CleanseColdness, 0, 100_000, "Neural cleansing: Coldness gained per person.");
            v.Int("crackdown_people", ref CrackdownPeople, 0, 100, "Crackdown: people removed.");
            v.Int("crackdown_loyalty", ref CrackdownLoyalty, 0, 100_000, "Crackdown: loyalty restored (fear).");
            v.Int("crackdown_coldness", ref CrackdownColdness, 0, 100_000, "Crackdown: Coldness gained.");
            v.Int("rogue_chance_pct", ref RogueChancePct, 0, 100, "Daily chance an operator goes rogue while Mutinous.");
            v.Int("rogue_energy", ref RogueEnergy, 0, 100_000, "Energy a rogue operator takes.");
            v.EndSection();
        }
    }
}
