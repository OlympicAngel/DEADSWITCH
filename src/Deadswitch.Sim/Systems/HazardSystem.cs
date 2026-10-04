using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Hazard zones and the drifting fallout front (SPEC-032, doc 05 s6-7). Wild zones pay what the factions do not
    /// (survivors, repair parts, rich salvage) at a zone-specific risk that scouting halves. Fallout drifts between
    /// faction sites and makes ops there dearer and riskier. Decisions use hashes, never RNG draws.
    /// </summary>
    public static class HazardSystem
    {
        /// <summary>A wild zone: owned by nobody, no heat, no diplomacy (rule 1).</summary>
        public static bool Wild(SiteKind kind)
        {
            return kind >= SiteKind.Radiation;
        }

        public static bool Covered(GameState s, int site)
        {
            return s.FalloutSite == site && site >= 0;
        }

        /// <summary>Chance in percent a squad back from this site falls sick (radiation zone or fallout), halved once scouted.</summary>
        public static int SickPct(GameState s, SimConfig c, int site)
        {
            SiteDef d = WorldSystem.Sites[site];
            return d.Kind == SiteKind.Radiation || Covered(s, site) ? Prepared(s, c, site, c.Hazards.RadiationSickPct) : 0;
        }

        public static int InfectionPct(GameState s, SimConfig c, int site)
        {
            return WorldSystem.Sites[site].Kind == SiteKind.Plague ? Prepared(s, c, site, c.Hazards.PlagueInfectionPct) : 0;
        }

        /// <summary>Extra fuel percent to reach a site (radiation zone and fallout stack).</summary>
        public static int FuelPct(GameState s, SimConfig c, int site)
        {
            int pct = WorldSystem.Sites[site].Kind == SiteKind.Radiation ? c.Hazards.RadiationFuelPct : 0;
            return pct + (Covered(s, site) ? c.Hazards.FalloutFuelPct : 0);
        }

        /// <summary>Hazard effects on a returning squad (survivors are already home). Returns the extra people lost from the squad.</summary>
        public static int Returned(SimContext ctx, Operation op, bool won, int survivors)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            HazardConfig h = c.Hazards;
            SiteDef d = WorldSystem.Sites[op.Site];
            if (op.Kind == OpKind.Hack)
            {
                return 0;
            }

            int lost = 0;
            if (d.Kind == SiteKind.Graveyard && !won && op.Kind == OpKind.Raid)
            {
                // drone nests chase the squad out
                lost += System.Math.Min(survivors, (op.Squad * h.GraveyardNestPct) / 100);
            }

            if (survivors - lost > 0 && Roll(s, op.Id, 0x51C4u) < SickPct(s, c, op.Site))
            {
                int sick = System.Math.Min(survivors - lost, System.Math.Max(1, ((survivors - lost) * h.RadiationCasualtyPct) / 100));
                lost += sick;
                ctx.Emit(EventKind.HazardSickness, op.Site, sick);
            }

            s.People -= lost;
            if (won && op.Kind == OpKind.Raid)
            {
                if (d.Kind == SiteKind.Plague)
                {
                    int room = Economy.PopulationCap(s, c) - s.People - WorldSystem.Away(s) - IntelSystem.Away(s, c);
                    int found = System.Math.Min(h.PlaguePeople, System.Math.Max(0, room));
                    if (found > 0)
                    {
                        s.People += found;
                        ctx.Emit(EventKind.SurvivorsFound, op.Site, found);
                    }
                }
                else if (d.Kind == SiteKind.Graveyard)
                {
                    Repair(ctx, h.GraveyardRepairPoints);
                }
            }

            if (Roll(s, op.Id, 0x9A61u) < InfectionPct(s, c, op.Site))
            {
                int dead = System.Math.Min(h.PlagueInfected, System.Math.Max(0, s.People - s.Garrison - c.PeopleChoices.MinPeople));
                if (dead > 0)
                {
                    s.People -= dead;
                    ctx.Emit(EventKind.PlagueInfection, op.Site, dead);
                }
            }

            return lost;
        }

        /// <summary>Outpost output percent at a site (fallout cuts it).</summary>
        public static int OutpostPct(GameState s, SimConfig c, int site)
        {
            return Covered(s, site) ? c.Hazards.FalloutOutpostPct : 100;
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            HazardConfig h = ctx.Config.Hazards;
            if (s.Tick < (long)h.FalloutFirstDay * SimConfig.TicksPerDay || s.Tick < s.NextFalloutTick)
            {
                return;
            }

            int from = s.FalloutSite;
            int to = Next(s, from);
            long hour = s.Tick / SimConfig.TicksPerHour;
            int hours = h.FalloutDriftHours + (int)(SimMath.Hash((uint)hour ^ 0xFA11u, (uint)(s.Rng.State >> 32)) % (uint)(h.FalloutJitterHours + 1));
            s.FalloutSite = to;
            s.NextFalloutTick = s.Tick + ((long)hours * SimConfig.TicksPerHour);
            ctx.Emit(EventKind.FalloutDrifted, to, from, hours);
        }

        /// <summary>One of the two faction sites nearest the front (or the crater before it first settles), by hash.</summary>
        private static int Next(GameState s, int from)
        {
            int ax;
            int ay;
            if (from >= 0)
            {
                ax = WorldSystem.Sites[from].MapX;
                ay = WorldSystem.Sites[from].MapY;
            }
            else
            {
                ax = 0;
                ay = 0;
                for (int i = 0; i < WorldSystem.Sites.Count; i++)
                {
                    if (WorldSystem.Sites[i].Kind == SiteKind.Radiation)
                    {
                        ax = WorldSystem.Sites[i].MapX;
                        ay = WorldSystem.Sites[i].MapY;
                        break;
                    }
                }
            }

            int first = -1;
            int second = -1;
            int firstDist = int.MaxValue;
            int secondDist = int.MaxValue;
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                if (i == from || Wild(d.Kind))
                {
                    continue;
                }

                int dist = ((d.MapX - ax) * (d.MapX - ax)) + ((d.MapY - ay) * (d.MapY - ay));
                if (dist < firstDist)
                {
                    second = first;
                    secondDist = firstDist;
                    first = i;
                    firstDist = dist;
                }
                else if (dist < secondDist)
                {
                    second = i;
                    secondDist = dist;
                }
            }

            if (second < 0)
            {
                return first;
            }

            uint pick = SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerHour) ^ 0xD21Fu, (uint)(s.Rng.State >> 32)) % 2u;
            return pick == 0 ? first : second;
        }

        /// <summary>Parts fix the most damaged facility that is not already under repair.</summary>
        private static void Repair(SimContext ctx, int points)
        {
            GameState s = ctx.State;
            int best = -1;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot f = s.Slots[i];
                if (!f.IsEmpty && f.Damage > 0 && !ScarSystem.Repairing(s, f) && (best < 0 || f.Damage > s.Slots[best].Damage))
                {
                    best = i;
                }
            }

            if (best < 0 || points <= 0)
            {
                return;
            }

            FacilitySlot hit = s.Slots[best];
            int fixedPoints = System.Math.Min(points, hit.Damage);
            hit.Damage -= fixedPoints;
            ctx.Emit(EventKind.PartsRecovered, best, fixedPoints);
        }

        /// <summary>Scouting first is the preparation (rule 5).</summary>
        private static int Prepared(GameState s, SimConfig c, int site, int pct)
        {
            return s.Sites[site].Scouted ? SimMath.PctFloor(pct, c.Hazards.PreparedRiskPct) : pct;
        }

        private static int Roll(GameState s, int opId, uint salt)
        {
            return (int)(SimMath.Hash((uint)opId * 2246822519u ^ salt, (uint)(s.Rng.State >> 32)) % 100u);
        }
    }
}
