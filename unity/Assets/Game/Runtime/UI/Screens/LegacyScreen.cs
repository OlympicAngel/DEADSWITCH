using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// LEGACY (SPEC-022): the cycle at a glance. Legacy score and its breakdown, mastery challenges, perks bought with
    /// legacy points, and RELOCATE (two taps: it ends this Hub). Opened from CORE; opens itself after a cycle ends.
    /// Layout: Resources/UI/Legacy.uxml.
    /// </summary>
    public sealed class LegacyScreen : IGameScreen
    {
        private const long ArmMs = 4000;

        private static readonly string[] MasteryNames =
        {
            "SURVIVE A PURGE WITHOUT THE AI", "REACH TIER 2 WITHOUT LOSING AN OUTPOST", "WIN A LIVE BATTLE WITH NO CASUALTIES",
            "COMPLETE A TIER ON MANUAL", "CATCH MY FIRST LIE WITHIN A DAY", "KEEP CORRUPTION UNDER 40% FOR A TIER", "RELOCATE AT PEAK POWER",
            "OUTLAST A COLLAPSE WITHOUT A ROLLBACK",
        };

        private static readonly string[] PerkNames = { "STARTING CACHE", "REBUILDING SURGE", "SPARE OVERRIDE", "COLD TRAIL", "OLD GUARD" };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly Label _reason;
        private long _armedAt = -1;
        private Region _region = Region.Ridge;
        private bool _visible;

        public LegacyScreen(ScreenRouter router)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Legacy");
            Root.Add(tree);
            _ui = tree;
            _reason = _ui.Q<Label>("lgc-reason");
            _ui.Q("lgc-close").RegisterCallback<ClickEvent>(_ => router.Show("core"));
            _ui.Q("lgc-move-btn").RegisterCallback<ClickEvent>(_ => Move());
            for (int i = 0; i <= (int)Region.Ruins; i++)
            {
                var region = (Region)i;
                _ui.Q("lgc-region-" + i).RegisterCallback<ClickEvent>(_ =>
                {
                    _region = region;
                    _armedAt = -1;
                    Refresh();
                });
            }
            _ui.Q("lgc-iron-btn").RegisterCallback<ClickEvent>(_ =>
            {
                // Ironman is a premium mode (ADR-0006)
                if (!Store.Entitlements.Instance.HasPremium)
                {
                    _reason.text = "Ironman is part of the full game.";
                    return;
                }

                Run(Command.SetIronman(!_host.Sim.State.Ironman));
            });

            VisualElement mastery = _ui.Q("lgc-mastery");
            mastery.Clear();
            for (int i = 0; i < MasteryNames.Length; i++)
            {
                var row = new VisualElement { name = "m-" + i };
                row.AddToClassList("row");
                row.AddToClassList("lgc-m");
                var pip = new VisualElement();
                pip.AddToClassList("lgc-m__pip");
                row.Add(pip);
                row.Add(Kit.Label(MasteryNames[i], "lgc-m__label"));
                mastery.Add(row);
            }

            VisualElement perks = _ui.Q("lgc-perks");
            perks.Clear();
            for (int i = 0; i < LegacySystem.PerkCount; i++)
            {
                var perk = (Perk)i;
                var row = new VisualElement { name = "p-" + i };
                row.AddToClassList("lgc-perk");
                var body = new VisualElement();
                body.AddToClassList("lgc-perk__body");
                body.Add(Kit.Label(PerkNames[i], "lgc-perk__name"));
                Label fx = Kit.Label(string.Empty, "lgc-perk__fx");
                fx.name = "p-" + i + "-fx";
                body.Add(fx);
                row.Add(body);
                VisualElement buy = Kit.Button("BUY", () => Run(Command.BuyPerk(perk)), "ds-btn--ghost", "lgc-perk__btn");
                buy.name = "p-" + i + "-buy";
                row.Add(buy);
                perks.Add(row);
            }

            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
        }

        public string Id => "legacy";

        public VisualElement Root { get; }

        public void OnShow()
        {
            _visible = true;
            Records.Hook(_host);
            Records.Update(_host);
            _armedAt = -1;
            _reason.text = string.Empty;
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
        }

        private void Move()
        {
            long now = System.Environment.TickCount;
            if (_armedAt < 0 || now - _armedAt > ArmMs)
            {
                _armedAt = now;
                _reason.text = "Tap again to leave this Hub. There is no way back to it.";
                Refresh();
                return;
            }

            _armedAt = -1;
            Run(Command.Relocate(_region));
        }

        private void Run(Command command)
        {
            CommandResult r = _host.Execute(command);
            _reason.text = r.Accepted ? string.Empty
                : r.Reason == RejectReason.Locked ? "Reach Tier " + _host.Sim.Config.Legacy.RelocateMinTier + " before the core can move."
                : r.Reason == RejectReason.NotEnoughLegacy ? "Not enough legacy points."
                : r.Reason == RejectReason.MaxLevel ? "That perk is at its highest level."
                : Texts.Reason(r.Reason);
            Refresh();
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;
            var l = c.Legacy;
            int score = LegacySystem.Score(s, c);
            int veterans = LegacySystem.Veterans(s, c);
            _ui.Q<Label>("lgc-cycle").text = "CYCLE " + (s.Cycle + 1) + " // " + s.Region.ToString().ToUpperInvariant() + " // LEGACY ON RECORD " + Fmt.Num(s.LegacyTotal);
            for (int i = 0; i <= (int)Region.Ruins; i++)
            {
                _ui.Q("lgc-region-" + i).EnableInClassList("is-selected", (int)_region == i);
            }

            _ui.Q<Label>("lgc-region-1-fx").text = "TURRETS +" + l.RidgeTurretPct + "%";
            _ui.Q<Label>("lgc-region-2-fx").text = "+" + l.RiverFuelPerHour + " FUEL/H";
            _ui.Q<Label>("lgc-region-3-fx").text = "COMPUTE +" + l.RuinsComputePct + "%";
            _ui.Q<Label>("lgc-points").text = Fmt.Num(s.LegacyPoints) + " LP";
            _ui.Q<Label>("lgc-score").text = Fmt.Num(score);
            _ui.Q<Label>("lgc-best").text = "PERSONAL BEST // " + Fmt.Num(Records.BestScore) + " // TIER " + Records.BestTier + " // " + Records.BestCycle + (Records.BestCycle == 1 ? " CYCLE" : " CYCLES");
            _ui.Q<Label>("lgc-breakdown").text = "TIER " + s.HighestTier + " x" + l.ScorePerTier + "  +  PEAK POWER " + Fmt.Num(s.PeakPower) + " / " + l.PowerDivisor
                + "  +  " + veterans + " VETERANS x" + l.ScorePerVeteran + "  +  " + LegacySystem.MasteryDone(s) + " MASTERY x" + l.ScorePerMastery;

            for (int i = 0; i < MasteryNames.Length; i++)
            {
                _ui.Q("m-" + i).EnableInClassList("is-done", (s.Mastery & (1 << i)) != 0);
            }

            string[] effects =
            {
                "+" + l.PerkStartEnergy + " energy and +" + l.PerkStartFuel + " fuel per level at each new site.",
                "Population regrowth +" + l.PerkRegrowthPct + "% per level.",
                "+1 OVERRIDE charge per level.",
                "Faction heat fades +" + l.PerkHeatDecayPct + "% faster per level.",
                "+1 veteran follows the core per level.",
            };
            for (int i = 0; i < LegacySystem.PerkCount; i++)
            {
                int level = s.Perks[i];
                bool max = level >= l.PerkMaxLevel;
                int price = LegacySystem.PerkPrice(s, c, (Perk)i);
                _ui.Q<Label>("p-" + i + "-fx").text = effects[i] + "  LEVEL " + level + "/" + l.PerkMaxLevel;
                VisualElement buy = _ui.Q("p-" + i + "-buy");
                Kit.SetButtonText(buy, max ? "MAX" : "BUY // " + price + " LP");
                buy.EnableInClassList("is-disabled", max || s.LegacyPoints < price);
            }

            bool choosing = s.Tick - s.CycleStartTick < (long)l.IronmanChooseHours * SimConfig.TicksPerHour;
            _ui.Q("lgc-iron").EnableInClassList("is-on", s.Ironman);
            _ui.Q<Label>("lgc-iron-title").text = "IRONMAN // " + (s.Ironman ? "THIS RUN" : choosing ? "CHOOSE IN THE FIRST " + l.IronmanChooseHours + " H" : "NEXT RUN");
            Kit.SetButtonText(_ui.Q("lgc-iron-btn"), s.Ironman ? "ON" : "OFF");
            _ui.Q("lgc-iron-btn").EnableInClassList("is-disabled", !choosing);

            bool can = s.HighestTier >= l.RelocateMinTier && s.RaidId == 0;
            _ui.Q<Label>("lgc-move-desc").text = "Leave this Hub at its peak. The core keeps " + l.KeptModules + " restored modules, " + l.HeatKeptPct + "% of every grudge, " + veterans
                + " veterans and the full score as legacy points. Everything else stays behind." + (s.HighestTier < l.RelocateMinTier ? " Reach Tier " + l.RelocateMinTier + " first." : string.Empty);
            VisualElement move = _ui.Q("lgc-move-btn");
            bool armed = _armedAt >= 0 && System.Environment.TickCount - _armedAt <= ArmMs;
            move.EnableInClassList("is-armed", armed);
            move.EnableInClassList("is-disabled", !can);
            Kit.SetButtonText(move, armed ? "CONFIRM // LEAVE THIS HUB" : "RELOCATE // +" + Fmt.Num(SimMath.PctFloor(score, l.VoluntaryBonusPct)) + " LP");
        }
    }
}
