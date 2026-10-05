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
        private static readonly FacilityKind[] Buildable =
        {
            FacilityKind.Generator, FacilityKind.SolarField, FacilityKind.ServerRack, FacilityKind.BatteryBank, FacilityKind.FuelDepot, FacilityKind.LifeSupport,
            FacilityKind.Turret, FacilityKind.DroneBay, FacilityKind.MotorPool, FacilityKind.CoolingTower, FacilityKind.MemoryChamber, FacilityKind.Reactor,
        };

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

        private void Header(string title, string level)
        {
            var head = new VisualElement();
            head.AddToClassList("sheet__head");
            head.Add(Kit.Label(title, "ds-display", "sheet__title"));
            if (level.Length > 0)
            {
                head.Add(Chip(level, "ds-chip--phosphor"));
            }

            var close = new VisualElement();
            close.AddToClassList("sheet__close");
            close.Add(Kit.Label("x", "sheet__close-x"));
            close.RegisterCallback<ClickEvent>(_ => _onClose());
            head.Add(close);
            _content.Add(head);
        }

        private void Empty(GameHost host)
        {
            Header("OPEN PLOT", "P" + (Slot + 1));
            _content.Add(Kit.Label("Cleared ground inside the wire. Tell me what to put here.", "ds-body", "sheet__blurb"));
            var order = new System.Collections.Generic.List<FacilityKind>(Buildable);
            if (order.Remove(_recommend))
            {
                order.Insert(0, _recommend);
            }

            foreach (FacilityKind kind in order)
            {
                // the reactor (SPEC-029) is offered only from its tier, and only one per Hub
                if (kind == FacilityKind.Reactor && (host.Sim.State.Tier < host.Config.ReactorRules.MinTier || Economy.CountOfKind(host.Sim.State, kind) >= host.Config.ReactorRules.MaxCount))
                {
                    continue;
                }

                // vehicles (SPEC-035) come with the district
                if (kind == FacilityKind.MotorPool && host.Sim.State.Tier < host.Config.Units.MotorPoolMinTier)
                {
                    continue;
                }

                FacilityConfig f = host.Config.Facility(kind);
                Economy.BuildCost(host.Sim.State, host.Config, kind, out int energy, out int compute);
                bool affordable = host.Sim.State.Energy >= energy && host.Sim.State.Compute >= compute;

                var opt = new VisualElement();
                opt.AddToClassList("opt");
                opt.EnableInClassList("is-disabled", !affordable);
                opt.EnableInClassList("opt--recommended", kind == _recommend);
                opt.Add(Icons.Create(Icons.ForFacility(kind), "opt__icon"));
                var body = new VisualElement();
                body.AddToClassList("opt__body");
                body.Add(Kit.Label(Fmt.FacilityName(kind), "opt__name"));
                string upkeep = f.UpkeepPerHour[0] > 0 ? "   -" + Fmt.Num(f.UpkeepPerHour[0]) + " ENERGY/H" : kind == FacilityKind.Reactor ? "   -" + host.Config.ReactorRules.FuelPerHour[0] + " FUEL/H" : string.Empty;
                upkeep += kind == FacilityKind.MotorPool ? "   -" + host.Config.Units.MotorPoolFuelPerHour[0] + " FUEL/H" : string.Empty;
                body.Add(Kit.Label(Texts.Output(kind, f.Output[0]) + upkeep, "opt__desc"));
                body.Add(Cost(host, energy, compute));
                opt.Add(body);
                opt.Add(Kit.Label(Fmt.Countdown(Economy.BuildMinutes(host.Sim.State, host.Config, kind, 1) * 60.0 / host.Settings.DevTimeScale), "opt__time"));
                FacilityKind k = kind;
                opt.RegisterCallback<ClickEvent>(_ => Run(Command.Build(Slot, k)));
                _content.Add(opt);
            }
        }

        private void Building(GameHost host, FacilitySlot slot, BuildJob job)
        {
            Header(Fmt.FacilityName(job.Kind), "LVL " + job.TargetLevel);
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
            Header(Fmt.FacilityName(slot.Kind), "LVL " + slot.Level);

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

            if (!_expanded)
            {
                // compact card: the quick-action tiles beside the facility carry the common actions
                _content.Add(Kit.Button("ALL DETAILS AND ACTIONS", Expand, "ds-btn--ghost", "sheet__more"));
                return;
            }

            bool max = slot.Level >= f.MaxLevel;
            var stats = new VisualElement();
            stats.AddToClassList("sheet__stats");
            stats.Add(Kv("Output", Texts.Output(slot.Kind, f.Output[slot.Level - 1]) + (max ? string.Empty : "  ->  " + Texts.Output(slot.Kind, f.Output[slot.Level]))));
            stats.Add(Kv("Upkeep", Fmt.Num(f.UpkeepPerHour[slot.Level - 1]) + "/H" + (max ? string.Empty : "  ->  " + Fmt.Num(f.UpkeepPerHour[slot.Level]) + "/H")));
            stats.Add(Kv("Crew", Fmt.Num(f.Crew[slot.Level - 1]) + (max ? string.Empty : "  ->  " + Fmt.Num(f.Crew[slot.Level]))));
            _content.Add(stats);

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

        private static VisualElement Kv(string key, string value)
        {
            var row = Row("ds-kv");
            row.Add(Kit.Label(key, "ds-kv__key"));
            row.Add(Kit.Label(value, "ds-kv__value"));
            return row;
        }
    }
}
