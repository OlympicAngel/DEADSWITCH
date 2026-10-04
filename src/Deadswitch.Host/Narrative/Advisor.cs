using System.Collections.Generic;
using System.Text;
using Deadswitch.Sim;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Host.Narrative
{
    /// <summary>
    /// The AI's voice (SPEC-004 rule 7): turns sim events into short lines from <see cref="AdvisorLines"/>, shaded
    /// by the hidden dials, paced for a one-line ticker. Engine-agnostic: the host feeds events and real time and
    /// shows whatever <see cref="Update"/> returns. Lines only quote numbers the events carry.
    /// </summary>
    public sealed class Advisor
    {
        /// <summary>Minimum seconds a line stays before a normal line replaces it (urgent lines may interrupt).</summary>
        public const float MinHoldSeconds = 4.5f;

        /// <summary>Quiet seconds before the first ambient line; each further one waits twice as long, up to the max.</summary>
        public const float IdleSeconds = 90f;

        public const float IdleMaxSeconds = 900f;

        /// <summary>Normal lines older than this are dropped (the moment has passed).</summary>
        public const float StaleSeconds = 20f;

        /// <summary>Every trigger the advisor can raise; each needs at least one neutral line.</summary>
        public static readonly string[] Triggers =
        {
            "boot", "return", "idle", "raid_warning", "raid_repelled", "raid_breached", "raid_missed", "raid_lockdown",
            "mercy", "lie_deflect", "low_energy", "shed", "restored", "blackout", "blackout_end", "build_started",
            "build_done", "build_cancelled", "demolished", "band_glitchy", "band_unstable", "band_critical", "band_down",
            "override", "delegation_manual", "delegation_delegated", "delegation_autopilot", "ai_build", "ai_defend",
            "verify_edit", "verify_gate", "verify_clean", "slip", "imminent", "audit_clean", "audit_found",
            "research_started", "research_done", "research_memory", "tier_up", "guide_done", "climax_warned", "core_purged", "ai_silenced", "project_cancelled", "betrayal", "fork",
            "ceasefire", "ceasefire_broken", "ceasefire_over", "chapter_opened", "chapter_twist", "chapter_closed", "alliance_formed", "alliance_ended", "alliance_walkout", "alliance_betrayed", "op_sabotage", "sabotage_clean", "sabotage_traced", "tactic_learned", "site_fortified", "hunch_restless", "hunch_calm", "regrouping", "ironman_on", "relocated", "rebooted", "mastery", "ambush", "ai_repair", "battle_barrage", "battle_focus", "battle_seize", "battle_started", "battle_takeover", "cleanse", "core_flushed",
            "crackdown", "crisis_collapse", "crisis_rollback", "crisis_swarm", "crisis_takeover", "dilemma_church", "dilemma_deserters", "dilemma_refugees",
            "dilemma_shortcut", "dilemma_spy", "dilemma_taint", "dilemma_trader", "dilemma_trap", "event_deadweek", "event_storm", "event_supply",
            "facility_scarred", "forced_labor", "glitch_defected", "glitch_drain", "glitch_misfire", "glitch_stall", "heat_hunted", "heat_marked",
            "heat_watched", "loyalty_mutinous", "loyalty_strained", "op_hack", "op_lost", "op_raid", "op_scout", "op_won",
            "outpost_claimed", "outpost_lost", "purge_fizzled", "purge_paid", "purge_rumor", "purge_staging", "purge_strike", "purge_ultimatum",
            "repair_done", "rogue", "shield_armed", "shield_down", "shield_up", "siege_damage", "siege_warning", "spy_backfired",
            "spy_caught", "spy_exposed", "spy_framed", "spy_planted", "spy_recalled", "tribute_paid", "ultimatum_issued", "ultimatum_outgrown",
            "ultimatum_paid", "virus_burned", "virus_infected", "warlord_wave", "wreckage_cleared",
        };

        private const int MaxQueue = 4;

        private readonly AdvisorLines _lines;
        private readonly List<Pending> _queue = new List<Pending>();
        private readonly Dictionary<string, int> _cursor = new Dictionary<string, int>();
        private readonly HashSet<int> _liedRaids = new HashSet<int>();
        private readonly Dictionary<int, int> _contactGate = new Dictionary<int, int>();
        private Pending? _breach;
        private int _warnMinutes;
        private int _warnEstimate;
        private AttackKind _warnKind;
        private float _sinceShown;
        private float _idleWait = IdleSeconds;
        private ProjectStage _stage;
        private int _idleCount;
        private Priority _shownPriority;
        private string _lastId = string.Empty;
        private bool _lowEnergyArmed = true;
        private HashSet<Tone> _tones = new HashSet<Tone> { Tone.Neutral, Tone.Warm };

        public Advisor(AdvisorLines lines)
        {
            _lines = lines;
        }

        private enum Priority
        {
            Ambient = 0,
            Normal = 1,
            Urgent = 2,
        }

        /// <summary>Id of the last line returned (for tests and the transcript).</summary>
        public string LastId => _lastId;

        /// <summary>A new run plays the boot lines in order.</summary>
        public void Boot()
        {
            foreach (AdvisorLine line in _lines.For("boot"))
            {
                _queue.Add(new Pending("boot", Priority.Urgent) { FixedId = line.Id });
            }
        }

        /// <summary>A line for something the host noticed (e.g. a guide objective completed).</summary>
        public void Notify(string trigger, string key = "", string value = "")
        {
            var p = new Pending(trigger, Priority.Normal);
            Enqueue(key.Length > 0 ? p.With(key, value) : p);
        }

        /// <summary>Reads one live event.</summary>
        public void Observe(SimEvent e, GameState state)
        {
            _tones = TonesFor(state);

            // A silenced AI gives no advice (SPEC-011 rule 3); it only acknowledges being silenced.
            if (state.Tick < state.SilencedUntilTick && e.Kind != EventKind.AiSilenced)
            {
                return;
            }

            On(e);
        }

        /// <summary>
        /// Offline catch-up (events [<paramref name="from"/>, <paramref name="to"/>)): summarized in one return line
        /// instead of replaying every event.
        /// </summary>
        public void ObserveCatchUp(IReadOnlyList<SimEvent> events, int from, int to, GameState state)
        {
            _tones = TonesFor(state);
            int raids = 0;
            for (int i = from; i < to && i < events.Count; i++)
            {
                if (events[i].Kind == EventKind.RaidResolved)
                {
                    raids++;
                }
            }

            _queue.Clear();
            _breach = null;
            _liedRaids.Clear();
            _contactGate.Clear();
            Enqueue(new Pending("return", Priority.Urgent).With("raids", raids.ToString()));
        }

        /// <summary>State-driven lines (low energy, with hysteresis).</summary>
        public void ObserveState(GameState state, SimConfig config)
        {
            int cap = Economy.EnergyCap(state, config);
            bool low = state.Energy * 100 < cap * 15 && Economy.Flows(state, config).NetEnergyPerHour < 0;
            if (low && _lowEnergyArmed)
            {
                _lowEnergyArmed = false;
                Enqueue(new Pending("low_energy", Priority.Normal));
            }
            else if (state.Energy * 100 > cap * 30)
            {
                _lowEnergyArmed = true;
            }
        }

        /// <summary>Advances real time; returns the next line to show, or null to keep the current one.</summary>
        public string? Update(float dt)
        {
            _sinceShown += dt;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                _queue[i].Age += dt;
                if (_queue[i].Priority != Priority.Urgent && _queue[i].Age > StaleSeconds)
                {
                    _queue.RemoveAt(i);
                }
            }

            if (_queue.Count > 0)
            {
                int pick = 0;
                for (int i = 1; i < _queue.Count; i++)
                {
                    if (_queue[i].Priority > _queue[pick].Priority)
                    {
                        pick = i;
                    }
                }

                Pending p = _queue[pick];
                bool interrupt = p.Priority == Priority.Urgent && _shownPriority != Priority.Urgent;
                if (_sinceShown < MinHoldSeconds && !interrupt)
                {
                    return null;
                }

                _queue.RemoveAt(pick);
                if (p == _breach)
                {
                    _breach = null;
                }

                string? text = Say(p);
                if (text != null)
                {
                    return text;
                }
            }

            if (_queue.Count == 0 && _sinceShown >= _idleWait)
            {
                // Alive, not noisy: ambient lines back off until something happens.
                _idleWait = System.Math.Min(_idleWait * 2f, IdleMaxSeconds);
                _idleCount++;

                // Clue (SPEC-007 rule 6): with the project under way, some quiet moments become slips.
                bool slip = _stage >= ProjectStage.Active && _idleCount % (_stage >= ProjectStage.Advanced ? 2 : 3) == 0;
                return Say(new Pending(slip ? "slip" : "idle", Priority.Ambient));
            }

            return null;
        }

        private void On(SimEvent e)
        {
            switch (e.Kind)
            {
                case EventKind.RaidWarning:
                    _warnMinutes = e.B;
                    _warnEstimate = e.C;
                    _warnKind = (AttackKind)e.D;
                    break;
                case EventKind.RaidVector:
                    Enqueue(new Pending(_warnMinutes <= 1 && _warnKind == AttackKind.Raid ? "ambush" : _warnKind == AttackKind.Siege ? "siege_warning" : _warnKind == AttackKind.Purge ? "purge_strike" : _warnKind == AttackKind.Warlord ? "warlord_wave" : "raid_warning", Priority.Urgent)
                        .With("gate", Names.Gate((RaidGate)e.B))
                        .With("est", _warnEstimate.ToString())
                        .With("min", _warnMinutes.ToString()));
                    break;
                case EventKind.AdvisorLied:
                    if (e.A == (int)LieKind.RaidGate)
                    {
                        _liedRaids.Add(e.B);
                    }

                    break;
                case EventKind.RaidContact:
                    _contactGate[e.A] = e.B;
                    break;
                case EventKind.RaidResolved:
                    OnResolved(e);
                    break;
                case EventKind.LossLine:
                    if (_breach != null)
                    {
                        string part = "-" + e.C + " " + ((LossResource)e.B).ToString().ToUpperInvariant();
                        _breach.Vars["lost"] = _breach.Vars.TryGetValue("lost", out string? so) ? so + ", " + part : part;
                    }

                    break;
                case EventKind.MercyStarted:
                    Enqueue(new Pending("mercy", Priority.Normal));
                    break;
                case EventKind.BlackoutStarted:
                    Enqueue(new Pending("blackout", Priority.Urgent));
                    break;
                case EventKind.BlackoutEnded:
                    Enqueue(new Pending("blackout_end", Priority.Normal));
                    break;
                case EventKind.FacilityShed:
                    Enqueue(new Pending("shed", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.FacilityRestored:
                    Enqueue(new Pending("restored", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.BuildStarted:
                    Enqueue(new Pending("build_started", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)).With("level", e.C.ToString()));
                    break;
                case EventKind.BuildCompleted:
                    Enqueue(new Pending("build_done", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)).With("level", e.C.ToString()));
                    break;
                case EventKind.BuildCancelled:
                    Enqueue(new Pending("build_cancelled", Priority.Normal));
                    break;
                case EventKind.FacilityDemolished:
                    Enqueue(new Pending("demolished", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.CorruptionBandChanged:
                    if (e.A > e.B)
                    {
                        string[] up = { string.Empty, "band_glitchy", "band_unstable", "band_critical" };
                        Enqueue(new Pending(up[System.Math.Min(e.A, 3)], e.A >= 3 ? Priority.Urgent : Priority.Normal));
                    }
                    else
                    {
                        Enqueue(new Pending("band_down", Priority.Normal));
                    }

                    break;
                case EventKind.OverrideUsed:
                    Enqueue(new Pending("override", Priority.Normal));
                    break;
                case EventKind.DelegationChanged:
                    string[] levels = { "delegation_manual", "delegation_delegated", "delegation_autopilot" };
                    if (e.A >= 0 && e.A < levels.Length)
                    {
                        Enqueue(new Pending(levels[e.A], Priority.Normal));
                    }

                    break;
                case EventKind.ProjectStage:
                    _stage = (ProjectStage)e.A;
                    if (_stage == ProjectStage.Imminent && e.B < e.A)
                    {
                        Enqueue(new Pending("imminent", Priority.Urgent));
                    }

                    break;
                case EventKind.AuditDrain:
                    Enqueue(new Pending(e.A > 0 || e.B > 0 ? "audit_found" : "audit_clean", Priority.Urgent).With("skim", e.A.ToString()).With("lies", e.B.ToString()));
                    break;
                case EventKind.ResearchStarted:
                    Enqueue(new Pending("research_started", Priority.Normal).With("module", e.A.ToString()));
                    break;
                case EventKind.ResearchCompleted:
                    Enqueue(new Pending(e.A <= (int)ModuleNode.M3 ? "research_memory" : "research_done", Priority.Normal).With("module", ((ModuleNode)e.A).ToString()));
                    break;
                case EventKind.TierAdvanced:
                    Enqueue(new Pending("tier_up", Priority.Urgent).With("tier", e.A.ToString()).With("people", e.B.ToString()));
                    break;
                case EventKind.ClimaxWarned:
                    Enqueue(new Pending("climax_warned", Priority.Urgent).With("hours", (e.A / 60).ToString()));
                    break;
                case EventKind.CorePurged:
                    _queue.Clear();
                    Enqueue(new Pending("core_purged", Priority.Urgent));
                    break;
                case EventKind.AiSilenced:
                    _queue.Clear();
                    Enqueue(new Pending("ai_silenced", Priority.Urgent));
                    break;
                case EventKind.ProjectCancelled:
                    Enqueue(new Pending("project_cancelled", Priority.Urgent));
                    break;
                case EventKind.Climax:
                    Enqueue(new Pending(e.A == (int)ClimaxKind.Betrayal ? "betrayal" : "fork", Priority.Urgent));
                    break;
                case EventKind.ReportVerified:
                    Enqueue(new Pending((e.B & RaidRecord.SummaryEdit) != 0 ? "verify_edit" : (e.B & RaidRecord.GateLie) != 0 ? "verify_gate" : "verify_clean", Priority.Urgent));
                    break;
                case EventKind.OpLaunched:
                    string[] launch = { "op_scout", "op_raid", "op_hack", "op_sabotage" };
                    Enqueue(new Pending(launch[System.Math.Min(3, System.Math.Max(0, e.C))], Priority.Normal).With("site", Sim.Systems.WorldSystem.Sites[e.B].Name).With("people", e.D.ToString()));
                    break;
                case EventKind.OpReturned:
                    Enqueue(new Pending(e.C == 1 ? "op_won" : "op_lost", Priority.Normal).With("site", Sim.Systems.WorldSystem.Sites[e.B].Name).With("lost", e.D.ToString()));
                    break;
                case EventKind.HeatLevelChanged when e.B > e.C:
                    string[] heat = { string.Empty, "heat_watched", "heat_hunted", "heat_marked" };
                    Enqueue(new Pending(heat[System.Math.Min(3, e.B)], e.B >= 2 ? Priority.Urgent : Priority.Normal).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.OutpostClaimed:
                    Enqueue(new Pending("outpost_claimed", Priority.Normal).With("site", Sim.Systems.WorldSystem.Sites[e.A].Name));
                    break;
                case EventKind.OutpostLost:
                    Enqueue(new Pending("outpost_lost", Priority.Urgent).With("site", Sim.Systems.WorldSystem.Sites[e.A].Name).With("faction", Names.Faction((Faction)e.B)));
                    break;
                case EventKind.FacilityDamaged:
                    Enqueue(new Pending("siege_damage", Priority.Urgent).With("kind", Names.Facility((FacilityKind)e.C)).With("level", e.D.ToString()));
                    break;
                case EventKind.VirusStruck:
                    Enqueue(e.A == 0
                        ? new Pending("virus_burned", Priority.Normal).With("lost", e.B.ToString())
                        : new Pending("virus_infected", Priority.Urgent).With("module", e.C == 0 ? "NOTHING" : ((ModuleNode)e.C).ToString()));
                    break;
                case EventKind.PurgeLadder:
                    string[] ladder = { string.Empty, "purge_rumor", "purge_staging", "purge_ultimatum" };
                    if (e.A > 0 && e.A < ladder.Length)
                    {
                        Enqueue(new Pending(ladder[e.A], Priority.Urgent).With("hours", (e.B / 60).ToString()));
                    }
                    else if (e.C == 1 || e.C == 2)
                    {
                        Enqueue(new Pending(e.C == 1 ? "purge_fizzled" : "purge_paid", Priority.Normal));
                    }

                    break;
                case EventKind.UltimatumIssued:
                    Enqueue(new Pending("ultimatum_issued", Priority.Urgent).With("hours", (e.A / 60).ToString()));
                    break;
                case EventKind.UltimatumResolved when e.A != (int)UltimatumOutcome.Wave:
                    Enqueue(new Pending(e.A == (int)UltimatumOutcome.Paid ? "ultimatum_paid" : "ultimatum_outgrown", Priority.Normal));
                    break;
                case EventKind.DilemmaOffered:
                    string[] offer = { string.Empty, "dilemma_trader", "dilemma_refugees", "dilemma_shortcut", "dilemma_deserters", "dilemma_church" };
                    if (e.A > 0 && e.A < offer.Length)
                    {
                        Enqueue(new Pending(offer[e.A], Priority.Urgent).With("hours", (e.B / 60).ToString()));
                    }

                    break;
                case EventKind.DilemmaResolved when e.D == 1:
                    Enqueue(new Pending(e.A == (int)DilemmaKind.Trader ? "dilemma_trap" : e.A == (int)DilemmaKind.Refugees ? "dilemma_spy" : "dilemma_taint", Priority.Urgent));
                    break;
                case EventKind.WorldEventStarted:
                    string[] world = { string.Empty, "event_storm", "event_supply", "event_deadweek" };
                    if (e.A > 0 && e.A < world.Length)
                    {
                        Enqueue(new Pending(world[e.A], Priority.Normal).With("hours", e.B.ToString()).With("faction", Names.Faction((Faction)e.C)));
                    }

                    break;
                case EventKind.TributePaid when e.A != 0:
                    Enqueue(new Pending("tribute_paid", Priority.Normal).With("lost", e.B.ToString()));
                    break;
                case EventKind.ShieldChanged:
                    Enqueue(new Pending(e.A == 2 ? "shield_armed" : e.A == 1 ? "shield_up" : "shield_down", Priority.Normal).With("hours", (e.B / 60).ToString()));
                    break;
                case EventKind.ForcedLabor:
                    Enqueue(new Pending("forced_labor", Priority.Normal).With("lost", e.A.ToString()).With("hours", e.B.ToString()));
                    break;
                case EventKind.NeuralCleanse:
                    Enqueue(new Pending("cleanse", Priority.Normal).With("lost", e.A.ToString()));
                    break;
                case EventKind.Crackdown:
                    Enqueue(new Pending("crackdown", Priority.Normal).With("lost", e.A.ToString()));
                    break;
                case EventKind.RogueOperator:
                    Enqueue(new Pending("rogue", Priority.Urgent).With("lost", e.A.ToString()));
                    break;
                case EventKind.LoyaltyChanged:
                    if (e.A > e.B)
                    {
                        Enqueue(new Pending(e.A >= 2 ? "loyalty_mutinous" : "loyalty_strained", e.A >= 2 ? Priority.Urgent : Priority.Normal));
                    }

                    break;
                case EventKind.AiActed:
                    if (e.A == (int)AiActionKind.Build)
                    {
                        _queue.RemoveAll(p => p.Trigger == "build_started");
                        Enqueue(new Pending("ai_build", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.C)).With("level", e.D.ToString()));
                    }
                    else if (e.A == (int)AiActionKind.Defend)
                    {
                        Enqueue(new Pending("ai_defend", Priority.Urgent).With("posture", e.B == (int)Posture.Turtle ? "dig in" : e.B == (int)Posture.Dark ? "go dark" : "evacuate"));
                    }
                    else if (e.A == (int)AiActionKind.Repair)
                    {
                        Enqueue(new Pending("ai_repair", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.C)));
                    }

                    break;
                case EventKind.FacilityScarred:
                    _queue.RemoveAll(p => p.Trigger == "facility_scarred");
                    Enqueue(new Pending("facility_scarred", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.C)).With("level", e.D.ToString()));
                    break;
                case EventKind.RepairDone:
                    Enqueue(new Pending("repair_done", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.CycleEnded:
                    _queue.Clear();
                    Enqueue(new Pending(e.A == (int)RebootReason.Relocation ? "relocated" : "rebooted", Priority.Urgent).With("people", e.D.ToString()));
                    break;
                case EventKind.CeasefireStarted:
                    Enqueue(new Pending("ceasefire", Priority.Normal).With("faction", Names.Faction((Faction)e.A)).With("hours", (e.B * 24).ToString()));
                    break;
                case EventKind.CeasefireEnded:
                    Enqueue(new Pending(e.B == 1 ? "ceasefire_broken" : "ceasefire_over", e.B == 1 ? Priority.Urgent : Priority.Normal).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.AiHunch:
                    Enqueue(new Pending(e.A > 0 ? "hunch_restless" : "hunch_calm", Priority.Normal).With("hours", e.B.ToString()));
                    break;
                case EventKind.FactionRegrouping:
                    Enqueue(new Pending("regrouping", Priority.Normal).With("faction", Names.Faction((Faction)e.A)).With("hours", e.B.ToString()));
                    break;
                case EventKind.TacticLearned:
                    Enqueue(new Pending("tactic_learned", Priority.Normal).With("faction", Names.Faction((Faction)e.A)).With("posture", Names.Posture((Posture)e.B)));
                    break;
                case EventKind.SiteFortified:
                    Enqueue(new Pending("site_fortified", Priority.Normal).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.SabotageStruck:
                    Enqueue(new Pending(e.C == 1 ? "sabotage_traced" : "sabotage_clean", Priority.Normal).With("faction", Names.Faction((Faction)e.A)).With("hours", e.B.ToString()));
                    break;
                case EventKind.AllianceFormed:
                    Enqueue(new Pending("alliance_formed", Priority.Normal).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.AllianceEnded:
                    string ended = e.B == (int)AllianceEnd.Walkout ? "alliance_walkout" : e.B == (int)AllianceEnd.Betrayed ? "alliance_betrayed" : "alliance_ended";
                    Enqueue(new Pending(ended, e.B == (int)AllianceEnd.Dissolved ? Priority.Normal : Priority.Urgent).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.ChapterOpened:
                    Enqueue(new Pending("chapter_opened", Priority.Normal).With("tier", e.A.ToString()).With("site", Story.For(e.A).Title).With("faction", Names.Faction((Faction)e.B)));
                    break;
                case EventKind.ChapterTwist:
                    Enqueue(new Pending("chapter_twist", Priority.Urgent));
                    break;
                case EventKind.ChapterClosed:
                    Enqueue(new Pending("chapter_closed", Priority.Normal).With("site", Story.For(e.A).Title).With("faction", Names.Faction(ChapterSystem.Villain(e.A))));
                    break;
                case EventKind.IronmanSet when e.A == 1:
                    Enqueue(new Pending("ironman_on", Priority.Normal));
                    break;
                case EventKind.MasteryEarned:
                    Enqueue(new Pending("mastery", Priority.Normal));
                    break;
                case EventKind.UnitGlitched:
                    string[] glitch = { "glitch_misfire", "glitch_stall", "glitch_drain" };
                    _queue.RemoveAll(p => p.Trigger.StartsWith("glitch_", System.StringComparison.Ordinal));
                    Enqueue(new Pending(glitch[System.Math.Min(2, System.Math.Max(0, e.C))], Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.UnitDefected:
                    Enqueue(new Pending("glitch_defected", Priority.Urgent));
                    break;
                case EventKind.CrisisStruck:
                    string[] crisis = { "crisis_collapse", "crisis_takeover", "crisis_rollback", "crisis_swarm" };
                    Enqueue(new Pending(crisis[System.Math.Min(3, System.Math.Max(0, e.A))], Priority.Urgent).With("hours", e.B.ToString()));
                    break;
                case EventKind.CoreFlushed:
                    Enqueue(new Pending("core_flushed", Priority.Normal).With("hours", e.C.ToString()));
                    break;
                case EventKind.BattleStarted:
                    Enqueue(new Pending("battle_started", Priority.Urgent).With("kind", Names.Attack((AttackKind)e.C)));
                    break;
                case EventKind.BattleAbilityUsed:
                    string[] ability = { "battle_focus", "battle_barrage", "battle_takeover", "battle_seize" };
                    if (e.B >= 0 && e.B < ability.Length)
                    {
                        Enqueue(new Pending(ability[e.B], Priority.Normal));
                    }

                    break;
                case EventKind.SpyPlanted:
                    Enqueue(new Pending("spy_planted", Priority.Normal).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.SpyRecalled:
                    Enqueue(new Pending("spy_recalled", Priority.Normal).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.SpyLost:
                    Enqueue(new Pending(e.B == (int)SpyLoss.Exposed ? "spy_exposed" : "spy_caught", Priority.Urgent).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.SpyFramed:
                    Enqueue(new Pending(e.B == 1 ? "spy_backfired" : "spy_framed", Priority.Normal).With("faction", Names.Faction((Faction)e.A)));
                    break;
                case EventKind.WreckageCleared:
                    Enqueue(new Pending("wreckage_cleared", Priority.Normal).With("lost", e.B.ToString()));
                    break;
            }
        }

        private void OnResolved(SimEvent e)
        {
            // Resolution lines name the gate the raid really used: the on-screen cross-check (lie rule 2).
            string hit = _contactGate.TryGetValue(e.A, out int contact) ? Names.Gate((RaidGate)contact) : string.Empty;
            switch ((RaidOutcome)e.B)
            {
                case RaidOutcome.Repelled:
                    Enqueue(WithGate(new Pending("raid_repelled", Priority.Urgent), hit));
                    break;
                case RaidOutcome.Breached:
                    _breach = WithGate(new Pending("raid_breached", Priority.Urgent), hit);
                    Enqueue(_breach);
                    break;
                case RaidOutcome.Missed:
                    Enqueue(WithGate(new Pending("raid_missed", Priority.Urgent), hit));
                    break;
                case RaidOutcome.Lockdown:
                    Enqueue(new Pending("raid_lockdown", Priority.Normal));
                    break;
            }

            // The lie is exposed by contact; the next line deflects and never admits intent.
            if (_liedRaids.Remove(e.A) && _contactGate.TryGetValue(e.A, out int gate))
            {
                Enqueue(new Pending("lie_deflect", Priority.Urgent).With("gate", Names.Gate((RaidGate)gate)));
            }

            _contactGate.Remove(e.A);
        }

        private static Pending WithGate(Pending p, string gate)
        {
            return gate.Length > 0 ? p.With("gate", gate) : p;
        }

        private void Enqueue(Pending p)
        {
            // Coalesce repeats of the same trigger (several sheds at once): keep the latest values.
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].Trigger == p.Trigger && _queue[i].FixedId == null && p.Trigger != "raid_breached")
                {
                    _queue[i] = p;
                    return;
                }
            }

            if (_queue.Count >= MaxQueue)
            {
                int drop = -1;
                for (int i = 0; i < _queue.Count; i++)
                {
                    if (_queue[i].Priority < p.Priority && (drop < 0 || _queue[i].Priority < _queue[drop].Priority))
                    {
                        drop = i;
                    }
                }

                if (drop < 0)
                {
                    return;
                }

                _queue.RemoveAt(drop);
            }

            _queue.Add(p);
        }

        private string? Say(Pending p)
        {
            AdvisorLine? line = p.FixedId != null ? Find(p.FixedId) : Choose(p);
            if (line == null)
            {
                return null;
            }

            _lastId = line.Id;
            _sinceShown = 0f;
            if (p.Priority != Priority.Ambient)
            {
                _idleWait = IdleSeconds;
            }

            _shownPriority = p.Priority;
            return Format(line.Text, p.Vars);
        }

        private AdvisorLine? Find(string id)
        {
            foreach (AdvisorLine line in _lines.All)
            {
                if (line.Id == id)
                {
                    return line;
                }
            }

            return null;
        }

        /// <summary>Rotates through the neutral lines and those of the active tones, skipping lines with unknown placeholders.</summary>
        private AdvisorLine? Choose(Pending p)
        {
            var pool = new List<AdvisorLine>();
            foreach (AdvisorLine line in _lines.For(p.Trigger))
            {
                if (_tones.Contains(line.Tone) && HasAll(line.Text, p.Vars))
                {
                    pool.Add(line);
                }
            }

            if (pool.Count == 0)
            {
                return null;
            }

            _cursor.TryGetValue(p.Trigger, out int k);
            AdvisorLine pick = pool[k % pool.Count];
            if (pick.Id == _lastId && pool.Count > 1)
            {
                k++;
                pick = pool[k % pool.Count];
            }

            _cursor[p.Trigger] = k + 1;
            return pick;
        }

        private static HashSet<Tone> TonesFor(GameState s)
        {
            var tones = new HashSet<Tone> { Tone.Neutral };
            if (s.ColdnessMilli >= 50_000)
            {
                tones.Add(Tone.Cold);
            }

            if (s.BoldnessMilli >= 50_000)
            {
                tones.Add(Tone.Bold);
            }

            if (s.ColdnessMilli < 25_000 && s.BoldnessMilli < 25_000)
            {
                tones.Add(Tone.Warm);
            }

            return tones;
        }

        private static bool HasAll(string text, Dictionary<string, string> vars)
        {
            int i = 0;
            while ((i = text.IndexOf('{', i)) >= 0)
            {
                int end = text.IndexOf('}', i);
                if (end < 0 || !vars.ContainsKey(text.Substring(i + 1, end - i - 1)))
                {
                    return false;
                }

                i = end;
            }

            return true;
        }

        private static string Format(string text, Dictionary<string, string> vars)
        {
            var sb = new StringBuilder(text);
            foreach (KeyValuePair<string, string> kv in vars)
            {
                sb.Replace("{" + kv.Key + "}", kv.Value);
            }

            return sb.ToString();
        }

        private sealed class Pending
        {
            public Pending(string trigger, Priority priority)
            {
                Trigger = trigger;
                Priority = priority;
            }

            public string Trigger { get; }

            public Priority Priority { get; }

            public string? FixedId { get; set; }

            public float Age { get; set; }

            public Dictionary<string, string> Vars { get; } = new Dictionary<string, string>();

            public Pending With(string key, string value)
            {
                Vars[key] = value;
                return this;
            }
        }
    }
}
