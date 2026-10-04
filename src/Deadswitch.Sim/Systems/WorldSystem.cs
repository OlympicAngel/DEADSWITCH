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
        public const int FactionCount = 3;

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
        };

        public static IReadOnlyList<SiteDef> Sites => CatalogArray;

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

        /// <summary>The AI's read of a site's defense: exact once scouted, otherwise off by a stable error (may be wrong).</summary>
        public static int EstimatedDefense(GameState s, SimConfig c, int site)
        {
            SiteDef d = CatalogArray[site];
            if (s.Sites[site].Scouted)
            {
                return d.Defense;
            }

            int span = (2 * c.World.EstimateErrorPct) + 1;
            int err = (int)(SimMath.Hash((uint)site * 7919u, 0x5173u) % (uint)span) - c.World.EstimateErrorPct;
            return System.Math.Max(1, SimMath.PctFloor(d.Defense, 100 + err));
        }

        /// <summary>Success chance in percent against a given defense (also used by the UI with the AI's estimate).</summary>
        public static int Odds(SimConfig c, SiteDef d, OpKind kind, int squad, int compute, int defense)
        {
            switch (kind)
            {
                case OpKind.Scout:
                    return SimMath.Clamp(55 + (squad * 12) - (defense / 4), 10, 95);
                case OpKind.Hack:
                    return SimMath.Clamp(50 + ((compute - d.Cyber) * 50 / System.Math.Max(1, d.Cyber)), 5, 95);
                default:
                    int power = squad * c.World.StrengthPerPerson;
                    return SimMath.Clamp(50 + ((power - defense) * 50 / System.Math.Max(1, defense)), 5, 95);
            }
        }

        public static int FuelCost(SimConfig c, SiteDef d, OpKind kind)
        {
            return kind == OpKind.Hack ? 0 : 2 * d.TravelHours * c.World.FuelPerTravelHour;
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
            };
            int total = weights[0] + weights[1] + weights[2];
            if (total <= 0)
            {
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
                AddHeat(ctx, (Faction)f, -w.HeatDecayPerHour);
            }

            for (int i = 0; i < s.Sites.Count; i++)
            {
                SiteState site = s.Sites[i];
                if (!site.Outpost)
                {
                    continue;
                }

                s.Energy = System.Math.Min(Economy.EnergyCap(s, ctx.Config), s.Energy + w.OutpostEnergyPerHour);
                s.Fuel = System.Math.Min(ctx.Config.Fuel.Cap, s.Fuel + w.OutpostFuelPerHour);

                // a hunting faction takes its ground back
                Faction owner = CatalogArray[i].Owner;
                if (Level(s.Heat[(int)owner]) >= HeatLevel.Hunted
                    && SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerHour) ^ (uint)(i * 104729), (uint)(s.Rng.State >> 32)) % 100 < (uint)w.OutpostLossPctPerHour)
                {
                    site.Outpost = false;
                    site.Cleared = false;
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
            if (site < 0 || site >= CatalogArray.Length || cmd.B < 0 || cmd.B > (int)OpKind.Hack || cmd.C < 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            SiteDef d = CatalogArray[site];
            int squad = kind == OpKind.Hack ? 0 : cmd.C;
            int compute = kind == OpKind.Hack ? cmd.C : 0;
            if ((kind != OpKind.Hack && squad < 1) || (kind == OpKind.Hack && (compute < 1 || d.Cyber == 0)))
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Ops.Count >= c.World.MaxOps)
            {
                return CommandResult.Reject(RejectReason.OpsBusy);
            }

            if (s.Sites[site].Outpost || (kind != OpKind.Scout && s.Tick < s.Sites[site].CooldownUntilTick))
            {
                return CommandResult.Reject(RejectReason.SiteCooldown);
            }

            if (squad > 0 && s.People - s.Garrison - squad < c.PeopleChoices.MinPeople)
            {
                return CommandResult.Reject(RejectReason.NotEnoughPeople);
            }

            int fuel = FuelCost(c, d, kind);
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
            if (CatalogArray[site].Kind != SiteKind.Ruins || !st.Cleared || st.Outpost)
            {
                return CommandResult.Reject(RejectReason.NotClaimable);
            }

            if (s.Energy < ctx.Config.World.OutpostClaimEnergy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            s.Energy -= ctx.Config.World.OutpostClaimEnergy;
            st.Outpost = true;
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
            int odds = Odds(c, d, op.Kind, op.Squad, op.Compute, d.Defense);
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
                    }
                    else
                    {
                        casualties = System.Math.Min(1, op.Squad);
                    }

                    break;
                case OpKind.Hack:
                    heat = w.HeatPerHack;
                    if (won)
                    {
                        Loot(ctx, op.Id, LossResource.Compute, d.Compute);
                        if (d.CleanData > 0)
                        {
                            CorruptionSystem.Add(ctx, -d.CleanData);
                        }
                    }

                    break;
                default:
                    heat = w.HeatPerRaid;
                    casualties = (op.Squad * (won ? w.CasualtyPctWin : w.CasualtyPctLoss)) / 100;
                    if (won)
                    {
                        st.CooldownUntilTick = s.Tick + ((long)w.RaidCooldownHours * SimConfig.TicksPerHour);
                        st.Cleared = d.Kind == SiteKind.Ruins;
                        Loot(ctx, op.Id, LossResource.Energy, d.Energy);
                        Loot(ctx, op.Id, LossResource.Fuel, d.Fuel);
                        Loot(ctx, op.Id, LossResource.Compute, d.Compute);
                    }

                    break;
            }

            s.People = System.Math.Min(Economy.PopulationCap(s, c), s.People + op.Squad - casualties);
            ctx.Emit(EventKind.OpReturned, op.Id, op.Site, won ? 1 : 0, casualties);
            AddHeat(ctx, d.Owner, heat);
        }

        private static void Loot(SimContext ctx, int opId, LossResource resource, int amount)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            int got;
            switch (resource)
            {
                case LossResource.Energy:
                    got = System.Math.Min(amount, Economy.EnergyCap(s, c) - s.Energy);
                    s.Energy += got;
                    break;
                case LossResource.Fuel:
                    got = System.Math.Min(amount, c.Fuel.Cap - s.Fuel);
                    s.Fuel += got;
                    break;
                default:
                    got = System.Math.Min(amount, c.Compute.Cap - s.Compute);
                    s.Compute += got;
                    break;
            }

            if (got > 0)
            {
                ctx.Emit(EventKind.OpLoot, opId, (int)resource, got);
            }
        }

        private static void AddHeat(SimContext ctx, Faction f, int milli)
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
