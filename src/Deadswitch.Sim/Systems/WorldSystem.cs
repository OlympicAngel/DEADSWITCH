using System.Collections.Generic;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>One map site (content catalog; numbers are placeholders, tune at F-099).</summary>
    public readonly struct SiteDef
    {
        public SiteDef(string name, Faction owner, SiteKind kind, int mapX, int mapY, int travelHours, int defense, int cyber, int energy, int fuel, int compute, int cleanData)
        {
            Name = name;
            Owner = owner;
            Kind = kind;
            MapX = mapX;
            MapY = mapY;
            TravelHours = travelHours;
            Defense = defense;
            Cyber = cyber;
            Energy = energy;
            Fuel = fuel;
            Compute = compute;
            CleanData = cleanData;
        }

        public string Name { get; }

        public Faction Owner { get; }

        public SiteKind Kind { get; }

        /// <summary>Map position around the Hub at (0, 0), -100..100 each way (north is +Y).</summary>
        public int MapX { get; }

        public int MapY { get; }

        /// <summary>One-way travel time.</summary>
        public int TravelHours { get; }

        public int Defense { get; }

        public int Cyber { get; }

        public int Energy { get; }

        public int Fuel { get; }

        public int Compute { get; }

        /// <summary>Corruption a successful hack scrubs (milli): clean archives (doc 03 s3).</summary>
        public int CleanData { get; }
    }

    /// <summary>
    /// The world beyond the walls (doc 04 s8, doc 05, doc 10 s4): faction heat, operations (scout, raid, hack),
    /// outposts, and which faction an attack comes from. Decisions use hashes, never RNG draws.
    /// </summary>
    public static class WorldSystem
    {
        public const int FactionCount = 4;

        private static readonly SiteDef[] CatalogArray =
        {
            new SiteDef("RUST MARKET", Faction.Rustborn, SiteKind.Outpost, -40, 30, 2, 30, 20, 200, 40, 20, 0),
            new SiteDef("SCRAP CONVOY ROUTE", Faction.Rustborn, SiteKind.Convoy, 55, 20, 1, 18, 10, 120, 60, 0, 0),
            new SiteDef("KESS'S YARD", Faction.Rustborn, SiteKind.Outpost, -70, -50, 4, 70, 30, 400, 80, 40, 0),
            new SiteDef("COLLAPSED MALL", Faction.Rustborn, SiteKind.Ruins, 20, 60, 2, 25, 0, 150, 30, 10, 0),
            new SiteDef("RELAY 9", Faction.Vanguard, SiteKind.DataCenter, 30, -60, 3, 80, 60, 100, 0, 120, 6_000),
            new SiteDef("FOB EAGLE", Faction.Vanguard, SiteKind.Outpost, 80, -20, 5, 120, 70, 500, 120, 60, 0),
            new SiteDef("SUPPLY ROAD 7", Faction.Vanguard, SiteKind.Convoy, -20, -85, 3, 50, 30, 200, 100, 30, 0),
            new SiteDef("WATER PLANT", Faction.Vanguard, SiteKind.Ruins, -55, -5, 2, 40, 0, 180, 40, 0, 0),
            new SiteDef("CATHEDRAL ARRAY", Faction.Church, SiteKind.DataCenter, -85, 70, 6, 60, 110, 50, 0, 200, 10_000),
            new SiteDef("HALCYON VAULT", Faction.Holdouts, SiteKind.DataCenter, 85, 65, 7, 150, 160, 300, 0, 300, 14_000),
            new SiteDef("MERCENARY COMPOUND", Faction.Holdouts, SiteKind.Outpost, 60, -88, 6, 170, 80, 700, 160, 80, 0),

            // wild hazard zones (SPEC-032): owned by nobody; the Owner field is unused for them
            new SiteDef("BLACK CRATER", Faction.Rustborn, SiteKind.Radiation, 10, -30, 2, 35, 0, 380, 20, 90, 0),
            new SiteDef("QUARANTINE BLOCK", Faction.Rustborn, SiteKind.Plague, -30, 85, 3, 30, 0, 60, 0, 70, 0),
            new SiteDef("DRONE BONEYARD", Faction.Rustborn, SiteKind.Graveyard, 45, 45, 3, 95, 0, 220, 70, 50, 0),
        };

        public static IReadOnlyList<SiteDef> Sites => CatalogArray;

        /// <summary>People out on operations (they come home; regrowth leaves their place free).</summary>
        public static int Away(GameState s)
        {
            int n = 0;
            foreach (Operation op in s.Ops)
            {
                n += op.Squad;
            }

            return n;
        }

        /// <summary>The faction with the most heat (ties: lowest index): who sends a purge.</summary>
        public static Faction Hottest(GameState s)
        {
            int best = 0;
            for (int f = 1; f < FactionCount; f++)
            {
                if (s.Heat[f] > s.Heat[best])
                {
                    best = f;
                }
            }

            return (Faction)best;
        }

        public static int Percent(int milli)
        {
            return milli / 1000;
        }

        public static HeatLevel Level(int milli)
        {
            int pct = Percent(milli);
            return pct >= 75 ? HeatLevel.Marked : pct >= 50 ? HeatLevel.Hunted : pct >= 25 ? HeatLevel.Watched : HeatLevel.Cold;
        }

        public static bool AnyMarked(GameState s)
        {
            for (int f = 0; f < FactionCount; f++)
            {
                if (Level(s.Heat[f]) == HeatLevel.Marked)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A site's real defense: faction sites adapt and swing with luck; wild zones do not.</summary>
        public static int TrueDefense(GameState s, SimConfig c, int site)
        {
            SiteDef d = CatalogArray[site];
            return HazardSystem.Wild(d.Kind) ? d.Defense : LuckSystem.SiteDefense(s, c, AdaptSystem.SiteDefense(s, c, d.Defense, d.Owner), d.Owner);
        }

        /// <summary>The AI's read of a site's defense: exact once scouted, otherwise off by a stable error (may be wrong).</summary>
        public static int EstimatedDefense(GameState s, SimConfig c, int site)
        {
            SiteDef d = CatalogArray[site];
            bool wild = HazardSystem.Wild(d.Kind);
            int defense = TrueDefense(s, c, site);
            if (s.Sites[site].Scouted || Modules.Has(s, ModuleNode.ST2A) || (!wild && IntelSystem.Loyal(s, d.Owner)))
            {
                return defense;
            }

            if (!wild && IntelSystem.Double(s, d.Owner))
            {
                // the double agent talks the site down (SPEC-019)
                return System.Math.Max(1, SimMath.PctFloor(defense, c.Intel.DoubleSiteDefensePct));
            }

            int span = (2 * c.World.EstimateErrorPct) + 1;
            int err = (int)(SimMath.Hash((uint)site * 7919u, 0x5173u) % (uint)span) - c.World.EstimateErrorPct;
            return System.Math.Max(1, SimMath.PctFloor(defense, 100 + err));
        }

        /// <summary>Success chance in percent against a given defense (also used by the UI with the AI's estimate).</summary>
        public static int Odds(GameState s, SimConfig c, SiteDef d, OpKind kind, int squad, int compute, int defense)
        {
            var m = c.Modules;
            switch (kind)
            {
                case OpKind.Scout:
                    return SimMath.Clamp(55 + (squad * 12) - (defense / 4), 10, 95);
                case OpKind.Sabotage:
                    // stealth over strength: a small team, helped by masking and a loyal agent inside
                    int masking = Modules.Has(s, ModuleNode.ST1) ? c.Modules.MaskingPts : 0;
                    int inside = s.Spies[(int)d.Owner] == SpyState.Loyal ? c.World.SabotageSpyPts : 0;
                    return SimMath.Clamp(40 + (squad * 10) + masking + inside - (defense / 6), 5, 90);
                case OpKind.Hack:
                    int counter = Modules.Has(s, ModuleNode.CY2A) ? m.CounterIntrusionPts : 0;
                    int storm = LivingSystem.Active(s, WorldEventKind.SignalStorm) ? c.Living.SignalStormOddsPts : 0;
                    return SimMath.Clamp(50 + counter - storm + ((compute - d.Cyber) * 50 / System.Math.Max(1, d.Cyber)), 5, 95);
                default:
                    int power = squad * (c.World.StrengthPerPerson + (Modules.Has(s, ModuleNode.WF6) ? m.VeteranStrength : 0));
                    int sims = Modules.Has(s, ModuleNode.WF3) ? m.CombatSimsPts : 0;
                    return SimMath.Clamp(50 + sims + ((power - defense) * 50 / System.Math.Max(1, defense)), 5, 95);
            }
        }

        public static int FuelCost(GameState s, SimConfig c, int site, OpKind kind)
        {
            int fuel = kind == OpKind.Hack ? 0 : SimMath.PctFloor(2 * CatalogArray[site].TravelHours * c.World.FuelPerTravelHour, 100 + HazardSystem.FuelPct(s, c, site));
            return Modules.Has(s, ModuleNode.ST2B) ? SimMath.PctFloor(fuel, 100 - c.Modules.QuietRoutesPct) : fuel;
        }

        public static int MaxOps(GameState s, SimConfig c)
        {
            return c.World.MaxOps + (Modules.Has(s, ModuleNode.ST6) ? c.Modules.GhostOps : 0);
        }

        /// <summary>Which faction an incoming attack comes from: weighted by base weight + heat (hash, no RNG draw).</summary>
        public static Faction PickAttacker(GameState s, SimConfig c)
        {
            WorldConfig w = c.World;
            int[] weights =
            {
                w.AttackerWeightRustborn + Percent(s.Heat[0]),
                s.Tier >= 2 ? w.AttackerWeightVanguardTier2 + Percent(s.Heat[1]) : Percent(s.Heat[1]),
                Modules.IsRestored(s, ModuleNode.M1) ? w.AttackerWeightChurch + Percent(s.Heat[2]) : Percent(s.Heat[2]),
                s.Tier >= 3 ? w.AttackerWeightHoldoutsTier3 + Percent(s.Heat[3]) : Percent(s.Heat[3]),
            };
            // a faction under ceasefire sends nobody (SPEC-023); drifters from another camp still come
            if (s.CeasefireFaction >= 0 && s.Tick < s.CeasefireUntilTick)
            {
                weights[s.CeasefireFaction] = 0;
            }

            // an ally sends fighters, not raiders (SPEC-025)
            if (s.AllyFaction >= 0)
            {
                weights[s.AllyFaction] = 0;
            }

            int total = weights[0] + weights[1] + weights[2] + weights[3];
            if (total <= 0)
            {
                // nobody wants a fight: drifters from the first camp that is neither at peace nor allied
                for (int f = 0; f < FactionCount; f++)
                {
                    if (!DiplomacySystem.Ceasefire(s, (Faction)f) && !DiplomacySystem.Allied(s, (Faction)f))
                    {
                        return (Faction)f;
                    }
                }

                return Faction.Rustborn;
            }

            int roll = (int)(SimMath.Hash((uint)s.NextRaidId * 31u, (uint)(s.Rng.State >> 32) ^ 0xFAC7u) % (uint)total);
            for (int f = 0; f < FactionCount; f++)
            {
                if (roll < weights[f])
                {
                    return (Faction)f;
                }

                roll -= weights[f];
            }

            return Faction.Rustborn;
        }

        /// <summary>Attack strength scaled by the attacker's heat (doc 10: x (1 + 0.8 x heat)).</summary>
        public static int ScaleByHeat(GameState s, SimConfig c, Faction f, int strength)
        {
            long k = 1000 + ((long)c.World.HeatStrengthPermille * s.Heat[(int)f] / 100_000);
            return (int)(strength * k / 1000);
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            WorldConfig w = ctx.Config.World;
            for (int f = 0; f < FactionCount; f++)
            {
                int decay = Modules.Has(s, ModuleNode.ST3) ? SimMath.PctFloor(w.HeatDecayPerHour, 100 + ctx.Config.Modules.HeatSinkPct) : w.HeatDecayPerHour;
                AddHeat(ctx, (Faction)f, -SimMath.PctFloor(decay, 100 + (s.Perks[(int)Perk.HeatDecay] * ctx.Config.Legacy.PerkHeatDecayPct)));
            }

            for (int i = 0; i < s.Sites.Count; i++)
            {
                SiteState site = s.Sites[i];
                if (!site.Outpost)
                {
                    continue;
                }

                // conquer and hold (doc 04 s8): a seized faction outpost pays more and is wanted back sooner
                // under the fallout front it sends only a share (SPEC-032)
                bool held = CatalogArray[i].Kind == SiteKind.Outpost;
                int pct = HazardSystem.OutpostPct(s, ctx.Config, i);
                s.Energy = System.Math.Max(s.Energy, System.Math.Min(Economy.EnergyCap(s, ctx.Config), s.Energy + SimMath.PctFloor(held ? w.HeldEnergyPerHour : w.OutpostEnergyPerHour, pct)));
                s.Fuel = System.Math.Max(s.Fuel, System.Math.Min(ctx.Config.Fuel.Cap, s.Fuel + SimMath.PctFloor(held ? w.HeldFuelPerHour : w.OutpostFuelPerHour, pct)));

                // a hunting faction takes its ground back (a held outpost's owner already at Watched)
                Faction owner = CatalogArray[i].Owner;
                if (Level(s.Heat[(int)owner]) >= (held ? HeatLevel.Watched : HeatLevel.Hunted)
                    && SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerHour) ^ (uint)(i * 104729), (uint)(s.Rng.State >> 32)) % 100 < (uint)(held ? w.HeldLossPctPerHour : w.OutpostLossPctPerHour))
                {
                    site.Outpost = false;
                    site.Cleared = false;
                    s.OutpostsLostThisCycle++;
                    ctx.Emit(EventKind.OutpostLost, i, (int)owner);
                }
            }
        }

        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            for (int i = 0; i < s.Ops.Count; i++)
            {
                if (s.Tick >= s.Ops[i].ReturnTick)
                {
                    Operation op = s.Ops[i];
                    s.Ops.RemoveAt(i);
                    i--;
                    Return(ctx, op);
                }
            }
        }

        public static CommandResult Launch(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            int site = cmd.A;
            var kind = (OpKind)cmd.B;
            if (site < 0 || site >= CatalogArray.Length || cmd.B < 0 || cmd.B > (int)OpKind.Sabotage || cmd.C < 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            SiteDef d = CatalogArray[site];
            bool wild = HazardSystem.Wild(d.Kind);
            if (wild && (kind == OpKind.Hack || kind == OpKind.Sabotage))
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            int squad = kind == OpKind.Hack ? 0 : cmd.C;
            int compute = kind == OpKind.Hack ? cmd.C : 0;
            if ((kind != OpKind.Hack && squad < 1) || (kind == OpKind.Hack && (compute < 1 || d.Cyber == 0)) || (kind == OpKind.Sabotage && squad > c.World.SabotageMaxSquad))
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Ops.Count >= MaxOps(s, c))
            {
                return CommandResult.Reject(RejectReason.OpsBusy);
            }

            bool inFlight = false;
            foreach (Operation other in s.Ops)
            {
                inFlight |= other.Site == site;
            }

            if (inFlight || s.Sites[site].Outpost || (kind != OpKind.Scout && s.Tick < s.Sites[site].CooldownUntilTick))
            {
                return CommandResult.Reject(RejectReason.SiteCooldown);
            }

            if (squad > 0 && s.People - s.Garrison - squad < c.PeopleChoices.MinPeople)
            {
                return CommandResult.Reject(RejectReason.NotEnoughPeople);
            }

            int fuel = FuelCost(s, c, site, kind);
            if (s.Fuel < fuel)
            {
                return CommandResult.Reject(RejectReason.NotEnoughFuel);
            }

            if (s.Compute < compute)
            {
                return CommandResult.Reject(RejectReason.NotEnoughCompute);
            }

            s.Fuel -= fuel;
            s.Compute -= compute;
            s.People -= squad;
            int hours = kind == OpKind.Hack ? 1 : 2 * d.TravelHours;
            var op = new Operation { Id = s.NextOpId++, Site = site, Kind = kind, Squad = squad, Compute = compute, ReturnTick = s.Tick + ((long)hours * SimConfig.TicksPerHour) };
            s.Ops.Add(op);

            // striking a faction under ceasefire breaks it (scouting does not); wild zones belong to nobody
            if (kind != OpKind.Scout && !wild)
            {
                DiplomacySystem.Struck(ctx, d.Owner);
            }

            // doc 10 s4: launching offense ends the mercy window
            s.MercyUntilTick = System.Math.Min(s.MercyUntilTick, s.Tick);
            ctx.Emit(EventKind.OpLaunched, op.Id, site, (int)kind, kind == OpKind.Hack ? compute : squad);
            return CommandResult.Ok;
        }

        public static CommandResult Claim(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            int site = cmd.A;
            if (site < 0 || site >= CatalogArray.Length || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            SiteState st = s.Sites[site];
            SiteDef d = CatalogArray[site];
            bool seize = d.Kind == SiteKind.Outpost;
            if ((d.Kind != SiteKind.Ruins && !seize) || !st.Cleared || st.Outpost)
            {
                return CommandResult.Reject(RejectReason.NotClaimable);
            }

            int cost = seize ? ctx.Config.World.SeizeEnergy : ctx.Config.World.OutpostClaimEnergy;
            if (s.Energy < cost)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            s.Energy -= cost;
            st.Outpost = true;
            if (seize)
            {
                // taking their outpost is an act of war (doc 04 s8)
                AddHeat(ctx, d.Owner, ctx.Config.World.SeizeHeat);
                DiplomacySystem.Struck(ctx, d.Owner);
            }

            ctx.Emit(EventKind.OutpostClaimed, site);
            return CommandResult.Ok;
        }

        private static void Return(SimContext ctx, Operation op)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            WorldConfig w = c.World;
            SiteDef d = CatalogArray[op.Site];
            SiteState st = s.Sites[op.Site];
            bool wild = HazardSystem.Wild(d.Kind);
            int cooldownHours = wild ? c.Hazards.ZoneCooldownHours : w.RaidCooldownHours;
            int odds = Odds(s, c, d, op.Kind, op.Squad, op.Compute, TrueDefense(s, c, op.Site));
            bool won = SimMath.Hash((uint)op.Id * 2654435761u, (uint)(s.Rng.State >> 32)) % 100 < (uint)odds;
            int casualties = 0;
            int heat;
            switch (op.Kind)
            {
                case OpKind.Scout:
                    heat = w.HeatPerScout;
                    if (won)
                    {
                        st.Scouted = true;
                        if (!wild)
                        {
                            IntelSystem.CrossCheck(ctx, d.Owner);
                        }
                    }
                    else
                    {
                        casualties = System.Math.Min(1, op.Squad);
                    }

                    break;
                case OpKind.Sabotage:
                    heat = w.HeatPerRaid;
                    if (won)
                    {
                        // cripple the owner's next strikes; traced or not decides how much they hate us for it
                        st.CooldownUntilTick = s.Tick + ((long)w.RaidCooldownHours * SimConfig.TicksPerHour);
                        bool traced = SimMath.Hash((uint)op.Id ^ 0x5AB0u, (uint)(s.Rng.State >> 32)) % 100 < (uint)w.SabotageTracePct;
                        heat = traced ? w.HeatPerRaid : w.HeatPerScout;
                        s.SabotageFaction = (int)d.Owner;
                        s.SabotageUntilTick = s.Tick + ((long)w.SabotageHours * SimConfig.TicksPerHour);
                        ctx.Emit(EventKind.SabotageStruck, (int)d.Owner, w.SabotageHours, traced ? 1 : 0);
                    }
                    else
                    {
                        casualties = System.Math.Min(1, op.Squad);
                    }

                    break;
                case OpKind.Hack:
                    heat = Modules.Has(s, ModuleNode.CY5B) ? 0 : w.HeatPerHack;
                    if (won)
                    {
                        st.CooldownUntilTick = s.Tick + ((long)w.RaidCooldownHours * SimConfig.TicksPerHour);
                        Loot(ctx, op.Id, LossResource.Compute, Modules.Has(s, ModuleNode.CY5A) ? SimMath.PctFloor(d.Compute, 100 + c.Modules.WormPct) : d.Compute);
                        IntelSystem.CrossCheck(ctx, d.Owner);
                        if (d.CleanData > 0)
                        {
                            CorruptionSystem.Add(ctx, -d.CleanData);
                        }
                    }

                    break;
                default:
                    heat = w.HeatPerRaid;
                    casualties = (op.Squad * (won ? w.CasualtyPctWin : w.CasualtyPctLoss)) / 100;
                    if (Modules.Has(s, ModuleNode.WF5A))
                    {
                        casualties = SimMath.PctFloor(casualties, c.Modules.AssaultCasualtyPct);
                    }
                    if (won)
                    {
                        st.CooldownUntilTick = s.Tick + ((long)cooldownHours * SimConfig.TicksPerHour);
                        // ruins can be claimed; a beaten faction outpost can be seized and held (doc 04 s8)
                        st.Cleared = d.Kind == SiteKind.Ruins || d.Kind == SiteKind.Outpost;
                        Loot(ctx, op.Id, LossResource.Energy, d.Energy);
                        Loot(ctx, op.Id, LossResource.Fuel, d.Fuel);
                        Loot(ctx, op.Id, LossResource.Compute, d.Compute);
                        if (!wild)
                        {
                            AdaptSystem.Fortify(ctx, d.Owner);
                        }
                    }

                    break;
            }

            // survivors always come home (a lowered cap never kills anyone)
            s.People += op.Squad - casualties;
            casualties += HazardSystem.Returned(ctx, op, won, op.Squad - casualties);
            ctx.Emit(EventKind.OpReturned, op.Id, op.Site, won ? 1 : 0, casualties);
            if (wild)
            {
                return;
            }

            AddHeat(ctx, d.Owner, Modules.Has(s, ModuleNode.ST5B) ? SimMath.PctFloor(heat, 100 - c.Modules.FalseTrailsPct) : heat);
        }

        private static void Loot(SimContext ctx, int opId, LossResource resource, int amount)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (LivingSystem.Active(s, WorldEventKind.SupplyWindow))
            {
                amount = SimMath.PctFloor(amount, 100 + c.Living.SupplyWindowLootPct);
            }

            int got;
            switch (resource)
            {
                case LossResource.Energy:
                    got = System.Math.Max(0, System.Math.Min(amount, Economy.EnergyCap(s, c) - s.Energy));
                    s.Energy += got;
                    break;
                case LossResource.Fuel:
                    got = System.Math.Max(0, System.Math.Min(amount, c.Fuel.Cap - s.Fuel));
                    s.Fuel += got;
                    break;
                default:
                    got = System.Math.Max(0, System.Math.Min(amount, c.Compute.Cap - s.Compute));
                    s.Compute += got;
                    break;
            }

            if (got > 0)
            {
                ctx.Emit(EventKind.OpLoot, opId, (int)resource, got);
            }
        }

        public static void AddHeat(SimContext ctx, Faction f, int milli)
        {
            GameState s = ctx.State;
            int i = (int)f;
            HeatLevel before = Level(s.Heat[i]);
            s.Heat[i] = SimMath.Clamp(s.Heat[i] + milli, 0, 100_000);
            HeatLevel after = Level(s.Heat[i]);
            if (after != before)
            {
                ctx.Emit(EventKind.HeatLevelChanged, i, (int)after, (int)before);
            }
        }
    }
}
