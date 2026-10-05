using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Base
{
    /// <summary>
    /// Bottom sheet for one plot (SPEC-003 rule 7): build options on an empty plot, progress and cancel while
    /// building, otherwise stats now -> next level, upgrade, power, priority and demolish. Every action is a sim
    /// command; refusals are explained in the advisor's voice. Costs the handler cannot pay are shown in red.
    /// </summary>
    public sealed class SlotSheet
    {
        private readonly VisualElement _root;
        private readonly VisualElement _content;
        private readonly System.Action _onClose;
        private bool _confirmDemolish;
        private FacilityKind _recommend = FacilityKind.None;
        private bool _expanded;
        private string _reason = string.Empty;

        public SlotSheet(VisualElement layer, System.Action onClose)
        {
            _onClose = onClose;
            _root = new VisualElement();
            _root.AddToClassList("ds-sheet");
            _root.AddToClassList("sheet");
            _root.AddToClassList("is-hidden");
            Sheen.Attach(_root);
            var grip = new VisualElement();
            grip.AddToClassList("ds-sheet__grip");
            _root.Add(grip);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("sheet__scroll");
            _root.Add(scroll);
            _content = scroll.contentContainer;
            _root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            layer.Add(_root);
        }

        public int Slot { get; private set; } = -1;

        public bool IsOpen => Slot >= 0;

        public void Open(int slot)
        {
            Slot = slot;
            _expanded = false;
            _confirmDemolish = false;
            _reason = string.Empty;
            _root.RemoveFromClassList("is-hidden");
            Refresh();
        }

        /// <summary>Shows the full stats and every action (the compact card shows status only, idea 24).</summary>
        public void Expand()
        {
            _expanded = true;
            Refresh();
        }

        /// <summary>Puts a facility first and marks it (a resource shortcut led here, SPEC-039 idea 15).</summary>
        public void Recommend(FacilityKind kind)
        {
            _recommend = kind;
            Refresh();
        }

        public void Close()
        {
            Slot = -1;
            _recommend = FacilityKind.None;
            _root.AddToClassList("is-hidden");
        }

        public void Refresh()
        {
            if (Slot < 0)
            {
                return;
            }

            GameHost host = GameHost.Instance;
            GameState s = host.Sim.State;
            _content.Clear();
            FacilitySlot slot = s.Slots[Slot];
            BuildJob job = s.JobForSlot(Slot);
            if (job != null)
            {
                Building(host, slot, job);
            }
            else if (slot.IsEmpty)
            {
                Empty(host);
            }
            else
            {
                Facility(host, slot);
            }

            if (_reason.Length > 0)
            {
                _content.Add(Kit.Label(_reason, "ds-body", "sheet__reason"));
            }
        }

        /// <summary>Live progress for an open construction sheet.</summary>
        public void Tick()
        {
            if (Slot < 0)
            {
                return;
            }

            GameHost host = GameHost.Instance;
            BuildJob job = host.Sim.State.JobForSlot(Slot);
            VisualElement fill = _content.Q(className: "sheet__progress-fill");
            Label time = _content.Q<Label>(className: "sheet__eta");
            if (job == null || fill == null || time == null)
            {
                return;
            }

            float total = job.CompleteTick - job.StartTick;
            float done = (host.Sim.State.Tick - job.StartTick) + host.TickProgress;
            fill.style.width = Length.Percent(UnityEngine.Mathf.Clamp01(done / UnityEngine.Mathf.Max(1f, total)) * 100f);
            time.text = Fmt.Countdown(SecondsUntil(host, job.CompleteTick));
        }

        private void Header(string title, string level, string glyph)
        {
            var head = new VisualElement();
            head.AddToClassList("sheet__head");
            var well = new VisualElement();
            well.AddToClassList("sheet__well");
            well.Add(Icons.Create(glyph, "sheet__icon"));
            head.Add(well);
            var titles = new VisualElement();
            titles.AddToClassList("sheet__titles");
            titles.Add(Kit.Label(title, "sheet__title"));
            if (level.Length > 0)
            {
                titles.Add(Kit.Label(level, "sheet__level"));
            }

            head.Add(titles);
            var close = new VisualElement();
            close.AddToClassList("ds-iconbtn");
            close.Add(Icons.Create("close", "ds-iconbtn__icon"));
            close.RegisterCallback<ClickEvent>(_ => _onClose());
            head.Add(close);
            _content.Add(head);
        }

        /// <summary>Output, upkeep and crew at a glance (now, and next level when there is one).</summary>
        private static VisualElement StatTiles(FacilitySlot slot, FacilityConfig f)
        {
            bool max = slot.Level >= f.MaxLevel;
            var row = Row("sheet__tiles");
            row.Add(StatTile("trendup", "OUTPUT", Texts.Output(slot.Kind, f.Output[slot.Level - 1]), max ? null : Texts.Output(slot.Kind, f.Output[slot.Level])));
            row.Add(StatTile("bolt", "UPKEEP", Fmt.Num(f.UpkeepPerHour[slot.Level - 1]) + "/H", max ? null : Fmt.Num(f.UpkeepPerHour[slot.Level]) + "/H"));
            row.Add(StatTile("people", "CREW", Fmt.Num(f.Crew[slot.Level - 1]), max ? null : Fmt.Num(f.Crew[slot.Level])));
            return row;
        }

        private static VisualElement StatTile(string glyph, string key, string now, string next)
        {
            var tile = Row("sheet__tile");
            var top = Row("sheet__tile-top");
            top.Add(Icons.Create(glyph, "sheet__tile-icon"));
            top.Add(Kit.Label(key, "sheet__tile-key"));
            tile.Add(top);
            tile.Add(Kit.Label(now, "sheet__tile-value"));
            if (next != null)
            {
                tile.Add(Kit.Label("NEXT  " + next, "sheet__tile-next"));
            }

            return tile;
        }

        /// <summary>Build categories (SPEC-042 finding 7): compare like with like instead of scrolling one long list.</summary>
        private static readonly (string Name, string Glyph, FacilityKind[] Kinds)[] Categories =
        {
            ("POWER", "bolt", new[] { FacilityKind.Generator, FacilityKind.SolarField, FacilityKind.BatteryBank, FacilityKind.Reactor }),
            ("COMPUTE", "chip", new[] { FacilityKind.ServerRack, FacilityKind.CoolingTower, FacilityKind.MemoryChamber }),
            ("PEOPLE", "people", new[] { FacilityKind.LifeSupport, FacilityKind.FuelDepot }),
            ("DEFENSE", "shield", new[] { FacilityKind.Turret, FacilityKind.DroneBay, FacilityKind.MotorPool }),
        };

        private static int _category;

        private void Empty(GameHost host)
        {
            Header("OPEN PLOT", "PLOT " + (Slot + 1) + "  //  CHOOSE WHAT TO BUILD", "plus");
            if (_recommend != FacilityKind.None)
            {
                for (int i = 0; i < Categories.Length; i++)
                {
                    if (System.Array.IndexOf(Categories[i].Kinds, _recommend) >= 0)
                    {
                        _category = i;
                    }
                }
            }

            var tabs = Row("ds-seg");
            tabs.AddToClassList("build-tabs");
            for (int i = 0; i < Categories.Length; i++)
            {
                var tab = Row("ds-seg__item");
                tab.AddToClassList("build-tab");
                tab.EnableInClassList("is-selected", i == _category);
                tab.Add(Icons.Create(Categories[i].Glyph, "build-tab__icon"));
                tab.Add(Kit.Label(Categories[i].Name, "build-tab__label"));
                int index = i;
                tab.RegisterCallback<ClickEvent>(_ =>
                {
                    _category = index;
                    Refresh();
                    Choreo.Enter(_content);
                });
                tabs.Add(tab);
            }

            _content.Add(tabs);
            var grid = Row("build-grid");
            foreach (FacilityKind kind in Categories[_category].Kinds)
            {
                grid.Add(BuildCard(host, kind));
            }

            _content.Add(grid);
        }

        /// <summary>One build card: icon, name, the number that matters, cost, then build time or when it is affordable.</summary>
        private VisualElement BuildCard(GameHost host, FacilityKind kind)
        {
            GameState s = host.Sim.State;
            SimConfig c = host.Config;
            FacilityConfig f = c.Facility(kind);
            string locked = null;
            if (kind == FacilityKind.Reactor && s.Tier < c.ReactorRules.MinTier)
            {
                locked = "TIER " + c.ReactorRules.MinTier;
            }
            else if (kind == FacilityKind.Reactor && Economy.CountOfKind(s, kind) >= c.ReactorRules.MaxCount)
            {
                locked = "ONE PER HUB";
            }
            else if (kind == FacilityKind.MotorPool && s.Tier < c.Units.MotorPoolMinTier)
            {
                locked = "TIER " + c.Units.MotorPoolMinTier;
            }

            Economy.BuildCost(s, c, kind, out int energy, out int compute);
            string when = locked != null ? string.Empty : Afford.When(s, c, energy, compute, 3600.0 / host.Settings.DevTimeScale);
            var card = Row("build-card");
            card.EnableInClassList("is-locked", locked != null);
            card.EnableInClassList("is-waiting", locked == null && when.Length > 0);
            card.EnableInClassList("is-recommended", kind == _recommend);
            Sheen.Attach(card);
            var top = Row("build-card__top");
            var well = Row("build-card__well");
            well.Add(Icons.Create(locked != null ? "lock" : Icons.ForFacility(kind), "build-card__icon"));
            top.Add(well);
            if (kind == _recommend)
            {
                top.Add(Kit.Label("SUGGESTED", "build-card__tag"));
            }

            card.Add(top);
            card.Add(Kit.Label(Fmt.FacilityName(kind), "build-card__name"));
            card.Add(Kit.Label(Texts.Output(kind, f.Output[0]), "build-card__out"));
            if (locked != null)
            {
                card.Add(Kit.Label("LOCKED // " + locked, "build-card__foot"));
                return card;
            }

            card.Add(Cost(host, energy, compute));
            card.Add(Kit.Label(when.Length > 0 ? when : "BUILD " + Fmt.SpanCoarse(Economy.BuildMinutes(s, c, kind, 1) * 60.0 / host.Settings.DevTimeScale), "build-card__foot"));
            FacilityKind k = kind;
            card.RegisterCallback<ClickEvent>(_ =>
            {
                if (card.ClassListContains("is-waiting"))
                {
                    // not yet: say when instead of a bare refusal
                    Kit.Shake(card);
                    Toasts.Show("clock", Fmt.FacilityName(k) + " // AFFORDABLE " + when, Toasts.Tone.Warn);
                    return;
                }

                Run(Command.Build(Slot, k));
            });
            return card;
        }

        private void Building(GameHost host, FacilitySlot slot, BuildJob job)
        {
            Header(Fmt.FacilityName(job.Kind), "BUILDING LEVEL " + job.TargetLevel, Icons.ForFacility(job.Kind));
            var chips = Row("sheet__chips");
            chips.Add(Chip("UNDER CONSTRUCTION", "ds-chip--amber"));
            _content.Add(chips);
            var bar = new VisualElement();
            bar.AddToClassList("sheet__progress");
            var fill = new VisualElement();
            fill.AddToClassList("sheet__progress-fill");
            bar.Add(fill);
            _content.Add(bar);
            _content.Add(Kit.Label(Fmt.Countdown(SecondsUntil(host, job.CompleteTick)), "ds-value", "sheet__eta"));
            if (!slot.IsEmpty)
            {
                _content.Add(Kit.Label("Still running at level " + slot.Level + " while the crew works.", "ds-body", "sheet__blurb"));
            }

            var actions = Row("sheet__actions");
            actions.Add(Kit.Button("CANCEL JOB", () => Run(Command.CancelJob(Slot)), "ds-btn--ghost"));
            _content.Add(actions);
            Tick();
        }

        private void Facility(GameHost host, FacilitySlot slot)
        {
            SimConfig c = host.Config;
            FacilityConfig f = c.Facility(slot.Kind);
            GameState s = host.Sim.State;
            Header(Fmt.FacilityName(slot.Kind), "LEVEL " + slot.Level + " / " + f.MaxLevel, Icons.ForFacility(slot.Kind));

            var chips = Row("sheet__chips");
            chips.Add(slot.Enabled ? (slot.Powered ? Chip("POWERED", "ds-chip--phosphor") : Chip("NO POWER", "ds-chip--red")) : Chip("SWITCHED OFF", "ds-chip--red"));
            if (slot.Enabled)
            {
                chips.Add(slot.Staffed ? Chip("CREWED " + Economy.CrewNeeded(c, slot), string.Empty) : Chip("AI-RUN", "ds-chip--amber"));
            }

            chips.Add(Chip("PRIORITY #" + (s.PowerPriority.IndexOf(Slot) + 1), string.Empty));
            bool repairing = ScarSystem.Repairing(s, slot);
            if (slot.Damage > 0)
            {
                chips.Add(Chip((repairing ? "REPAIRING " : "DAMAGED ") + slot.Damage + "/" + c.Scars.MaxDamage, repairing ? "ds-chip--amber" : "ds-chip--red"));
            }

            if (slot.Kind == FacilityKind.MotorPool)
            {
                chips.Add(s.Fuel > 0 ? Chip("FUEL -" + c.Units.MotorPoolFuelPerHour[System.Math.Min(slot.Level, c.Units.MotorPoolFuelPerHour.Length) - 1] + "/H", string.Empty) : Chip("NO FUEL // VEHICLES IDLE", "ds-chip--red"));
            }

            if (slot.Kind == FacilityKind.Reactor)
            {
                chips.Add(s.ReactorFueled ? Chip("FUEL -" + ReactorSystem.FuelPerHour(s, c) + "/H", string.Empty) : Chip("SCRAMMED // NO FUEL", "ds-chip--red"));
                if (slot.Damage >= c.ReactorRules.LeakDamage)
                {
                    chips.Add(Chip("RADIATION LEAK", "ds-chip--red"));
                }
            }

            _content.Add(chips);
            _content.Add(Kit.Label(Fmt.FacilityBlurb(slot.Kind), "ds-body", "sheet__blurb"));
            if (slot.Damage > 0)
            {
                // battle scars (SPEC-018): what the damage costs and what fixing it takes
                int lost = ScarSystem.PenaltyPct(s, c, slot);
                _content.Add(Kit.Label(repairing
                    ? "Crews are on it. Full output in " + Fmt.Countdown(SecondsUntil(host, slot.RepairUntilTick)) + "."
                    : (lost > 0 ? "Battle damage: output -" + lost + "% until repaired." : "Battle damage. It still works; it looks like it lost."), "ds-body", "sheet__blurb", "t-amber"));
            }

            _content.Add(StatTiles(slot, f));
            if (!_expanded)
            {
                // compact card: the quick-action tiles beside the facility carry the common actions
                _content.Add(Kit.Button("ALL DETAILS AND ACTIONS", Expand, "ds-btn--ghost", "sheet__more"));
                return;
            }

            bool max = slot.Level >= f.MaxLevel;

            var actions = Row("sheet__actions");
            if (slot.Damage > 0 && !repairing)
            {
                int cost = ScarSystem.RepairCost(c, slot);
                var fix = Kit.Button("REPAIR  " + Fmt.Num(cost) + " E  " + Fmt.Countdown(c.Scars.RepairMinutesPerPoint * slot.Damage * 60.0 / host.Settings.DevTimeScale), () => Run(Command.Repair(Slot)), "ds-btn--warn", "sheet__primary");
                fix.EnableInClassList("is-disabled", s.Energy < cost || s.RaidId != 0);
                actions.Add(fix);
            }

            if (!max)
            {
                Economy.UpgradeCost(c, slot.Kind, slot.Level, out int energy, out int compute);
                var up = Kit.Button("UPGRADE  " + Fmt.Countdown(Economy.BuildMinutes(host.Sim.State, c, slot.Kind, slot.Level + 1) * 60.0 / host.Settings.DevTimeScale), () => Run(Command.Upgrade(Slot)), "ds-btn--primary", "sheet__primary");
                up.EnableInClassList("is-disabled", s.Energy < energy || s.Compute < compute);
                actions.Add(up);
                _content.Add(Cost(host, energy, compute));
            }
            else
            {
                actions.Add(Kit.Button("MAX LEVEL", null, "ds-btn--ghost", "is-disabled"));
            }

            actions.Add(Kit.Button(slot.Enabled ? "POWER OFF" : "POWER ON", () => Run(Command.SetFacilityPower(Slot, !slot.Enabled))));
            int rank = s.PowerPriority.IndexOf(Slot);
            if (rank > 0)
            {
                actions.Add(Kit.Button("PRIORITY UP", () => Run(Command.SetPriority(Slot, rank - 1))));
            }

            actions.Add(Kit.Button(_confirmDemolish ? "CONFIRM DEMOLISH" : "DEMOLISH", Demolish, _confirmDemolish ? "ds-btn--warn" : "ds-btn--ghost"));
            _content.Add(actions);
        }

        private void Demolish()
        {
            if (!_confirmDemolish)
            {
                _confirmDemolish = true;
                _reason = "Demolition returns a quarter of the materials. Tap again to confirm.";
                Refresh();
                return;
            }

            _confirmDemolish = false;
            Run(Command.Demolish(Slot));
        }

        private void Run(Command command)
        {
            GameHost host = GameHost.Instance;
            CommandResult r = host.Execute(command);
            _reason = r.Accepted ? string.Empty : Texts.Reason(r.Reason);
            Hud.HudController.Instance?.Advisor.Say(r.Accepted ? Texts.Ack(command) : Texts.Reason(r.Reason));
            if (r.Accepted)
            {
                Toasts.Show("check", "ORDER ACCEPTED", Toasts.Tone.Good);
            }
            else
            {
                Toasts.Show("alert", "REFUSED", Toasts.Tone.Bad);
            }
            Refresh();
        }

        private VisualElement Cost(GameHost host, int energy, int compute)
        {
            var row = Row("cost");
            var e = Kit.Label("E " + Fmt.Num(energy), "cost__item");
            e.EnableInClassList("is-short", host.Sim.State.Energy < energy);
            row.Add(e);
            if (compute > 0)
            {
                var cpu = Kit.Label("C " + Fmt.Num(compute), "cost__item");
                cpu.EnableInClassList("is-short", host.Sim.State.Compute < compute);
                row.Add(cpu);
            }

            return row;
        }

        private static double SecondsUntil(GameHost host, long tick)
        {
            long ticks = tick - host.Sim.State.Tick;
            return ticks <= 0 ? 0 : host.SecondsToNextTick + ((ticks - 1) * 60.0 / host.Settings.DevTimeScale);
        }

        private static VisualElement Row(string cls)
        {
            var r = new VisualElement();
            r.AddToClassList(cls);
            return r;
        }

        private static VisualElement Chip(string text, string tone)
        {
            var chip = new VisualElement();
            chip.AddToClassList("ds-chip");
            if (!string.IsNullOrEmpty(tone))
            {
                chip.AddToClassList(tone);
            }

            chip.Add(Kit.Label(text, "ds-chip__label"));
            return chip;
        }
    }
}
