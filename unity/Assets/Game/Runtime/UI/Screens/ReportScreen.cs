using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using Deadswitch.Game.Reports;
using Deadswitch.Host.Narrative;
using Deadswitch.Host.Reports;
using Deadswitch.Sim.Commands;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// After-action report (SPEC-006): four graphic-novel stills rendered from the compound, the AI's summary,
    /// the true loss ledger, and Verify. Opened from the HUD chip or OPS; closes back to the base.
    /// </summary>
    public sealed class ReportScreen : IGameScreen
    {
        private readonly ScreenRouter _router;
        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private RenderTexture[] _stills;
        private int _raid;

        public ReportScreen(ScreenRouter router)
        {
            _router = router;
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Report");
            Root.Add(tree);
            _ui = tree;
            _ui.Q("rep-close").RegisterCallback<ClickEvent>(_ => _router.Show("base"));
            _ui.Q("rep-verify").RegisterCallback<ClickEvent>(_ => Verify());
        }

        public string Id => "report";

        public VisualElement Root { get; }

        /// <summary>Shows the report for a resolved raid.</summary>
        public void Open(int raidId)
        {
            BattleReport report = BattleReport.Build(_host.Sim.Log.Events, raidId);
            if (report == null)
            {
                return;
            }

            _raid = raidId;
            ReportStills.Release(_stills);
            _stills = ReportStills.Capture(report, BaseView.Seed);
            for (int i = 0; i < _stills.Length; i++)
            {
                _ui.Q("panel-" + i).style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_stills[i]));
            }

            Fill(report);
            _router.Show(Id);
        }

        public void OnShow()
        {
            _ui.Q<Label>("rep-reason").text = string.Empty;
        }

        public void OnHide()
        {
            ReportStills.Release(_stills);
            _stills = null;
        }

        private void Fill(BattleReport r)
        {
            _ui.Q<Label>("rep-title").text = "AFTER-ACTION // " + Names.Attack(r.Kind) + " " + r.RaidId + " // " + r.Outcome.ToString().ToUpperInvariant();
            var vector = _ui.Q<Label>("rep-vector");
            vector.text = "PREDICTED " + Host.Narrative.Names.Gate(r.PredictedGate) + "  //  CONTACT " + Host.Narrative.Names.Gate(r.ContactGate);
            vector.EnableInClassList("is-mismatch", r.PredictionMismatch);
            for (int i = 0; i < r.Panels.Count; i++)
            {
                _ui.Q<Label>("cap-" + i).text = r.Panels[i].Caption;
            }

            _ui.Q<Label>("rep-summary").text = "> " + r.Summary;
            _ui.Q<Label>("rep-ledger").text = r.LossText(false);
            FillOutcomeAndLosses(r);
            _ui.Q<Label>("rep-state-label").text = r.Verified ? "VERIFIED" : "UNVERIFIED";
            _ui.Q("rep-state").EnableInClassList("ds-chip--amber", !r.Verified);
            _ui.Q("rep-state").EnableInClassList("ds-chip--phosphor", r.Verified);

            VisualElement findings = _ui.Q("rep-findings");
            findings.Clear();
            findings.EnableInClassList("is-hidden", !r.Verified);
            foreach (string f in r.Findings)
            {
                Label line = Kit.Label(f, "rep-finding");
                line.EnableInClassList("is-bad", f.EndsWith("MISMATCH", System.StringComparison.Ordinal) || f.EndsWith("ALTERED", System.StringComparison.Ordinal));
                findings.Add(line);
            }

            VisualElement verify = _ui.Q("rep-verify");
            verify.style.display = r.Verified ? DisplayStyle.None : DisplayStyle.Flex;
            Kit.SetButtonText(verify, "VERIFY // " + _host.Sim.Config.Report.VerifyComputeCost + " COMPUTE");
        }

        private void FillOutcomeAndLosses(BattleReport r)
        {
            Label stamp = _ui.Q<Label>("rep-outcome");
            if (stamp != null)
            {
                stamp.text = r.Outcome.ToString().ToUpperInvariant();
                stamp.EnableInClassList("is-bad", r.Outcome == Deadswitch.Sim.State.RaidOutcome.Breached);
                stamp.EnableInClassList("is-good", r.Outcome == Deadswitch.Sim.State.RaidOutcome.Repelled);
            }

            VisualElement items = _ui.Q("rep-loss");
            if (items == null)
            {
                return;
            }

            items.Clear();
            foreach (string part in r.LossText(false).Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string glyph = part.Contains("ENERGY") ? "bolt" : part.Contains("COMPUTE") ? "chip" : part.Contains("PEOPLE") ? "people" : part.Contains("FUEL") ? "fuel" : part.Contains("DOWN TO") ? "wrench" : "check";
                var row = new VisualElement();
                row.AddToClassList("rep-loss__item");
                row.Add(Icons.Create(glyph, "rep-loss__icon"));
                row.Add(Kit.Label(part, "rep-loss__label"));
                items.Add(row);
            }
        }

        private void Verify()
        {
            CommandResult result = _host.Execute(Command.VerifyReport(_raid));
            _ui.Q<Label>("rep-reason").text = result.Accepted ? string.Empty : Presentation.Texts.Reason(result.Reason);
            BattleReport report = BattleReport.Build(_host.Sim.Log.Events, _raid);
            if (report != null)
            {
                Fill(report);
            }
        }
    }
}
